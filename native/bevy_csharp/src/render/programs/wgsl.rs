//! The WGSL the bridge writes itself, put in front of what slangc compiled or in place of a stage
//! that has never compiled.

use super::Role;

// -- Bevy's lighting

/// What the names `bcs::light` and `bcs::finish` call in the WGSL Slang writes begin with, which
/// [`LIGHTING`] defines.
pub const LIGHTING_CALL: &str = "bcs_pbr_";

/// Functions of the same signatures as those [`LIGHTING`] defines, for reading a compiled shader
/// that calls them before the real ones, which import Bevy's, are put in front.
pub const LIGHTING_STAND_IN: &str = "fn bcs_pbr_light(base_color: vec4<f32>, emissive: vec4<f32>, \
    metallic: f32, roughness: f32, reflectance: vec3<f32>, occlusion: vec3<f32>, frag_coord: vec4<f32>, \
    world_position: vec4<f32>, world_normal: vec3<f32>, normal: vec3<f32>) -> vec4<f32> { return base_color; }\n\
    fn bcs_pbr_finish(color: vec4<f32>, frag_coord: vec4<f32>, world_position: vec4<f32>) -> vec4<f32> { return color; }\n";

/// What `bcs::light` and `bcs::finish` call: Bevy's own lighting of a standard material's surface,
/// over the view's lights, shadows and environment maps, and Bevy's own processing after it, which
/// is fog and, for a camera that does not draw in high dynamic range, tonemapping. Each is imported
/// from Bevy as its own materials import it.
///
/// They are put in front of a compiled fragment shader that calls them and of no other, since
/// importing Bevy's view bindings into a shader that does not light anything would only lengthen
/// its compile. The surface is a shadow receiver, which a standard material's mesh is unless told
/// otherwise, and takes fog, as one does by default.
const LIGHTING: &str = r#"#import bevy_pbr::{
    pbr_types,
    pbr_functions,
    mesh_types::MESH_FLAGS_SHADOW_RECEIVER_BIT,
    mesh_view_bindings::view,
}

fn bcs_pbr_input(frag_coord: vec4<f32>, world_position: vec4<f32>) -> pbr_types::PbrInput {
    var pbr_input = pbr_types::pbr_input_new();
    pbr_input.material.flags |= pbr_types::STANDARD_MATERIAL_FLAGS_FOG_ENABLED_BIT;
    pbr_input.frag_coord = frag_coord;
    pbr_input.world_position = world_position;
    pbr_input.is_orthographic = view.clip_from_world[3].w == 1.0;
    pbr_input.V = pbr_functions::calculate_view(world_position, pbr_input.is_orthographic);
    pbr_input.flags = MESH_FLAGS_SHADOW_RECEIVER_BIT;
    return pbr_input;
}

fn bcs_pbr_light(
    base_color: vec4<f32>,
    emissive: vec4<f32>,
    metallic: f32,
    roughness: f32,
    reflectance: vec3<f32>,
    occlusion: vec3<f32>,
    frag_coord: vec4<f32>,
    world_position: vec4<f32>,
    world_normal: vec3<f32>,
    normal: vec3<f32>,
) -> vec4<f32> {
    var pbr_input = bcs_pbr_input(frag_coord, world_position);
    pbr_input.material.base_color = base_color;
    pbr_input.material.emissive = emissive;
    pbr_input.material.metallic = metallic;
    pbr_input.material.perceptual_roughness = roughness;
    pbr_input.material.reflectance = reflectance;
    pbr_input.diffuse_occlusion = occlusion;
    pbr_input.world_normal = normalize(world_normal);
    pbr_input.N = normalize(normal);
    return pbr_functions::apply_pbr_lighting(pbr_input);
}

fn bcs_pbr_finish(color: vec4<f32>, frag_coord: vec4<f32>, world_position: vec4<f32>) -> vec4<f32> {
    return pbr_functions::main_pass_post_lighting_processing(bcs_pbr_input(frag_coord, world_position), color);
}
"#;

/// A compiled fragment shader with Bevy's lighting put in front where it calls for it.
pub(super) fn with_lighting(role: Role, wgsl: String) -> String {
    if matches!(role, Role::Fragment) && wgsl.contains(LIGHTING_CALL) {
        format!("{LIGHTING}\n{wgsl}")
    } else {
        wgsl
    }
}

// -- Fallbacks

/// A shader standing in for a stage that has never compiled, with the entry point it asked for.
pub(super) fn fallback_source(role: Role, entry: &str) -> String {
    match role {
        // Magenta, reading nothing but the position, so it is valid after any vertex shader and
        // in a pass as well as in a material.
        Role::Fragment | Role::Pass | Role::DrawFragment => format!(
            "@fragment\nfn {entry}(@builtin(position) position: vec4<f32>) -> @location(0) vec4<f32> {{\n    \
             let checker = (u32(position.x / 8.0) + u32(position.y / 8.0)) % 2u;\n    \
             return select(vec4<f32>(1.0, 0.0, 1.0, 1.0), vec4<f32>(0.1, 0.0, 0.1, 1.0), checker == 1u);\n}}\n"
        ),

        Role::Vertex => format!(
            r#"#import bevy_pbr::{{
    mesh_functions,
    forward_io::{{Vertex, VertexOutput}},
    view_transformations::position_world_to_clip,
}}

@vertex
fn {entry}(vertex: Vertex) -> VertexOutput {{
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(vertex.position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
#ifdef VERTEX_NORMALS
    out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
#endif
#ifdef VERTEX_UVS_A
    out.uv = vertex.uv;
#endif
#ifdef VERTEX_UVS_B
    out.uv_b = vertex.uv_b;
#endif
#ifdef VERTEX_TANGENTS
    out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
#endif
#ifdef VERTEX_COLORS
    out.color = vertex.color;
#endif
#ifdef VERTEX_OUTPUT_INSTANCE_INDEX
    out.instance_index = vertex.instance_index;
#endif
    return out;
}}
"#
        ),

        Role::PrepassVertex => format!(
            r#"#import bevy_pbr::{{
    mesh_functions,
    prepass_io::{{Vertex, VertexOutput}},
    view_transformations::position_world_to_clip,
}}

@vertex
fn {entry}(vertex: Vertex) -> VertexOutput {{
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(vertex.position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
#ifdef UNCLIPPED_DEPTH_ORTHO_EMULATION
    out.unclipped_depth = out.position.z;
    out.position.z = min(out.position.z, 1.0);
#endif
#ifdef VERTEX_UVS_A
    out.uv = vertex.uv;
#endif
#ifdef VERTEX_UVS_B
    out.uv_b = vertex.uv_b;
#endif
#ifdef NORMAL_PREPASS_OR_DEFERRED_PREPASS
#ifdef VERTEX_NORMALS
    out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
#endif
#ifdef VERTEX_TANGENTS
    out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
#endif
#endif
#ifdef MOTION_VECTOR_PREPASS_OR_DEFERRED_PREPASS
    let previous_world_from_local = mesh_functions::get_previous_world_from_local(vertex.instance_index);
    out.previous_world_position = mesh_functions::mesh_position_local_to_world(previous_world_from_local, vec4<f32>(vertex.position, 1.0));
#endif
#ifdef VERTEX_OUTPUT_INSTANCE_INDEX
    out.instance_index = vertex.instance_index;
#endif
    return out;
}}
"#
        ),

        // Every vertex at one point, which draws nothing, since what a draw's vertex shader reads
        // to place its geometry is not known here.
        Role::DrawVertex => format!(
            "@vertex\nfn {entry}(@builtin(vertex_index) index: u32) -> @builtin(position) vec4<f32> {{\n    \
             return vec4<f32>(0.0, 0.0, 0.0, 1.0);\n}}\n"
        ),

        // Does nothing, which is the only thing a compute shader can safely do without knowing
        // what the buffers it was handed hold.
        Role::Compute => {
            format!("@compute @workgroup_size(1)\nfn {entry}() {{\n}}\n")
        }

        // What Bevy's own prepass writes, which is a normal and a motion vector where the camera
        // asked for them, and nothing where it did not.
        Role::PrepassFragment => format!(
            r#"#import bevy_pbr::{{
    prepass_io::{{VertexOutput, FragmentOutput}},
    prepass_bindings,
    mesh_view_bindings::view,
}}

#ifdef PREPASS_FRAGMENT
@fragment
fn {entry}(in: VertexOutput) -> FragmentOutput {{
    var out: FragmentOutput;
#ifdef NORMAL_PREPASS
    out.normal = vec4(in.world_normal * 0.5 + vec3(0.5), 1.0);
#endif
#ifdef UNCLIPPED_DEPTH_ORTHO_EMULATION
    out.frag_depth = in.unclipped_depth;
#endif
#ifdef MOTION_VECTOR_PREPASS
    let clip_position_t = view.unjittered_clip_from_world * in.world_position;
    let clip_position = clip_position_t.xy / clip_position_t.w;
    let previous_clip_position_t = prepass_bindings::previous_view_uniforms.clip_from_world * in.previous_world_position;
    let previous_clip_position = previous_clip_position_t.xy / previous_clip_position_t.w;
    out.motion_vector = (clip_position - previous_clip_position) * vec2(0.5, -0.5);
#endif
    return out;
}}
#else
@fragment
fn {entry}(@builtin(position) position: vec4<f32>) {{
}}
#endif
"#
        ),
    }
}

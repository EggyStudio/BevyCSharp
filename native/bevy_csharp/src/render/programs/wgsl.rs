//! The WGSL the bridge writes itself, put in front of what slangc compiled or in place of a stage
//! that has never compiled.

use super::Role;

// -- What a shader reaches of Bevy's WGSL

/// WGSL the bridge puts in front of a compiled fragment shader that calls it, over Bevy's own.
///
/// `bcs.slang` reaches each by functions whose names begin with `call`, written into the WGSL
/// Slang writes as they are, and which take and return only numbers and vectors, so nothing depends
/// on the names Slang gives its structs. A shader that calls none of a prelude's functions is given
/// none of it, since importing Bevy's view bindings into a shader that does not use them would
/// only lengthen its compile.
struct Prelude {
    /// What every function the prelude defines is called, to the end of a word.
    call: &'static str,
    /// The functions themselves, over Bevy's imports.
    source: &'static str,
    /// Functions of the same signatures that import nothing, for reading the compiled shader
    /// before the real ones are put in front, which naga cannot follow there.
    stand_in: &'static str,
}

/// Every prelude, in the order they are put in front.
const PRELUDES: [Prelude; 2] = [
    Prelude { call: "bcs_pbr_", source: LIGHTING, stand_in: LIGHTING_STAND_IN },
    Prelude { call: "bcs_decal_", source: DECALS, stand_in: DECALS_STAND_IN },
];

/// A compiled fragment shader with each prelude it calls put in front of it.
pub(super) fn with_bevy(role: Role, wgsl: String) -> String {
    if !matches!(role, Role::Fragment) {
        return wgsl;
    }

    let mut out = String::new();
    for prelude in PRELUDES.iter().filter(|prelude| wgsl.contains(prelude.call)) {
        out.push_str(prelude.source);
        out.push('\n');
    }

    if out.is_empty() {
        wgsl
    } else {
        out.push_str(&wgsl);
        out
    }
}

/// The stand-ins of every prelude a compiled shader calls, for reading it, or nothing where it
/// calls none.
pub fn stand_ins(wgsl: &str) -> Option<String> {
    let called: Vec<&str> = PRELUDES
        .iter()
        .filter(|prelude| wgsl.contains(prelude.call))
        .map(|prelude| prelude.stand_in)
        .collect();

    (!called.is_empty()).then(|| called.concat())
}

// -- Bevy's lighting

/// Functions of the same signatures as those [`LIGHTING`] defines, which return what they were
/// given.
const LIGHTING_STAND_IN: &str = "fn bcs_pbr_light(base_color: vec4<f32>, emissive: vec4<f32>, \
    metallic: f32, roughness: f32, reflectance: vec3<f32>, occlusion: vec3<f32>, frag_coord: vec4<f32>, \
    world_position: vec4<f32>, world_normal: vec3<f32>, normal: vec3<f32>) -> vec4<f32> { return base_color; }\n\
    fn bcs_pbr_finish(color: vec4<f32>, frag_coord: vec4<f32>, world_position: vec4<f32>) -> vec4<f32> { return color; }\n";

/// What `bcs::light` and `bcs::finish` call: Bevy's own lighting of a standard material's surface,
/// over the view's lights, shadows and environment maps, and Bevy's own processing after it, which
/// is fog and, for a camera that does not draw in high dynamic range, tonemapping. Each is imported
/// from Bevy as its own materials import it.
///
/// The surface is a shadow receiver, which a standard material's mesh is unless told otherwise,
/// and takes fog, as one does by default.
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

// -- Bevy's clustered decals

/// Functions of the same signatures as those [`DECALS`] defines, which find no decal.
const DECALS_STAND_IN: &str = "fn bcs_decal_count(frag_coord: vec4<f32>, world_position: vec4<f32>) -> u32 { return 0u; }\n\
    fn bcs_decal_tag(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32) -> u32 { return 0u; }\n\
    fn bcs_decal_has(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, map: u32) -> bool { return false; }\n\
    fn bcs_decal_sample(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, map: u32) -> vec4<f32> { return vec4<f32>(0.0); }\n\
    fn bcs_decal_normal(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, normal: vec3<f32>) -> vec3<f32> { return normal; }\n";

/// What `bcs::decal_count`, `bcs::decal_tag`, `bcs::decal_has`, `bcs::decal_sample` and
/// `bcs::decals` call: Bevy's own walk through the clustered decals over a point of the view, as
/// its `ClusteredDecalIterator` takes it, and the textures Bevy holds for them.
///
/// A decal is found by its place in that walk, so each call walks again from the first, which for
/// the few decals over one point costs less than a walk Slang could hold on to between calls. A
/// map is 0 for the base color, 1 for the normal map, 2 for metallic and roughness and 3 for the
/// light given off, as Bevy's iterator names them. Where the device cannot have clustered decals,
/// Bevy compiles none of their bindings, and every point has none over it.
const DECALS: &str = r#"#import bevy_pbr::{
    clustered_forward,
    decal::clustered,
    mesh_view_bindings,
}

// Moves the iterator on to the decal at `index` over the point, saying whether there is one.
fn bcs_decal_seek(
    frag_coord: vec4<f32>,
    world_position: vec4<f32>,
    index: u32,
    iterator: ptr<function, clustered::ClusteredDecalIterator>,
) -> bool {
#ifdef CLUSTERED_DECALS_ARE_USABLE
    let view_z = clustered::get_view_z(world_position.xyz);
    let cluster_index = clustered_forward::view_fragment_cluster_index(
        frag_coord.xy,
        view_z,
        clustered::view_is_orthographic(),
    );
    var ranges = clustered_forward::unpack_clusterable_object_index_ranges(cluster_index);
    *iterator = clustered::clustered_decal_iterator_new(world_position.xyz, &ranges);

    var at = 0u;
    while (clustered::clustered_decal_iterator_next(iterator)) {
        if (at == index) {
            return true;
        }
        at += 1u;
    }
#endif
    return false;
}

fn bcs_decal_count(frag_coord: vec4<f32>, world_position: vec4<f32>) -> u32 {
    var iterator: clustered::ClusteredDecalIterator;
    var count = 0u;
    while (bcs_decal_seek(frag_coord, world_position, count, &iterator)) {
        count += 1u;
    }
    return count;
}

fn bcs_decal_tag(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32) -> u32 {
    var iterator: clustered::ClusteredDecalIterator;
    if (!bcs_decal_seek(frag_coord, world_position, index, &iterator)) {
        return 0u;
    }
    return iterator.tag;
}

// The index of one of the decal's textures in Bevy's array, or -1 where it has none.
fn bcs_decal_texture(iterator: clustered::ClusteredDecalIterator, map: u32) -> i32 {
    switch (map) {
        case 0u: { return iterator.base_color_texture_index; }
        case 1u: { return iterator.normal_map_texture_index; }
        case 2u: { return iterator.metallic_roughness_texture_index; }
        case 3u: { return iterator.emissive_texture_index; }
        default: { return -1; }
    }
}

fn bcs_decal_has(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, map: u32) -> bool {
    var iterator: clustered::ClusteredDecalIterator;
    return bcs_decal_seek(frag_coord, world_position, index, &iterator)
        && bcs_decal_texture(iterator, map) >= 0;
}

fn bcs_decal_sample(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, map: u32) -> vec4<f32> {
#ifdef CLUSTERED_DECALS_ARE_USABLE
    var iterator: clustered::ClusteredDecalIterator;
    if (bcs_decal_seek(frag_coord, world_position, index, &iterator)) {
        let texture = bcs_decal_texture(iterator, map);
        if (texture >= 0) {
            return textureSampleLevel(
                mesh_view_bindings::clustered_decal_textures[texture],
                mesh_view_bindings::clustered_decal_sampler,
                iterator.uv,
                0.0,
            );
        }
    }
#endif
    return vec4<f32>(0.0);
}

// A normal bent by the decal's normal map, by the Whiteout blend Bevy's `apply_decals` uses, on a
// mesh with tangents as there, and as it was elsewhere.
fn bcs_decal_normal(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, normal: vec3<f32>) -> vec3<f32> {
#ifdef VERTEX_TANGENTS
    if (bcs_decal_has(frag_coord, world_position, index, 1u)) {
        let bent = bcs_decal_sample(frag_coord, world_position, index, 1u).rgb * 2.0 - 1.0;
        return vec3(normal.xy + bent.xy, normal.z * bent.z);
    }
#endif
    return normal;
}
"#;

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

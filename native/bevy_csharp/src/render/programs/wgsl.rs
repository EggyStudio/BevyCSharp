//! The shaders the bridge writes itself, WESL put in front of what slangc compiled where it reaches
//! Bevy's own, or a stage in place of one that has never compiled.

use bevy::shader::Shader;

use super::Role;

/// A shader's text, and the language Bevy reads it in.
///
/// WESL where it imports Bevy's modules, since from Bevy 0.20 only WESL is composed with them and
/// WGSL is handed to the device as it is, and WGSL elsewhere, which then takes no composing.
pub(super) enum Source {
    Wgsl(String),
    Wesl(String),
}

impl Source {
    /// The shader Bevy is given, named `name`.
    pub(super) fn shader(self, name: &str) -> Shader {
        match self {
            Source::Wgsl(text) => Shader::from_wgsl(text, name.to_string()),
            Source::Wesl(text) => Shader::from_wesl(text, module_path(name)),
        }
    }

    /// What the shader says.
    #[cfg(test)]
    pub(super) fn text(&self) -> &str {
        match self {
            Source::Wgsl(text) | Source::Wesl(text) => text,
        }
    }
}

/// `name` as the path of a WESL module of its own, under a folder of the bridge's.
///
/// WESL composes by module path, which Bevy makes of a shader's path without its extension, so
/// every character but a letter, a digit or an underscore becomes an underscore, which keeps the
/// role and the compile's number in a name that ends in a file's extension apart.
fn module_path(name: &str) -> String {
    let flat: String = name
        .chars()
        .map(|c| if c.is_ascii_alphanumeric() { c } else { '_' })
        .collect();
    format!("bcs/{flat}.wesl")
}

// -- What a shader reaches of Bevy's WESL

/// WESL the bridge puts in front of a compiled fragment shader that calls it, over Bevy's own.
///
/// `bcs.slang` reaches each by functions whose names begin with `call`, written into the WGSL
/// Slang writes as they are, and which take and return only numbers and vectors, so nothing depends
/// on the names Slang gives its structs. A shader that calls none of a prelude's functions is given
/// none of it, since importing Bevy's view bindings into a shader that does not use them would
/// only lengthen its compile.
struct Prelude {
    /// What every function the prelude defines is called, to the end of a word.
    call: &'static str,
    /// What it imports of Bevy's, a statement a line, put once at the head of the shader however
    /// many preludes import it, since WESL takes its imports before anything else.
    imports: &'static [&'static str],
    /// The functions themselves.
    body: &'static str,
    /// Functions of the same signatures that import nothing, for reading the compiled shader
    /// before the real ones are put in front, which naga cannot follow there.
    stand_in: &'static str,
}

/// Every prelude, in the order they are put in front.
const PRELUDES: [Prelude; 4] = [
    Prelude { call: "bcs_pbr_", imports: LIGHTING_IMPORTS, body: LIGHTING, stand_in: LIGHTING_STAND_IN },
    Prelude { call: "bcs_decal_", imports: DECALS_IMPORTS, body: DECALS, stand_in: DECALS_STAND_IN },
    Prelude {
        call: "bcs_irradiance_",
        imports: IRRADIANCE_IMPORTS,
        body: IRRADIANCE,
        stand_in: IRRADIANCE_STAND_IN,
    },
    Prelude { call: "bcs_deferred_", imports: DEFERRED_IMPORTS, body: DEFERRED, stand_in: DEFERRED_STAND_IN },
];

/// A compiled shader with each prelude it calls put in front of it, a material's fragment shader
/// for the main pass or its stage for the deferred buffers, or the shader as it was where it calls
/// none.
pub(super) fn with_bevy(role: Role, wgsl: String) -> Source {
    if !matches!(role, Role::Fragment | Role::Deferred) {
        return Source::Wgsl(wgsl);
    }

    let called: Vec<&Prelude> = PRELUDES.iter().filter(|prelude| wgsl.contains(prelude.call)).collect();
    if called.is_empty() {
        return Source::Wgsl(wgsl);
    }

    let mut out = String::new();
    let mut imported: Vec<&str> = Vec::new();
    for import in called.iter().flat_map(|prelude| prelude.imports.iter()) {
        if !imported.contains(import) {
            imported.push(import);
            out.push_str(import);
            out.push('\n');
        }
    }

    // A directive slangc wrote, such as an `enable`, after the imports and before any declaration,
    // which is the one place WESL takes it.
    let (directives, rest) = split_directives(&wgsl);
    out.push_str(&directives);

    for prelude in &called {
        out.push('\n');
        out.push_str(prelude.body);
    }

    out.push('\n');
    out.push_str(rest);
    Source::Wesl(out)
}

/// The directives at the head of compiled WGSL, and what follows them.
fn split_directives(wgsl: &str) -> (String, &str) {
    let mut directives = String::new();
    let mut rest = wgsl;

    loop {
        let line_end = rest.find('\n').map_or(rest.len(), |end| end + 1);
        let line = rest[..line_end].trim();
        let directive = ["enable ", "requires ", "diagnostic("].iter().any(|word| line.starts_with(word));

        if directive {
            directives.push_str(line);
            directives.push('\n');
        } else if !(line.is_empty() || line.starts_with("//")) || line_end == 0 {
            break;
        }

        rest = &rest[line_end..];
    }

    (directives, rest)
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
const LIGHTING: &str = r#"
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

/// What [`LIGHTING`] imports of Bevy's.
const LIGHTING_IMPORTS: &[&str] = &[
    "import bevy_pbr::render::pbr_types;",
    "import bevy_pbr::render::pbr_functions;",
    "import bevy_pbr::render::mesh_types::MESH_FLAGS_SHADOW_RECEIVER_BIT;",
    "import bevy_pbr::render::mesh_view_bindings::view;",
];

// -- Bevy's deferred buffers

/// Functions of the same signatures as those [`DEFERRED`] defines, which write nothing.
const DEFERRED_STAND_IN: &str = "fn bcs_deferred_gbuffer(base_color: vec4<f32>, emissive: vec4<f32>, \
    metallic: f32, roughness: f32, reflectance: vec3<f32>, occlusion: vec3<f32>, frag_coord: vec4<f32>, \
    world_position: vec4<f32>, world_normal: vec3<f32>, normal: vec3<f32>) -> vec4<u32> { return vec4<u32>(0u); }\n\
    fn bcs_deferred_lighting_pass() -> u32 { return 0u; }\n\
    fn bcs_deferred_motion(world_position: vec4<f32>, previous_world_position: vec4<f32>) -> vec2<f32> { return vec2<f32>(0.0); }\n";

/// What `bcs::deferred` calls, a standard material's surface packed into Bevy's deferred buffer as
/// Bevy's own deferred materials pack theirs, the lighting pass that lights it, which is the
/// standard material's, and the motion of the point since the frame before, where the camera
/// draws motion vectors. Each is imported from Bevy as its deferred functions import it, so the
/// buffer holds what Bevy's deferred lighting pass and its screen-space reflections read.
///
/// The surface is a shadow receiver and takes fog, as a standard material does by default, which
/// the deferred lighting pass reads from the buffer's flags.
const DEFERRED: &str = r#"
fn bcs_deferred_gbuffer(
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
) -> vec4<u32> {
    var pbr_input = pbr_types::pbr_input_new();
    pbr_input.material.flags |= pbr_types::STANDARD_MATERIAL_FLAGS_FOG_ENABLED_BIT;
    pbr_input.material.base_color = base_color;
    pbr_input.material.emissive = emissive;
    pbr_input.material.metallic = metallic;
    pbr_input.material.perceptual_roughness = roughness;
    pbr_input.material.reflectance = reflectance;
    pbr_input.diffuse_occlusion = occlusion;
    pbr_input.frag_coord = frag_coord;
    pbr_input.world_position = world_position;
    pbr_input.is_orthographic = view.clip_from_world[3].w == 1.0;
    pbr_input.V = pbr_functions::calculate_view(world_position, pbr_input.is_orthographic);
    pbr_input.flags = MESH_FLAGS_SHADOW_RECEIVER_BIT;
    pbr_input.world_normal = normalize(world_normal);
    pbr_input.N = normalize(normal);
    return pbr_deferred_functions::deferred_gbuffer_from_pbr_input(pbr_input);
}

fn bcs_deferred_lighting_pass() -> u32 {
    return pbr_types::pbr_input_new().material.deferred_lighting_pass_id;
}

fn bcs_deferred_motion(world_position: vec4<f32>, previous_world_position: vec4<f32>) -> vec2<f32> {
    @if(MOTION_VECTOR_PREPASS) {
        return calculate_motion_vector(world_position, previous_world_position);
    }
    return vec2<f32>(0.0);
}
"#;

/// What [`DEFERRED`] imports of Bevy's, motion vectors' function only where the camera draws them.
const DEFERRED_IMPORTS: &[&str] = &[
    "import bevy_pbr::render::pbr_types;",
    "import bevy_pbr::render::pbr_functions;",
    "import bevy_pbr::render::mesh_types::MESH_FLAGS_SHADOW_RECEIVER_BIT;",
    "import bevy_pbr::render::mesh_view_bindings::view;",
    "import bevy_pbr::deferred::functions as pbr_deferred_functions;",
    "@if(MOTION_VECTOR_PREPASS) import bevy_pbr::render::pbr_prepass_functions::calculate_motion_vector;",
];

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
const DECALS: &str = r#"
// Moves the iterator on to the decal at `index` over the point, saying whether there is one.
fn bcs_decal_seek(
    frag_coord: vec4<f32>,
    world_position: vec4<f32>,
    index: u32,
    iterator: ptr<function, clustered::ClusteredDecalIterator>,
) -> bool {
    @if(CLUSTERED_DECALS_ARE_USABLE) {
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
    }
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
    @if(CLUSTERED_DECALS_ARE_USABLE) {
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
    }
    return vec4<f32>(0.0);
}

// A normal bent by the decal's normal map, by the Whiteout blend Bevy's `apply_decals` uses, on a
// mesh with tangents as there, and as it was elsewhere.
fn bcs_decal_normal(frag_coord: vec4<f32>, world_position: vec4<f32>, index: u32, normal: vec3<f32>) -> vec3<f32> {
    @if(VERTEX_TANGENTS) {
        if (bcs_decal_has(frag_coord, world_position, index, 1u)) {
            let bent = bcs_decal_sample(frag_coord, world_position, index, 1u).rgb * 2.0 - 1.0;
            return vec3(normal.xy + bent.xy, normal.z * bent.z);
        }
    }
    return normal;
}
"#;

/// What [`DECALS`] imports of Bevy's.
const DECALS_IMPORTS: &[&str] = &[
    "import bevy_pbr::render::clustered_forward;",
    "import bevy_pbr::decal::clustered;",
    "import bevy_pbr::render::mesh_view_bindings;",
];

// -- Bevy's irradiance volumes

/// A function of the same signature as the one [`IRRADIANCE`] defines, which finds no light.
const IRRADIANCE_STAND_IN: &str = "fn bcs_irradiance_light(frag_coord: vec4<f32>, world_position: vec4<f32>, \
    normal: vec3<f32>) -> vec3<f32> { return vec3<f32>(0.0); }\n";

/// What `bcs::irradiance` calls: Bevy's own `irradiance_volume_light`, the diffuse light the
/// irradiance volumes over a point give a surface facing a way there, at each volume's intensity,
/// found through the view's clusters as Bevy's standard material finds it. Where the device cannot
/// have irradiance volumes, or the view has none, Bevy declares none of their bindings, and every
/// point has none over it.
const IRRADIANCE: &str = r#"
fn bcs_irradiance_light(frag_coord: vec4<f32>, world_position: vec4<f32>, normal: vec3<f32>) -> vec3<f32> {
    @if(IRRADIANCE_VOLUME && IRRADIANCE_VOLUMES_ARE_USABLE) {
        let view_z = dot(vec4<f32>(
            view.view_from_world[0].z,
            view.view_from_world[1].z,
            view.view_from_world[2].z,
            view.view_from_world[3].z,
        ), world_position);
        let cluster_index = clustered_forward::view_fragment_cluster_index(
            frag_coord.xy,
            view_z,
            view.clip_from_view[3].w == 1.0,
        );
        var ranges = clustered_forward::unpack_clusterable_object_index_ranges(cluster_index);
        return irradiance_volume::irradiance_volume_light(world_position.xyz, normalize(normal), &ranges);
    }
    return vec3<f32>(0.0);
}
"#;

/// What [`IRRADIANCE`] imports of Bevy's, the volumes' module only where the view has a volume,
/// since from Bevy 0.20 their bindings are declared only then.
const IRRADIANCE_IMPORTS: &[&str] = &[
    "import bevy_pbr::render::clustered_forward;",
    "import bevy_pbr::render::mesh_view_bindings::view;",
    "@if(IRRADIANCE_VOLUME && IRRADIANCE_VOLUMES_ARE_USABLE) import bevy_pbr::light_probe::irradiance_volume;",
];

// -- Fallbacks

/// A shader standing in for a stage that has never compiled, with the entry point it asked for.
pub(super) fn fallback_source(role: Role, entry: &str) -> Source {
    match role {
        // Magenta, reading nothing but the position, so it is valid after any vertex shader and
        // in a pass as well as in a material.
        Role::Fragment | Role::Pass | Role::DrawFragment => Source::Wgsl(format!(
            "@fragment\nfn {entry}(@builtin(position) position: vec4<f32>) -> @location(0) vec4<f32> {{\n    \
             let checker = (u32(position.x / 8.0) + u32(position.y / 8.0)) % 2u;\n    \
             return select(vec4<f32>(1.0, 0.0, 1.0, 1.0), vec4<f32>(0.1, 0.0, 0.1, 1.0), checker == 1u);\n}}\n"
        )),

        // Bevy's own mesh vertex shader, short of skins and morph targets, the vertex decompressed
        // first as Bevy's is, since a mesh may carry its attributes packed smaller.
        Role::Vertex => Source::Wesl(format!(
            r#"import bevy_pbr::render::mesh_functions;
import bevy_pbr::render::forward_io::{{Vertex, VertexOutput, decompress_vertex}};
import bevy_pbr::render::view_transformations::position_world_to_clip;

@vertex
fn {entry}(vertex_in: Vertex) -> VertexOutput {{
    let vertex = decompress_vertex(vertex_in, vertex_in.instance_index);
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(vertex.position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
    @if(VERTEX_NORMALS) {{
        out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
    }}
    @if(VERTEX_UVS_A) {{
        out.uv = vertex.uv;
    }}
    @if(VERTEX_UVS_B) {{
        out.uv_b = vertex.uv_b;
    }}
    @if(VERTEX_TANGENTS) {{
        out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
    }}
    @if(VERTEX_COLORS) {{
        out.color = vertex.color;
    }}
    @if(VERTEX_OUTPUT_INSTANCE_INDEX) {{
        out.instance_index = vertex.instance_index;
    }}
    return out;
}}
"#
        )),

        Role::PrepassVertex => Source::Wesl(format!(
            r#"import bevy_pbr::render::mesh_functions;
import bevy_pbr::prepass::io::{{Vertex, VertexOutput, decompress_vertex}};
import bevy_pbr::render::view_transformations::position_world_to_clip;

@vertex
fn {entry}(vertex_in: Vertex) -> VertexOutput {{
    let vertex = decompress_vertex(vertex_in, vertex_in.instance_index);
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(vertex.position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
    @if(UNCLIPPED_DEPTH_ORTHO_EMULATION) {{
        out.unclipped_depth = out.position.z;
        out.position.z = min(out.position.z, 1.0);
    }}
    @if(VERTEX_UVS_A) {{
        out.uv = vertex.uv;
    }}
    @if(VERTEX_UVS_B) {{
        out.uv_b = vertex.uv_b;
    }}
    @if(NORMAL_PREPASS_OR_DEFERRED_PREPASS && VERTEX_NORMALS) {{
        out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
    }}
    @if(NORMAL_PREPASS_OR_DEFERRED_PREPASS && VERTEX_TANGENTS) {{
        out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
    }}
    @if(MOTION_VECTOR_PREPASS) {{
        let previous_world_from_local = mesh_functions::get_previous_world_from_local(vertex.instance_index);
        out.previous_world_position = mesh_functions::mesh_position_local_to_world(previous_world_from_local, vec4<f32>(vertex.position, 1.0));
    }}
    @if(VERTEX_OUTPUT_INSTANCE_INDEX) {{
        out.instance_index = vertex.instance_index;
    }}
    return out;
}}
"#
        )),

        // Every vertex at one point, which draws nothing, since what a draw's vertex shader reads
        // to place its geometry is not known here.
        Role::DrawVertex => Source::Wgsl(format!(
            "@vertex\nfn {entry}(@builtin(vertex_index) index: u32) -> @builtin(position) vec4<f32> {{\n    \
             return vec4<f32>(0.0, 0.0, 0.0, 1.0);\n}}\n"
        )),

        // Does nothing, which is the only thing a compute shader can safely do without knowing
        // what the buffers it was handed hold.
        Role::Compute => Source::Wgsl(format!("@compute @workgroup_size(1)\nfn {entry}() {{\n}}\n")),

        // Magenta in the deferred buffers, through Bevy's own deferred output, so a deferred stage
        // that has never compiled is lit as a magenta surface where the rest of the scene is.
        Role::Deferred => Source::Wesl(format!(
            r#"import bevy_pbr::prepass::io::VertexOutput;
@if(PREPASS_FRAGMENT) import bevy_pbr::prepass::io::FragmentOutput;
import bevy_pbr::render::pbr_types;
import bevy_pbr::deferred::functions as pbr_deferred_functions;

@if(PREPASS_FRAGMENT)
@fragment
fn {entry}(in: VertexOutput) -> FragmentOutput {{
    var pbr_input = pbr_types::pbr_input_new();
    pbr_input.material.base_color = vec4<f32>(1.0, 0.0, 1.0, 1.0);
    pbr_input.frag_coord = in.position;
    pbr_input.world_position = in.world_position;
    @if(NORMAL_PREPASS_OR_DEFERRED_PREPASS) {{
        pbr_input.world_normal = in.world_normal;
        pbr_input.N = in.world_normal;
    }}
    return pbr_deferred_functions::deferred_output(in, pbr_input);
}}

@if(!PREPASS_FRAGMENT)
@fragment
fn {entry}(@builtin(position) position: vec4<f32>) {{
}}
"#
        )),

        // What Bevy's own prepass writes, which is a normal and a motion vector where the camera
        // asked for them, and nothing where it did not.
        Role::PrepassFragment => Source::Wesl(format!(
            r#"import bevy_pbr::prepass::io::VertexOutput;
@if(PREPASS_FRAGMENT) import bevy_pbr::prepass::io::FragmentOutput;
@if(MOTION_VECTOR_PREPASS) import bevy_pbr::render::pbr_prepass_functions::calculate_motion_vector;

@if(PREPASS_FRAGMENT)
@fragment
fn {entry}(in: VertexOutput) -> FragmentOutput {{
    var out: FragmentOutput;
    @if(NORMAL_PREPASS) {{
        out.normal = vec4(in.world_normal * 0.5 + vec3(0.5), 1.0);
    }}
    @if(UNCLIPPED_DEPTH_ORTHO_EMULATION) {{
        out.frag_depth = in.unclipped_depth;
    }}
    @if(MOTION_VECTOR_PREPASS) {{
        out.motion_vector = calculate_motion_vector(in.world_position, in.previous_world_position);
    }}
    return out;
}}

@if(!PREPASS_FRAGMENT)
@fragment
fn {entry}(@builtin(position) position: vec4<f32>) {{
}}
"#
        )),
    }
}

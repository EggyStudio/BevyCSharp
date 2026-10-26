//! The material a far tree is drawn with as an octahedral impostor, one quad turned to whatever
//! looks at it and painted from the three pictures of the tree taken nearest the way it is seen.
//!
//! A tree is pictured once from a grid of ways over the upper half of the sphere, each way a frame
//! of an atlas, the ways laid on a square folded over the half sphere octahedrally, so the frames
//! cover it evenly and the three nearest any way are the corners of the triangle of the grid the
//! way falls in. Its vertex stage stands the mesh's one quad at the middle of the tree's sphere,
//! square to the eye in a perspective view and to the view's way in an orthographic one, so a
//! light's shadow sees the tree from the light, and finds the three frames once a quad. Its
//! fragment stage finds where its ray from the eye meets each frame's plane, through the tree's
//! middle square to the way the frame was pictured from, reads the frame there, and blends the
//! three by how near each one's way is, weighted by their opacity, so a tree turning past the eye
//! changes its picture by degrees.
//!
//! The color atlas holds the tree as it is pictured unlit. The two normal atlases hold, in the
//! tree's own space, the normals the leaf material lights a leaf by. The first holds the normal its
//! diffuse light is lit by, its own bent half way out of the crown, and in its fourth channel
//! whether a leaf or a stem is pictured there, and the second its own normal turned to face the eye
//! the frame was pictured from, as Bevy turns a double-sided card's. A stem's own normal stands in
//! both. The light let through a crown's leaves and their specular come from leaves turned every
//! way. A leaf turned from the sun lets some of it through where a normal facing the eye lets none
//! through for a sun on the eye's side, and a leaf seen at a slant takes the sky's sheen where a
//! normal facing the eye takes little.
//!
//! The quad's mesh holds a corner of the quad in each vertex's texture coordinates and places that
//! span the tree's sphere, so Bevy's bounds round the mesh, which cull it from a view and a shadow,
//! hold the tree from every way.
//!
//! In the wind the quad moves as the tree's trunk bends at the height of its middle (see
//! [`super::sway`]), its frames read where they would be without it, so the whole tree leans as
//! the nearer levels' trunks do and its limbs and leaves stay still in it, too small to see.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use crate::state::with_world;

/// The shape of a tree's impostor, which its frames picture.
#[cfg(feature = "render")]
#[derive(bevy::render::render_resource::ShaderType, bevy::reflect::Reflect, Debug, Clone, Copy, Default)]
pub struct ImpostorShape {
    /// The middle of the tree's sphere in the tree's own space.
    pub middle: bevy::math::Vec3,
    /// Half a frame's side in the tree's meters, the sphere's radius and the frame's margin.
    pub extent: f32,
    /// The frames a side of the atlas.
    pub across: u32,
}

/// What the impostor material adds to the standard material, the shape, the three atlases and the
/// wind it leans in.
#[cfg(feature = "render")]
#[derive(bevy::asset::Asset, bevy::render::render_resource::AsBindGroup, bevy::reflect::Reflect, Debug, Clone, Default)]
pub struct ImpostorFrames {
    #[uniform(100)]
    pub shape: ImpostorShape,
    #[texture(101)]
    #[sampler(102)]
    pub colors: Option<bevy::asset::Handle<bevy::image::Image>>,
    #[texture(103)]
    #[sampler(104)]
    pub normals: Option<bevy::asset::Handle<bevy::image::Image>>,
    #[texture(105)]
    #[sampler(106)]
    pub faces: Option<bevy::asset::Handle<bevy::image::Image>>,
    #[uniform(107)]
    pub wind: super::sway::TreeWind,
}

#[cfg(feature = "render")]
impl bevy::pbr::MaterialExtension for ImpostorFrames {
    fn vertex_shader() -> bevy::shader::ShaderRef {
        IMPOSTOR_MAIN.into()
    }

    fn fragment_shader() -> bevy::shader::ShaderRef {
        IMPOSTOR_MAIN.into()
    }

    fn prepass_vertex_shader() -> bevy::shader::ShaderRef {
        IMPOSTOR_PREPASS.into()
    }

    fn prepass_fragment_shader() -> bevy::shader::ShaderRef {
        IMPOSTOR_PREPASS.into()
    }
}

/// The standard material's values drawn as a tree's impostor.
#[cfg(feature = "render")]
pub type ImpostorMaterial = bevy::pbr::ExtendedMaterial<bevy::pbr::StandardMaterial, ImpostorFrames>;

/// The shader the impostor is drawn with, its vertex and fragment stages.
#[cfg(feature = "render")]
const IMPOSTOR_MAIN: bevy::asset::Handle<bevy::shader::Shader> =
    bevy::asset::uuid_handle!("8d2c41f0-6b7e-4a53-9e18-3f5a0c7d2b64");

/// The shader its depth and its shadows are drawn with.
#[cfg(feature = "render")]
const IMPOSTOR_PREPASS: bevy::asset::Handle<bevy::shader::Shader> =
    bevy::asset::uuid_handle!("2f9b7c35-81d4-4e06-b5a2-6c0e94d1f378");

/// What both shaders share, the bindings, the frames' ways, the quad stood to the view and the
/// frames read where the eye's ray meets them. A frame's way is unfolded from its column and row,
/// its right level and its up square to both, as the library pictured it.
#[cfg(feature = "render")]
const IMPOSTOR_COMMON: &str = r#"
struct ImpostorShape {
    middle: vec3<f32>,
    extent: f32,
    across: u32,
}

@group(constants::MATERIAL_BIND_GROUP) @binding(100) var<uniform> shape: ImpostorShape;
@group(constants::MATERIAL_BIND_GROUP) @binding(101) var colors: texture_2d<f32>;
@group(constants::MATERIAL_BIND_GROUP) @binding(102) var colors_sampler: sampler;
@group(constants::MATERIAL_BIND_GROUP) @binding(103) var normals: texture_2d<f32>;
@group(constants::MATERIAL_BIND_GROUP) @binding(104) var normals_sampler: sampler;
@group(constants::MATERIAL_BIND_GROUP) @binding(105) var faces: texture_2d<f32>;
@group(constants::MATERIAL_BIND_GROUP) @binding(106) var faces_sampler: sampler;

struct Nearest {
    frames: vec3<u32>,
    weights: vec3<f32>,
}

struct Billboard {
    world_position: vec4<f32>,
    local_position: vec3<f32>,
    local_ray: vec3<f32>,
    nearest: Nearest,
}

fn frame_index(cell: vec2<u32>) -> u32 {
    return cell.x + cell.y * shape.across;
}

fn frame_cell(frame: u32) -> vec2<u32> {
    return vec2<u32>(frame % shape.across, frame / shape.across);
}

fn frame_way(cell: vec2<u32>) -> vec3<f32> {
    let folded = vec2<f32>(cell) * (2.0 / f32(shape.across - 1u)) - 1.0;
    let x = (folded.x + folded.y) * 0.5;
    let z = (folded.x - folded.y) * 0.5;
    return normalize(vec3<f32>(x, max(1.0 - abs(x) - abs(z), 0.0), z));
}

fn frame_right(way: vec3<f32>) -> vec3<f32> {
    let right = cross(vec3<f32>(0.0, 1.0, 0.0), way);
    if dot(right, right) <= 1e-8 {
        return vec3<f32>(1.0, 0.0, 0.0);
    }
    return normalize(right);
}

// The three frames pictured nearest a way the tree is seen from, a way from below seen as from
// level, and how much of each the picture takes.
fn nearest_frames(toward: vec3<f32>) -> Nearest {
    var way = vec3<f32>(toward.x, max(toward.y, 0.0), toward.z);
    var sum = abs(way.x) + abs(way.y) + abs(way.z);
    if sum < 1e-6 {
        way = vec3<f32>(1.0, 0.0, 0.0);
        sum = 1.0;
    }
    let p = way / sum;
    let last = f32(shape.across - 1u);
    let grid = (vec2<f32>(p.x + p.z, p.x - p.z) * 0.5 + 0.5) * last;
    let cell = clamp(floor(grid), vec2<f32>(0.0), vec2<f32>(last - 1.0));
    let f = grid - cell;
    let c = vec2<u32>(cell);
    var out: Nearest;
    if f.x + f.y <= 1.0 {
        out.frames = vec3<u32>(frame_index(c), frame_index(c + vec2<u32>(1u, 0u)), frame_index(c + vec2<u32>(0u, 1u)));
        out.weights = vec3<f32>(1.0 - f.x - f.y, f.x, f.y);
    } else {
        out.frames = vec3<u32>(frame_index(c + vec2<u32>(1u, 1u)), frame_index(c + vec2<u32>(1u, 0u)), frame_index(c + vec2<u32>(0u, 1u)));
        out.weights = vec3<f32>(f.x + f.y - 1.0, 1.0 - f.y, 1.0 - f.x);
    }
    return out;
}

// The quad's corner stood square to the view at the middle of the tree's sphere, with its place
// and the eye's ray in the tree's space and the frames it is painted from.
fn billboard(instance_index: u32, corner_uv: vec2<f32>) -> Billboard {
    let world_from_local = mesh_functions::get_world_from_local(instance_index);
    let local_from_world = mesh_functions::get_local_from_world(instance_index);
    let orthographic = view.clip_from_view[3].w == 1.0;

    let shift = tree_bent(shape.middle.y, tree_wind.now.xyz, globals.time, local_from_world, tree_phase(world_from_local));

    var eye = vec3<f32>(0.0);
    var toward: vec3<f32>;
    if orthographic {
        toward = normalize((local_from_world * vec4<f32>(view.world_from_view[2].xyz, 0.0)).xyz);
    } else {
        eye = (local_from_world * vec4<f32>(view.world_position, 1.0)).xyz;
        toward = normalize(eye - shape.middle);
    }

    var right = cross(vec3<f32>(0.0, 1.0, 0.0), toward);
    if dot(right, right) <= 1e-8 {
        right = (local_from_world * vec4<f32>(view.world_from_view[0].xyz, 0.0)).xyz;
        right = right - toward * dot(right, toward);
    }
    right = normalize(right);
    let up = cross(toward, right);

    let corner = vec2<f32>(corner_uv.x * 2.0 - 1.0, 1.0 - corner_uv.y * 2.0);
    let local = shape.middle + (right * corner.x + up * corner.y) * shape.extent;

    var out: Billboard;
    out.local_position = local;
    out.local_ray = select(local + shift - eye, -toward, orthographic);
    out.world_position = world_from_local * vec4<f32>(local + shift, 1.0);
    out.nearest = nearest_frames(toward);
    return out;
}

// Where the eye's ray through a point of the quad meets a frame's plane, as a place in the atlas,
// held half a texel inside the frame, and one where it meets the plane inside the frame, else 0.
fn frame_place(frame: u32, local_position: vec3<f32>, local_ray: vec3<f32>) -> vec3<f32> {
    let cell = frame_cell(frame);
    let way = frame_way(cell);
    let right = frame_right(way);
    let up = cross(way, right);
    let along = dot(local_ray, way);
    let t = select(0.0, dot(shape.middle - local_position, way) / along, abs(along) > 1e-6);
    let hit = local_position + local_ray * t - shape.middle;
    let x = dot(hit, right) / (2.0 * shape.extent);
    let y = dot(hit, up) / (2.0 * shape.extent);
    let inside = select(0.0, 1.0, abs(x) < 0.5 && abs(y) < 0.5);
    let half_texel = 0.5 * f32(shape.across) / f32(textureDimensions(colors).x);
    let within = clamp(vec2<f32>(0.5 + x, 0.5 - y), vec2<f32>(half_texel), vec2<f32>(1.0 - half_texel));
    return vec3<f32>((vec2<f32>(cell) + within) / f32(shape.across), inside);
}

// The three frames' opacity blended.
fn seen_alpha(nearest: Nearest, local_position: vec3<f32>, local_ray: vec3<f32>) -> f32 {
    var alpha = 0.0;
    for (var k = 0u; k < 3u; k++) {
        let place = frame_place(nearest.frames[k], local_position, local_ray);
        alpha += textureSample(colors, colors_sampler, place.xy).a * place.z * nearest.weights[k];
    }
    return alpha;
}

struct Seen {
    color: vec3<f32>,
    alpha: f32,
    normal: vec3<f32>,
    face: vec3<f32>,
    leaf: f32,
}

fn unit_or_up(v: vec3<f32>) -> vec3<f32> {
    return select(vec3<f32>(0.0, 1.0, 0.0), normalize(v), dot(v, v) > 1e-8);
}

// The three frames blended, the color, the normals and the leaves' share weighted by each frame's
// opacity, so a frame that pictures nothing at a point takes nothing from those that do.
fn seen_frames(nearest: Nearest, local_position: vec3<f32>, local_ray: vec3<f32>) -> Seen {
    var color = vec4<f32>(0.0);
    var normal = vec4<f32>(0.0);
    var face = vec3<f32>(0.0);
    for (var k = 0u; k < 3u; k++) {
        let place = frame_place(nearest.frames[k], local_position, local_ray);
        let c = textureSample(colors, colors_sampler, place.xy);
        let n = textureSample(normals, normals_sampler, place.xy);
        let f = textureSample(faces, faces_sampler, place.xy);
        let a = c.a * place.z * nearest.weights[k];
        color += vec4<f32>(c.rgb * a, a);
        normal += vec4<f32>((n.rgb * 2.0 - 1.0) * a, n.a * a);
        face += (f.rgb * 2.0 - 1.0) * a;
    }
    var out: Seen;
    let total = max(color.a, 1e-4);
    out.color = color.rgb / total;
    out.alpha = color.a;
    out.normal = unit_or_up(normal.xyz);
    out.face = unit_or_up(face);
    out.leaf = normal.a / total;
    return out;
}
"#;

/// The main pass's imports.
#[cfg(feature = "render")]
const IMPOSTOR_MAIN_IMPORTS: &str = r#"import bevy_pbr::render::{
    forward_io::{Vertex, FragmentOutput, decompress_vertex},
    mesh_functions,
    mesh_bindings::mesh,
    mesh_view_bindings::{view, globals},
    view_transformations::position_world_to_clip,
    pbr_bindings,
    pbr_types::pbr_input_new,
    pbr_functions::{apply_pbr_lighting, calculate_view, main_pass_post_lighting_processing},
};

@if(VISIBILITY_RANGE_DITHER)
import bevy_pbr::render::pbr_functions::visibility_range_dither;
"#;

/// The main pass, the quad stood and lit twice as the leaf material lights a leaf, once by the
/// normal of its diffuse light with the diffuse share of its color, no specular and nothing let
/// through, and once by its own normal, turned to the eye, with the share let through and the
/// specular.
#[cfg(feature = "render")]
const IMPOSTOR_MAIN_STAGES: &str = r#"
struct ImpostorOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) world_position: vec4<f32>,
    @location(1) local_position: vec3<f32>,
    @location(2) local_ray: vec3<f32>,
    @location(3) @interpolate(flat) frames: vec3<u32>,
    @location(4) @interpolate(flat) weights: vec3<f32>,
    @location(5) @interpolate(flat) instance_index: u32,
    @if(VISIBILITY_RANGE_DITHER)
    @location(6) @interpolate(flat) visibility_range_dither: i32,
}

@vertex
fn vertex(vertex_in: Vertex) -> ImpostorOutput {
    let vertex = decompress_vertex(vertex_in, vertex_in.instance_index);
    let board = billboard(vertex_in.instance_index, vertex.uv);
    var out: ImpostorOutput;
    out.world_position = board.world_position;
    out.position = position_world_to_clip(board.world_position.xyz);
    out.local_position = board.local_position;
    out.local_ray = board.local_ray;
    out.frames = board.nearest.frames;
    out.weights = board.nearest.weights;
    out.instance_index = vertex_in.instance_index;
@if(VISIBILITY_RANGE_DITHER)
    out.visibility_range_dither = mesh_functions::get_visibility_range_dither_level(
        vertex_in.instance_index, mesh_functions::get_world_from_local(vertex_in.instance_index)[3]);
    return out;
}

@fragment
fn fragment(in: ImpostorOutput) -> FragmentOutput {
@if(VISIBILITY_RANGE_DITHER)
    visibility_range_dither(in.position, in.visibility_range_dither);

    var nearest: Nearest;
    nearest.frames = in.frames;
    nearest.weights = in.weights;
    let seen = seen_frames(nearest, in.local_position, in.local_ray);
    let material = pbr_bindings::material;
    if seen.alpha < material.alpha_cutoff {
        discard;
    }

    var pbr_input = pbr_input_new();
    pbr_input.material = material;
    pbr_input.flags = mesh[in.instance_index].flags;
    pbr_input.is_orthographic = view.clip_from_view[3].w == 1.0;
    pbr_input.V = calculate_view(in.world_position, pbr_input.is_orthographic);
    pbr_input.frag_coord = in.position;
    pbr_input.world_position = in.world_position;
    let normal = normalize(mesh_functions::mesh_normal_local_to_world(seen.normal, in.instance_index));
    pbr_input.world_normal = normal;
    pbr_input.N = normal;

    let base = material.base_color.rgb * seen.color;
    let through = material.diffuse_transmission * seen.leaf;

    var diffuse = pbr_input;
    diffuse.material.base_color = vec4<f32>(base * (1.0 - through), 1.0);
    diffuse.material.reflectance = vec3<f32>(0.0);
    diffuse.material.diffuse_transmission = 0.0;

    var rest = pbr_input;
    let face = normalize(mesh_functions::mesh_normal_local_to_world(seen.face, in.instance_index));
    rest.N = select(-face, face, dot(face, pbr_input.V) >= 0.0);
    rest.material.base_color = vec4<f32>(base * through, 1.0);
    rest.material.diffuse_transmission = 1.0;
    rest.material.emissive = vec4<f32>(0.0);

    var out: FragmentOutput;
    out.color = vec4<f32>(apply_pbr_lighting(diffuse).rgb + apply_pbr_lighting(rest).rgb, 1.0);
    out.color = main_pass_post_lighting_processing(pbr_input, out.color);
    return out;
}
"#;

/// The prepass's imports.
#[cfg(feature = "render")]
const IMPOSTOR_PREPASS_IMPORTS: &str = r#"import bevy_pbr::{
    prepass::io::{Vertex, decompress_vertex},
    render::{
        mesh_functions,
        mesh_view_bindings::view,
        view_transformations::position_world_to_clip,
        pbr_bindings,
    },
};

@if(PREPASS_FRAGMENT)
import bevy_pbr::prepass::io::FragmentOutput;
@if(VISIBILITY_RANGE_DITHER)
import bevy_pbr::render::pbr_functions::visibility_range_dither;
@if(MOTION_VECTOR_PREPASS)
import bevy_pbr::render::pbr_prepass_functions::calculate_motion_vector;
"#;

/// The prepass, the quad stood as the main pass stands it and cut where the frames picture
/// nothing, its depth for the scene's cameras and the shadows' maps, with the normal of its diffuse
/// light and a tree's stillness where the camera asks for them. An orthographic shadow's depth is
/// written from the fragment where the device cannot leave it unclipped, as Bevy's prepass does.
#[cfg(feature = "render")]
const IMPOSTOR_PREPASS_STAGES: &str = r#"
struct ImpostorPrepassOutput {
    @builtin(position) position: vec4<f32>,
    @location(0) world_position: vec4<f32>,
    @location(1) local_position: vec3<f32>,
    @location(2) local_ray: vec3<f32>,
    @location(3) @interpolate(flat) frames: vec3<u32>,
    @location(4) @interpolate(flat) weights: vec3<f32>,
    @location(5) @interpolate(flat) instance_index: u32,
    @if(UNCLIPPED_DEPTH_ORTHO_EMULATION)
    @location(6) unclipped_depth: f32,
    @if(VISIBILITY_RANGE_DITHER)
    @location(7) @interpolate(flat) visibility_range_dither: i32,
}

@vertex
fn vertex(vertex_in: Vertex) -> ImpostorPrepassOutput {
    let vertex = decompress_vertex(vertex_in, vertex_in.instance_index);
    let board = billboard(vertex_in.instance_index, vertex.uv);
    var out: ImpostorPrepassOutput;
    out.world_position = board.world_position;
    out.position = position_world_to_clip(board.world_position.xyz);
@if(UNCLIPPED_DEPTH_ORTHO_EMULATION) {
    out.unclipped_depth = out.position.z;
    out.position.z = min(out.position.z, 1.0);
}
    out.local_position = board.local_position;
    out.local_ray = board.local_ray;
    out.frames = board.nearest.frames;
    out.weights = board.nearest.weights;
    out.instance_index = vertex_in.instance_index;
@if(VISIBILITY_RANGE_DITHER)
    out.visibility_range_dither = mesh_functions::get_visibility_range_dither_level(
        vertex_in.instance_index, mesh_functions::get_world_from_local(vertex_in.instance_index)[3]);
    return out;
}

@if(PREPASS_FRAGMENT)
@fragment
fn fragment(in: ImpostorPrepassOutput) -> FragmentOutput {
@if(VISIBILITY_RANGE_DITHER)
    visibility_range_dither(in.position, in.visibility_range_dither);

    var nearest: Nearest;
    nearest.frames = in.frames;
    nearest.weights = in.weights;
    let seen = seen_frames(nearest, in.local_position, in.local_ray);
    if seen.alpha < pbr_bindings::material.alpha_cutoff {
        discard;
    }

    var out: FragmentOutput;
@if(NORMAL_PREPASS)
    out.normal = vec4<f32>(normalize(mesh_functions::mesh_normal_local_to_world(seen.normal, in.instance_index)) * 0.5 + vec3<f32>(0.5), 1.0);
@if(UNCLIPPED_DEPTH_ORTHO_EMULATION)
    out.frag_depth = in.unclipped_depth;
@if(MOTION_VECTOR_PREPASS)
    out.motion_vector = calculate_motion_vector(in.world_position, in.world_position);
    return out;
}
@else
@fragment
fn fragment(in: ImpostorPrepassOutput) {
@if(VISIBILITY_RANGE_DITHER)
    visibility_range_dither(in.position, in.visibility_range_dither);

    var nearest: Nearest;
    nearest.frames = in.frames;
    nearest.weights = in.weights;
    if seen_alpha(nearest, in.local_position, in.local_ray) < pbr_bindings::material.alpha_cutoff {
        discard;
    }
}
"#;

/// Adds the impostor material's plugin and its two shaders, each its imports, the shared part and
/// its stages, since an import must come before anything a module declares.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    app.add_plugins(bevy::pbr::MaterialPlugin::<ImpostorMaterial>::default());
    if let Some(mut shaders) = app.world_mut().get_resource_mut::<bevy::asset::Assets<bevy::shader::Shader>>() {
        // UUID handles, whose insertion cannot fail.
        let _ = shaders.insert(
            IMPOSTOR_MAIN.id(),
            bevy::shader::Shader::from_wesl(
                format!("{IMPOSTOR_MAIN_IMPORTS}{}{IMPOSTOR_COMMON}{IMPOSTOR_MAIN_STAGES}", super::sway::sway_at(107)),
                "bcs/impostor.wesl",
            ),
        );
        let _ = shaders.insert(
            IMPOSTOR_PREPASS.id(),
            bevy::shader::Shader::from_wesl(
                format!("{IMPOSTOR_PREPASS_IMPORTS}{}{}{IMPOSTOR_COMMON}{IMPOSTOR_PREPASS_STAGES}", super::sway::PREPASS_GLOBALS, super::sway::sway_at(107)),
                "bcs/impostor_prepass.wesl",
            ),
        );
    }
}

/// Puts an impostor material on an entity when `material` is one, answering whether it was.
#[cfg(feature = "render")]
pub fn attach(entity: &mut bevy::ecs::world::EntityWorldMut, material: &bevy::asset::UntypedHandle) -> bool {
    match material.clone().try_typed::<ImpostorMaterial>() {
        Ok(handle) => {
            entity.insert(bevy::pbr::MeshMaterial3d(handle));
            true
        }
        Err(_) => false,
    }
}

/// Makes an impostor material from the settings a standard material is made with, the three
/// atlases its frames are read from, the shape they picture and the trunk's stiffness in the wind,
/// as [`super::sway::TreeWind`] holds it, and returns its key.
///
/// The standard material's values light it, its base color scaling the pictured color, its
/// roughness, reflectance and diffuse transmission as the leaf material takes them, and its alpha
/// mode's cutoff cutting the blended frames' opacity. Its textures are not read. It is drawn
/// forward, as the leaf material is, being lit twice.
///
/// # Safety
///
/// `config` must be null or point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create_impostor(
    config: *const BcsMaterialConfig,
    colors: i32,
    normals: i32,
    faces: i32,
    middle_x: f32,
    middle_y: f32,
    middle_z: f32,
    extent: f32,
    across: u32,
    trunk: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (config, colors, normals, faces, middle_x, middle_y, middle_z, extent, across, trunk);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;

            if config.is_null() {
                return status::NULL_ARG;
            }
            if across < 2 || !(extent > 0.0) || !extent.is_finite() {
                return status::INVALID_STATE;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let mut base = match super::assets::standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };
                base.opaque_render_method = bevy::material::OpaqueRendererMethod::Forward;
                let colors = match super::image_handle(world, colors) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NO_COMPONENT,
                    Err(status) => return status,
                };
                let normals = match super::image_handle(world, normals) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NO_COMPONENT,
                    Err(status) => return status,
                };
                let faces = match super::image_handle(world, faces) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NO_COMPONENT,
                    Err(status) => return status,
                };
                let extension = ImpostorFrames {
                    shape: ImpostorShape {
                        middle: bevy::math::Vec3::new(middle_x, middle_y, middle_z),
                        extent,
                        across,
                    },
                    colors: Some(colors),
                    normals: Some(normals),
                    faces: Some(faces),
                    wind: super::sway::TreeWind::new(world, trunk, 0.0, 0.0, 0.0),
                };

                let Some(mut materials) = world.get_resource_mut::<Assets<ImpostorMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(ImpostorMaterial { base, extension }).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

//! A tree swaying in the wind, its trunk bending, each limb swinging about where it leaves its
//! parent and each leaf fluttering about its stalk, moved in the vertex stages of the tree's
//! materials, so its shadow sways with it.
//!
//! Each vertex of a tree's stems and leaves carries two streams of the game's own (see
//! [`super::streams`]), which the trees write as they build their meshes. The first is the pivot
//! of the limb the vertex hangs from, where the limb leaves its parent, and the limb's phase, or
//! the vertex's own place where it is on the trunk, so it does not swing. The second is the pivot
//! a leaf flutters about, its stalk, and the leaf's phase, or the vertex's own place for a stem.
//! The limbs are the tree's thickest stems, the same ones its far level of cards gathers its
//! foliage by, so a card swings with the limb whose leaves it pictures as they did.
//!
//! The trunk bends downwind by the square of a point's height, a limb swings by the square of a
//! point's reach from its pivot, and a leaf flutters across its reach by the square of it, each by
//! a stiffness the material is made with, as SpeedTree's and Pivot Painter's wind move a tree by
//! the pivots its vertices carry. The push grows with the square of the wind's speed, as drag does,
//! and gives way toward a gale. Each tree takes a phase from where it stands, so the trees of one
//! variant sway out of step. The wind is the world's, written into every tree material on the
//! frames it changes, with the frame's before it, which the motion vectors read.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use crate::state::with_world;

/// The names the two sway streams are registered under, and their locations, which the vertex
/// stages read them at.
pub const LIMB_STREAM: &str = "bcs_tree_limb";
pub const OWN_STREAM: &str = "bcs_tree_own";

/// What a tree material is moved by, the wind this frame and the one before, in the world's
/// meters a second, and the tree's stiffness.
#[cfg(feature = "render")]
#[derive(bevy::render::render_resource::ShaderType, bevy::reflect::Reflect, Debug, Clone, Copy, Default, PartialEq)]
pub struct TreeWind {
    /// The wind this frame, its fourth component unused.
    pub now: bevy::math::Vec4,
    /// The wind the frame before.
    pub before: bevy::math::Vec4,
    /// How far the trunk bends a meter of height squared, a limb swings a meter of reach squared and
    /// a leaf flutters a meter of reach squared, at the full push, and the flutter's angular speed.
    pub stiffness: bevy::math::Vec4,
}

#[cfg(feature = "render")]
impl TreeWind {
    /// A tree's stiffness, with the wind as it blows.
    pub fn new(world: &bevy::ecs::world::World, trunk: f32, limb: f32, flutter: f32, frequency: f32) -> Self {
        let blowing = world.get_resource::<Blowing>().map_or(bevy::math::Vec3::ZERO, |blowing| blowing.velocity);
        Self {
            now: blowing.extend(0.0),
            before: blowing.extend(0.0),
            stiffness: bevy::math::Vec4::new(trunk, limb, flutter, frequency),
        }
    }
}

/// The wind the trees sway in, as the game last gave it, and what was written into their materials.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct Blowing {
    pub velocity: bevy::math::Vec3,
    written: Option<(bevy::math::Vec3, bevy::math::Vec3)>,
}

/// The WESL both a tree's vertex stages share, the wind's binding, which `@binding(WIND)` stands
/// for until the material's own number replaces it, a tree's phase from where it stands and the
/// sway itself. A point's limb swings along the wind and a little up and down, and a leaf flutters
/// across its reach, square to the wind and up.
#[cfg(feature = "render")]
pub const SWAY_COMMON: &str = r#"
struct TreeWind {
    now: vec4<f32>,
    before: vec4<f32>,
    stiffness: vec4<f32>,
}

@group(constants::MATERIAL_BIND_GROUP) @binding(WIND) var<uniform> tree_wind: TreeWind;

fn tree_phase(world_from_local: mat4x4<f32>) -> f32 {
    let place = world_from_local[3].xz;
    return fract(sin(dot(place, vec2<f32>(12.9898, 78.233))) * 43758.5453) * 6.2831853;
}

fn tree_push(velocity: vec3<f32>) -> f32 {
    let square = dot(velocity, velocity);
    return square / (square + 100.0);
}

fn tree_way(velocity: vec3<f32>, local_from_world: mat4x4<f32>) -> vec3<f32> {
    let way = (local_from_world * vec4<f32>(velocity, 0.0)).xyz;
    return way / max(length(way), 1e-6);
}

fn tree_bent(height: f32, velocity: vec3<f32>, time: f32, local_from_world: mat4x4<f32>, phase: f32) -> vec3<f32> {
    let up = max(height, 0.0);
    let bend = tree_wind.stiffness.x * tree_push(velocity) * up * up * (0.8 + 0.2 * sin(time * 0.9 + phase));
    return tree_way(velocity, local_from_world) * bend;
}

fn tree_swayed(position: vec3<f32>, limb: vec4<f32>, own: vec4<f32>, velocity: vec3<f32>, time: f32, local_from_world: mat4x4<f32>, phase: f32) -> vec3<f32> {
    if dot(velocity, velocity) < 1e-8 {
        return position;
    }
    let way = tree_way(velocity, local_from_world);
    let push = tree_push(velocity);
    let reach = position - limb.xyz;
    let swing = sin(time * 1.7 + 6.2831853 * limb.w + phase);
    let bough = (way * (0.6 + 0.4 * swing) + vec3<f32>(0.0, 0.3 * swing, 0.0)) * (tree_wind.stiffness.y * push * dot(reach, reach));
    let held = position - own.xyz;
    let across = cross(way + vec3<f32>(0.0, 1.0, 0.0), held);
    let flutter = across * (tree_wind.stiffness.z * sqrt(push) * length(held) * sin(time * tree_wind.stiffness.w + 6.2831853 * own.w + phase));
    return position + tree_bent(position.y, velocity, time, local_from_world, phase) + bough + flutter;
}
"#;

/// The main pass's vertex stage of a tree's stems and leaves, Bevy's mesh vertex stage for a mesh
/// that is neither skinned, morphed nor compressed, its place swayed where the mesh carries the
/// streams.
#[cfg(feature = "render")]
const TREE_VERTEX_MAIN: &str = r#"import bevy_pbr::render::{
    forward_io::VertexOutput,
    mesh_functions,
    mesh_view_bindings::globals,
    view_transformations::position_world_to_clip,
};
"#;

#[cfg(feature = "render")]
const TREE_VERTEX_MAIN_STAGE: &str = r#"
struct TreeVertex {
    @builtin(instance_index) instance_index: u32,
    @location(0) position: vec3<f32>,
    @if(VERTEX_NORMALS)
    @location(1) normal: vec3<f32>,
    @if(VERTEX_UVS_A)
    @location(2) uv: vec2<f32>,
    @if(VERTEX_UVS_B)
    @location(3) uv_b: vec2<f32>,
    @if(VERTEX_TANGENTS)
    @location(4) tangent: vec4<f32>,
    @if(VERTEX_COLORS)
    @location(5) color: vec4<f32>,
    @if(TREE_SWAY)
    @location(8) sway_limb: vec4<f32>,
    @if(TREE_SWAY)
    @location(9) sway_own: vec4<f32>,
}

@vertex
fn vertex(vertex: TreeVertex) -> VertexOutput {
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    var position = vertex.position;
@if(TREE_SWAY)
    position = tree_swayed(position, vertex.sway_limb, vertex.sway_own, tree_wind.now.xyz, globals.time, mesh_functions::get_local_from_world(vertex.instance_index), tree_phase(world_from_local));
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
@if(VERTEX_NORMALS)
    out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
@if(VERTEX_UVS_A)
    out.uv = vertex.uv;
@if(VERTEX_UVS_B)
    out.uv_b = vertex.uv_b;
@if(VERTEX_TANGENTS)
    out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
@if(VERTEX_COLORS)
    out.color = vertex.color;
@if(VERTEX_OUTPUT_INSTANCE_INDEX)
    out.instance_index = vertex.instance_index;
@if(VISIBILITY_RANGE_DITHER)
    out.visibility_range_dither = mesh_functions::get_visibility_range_dither_level(vertex.instance_index, world_from_local[3]);
    return out;
}
"#;

/// The prepass's vertex stage, Bevy's prepass vertex stage for the same meshes, swayed alike, with
/// the place the frame before swayed by the wind the frame before for the motion vectors.
#[cfg(feature = "render")]
const TREE_VERTEX_PREPASS: &str = r#"import bevy_pbr::{
    prepass::io::VertexOutput,
    render::{
        mesh_functions,
        view_transformations::position_world_to_clip,
    },
};
"#;

/// The prepass's view globals, the time among them, which its view bind group holds at binding 1,
/// where the main pass's holds them at 11 and Bevy's prepass shaders read none.
#[cfg(feature = "render")]
pub const PREPASS_GLOBALS: &str = r#"import bevy_render::globals::Globals;

@group(0) @binding(1) var<uniform> globals: Globals;
"#;

#[cfg(feature = "render")]
const TREE_VERTEX_PREPASS_STAGE: &str = r#"
struct TreeVertex {
    @builtin(instance_index) instance_index: u32,
    @location(0) position: vec3<f32>,
    @if(VERTEX_UVS_A)
    @location(1) uv: vec2<f32>,
    @if(VERTEX_UVS_B)
    @location(2) uv_b: vec2<f32>,
    @if(NORMAL_PREPASS_OR_DEFERRED_PREPASS && VERTEX_NORMALS)
    @location(3) normal: vec3<f32>,
    @if(NORMAL_PREPASS_OR_DEFERRED_PREPASS && VERTEX_TANGENTS)
    @location(4) tangent: vec4<f32>,
    @if(VERTEX_COLORS)
    @location(7) color: vec4<f32>,
    @if(TREE_SWAY)
    @location(8) sway_limb: vec4<f32>,
    @if(TREE_SWAY)
    @location(9) sway_own: vec4<f32>,
}

@vertex
fn vertex(vertex: TreeVertex) -> VertexOutput {
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    var position = vertex.position;
@if(TREE_SWAY)
    position = tree_swayed(position, vertex.sway_limb, vertex.sway_own, tree_wind.now.xyz, globals.time, mesh_functions::get_local_from_world(vertex.instance_index), tree_phase(world_from_local));
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
@if(UNCLIPPED_DEPTH_ORTHO_EMULATION) {
    out.unclipped_depth = out.position.z;
    out.position.z = min(out.position.z, 1.0);
}
@if(VERTEX_UVS_A)
    out.uv = vertex.uv;
@if(VERTEX_UVS_B)
    out.uv_b = vertex.uv_b;
@if(NORMAL_PREPASS_OR_DEFERRED_PREPASS && VERTEX_NORMALS)
    out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
@if(NORMAL_PREPASS_OR_DEFERRED_PREPASS && VERTEX_TANGENTS)
    out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
@if(VERTEX_COLORS)
    out.color = vertex.color;
@if(MOTION_VECTOR_PREPASS) {
    let previous_from_local = mesh_functions::get_previous_world_from_local(vertex.instance_index);
    var previous = vertex.position;
@if(TREE_SWAY)
    previous = tree_swayed(previous, vertex.sway_limb, vertex.sway_own, tree_wind.before.xyz, globals.time - globals.delta_time, mesh_functions::get_local_from_world(vertex.instance_index), tree_phase(world_from_local));
    out.previous_world_position = mesh_functions::mesh_position_local_to_world(previous_from_local, vec4<f32>(previous, 1.0));
}
@if(VERTEX_OUTPUT_INSTANCE_INDEX)
    out.instance_index = vertex.instance_index;
@if(VISIBILITY_RANGE_DITHER)
    out.visibility_range_dither = mesh_functions::get_visibility_range_dither_level(vertex.instance_index, world_from_local[3]);
    return out;
}
"#;

/// The main pass's vertex stage of a tree's stems and leaves.
#[cfg(feature = "render")]
pub const TREE_MAIN: bevy::asset::Handle<bevy::shader::Shader> =
    bevy::asset::uuid_handle!("c3e85a1d-47b2-4f90-8d6e-2a9b51f07e48");

/// Its prepass's and deferred prepass's vertex stage.
#[cfg(feature = "render")]
pub const TREE_PREPASS: bevy::asset::Handle<bevy::shader::Shader> =
    bevy::asset::uuid_handle!("6f1d92b8-0c3a-4e75-a4d9-81e7c25b3f06");

/// The sway's WESL with the wind at a material's binding.
#[cfg(feature = "render")]
pub fn sway_at(binding: u32) -> String {
    SWAY_COMMON.replace("@binding(WIND)", &format!("@binding({binding})"))
}

/// Hands a tree material's vertex stages the two sway streams where its mesh carries them, after
/// the attributes Bevy's pipeline gave them, and tells the stages so.
///
/// Bevy builds a pipeline's vertex buffer from its own attributes before a material's specialize
/// runs, and the layout's offsets are the mesh's whole vertex's, so the streams are appended to the
/// buffer Bevy built, at their own offsets, in the main pass and the prepasses alike.
#[cfg(feature = "render")]
pub fn specialize(
    descriptor: &mut bevy::render::render_resource::RenderPipelineDescriptor,
    layout: &bevy::mesh::MeshVertexBufferLayoutRef,
) -> Result<(), bevy::render::render_resource::SpecializedMeshPipelineError> {
    let streams: Vec<_> = [LIMB_STREAM, OWN_STREAM]
        .iter()
        .filter_map(|name| super::streams::lookup(name))
        .filter(|stream| layout.0.contains(stream.attribute.id))
        .map(|stream| stream.attribute.at_shader_location(stream.location))
        .collect();
    if streams.len() == 2
        && let Some(buffer) = descriptor.vertex.buffers.first_mut()
    {
        buffer.attributes.extend(layout.0.get_layout(&streams)?.attributes);
        descriptor.vertex.shader_defs.push("TREE_SWAY".into());
    }
    Ok(())
}

/// What the bark adds to the standard material, the wind it sways in.
#[cfg(feature = "render")]
#[derive(bevy::asset::Asset, bevy::render::render_resource::AsBindGroup, bevy::reflect::Reflect, Debug, Clone, Default)]
pub struct BarkSway {
    #[uniform(100)]
    pub wind: TreeWind,
}

#[cfg(feature = "render")]
impl bevy::pbr::MaterialExtension for BarkSway {
    fn vertex_shader() -> bevy::shader::ShaderRef {
        TREE_MAIN.into()
    }

    fn prepass_vertex_shader() -> bevy::shader::ShaderRef {
        TREE_PREPASS.into()
    }

    fn deferred_vertex_shader() -> bevy::shader::ShaderRef {
        TREE_PREPASS.into()
    }

    fn specialize(
        _pipeline: &bevy::pbr::MaterialExtensionPipeline,
        descriptor: &mut bevy::render::render_resource::RenderPipelineDescriptor,
        layout: &bevy::mesh::MeshVertexBufferLayoutRef,
        _key: bevy::pbr::MaterialExtensionKey<Self>,
    ) -> Result<(), bevy::render::render_resource::SpecializedMeshPipelineError> {
        specialize(descriptor, layout)
    }
}

/// The standard material swaying as a tree's stems do.
#[cfg(feature = "render")]
pub type BarkMaterial = bevy::pbr::ExtendedMaterial<bevy::pbr::StandardMaterial, BarkSway>;

/// Writes the wind into every tree material on a frame it changed, with the wind the frame before,
/// and once more the frame after it settles, so the frame before matches it again.
#[cfg(feature = "render")]
fn write_wind(
    mut blowing: bevy::ecs::system::ResMut<Blowing>,
    mut barks: bevy::ecs::system::ResMut<bevy::asset::Assets<BarkMaterial>>,
    mut leaves: bevy::ecs::system::ResMut<bevy::asset::Assets<super::leaves::LeafMaterial>>,
    mut impostors: bevy::ecs::system::ResMut<bevy::asset::Assets<super::impostors::ImpostorMaterial>>,
) {
    let now = blowing.velocity;
    let before = blowing.written.map_or(now, |(written, _)| written);
    if blowing.written == Some((now, before)) {
        return;
    }
    blowing.written = Some((now, before));

    let blow = |wind: &mut TreeWind| {
        wind.now = now.extend(0.0);
        wind.before = before.extend(0.0);
    };
    for (_, material) in barks.iter_mut() {
        blow(&mut material.extension.wind);
    }
    for (_, material) in leaves.iter_mut() {
        blow(&mut material.extension.wind);
    }
    for (_, material) in impostors.iter_mut() {
        blow(&mut material.extension.wind);
    }
}

/// Adds the bark material's plugin, the two vertex stages and the wind's resource and system.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    use bevy::app::PostUpdate;

    app.add_plugins(bevy::pbr::MaterialPlugin::<BarkMaterial>::default());
    app.init_resource::<Blowing>();
    app.add_systems(PostUpdate, write_wind);
    if let Some(mut shaders) = app.world_mut().get_resource_mut::<bevy::asset::Assets<bevy::shader::Shader>>() {
        // UUID handles, whose insertion cannot fail.
        let _ = shaders.insert(
            TREE_MAIN.id(),
            bevy::shader::Shader::from_wesl(format!("{TREE_VERTEX_MAIN}{}{TREE_VERTEX_MAIN_STAGE}", sway_at(100)), "bcs/tree_vertex.wesl"),
        );
        let _ = shaders.insert(
            TREE_PREPASS.id(),
            bevy::shader::Shader::from_wesl(format!("{TREE_VERTEX_PREPASS}{PREPASS_GLOBALS}{}{TREE_VERTEX_PREPASS_STAGE}", sway_at(100)), "bcs/tree_prepass.wesl"),
        );
    }
}

/// Puts a bark material on an entity when `material` is one, answering whether it was.
#[cfg(feature = "render")]
pub fn attach(entity: &mut bevy::ecs::world::EntityWorldMut, material: &bevy::asset::UntypedHandle) -> bool {
    match material.clone().try_typed::<BarkMaterial>() {
        Ok(handle) => {
            entity.insert(bevy::pbr::MeshMaterial3d(handle));
            true
        }
        Err(_) => false,
    }
}

/// Makes a bark material from the settings a standard material is made with and the tree's
/// stiffness, and returns its key.
///
/// # Safety
///
/// `config` must be null or point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create_bark(
    config: *const BcsMaterialConfig,
    trunk: f32,
    limb: f32,
    flutter: f32,
    frequency: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (config, trunk, limb, flutter, frequency);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let base = match super::assets::standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };
                let extension = BarkSway { wind: TreeWind::new(world, trunk, limb, flutter, frequency) };

                let Some(mut materials) = world.get_resource_mut::<Assets<BarkMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(BarkMaterial { base, extension }).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// Sets the wind the trees sway in, in the world's meters a second.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_tree_wind(x: f32, y: f32, z: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (x, y, z);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !(x.is_finite() && y.is_finite() && z.is_finite()) {
                return status::INVALID_STATE;
            }

            with_world(|world| match world.get_resource_mut::<Blowing>() {
                Some(mut blowing) => {
                    blowing.velocity = bevy::math::Vec3::new(x, y, z);
                    status::OK
                }
                None => status::UNSUPPORTED,
            })
        }
    })
}

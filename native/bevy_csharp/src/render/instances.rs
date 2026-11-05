//! Buffers the engine keeps filled with entities' transforms, this frame's and the previous one's,
//! for shaders that work over the scene rather than over one mesh.
//!
//! Culling instances on the GPU, voxelizing a scene, writing motion for geometry a shader placed
//! itself each need every instance's transform in a buffer, and the previous frame's beside it. The
//! engine knows both, so a buffer is made with room for some number of instances, entities are put
//! in its slots, and every frame after transforms are propagated the slots are written again. A
//! shader reads it as a `StructuredBuffer<Instance>`, whose layout `bcs_scene` declares,
//! two matrices a slot, each four columns, which is 128 bytes.
//!
//! The previous transform is the one the slot held last frame, so an entity put in a slot starts
//! with both the same, and one teleported shows the jump as motion for a frame, which anything
//! reprojecting it needs to know.
//!
//! A material buffer is the same thing for what an entity is made of. Each slot holds its standard
//! material's base color, emissive color, roughness, metallic, reflectance and whether it is unlit,
//! with the alpha its texture's is cut out under and which of the buffer's masks that texture and
//! its metallic and roughness texture are, as `Material`, 64 bytes. A ray that hits something in
//! world-space GI has to know its color for the indirect light it passes on, and an engine can read
//! it from the material where a package guessing from property names cannot. Put the same entity in
//! the same slot of both buffers and a shader has transform and material by one index. A leaf's, a
//! grass card's and a bark's material are the standard material they extend. A cut-out material's
//! base color texture and its metallic and roughness texture are among the buffer's masks, the
//! first [`MOST_MASKS`] such textures among its slots, which a shader binds to read the cutout, the
//! color and how metallic and rough the surface is at a hit, a leaf's metal its texture's to say
//! where the material's own is one. Other textures are not in it, since a texture's average color
//! is work on the GPU, and the material multiplies them by the base color. A tree's impostor stands
//! in for a far tree, turned to the eye as it draws, and its slot's cutoff is above any alpha, so a
//! ray passes it.

#![cfg(feature = "render")]

use std::collections::HashMap;

use bevy::asset::{AssetId, Assets, Handle};
use bevy::ecs::entity::Entity;
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Query, ResMut};
use bevy::ecs::world::World;
use bevy::math::Mat4;
use bevy::render::storage::ShaderBuffer;
use bevy::transform::components::GlobalTransform;

use crate::interop::status;

/// How many bytes one slot of transforms takes.
pub const SLOT_BYTES: usize = 128;

/// How many bytes one slot of a material buffer takes.
pub const MATERIAL_SLOT_BYTES: usize = 64;

/// How many of cut-out materials' base color and metallic and roughness textures a material buffer
/// keeps as masks.
pub const MOST_MASKS: usize = 16;

/// The cutoff of a slot no ray stops at, a tree's impostor's.
pub const PASSED: f32 = 2.0;

/// What a kept buffer's slots hold.
#[derive(Clone, Copy, PartialEq, Eq)]
enum Kind {
    Transforms,
    Materials,
}

/// One buffer the engine keeps filled.
struct Tracked {
    kind: Kind,
    handle: Handle<ShaderBuffer>,
    slots: Vec<Option<Entity>>,
    /// What each slot held on the frame before, which is this frame's previous transform.
    previous: Vec<Mat4>,
    /// What was last written, so a frame on which nothing moved writes nothing.
    written: Vec<u8>,
    /// A material buffer's masks, the base color textures of its cut-out materials, in the order
    /// their slots give them.
    masks: Vec<Handle<bevy::image::Image>>,
    /// How many of its slots' cut-out textures there was no room for among the masks.
    unmasked: usize,
}

/// Every buffer the engine keeps filled with transforms.
#[derive(Resource, Default)]
pub struct InstanceBuffers(HashMap<AssetId<ShaderBuffer>, Tracked>);

/// Makes a buffer with `capacity` slots of transforms and answers its asset key.
pub fn create(world: &mut World, capacity: u32) -> i32 {
    create_kind(world, capacity, Kind::Transforms)
}

/// Makes a buffer with `capacity` slots of materials and answers its asset key.
pub fn create_materials(world: &mut World, capacity: u32) -> i32 {
    create_kind(world, capacity, Kind::Materials)
}

fn create_kind(world: &mut World, capacity: u32, kind: Kind) -> i32 {
    if capacity == 0 {
        return status::NULL_ARG;
    }

    let slot_bytes = match kind {
        Kind::Transforms => SLOT_BYTES,
        Kind::Materials => MATERIAL_SLOT_BYTES,
    };

    let key = super::compute::create_buffer(world, &[], capacity as u64 * slot_bytes as u64);

    if key <= 0 {
        return key;
    }

    let Some(handle) = super::compute::buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };

    let slots = capacity as usize;

    world.get_resource_or_init::<InstanceBuffers>().0.insert(
        handle.id(),
        Tracked {
            kind,
            handle,
            slots: vec![None; slots],
            previous: vec![Mat4::ZERO; slots],
            written: Vec::new(),
            masks: Vec::new(),
            unmasked: 0,
        },
    );

    key
}

/// Puts an entity in a slot of a buffer, or takes the slot's entity out with `None`.
pub fn set(world: &mut World, key: i32, slot: u32, entity: Option<Entity>) -> i32 {
    let Some(handle) = super::compute::buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };

    let current = entity.and_then(|entity| world.get::<GlobalTransform>(entity).map(|global| global.to_matrix()));

    let Some(mut buffers) = world.get_resource_mut::<InstanceBuffers>() else {
        return status::NO_COMPONENT;
    };

    let Some(tracked) = buffers.0.get_mut(&handle.id()) else {
        return status::NO_COMPONENT;
    };

    let Some(place) = tracked.slots.get_mut(slot as usize) else {
        return status::NOT_PRESENT;
    };

    *place = entity;

    // Starts with no motion, rather than moving from wherever the slot's last entity was.
    tracked.previous[slot as usize] = current.unwrap_or(Mat4::ZERO);
    status::OK
}

/// Writes every kept buffer's slots from its entities' transforms, once transforms are propagated.
pub fn write_instances(
    buffers: Option<ResMut<InstanceBuffers>>,
    transforms: Query<&GlobalTransform>,
    mut assets: ResMut<Assets<ShaderBuffer>>,
) {
    let Some(mut buffers) = buffers else {
        return;
    };

    for tracked in buffers.0.values_mut().filter(|tracked| tracked.kind == Kind::Transforms) {
        let mut bytes = Vec::with_capacity(tracked.slots.len() * SLOT_BYTES);

        for (slot, entity) in tracked.slots.iter().enumerate() {
            let current = entity
                .and_then(|entity| transforms.get(entity).ok())
                .map(GlobalTransform::to_matrix)
                .unwrap_or(Mat4::ZERO);

            bytes.extend_from_slice(bytemuck::cast_slice(&current.to_cols_array()));
            bytes.extend_from_slice(bytemuck::cast_slice(&tracked.previous[slot].to_cols_array()));

            tracked.previous[slot] = current;
        }

        upload(tracked, bytes, &mut assets);
    }
}

/// Writes every material buffer's slots from its entities' standard materials, a leaf's, a grass
/// card's and a bark's the standard material each extends. A slot whose entity has none, or is
/// drawn by a shader program, holds zeros.
#[allow(clippy::too_many_arguments)]
pub fn write_materials(
    buffers: Option<ResMut<InstanceBuffers>>,
    standard: Query<&bevy::pbr::MeshMaterial3d<bevy::pbr::StandardMaterial>>,
    leaves: Query<&bevy::pbr::MeshMaterial3d<super::leaves::LeafMaterial>>,
    barks: Query<&bevy::pbr::MeshMaterial3d<super::sway::BarkMaterial>>,
    impostors: Query<(), bevy::ecs::query::With<bevy::pbr::MeshMaterial3d<super::impostors::ImpostorMaterial>>>,
    materials: (
        bevy::ecs::system::Res<Assets<bevy::pbr::StandardMaterial>>,
        bevy::ecs::system::Res<Assets<super::leaves::LeafMaterial>>,
        bevy::ecs::system::Res<Assets<super::sway::BarkMaterial>>,
    ),
    mut assets: ResMut<Assets<ShaderBuffer>>,
) {
    let Some(mut buffers) = buffers else {
        return;
    };
    let (standards, leaf_materials, bark_materials) = materials;

    for tracked in buffers.0.values_mut().filter(|tracked| tracked.kind == Kind::Materials) {
        let mut bytes = Vec::with_capacity(tracked.slots.len() * MATERIAL_SLOT_BYTES);
        let mut masks: Vec<Handle<bevy::image::Image>> = Vec::new();
        let mut unmasked: Vec<AssetId<bevy::image::Image>> = Vec::new();

        for entity in &tracked.slots {
            let base = entity.and_then(|entity| {
                standard
                    .get(entity)
                    .ok()
                    .and_then(|carried| standards.get(&carried.0))
                    .or_else(|| leaves.get(entity).ok().and_then(|carried| leaf_materials.get(&carried.0)).map(|leaf| &leaf.base))
                    .or_else(|| barks.get(entity).ok().and_then(|carried| bark_materials.get(&carried.0)).map(|bark| &bark.base))
            });
            let passed = entity.is_some_and(|entity| impostors.contains(entity));

            let slot: [f32; 16] = match base {
                _ if passed => {
                    let mut slot = [0.0; 16];
                    (slot[12], slot[13]) = (PASSED, -1.0);
                    slot
                }
                None => [0.0; 16],
                Some(material) => {
                    let base = material.base_color.to_linear();
                    let emissive = material.emissive;
                    let reflectance = material.reflectance;
                    let cutoff = cutoff_of(material.alpha_mode);
                    let mask = cut_out_texture(material, cutoff).map_or(-1.0, |texture| mask_of(&mut masks, &mut unmasked, texture));
                    let surface = (cutoff >= 0.0)
                        .then_some(material.metallic_roughness_texture.as_ref())
                        .flatten()
                        .map_or(-1.0, |texture| mask_of(&mut masks, &mut unmasked, texture));

                    [
                        base.red,
                        base.green,
                        base.blue,
                        base.alpha,
                        emissive.red,
                        emissive.green,
                        emissive.blue,
                        // The share of the camera's exposure the emission takes, as Bevy's
                        // shaders read it in the emission's fourth.
                        material.emissive_exposure_weight,
                        material.perceptual_roughness,
                        material.metallic,
                        reflectance,
                        if material.unlit { 1.0 } else { 0.0 },
                        cutoff,
                        mask,
                        surface,
                        0.0,
                    ]
                }
            };

            bytes.extend_from_slice(bytemuck::cast_slice(&slot));
        }

        tracked.masks = masks;
        tracked.unmasked = unmasked.len();
        upload(tracked, bytes, &mut assets);
    }
}

/// The alpha a material is cut out under as Bevy draws it, the cutoff of a masked material and a
/// half where its coverage is spread over the picture's samples, or -1 where it is drawn whole.
fn cutoff_of(mode: bevy::material::AlphaMode) -> f32 {
    match mode {
        bevy::material::AlphaMode::Mask(cutoff) => cutoff,
        bevy::material::AlphaMode::AlphaToCoverage => 0.5,
        _ => -1.0,
    }
}

/// The base color texture of a material cut out under `cutoff`, whose alpha says where it is.
fn cut_out_texture(material: &bevy::pbr::StandardMaterial, cutoff: f32) -> Option<&Handle<bevy::image::Image>> {
    (cutoff >= 0.0).then_some(material.base_color_texture.as_ref()).flatten()
}

/// Which of the buffer's masks a texture is, taking the next where it is none yet and there is
/// room, or -1 where there is none, the material then cut out by its base color's alpha alone and
/// the texture counted among those left out.
fn mask_of(
    masks: &mut Vec<Handle<bevy::image::Image>>,
    unmasked: &mut Vec<AssetId<bevy::image::Image>>,
    texture: &Handle<bevy::image::Image>,
) -> f32 {
    if let Some(index) = masks.iter().position(|mask| mask.id() == texture.id()) {
        return index as f32;
    }
    if masks.len() >= MOST_MASKS {
        if !unmasked.contains(&texture.id()) {
            unmasked.push(texture.id());
        }
        return -1.0;
    }
    masks.push(texture.clone());
    (masks.len() - 1) as f32
}

/// The asset key of mask `index` of the material buffer `key`, or [`status::NOT_PRESENT`] where it
/// has no such mask.
pub fn mask(world: &mut World, key: i32, index: u32) -> i32 {
    let Some(handle) = super::compute::buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };
    let texture = world
        .get_resource::<InstanceBuffers>()
        .and_then(|buffers| buffers.0.get(&handle.id()))
        .filter(|tracked| tracked.kind == Kind::Materials)
        .and_then(|tracked| tracked.masks.get(index as usize).cloned());
    match texture {
        Some(texture) => crate::assets::key_for(world, texture.untyped()),
        None => status::NOT_PRESENT,
    }
}

/// How many cut-out textures the slots of the material buffer `key` name that there was no room
/// for among its masks, or [`status::NO_COMPONENT`] where it is no material buffer.
pub fn unmasked(world: &mut World, key: i32) -> i32 {
    let Some(handle) = super::compute::buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };
    world
        .get_resource::<InstanceBuffers>()
        .and_then(|buffers| buffers.0.get(&handle.id()))
        .filter(|tracked| tracked.kind == Kind::Materials)
        .map_or(status::NO_COMPONENT, |tracked| tracked.unmasked as i32)
}

/// Hands a buffer's new contents to the GPU, if they changed.
fn upload(tracked: &mut Tracked, bytes: Vec<u8>, assets: &mut Assets<ShaderBuffer>) {
    if bytes == tracked.written {
        return;
    }

    let Some(mut buffer) = assets.get_mut(&tracked.handle) else {
        return;
    };

    // The buffer may have been grown since, in which case the slots past the first are zeros.
    let size = buffer.buffer_size() as usize;
    let mut data = bytes.clone();
    data.resize(size.max(data.len()), 0);
    data.truncate(size);
    super::compute::fill(&mut buffer, &data);

    tracked.written = bytes;
}

#[cfg(test)]
mod tests {
    use super::*;

    /// `bcs_scene`'s `Material` is four of four floats, its cutoff and its two masks the fourth's
    /// first three.
    #[test]
    fn a_material_slot_is_four_rows_of_four_floats() {
        assert_eq!(MATERIAL_SLOT_BYTES, 4 * 4 * 4);
        assert_eq!(cutoff_of(bevy::material::AlphaMode::Mask(0.3)), 0.3);
        assert_eq!(cutoff_of(bevy::material::AlphaMode::Opaque), -1.0);
        assert!(PASSED > 1.0);
    }

    /// A buffer's masks are its cut-out textures in the order their slots give them, a texture met
    /// again taking its first index, and none past the last there is room for, each texture left
    /// out counted once however many slots name it.
    #[test]
    fn a_texture_takes_one_mask_however_many_slots_cut_it_out() {
        let (mut masks, mut unmasked) = (Vec::new(), Vec::new());
        let textures: Vec<Handle<bevy::image::Image>> =
            (0..=MOST_MASKS as u128 + 1).map(|n| Handle::Uuid(bevy::asset::uuid::Uuid::from_u128(n + 1), Default::default())).collect();
        assert_eq!(mask_of(&mut masks, &mut unmasked, &textures[0]), 0.0);
        assert_eq!(mask_of(&mut masks, &mut unmasked, &textures[1]), 1.0);
        assert_eq!(mask_of(&mut masks, &mut unmasked, &textures[0]), 0.0);
        for texture in &textures[2..MOST_MASKS] {
            mask_of(&mut masks, &mut unmasked, texture);
        }
        assert!(unmasked.is_empty());
        assert_eq!(mask_of(&mut masks, &mut unmasked, &textures[MOST_MASKS]), -1.0);
        assert_eq!(mask_of(&mut masks, &mut unmasked, &textures[MOST_MASKS]), -1.0);
        assert_eq!(mask_of(&mut masks, &mut unmasked, &textures[MOST_MASKS + 1]), -1.0);
        assert_eq!(masks.len(), MOST_MASKS);
        assert_eq!(unmasked.len(), 2);
    }

    /// The managed side and `bcs_scene` both read a slot as two column-major matrices.
    #[test]
    fn a_slot_is_two_matrices_of_columns() {
        assert_eq!(SLOT_BYTES, 2 * 16 * 4);

        let moved = Mat4::from_translation(bevy::math::Vec3::new(1.0, 2.0, 3.0));
        let columns = moved.to_cols_array();
        assert_eq!(&columns[12..15], &[1.0, 2.0, 3.0]);
    }
}

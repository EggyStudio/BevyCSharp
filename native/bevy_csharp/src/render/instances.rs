//! Buffers the engine keeps filled with entities' transforms, this frame's and the previous one's,
//! for shaders that work over the scene rather than over one mesh.
//!
//! Culling instances on the GPU, voxelizing a scene, writing motion for geometry a shader placed
//! itself each need every instance's transform in a buffer, and the previous frame's beside it. The
//! engine knows both, so a buffer is made with room for some number of instances, entities are put
//! in its slots, and every frame after transforms are propagated the slots are written again. A
//! shader reads it as a `StructuredBuffer<bcs_scene::Instance>`, whose layout `bcs_scene` declares:
//! two matrices a slot, each four columns, which is 128 bytes.
//!
//! The previous transform is the one the slot held last frame, so an entity put in a slot starts
//! with both the same, and one teleported shows the jump as motion for a frame, which anything
//! reprojecting it needs to know.
//!
//! A material buffer is the same thing for what an entity is made of. Each slot holds its standard
//! material's base color, emissive color, roughness, metallic, reflectance and whether it is unlit,
//! as `bcs_scene::Material`, 48 bytes. A ray that hits something in world-space GI has to know its
//! color to bounce light off it, and an engine can read it from the material where a package
//! guessing from property names cannot. Put the same entity in the same slot of both buffers and a
//! shader has transform and material by one index. Textures are not in it, since a texture's
//! average color is work on the GPU; the material multiplies them by the base color.

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
pub const MATERIAL_SLOT_BYTES: usize = 48;

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

/// Writes every material buffer's slots from its entities' standard materials. A slot whose entity
/// has none, or is drawn by a shader program, holds zeros.
pub fn write_materials(
    buffers: Option<ResMut<InstanceBuffers>>,
    carried: Query<&bevy::pbr::MeshMaterial3d<bevy::pbr::StandardMaterial>>,
    materials: bevy::ecs::system::Res<Assets<bevy::pbr::StandardMaterial>>,
    mut assets: ResMut<Assets<ShaderBuffer>>,
) {
    let Some(mut buffers) = buffers else {
        return;
    };

    for tracked in buffers.0.values_mut().filter(|tracked| tracked.kind == Kind::Materials) {
        let mut bytes = Vec::with_capacity(tracked.slots.len() * MATERIAL_SLOT_BYTES);

        for entity in &tracked.slots {
            let material = entity
                .and_then(|entity| carried.get(entity).ok())
                .and_then(|carried| materials.get(&carried.0));

            let slot: [f32; 12] = match material {
                None => [0.0; 12],
                Some(material) => {
                    let base = material.base_color.to_linear();
                    let emissive = material.emissive;
                    let reflectance = material.reflectance;

                    [
                        base.red,
                        base.green,
                        base.blue,
                        base.alpha,
                        emissive.red,
                        emissive.green,
                        emissive.blue,
                        emissive.alpha,
                        material.perceptual_roughness,
                        material.metallic,
                        reflectance,
                        if material.unlit { 1.0 } else { 0.0 },
                    ]
                }
            };

            bytes.extend_from_slice(bytemuck::cast_slice(&slot));
        }

        upload(tracked, bytes, &mut assets);
    }
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
    let size = buffer.buffer_description.size as usize;
    let mut data = bytes.clone();
    data.resize(size.max(data.len()), 0);
    data.truncate(size);
    buffer.data = Some(data);

    tracked.written = bytes;
}

#[cfg(test)]
mod tests {
    use super::*;

    /// The managed side and `bcs_scene` both read a slot as two column-major matrices.
    #[test]
    fn a_slot_is_two_matrices_of_columns() {
        assert_eq!(SLOT_BYTES, 2 * 16 * 4);

        let moved = Mat4::from_translation(bevy::math::Vec3::new(1.0, 2.0, 3.0));
        let columns = moved.to_cols_array();
        assert_eq!(&columns[12..15], &[1.0, 2.0, 3.0]);
    }
}

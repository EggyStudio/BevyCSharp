//! Buffers the engine keeps filled with entities' transforms, this frame's and the previous one's,
//! for shaders that work over the scene rather than over one mesh.
//!
//! Culling instances on the GPU, voxelizing a scene, writing motion for geometry a shader placed
//! itself: each needs every instance's transform in a buffer, and the previous frame's beside it.
//! The engine knows both, so a buffer is made with room for some number of instances, entities are
//! put in its slots, and every frame after transforms are propagated the slots are written again.
//! A shader reads it as a `StructuredBuffer<bcs_scene::Instance>`, whose layout `bcs_scene`
//! declares: two matrices a slot, each four columns, which is 128 bytes.
//!
//! The previous transform is what the slot held last frame, so an entity put in a slot starts with
//! both the same, and one teleported shows the jump as motion for a frame, which is what anything
//! reprojecting it would want to know.

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

/// How many bytes one slot takes.
pub const SLOT_BYTES: usize = 128;

/// One buffer the engine keeps filled.
struct Tracked {
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

/// Makes a buffer with `capacity` slots and answers its asset key.
pub fn create(world: &mut World, capacity: u32) -> i32 {
    if capacity == 0 {
        return status::NULL_ARG;
    }

    let key = super::compute::create_buffer(world, &[], capacity as u64 * SLOT_BYTES as u64);

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

    for tracked in buffers.0.values_mut() {
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

        if bytes == tracked.written {
            continue;
        }

        let Some(mut buffer) = assets.get_mut(&tracked.handle) else {
            continue;
        };

        // The buffer may have been grown since, in which case the slots past the first are zeros.
        let size = buffer.buffer_description.size as usize;
        let mut data = bytes.clone();
        data.resize(size.max(data.len()), 0);
        data.truncate(size);
        buffer.data = Some(data);

        tracked.written = bytes;
    }
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

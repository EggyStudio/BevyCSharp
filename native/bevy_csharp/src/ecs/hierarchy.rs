//! Parents and children.
//!
//! Parenting goes through Bevy's own API rather than through raw component writes. `ChildOf` is a
//! relationship. Inserting it makes Bevy maintain the matching `Children` list, fire hooks and
//! keep transform propagation honest. Writing the bytes directly would set the field and skip all
//! of that, leaving a hierarchy that looks right in one direction only.

use crate::interop::status;
use crate::state::{with_world, with_world_opt};

use super::entity_from;

/// Makes `child` a child of `parent`, replacing any previous parent.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_set_parent(child: u64, parent: u64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let child = entity_from(child);
            let parent = entity_from(parent);

            if world.get_entity(parent).is_err() {
                return status::NO_ENTITY;
            }
            let Ok(mut entity_mut) = world.get_entity_mut(child) else {
                return status::NO_ENTITY;
            };

            entity_mut.insert(bevy::ecs::hierarchy::ChildOf(parent));
            status::OK
        })
    })
}

/// Detaches an entity from its parent. Succeeds whether or not it had one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_clear_parent(child: u64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Ok(mut entity_mut) = world.get_entity_mut(entity_from(child)) else {
                return status::NO_ENTITY;
            };
            entity_mut.remove::<bevy::ecs::hierarchy::ChildOf>();
            status::OK
        })
    })
}

/// Returns an entity's parent, or `0` if it has none.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_parent_of(entity: u64) -> u64 {
    crate::interop::guard_with(0u64, || {
        with_world_opt(|world| {
            world
                .get_entity(entity_from(entity))
                .ok()
                .and_then(|e| e.get::<bevy::ecs::hierarchy::ChildOf>())
                .map_or(0, |child_of| child_of.0.to_bits())
        })
        .unwrap_or(0)
    })
}

/// Writes an entity's children into `out` and returns how many it has.
///
/// A return value greater than `capacity` means nothing was written; grow the buffer and call
/// again. Mirrors the chunk query, so the managed side has one pattern for both.
///
/// # Safety
/// `out` must be valid for `capacity` writes of `u64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_children(entity: u64, out: *mut u64, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let Ok(entity_ref) = world.get_entity(entity_from(entity)) else {
                return status::NO_ENTITY;
            };

            let Some(children) = entity_ref.get::<bevy::ecs::hierarchy::Children>() else {
                return 0;
            };

            let total = children.len();
            if total <= capacity.max(0) as usize {
                for (index, child) in children.iter().enumerate() {
                    // SAFETY: the length was checked against the caller's stated capacity.
                    unsafe { out.add(index).write(child.to_bits()) };
                }
            }

            total as i32
        })
    })
}

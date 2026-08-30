//! Entity and component operations, plus the chunked query C# iterates over.
//!
//! Everything here requires an active world loan (see [`crate::state`]), which means it
//! is only callable from the main thread while a registered system is running. Calls
//! from the generator's parallel worker threads get [`status::NO_WORLD`] back; those
//! paths are expected to mutate through the chunk pointers and queue structural changes
//! on the managed command buffer instead.

use core::ptr::NonNull;

use bevy::ecs::change_detection::DetectChanges;
use bevy::ecs::component::{ComponentId, StorageType};
use bevy::ecs::entity::Entity;
use bevy::ecs::world::World;
use bevy::ptr::OwningPtr;

use crate::interop::{opt_slice, status, BcsChunk};
use crate::state::{with_world, with_world_opt};

/// Bevy documents `Entity` as bit-equivalent to a `u64`; the chunk API relies on it.
const _: () = assert!(size_of::<Entity>() == size_of::<u64>());

/// Rebuilds an `Entity` from the handle C# is holding.
#[inline]
fn entity_from(bits: u64) -> Entity {
    Entity::from_bits(bits)
}

/// Rebuilds a `ComponentId` from the index C# is holding.
#[inline]
fn component_from(index: i32) -> Option<ComponentId> {
    if index < 0 {
        None
    } else {
        Some(ComponentId::new(index as usize))
    }
}

/// Splits an entity handle into its logical index and generation.
///
/// The handle's own bits are deliberately opaque, Bevy documents them as "not meaningful",
/// so this is the only correct way to read an index out of one. It touches no world state, so
/// it is safe to call from any thread at any time. Intended for diagnostics and display, not
/// for hot loops.
///
/// # Safety
/// `index` and `generation` must be writable, or null to skip that output.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_entity_parts(entity: u64, index: *mut u32, generation: *mut u32) {
    crate::interop::guard_with((), || {
        let entity = entity_from(entity);
        if !index.is_null() {
            unsafe { index.write(entity.index().index()) };
        }
        if !generation.is_null() {
            unsafe { generation.write(entity.generation().to_bits()) };
        }
    });
}

/// Spawns an empty entity. Returns `0` if there is no active world loan.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_spawn() -> u64 {
    crate::interop::guard_with(0u64, || {
        with_world_opt(|world| world.spawn_empty().id().to_bits()).unwrap_or(0)
    })
}

/// Despawns an entity and everything on it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_despawn(entity: u64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            if world.despawn(entity_from(entity)) {
                status::OK
            } else {
                status::NO_ENTITY
            }
        })
    })
}

/// Reports whether an entity handle still refers to a live entity.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_alive(entity: u64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| i32::from(world.get_entity(entity_from(entity)).is_ok()))
    })
}

/// Inserts (or replaces) a component, copying `size` bytes from `data`.
///
/// # Safety
/// `data` must point to at least the registered size of `component` readable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_insert(
    entity: u64,
    component: i32,
    data: *const u8,
) -> i32 {
    crate::interop::guard(|| {
        if data.is_null() {
            return status::NULL_ARG;
        }
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };

        with_world(|world| {
            if world.components().get_info(component).is_none() {
                return status::NO_COMPONENT;
            }
            let Ok(mut entity_mut) = world.get_entity_mut(entity_from(entity)) else {
                return status::NO_ENTITY;
            };

            // SAFETY: `component` is registered with a layout matching what C# wrote into
            // `data`, the bytes are plain old data with no drop glue, and Bevy copies them
            // out before the pointer goes away.
            unsafe {
                let ptr = OwningPtr::new(NonNull::new_unchecked(data as *mut u8));
                entity_mut.insert_by_id(component, ptr);
            }
            status::OK
        })
    })
}

/// Removes a component from an entity. Succeeds even if it was not present.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_remove(entity: u64, component: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };
        with_world(|world| {
            let Ok(mut entity_mut) = world.get_entity_mut(entity_from(entity)) else {
                return status::NO_ENTITY;
            };
            entity_mut.remove_by_id(component);
            status::OK
        })
    })
}

/// Reports whether an entity carries a component.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_has(entity: u64, component: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };
        with_world(|world| match world.get_entity(entity_from(entity)) {
            Ok(entity_ref) => i32::from(entity_ref.contains_id(component)),
            Err(_) => status::NO_ENTITY,
        })
    })
}

/// Returns a writable pointer to an entity's component data, or null.
///
/// The pointer is valid until the next structural change to the world.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_get_ptr(entity: u64, component: i32) -> *mut u8 {
    crate::interop::guard_with(core::ptr::null_mut(), || {
        let Some(component) = component_from(component) else {
            return core::ptr::null_mut();
        };
        with_world_opt(|world| {
            let Ok(mut entity_mut) = world.get_entity_mut(entity_from(entity)) else {
                return core::ptr::null_mut();
            };
            match entity_mut.get_mut_by_id(component) {
                Ok(mut value) => value.as_mut().as_ptr(),
                Err(_) => core::ptr::null_mut(),
            }
        })
        .unwrap_or(core::ptr::null_mut())
    })
}

/// Reports whether a component changed since the previous run of the current system.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_changed(entity: u64, component: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };
        with_world(|world| {
            let Ok(mut entity_mut) = world.get_entity_mut(entity_from(entity)) else {
                return status::NO_ENTITY;
            };
            match entity_mut.get_mut_by_id(component) {
                Ok(value) => i32::from(value.is_changed()),
                Err(_) => status::NOT_PRESENT,
            }
        })
    })
}

/// Counts entities carrying `component`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ecs_count(component: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };
        with_world(|world| {
            let mut total = 0i32;
            for table in world.storages().tables.iter() {
                if table.has_column(component) {
                    total += table.entity_count() as i32;
                }
            }
            total
        })
    })
}

/// Describes how a filter component should be tested.
fn table_storage(world: &World, component: ComponentId) -> Option<StorageType> {
    world.components().get_info(component).map(|i| i.storage_type())
}

/// Collects every contiguous run of `component`'s storage that satisfies the filters.
///
/// Writes at most `capacity` chunks into `out` and returns the number of chunks that
/// exist. A return value greater than `capacity` means the buffer was too small and
/// nothing usable was written past the end, the caller should grow and call again.
///
/// `mark_changed` mirrors what Bevy's `Query<&mut T>` does: it stamps every returned row
/// with the current change tick, because C# writes straight through `data` and Bevy has
/// no way to observe those writes itself.
///
/// # Safety
/// `out` must be valid for `capacity` writes of [`BcsChunk`]; the filter arrays must be
/// valid for their stated lengths.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_chunks(
    component: i32,
    with: *const i32,
    with_len: i32,
    without: *const i32,
    without_len: i32,
    mark_changed: i32,
    out: *mut BcsChunk,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        let with = unsafe { opt_slice(with, with_len) };
        let without = unsafe { opt_slice(without, without_len) };

        with_world(|world| {
            let Some(info) = world.components().get_info(component) else {
                return status::NO_COMPONENT;
            };
            if info.storage_type() != StorageType::Table {
                return status::UNSUPPORTED;
            }
            let stride = info.layout().size() as u32;

            // Filters are resolved at the table level, which is only sound for
            // table-stored components. Everything C# registers is table-stored.
            let mut with_ids = Vec::with_capacity(with.len());
            for raw in with {
                let Some(id) = component_from(*raw) else {
                    return status::NO_COMPONENT;
                };
                match table_storage(world, id) {
                    Some(StorageType::Table) => with_ids.push(id),
                    Some(_) => return status::UNSUPPORTED,
                    // An unregistered required component can never be present, so the
                    // result set is empty.
                    None => return 0,
                }
            }
            let mut without_ids = Vec::with_capacity(without.len());
            for raw in without {
                let Some(id) = component_from(*raw) else {
                    return status::NO_COMPONENT;
                };
                match table_storage(world, id) {
                    Some(StorageType::Table) => without_ids.push(id),
                    Some(_) => return status::UNSUPPORTED,
                    // Never present, so it excludes nothing.
                    None => {}
                }
            }

            let this_run = world.change_tick();
            let capacity = capacity.max(0) as usize;
            let mut written = 0usize;
            let mut total = 0usize;

            for table in world.storages().tables.iter() {
                let len = table.entity_count() as usize;
                if len == 0 {
                    continue;
                }
                let Some(column) = table.get_column(component) else {
                    continue;
                };
                if with_ids.iter().any(|id| !table.has_column(*id)) {
                    continue;
                }
                if without_ids.iter().any(|id| table.has_column(*id)) {
                    continue;
                }

                total += 1;
                if written < capacity {
                    // SAFETY: `len > 0`, so row 0 is in bounds; the returned pointer is the
                    // base of `len` tightly packed components.
                    let data = unsafe {
                        column
                            .get_data_unchecked(bevy::ecs::storage::TableRow::new(
                                nonmax::NonMaxU32::ZERO,
                            ))
                            .as_ptr()
                    };
                    let chunk = BcsChunk {
                        entities: table.entities().as_ptr() as *const u64,
                        data,
                        len: len as u32,
                        stride,
                    };
                    // SAFETY: `written < capacity` and `out` is valid for `capacity` writes.
                    unsafe { out.add(written).write(chunk) };
                    written += 1;

                    if mark_changed != 0
                        && let Some(ticks) = table.get_changed_ticks_slice_for(component)
                    {
                        for cell in ticks {
                            // SAFETY: we hold the world exclusively for this call, so no
                            // other reader or writer of these ticks is live.
                            unsafe { *cell.get() = this_run };
                        }
                    }
                }
            }

            total as i32
        })
    })
}

// -- Hierarchy
//
// Parenting goes through Bevy's own API rather than through raw component writes. `ChildOf` is a
// relationship: inserting it makes Bevy maintain the matching `Children` list, fire hooks and
// keep transform propagation honest. Writing the bytes directly would set the field and skip all
// of that, leaving a hierarchy that looks right in one direction only.

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

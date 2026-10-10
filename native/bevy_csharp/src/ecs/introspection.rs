//! What is in the world at all: every entity, the components each carries, and their names.
//!
//! What an editor needs and a game does not. Not "read this component off that entity", which the
//! calls in `ecs` already do, but "what is here at all". A hierarchy has nothing to list and an
//! inspector cannot label a row without these.

use crate::interop::status;
use crate::state::with_world;

use super::{component_from, entity_from};

/// Copies every live entity into `out`, returning how many exist.
///
/// A return value greater than `capacity` means the buffer was too small and nothing usable was
/// written; grow it and call again.
///
/// Every entity, including the ones Bevy spawned for itself, since windows, monitors, cameras and
/// the observers hung off them are all entities. Deciding which of those are worth showing is a
/// question about the editor rather than about the world, so it is answered on the managed side.
///
/// # Safety
/// `out` must be valid for `capacity` writes of `u64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_entities(out: *mut u64, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let capacity = capacity.max(0) as usize;
            let mut total = 0usize;

            for entity in world.iter_entities() {
                if total < capacity {
                    // SAFETY: `total < capacity` and `out` is valid for `capacity` writes.
                    unsafe { out.add(total).write(entity.id().to_bits()) };
                }

                total += 1;
            }

            total as i32
        })
    })
}

/// Copies the ids of the components an entity carries into `out`, returning how many it has.
///
/// Follows the same probe convention, where a return greater than `capacity` means nothing usable
/// was written.
///
/// # Safety
/// `out` must be valid for `capacity` writes of `i32`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_components_of(entity: u64, out: *mut i32, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let Ok(components) = world.inspect_entity(entity_from(entity)) else {
                return status::NO_ENTITY;
            };

            let capacity = capacity.max(0) as usize;
            let mut total = 0usize;

            for (id, _) in components {
                if total < capacity {
                    // SAFETY: `total < capacity` and `out` is valid for `capacity` writes.
                    unsafe { out.add(total).write(id.index() as i32) };
                }

                total += 1;
            }

            total as i32
        })
    })
}

/// Writes a component's name into `out`, returning the length in bytes it needs.
///
/// The other direction of [`crate::app::bcs_component_id_of`], and the one an inspector needs,
/// because asking by name requires knowing the name already, while showing an entity means being
/// handed ids and having to label them.
///
/// The name is whatever registered the component. A C# component carries the name its layout was
/// registered under, which is the managed type's full name; one of Bevy's own carries the Rust
/// path.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_name(component: i32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = component_from(component) else {
            return status::NO_COMPONENT;
        };

        with_world(|world| {
            let Some(info) = world.components().get_info(component) else {
                return status::NO_COMPONENT;
            };

            unsafe { crate::interop::write_text(&info.name().to_string(), out, capacity) }
        })
    })
}

/// Writes an entity's `Name` into `out`, returning the length in bytes it needs.
///
/// Reports [`status::NOT_PRESENT`] for an entity that has no name, which most have. A name is
/// something a scene or a caller gave it, not something every entity carries, and an editor
/// showing "Entity(42v0)" for the rest is telling the truth.
///
/// `Name` holds a string, which is why it is read through here rather than mirrored as a
/// component the managed side can lay out.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_entity_name(entity: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Ok(entity_ref) = world.get_entity(entity_from(entity)) else {
                return status::NO_ENTITY;
            };
            let Some(name) = entity_ref.get::<bevy::ecs::name::Name>() else {
                return status::NOT_PRESENT;
            };

            unsafe { crate::interop::write_text(name.as_str(), out, capacity) }
        })
    })
}

/// Gives an entity a `Name`, replacing any it had.
///
/// The write half of [`bcs_ecs_entity_name`]. A name is the one thing about an entity that is for
/// people rather than for the program, so it is the one an editor has to be able to set, because a
/// list of "Entity(42v0)" is a list nobody can work in.
///
/// An empty name removes the component, which is how an entity goes back to being unnamed.
///
/// # Safety
/// `name` must be a valid null-terminated UTF-8 string, or null to remove the name.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_set_entity_name(
    entity: u64,
    name: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        let text = unsafe { crate::interop::cstr_to_string(name) }.unwrap_or_default();

        with_world(|world| {
            let Ok(mut entity_mut) = world.get_entity_mut(entity_from(entity)) else {
                return status::NO_ENTITY;
            };

            if text.is_empty() {
                entity_mut.remove::<bevy::ecs::name::Name>();
            } else {
                entity_mut.insert(bevy::ecs::name::Name::new(text));
            }

            status::OK
        })
    })
}

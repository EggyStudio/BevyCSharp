//! The entities carrying one of Bevy's components, found by its type path.

use core::ffi::c_char;

use bevy::ecs::entity::Entity;
use bevy::ecs::query::QueryBuilder;
use bevy::ecs::reflect::AppTypeRegistry;

use super::component_of;
use crate::interop::{cstr_to_string, status};
use crate::state::with_world;

/// Writes the entities carrying the component at `type_path` to `out`, as many as `capacity`
/// allows, and answers how many there are.
///
/// A component reached through reflection has no Rust type on the C# side to query by, so this
/// builds Bevy's query by the component's id instead. It is Bevy's query all the same, so Bevy's
/// default filters hide what they hide from a system's query, a disabled entity among them, where
/// walking the archetypes would answer every entity that holds the component.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `out` writable for `capacity` entities, or
/// null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_entities(type_path: *const c_char, out: *mut u64, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let reflect = match component_of(&registry, &type_path) {
                Ok((_, reflect)) => reflect,
                Err(code) => return code,
            };

            let id = reflect.register_component(world);
            let mut query = QueryBuilder::<Entity>::new(world).with_id(id).build();
            let entities: Vec<Entity> = query.iter(world).collect();
            for (place, entity) in entities.iter().take(capacity.max(0) as usize).enumerate() {
                unsafe { out.add(place).write(entity.to_bits()) };
            }
            entities.len() as i32
        })
    })
}

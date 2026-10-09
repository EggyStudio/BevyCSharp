//! Bevy's resources, found as the components on entities of their own they are in this Bevy.

use core::ffi::c_char;

use bevy::ecs::reflect::AppTypeRegistry;

use super::{component_of, fail};
use crate::interop::{cstr_to_string, status};
use crate::state::with_world;

/// Finds the entity holding one of Bevy's resources, writing its bits to `out`, and reports
/// `NOT_PRESENT` when the world has none of it.
///
/// In this Bevy a resource is a component on an entity of its own, marked by `IsResource`, and
/// reflected with a `ReflectComponent` like any other. So once its entity is found, everything
/// this module does to a component it does to a resource, and a resource needs nothing of its own
/// beyond the finding. A world that has none of a resource is given one by inserting it on a new
/// entity, which Bevy's required `IsResource` makes the resource.
///
/// A component that is no resource is refused rather than answered with `NOT_PRESENT`, since a
/// caller asking for the resource of a component's path has made a mistake that absence would
/// hide.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `out` writable for one `u64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_resource_entity(
    type_path: *const c_char,
    out: *mut u64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        if out.is_null() {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (registration, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };
            if registration.data::<bevy::ecs::reflect::ReflectResource>().is_none() {
                return fail(
                    status::NO_COMPONENT,
                    format!("'{type_path}' is a component, not a resource."),
                );
            }

            let id = reflect.register_component(world);
            let Some(entity) = world.resource_entities().get(id) else {
                return fail(status::NOT_PRESENT, format!("The world has no '{type_path}'."));
            };

            unsafe { out.write(entity.to_bits()) };
            status::OK
        })
    })
}

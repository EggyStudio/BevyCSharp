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
/// beyond the finding.
///
/// Bevy keeps the entity once the resource is taken off it, still marked as the resource's, so the
/// world holds the resource only while that entity carries it, as Bevy's `contains_resource` reads
/// it. An entity carrying none is answered as `NOT_PRESENT` here, and [`bcs_reflect_resource_place`]
/// finds it for an insert.
///
/// A component that is no resource is refused rather than answered with `NOT_PRESENT`, since a
/// caller asking for the resource of a component's path has made a mistake that absence would
/// hide.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `out` writable for one `u64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_resource_entity(type_path: *const c_char, out: *mut u64) -> i32 {
    crate::interop::guard(|| unsafe { find(type_path, out, true) })
}

/// Finds the entity Bevy keeps for one of Bevy's resources, writing its bits to `out`, whether or
/// not it carries the resource now, and reports `NOT_PRESENT` when Bevy keeps none.
///
/// A resource is inserted there when Bevy keeps one, and on a new entity only when it keeps none,
/// which Bevy's required `IsResource` then makes the resource's. A new entity beside the one Bevy
/// keeps would have the resource taken off it again by `IsResource`'s hook, with a warning.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `out` writable for one `u64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_resource_place(type_path: *const c_char, out: *mut u64) -> i32 {
    crate::interop::guard(|| unsafe { find(type_path, out, false) })
}

/// Finds the entity Bevy keeps for a resource, and where `holding` asks, only while it carries the
/// resource.
///
/// # Safety
/// As the two callers above.
unsafe fn find(type_path: *const c_char, out: *mut u64, holding: bool) -> i32 {
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
        if holding && !world.get_entity(entity).is_ok_and(|kept| kept.contains_id(id)) {
            return fail(status::NOT_PRESENT, format!("The world has no '{type_path}'."));
        }

        unsafe { out.write(entity.to_bits()) };
        status::OK
    })
}

/// Answers 1 where a reflected type has a default to insert from nothing, Bevy's `ReflectDefault`
/// or `ReflectFromWorld`, and 0 where an insert from nothing builds it field by field.
///
/// A plugin starts a resource at what its own code chooses, and a type with neither default is
/// built with each field at its own, a zero, an empty list or the first variant, which can undo
/// what the plugin set, as `DefaultQueryFilters` built so holds no filter and disabled entities
/// would reappear in every query. So a C# insert from nothing asks this before it replaces a
/// resource the world holds.
///
/// # Safety
/// `type_path` must be a NUL-terminated string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_has_default(type_path: *const c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().read();
            match component_of(&registry, &type_path) {
                Ok((registration, _)) => i32::from(
                    registration.data::<bevy::reflect::std_traits::ReflectDefault>().is_some()
                        || registration.data::<bevy::ecs::reflect::ReflectFromWorld>().is_some(),
                ),
                Err(code) => code,
            }
        })
    })
}

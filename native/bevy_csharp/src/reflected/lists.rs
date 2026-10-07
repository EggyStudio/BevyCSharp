//! How many items a list a component holds has, and its resizing, which a typed wrapper reads and
//! writes a list of records through, each item's fields at an indexed path as a field is.

use bevy::reflect::ReflectMut;

use super::values::read_field;
use super::*;

/// Reads how many items a list or an array a component holds at `path` has.
///
/// Returns the count, or a negative status where the component, the path or a list is not there.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_list_len(entity: u64, type_path: *const c_char, path: *const c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();
        let counted = read_field(entity, &type_path, &path, "a list", |value| match value.reflect_ref() {
            ReflectRef::List(list) => Some(list.len()),
            ReflectRef::Array(array) => Some(array.len()),
            _ => None,
        });
        match counted {
            Ok(len) => i32::try_from(len).unwrap_or(i32::MAX),
            Err(code) => code,
        }
    })
}

/// Makes a list a component holds at `path` `len` items long, taking items off its end or adding
/// them at their default, made as a variant's values are, for the caller to write over.
///
/// The list is changed in place, which marks the component changed, rather than written whole,
/// since applying a shorter list over a longer one leaves the longer one's tail. An array keeps its
/// length and is refused, and so is a list inside an immutable component, which no list a wrapper
/// writes is.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_list_resize(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    len: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let Ok(len) = usize::try_from(len) else {
            return fail(status::INVALID_STATE, "A list cannot be shorter than empty.");
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let id = reflect.register_component(world);
            if world.components().get_info(id).is_some_and(|info| !info.mutable()) {
                return fail(status::UNSUPPORTED, format!("'{type_path}' is immutable, so a list of it is not resized."));
            }

            let Ok(mut found) = world.get_entity_mut(entity_from(entity)) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            let Some(mut component) = reflect.reflect_mut(&mut found) else {
                return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
            };

            let root = component.as_partial_reflect_mut();
            let target = if path.is_empty() {
                root
            } else {
                match path.reflect_element_mut(root) {
                    Ok(target) => target,
                    Err(error) => return unfollowed(&path, error),
                }
            };

            let ReflectMut::List(list) = target.reflect_mut() else {
                return fail(status::INVALID_STATE, format!("'{path}' is not a list."));
            };
            let Some((item, item_path)) = list.get_represented_list_info().map(|info| (info.item_ty().id(), info.item_ty().path())) else {
                return fail(status::INVALID_STATE, format!("'{path}' does not say what it holds."));
            };

            while list.len() > len {
                list.pop();
            }
            while list.len() < len {
                match default_of(&registry, item, item_path) {
                    Ok(value) => list.push(value),
                    Err(code) => return code,
                }
            }
            status::OK
        })
    })
}

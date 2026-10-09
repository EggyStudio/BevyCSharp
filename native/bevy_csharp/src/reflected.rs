//! Bevy's own components, read and written through Bevy's reflection.
//!
//! A mirror (a C# struct with the same bytes as the Rust type) is the fastest way to reach a
//! component, and the most expensive to keep, because each needs a match arm, a layout export, a
//! schema and a test, and each breaks when Bevy reorders a field. So most of Bevy had none and was
//! invisible to C#. Bevy already describes its types at runtime, through the `AppTypeRegistry`
//! every app carries: a type's path, its fields and their types, its enum variants, and through
//! `ReflectComponent` how to insert, read and remove it. This module reads that description
//! instead of writing mirrors, so every reflected component, including one a later Bevy or a
//! plugin adds, is reachable with no code per type.
//!
//! Values cross as JSON, through Bevy's own `TypedReflectSerializer` and
//! `TypedReflectDeserializer`, and a field is addressed by Bevy's reflect path (`intensity`,
//! `color.0.red`). That costs a serialization a call, which suits an inspector, a save, a script
//! setting a light once, and the CLI. A system touching thousands of entities a frame still uses a
//! mirror.
//!
//! A component is named by its full type path (`bevy_light::point_light::PointLight`) rather than
//! its short name, because short names collide across Bevy's crates.
//!
//! Every export reports failure as a status code, and the sentence explaining it is kept in a
//! thread-local that [`bcs_reflect_error`] hands back. A refused path or a malformed value has a
//! reason a caller cannot reconstruct from a code alone, and the code stays the one stable thing to
//! branch on.

use core::any::TypeId;
use core::cell::RefCell;
use core::ffi::c_char;

use bevy::asset::ReflectHandle;
use bevy::color::{Color, LinearRgba, Srgba};
use bevy::ecs::component::ComponentId;
use bevy::ecs::reflect::{AppTypeRegistry, ReflectComponent, ReflectFromWorld};
use bevy::ecs::world::World;
use bevy::reflect::enums::{DynamicEnum, DynamicVariant, VariantInfo};
use bevy::reflect::serde::{TypedReflectDeserializer, TypedReflectSerializer};
use bevy::reflect::std_traits::ReflectDefault;
use bevy::reflect::structs::DynamicStruct;
use bevy::reflect::tuple::DynamicTuple;
use bevy::reflect::{
    PartialReflect, ReflectPath, ReflectRef, ReflectSerialize, TypeInfo, TypeRegistration,
    TypeRegistry,
};
use serde::de::DeserializeSeed;
use serde_json::{json, Map, Value};

use crate::ecs::entity_from;
use crate::interop::{cstr_to_string, status, write_text};
use crate::state::with_world;

mod describe;
mod lists;
mod resources;
mod values;

#[cfg(test)]
mod tests;

thread_local! {
    /// Why the last call on this thread failed. Only read after a failure, so a success leaves it
    /// as it was rather than paying to clear it.
    static LAST_ERROR: RefCell<String> = const { RefCell::new(String::new()) };
}

/// Records why a call failed and returns its status, so a failure is one expression.
fn fail(code: i32, message: impl Into<String>) -> i32 {
    LAST_ERROR.with(|last| *last.borrow_mut() = message.into());
    code
}

/// Finds a reflected component by its type path.
fn component_of<'r>(
    registry: &'r TypeRegistry,
    path: &str,
) -> Result<(&'r TypeRegistration, &'r ReflectComponent), i32> {
    let Some(registration) = registry.get_with_type_path(path) else {
        return Err(fail(
            status::NO_COMPONENT,
            format!("No reflected type is registered as '{path}'. Names are full type paths."),
        ));
    };

    let Some(reflect) = registration.data::<ReflectComponent>() else {
        return Err(fail(
            status::NO_COMPONENT,
            format!("'{path}' is reflected but is not registered as a component."),
        ));
    };

    Ok((registration, reflect))
}

/// Walks a reflect path from a component's root, where an empty path is the component itself.
fn at<'a>(root: &'a dyn PartialReflect, path: &str) -> Result<&'a dyn PartialReflect, i32> {
    if path.is_empty() {
        return Ok(root);
    }
    path.reflect_element(root).map_err(|error| unfollowed(path, error))
}

/// Reports a reflect path that leads nowhere, with Bevy's account of where it stopped.
fn unfollowed(path: &str, error: impl core::fmt::Display) -> i32 {
    fail(status::INVALID_STATE, format!("The path '{path}' cannot be followed. {error}"))
}

/// The registration of whatever a field holds, which deserializing into it needs.
fn registration_of<'r>(
    registry: &'r TypeRegistry,
    value: &dyn PartialReflect,
) -> Result<&'r TypeRegistration, i32> {
    let Some(info) = value.get_represented_type_info() else {
        return Err(fail(status::INVALID_STATE, "The value has no type information."));
    };
    registry.get(info.type_id()).ok_or_else(|| {
        fail(
            status::INVALID_STATE,
            format!("'{}' is not registered, so it cannot be read from JSON.", info.type_path()),
        )
    })
}

/// Reads a JSON value as an instance of a registered type.
fn from_json(
    registry: &TypeRegistry,
    registration: &TypeRegistration,
    json: &str,
) -> Result<Box<dyn PartialReflect>, i32> {
    let mut reader = serde_json::Deserializer::from_str(json);
    TypedReflectDeserializer::new(registration, registry)
        .deserialize(&mut reader)
        .map_err(|error| {
            fail(
                status::INVALID_STATE,
                format!(
                    "'{json}' cannot be read as {}. {error}",
                    registration.type_info().type_path()
                ),
            )
        })
}

/// A type's default value, from `ReflectDefault`, which building an enum variant's fields, and a
/// struct with no default of its own, needs.
///
/// A handle registers no default, though it has one, the handle to its type's default asset, so
/// one is made from its `ReflectHandle` instead, and an enum with none is its first variant that
/// holds nothing, and a struct with none is made from its fields'. Without it a variant holding a
/// handle, as a
/// text's font source or a fog volume's optional density texture does, could not be chosen, and a
/// caller choosing that variant writes the handle it means right after.
fn default_of(
    registry: &TypeRegistry,
    type_id: TypeId,
    path: &str,
) -> Result<Box<dyn PartialReflect>, i32> {
    if let Some(handle) = registry.get_type_data::<ReflectHandle>(type_id) {
        let untyped = bevy::asset::UntypedHandle::Uuid {
            type_id: handle.asset_type_id(),
            uuid: bevy::asset::AssetId::<()>::DEFAULT_UUID,
        };
        return Ok(handle.typed(untyped).into_partial_reflect());
    }

    if let Some(default) = registry.get_type_data::<ReflectDefault>(type_id) {
        return Ok(default.default().into_partial_reflect());
    }

    // An entity has no default, and a component pointing at one, as a scrollbar names the node it
    // scrolls, is made pointing at Bevy's placeholder, which names nothing, for the caller to
    // write the real one over.
    if type_id == TypeId::of::<bevy::ecs::entity::Entity>() {
        return Ok(Box::new(bevy::ecs::entity::Entity::PLACEHOLDER));
    }

    // A list registers no default, though Rust's is the empty one, as a gradient's stops are.
    if let Some(info @ TypeInfo::List(_)) = registry.get_type_info(type_id) {
        let mut made = bevy::reflect::list::DynamicList::default();
        made.set_represented_type(Some(info));
        return Ok(Box::new(made));
    }

    // A range of numbers registers no default, though Rust's is the empty range at zero, which a
    // struct holding one, as a visibility range holds its margins, is made with for the caller to
    // write the real ones over.
    if type_id == TypeId::of::<core::ops::Range<f32>>() {
        return Ok(Box::new(0.0f32..0.0));
    }

    // An enum with no default of its own, as a cubemap's layout is, takes its first variant that
    // holds nothing, or where every variant holds something, as a gradient's, its first made from
    // its values' defaults, which a caller choosing a variant then writes over.
    if let Some(TypeInfo::Enum(info)) = registry.get_type_info(type_id)
        && let Some(chosen) = info.iter().find(|variant| matches!(variant, VariantInfo::Unit(_))).or_else(|| info.iter().next())
    {
        let mut made = DynamicEnum::new(chosen.name(), values_of(registry, chosen)?);
        made.set_represented_type(Some(registry.get_type_info(type_id).unwrap()));
        return Ok(Box::new(made));
    }

    if let Some(built) = registry.get_type_info(type_id).and_then(|info| from_fields(registry, info)) {
        return built;
    }

    Err(fail(
        status::UNSUPPORTED,
        format!("'{path}' has no reflected default, so a variant holding one cannot be made."),
    ))
}

/// A variant's values, each at its default, for a variant chosen or made as an enum's default.
fn values_of(registry: &TypeRegistry, chosen: &VariantInfo) -> Result<DynamicVariant, i32> {
    match chosen {
        VariantInfo::Unit(_) => Ok(DynamicVariant::Unit),
        VariantInfo::Tuple(tuple) => tuple
            .iter()
            .try_fold(DynamicTuple::default(), |mut values, f| {
                values.insert_boxed(default_of(registry, f.type_id(), f.type_path())?);
                Ok(values)
            })
            .map(DynamicVariant::Tuple),
        VariantInfo::Struct(named) => named
            .iter()
            .try_fold(DynamicStruct::default(), |mut values, f| {
                values.insert_boxed(f.name(), default_of(registry, f.type_id(), f.type_path())?);
                Ok(values)
            })
            .map(DynamicVariant::Struct),
    }
}

/// A struct with no default of its own made from each field's default, a handle's included, for
/// the caller to write over, as a component holding a handle is inserted and a sprite's slicer is
/// made for its variant, or nothing for a type that is no struct.
fn from_fields(registry: &TypeRegistry, info: &'static TypeInfo) -> Option<Result<Box<dyn PartialReflect>, i32>> {
    let built = match info {
        TypeInfo::Struct(named) => named
            .iter()
            .try_fold(DynamicStruct::default(), |mut made, field| {
                made.insert_boxed(field.name(), default_of(registry, field.type_id(), field.type_path())?);
                Ok(made)
            })
            .map(|mut made| {
                made.set_represented_type(Some(info));
                Box::new(made) as Box<dyn PartialReflect>
            }),
        // The same for a struct of unnamed fields, as a slider's value is.
        TypeInfo::TupleStruct(unnamed) => unnamed
            .iter()
            .try_fold(bevy::reflect::tuple_struct::DynamicTupleStruct::default(), |mut made, field| {
                made.insert_boxed(default_of(registry, field.type_id(), field.type_path())?);
                Ok(made)
            })
            .map(|mut made| {
                made.set_represented_type(Some(info));
                Box::new(made) as Box<dyn PartialReflect>
            }),
        _ => return None,
    };

    Some(built)
}

/// Reads UTF-8 bytes the caller passed with their length.
///
/// # Safety
/// `text` must be readable for `len` bytes when `len` is positive.
unsafe fn text_from(text: *const u8, len: i32) -> Result<String, i32> {
    let bytes = unsafe { crate::interop::opt_slice(text, len) };
    core::str::from_utf8(bytes)
        .map(str::to_owned)
        .map_err(|_| fail(status::NULL_ARG, "The value is not UTF-8."))
}

/// Resolves a reflected component's type path to its id, registering the component if no plugin
/// has yet, as [`crate::app::bcs_component_id_of`] does for the names it matches by hand.
pub(crate) fn component_id(world: &mut World, path: &str) -> Option<ComponentId> {
    let registry = world.resource::<AppTypeRegistry>().clone();
    let registry = registry.read();
    let reflect = registry.get_with_type_path(path)?.data::<ReflectComponent>()?;
    Some(reflect.register_component(world))
}

// -- Values

/// Reads a component, or one field of it, as JSON.
///
/// `path` is Bevy's reflect path from the component's root, and empty for the whole component.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, `path` one or null, and `out` writable for
/// `capacity` bytes or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_get(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let Ok(entity) = world.get_entity(entity_from(entity)) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            let Some(component) = reflect.reflect(entity) else {
                return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
            };
            let value = match at(component.as_partial_reflect(), &path) {
                Ok(value) => value,
                Err(code) => return code,
            };

            match serde_json::to_string(&TypedReflectSerializer::new(value, &registry)) {
                Ok(text) => unsafe { write_text(&text, out, capacity) },
                Err(error) => match type_names(value, &registry) {
                    Some(names) => unsafe { write_text(&names.to_string(), out, capacity) },
                    None => fail(
                        status::INVALID_STATE,
                        format!("'{path}' cannot be written as JSON. {error}"),
                    ),
                },
            }
        })
    })
}

/// A value made of Rust type ids, written as the names of the types they stand for.
///
/// A type id has no serialized form, which is right for a file, since an id is good for one build
/// only, and leaves a component that lists types, such as `VisibilityClass`, with nothing to show
/// in an inspector or a listing. The registry knows each registered type's name, so a lone id, a
/// list or an array of them, or a newtype over either, reads as those names, and anything else
/// as nothing, which leaves the refusal as it was.
fn type_names(value: &dyn PartialReflect, registry: &TypeRegistry) -> Option<Value> {
    if let Some(id) = value.try_downcast_ref::<TypeId>() {
        let name = registry
            .get_type_info(*id)
            .map(|info| info.type_path_table().short_path().to_string())
            .unwrap_or_else(|| "an unregistered type".to_string());
        return Some(Value::String(name));
    }

    match value.reflect_ref() {
        ReflectRef::List(list) => list
            .iter()
            .map(|item| type_names(item, registry))
            .collect::<Option<Vec<_>>>()
            .map(Value::Array),
        ReflectRef::Array(array) => array
            .iter()
            .map(|item| type_names(item, registry))
            .collect::<Option<Vec<_>>>()
            .map(Value::Array),
        ReflectRef::TupleStruct(newtype) if newtype.field_len() == 1 => {
            type_names(newtype.field(0)?, registry)
        }
        _ => None,
    }
}

/// Reads the name of the variant an enum field holds.
///
/// An enum's JSON is not a reliable way to learn this, because Bevy writes an `Option` as `null`
/// or the bare value, and an enum serialized through serde may rename its variants.
///
/// # Safety
/// As [`bcs_reflect_get`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_variant(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let Ok(entity) = world.get_entity(entity_from(entity)) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            let Some(component) = reflect.reflect(entity) else {
                return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
            };
            let value = match at(component.as_partial_reflect(), &path) {
                Ok(value) => value,
                Err(code) => return code,
            };

            match value.reflect_ref() {
                ReflectRef::Enum(value) => unsafe {
                    write_text(value.variant_name(), out, capacity)
                },
                _ => fail(status::INVALID_STATE, format!("'{path}' is not an enum.")),
            }
        })
    })
}


/// Writes a value read from JSON over a component or one field of it.
///
/// The value is read against the type the field holds before anything is borrowed mutably, so a
/// malformed one is refused without the component being marked changed. The write goes through
/// Bevy's `Mut`, so change detection sees it as it sees any other.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, `path` one or null, and `json` readable for
/// `len` bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_set(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    json: *const u8,
    len: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();
        let json = match unsafe { text_from(json, len) } {
            Ok(json) => json,
            Err(code) => return code,
        };

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let entity = entity_from(entity);
            let value = {
                let Ok(found) = world.get_entity(entity) else {
                    return fail(status::NO_ENTITY, "The entity does not exist.");
                };
                let Some(component) = reflect.reflect(found) else {
                    return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
                };
                let target = match at(component.as_partial_reflect(), &path) {
                    Ok(target) => target,
                    Err(code) => return code,
                };
                let registration = match registration_of(&registry, target) {
                    Ok(registration) => registration,
                    Err(code) => return code,
                };
                match from_json(&registry, registration, &json) {
                    Ok(value) => value,
                    Err(code) => return code,
                }
            };

            apply(world, reflect, entity, &type_path, &path, value.as_ref())
        })
    })
}

/// Applies a value over a field of a component that is known to be present and mutable.
fn apply(
    world: &mut World,
    reflect: &ReflectComponent,
    entity: bevy::ecs::entity::Entity,
    type_path: &str,
    path: &str,
    value: &dyn PartialReflect,
) -> i32 {
    // An immutable component, as a slider's value is, is changed by inserting it again, which
    // runs its hooks and observers as Bevy means it to. So it is copied, the copy written, and the
    // copy inserted in its place.
    let id = reflect.register_component(world);
    if world.components().get_info(id).is_some_and(|info| !info.mutable()) {
        return replace(world, reflect, entity, type_path, path, value);
    }

    let Ok(mut found) = world.get_entity_mut(entity) else {
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
            Err(error) => return unfollowed(path, error),
        }
    };

    match target.try_apply(value) {
        Ok(()) => status::OK,
        Err(error) => fail(
            status::INVALID_STATE,
            format!("'{path}' could not take the value. {error}"),
        ),
    }
}

/// Writes a value over an immutable component, or one field of it, by inserting a written copy.
fn replace(
    world: &mut World,
    reflect: &ReflectComponent,
    entity: bevy::ecs::entity::Entity,
    type_path: &str,
    path: &str,
    value: &dyn PartialReflect,
) -> i32 {
    let mut copy = {
        let Ok(found) = world.get_entity(entity) else {
            return fail(status::NO_ENTITY, "The entity does not exist.");
        };
        let Some(component) = reflect.reflect(found) else {
            return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
        };
        match component.as_partial_reflect().to_dynamic() {
            Ok(copy) => copy,
            Err(error) => {
                return fail(
                    status::INVALID_STATE,
                    format!("'{type_path}' could not be copied to write. {error}"),
                );
            }
        }
    };

    let target = if path.is_empty() {
        copy.as_mut()
    } else {
        match path.reflect_element_mut(copy.as_mut()) {
            Ok(target) => target,
            Err(error) => return unfollowed(path, error),
        }
    };
    if let Err(error) = target.try_apply(value) {
        return fail(
            status::INVALID_STATE,
            format!("'{path}' could not take the value. {error}"),
        );
    }

    let registry = world.resource::<AppTypeRegistry>().clone();
    let registry = registry.read();
    let Ok(mut found) = world.get_entity_mut(entity) else {
        return fail(status::NO_ENTITY, "The entity does not exist.");
    };
    reflect.insert(&mut found, copy.as_ref(), &registry);
    status::OK
}

/// Switches an enum field to another variant, with every field of that variant at its default.
///
/// A variant cannot be written as JSON without its fields' values, which a caller choosing a
/// variant from a list does not have. Bevy's derived `apply` builds the new variant from a
/// `DynamicEnum`, so one is assembled here from each field type's `ReflectDefault`. A variant
/// holding a type with no reflected default cannot be made, and is refused with `UNSUPPORTED`.
///
/// # Safety
/// `type_path` and `variant` must be NUL-terminated strings, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_set_variant(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    variant: *const c_char,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let Some(variant) = (unsafe { cstr_to_string(variant) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let entity = entity_from(entity);
            let made = {
                let Ok(found) = world.get_entity(entity) else {
                    return fail(status::NO_ENTITY, "The entity does not exist.");
                };
                let Some(component) = reflect.reflect(found) else {
                    return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
                };
                let target = match at(component.as_partial_reflect(), &path) {
                    Ok(target) => target,
                    Err(code) => return code,
                };
                let Some(TypeInfo::Enum(info)) = target.get_represented_type_info() else {
                    return fail(status::INVALID_STATE, format!("'{path}' is not an enum."));
                };
                let Some(chosen) = info.variant(&variant) else {
                    return fail(
                        status::INVALID_STATE,
                        format!("'{}' has no variant '{variant}'.", info.type_path()),
                    );
                };

                let fields = match values_of(&registry, chosen) {
                    Ok(fields) => fields,
                    Err(code) => return code,
                };

                let mut made = DynamicEnum::new(variant.as_str(), fields);
                made.set_represented_type(Some(target.get_represented_type_info().unwrap()));
                made
            };

            apply(world, reflect, entity, &type_path, &path, &made)
        })
    })
}

/// Inserts a component from JSON, or at its default when `len` is zero.
///
/// The default is `ReflectDefault` where the type has one and `ReflectFromWorld` otherwise, which
/// is how Bevy builds a component that needs a resource to make. Inserting over a component the
/// entity already has replaces it, as an insert does anywhere else.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `json` readable for `len` bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_insert(
    entity: u64,
    type_path: *const c_char,
    json: *const u8,
    len: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let json = match unsafe { text_from(json, len) } {
            Ok(json) => json,
            Err(code) => return code,
        };

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (registration, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let value: Box<dyn PartialReflect> = if !json.is_empty() {
                match from_json(&registry, registration, &json) {
                    Ok(value) => value,
                    Err(code) => return code,
                }
            } else if let Some(default) = registration.data::<ReflectDefault>() {
                default.default().into_partial_reflect()
            } else if let Some(from_world) = registration.data::<ReflectFromWorld>() {
                from_world.from_world(world).into_partial_reflect()
            } else if let Some(built) = from_fields(&registry, registration.type_info()) {
                match built {
                    Ok(value) => value,
                    Err(code) => return code,
                }
            } else {
                return fail(
                    status::UNSUPPORTED,
                    format!(
                        "'{type_path}' has no reflected default, so it needs a value to insert."
                    ),
                );
            };

            let Ok(mut found) = world.get_entity_mut(entity_from(entity)) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            reflect.insert(&mut found, value.as_ref(), &registry);
            status::OK
        })
    })
}

/// Removes a reflected component, reporting `NOT_PRESENT` when the entity had none.
///
/// # Safety
/// `type_path` must be a NUL-terminated string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_remove(entity: u64, type_path: *const c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let Ok(mut found) = world.get_entity_mut(entity_from(entity)) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            if !reflect.contains(&found) {
                return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
            }
            reflect.remove(&mut found);
            status::OK
        })
    })
}

/// Reports why the last reflected call on this thread failed.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_error(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        LAST_ERROR.with(|last| unsafe { write_text(&last.borrow(), out, capacity) })
    })
}

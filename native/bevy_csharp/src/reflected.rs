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
/// holds nothing. Without it a variant holding a handle, as a
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

    // An enum with no default of its own, as a cubemap's layout is, takes its first variant that
    // holds nothing, which a caller choosing it then writes over.
    if let Some(TypeInfo::Enum(info)) = registry.get_type_info(type_id)
        && let Some(unit) = info.iter().find(|variant| matches!(variant, VariantInfo::Unit(_)))
    {
        let mut made = DynamicEnum::new(unit.name(), DynamicVariant::Unit);
        made.set_represented_type(Some(registry.get_type_info(type_id).unwrap()));
        return Ok(Box::new(made));
    }

    Err(fail(
        status::UNSUPPORTED,
        format!("'{path}' has no reflected default, so a variant holding one cannot be made."),
    ))
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

// -- The registry

/// A field's or type's documentation, which only an editor build carries.
///
/// `docs()` exists only when `bevy_reflect` keeps doc comments, which the editor profile turns on
/// and a game's profile leaves off, since a player has no tooltip to read them in and the comments
/// would make every shipped library larger.
#[cfg(feature = "editor")]
macro_rules! docs {
    ($described:expr) => {
        $described.docs()
    };
}

#[cfg(not(feature = "editor"))]
macro_rules! docs {
    ($described:expr) => {{
        let _ = &$described;
        None::<&'static str>
    }};
}

/// Describes a type and everything its fields hold, once each, into `types`.
///
/// The description is closed over what is reachable, so the managed side can flatten a nested
/// struct into rows without asking again. A type is entered before its fields are walked, which
/// ends the walk at a type that contains itself.
fn describe(registry: &TypeRegistry, info: &'static TypeInfo, types: &mut Map<String, Value>) {
    let path = info.type_path();
    if types.contains_key(path) {
        return;
    }
    types.insert(path.to_owned(), Value::Null);

    let registration = registry.get(info.type_id());
    let mut entry = json!({
        "short": info.type_path_table().short_path(),
        // An unregistered type cannot be serialized at all, since the serializer looks every
        // struct up, so the managed side shows such a field without offering to edit it.
        "registered": registration.is_some(),
        "serialize": registration.is_some_and(|r| r.data::<ReflectSerialize>().is_some()),
        "default": registration.is_some_and(|r| r.data::<ReflectDefault>().is_some()),
    });

    if let Some(text) = docs!(info) {
        entry["docs"] = Value::from(text);
    }

    // A color is one value to the managed side, a swatch, whatever space it is held in, so the
    // three types Bevy keeps colors in are marked rather than walked into their fields.
    if is_color(info.type_id()) {
        entry["color"] = Value::Bool(true);
    }

    // A handle names the kind of asset it holds, so the managed side can offer files of that kind.
    // An asset type the bridge does not load gets an empty kind, which still says it is a handle.
    if let Some(handle) = registration.and_then(|r| r.data::<ReflectHandle>()) {
        entry["asset"] = Value::from(crate::events::kind_of(handle.asset_type_id()));
    }

    let mut field = |name: String,
                     ty: &'static str,
                     nested: Option<&'static TypeInfo>,
                     text: Option<&'static str>| {
        if let Some(nested) = nested {
            describe(registry, nested, types);
        }
        match text {
            Some(text) => json!({ "name": name, "type": ty, "docs": text }),
            None => json!({ "name": name, "type": ty }),
        }
    };

    let kind = match info {
        TypeInfo::Struct(info) => {
            let fields: Vec<Value> = info
                .iter()
                .map(|f| field(f.name().to_owned(), f.type_path(), f.type_info(), docs!(f)))
                .collect();
            entry["fields"] = Value::Array(fields);
            "struct"
        }
        TypeInfo::TupleStruct(info) => {
            let fields: Vec<Value> = info
                .iter()
                .map(|f| field(f.index().to_string(), f.type_path(), f.type_info(), docs!(f)))
                .collect();
            entry["fields"] = Value::Array(fields);
            "tuple_struct"
        }
        TypeInfo::Enum(info) => {
            let variants: Vec<Value> = info
                .iter()
                .map(|variant| match variant {
                    VariantInfo::Unit(unit) => json!({ "name": unit.name(), "kind": "unit" }),
                    VariantInfo::Tuple(tuple) => json!({
                        "name": tuple.name(),
                        "kind": "tuple",
                        "fields": tuple.iter()
                            .map(|f| {
                                let name = f.index().to_string();
                                field(name, f.type_path(), f.type_info(), docs!(f))
                            })
                            .collect::<Vec<_>>(),
                    }),
                    VariantInfo::Struct(named) => json!({
                        "name": named.name(),
                        "kind": "struct",
                        "fields": named.iter()
                            .map(|f| {
                                let name = f.name().to_owned();
                                field(name, f.type_path(), f.type_info(), docs!(f))
                            })
                            .collect::<Vec<_>>(),
                    }),
                })
                .collect();
            entry["variants"] = Value::Array(variants);
            "enum"
        }
        TypeInfo::Tuple(_) => "tuple",
        // A list or an array says what it holds, so one of numbers or vectors can be edited as a
        // list rather than shown as JSON. An array's length is fixed, which the managed side keeps.
        TypeInfo::List(info) => {
            entry["item"] = Value::from(info.item_ty().path());
            if let Some(item) = info.item_info() {
                describe(registry, item, types);
            }
            "list"
        }
        TypeInfo::Array(info) => {
            entry["item"] = Value::from(info.item_ty().path());
            entry["capacity"] = Value::from(info.capacity());
            if let Some(item) = info.item_info() {
                describe(registry, item, types);
            }
            "array"
        }
        TypeInfo::Map(_) => "map",
        TypeInfo::Set(_) => "set",
        TypeInfo::Opaque(_) => "opaque",
    };
    entry["kind"] = Value::from(kind);

    types.insert(path.to_owned(), entry);
}

/// Builds the description [`bcs_reflect_types`] returns.
///
/// Every reflected component is registered with the world here, deliberately. Its id is then
/// fixed for the run, so the managed side keeps it as a constant rather than asking for it again,
/// and a component no plugin has touched yet can still be inserted by the editor.
fn registry_json(world: &mut World) -> String {
    let registry = world.resource::<AppTypeRegistry>().clone();
    let registry = registry.read();

    let mut components = Vec::new();
    let mut types = Map::new();

    for registration in registry.iter() {
        let Some(reflect) = registration.data::<ReflectComponent>() else {
            continue;
        };

        let id = reflect.register_component(world);
        let mutable = world.components().get_info(id).is_none_or(|info| info.mutable());
        let info = registration.type_info();
        let table = info.type_path_table();

        components.push(json!({
            "path": table.path(),
            "short": table.short_path(),
            "id": id.index(),
            "default": registration.data::<ReflectDefault>().is_some()
                || registration.data::<ReflectFromWorld>().is_some(),
            "mutable": mutable,
        }));
        describe(&registry, info, &mut types);
    }

    components.sort_by(|a, b| a["path"].as_str().cmp(&b["path"].as_str()));

    json!({ "components": components, "types": types }).to_string()
}

/// Describes every reflected component and every type its fields reach, as JSON.
///
/// Taken after startup, because a type a plugin registers exists only once that plugin has been
/// added. The text is built again on each call, so a caller probing for its length pays twice,
/// which is acceptable for something asked once a run.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_types(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let text = registry_json(world);
            unsafe { write_text(&text, out, capacity) }
        })
    })
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

/// Checks a component can be written in place, before `reflect_mut` is asked to and panics.
fn writable(world: &mut World, reflect: &ReflectComponent, type_path: &str) -> Result<(), i32> {
    let id = reflect.register_component(world);
    match world.components().get_info(id) {
        Some(info) if !info.mutable() => Err(fail(
            status::UNSUPPORTED,
            format!(
                "'{type_path}' is immutable, so it is replaced by inserting it rather than written."
            ),
        )),
        _ => Ok(()),
    }
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
            if let Err(code) = writable(world, reflect, &type_path) {
                return code;
            }

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
            if let Err(code) = writable(world, reflect, &type_path) {
                return code;
            }

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

                let fields = match chosen {
                    VariantInfo::Unit(_) => Ok(DynamicVariant::Unit),
                    VariantInfo::Tuple(tuple) => {
                        let mut fields = DynamicTuple::default();
                        tuple
                            .iter()
                            .try_for_each(|f| {
                                let value = default_of(&registry, f.type_id(), f.type_path())?;
                                fields.insert_boxed(value);
                                Ok(())
                            })
                            .map(|()| DynamicVariant::Tuple(fields))
                    }
                    VariantInfo::Struct(named) => {
                        let mut fields = DynamicStruct::default();
                        named
                            .iter()
                            .try_for_each(|f| {
                                let value = default_of(&registry, f.type_id(), f.type_path())?;
                                fields.insert_boxed(f.name(), value);
                                Ok(())
                            })
                            .map(|()| DynamicVariant::Struct(fields))
                    }
                };
                let fields = match fields {
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
            } else if let TypeInfo::Struct(info) = registration.type_info() {
                // A struct with no default of its own, as one holding a handle is, made from each
                // field's default, a handle's included, for the caller to write over.
                let mut fields = DynamicStruct::default();
                for field in info.iter() {
                    match default_of(&registry, field.type_id(), field.type_path()) {
                        Ok(value) => fields.insert_boxed(field.name(), value),
                        Err(code) => return code,
                    }
                }
                fields.set_represented_type(Some(registration.type_info()));
                Box::new(fields)
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

// -- Asset handles

/// The `ReflectHandle` of the handle a field holds, which moves it in and out of the key table.
fn handle_data<'r>(
    registry: &'r TypeRegistry,
    value: &dyn PartialReflect,
    path: &str,
) -> Result<&'r ReflectHandle, i32> {
    value
        .get_represented_type_info()
        .and_then(|info| registry.get_type_data::<ReflectHandle>(info.type_id()))
        .ok_or_else(|| fail(status::INVALID_STATE, format!("'{path}' is not an asset handle.")))
}

/// What makes a typed handle of the kind an `Option<Handle<T>>` holds, or nothing for a value that
/// is no such option.
fn optional_handle_data<'r>(registry: &'r TypeRegistry, value: &dyn PartialReflect) -> Option<&'r ReflectHandle> {
    let bevy::reflect::TypeInfo::Enum(info) = value.get_represented_type_info()? else {
        return None;
    };
    let VariantInfo::Tuple(some) = info.variant("Some")? else {
        return None;
    };
    registry.get_type_data::<ReflectHandle>(some.field_at(0)?.type_id())
}

/// Reads an asset handle a component holds, as the key C# knows assets by.
///
/// A handle has no JSON form, since what it holds is a reference count rather than a value, so it
/// crosses as the same key `AssetServer.Load` returns. A handle already in the table comes back
/// with the key it has, so a field read every frame does not take a slot every frame.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_get_asset(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
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

            let handle = {
                let Ok(found) = world.get_entity(entity_from(entity)) else {
                    return fail(status::NO_ENTITY, "The entity does not exist.");
                };
                let Some(component) = reflect.reflect(found) else {
                    return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
                };
                let value = match at(component.as_partial_reflect(), &path) {
                    Ok(value) => value,
                    Err(code) => return code,
                };
                let data = match handle_data(&registry, value, &path) {
                    Ok(data) => data,
                    Err(code) => return code,
                };
                let held = value.try_as_reflect().map(|value| value.as_any());
                match held.and_then(|held| data.downcast_handle_untyped(held)) {
                    Some(handle) => handle,
                    None => {
                        return fail(status::INVALID_STATE, format!("'{path}' holds no handle."));
                    }
                }
            };

            crate::assets::key_for(world, handle)
        })
    })
}

/// Points an asset handle a component holds at the asset behind a key.
///
/// The handle is retyped to the field's own asset type, which refuses a key naming an asset of
/// another kind, as a mesh offered to a material's image would be.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_set_asset(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    key: i32,
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
            if let Err(code) = writable(world, reflect, &type_path) {
                return code;
            }
            let Some(handle) = crate::assets::clone_handle(world, key) else {
                return fail(status::INVALID_STATE, format!("The key {key} names no asset."));
            };

            let entity = entity_from(entity);
            let typed = {
                let Ok(found) = world.get_entity(entity) else {
                    return fail(status::NO_ENTITY, "The entity does not exist.");
                };
                let Some(component) = reflect.reflect(found) else {
                    return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
                };
                let value = match at(component.as_partial_reflect(), &path) {
                    Ok(value) => value,
                    Err(code) => return code,
                };
                // A handle, or an optional one, which a fog volume's density texture and many of
                // Bevy's images are, and which is set to hold the asset whatever it held.
                let (data, optional) = match handle_data(&registry, value, &path) {
                    Ok(data) => (data, false),
                    Err(code) => match optional_handle_data(&registry, value) {
                        Some(data) => (data, true),
                        None => return code,
                    },
                };
                if handle.type_id() != data.asset_type_id() {
                    return fail(
                        status::INVALID_STATE,
                        format!("The key {key} names another kind of asset than '{path}' holds."),
                    );
                }
                let typed = data.typed(handle);
                if optional {
                    let mut some = DynamicTuple::default();
                    some.insert_boxed(typed.into_partial_reflect());
                    Box::new(DynamicEnum::new("Some", DynamicVariant::Tuple(some))) as Box<dyn PartialReflect>
                } else {
                    typed.into_partial_reflect()
                }
            };

            apply(world, reflect, entity, &type_path, &path, typed.as_ref())
        })
    })
}

// -- Colors

/// Whether a type is one of the three Bevy holds a color in.
fn is_color(id: TypeId) -> bool {
    id == TypeId::of::<Color>() || id == TypeId::of::<LinearRgba>() || id == TypeId::of::<Srgba>()
}

/// A color field's value as linear RGBA, whatever space it holds, converted by Bevy.
fn linear_of(value: &dyn PartialReflect) -> Option<LinearRgba> {
    let value = value.try_as_reflect()?;
    if let Some(color) = value.downcast_ref::<Color>() {
        return Some(color.to_linear());
    }
    if let Some(linear) = value.downcast_ref::<LinearRgba>() {
        return Some(*linear);
    }
    value.downcast_ref::<Srgba>().map(|srgb| LinearRgba::from(*srgb))
}

/// A linear color as a value of the type `held` is, keeping the space a `Color` was in.
///
/// A light whose color was given in sRGB stays sRGB after the inspector changes it, rather than
/// coming back as a different variant than the code that made it wrote.
fn retyped(held: &dyn PartialReflect, linear: LinearRgba) -> Option<Box<dyn PartialReflect>> {
    let held = held.try_as_reflect()?;
    if let Some(color) = held.downcast_ref::<Color>() {
        let color = match color {
            Color::Srgba(_) => Color::Srgba(linear.into()),
            Color::LinearRgba(_) => Color::LinearRgba(linear),
            Color::Hsla(_) => Color::Hsla(linear.into()),
            Color::Hsva(_) => Color::Hsva(linear.into()),
            Color::Hwba(_) => Color::Hwba(linear.into()),
            Color::Laba(_) => Color::Laba(linear.into()),
            Color::Lcha(_) => Color::Lcha(linear.into()),
            Color::Oklaba(_) => Color::Oklaba(linear.into()),
            Color::Oklcha(_) => Color::Oklcha(linear.into()),
            Color::Xyza(_) => Color::Xyza(linear.into()),
        };
        return Some(Box::new(color));
    }
    if held.is::<LinearRgba>() {
        return Some(Box::new(linear));
    }
    held.is::<Srgba>().then(|| Box::new(Srgba::from(linear)) as Box<dyn PartialReflect>)
}

/// Reads a color field as linear red, green, blue and alpha.
///
/// A `Color` holds a color in any of ten spaces, and the managed side has one color type, so the
/// conversion is Bevy's own rather than a second copy of it there.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, `path` one or null, and `out` writable for four
/// floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_get_color(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    out: *mut f32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        if out.is_null() {
            return status::NULL_ARG;
        }
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();

        with_world(|world| {
            let registry = world.resource::<AppTypeRegistry>().clone();
            let registry = registry.read();
            let (_, reflect) = match component_of(&registry, &type_path) {
                Ok(found) => found,
                Err(code) => return code,
            };

            let Ok(found) = world.get_entity(entity_from(entity)) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            let Some(component) = reflect.reflect(found) else {
                return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
            };
            let value = match at(component.as_partial_reflect(), &path) {
                Ok(value) => value,
                Err(code) => return code,
            };
            let Some(linear) = linear_of(value) else {
                return fail(status::INVALID_STATE, format!("'{path}' is not a color."));
            };

            let parts = [linear.red, linear.green, linear.blue, linear.alpha];
            unsafe { core::ptr::copy_nonoverlapping(parts.as_ptr(), out, 4) };
            status::OK
        })
    })
}

/// Writes a color field from linear red, green, blue and alpha, in the space it already holds.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_set_color(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    red: f32,
    green: f32,
    blue: f32,
    alpha: f32,
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
            if let Err(code) = writable(world, reflect, &type_path) {
                return code;
            }

            let entity = entity_from(entity);
            let value = {
                let Ok(found) = world.get_entity(entity) else {
                    return fail(status::NO_ENTITY, "The entity does not exist.");
                };
                let Some(component) = reflect.reflect(found) else {
                    return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
                };
                let held = match at(component.as_partial_reflect(), &path) {
                    Ok(held) => held,
                    Err(code) => return code,
                };
                match retyped(held, LinearRgba::new(red, green, blue, alpha)) {
                    Some(value) => value,
                    None => return fail(status::INVALID_STATE, format!("'{path}' is not a color.")),
                }
            };

            apply(world, reflect, entity, &type_path, &path, value.as_ref())
        })
    })
}

/// A number field's value, whatever width of float it is.
fn float_of(value: &dyn PartialReflect) -> Option<f64> {
    if let Some(v) = value.try_downcast_ref::<f32>() {
        return Some(f64::from(*v));
    }
    value.try_downcast_ref::<f64>().copied()
}

/// A whole number or a flag's value, whatever width or signedness it is, with a flag as one or
/// zero. A `u64` past `i64`'s range wraps, which no field a game writes is near.
fn integer_of(value: &dyn PartialReflect) -> Option<i64> {
    macro_rules! widen {
        ($($t:ty),*) => {$(
            if let Some(v) = value.try_downcast_ref::<$t>() {
                return Some(*v as i64);
            }
        )*};
    }
    widen!(i8, i16, i32, i64, isize, u8, u16, u32, u64, usize);
    value.try_downcast_ref::<bool>().map(|on| i64::from(*on))
}

/// A float as a value of the same width as the one a field holds.
fn refloated(held: &dyn PartialReflect, number: f64) -> Option<Box<dyn PartialReflect>> {
    if held.try_downcast_ref::<f32>().is_some() {
        return Some(Box::new(number as f32));
    }
    held.try_downcast_ref::<f64>().map(|_| Box::new(number) as Box<dyn PartialReflect>)
}

/// A whole number as a value of the type a field holds, refused where it does not fit, so a
/// negative number is not written into an unsigned field as a large one.
fn reintegered(held: &dyn PartialReflect, whole: i64) -> Option<Box<dyn PartialReflect>> {
    macro_rules! narrow {
        ($($t:ty),*) => {$(
            if held.try_downcast_ref::<$t>().is_some() {
                return <$t>::try_from(whole).ok().map(|v| Box::new(v) as Box<dyn PartialReflect>);
            }
        )*};
    }
    narrow!(i8, i16, i32, i64, isize, u8, u16, u32, u64, usize);
    held.try_downcast_ref::<bool>().map(|_| Box::new(whole != 0) as Box<dyn PartialReflect>)
}

/// Reads one field of a component on an entity through `read`, which turns it into what the
/// caller is handed or reports that it is not that kind of value.
fn read_field<R>(
    entity: u64,
    type_path: &str,
    path: &str,
    what: &str,
    read: impl FnOnce(&dyn PartialReflect) -> Option<R>,
) -> Result<R, i32> {
    let mut found = None;
    let code = with_world(|world| {
        let registry = world.resource::<AppTypeRegistry>().clone();
        let registry = registry.read();
        let (_, reflect) = match component_of(&registry, type_path) {
            Ok(found) => found,
            Err(code) => return code,
        };
        let Ok(held) = world.get_entity(entity_from(entity)) else {
            return fail(status::NO_ENTITY, "The entity does not exist.");
        };
        let Some(component) = reflect.reflect(held) else {
            return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
        };
        let value = match at(component.as_partial_reflect(), path) {
            Ok(value) => value,
            Err(code) => return code,
        };
        match read(value) {
            Some(value) => {
                found = Some(value);
                status::OK
            }
            None => fail(status::INVALID_STATE, format!("'{path}' is not {what}.")),
        }
    });
    found.ok_or(code)
}

/// Writes one field of a component on an entity with the value `make` builds against what the
/// field holds, as [`bcs_reflect_set`] writes one read from JSON.
fn write_field(
    entity: u64,
    type_path: &str,
    path: &str,
    what: &str,
    make: impl FnOnce(&dyn PartialReflect) -> Option<Box<dyn PartialReflect>>,
) -> i32 {
    with_world(|world| {
        let registry = world.resource::<AppTypeRegistry>().clone();
        let registry = registry.read();
        let (_, reflect) = match component_of(&registry, type_path) {
            Ok(found) => found,
            Err(code) => return code,
        };
        if let Err(code) = writable(world, reflect, type_path) {
            return code;
        }

        let entity = entity_from(entity);
        let value = {
            let Ok(found) = world.get_entity(entity) else {
                return fail(status::NO_ENTITY, "The entity does not exist.");
            };
            let Some(component) = reflect.reflect(found) else {
                return fail(status::NOT_PRESENT, format!("The entity has no '{type_path}'."));
            };
            let held = match at(component.as_partial_reflect(), path) {
                Ok(held) => held,
                Err(code) => return code,
            };
            match make(held) {
                Some(value) => value,
                None => return fail(status::INVALID_STATE, format!("'{path}' cannot hold {what}.")),
            }
        };

        apply(world, reflect, entity, type_path, path, value.as_ref())
    })
}

/// Reads a float field, `f32` or `f64`, as a number rather than as JSON.
///
/// A typed wrapper reads its numbers this way, so a number never passes through text on its way
/// across.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, `path` one or null, and `out` writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_get_float(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    out: *mut f64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        if out.is_null() {
            return status::NULL_ARG;
        }
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();
        match read_field(entity, &type_path, &path, "a float", float_of) {
            Ok(value) => {
                unsafe { out.write(value) };
                status::OK
            }
            Err(code) => code,
        }
    })
}

/// Writes a float field at the width it holds.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_set_float(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    value: f64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();
        write_field(entity, &type_path, &path, "a float", |held| refloated(held, value))
    })
}

/// Reads a whole number or a flag field, of any width, as a number rather than as JSON.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, `path` one or null, and `out` writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_get_integer(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    out: *mut i64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        if out.is_null() {
            return status::NULL_ARG;
        }
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();
        match read_field(entity, &type_path, &path, "a whole number or a flag", integer_of) {
            Ok(value) => {
                unsafe { out.write(value) };
                status::OK
            }
            Err(code) => code,
        }
    })
}

/// Writes a whole number or a flag field at the type it holds, refusing a number that does not
/// fit it.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `path` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_set_integer(
    entity: u64,
    type_path: *const c_char,
    path: *const c_char,
    value: i64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        let path = unsafe { cstr_to_string(path) }.unwrap_or_default();
        write_field(entity, &type_path, &path, &format!("{value}"), |held| reintegered(held, value))
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

#[cfg(test)]
mod tests {
    use super::*;
    use std::ffi::CString;

    use bevy::app::App;
    use bevy::ecs::component::Component;
    use bevy::ecs::entity::Entity;
    use bevy::math::Vec3;
    use bevy::reflect::Reflect;

    use crate::state::loan_world;

    #[derive(Reflect, Default, Clone, Copy, PartialEq, Debug)]
    #[reflect(Default)]
    struct Inner {
        depth: i32,
    }

    #[derive(Reflect, Default, Clone, Copy, PartialEq, Debug)]
    #[reflect(Default)]
    enum Mode {
        #[default]
        Off,
        Fixed(f32),
        Ranged { low: f32, high: f32 },
    }

    #[derive(Component, Reflect, Default)]
    #[reflect(Component, Default)]
    struct Probe {
        speeds: Vec<f32>,
        speed: f32,
        at: Vec3,
        inner: Inner,
        mode: Mode,
    }

    #[derive(Component, Reflect, Default)]
    #[component(immutable)]
    #[reflect(Component, Default)]
    struct Frozen(f32);

    #[derive(Component, Reflect, Default)]
    #[reflect(Component, Default)]
    struct Tinted {
        tint: Color,
        glow: LinearRgba,
    }

    #[derive(Component, Reflect, Default)]
    #[reflect(Component, Default)]
    struct Textured {
        image: bevy::asset::Handle<bevy::image::Image>,
    }

    const PROBE: &str = "bevy_csharp::reflected::tests::Probe";
    const TEXTURED: &str = "bevy_csharp::reflected::tests::Textured";
    const TINTED: &str = "bevy_csharp::reflected::tests::Tinted";
    const FROZEN: &str = "bevy_csharp::reflected::tests::Frozen";

    fn probe_app() -> (App, Entity) {
        let mut app = App::new();
        app.register_type::<Probe>().register_type::<Frozen>();
        let entity = app
            .world_mut()
            .spawn((Probe { speed: 2.0, ..Default::default() }, Frozen(1.0)))
            .id();
        (app, entity)
    }

    fn c(text: &str) -> CString {
        CString::new(text).unwrap()
    }

    /// Calls a text-returning export the way C# does, probing for the length and asking again.
    fn text(call: impl Fn(*mut u8, i32) -> i32) -> Result<String, i32> {
        let needed = call(core::ptr::null_mut(), 0);
        if needed < 0 {
            return Err(needed);
        }
        let mut buffer = vec![0u8; needed as usize];
        assert_eq!(needed, call(buffer.as_mut_ptr(), needed));
        Ok(String::from_utf8(buffer).unwrap())
    }

    fn get(entity: Entity, type_path: &str, path: &str) -> Result<String, i32> {
        let (type_path, path) = (c(type_path), c(path));
        text(|out, capacity| unsafe {
            bcs_reflect_get(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), out, capacity)
        })
    }

    fn set(entity: Entity, type_path: &str, path: &str, json: &str) -> i32 {
        let (type_path, path) = (c(type_path), c(path));
        unsafe {
            let len = json.len() as i32;
            bcs_reflect_set(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), json.as_ptr(), len)
        }
    }

    fn last_error() -> String {
        text(|out, capacity| unsafe { bcs_reflect_error(out, capacity) }).unwrap()
    }

    #[test]
    fn the_registry_describes_a_component_and_what_its_fields_hold() {
        let (mut app, _) = probe_app();
        let dump = loan_world(app.world_mut(), || {
            text(|out, capacity| unsafe { bcs_reflect_types(out, capacity) })
        });
        let dump: Value = serde_json::from_str(&dump.unwrap()).unwrap();
        let component = |path: &str| {
            dump["components"].as_array().unwrap().iter().find(|c| c["path"] == path).cloned()
        };

        let probe = component(PROBE).expect("the probe is listed");
        assert_eq!(true, probe["default"]);
        assert_eq!(true, probe["mutable"]);

        let frozen = component(FROZEN).expect("the immutable one is listed");
        assert_eq!(false, frozen["mutable"]);

        // The nested struct and the enum are described too, so the managed side can flatten them
        // without asking again.
        let types = &dump["types"];
        assert_eq!("struct", types[PROBE]["kind"]);
        let speeds = &types["alloc::vec::Vec<f32>"];
        assert_eq!("list", speeds["kind"]);
        assert_eq!("f32", speeds["item"]);

        let mode = &types["bevy_csharp::reflected::tests::Mode"];
        assert_eq!("enum", mode["kind"]);
        assert_eq!(3, mode["variants"].as_array().unwrap().len());
        assert_eq!("struct", types["bevy_csharp::reflected::tests::Inner"]["kind"]);
    }

    #[test]
    fn a_field_is_read_and_written_by_its_path() {
        let (mut app, entity) = probe_app();
        loan_world(app.world_mut(), || {
            assert_eq!(Ok("2.0".to_owned()), get(entity, PROBE, "speed"));
            assert_eq!(status::OK, set(entity, PROBE, "speed", "4.5"));
            assert_eq!(status::OK, set(entity, PROBE, "at", "[1.0, 2.0, 3.0]"));
            assert_eq!(status::OK, set(entity, PROBE, "inner.depth", "7"));
        });

        let probe = app.world().get::<Probe>(entity).unwrap();
        assert_eq!(4.5, probe.speed);
        assert_eq!(Vec3::new(1.0, 2.0, 3.0), probe.at);
        assert_eq!(7, probe.inner.depth);
    }

    #[test]
    fn a_variant_is_chosen_by_name_with_its_fields_at_their_defaults() {
        let (mut app, entity) = probe_app();
        loan_world(app.world_mut(), || {
            let (type_path, path, variant) = (c(PROBE), c("mode"), c("Ranged"));
            let bits = entity.to_bits();
            let code = unsafe {
                bcs_reflect_set_variant(bits, type_path.as_ptr(), path.as_ptr(), variant.as_ptr())
            };
            assert_eq!(status::OK, code);

            let named = text(|out, capacity| unsafe {
                bcs_reflect_variant(bits, type_path.as_ptr(), path.as_ptr(), out, capacity)
            });
            assert_eq!(Ok("Ranged".to_owned()), named);

            // A field of the active variant is reached by name, without naming the variant.
            assert_eq!(status::OK, set(entity, PROBE, "mode.high", "9.0"));
        });

        let mode = app.world().get::<Probe>(entity).unwrap().mode;
        assert_eq!(Mode::Ranged { low: 0.0, high: 9.0 }, mode);
    }

    #[test]
    fn a_component_is_inserted_at_its_default_and_removed() {
        let (mut app, _) = probe_app();
        let entity = app.world_mut().spawn_empty().id();
        loan_world(app.world_mut(), || {
            let type_path = c(PROBE);
            assert_eq!(status::OK, unsafe {
                bcs_reflect_insert(entity.to_bits(), type_path.as_ptr(), core::ptr::null(), 0)
            });
            assert_eq!(Ok("0.0".to_owned()), get(entity, PROBE, "speed"));

            let remove = || unsafe { bcs_reflect_remove(entity.to_bits(), type_path.as_ptr()) };
            assert_eq!(status::OK, remove());
            assert_eq!(status::NOT_PRESENT, remove(), "a second removal finds nothing");
        });
        assert!(app.world().get::<Probe>(entity).is_none());
    }

    #[test]
    fn a_bad_path_or_value_is_refused_with_a_reason() {
        let (mut app, entity) = probe_app();
        loan_world(app.world_mut(), || {
            assert_eq!(status::INVALID_STATE, set(entity, PROBE, "nowhere", "1.0"));
            assert!(last_error().contains("nowhere"), "{}", last_error());

            assert_eq!(status::INVALID_STATE, set(entity, PROBE, "speed", "\"fast\""));
            assert!(last_error().contains("fast"), "{}", last_error());

            assert_eq!(Err(status::NO_COMPONENT), get(entity, "no::such::Type", ""));
        });
        assert_eq!(2.0, app.world().get::<Probe>(entity).unwrap().speed);
    }

    #[test]
    fn a_handle_crosses_as_the_key_the_asset_table_gives_it() {
        use bevy::asset::{AssetApp, Assets};
        use bevy::image::Image;
        use bevy::mesh::{Mesh, MeshBuilder, Meshable};

        let mut app = App::new();
        app.add_plugins(bevy::app::TaskPoolPlugin::default());
        app.add_plugins(bevy::asset::AssetPlugin::default());
        app.init_asset::<Image>().init_asset::<Mesh>();
        app.register_type::<Textured>();

        let first = app.world_mut().resource_mut::<Assets<Image>>().add(Image::default());
        let second = app.world_mut().resource_mut::<Assets<Image>>().add(Image::default());
        let mesh = bevy::math::primitives::Cuboid::new(1.0, 1.0, 1.0).mesh().build();
        let mesh = app.world_mut().resource_mut::<Assets<Mesh>>().add(mesh);
        let entity = app.world_mut().spawn(Textured { image: first.clone() }).id();

        loan_world(app.world_mut(), || {
            let (type_path, path) = (c(TEXTURED), c(".image"));
            let bits = entity.to_bits();
            let read = || unsafe { bcs_reflect_get_asset(bits, type_path.as_ptr(), path.as_ptr()) };
            let write = |key| unsafe {
                bcs_reflect_set_asset(bits, type_path.as_ptr(), path.as_ptr(), key)
            };

            // Read every frame by an inspector, so the second read has to find the first's slot.
            let key = read();
            assert!(key >= 0, "the read failed with {key}");
            assert_eq!(key, read(), "a second read took a slot of its own");

            let key_of = |handle: bevy::asset::UntypedHandle| {
                crate::state::with_world(|world| crate::assets::key_for(world, handle))
            };
            assert_eq!(status::OK, write(key_of(second.clone().untyped())));

            // A mesh offered to an image field is refused rather than retyped into nonsense.
            assert_eq!(status::INVALID_STATE, write(key_of(mesh.clone().untyped())));
            assert!(last_error().contains("another kind"), "{}", last_error());
        });

        assert_eq!(second.id(), app.world().get::<Textured>(entity).unwrap().image.id());
    }

    #[test]
    fn a_color_crosses_as_linear_and_keeps_the_space_it_was_held_in() {
        let mut app = App::new();
        app.register_type::<Tinted>();
        let tinted = Tinted { tint: Color::srgb(1.0, 0.5, 0.0), glow: LinearRgba::BLACK };
        let entity = app.world_mut().spawn(tinted).id();

        loan_world(app.world_mut(), || {
            let (type_path, tint, glow) = (c(TINTED), c(".tint"), c(".glow"));
            let bits = entity.to_bits();

            let mut read = [0f32; 4];
            let (owner, into) = (type_path.as_ptr(), read.as_mut_ptr());
            let code = unsafe { bcs_reflect_get_color(bits, owner, tint.as_ptr(), into) };
            assert_eq!(status::OK, code);

            // sRGB one half is about a fifth in linear, which says the conversion ran.
            assert!((read[1] - 0.214).abs() < 0.01, "green read as {}", read[1]);
            assert_eq!(1.0, read[0]);

            let write = |path: &CString| unsafe {
                bcs_reflect_set_color(bits, type_path.as_ptr(), path.as_ptr(), 0.0, 1.0, 0.0, 1.0)
            };
            assert_eq!(status::OK, write(&tint));
            assert_eq!(status::OK, write(&glow));
        });

        let tinted = app.world().get::<Tinted>(entity).unwrap();
        assert!(matches!(tinted.tint, Color::Srgba(_)), "the color changed space");
        assert_eq!(LinearRgba::GREEN, tinted.glow);
        assert_eq!(LinearRgba::GREEN, tinted.tint.to_linear());
    }

    #[test]
    fn an_immutable_component_is_refused_rather_than_written() {
        // `reflect_mut` panics on an immutable component, so the export has to ask first. The
        // guard would turn the panic into a status, but without the reason.
        let (mut app, entity) = probe_app();
        loan_world(app.world_mut(), || {
            assert_eq!(status::UNSUPPORTED, set(entity, FROZEN, "0", "3.0"));
            assert!(last_error().contains("immutable"));
        });
        assert_eq!(1.0, app.world().get::<Frozen>(entity).unwrap().0);
    }
}

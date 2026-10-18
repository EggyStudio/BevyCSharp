//! Where the compiler put each field of a reflected type, read from a value of it, which the
//! managed side holds its byte mirrors to.
//!
//! A mirror reads and writes a component's bytes in place, so it has to agree with Rust's layout
//! byte for byte, and Rust promises none for its default representation: it reorders fields, as
//! `Transform`'s rotation is moved ahead of its translation, and a Bevy release may move them
//! again. `offset_of!` would say where each field is, but only for a field the bridge can name,
//! which a private one and one inside an enum's variant are not. A reflected field is a reference
//! into the value it belongs to, so its address less the value's is the offset the compiler
//! chose, private or not, read from a value made at its default as an insert from nothing makes
//! one.

use super::*;
use bevy::reflect::{Reflect, ReflectFromReflect};

/// A value of the registered type, made at its default, and refused where it has none to measure.
///
/// The value has to be of the type itself, since a dynamic stand-in, as the struct
/// [`default_of`] builds from its fields' defaults, lies in memory as a list of boxes rather than
/// as the type does, so one built that way is made into the type through `FromReflect`.
fn made(registry: &TypeRegistry, registration: &TypeRegistration, path: &str) -> Result<Box<dyn Reflect>, i32> {
    if let Some(default) = registration.data::<ReflectDefault>() {
        return Ok(default.default());
    }

    let built = default_of(registry, registration.type_id(), path)?;
    if let Some(from) = registration.data::<ReflectFromReflect>()
        && let Some(value) = from.from_reflect(built.as_ref())
    {
        return Ok(value);
    }
    built
        .try_into_reflect()
        .map_err(|_| fail(status::UNSUPPORTED, format!("'{path}' has no default to measure a value of.")))
}

/// Adds a row for `value` at `path` and for each value inside it, its offset taken from `base`.
///
/// A struct, a tuple struct and a tuple are walked into, an enum none of whose variants holds a
/// value is one row, and anything else is one row of its kind, an enum whose variants hold values
/// among them, since where a variant's value lies depends on the variant held.
fn walk(value: &dyn PartialReflect, base: usize, size: usize, path: &str, rows: &mut Vec<Value>) -> Result<(), i32> {
    let at = value as *const dyn PartialReflect as *const () as usize;
    let length = core::mem::size_of_val(value);
    if at < base || at + length > base + size {
        return Err(fail(
            status::UNSUPPORTED,
            format!("'{path}' is reflected from outside the value it belongs to, so its place cannot be read."),
        ));
    }

    let kind = kind_of(value);
    if !path.is_empty() {
        let align = core::mem::align_of_val(value);
        rows.push(json!({ "path": path, "offset": at - base, "size": length, "align": align, "kind": kind }));
    }

    match value.reflect_ref() {
        ReflectRef::Struct(fields) => {
            for i in 0..fields.field_len() {
                let name = fields.name_at(i).unwrap_or_default();
                walk(fields.field_at(i).unwrap(), base, size, &format!("{path}.{name}"), rows)?;
            }
        }
        ReflectRef::TupleStruct(fields) => {
            for i in 0..fields.field_len() {
                walk(fields.field(i).unwrap(), base, size, &format!("{path}.{i}"), rows)?;
            }
        }
        ReflectRef::Tuple(fields) => {
            for i in 0..fields.field_len() {
                walk(fields.field(i).unwrap(), base, size, &format!("{path}.{i}"), rows)?;
            }
        }
        _ => {}
    }
    Ok(())
}

/// What a value is to reflection, which says whether a mirror could read its bytes.
///
/// A struct, a tuple struct and a tuple are walked into, an enum none of whose variants holds a
/// value is read whole, and an enum whose variants hold values is a `variant`, since where a
/// variant's values lie depends on the variant held.
fn kind_of(value: &dyn PartialReflect) -> &'static str {
    match value.reflect_ref() {
        ReflectRef::Struct(_) => "struct",
        ReflectRef::TupleStruct(_) => "tuple_struct",
        ReflectRef::Tuple(_) => "tuple",
        ReflectRef::Enum(held) => match held.get_represented_enum_info() {
            Some(info) if info.iter().all(|variant| matches!(variant, VariantInfo::Unit(_))) => "enum",
            _ => "variant",
        },
        ReflectRef::List(_) => "list",
        ReflectRef::Array(_) => "array",
        ReflectRef::Map(_) => "map",
        ReflectRef::Set(_) => "set",
        ReflectRef::Opaque(_) => "value",
        #[allow(unreachable_patterns)]
        _ => "other",
    }
}

/// The layout of the type registered at `path`, as the JSON [`bcs_reflect_layout`] answers.
fn layout_json(world: &World, path: &str) -> Result<String, i32> {
    let registry = world.resource::<AppTypeRegistry>().clone();
    let registry = registry.read();
    let Some(registration) = registry.get_with_type_path(path) else {
        return Err(fail(
            status::NO_COMPONENT,
            format!("No reflected type is registered as '{path}'. Names are full type paths."),
        ));
    };

    let value = made(&registry, registration, path)?;
    let value = value.as_partial_reflect();

    // The size and alignment a component is stored with, which the world registered for it, and
    // for a type that is not a component the value's own, which are the same for one that is.
    let (size, align) = match world.components().get_id(registration.type_id()).and_then(|id| world.components().get_info(id)) {
        Some(info) => (info.layout().size(), info.layout().align()),
        None => (core::mem::size_of_val(value), core::mem::align_of_val(value)),
    };

    let base = value as *const dyn PartialReflect as *const () as usize;
    let mut rows = Vec::new();
    walk(value, base, size, "", &mut rows)?;
    Ok(json!({ "size": size, "align": align, "kind": kind_of(value), "fields": rows }).to_string())
}

/// Describes where the compiler put each field of a reflected type, as JSON.
///
/// The type's size, alignment and kind, then a row for each field reflection reaches, walked into
/// structs, tuple structs and tuples, each with its reflect path, its offset from the value's
/// start, its size, its alignment and its kind. A type with no default to make a value of is refused, and so is
/// one a field of which is reflected from somewhere other than the value, which no derived type
/// is. Bytes no row covers are padding or a field reflection leaves out, which the managed side
/// tells apart by the alignment of what follows them.
///
/// # Safety
/// `type_path` must be a NUL-terminated string, and `out` writable for `capacity` bytes, or null
/// when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_layout(type_path: *const c_char, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(type_path) = (unsafe { cstr_to_string(type_path) }) else {
            return status::NULL_ARG;
        };
        with_world(|world| match layout_json(world, &type_path) {
            Ok(text) => unsafe { write_text(&text, out, capacity) },
            Err(code) => code,
        })
    })
}

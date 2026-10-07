//! Asset handles, colors and numbers, each read and written through an export of its own, since
//! JSON is no form for a handle and a color or a number has a cheaper one.

use super::*;

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
pub(super) fn is_color(id: TypeId) -> bool {
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
pub(super) fn retyped(held: &dyn PartialReflect, linear: LinearRgba) -> Option<Box<dyn PartialReflect>> {
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

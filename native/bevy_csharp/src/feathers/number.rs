//! A Feathers number input's value, its limits and its units, Bevy components its reflection does
//! not reach, the value and the limits reflecting without saying they are components and the units
//! holding a string the description leaves out, so each has a call of its own.

use crate::interop::status;

/// Writes a number input's value, of `kind` zero `f32`, one `f64`, two `i32` and three `i64`,
/// from `value` for the first two and `whole` for the last two. The kind is the one its changes
/// are then reported in.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_feathers_number_set(entity: u64, kind: i32, value: f64, whole: i64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, kind, value, whole);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::NumberInputValue;

            let number = match kind {
                0 => NumberInputValue::F32(value as f32),
                1 => NumberInputValue::F64(value),
                2 => NumberInputValue::I32(whole as i32),
                3 => NumberInputValue::I64(whole),
                _ => return status::NULL_ARG,
            };
            crate::state::with_world(|world| {
                let Ok(mut input) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                input.insert(number);
                status::OK
            })
        }
    })
}

/// Reads a number input's value into `kind`, `value` and `whole`, as
/// [`bcs_feathers_number_set`] writes them, or answers [`status::NOT_PRESENT`] where it holds none.
///
/// # Safety
/// `kind`, `value` and `whole` must each be valid for one write.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_number_get(entity: u64, kind: *mut i32, value: *mut f64, whole: *mut i64) -> i32 {
    crate::interop::guard(|| {
        if kind.is_null() || value.is_null() || whole.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::NumberInputValue;

            crate::state::with_world(|world| {
                let Ok(input) = world.get_entity(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(number) = input.get::<NumberInputValue>() else {
                    return status::NOT_PRESENT;
                };
                let (read, as_float, as_whole) = match *number {
                    NumberInputValue::F32(v) => (0, v as f64, 0),
                    NumberInputValue::F64(v) => (1, v, 0),
                    NumberInputValue::I32(v) => (2, v as f64, v as i64),
                    NumberInputValue::I64(v) => (3, v as f64, v),
                };
                unsafe {
                    *kind = read;
                    *value = as_float;
                    *whole = as_whole;
                }
                status::OK
            })
        }
    })
}

/// Bounds a number input's value, Bevy's `HardLimit` where `hard` is non-zero, which typing and
/// dragging cannot pass, and its `SoftLimit` otherwise, which a drag stays within and typing can
/// pass. The range is of `kind` as [`bcs_feathers_number_set`] numbers it, from `min` to `max` for
/// the floats and `min_whole` to `max_whole` for the integers.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_feathers_number_limit(
    entity: u64,
    hard: i32,
    kind: i32,
    min: f64,
    max: f64,
    min_whole: i64,
    max_whole: i64,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, hard, kind, min, max, min_whole, max_whole);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::{HardLimit, NumberInputRange, SoftLimit};

            let range = match kind {
                0 => NumberInputRange::F32(min as f32..=max as f32),
                1 => NumberInputRange::F64(min..=max),
                2 => NumberInputRange::I32(min_whole as i32..=max_whole as i32),
                3 => NumberInputRange::I64(min_whole..=max_whole),
                _ => return status::NULL_ARG,
            };
            crate::state::with_world(|world| {
                let Ok(mut input) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                if hard != 0 {
                    input.insert(HardLimit(range));
                } else {
                    input.insert(SoftLimit(range));
                }
                status::OK
            })
        }
    })
}

/// Has a number input show its value in units, Bevy's `NumberInputUnits` by the id its registry
/// knows the units under, such as `length_meters`, `time_seconds` or `angle_degrees`, where typing
/// a value takes it in any unit of that dimension and the value is held in the first.
///
/// # Safety
/// `units` must be valid for `len` bytes of UTF-8.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_number_units(entity: u64, units: *const u8, len: i32) -> i32 {
    crate::interop::guard(|| {
        if units.is_null() || len <= 0 {
            return status::NULL_ARG;
        }
        let Ok(units) = core::str::from_utf8(unsafe { core::slice::from_raw_parts(units, len as usize) }) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, units);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::NumberInputUnits;

            crate::state::with_world(|world| {
                let Ok(mut input) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                input.insert(NumberInputUnits(units.into()));
                status::OK
            })
        }
    })
}

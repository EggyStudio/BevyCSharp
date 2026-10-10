//! Pipeline constants, a Slang `[SpecializationConstant]` and a WGSL `override`, which a material
//! sets by name as it sets any value and which are compiled into its pipelines rather than bound.
//!
//! Bevy's own materials set them in their `specialize`, from the material's bind group data, which
//! keys the pipeline. Here the values a material sets for its program's constants go into
//! the material's pipeline key, so each set of values gets pipelines of its own, and the hook that
//! specializes a pipeline hands each stage the constants that stage declares.

#![cfg(feature = "render")]

use std::sync::Arc;

use bevy::render::render_resource::RenderPipelineDescriptor;

use super::programs::PipelineProgram;
use super::reflect::{Layout, Scalar};
use super::values::{Value, Values};

/// The values a material sets for its program's constants, as the bits of an `f64` by name, in
/// the order of the names, ready for its pipeline key.
///
/// A constant the material leaves unset keeps the value the shader gives it, so it is not listed.
pub fn of(layout: &Layout, values: &Values) -> Arc<[(String, u64)]> {
    layout
        .constants
        .keys()
        .filter_map(|name| {
            let Some(Value::Numbers { scalar, data, .. }) = values.entries.get(name) else {
                return None;
            };
            let bytes: [u8; 4] = data.get(..4)?.try_into().ok()?;

            let number = match scalar {
                Scalar::F32 => f64::from(f32::from_ne_bytes(bytes)),
                Scalar::I32 => f64::from(i32::from_ne_bytes(bytes)),
                Scalar::U32 => f64::from(u32::from_ne_bytes(bytes)),
                Scalar::Bool => f64::from(u8::from(u32::from_ne_bytes(bytes) != 0)),
            };

            Some((name.clone(), number.to_bits()))
        })
        .collect()
}

/// Hands each stage of a pipeline being made the constants it declares, from `constants`.
///
/// A stage is known by its shader, which is the program's own for each stage it has, and a stage
/// the program does not have, Bevy's own vertex shader among them, declares none.
pub fn apply(descriptor: &mut RenderPipelineDescriptor, program: &PipelineProgram, constants: &[(String, u64)]) {
    if constants.is_empty() {
        return;
    }

    for stage in program.stages.iter().flatten() {
        let wanted = stage.constants.iter().filter_map(|(name, key)| {
            constants
                .iter()
                .find(|(set, _)| set == name)
                .map(|(_, bits)| (key.clone().into(), f64::from_bits(*bits)))
        });

        if descriptor.vertex.shader == stage.shader {
            descriptor.vertex.constants.extend(wanted);
        } else if let Some(fragment) = descriptor.fragment.as_mut()
            && fragment.shader == stage.shader
        {
            fragment.constants.extend(wanted);
        }
    }
}

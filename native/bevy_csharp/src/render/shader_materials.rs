//! Materials a program draws, on a mesh or on a 2D mesh: made, changed and put on an entity.

use crate::interop::status;
#[cfg(feature = "render")]
use super::shader_targets::{Target, TARGET_MATERIAL, read_target, with_target};

#[cfg(feature = "render")]
fn alpha_mode(alpha: i32, cutoff: f32) -> bevy::material::AlphaMode {
    use bevy::material::AlphaMode;

    match alpha {
        1 => AlphaMode::Mask(cutoff),
        2 => AlphaMode::Blend,
        3 => AlphaMode::Add,
        4 => AlphaMode::Multiply,
        5 => AlphaMode::Premultiplied,
        _ => AlphaMode::Opaque,
    }
}

/// Whether `program` names a program the running app made.
#[cfg(feature = "render")]
pub(super) fn program_exists(program: i32) -> bool {
    program >= 0 && super::programs::lookup(program as u32).is_some()
}

/// Makes a material drawn by a program, and answers its asset key.
///
/// `alpha` is `0` opaque, `1` masked at `cutoff`, `2` blended, `3` added, `4` multiplied and `5`
/// premultiplied. `cull` is `0` back faces, `1` front faces, `2` neither.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_create(
    program: i32,
    alpha: i32,
    cutoff: f32,
    cull: i32,
    depth_bias: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (program, alpha, cutoff, cull, depth_bias);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::BcsMaterial;
            use bevy::render::render_resource::Face;

            if !program_exists(program) {
                return status::INVALID_STATE;
            }

            let mut material = BcsMaterial::new(program as u32);
            material.alpha = alpha_mode(alpha, cutoff);
            material.depth_bias = depth_bias;
            material.cull = match cull {
                1 => Some(Face::Front),
                2 => None,
                _ => Some(Face::Back),
            };

            crate::state::with_world(|world| {
                let Some(mut assets) =
                    world.get_resource_mut::<bevy::asset::Assets<BcsMaterial>>()
                else {
                    return status::UNSUPPORTED;
                };

                let handle = assets.add(material);
                crate::assets::insert_handle(world, handle.untyped())
            })
        }
    })
}

/// Makes a material a program draws a 2D mesh with, and answers its asset key.
///
/// `alpha` is `0` opaque, `1` masked at `cutoff` and `2` blended, which is every way Bevy's 2D
/// pipeline draws.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_2d_create(program: i32, alpha: i32, cutoff: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (program, alpha, cutoff);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::BcsMaterial;
            use super::material2d::BcsMaterial2d;

            if !program_exists(program) {
                return status::INVALID_STATE;
            }
            if !(0..=2).contains(&alpha) {
                return status::NULL_ARG;
            }

            let mut material = BcsMaterial::new(program as u32);
            material.alpha = alpha_mode(alpha, cutoff);
            material.cull = None;
            material.flat = true;

            crate::state::with_world(|world| {
                let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<BcsMaterial2d>>() else {
                    return status::UNSUPPORTED;
                };

                let handle = assets.add(BcsMaterial2d(material));
                crate::assets::insert_handle(world, handle.untyped())
            })
        }
    })
}

/// Changes how a material is drawn: its program, alpha, faces culled and depth bias.
///
/// A negative `program` leaves the program as it is, and a negative `alpha` leaves the rest, so
/// changing the program alone needs nothing read back first.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_configure(
    material: i32,
    program: i32,
    alpha: i32,
    cutoff: f32,
    cull: i32,
    depth_bias: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, program, alpha, cutoff, cull, depth_bias);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::render::render_resource::Face;

            if program >= 0 && !program_exists(program) {
                return status::INVALID_STATE;
            }

            with_target(TARGET_MATERIAL, material as i64, |target| {
                let Target::Material(current) = target else {
                    return status::NO_COMPONENT;
                };

                if program >= 0 {
                    current.program = program as u32;
                }

                if alpha < 0 {
                    return status::OK;
                }

                current.alpha = alpha_mode(alpha, cutoff);
                current.depth_bias = depth_bias;
                current.cull = match cull {
                    1 => Some(Face::Front),
                    2 => None,
                    _ => Some(Face::Back),
                };

                status::OK
            })
        }
    })
}

/// Reports which program draws a material, or an entity's material with `kind` two.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_target_program(kind: i32, id: i64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mut program = status::NO_COMPONENT;

            let answer = read_target(kind, id, |target| {
                program = match target {
                    Target::Material(material) => material.program as i32,
                    Target::Instance(instance) => instance.program as i32,
                };
            });

            if answer == status::OK { program } else { answer }
        }
    })
}

/// Gives an entity a 2D shader material, if the handle names one.
#[cfg(feature = "render")]
pub fn attach_2d(
    entity: &mut bevy::ecs::world::EntityWorldMut,
    untyped: &bevy::asset::UntypedHandle,
) -> bool {
    match untyped.clone().try_typed::<super::material2d::BcsMaterial2d>() {
        Ok(handle) => {
            entity.insert(super::material2d::BcsMeshMaterial2d(handle));
            true
        }
        Err(_) => false,
    }
}

/// Gives an entity a shader material, if the handle names one.
#[cfg(feature = "render")]
pub fn attach(
    entity: &mut bevy::ecs::world::EntityWorldMut,
    untyped: &bevy::asset::UntypedHandle,
) -> bool {
    match untyped
        .clone()
        .try_typed::<super::material::BcsMaterial>()
    {
        Ok(handle) => {
            entity.insert(super::material::BcsMaterial3d(handle));
            true
        }
        Err(_) => false,
    }
}

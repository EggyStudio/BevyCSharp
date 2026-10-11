//! Materials a program draws, on a mesh, a 2D mesh or an interface node: made, changed and put on
//! an entity.

use crate::interop::status;
#[cfg(feature = "render")]
use super::shader_targets::{Target, TARGET_MATERIAL, read_target, with_target};
#[cfg(feature = "render")]
use bevy::asset::{Asset, AssetId, Assets};
#[cfg(feature = "render")]
use bevy::ecs::system::ResMut;

#[cfg(feature = "render")]
fn alpha_mode(alpha: i32, cutoff: f32) -> bevy::material::AlphaMode {
    use bevy::material::AlphaMode;

    match alpha {
        1 => AlphaMode::Mask(cutoff),
        2 => AlphaMode::Blend,
        3 => AlphaMode::Add,
        4 => AlphaMode::Multiply,
        5 => AlphaMode::Premultiplied,
        6 => AlphaMode::AlphaToCoverage,
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
/// `alpha` is `0` opaque, `1` masked at `cutoff`, `2` blended, `3` added, `4` multiplied, `5`
/// premultiplied and `6` alpha to coverage. `cull` is `0` back faces, `1` front faces, `2` neither.
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
            material.alpha_given = true;
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

/// Makes a material a program draws a 2D mesh or a sprite with, and answers its asset key.
///
/// `alpha` is `0` opaque, `1` masked at `cutoff` and `2` blended, which is every way Bevy's 2D
/// pipeline draws, or `-1` for none, which draws a 2D mesh opaque and a sprite as the sprite says.
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
            if !(-1..=2).contains(&alpha) {
                return status::NULL_ARG;
            }

            let mut material = BcsMaterial::new(program as u32);
            material.alpha = alpha_mode(alpha, cutoff);
            material.alpha_given = alpha >= 0;
            material.cull = None;
            material.surface = super::material::Surface::Flat;

            crate::state::with_world(|world| {
                let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<BcsMaterial2d>>() else {
                    return status::UNSUPPORTED;
                };

                let handle = assets.add(BcsMaterial2d { material, sprite: None });
                crate::assets::insert_handle(world, handle.untyped())
            })
        }
    })
}

/// Makes a material a program draws an interface node with, and answers its asset key.
///
/// Blended as everything an interface draws is, so it takes no alpha, and answers
/// [`status::UNSUPPORTED`] where the app draws no interface.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_ui_create(program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = program;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::{BcsMaterial, Surface};
            use super::ui_material::BcsUiMaterial;

            if !program_exists(program) {
                return status::INVALID_STATE;
            }

            let mut material = BcsMaterial::new(program as u32);
            material.alpha = bevy::material::AlphaMode::Blend;
            material.cull = None;
            material.surface = Surface::Node;

            crate::state::with_world(|world| {
                let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<BcsUiMaterial>>() else {
                    return status::UNSUPPORTED;
                };

                let handle = assets.add(BcsUiMaterial { material });
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
                current.alpha_given = true;
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
///
/// A sprite takes it as Bevy's `SpriteMaterial`, which [`super::sprite_material`] draws the sprite
/// with, and any other entity as the material of its 2D mesh.
#[cfg(feature = "render")]
pub fn attach_2d(
    entity: &mut bevy::ecs::world::EntityWorldMut,
    untyped: &bevy::asset::UntypedHandle,
) -> bool {
    use super::material2d::{BcsMaterial2d, BcsMeshMaterial2d};

    match untyped.clone().try_typed::<BcsMaterial2d>() {
        Ok(handle) if entity.contains::<bevy::sprite::Sprite>() => {
            entity.insert(bevy::sprite_render::SpriteMaterial(handle));
            true
        }
        Ok(handle) => {
            entity.insert(BcsMeshMaterial2d(handle));
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

/// Marks every material of a program whose shaders were replaced as changed, so each builds its
/// bind group again in the new layout, keeping its values by name, 2D and interface materials among
/// them.
#[cfg(feature = "render")]
pub(super) fn refresh_materials_of_changed_programs(
    mut programs: ResMut<super::programs::ShaderPrograms>,
    mut materials: ResMut<Assets<super::material::BcsMaterial>>,
    mut flat: ResMut<Assets<super::material2d::BcsMaterial2d>>,
    nodes: Option<ResMut<Assets<super::ui_material::BcsUiMaterial>>>,
) {
    let changed = programs.take_changed();

    if changed.is_empty() {
        return;
    }

    touch(&mut materials, |material| material.program, &changed);
    touch(&mut flat, |flat| flat.material.program, &changed);

    // Only where the app draws an interface, which is where the type was added.
    if let Some(mut nodes) = nodes {
        touch(&mut nodes, |node| node.material.program, &changed);
    }
}

/// Marks every material in `assets` whose program, as `program` reads it, is one of `changed`.
#[cfg(feature = "render")]
fn touch<A: Asset>(assets: &mut Assets<A>, program: fn(&A) -> u32, changed: &[u32]) {
    let stale: Vec<AssetId<A>> = assets
        .iter()
        .filter(|(_, material)| changed.contains(&program(material)))
        .map(|(id, _)| id)
        .collect();

    for id in stale {
        // `into_inner` rather than only asking for it mutably, because the guard `get_mut` hands
        // back reports a change only when it is written through, and nothing here writes.
        if let Some(material) = assets.get_mut(id) {
            material.into_inner();
        }
    }
}

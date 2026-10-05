//! Bevy's material for 2D meshes, `ColorMaterial`, made and changed from C#.
//!
//! A 2D mesh is drawn by a 2D camera with a `Mesh2d` and a `MeshMaterial2d`, and the material most
//! of Bevy's 2D examples draw with is a color, an optional image multiplied by it, how its alpha
//! is read, and a transform of its texture coordinates, which a picture repeated over a mesh uses.
//! It is made here as the standard material is in [`super::assets`], an asset in the handle table
//! that the managed side holds a key to.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// A `ColorMaterial` as C# describes it.
///
/// `color` is linear RGBA, `texture` an image's key or zero for none, `alpha_mode` `0` opaque, `1`
/// masked at `alpha_cutoff`, `2` blended, and `uv` the texture coordinates' affine transform as its
/// two matrix columns and its translation.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsColorMaterial {
    pub color: [f32; 4],
    pub texture: i32,
    pub alpha_mode: i32,
    pub alpha_cutoff: f32,
    pub uv: [f32; 6],
}

/// The material a description makes, or the status that refuses it.
#[cfg(feature = "render")]
fn color_material(
    world: &mut bevy::ecs::world::World,
    config: &BcsColorMaterial,
) -> Result<bevy::sprite_render::ColorMaterial, i32> {
    use bevy::color::{Color, LinearRgba};
    use bevy::math::Affine2;
    use bevy::sprite_render::{AlphaMode2d, ColorMaterial};

    let [r, g, b, a] = config.color;
    let alpha_mode = match config.alpha_mode {
        0 => AlphaMode2d::Opaque,
        1 => AlphaMode2d::Mask(config.alpha_cutoff),
        2 => AlphaMode2d::Blend,
        _ => return Err(status::NULL_ARG),
    };

    Ok(ColorMaterial {
        color: Color::LinearRgba(LinearRgba::new(r, g, b, a)),
        alpha_mode,
        uv_transform: Affine2::from_cols_array(&config.uv),
        texture: super::image_handle(world, config.texture)?,
    })
}

/// Makes a `ColorMaterial` and returns its key, or a negative status.
///
/// # Safety
/// `config` must point at one readable [`BcsColorMaterial`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_color_material_create(config: *const BcsColorMaterial) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::sprite_render::ColorMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let material = match color_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };
                let Some(mut materials) = world.get_resource_mut::<Assets<ColorMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(material).untyped();
                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// Writes a description over an existing `ColorMaterial`, so every mesh drawn with it changes.
///
/// Reports [`status::INVALID_STATE`] for a handle to something other than a `ColorMaterial`.
///
/// # Safety
/// `config` must point at one readable [`BcsColorMaterial`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_color_material_write(handle: i32, config: *const BcsColorMaterial) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (handle, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::sprite_render::ColorMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, handle) else {
                    return status::NOT_PRESENT;
                };
                let Ok(handle) = handle.try_typed::<ColorMaterial>() else {
                    return status::INVALID_STATE;
                };
                let material = match color_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };
                let Some(mut materials) = world.get_resource_mut::<Assets<ColorMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                // Written through the asset's guard, which marks it changed for the renderer.
                let Some(mut held) = materials.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                *held = material;
                status::OK
            })
        }
    })
}

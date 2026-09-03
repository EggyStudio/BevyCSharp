//! Building the meshes and materials a picture is made of, and attaching them to entities.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use super::image_handle;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Builds a mesh primitive and returns an asset handle for it.
///
/// `kind` selects the shape and decides what the three dimensions mean:
///
/// | kind      | a       | b      | c     |
/// |-----------|---------|--------|-------|
/// | `Cuboid`  | width   | height | depth |
/// | `Sphere`  | radius  |        |       |
/// | `Plane`   | width   | depth  |       |
/// | `Capsule` | radius  | length |       |
///
/// # Safety
/// `kind` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_create(
    kind: *const core::ffi::c_char,
    a: f32,
    b: f32,
    c: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, a, b, c);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::math::primitives::{Capsule3d, Cuboid, Plane3d, Sphere};
            use bevy::mesh::{Mesh, Meshable};

            let Some(kind) = (unsafe { crate::interop::cstr_to_string(kind) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let mesh: Mesh = match kind.as_str() {
                    "Cuboid" => Cuboid::new(a, b, c).mesh().into(),
                    "Sphere" => Sphere::new(a).mesh().into(),
                    "Plane" => Plane3d::default()
                        .mesh()
                        .size(a, b)
                        .into(),
                    "Capsule" => Capsule3d::new(a, b).mesh().into(),
                    _ => return status::NO_COMPONENT,
                };

                let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = meshes.add(mesh).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// Builds a physically based material and returns an asset handle for it.
///
/// Colour components are linear sRGB in the range zero to one. `metallic` and `roughness` follow
/// the usual convention: zero metallic for a dielectric, roughness near zero for a mirror.
///
/// A texture is named by the asset key of an already-loaded image, which is how the two halves of
/// the asset surface meet: `bcs_asset_load` produces the key, this consumes it. The image does
/// not have to have finished loading; the material picks it up when it arrives, which is the
/// behavior a Bevy handle has anyway.
///
/// # Safety
/// `config` must point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create(config: *const BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::{Assets, Handle};
            use bevy::color::{Color, LinearRgba};
            use bevy::image::Image;
            use bevy::material::AlphaMode;
            use bevy::pbr::StandardMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                // Resolved before the material is built, because each one needs the world and
                // building it needs the world back to insert the result.
                let mut textures: [Option<Handle<Image>>; 5] = Default::default();
                let keys = [
                    config.base_color_texture,
                    config.normal_map,
                    config.metallic_roughness_texture,
                    config.emissive_texture,
                    config.occlusion_texture,
                ];

                for (slot, key) in keys.iter().enumerate() {
                    match image_handle(world, *key) {
                        Ok(handle) => textures[slot] = handle,
                        Err(status) => return status,
                    }
                }

                let [
                    base_color_texture,
                    normal_map_texture,
                    metallic_roughness_texture,
                    emissive_texture,
                    occlusion_texture,
                ] = textures;

                let alpha_mode = match config.alpha_mode {
                    1 => AlphaMode::Mask(config.alpha_cutoff),
                    2 => AlphaMode::Blend,
                    3 => AlphaMode::Add,
                    _ => AlphaMode::Opaque,
                };

                let material = StandardMaterial {
                    base_color: Color::linear_rgba(
                        config.base_color[0],
                        config.base_color[1],
                        config.base_color[2],
                        config.base_color[3],
                    ),
                    metallic: config.metallic,
                    perceptual_roughness: config.roughness,
                    emissive: LinearRgba::new(
                        config.emissive[0],
                        config.emissive[1],
                        config.emissive[2],
                        config.emissive[3],
                    ),
                    alpha_mode,
                    double_sided: config.double_sided != 0,
                    // A double-sided material still culls unless the back faces are kept, which
                    // is a separate field and the one people actually mean.
                    cull_mode: if config.double_sided != 0 {
                        None
                    } else {
                        Some(bevy::render::render_resource::Face::Back)
                    },
                    unlit: config.unlit != 0,
                    // Scale first, then rotate, then shift, which is the order that makes a
                    // scale of eight mean "eight tiles" whatever the other two are set to.
                    uv_transform: bevy::math::Affine2::from_scale_angle_translation(
                        bevy::math::Vec2::new(config.uv_scale[0], config.uv_scale[1]),
                        config.uv_rotation,
                        bevy::math::Vec2::new(config.uv_offset[0], config.uv_offset[1]),
                    ),
                    base_color_texture,
                    normal_map_texture,
                    metallic_roughness_texture,
                    emissive_texture,
                    occlusion_texture,
                    ..Default::default()
                };

                let Some(mut materials) = world.get_resource_mut::<Assets<StandardMaterial>>()
                else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(material).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// Attaches an asset to an entity through one of the components that carry a handle.
///
/// `component` is `Mesh3d` or `MeshMaterial3d`. These go through Bevy's own insert rather than a
/// byte copy, both because the handle has to be retyped and because inserting them pulls in the
/// components Bevy requires alongside, such as `Transform` and `Visibility`.
///
/// # Safety
/// `component` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_insert_asset(
    entity: u64,
    component: *const core::ffi::c_char,
    handle: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, component, handle);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::mesh::{Mesh, Mesh3d};
            use bevy::pbr::{MeshMaterial3d, StandardMaterial};

            let Some(component) = (unsafe { crate::interop::cstr_to_string(component) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Some(untyped) = crate::assets::clone_handle(world, handle) else {
                    return status::NO_ENTITY;
                };

                let entity = Entity::from_bits(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                // try_typed rather than typed: asking for a mesh component with a material handle
                // is a mistake the managed side can make, and it should be an error rather than
                // a panic crossing the boundary.
                match component.as_str() {
                    "Mesh3d" => match untyped.try_typed::<Mesh>() {
                        Ok(handle) => {
                            entity_mut.insert(Mesh3d(handle));
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    "MeshMaterial3d" => match untyped.try_typed::<StandardMaterial>() {
                        Ok(handle) => {
                            entity_mut.insert(MeshMaterial3d(handle));
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    _ => status::NO_COMPONENT,
                }
            })
        }
    })
}

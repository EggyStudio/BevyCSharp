//! Creating renderable assets and attaching them to entities.
//!
//! Everything here needs the `render` feature. A headless build keeps the entry points so the
//! managed side links either way, and they report [`status::UNSUPPORTED`].
//!
//! Two things cannot be done through the generic component path. Meshes and materials are Rust
//! values that have to be constructed rather than described by a layout, and the components that
//! carry them hold a typed `Handle<T>`, which raw bytes cannot represent. Both are therefore
//! named operations rather than data the managed side writes directly.

use crate::interop::status;

#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

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
#[unsafe(no_mangle)]
pub extern "C" fn bcs_material_create(
    red: f32,
    green: f32,
    blue: f32,
    alpha: f32,
    metallic: f32,
    roughness: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (red, green, blue, alpha, metallic, roughness);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::color::Color;
            use bevy::pbr::StandardMaterial;

            with_world(|world| {
                let material = StandardMaterial {
                    base_color: Color::srgba(red, green, blue, alpha),
                    metallic,
                    perceptual_roughness: roughness,
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

/// Spawns a 3D camera and returns its entity, or `0` on a headless build.
///
/// The camera is placed at the origin looking down negative Z. Position it by writing its
/// `Transform` like any other entity.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_spawn_camera_3d() -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            0
        }

        #[cfg(feature = "render")]
        {
            with_world_opt(|world| {
                world
                    .spawn((
                        bevy::camera::Camera3d::default(),
                        bevy::transform::components::Transform::default(),
                    ))
                    .id()
                    .to_bits()
            })
            .unwrap_or(0)
        }
    })
}

/// Spawns a light and returns its entity, or `0` on a headless build.
///
/// `kind` is `0` for a directional light, whose `intensity` is illuminance in lux, or `1` for a
/// point light, whose `intensity` is luminous power in lumens. Position it by writing its
/// `Transform`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_spawn_light(kind: i32, intensity: f32) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, intensity);
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::{DirectionalLight, PointLight};
            use bevy::transform::components::Transform;

            with_world_opt(|world| match kind {
                0 => world
                    .spawn((
                        DirectionalLight {
                            illuminance: intensity,
                            shadow_maps_enabled: true,
                            ..Default::default()
                        },
                        Transform::default(),
                    ))
                    .id()
                    .to_bits(),
                _ => world
                    .spawn((
                        PointLight {
                            intensity,
                            shadow_maps_enabled: true,
                            ..Default::default()
                        },
                        Transform::default(),
                    ))
                    .id()
                    .to_bits(),
            })
            .unwrap_or(0)
        }
    })
}

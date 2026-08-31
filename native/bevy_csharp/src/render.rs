//! Creating renderable assets and attaching them to entities.
//!
//! Everything here needs the `render` feature. A headless build keeps the entry points so the
//! managed side links either way, and they report [`status::UNSUPPORTED`].
//!
//! Two things cannot be done through the generic component path. Meshes and materials are Rust
//! values that have to be constructed rather than described by a layout, and the components that
//! carry them hold a typed `Handle<T>`, which raw bytes cannot represent. Both are therefore
//! named operations rather than data the managed side writes directly.

use crate::interop::{status, BcsCameraConfig, BcsLightConfig};

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
/// `config` may be null, which spawns Bevy's default perspective camera.
///
/// # Safety
/// `config` must be null or point to a readable [`BcsCameraConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_spawn_camera_3d(config: *const BcsCameraConfig) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::{
                Camera, Camera3d, ClearColorConfig, OrthographicProjection, PerspectiveProjection,
                Projection, ScalingMode,
            };
            use bevy::color::Color;
            use bevy::transform::components::Transform;

            let config = if config.is_null() {
                None
            } else {
                Some(unsafe { *config })
            };

            with_world_opt(|world| {
                let Some(config) = config else {
                    return world
                        .spawn((Camera3d::default(), Transform::default()))
                        .id()
                        .to_bits();
                };

                let projection = if config.projection == 1 {
                    // The height is what C# asked for; the width follows from the window, which
                    // is what keeps the picture from stretching when the window is resized.
                    Projection::Orthographic(OrthographicProjection {
                        scaling_mode: ScalingMode::FixedVertical {
                            viewport_height: config.ortho_height,
                        },
                        near: config.near,
                        far: config.far,
                        ..OrthographicProjection::default_3d()
                    })
                } else {
                    Projection::Perspective(PerspectiveProjection {
                        fov: config.fov_degrees.to_radians(),
                        near: config.near,
                        far: config.far,
                        ..Default::default()
                    })
                };

                let clear_color = match config.clear_mode {
                    1 => ClearColorConfig::Custom(Color::linear_rgba(
                        config.clear[0],
                        config.clear[1],
                        config.clear[2],
                        config.clear[3],
                    )),
                    2 => ClearColorConfig::None,
                    _ => ClearColorConfig::Default,
                };

                world
                    .spawn((
                        Camera3d::default(),
                        Camera {
                            order: config.order as isize,
                            clear_color,
                            ..Default::default()
                        },
                        projection,
                        Transform::default(),
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
/// Position and aim it by writing its `Transform`; a directional or spot light shines down its
/// own negative Z, which is what `Transform.LookingAt` produces.
///
/// # Safety
/// `config` must point to a readable [`BcsLightConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_spawn_light(config: *const BcsLightConfig) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::light::{DirectionalLight, PointLight, SpotLight};
            use bevy::transform::components::Transform;

            if config.is_null() {
                return 0;
            }
            let config = unsafe { *config };
            let color = Color::linear_rgb(config.color[0], config.color[1], config.color[2]);
            let shadows = config.shadows != 0;

            with_world_opt(|world| match config.kind {
                0 => world
                    .spawn((
                        DirectionalLight {
                            color,
                            illuminance: config.intensity,
                            shadow_maps_enabled: shadows,
                            ..Default::default()
                        },
                        Transform::default(),
                    ))
                    .id()
                    .to_bits(),
                2 => world
                    .spawn((
                        SpotLight {
                            color,
                            intensity: config.intensity,
                            range: config.range,
                            radius: config.radius,
                            shadow_maps_enabled: shadows,
                            inner_angle: config.inner_angle,
                            outer_angle: config.outer_angle,
                            ..Default::default()
                        },
                        Transform::default(),
                    ))
                    .id()
                    .to_bits(),
                _ => world
                    .spawn((
                        PointLight {
                            color,
                            intensity: config.intensity,
                            range: config.range,
                            radius: config.radius,
                            shadow_maps_enabled: shadows,
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

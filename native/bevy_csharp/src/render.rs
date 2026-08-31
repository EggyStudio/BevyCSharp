//! Creating renderable assets and attaching them to entities.
//!
//! Everything here needs the `render` feature. A headless build keeps the entry points so the
//! managed side links either way, and they report [`status::UNSUPPORTED`].
//!
//! Two things cannot be done through the generic component path. Meshes and materials are Rust
//! values that have to be constructed rather than described by a layout, and the components that
//! carry them hold a typed `Handle<T>`, which raw bytes cannot represent. Both are therefore
//! named operations rather than data the managed side writes directly.

use crate::interop::{
    status, BcsCameraConfig, BcsLightConfig, BcsMaterialConfig, BcsSpriteConfig,
};

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
                let texture = |key: i32| -> Option<Handle<Image>> {
                    if key < 0 {
                        return None;
                    }
                    crate::assets::clone_handle(world, key).map(|handle| handle.typed::<Image>())
                };

                let base_color_texture = texture(config.base_color_texture);
                let normal_map_texture = texture(config.normal_map);
                let metallic_roughness_texture = texture(config.metallic_roughness_texture);
                let emissive_texture = texture(config.emissive_texture);
                let occlusion_texture = texture(config.occlusion_texture);

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

                let camera = world
                    .spawn((
                        Camera3d::default(),
                        Camera {
                            order: config.order as isize,
                            clear_color,
                            viewport: viewport_from(&config),
                            ..Default::default()
                        },
                        projection,
                        Transform::default(),
                    ))
                    .id();

                if let Some(layers) = layers_from(config.layers) {
                    world.entity_mut(camera).insert(layers);
                }

                camera.to_bits()
            })
            .unwrap_or(0)
        }
    })
}

/// Builds the viewport a camera config asks for, if it asks for one.
///
/// Measured in physical pixels rather than logical ones, because that is what a framebuffer is
/// divided into: half of a window is half its physical width whatever the display scaling.
#[cfg(feature = "render")]
fn viewport_from(config: &BcsCameraConfig) -> Option<bevy::camera::Viewport> {
    if config.has_viewport == 0 {
        return None;
    }

    Some(bevy::camera::Viewport {
        physical_position: bevy::math::UVec2::new(config.viewport[0], config.viewport[1]),
        physical_size: bevy::math::UVec2::new(config.viewport[2], config.viewport[3]),
        ..Default::default()
    })
}

/// Turns a bit per layer into Bevy's own set, or `None` for the default layer.
///
/// A mask of zero means "say nothing", so the entity or camera keeps Bevy's default of layer 0.
/// Asking for layer 0 explicitly is bit 0, which is the same thing said out loud.
#[cfg(feature = "render")]
fn layers_from(mask: u32) -> Option<bevy::camera::visibility::RenderLayers> {
    if mask == 0 {
        return None;
    }

    let layers: Vec<usize> = (0..32).filter(|bit| mask & (1 << bit) != 0).collect();
    Some(bevy::camera::visibility::RenderLayers::from_layers(&layers))
}

/// Puts an entity on a set of render layers, or takes it back to the default.
///
/// A camera draws an entity only when their layers overlap, which is what separates a minimap's
/// contents from the world's, or one player's view from another's in splitscreen.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_layers(entity: u64, mask: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, mask);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::visibility::RenderLayers;

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                match layers_from(mask) {
                    Some(layers) => {
                        entity_mut.insert(layers);
                    }
                    None => {
                        entity_mut.remove::<RenderLayers>();
                    }
                }
                status::OK
            })
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

/// Spawns a 2D camera and returns its entity, or `0` on a headless build.
///
/// A 2D camera looks down negative Z with one world unit to a pixel, which is what makes a sprite
/// placed at `(100, 50)` land a hundred pixels right and fifty up from the middle of the window.
/// Give it an `order` above a 3D camera's to draw over the top of one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_spawn_camera_2d(order: i32) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = order;
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::{Camera, Camera2d, ClearColorConfig};
            use bevy::transform::components::Transform;

            with_world_opt(|world| {
                world
                    .spawn((
                        Camera2d,
                        Camera {
                            order: order as isize,
                            // A second camera that cleared would wipe out whatever drew before
                            // it, so one layered over a scene keeps what is already there.
                            clear_color: if order == 0 {
                                ClearColorConfig::Default
                            } else {
                                ClearColorConfig::None
                            },
                            ..Default::default()
                        },
                        Transform::default(),
                    ))
                    .id()
                    .to_bits()
            })
            .unwrap_or(0)
        }
    })
}

/// Attaches a sprite to an entity, or replaces the one it has.
///
/// Position it by writing its `Transform`, like anything else in the world.
///
/// # Safety
/// `config` must point to a readable [`BcsSpriteConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_sprite(entity: u64, config: *const BcsSpriteConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::image::Image;
            use bevy::math::{Rect, Vec2};
            use bevy::sprite::Sprite;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(image) = crate::assets::clone_handle(world, config.image) else {
                    return status::NO_COMPONENT;
                };

                let sprite = Sprite {
                    image: image.typed::<Image>(),
                    color: Color::linear_rgba(
                        config.color[0],
                        config.color[1],
                        config.color[2],
                        config.color[3],
                    ),
                    flip_x: config.flip_x != 0,
                    flip_y: config.flip_y != 0,
                    custom_size: (config.has_size != 0)
                        .then(|| Vec2::new(config.size[0], config.size[1])),
                    rect: (config.has_rect != 0).then(|| {
                        Rect::new(config.rect[0], config.rect[1], config.rect[2], config.rect[3])
                    }),
                    ..Default::default()
                };

                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                // Bevy's own insert, so the components a sprite requires arrive with it.
                entity_mut.insert(sprite);
                status::OK
            })
        }
    })
}

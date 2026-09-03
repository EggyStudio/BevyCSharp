//! Spawning what a picture contains: cameras, lights and sprites.

use crate::interop::{status, BcsCameraConfig, BcsLightConfig, BcsSpriteConfig};

#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

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
                            shadow_depth_bias: config.shadow_depth_bias,
                            shadow_normal_bias: config.shadow_normal_bias,
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
                            shadow_depth_bias: config.shadow_depth_bias,
                            shadow_normal_bias: config.shadow_normal_bias,
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
                            shadow_depth_bias: config.shadow_depth_bias,
                            shadow_normal_bias: config.shadow_normal_bias,
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
            use bevy::image::{Image, TextureAtlas, TextureAtlasLayout};
            use bevy::math::{Rect, Vec2};
            use bevy::sprite::{
                Anchor, BorderRect, SliceScaleMode, Sprite, SpriteImageMode, TextureSlicer,
            };

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(image) = crate::assets::clone_handle(world, config.image) else {
                    return status::NO_COMPONENT;
                };

                // A negative key is "no atlas", which is the whole image. A key that names
                // nothing is a mistake rather than a default, so it is refused.
                let atlas = if config.atlas < 0 {
                    None
                } else {
                    let Some(layout) = crate::assets::clone_handle(world, config.atlas) else {
                        return status::NO_COMPONENT;
                    };

                    Some(TextureAtlas {
                        layout: layout.typed::<TextureAtlasLayout>(),
                        index: config.atlas_index as usize,
                    })
                };

                let slicer = TextureSlicer {
                    border: BorderRect {
                        min_inset: Vec2::new(config.slice_border[0], config.slice_border[1]),
                        max_inset: Vec2::new(config.slice_border[2], config.slice_border[3]),
                    },
                    center_scale_mode: SliceScaleMode::Stretch,
                    sides_scale_mode: SliceScaleMode::Stretch,
                    max_corner_scale: if config.corner_scale > 0.0 {
                        config.corner_scale
                    } else {
                        1.0
                    },
                };

                let image_mode = match config.mode {
                    1 => SpriteImageMode::Sliced(slicer),
                    2 => SpriteImageMode::Tiled {
                        tile_x: config.tile_x != 0,
                        tile_y: config.tile_y != 0,
                        stretch_value: if config.tile_stretch > 0.0 {
                            config.tile_stretch
                        } else {
                            1.0
                        },
                    },
                    _ => SpriteImageMode::Auto,
                };

                let sprite = Sprite {
                    texture_atlas: atlas,
                    image_mode,
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

                // The anchor is its own component rather than a field on the sprite, so an
                // entity that was never given one keeps Bevy's centred default.
                if config.has_anchor != 0 {
                    entity_mut.insert(Anchor(Vec2::new(config.anchor[0], config.anchor[1])));
                }

                status::OK
            })
        }
    })
}

/// Sets how large a shadow map each kind of light gets, in pixels on a side.
///
/// One number for every directional light and one for every point and spot light, because Bevy
/// keeps these as global settings rather than per light. Larger is sharper and costs memory and
/// fill rate on every shadow-casting light at once; Bevy's defaults are 2048 and 1024.
///
/// A size of `0` leaves that kind alone, so one can be changed without knowing the other.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_shadow_maps(directional: u32, point: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (directional, point);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::{DirectionalLightShadowMap, PointLightShadowMap};

            with_world(|world| {
                if directional > 0 {
                    world.insert_resource(DirectionalLightShadowMap {
                        size: directional as usize,
                    });
                }
                if point > 0 {
                    world.insert_resource(PointLightShadowMap {
                        size: point as usize,
                    });
                }
                status::OK
            })
        }
    })
}

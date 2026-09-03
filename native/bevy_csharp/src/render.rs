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
    status, BcsAtmosphereConfig, BcsCameraConfig, BcsEffectsConfig, BcsLightConfig,
    BcsMaterialConfig, BcsPostConfig, BcsSpriteConfig,
};

#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

/// Resolves an asset key to the image it names.
///
/// A negative key is the caller saying "no image", which every image on a config is allowed to
/// be. A key that names nothing is a mistake rather than a default: that is what a released or
/// fabricated handle looks like from this side, and quietly drawing without the texture that was
/// asked for is a wrong picture nothing reports.
#[cfg(feature = "render")]
fn image_handle(
    world: &mut bevy::ecs::world::World,
    key: i32,
) -> Result<Option<bevy::asset::Handle<bevy::image::Image>>, i32> {
    if key < 0 {
        return Ok(None);
    }

    match crate::assets::clone_handle(world, key) {
        Some(handle) => Ok(Some(handle.typed::<bevy::image::Image>())),
        None => Err(status::NO_COMPONENT),
    }
}

/// Reports why `entity` cannot be given what a camera is given, or `None` if it can.
///
/// Everything these calls attach is read by the render graph the camera drives, so on any other
/// entity it would sit there doing nothing. Answered through a shared reference, so a call can be
/// refused before it has built anything it would have to throw away.
#[cfg(feature = "render")]
fn refuse_unless_camera(
    world: &bevy::ecs::world::World,
    entity: bevy::ecs::entity::Entity,
) -> Option<i32> {
    let Ok(entity_ref) = world.get_entity(entity) else {
        return Some(status::NO_ENTITY);
    };

    if !entity_ref.contains::<bevy::camera::Camera>() {
        return Some(status::NOT_PRESENT);
    }

    None
}

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

/// Takes temporal antialiasing off a camera, along with what it brought with it.
///
/// Bevy adds the jitter, the mip bias and the two prepasses as required components and leaves
/// them behind when the component that asked for them goes. Left alone the jitter keeps nudging
/// the projection every frame with nothing to resolve it, which reads as a shimmer, and the
/// prepasses keep drawing the scene again for nobody.
///
/// The motion vector prepass is the one piece that is not exclusively temporal antialiasing's:
/// motion blur asks for it too, so it stays while a camera is still smearing.
#[cfg(feature = "render")]
fn drop_temporal(entity: &mut bevy::ecs::world::EntityWorldMut) {
    use bevy::anti_alias::taa::TemporalAntiAliasing;
    use bevy::core_pipeline::prepass::{DepthPrepass, MotionVectorPrepass};
    use bevy::post_process::motion_blur::MotionBlur;
    use bevy::render::camera::{MipBias, TemporalJitter};

    if !entity.contains::<TemporalAntiAliasing>() {
        return;
    }

    entity.remove::<(TemporalAntiAliasing, TemporalJitter, MipBias, DepthPrepass)>();

    if !entity.contains::<MotionBlur>() {
        entity.remove::<MotionVectorPrepass>();
    }
}

/// Sets what a camera does to the picture after the scene has been drawn.
///
/// Every effect is applied on every call, so a config describes the whole pipeline rather than
/// one change to it: an effect the config leaves off is removed from the camera if it was there.
/// That keeps a settings screen honest, since turning bloom off is the same call as turning it on.
///
/// Bloom reads a high dynamic range target, so asking for it without `hdr` gets a picture where
/// nothing is bright enough to scatter. The two are left to the caller rather than forced
/// together, because a game may want the range without the glow.
///
/// Temporal antialiasing is the one arm that can be refused. It resolves the whole picture from
/// past frames, which a multisampled target has not got, and Bevy answers the pair by warning
/// once a frame and drawing nothing, so a config asking for both is reported as
/// [`status::INVALID_STATE`] and the camera is left as it was. It also wants a 3D camera: the
/// jitter it reads back is only applied to one, and on a 2D camera the pass finds nothing to
/// resolve.
///
/// # Safety
/// `config` must point to a readable [`BcsPostConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_post(entity: u64, config: *const BcsPostConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::anti_alias::contrast_adaptive_sharpening::ContrastAdaptiveSharpening;
            use bevy::anti_alias::fxaa::{Fxaa, Sensitivity};
            use bevy::anti_alias::smaa::{Smaa, SmaaPreset};
            use bevy::anti_alias::taa::TemporalAntiAliasing;
            use bevy::camera::Hdr;
            use bevy::core_pipeline::tonemapping::{DebandDither, Tonemapping};
            use bevy::post_process::bloom::{Bloom, BloomCompositeMode, BloomPrefilter};
            use bevy::render::view::Msaa;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            let tonemapping = match config.tonemapping {
                0 => Tonemapping::None,
                1 => Tonemapping::Reinhard,
                2 => Tonemapping::ReinhardLuminance,
                3 => Tonemapping::AcesFitted,
                4 => Tonemapping::AgX,
                5 => Tonemapping::SomewhatBoringDisplayTransform,
                7 => Tonemapping::BlenderFilmic,
                _ => Tonemapping::TonyMcMapface,
            };

            let msaa = match config.msaa {
                2 => Msaa::Sample2,
                4 => Msaa::Sample4,
                8 => Msaa::Sample8,
                _ => Msaa::Off,
            };

            // Refused before anything is written, so a camera that asked for the impossible pair
            // keeps the pipeline it had rather than half of the new one.
            if config.antialias == 3 && msaa != Msaa::Off {
                return status::INVALID_STATE;
            }

            let sensitivity = match config.antialias_quality {
                0 => Sensitivity::Low,
                2 => Sensitivity::High,
                3 => Sensitivity::Ultra,
                _ => Sensitivity::Medium,
            };

            let preset = match config.antialias_quality {
                0 => SmaaPreset::Low,
                2 => SmaaPreset::High,
                3 => SmaaPreset::Ultra,
                _ => SmaaPreset::Medium,
            };

            with_world(|world| {
                let entity = crate::ecs::entity_from(entity);

                if let Some(status) = refuse_unless_camera(world, entity) {
                    return status;
                }

                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                entity_mut.insert(tonemapping);
                entity_mut.insert(if config.dither != 0 {
                    DebandDither::Enabled
                } else {
                    DebandDither::Disabled
                });
                entity_mut.insert(msaa);

                if config.hdr != 0 {
                    entity_mut.insert(Hdr);
                } else {
                    entity_mut.remove::<Hdr>();
                }

                if config.bloom != 0 {
                    entity_mut.insert(Bloom {
                        intensity: config.bloom_intensity,
                        prefilter: BloomPrefilter {
                            threshold: config.bloom_threshold,
                            threshold_softness: config.bloom_threshold_softness,
                        },
                        composite_mode: if config.bloom_mode == 1 {
                            BloomCompositeMode::Additive
                        } else {
                            BloomCompositeMode::EnergyConserving
                        },
                        ..Bloom::NATURAL
                    });
                } else {
                    entity_mut.remove::<Bloom>();
                }

                match config.antialias {
                    1 => {
                        entity_mut.remove::<Smaa>();
                        drop_temporal(&mut entity_mut);
                        entity_mut.insert(Fxaa {
                            enabled: true,
                            edge_threshold: sensitivity,
                            edge_threshold_min: sensitivity,
                        });
                    }
                    2 => {
                        entity_mut.remove::<Fxaa>();
                        drop_temporal(&mut entity_mut);
                        entity_mut.insert(Smaa { preset });
                    }
                    3 => {
                        entity_mut.remove::<Fxaa>();
                        entity_mut.remove::<Smaa>();

                        // Inserted only if it is not there, unlike every other effect here. The
                        // component's one field asks Bevy to throw away the frames it has
                        // accumulated, and the config has nothing to say about it, so writing a
                        // fresh one on every call would keep clearing the history the pass
                        // exists to build.
                        if !entity_mut.contains::<TemporalAntiAliasing>() {
                            entity_mut.insert(TemporalAntiAliasing::default());
                        }
                    }
                    _ => {
                        entity_mut.remove::<Fxaa>();
                        entity_mut.remove::<Smaa>();
                        drop_temporal(&mut entity_mut);
                    }
                }

                if config.sharpen > 0.0 {
                    entity_mut.insert(ContrastAdaptiveSharpening {
                        enabled: true,
                        sharpening_strength: config.sharpen,
                        denoise: false,
                    });
                } else {
                    entity_mut.remove::<ContrastAdaptiveSharpening>();
                }

                status::OK
            })
        }
    })
}

/// Sets the lens effects a camera draws through.
///
/// Beside [`bcs_render_set_post`] rather than part of it: that call is the pipeline a settings
/// screen owns, and these are what a scene does for a moment. The same rule holds, so a config is
/// the whole set rather than one change to it and an effect left off is taken off the camera.
///
/// Depth of field needs a perspective camera, because focus has no meaning without one, and Bevy
/// drops the effect rather than reporting it. Auto exposure needs compute shaders, which every
/// desktop backend has and WebGL2 does not, and a high dynamic range target, which it brings with
/// it. That target belongs to [`bcs_render_set_post`], so a later call there without `hdr` takes
/// it away again.
///
/// # Safety
/// `config` must point to a readable [`BcsEffectsConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_effects(
    entity: u64,
    config: *const BcsEffectsConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::anti_alias::taa::TemporalAntiAliasing;
            use bevy::asset::{Assets, Handle};
            use bevy::color::Color;
            use bevy::core_pipeline::prepass::MotionVectorPrepass;
            use bevy::math::Vec2;
            use bevy::post_process::auto_exposure::{AutoExposure, AutoExposureCompensationCurve};
            use bevy::post_process::dof::{DepthOfField, DepthOfFieldMode};
            use bevy::post_process::effect_stack::{ChromaticAberration, LensDistortion, Vignette};
            use bevy::post_process::motion_blur::MotionBlur;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let entity = crate::ecs::entity_from(entity);

                // Checked before anything is built, so a call that is going to be refused does
                // not leave a compensation curve behind in the asset store.
                if let Some(status) = refuse_unless_camera(world, entity) {
                    return status;
                }

                // The images and the curve are resolved next, because each needs the world and
                // the inserts need it back afterwards.
                let aberration_lut = match image_handle(world, config.aberration_lut) {
                    Ok(handle) => handle,
                    Err(status) => return status,
                };

                // Bevy's own default is a white image, which weights the whole frame alike.
                let metering_mask = match image_handle(world, config.metering_mask) {
                    Ok(handle) => handle.unwrap_or_default(),
                    Err(status) => return status,
                };

                let points = (config.compensation_points as usize).min(8);
                let compensation = if config.auto_exposure != 0 && points >= 2 {
                    let curve =
                        bevy::math::cubic_splines::LinearSpline::new((0..points).map(|i| {
                            Vec2::new(
                                config.compensation_curve[i * 2],
                                config.compensation_curve[i * 2 + 1],
                            )
                        }));

                    // The curve has to rise in luminance, since it is read by looking a measured
                    // brightness up in it. Bevy reports that as an error rather than sorting the
                    // points, and so does this.
                    let Ok(built) = AutoExposureCompensationCurve::from_curve(curve) else {
                        return status::INVALID_STATE;
                    };

                    let Some(mut curves) =
                        world.get_resource_mut::<Assets<AutoExposureCompensationCurve>>()
                    else {
                        return status::INVALID_STATE;
                    };

                    curves.add(built)
                } else {
                    Handle::default()
                };

                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                match config.dof_mode {
                    0 => {
                        entity_mut.remove::<DepthOfField>();
                    }
                    mode => {
                        let default = DepthOfField::default();
                        entity_mut.insert(DepthOfField {
                            mode: if mode == 2 {
                                DepthOfFieldMode::Bokeh
                            } else {
                                DepthOfFieldMode::Gaussian
                            },
                            focal_distance: config.focal_distance,
                            // The aperture divides the scale of the blur, so a zero left in the
                            // config would make every circle of confusion infinite.
                            aperture_f_stops: if config.aperture_f_stops > 0.0 {
                                config.aperture_f_stops
                            } else {
                                default.aperture_f_stops
                            },
                            sensor_height: if config.sensor_height > 0.0 {
                                config.sensor_height
                            } else {
                                default.sensor_height
                            },
                            max_circle_of_confusion_diameter: if config.max_blur_diameter > 0.0 {
                                config.max_blur_diameter
                            } else {
                                default.max_circle_of_confusion_diameter
                            },
                            max_depth: if config.max_depth > 0.0 {
                                config.max_depth
                            } else {
                                f32::INFINITY
                            },
                        });
                    }
                }

                if config.shutter_angle > 0.0 && config.motion_blur_samples > 0 {
                    entity_mut.insert(MotionBlur {
                        shutter_angle: config.shutter_angle,
                        samples: config.motion_blur_samples,
                    });
                } else {
                    // The prepass goes with it. Bevy brings it in as a required component and
                    // leaves it behind on removal, and it draws the scene a second time every
                    // frame, so a camera that stopped blurring would go on paying for it. Unless
                    // temporal antialiasing is reading it too, which is the other thing that
                    // asks for motion vectors.
                    entity_mut.remove::<MotionBlur>();
                    if !entity_mut.contains::<TemporalAntiAliasing>() {
                        entity_mut.remove::<MotionVectorPrepass>();
                    }
                }

                if config.aberration > 0.0 {
                    let default = ChromaticAberration::default();
                    entity_mut.insert(ChromaticAberration {
                        color_lut: aberration_lut,
                        intensity: config.aberration,
                        max_samples: if config.aberration_samples > 0 {
                            config.aberration_samples
                        } else {
                            default.max_samples
                        },
                    });
                } else {
                    entity_mut.remove::<ChromaticAberration>();
                }

                if config.distortion != 0.0 {
                    entity_mut.insert(LensDistortion {
                        intensity: config.distortion,
                        scale: if config.distortion_scale > 0.0 {
                            config.distortion_scale
                        } else {
                            1.0
                        },
                        multiplier: Vec2::new(config.distortion_axes[0], config.distortion_axes[1]),
                        center: Vec2::new(config.distortion_center[0], config.distortion_center[1]),
                        edge_curvature: config.distortion_edge_curvature,
                    });
                } else {
                    entity_mut.remove::<LensDistortion>();
                }

                if config.vignette > 0.0 {
                    let default = Vignette::default();
                    entity_mut.insert(Vignette {
                        intensity: config.vignette,
                        radius: config.vignette_radius,
                        smoothness: if config.vignette_smoothness > 0.0 {
                            config.vignette_smoothness
                        } else {
                            default.smoothness
                        },
                        roundness: config.vignette_roundness,
                        center: Vec2::new(config.vignette_center[0], config.vignette_center[1]),
                        edge_compensation: config.vignette_edge_compensation,
                        color: Color::linear_rgba(
                            config.vignette_color[0],
                            config.vignette_color[1],
                            config.vignette_color[2],
                            config.vignette_color[3],
                        ),
                    });
                } else {
                    entity_mut.remove::<Vignette>();
                }

                if config.auto_exposure != 0 {
                    let default = AutoExposure::default();
                    entity_mut.insert(AutoExposure {
                        range: config.metering_min..=config.metering_max,
                        filter: config.metering_low..=config.metering_high,
                        speed_brighten: config.speed_brighten,
                        speed_darken: config.speed_darken,
                        exponential_transition_distance: if config.exposure_transition > 0.0 {
                            config.exposure_transition
                        } else {
                            default.exponential_transition_distance
                        },
                        metering_mask,
                        compensation_curve: compensation,
                    });
                } else {
                    entity_mut.remove::<AutoExposure>();
                }

                status::OK
            })
        }
    })
}

/// Draws the sky earth's air scatters, seen from `camera`.
///
/// Two things make a sky: a planet, which is an entity the size of a world with the air described
/// on it, and a camera told to sample it. This call keeps at most one planet in the world and
/// points the camera at it, because a scene has one sky and a second planet would be picked
/// between by distance rather than by intent.
///
/// The sun is whichever directional light is in the scene: the sky is scattered from its
/// direction and colour, so moving that light moves the sun and a scene without one gets a
/// night sky.
///
/// The planet is metres across, and Bevy places it so the ground sits at the origin. A scene
/// measured in something other than metres says so with `scale` rather than by moving anything.
///
/// # Safety
/// `config` must point to a readable [`BcsAtmosphereConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_atmosphere(
    camera: u64,
    config: *const BcsAtmosphereConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::camera::Hdr;
            use bevy::light::atmosphere::{Atmosphere, ScatteringMedium};
            use bevy::pbr::AtmosphereSettings;
            use bevy::transform::components::Transform;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };
            let entity = crate::ecs::entity_from(camera);

            with_world(|world| {
                if let Some(status) = refuse_unless_camera(world, entity) {
                    return status;
                }

                if config.enabled == 0 {
                    // The planet is left where it is. Nothing computes an atmosphere until a
                    // camera asks for one, so an unused planet costs a component and no work.
                    world.entity_mut(entity).remove::<AtmosphereSettings>();
                    return status::OK;
                }

                // Earth's air. Mars is the other medium Bevy ships and it is not offered here:
                // its dust phase comes from a texture the caller would have to supply, and one
                // that is not supplied leaves a sky that cannot be built at all.
                let density = if config.density > 0.0 { config.density } else { 1.0 };
                let medium = ScatteringMedium::earth(256, 256).with_density_multiplier(density);

                let Some(mut media) = world.get_resource_mut::<Assets<ScatteringMedium>>() else {
                    return status::INVALID_STATE;
                };

                let atmosphere = Atmosphere::earth(media.add(medium));

                let scale = if config.scale > 0.0 { config.scale } else { 1.0 };

                // One planet: the existing one is rewritten rather than joined by another, since
                // Bevy renders whichever is nearest and two would be a coin toss.
                let existing = world
                    .query_filtered::<bevy::ecs::entity::Entity, bevy::ecs::query::With<Atmosphere>>()
                    .iter(world)
                    .next();

                match existing {
                    Some(planet) => {
                        world.entity_mut(planet).insert(atmosphere);
                    }
                    None => {
                        // Spawned without a transform of its own, so Bevy's own hook drops the
                        // planet below the origin and the ground ends up where the scene is.
                        world.spawn(atmosphere);
                    }
                }

                if scale != 1.0 {
                    let planet = world
                        .query_filtered::<bevy::ecs::entity::Entity, bevy::ecs::query::With<Atmosphere>>()
                        .iter(world)
                        .next();

                    if let Some(planet) = planet {
                        world
                            .entity_mut(planet)
                            .insert(Transform::from_scale(bevy::math::Vec3::splat(scale)));
                    }
                }

                let mut settings = AtmosphereSettings::default();
                if config.haze_distance > 0.0 {
                    settings.aerial_view_lut_max_distance = config.haze_distance;
                }

                // `AtmosphereSettings` requires `Hdr`, and Bevy's insert brings it, but a camera
                // that had it removed would otherwise keep drawing without one.
                world.entity_mut(entity).insert((Hdr, settings));
                status::OK
            })
        }
    })
}

//! Cameras and what they show, their targets, viewports, layers and projection, the clear color and
//! rounded corners of their pictures, an entity's bounds and wireframe, and points carried between
//! the world and a camera's picture.

use crate::interop::{status, BcsCameraConfig};

#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

/// Points a camera at an image instead of at the window.
///
/// A negative `image` puts it back on the window, which is where a camera starts. The image comes
/// from [`crate::render::images::bcs_render_create_target`], and a material given the same handle
/// samples what this camera drew, which is the whole of a portal or a security monitor.
///
/// The camera keeps everything else it was given. Its projection, its layers, its order and its
/// post-processing are about what it draws rather than about where the result goes.
///
/// A `layer` of zero or more draws into that layer of an image with several, such as a face of a
/// cube, through the companion [`crate::render::layers`] keeps. Below zero draws into the whole.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_camera_target(entity: u64, image: i32, layer: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, image, layer);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::RenderTarget;
            use bevy::ecs::entity::Entity;
            use bevy::window::WindowRef;

            let entity = Entity::from_bits(entity);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let target = match crate::render::image_handle(world, image) {
                    Err(refusal) => return refusal,
                    // One layer of an image with several, drawn through a companion.
                    Ok(Some(image)) if layer >= 0 => {
                        return crate::render::layers::set(world, entity, image, layer as u32);
                    }
                    Ok(Some(image)) => RenderTarget::Image(image.into()),
                    Ok(None) => RenderTarget::Window(WindowRef::Primary),
                };

                crate::render::layers::clear(world, entity);
                world.entity_mut(entity).insert(target);
                status::OK
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
                    // C# asked for the height; the width follows from the window, which keeps the
                    // picture from stretching when the window is resized.
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
/// Measured in physical pixels rather than logical ones, because a framebuffer is divided into
/// those, so half of a window is half its physical width whatever the display scaling.
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
pub(crate) fn layers_from(mask: u32) -> Option<bevy::camera::visibility::RenderLayers> {
    if mask == 0 {
        return None;
    }

    let layers: Vec<usize> = (0..32).filter(|bit| mask & (1 << bit) != 0).collect();
    Some(bevy::camera::visibility::RenderLayers::from_layers(&layers))
}

/// Puts an entity on a set of render layers, or takes it back to the default.
///
/// A camera draws an entity only when their layers overlap, which separates a minimap's contents
/// from the world's, or one player's view from another's in splitscreen.
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

/// Gives a camera part of the window to draw into, or the whole of it.
///
/// What a docked panel needs. The interface takes the right of the window and the scene is told to
/// draw into what is left, so the picture is the shape of the space rather than the shape of the
/// window with something over it. A width or height of zero means the whole window again.
///
/// In physical pixels, because a framebuffer is divided into those.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_viewport(
    camera: u64,
    x: u32,
    y: u32,
    width: u32,
    height: u32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, x, y, width, height);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(camera))
                else {
                    return status::NO_ENTITY;
                };

                let Some(mut camera) = entity_mut.get_mut::<bevy::camera::Camera>() else {
                    return status::NOT_PRESENT;
                };

                if width == 0 || height == 0 {
                    camera.viewport = None;
                    return status::OK;
                }

                camera.viewport = Some(bevy::camera::Viewport {
                    physical_position: bevy::math::UVec2::new(x, y),
                    physical_size: bevy::math::UVec2::new(width, height),
                    ..Default::default()
                });

                status::OK
            })
        }
    })
}

/// Spawns a 2D camera and returns its entity, or `0` on a headless build.
///
/// A 2D camera looks down negative Z with one world unit to a pixel, which makes a sprite placed at
/// `(100, 50)` land a hundred pixels right and fifty up from the middle of the window. Give it an
/// `order` above a 3D camera's to draw over the top of one.
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
            use bevy::camera::{Camera, Camera2d, CameraOutputMode, ClearColorConfig};
            use bevy::color::Color;
            use bevy::render::render_resource::BlendState;
            use bevy::transform::components::Transform;

            // An overlay is more than a second camera with a higher order. A camera renders into a
            // view texture of its own and then writes that over the target, so one left alone
            // replaces whatever the camera below it drew, and one told not to clear accumulates its
            // own output frame after frame instead. Both have to be said, which means clearing the
            // camera's own view to nothing, and blending the result over the target rather than
            // overwriting it.
            let (clear_color, output_mode) = if order == 0 {
                (ClearColorConfig::Default, CameraOutputMode::default())
            } else {
                (
                    ClearColorConfig::Custom(Color::NONE),
                    CameraOutputMode::Write {
                        blend_state: Some(BlendState::ALPHA_BLENDING),
                        clear_color: ClearColorConfig::None,
                    },
                )
            };

            with_world_opt(|world| {
                world
                    .spawn((
                        Camera2d,
                        Camera {
                            order: order as isize,
                            clear_color,
                            output_mode,
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

/// Writes an entity's world-space bounds into `out`, min x, y, z then max x, y, z.
///
/// A tool needs this to draw a box around what is selected, and a camera needs it to frame it. Bevy
/// computes an `Aabb` for every mesh it draws, in the mesh's own space, so the eight corners are
/// put through the entity's global transform here and the box around those comes back. Doing it on
/// this side keeps a rotated object's box honest, because transforming the two corners alone would
/// give a box that shrinks as the object turns.
///
/// Reports [`status::NOT_PRESENT`] for an entity Bevy has computed no bounds for, which is
/// anything that is not drawn.
///
/// # Safety
/// `out` must be writable for six floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_bounds(entity: u64, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::primitives::Aabb;
            use bevy::math::Vec3A;
            use bevy::transform::components::GlobalTransform;

            if out.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(aabb) = entity_ref.get::<Aabb>() else {
                    return status::NOT_PRESENT;
                };

                let transform = entity_ref
                    .get::<GlobalTransform>()
                    .copied()
                    .unwrap_or_default();

                let affine = transform.affine();
                let mut min = Vec3A::splat(f32::INFINITY);
                let mut max = Vec3A::splat(f32::NEG_INFINITY);

                for corner in 0..8 {
                    let offset = Vec3A::new(
                        if corner & 1 == 0 { -1.0 } else { 1.0 },
                        if corner & 2 == 0 { -1.0 } else { 1.0 },
                        if corner & 4 == 0 { -1.0 } else { 1.0 },
                    ) * aabb.half_extents;

                    let point = affine.transform_point3a(aabb.center + offset);
                    min = min.min(point);
                    max = max.max(point);
                }

                unsafe {
                    out.write(min.x);
                    out.add(1).write(min.y);
                    out.add(2).write(min.z);
                    out.add(3).write(max.x);
                    out.add(4).write(max.y);
                    out.add(5).write(max.z);
                }

                status::OK
            })
        }
    })
}

/// Draws an entity's mesh as a wireframe, or stops.
///
/// An editor outlines a selection with this when a box round it is not enough. It draws the shape
/// itself, edge by edge, which says what was picked rather than roughly where it is.
///
/// Reports [`status::UNSUPPORTED`] where there is no renderer. The wireframe pipeline itself needs
/// a backend that can draw lines, which is every desktop one and neither of the web ones; where it
/// cannot, the component is set and nothing is drawn, which is the engine's answer to give.
///
/// # Safety
/// Nothing is dereferenced; `entity` is checked against the world.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_wireframe(entity: u64, on: i32, r: f32, g: f32, b: f32, a: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, on, r, g, b, a);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::pbr::wireframe::{Wireframe, WireframeColor};

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                if on == 0 {
                    entity_mut.remove::<(Wireframe, WireframeColor)>();
                    return status::OK;
                }

                entity_mut.insert((
                    Wireframe,
                    WireframeColor {
                        color: bevy::color::Color::linear_rgba(r, g, b, a),
                    },
                ));

                status::OK
            })
        }
    })
}

/// Projects a world point onto the camera's viewport, writing `x` and `y` in logical pixels.
///
/// What a tool needs to hit-test something it drew in the world against the cursor, which is
/// reported in the same coordinates. A gizmo is three lines in the scene and a handle under the
/// pointer, and only this turns one into the other.
///
/// Reports [`status::NOT_PRESENT`] when the point is behind the camera or otherwise off the
/// viewport, which is an ordinary answer rather than a failure.
///
/// # Safety
/// `out` must be writable for two floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_world_to_viewport(
    camera: u64,
    x: f32,
    y: f32,
    z: f32,
    out: *mut f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, x, y, z, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::Camera;
            use bevy::math::Vec3;
            use bevy::transform::components::GlobalTransform;

            if out.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(camera)) else {
                    return status::NO_ENTITY;
                };
                let (Some(camera), Some(transform)) = (
                    entity_ref.get::<Camera>(),
                    entity_ref.get::<GlobalTransform>(),
                ) else {
                    return status::NOT_PRESENT;
                };

                let Ok(point) = camera.world_to_viewport(transform, Vec3::new(x, y, z)) else {
                    return status::NOT_PRESENT;
                };

                unsafe {
                    out.write(point.x);
                    out.add(1).write(point.y);
                }

                status::OK
            })
        }
    })
}

/// Turns a point on the viewport into a ray, origin `x, y, z` then direction `x, y, z`.
///
/// The other half of [`bcs_render_world_to_viewport`], and what makes dragging a gizmo handle
/// exact rather than approximate. The point being dragged to is where the cursor's ray comes
/// closest to the axis being dragged along, which is a question about two lines in the world.
///
/// # Safety
/// `out` must be writable for six floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_viewport_to_world(
    camera: u64,
    x: f32,
    y: f32,
    out: *mut f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, x, y, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::Camera;
            use bevy::math::Vec2;
            use bevy::transform::components::GlobalTransform;

            if out.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(camera)) else {
                    return status::NO_ENTITY;
                };
                let (Some(camera), Some(transform)) = (
                    entity_ref.get::<Camera>(),
                    entity_ref.get::<GlobalTransform>(),
                ) else {
                    return status::NOT_PRESENT;
                };

                let Ok(ray) = camera.viewport_to_world(transform, Vec2::new(x, y)) else {
                    return status::NOT_PRESENT;
                };

                unsafe {
                    out.write(ray.origin.x);
                    out.add(1).write(ray.origin.y);
                    out.add(2).write(ray.origin.z);
                    out.add(3).write(ray.direction.x);
                    out.add(4).write(ray.direction.y);
                    out.add(5).write(ray.direction.z);
                }

                status::OK
            })
        }
    })
}

/// Rounds the corners of a camera's picture by `radius` physical pixels, showing `r`, `g`, `b`, `a`
/// outside them in straight linear RGBA, which is clear at all zeros, or with a radius of `0`
/// squares them again.
///
/// The corners of the camera's viewport where it has one, and of its whole picture otherwise. See
/// [`super::corners`].
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_rounded_corners(camera: u64, radius: f32, r: f32, g: f32, b: f32, a: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, radius, r, g, b, a);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !radius.is_finite() || radius < 0.0 {
                return crate::interop::status::NULL_ARG;
            }

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                super::corners::set(world, entity, radius, [r, g, b, a])
            })
        }
    })
}

/// Sets the world's clear color, in linear RGBA, which every camera clearing to the world's color
/// clears to, and which fills the part of a window a camera's viewport leaves out.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_clear_color(r: f32, g: f32, b: f32, a: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (r, g, b, a);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                world.insert_resource(bevy::camera::ClearColor(bevy::color::Color::linear_rgba(r, g, b, a)));
                crate::interop::status::OK
            })
        }
    })
}

/// Sets a camera's field of view, in degrees across its height, and how near and how far it sees,
/// making it a perspective camera if it was not one.
///
/// For a camera being tuned while it runs, such as an editor's view of the scene, where the
/// projection it was spawned with is a starting point rather than a decision.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_perspective(camera: u64, fov_degrees: f32, near: f32, far: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, fov_degrees, near, far);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::{PerspectiveProjection, Projection};

            if !(fov_degrees > 0.0 && fov_degrees < 180.0) || !(near > 0.0) || !(far > near) {
                return crate::interop::status::NULL_ARG;
            }

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut target = world.entity_mut(entity);

                match target.get_mut::<Projection>() {
                    Some(mut projection) => match projection.as_mut() {
                        Projection::Perspective(perspective) => {
                            perspective.fov = fov_degrees.to_radians();
                            perspective.near = near;
                            perspective.far = far;
                        }
                        other => {
                            *other = Projection::Perspective(PerspectiveProjection {
                                fov: fov_degrees.to_radians(),
                                near,
                                far,
                                ..Default::default()
                            });
                        }
                    },
                    None => {
                        target.insert(Projection::Perspective(PerspectiveProjection {
                            fov: fov_degrees.to_radians(),
                            near,
                            far,
                            ..Default::default()
                        }));
                    }
                }

                crate::interop::status::OK
            })
        }
    })
}

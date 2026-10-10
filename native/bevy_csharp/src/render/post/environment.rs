//! Lighting and a background from cubemaps: one filtered on the GPU, one derived from the sky, a
//! pair somebody baked, and the skybox, each waiting for its images to become cubes.

use crate::interop::status;

#[cfg(feature = "render")]
use crate::state::with_world;

/// Lights the scene from the sky the camera is already scattering.
///
/// An environment map makes a surface pick up the color of what is around it rather than only what
/// a lamp points at it, and this derives one from the atmosphere instead of from a file somebody
/// baked. It needs [`bcs_render_set_atmosphere`](super::bcs_render_set_atmosphere) on the same camera, because what it filters is the
/// sky that is being drawn.
///
/// `intensity` scales the result, `size` is the square resolution of the cubemap it generates and
/// has to be a power of two, and `on` at zero takes it off again.
///
/// # Safety
/// `camera` must be a live camera entity.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_sky_lighting(
    camera: u64,
    on: i32,
    intensity: f32,
    size: u32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, on, intensity, size);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::light::AtmosphereEnvironmentMapLight;
            use bevy::math::UVec2;

            let entity = Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut camera = world.entity_mut(entity);

                if on == 0 {
                    camera.remove::<AtmosphereEnvironmentMapLight>();
                    return status::OK;
                }

                // A size that is not a power of two is refused here rather than by the renderer,
                // which would take it, allocate nothing usable and light the scene with black.
                if size == 0 || !size.is_power_of_two() {
                    return status::NULL_ARG;
                }

                camera.insert(AtmosphereEnvironmentMapLight {
                    intensity,
                    size: UVec2::new(size, size),
                    ..Default::default()
                });

                status::OK
            })
        }
    })
}

/// Lights the scene from a cubemap, filtered on the GPU.
///
/// The other way to light a scene from its surroundings. [`bcs_render_set_sky_lighting`] derives
/// the map from the atmosphere, which covers an outdoor scene; this takes a picture, for an indoor
/// one or a scene lit from a photograph.
///
/// One cubemap rather than the two a baked environment map carries, because Bevy filters it into
/// the diffuse and specular halves itself. It is the same column of six faces a skybox takes, and
/// it is reinterpreted the same way once it has loaded, so the same file can be seen and be the
/// light.
///
/// A negative `image` takes the lighting off. `rotation` turns the map, for a cubemap authored
/// with a different axis up, and is four floats or null for none.
///
/// # Safety
/// `camera` must be a live camera entity; `rotation` must be null or point to four readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_image_lighting(
    camera: u64,
    image: i32,
    intensity: f32,
    rotation: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, image, intensity, rotation);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::light::GeneratedEnvironmentMapLight;
            use bevy::math::Quat;

            let turn = if rotation.is_null() {
                Quat::IDENTITY
            } else {
                let parts = unsafe { core::slice::from_raw_parts(rotation, 4) };
                Quat::from_xyzw(parts[0], parts[1], parts[2], parts[3])
            };

            let entity = Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let handle = match crate::render::image_handle(world, image) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                let Some(handle) = handle else {
                    world.entity_mut(entity).remove::<GeneratedEnvironmentMapLight>();
                    return status::OK;
                };

                // Asked for here and inserted by `reinterpret_cubemaps` once the image is a
                // cube, because Bevy's own filter panics on one that is not rather than waiting.
                world
                    .get_resource_or_init::<PendingCubemaps>()
                    .0
                    .push(PendingCubemap {
                        image: handle,
                        light: Some((entity, intensity, turn)),
                    });

                status::OK
            })
        }
    })
}

/// One image asked to become a cubemap, and what to do once it is.
#[cfg(feature = "render")]
pub struct PendingCubemap {
    /// The image, still a tall picture until its pixels arrive.
    pub image: bevy::asset::Handle<bevy::image::Image>,
    /// A camera to light with it once it is a cube, with the intensity and rotation asked for.
    ///
    /// Held rather than inserted straight away, because Bevy refuses to filter an image that is
    /// not yet square, and refuses it by panicking inside a system rather than by reporting it.
    pub light: Option<(bevy::ecs::entity::Entity, f32, bevy::math::Quat)>,
}

/// One camera or light probe waiting to be lit by a pair of cubemaps somebody baked.
///
/// Two images rather than one, so both have to be a cube before the light can be inserted, which is
/// why this waits on its own rather than riding on [`PendingCubemap`]. On a camera the pair lights
/// everything it sees; on a light probe it lights what is inside the probe's box, which Bevy calls
/// a reflection probe.
#[cfg(feature = "render")]
pub struct PendingEnvironment {
    /// The camera or light probe to light.
    pub target: bevy::ecs::entity::Entity,
    /// The blurred map, which a rough surface reflects.
    pub diffuse: bevy::asset::Handle<bevy::image::Image>,
    /// The sharp one, which a polished surface reflects.
    pub specular: bevy::asset::Handle<bevy::image::Image>,
    /// How bright, in candelas per square meter.
    pub intensity: f32,
    /// Which way the map is turned.
    pub rotation: bevy::math::Quat,
}

/// The baked environment maps waiting for both their images to become cubes.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct PendingEnvironments(pub(crate) Vec<PendingEnvironment>);

/// The images asked to become cubemaps, waiting for their pixels to arrive.
///
/// An image loads as one tall picture and has to be told it is six square faces stacked on top of
/// each other, which can only happen once the file has been decoded. A handle asked for before then
/// waits here until it has.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct PendingCubemaps(Vec<PendingCubemap>);

#[cfg(feature = "render")]
impl PendingCubemaps {
    /// Adds an image to turn into a cubemap with nothing waiting on it.
    pub fn push(&mut self, image: bevy::asset::Handle<bevy::image::Image>) {
        self.0.push(PendingCubemap { image, light: None });
    }
}

/// Cubemaps being put together from six images of their own, each waiting for all six to load.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct PendingFaces(pub(crate) Vec<PendingFace>);

/// One cubemap made of six images: the image it becomes, and the faces in the column's order.
#[cfg(feature = "render")]
pub struct PendingFace {
    /// The image handed back at once, which holds a placeholder until the faces arrive.
    pub target: bevy::asset::Handle<bevy::image::Image>,
    /// +X, -X, +Y, -Y, +Z, -Z.
    pub faces: [bevy::asset::Handle<bevy::image::Image>; 6],
}

/// Fills each cubemap made of six images once all six have loaded, as a column of their pixels,
/// and hands it on to be made a cube.
///
/// The faces have to be square, one size and one format, since a cube's faces are one texture's
/// layers. A set that is not is dropped with a warning rather than drawn wrongly.
#[cfg(feature = "render")]
pub fn gather_faces(
    mut pending: bevy::ecs::system::ResMut<PendingFaces>,
    mut cubemaps: bevy::ecs::system::ResMut<PendingCubemaps>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
) {
    pending.0.retain(|waiting| {
        let faces: Vec<_> = waiting.faces.iter().filter_map(|face| images.get(face)).collect();
        if faces.len() < 6 {
            return true;
        }

        let first = &faces[0].texture_descriptor;
        let fits = faces.iter().all(|face| {
            let descriptor = &face.texture_descriptor;
            descriptor.size == first.size
                && descriptor.format == first.format
                && descriptor.size.width == descriptor.size.height
                && descriptor.size.depth_or_array_layers == 1
                && face.data.is_some()
        });

        if !fits {
            bevy::log::warn!(
                "The six images asked to be a cubemap are not square faces of one size and one \
                 format, so the cubemap will not draw."
            );
            return false;
        }

        let mut column = Vec::new();
        for face in &faces {
            column.extend_from_slice(face.data.as_deref().unwrap_or_default());
        }

        let mut made = bevy::image::Image::new(
            bevy::render::render_resource::Extent3d {
                width: first.size.width,
                height: first.size.height * 6,
                depth_or_array_layers: 1,
            },
            bevy::render::render_resource::TextureDimension::D2,
            column,
            first.format,
            faces[0].asset_usage,
        );
        made.sampler = faces[0].sampler.clone();

        let _ = images.insert(&waiting.target, made);
        cubemaps.push(waiting.target.clone());
        false
    });
}

/// Turns each loaded image on the list into a cubemap, and forgets it.
///
/// Six faces stacked vertically, the layout every cubemap texture on the web is in and the one
/// Bevy's own examples use. An image that is not six times as tall as it is wide is refused by Bevy
/// rather than by this, and stays on the list doing nothing, since a loud failure would cost a
/// frame instead of once.
/// Lays a cubemap drawn as a cross or a row of faces out as the column Bevy reads, in place.
///
/// Three layouts besides the column are common in the files a game is given. A horizontal cross,
/// four faces wide and three tall, with +Y over +Z, -Y under it, and -X, +Z, +X and -Z across the
/// middle. A vertical cross, three wide and four tall, the same with -Z under -Y, turned half a
/// turn as it is drawn there. And a row of six in the column's order, +X, -X, +Y, -Y, +Z, -Z. The
/// shape says which, since each has its own proportion. Answers false for a shape that is none of
/// them, or a format whose pixels are not whole bytes each, such as a compressed one, which would
/// have to be rearranged block by block.
#[cfg(feature = "render")]
fn restack(image: &mut bevy::image::Image) -> bool {
    let size = image.texture_descriptor.size;
    let (width, height) = (size.width as usize, size.height as usize);

    if height == width * 6 {
        return true;
    }

    // Where each face is in the picture, in faces, in the column's order, and whether it is drawn
    // turned half a turn.
    let (face, places): (usize, [(usize, usize, bool); 6]) = if width == height * 6 {
        (height, [(0, 0, false), (1, 0, false), (2, 0, false), (3, 0, false), (4, 0, false), (5, 0, false)])
    } else if width * 3 == height * 4 {
        (width / 4, [(2, 1, false), (0, 1, false), (1, 0, false), (1, 2, false), (1, 1, false), (3, 1, false)])
    } else if width * 4 == height * 3 {
        (width / 3, [(2, 1, false), (0, 1, false), (1, 0, false), (1, 2, false), (1, 1, false), (1, 3, true)])
    } else {
        return false;
    };

    let format = image.texture_descriptor.format;
    if format.block_dimensions() != (1, 1) {
        return false;
    }

    let Some(bytes) = format.block_copy_size(None) else {
        return false;
    };
    let pixel = bytes as usize;

    let Some(data) = image.data.as_ref() else {
        return false;
    };

    let mut column = vec![0u8; face * face * 6 * pixel];

    for (index, (across, down, turned)) in places.into_iter().enumerate() {
        for y in 0..face {
            for x in 0..face {
                // A face drawn half a turn round is read from its far corner back.
                let (from_x, from_y) = if turned { (face - 1 - x, face - 1 - y) } else { (x, y) };
                let from = (((down * face + from_y) * width) + (across * face + from_x)) * pixel;
                let to = (((index * face + y) * face) + x) * pixel;
                column[to..to + pixel].copy_from_slice(&data[from..from + pixel]);
            }
        }
    }

    image.data = Some(column);
    image.texture_descriptor.size = bevy::render::render_resource::Extent3d {
        width: face as u32,
        height: (face * 6) as u32,
        depth_or_array_layers: 1,
    };

    true
}

#[cfg(feature = "render")]
pub fn reinterpret_cubemaps(
    mut commands: bevy::ecs::system::Commands,
    mut pending: bevy::ecs::system::ResMut<PendingCubemaps>,
    mut environments: bevy::ecs::system::ResMut<PendingEnvironments>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
    faces: bevy::ecs::system::Res<PendingFaces>,
) {
    use bevy::light::GeneratedEnvironmentMapLight;
    use bevy::render::render_resource::{TextureViewDescriptor, TextureViewDimension};

    pending.0.retain(|waiting| {
        // One made of six images holds a placeholder until all six arrive, which is not a cube.
        if faces.0.iter().any(|gathering| gathering.target == waiting.image) {
            return true;
        }

        let Some(mut image) = images.get_mut(&waiting.image) else {
            // Still loading. Asking again next frame is the whole of the wait.
            return true;
        };

        // Six layers make it a cube, and an image that already has them was asked for twice, which
        // is not worth refusing. A cross or a row of faces is laid out as a column first.
        if image.texture_descriptor.size.depth_or_array_layers == 1
            && (!restack(&mut image) || image.reinterpret_stacked_2d_as_array(6).is_err())
        {
            bevy::log::warn!(
                "An image asked to be a cubemap is not six square faces in a column, a row or a \
                 cross, so whatever asked for it will not draw."
            );

            return false;
        }

        image.texture_view_descriptor = Some(TextureViewDescriptor {
            dimension: Some(TextureViewDimension::Cube),
            ..Default::default()
        });

        // Only now, because filtering an image that is not yet square is a panic inside Bevy
        // rather than a refusal, and the image is not square until the line above.
        if let Some((camera, intensity, rotation)) = waiting.light
            && let Ok(mut camera) = commands.get_entity(camera)
        {
            camera.insert(GeneratedEnvironmentMapLight {
                environment_map: waiting.image.clone(),
                intensity,
                rotation,
                ..Default::default()
            });
        }

        false
    });

    // A baked map is two images, and a light inserted while either is still a tall picture
    // samples it as one. Both are pushed through the list above, so this only has to wait for
    // the shape to change.
    environments.0.retain(|waiting| {
        let cube = |handle: &bevy::asset::Handle<bevy::image::Image>| {
            images
                .get(handle)
                .is_some_and(|image| image.texture_descriptor.size.depth_or_array_layers == 6)
        };

        if !cube(&waiting.diffuse) || !cube(&waiting.specular) {
            return true;
        }

        if let Ok(mut target) = commands.get_entity(waiting.target) {
            target.insert(bevy::light::EnvironmentMapLight {
                diffuse_map: waiting.diffuse.clone(),
                specular_map: waiting.specular.clone(),
                intensity: waiting.intensity,
                rotation: waiting.rotation,
                ..Default::default()
            });
        }

        false
    });
}

/// Draws a cubemap behind everything a camera draws.
///
/// The image is a column of six square faces, in the order every cubemap texture uses, and it is
/// reinterpreted as a cube once it has loaded. `brightness` scales the samples into the units the
/// rest of the scene is lit in, which are candelas per square meter, so the useful numbers are in
/// the hundreds or thousands and a brightness of `1` comes out black.
///
/// A negative `image` takes the skybox off. A null `rotation` leaves the cube unturned; otherwise
/// it is four floats, `x, y, z, w`, which puts a Z-up cubemap the right way round in a Y-up world.
///
/// # Safety
/// `camera` must be a live camera entity; `rotation` must be null or point to four readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_skybox(
    camera: u64,
    image: i32,
    brightness: f32,
    rotation: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, image, brightness, rotation);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::light::Skybox;
            use bevy::math::Quat;

            let turn = if rotation.is_null() {
                Quat::IDENTITY
            } else {
                let parts = unsafe { core::slice::from_raw_parts(rotation, 4) };
                Quat::from_xyzw(parts[0], parts[1], parts[2], parts[3])
            };

            let entity = Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let handle = match crate::render::image_handle(world, image) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                if let Some(handle) = handle.clone() {
                    world
                        .get_resource_or_init::<PendingCubemaps>()
                        .0
                        .push(PendingCubemap {
                            image: handle,
                            light: None,
                        });
                }

                world.entity_mut(entity).insert(Skybox {
                    image: handle,
                    brightness,
                    rotation: turn,
                });

                status::OK
            })
        }
    })
}

/// Lights the scene from a pair of cubemaps somebody baked earlier.
///
/// The other end of [`bcs_render_set_image_lighting`], which filters one cubemap on the GPU every
/// time the app starts. This takes the two maps a tool produced, which costs nothing at startup and
/// suits a shipped game, especially for an environment too large to filter again.
///
/// `diffuse` is the blurred map a rough surface reflects and `specular` is the sharp one a
/// polished surface reflects. Both are a column of six faces like a skybox, and both are
/// reinterpreted before the light is inserted, because a map sampled while it is still a tall
/// picture is sampled wrongly rather than refused.
///
/// A negative for either takes the lighting off. `rotation` turns both maps together, and is four
/// floats or null for none.
///
/// Returns [`status::NOT_PRESENT`] where the entity is not a camera, and
/// [`status::NO_COMPONENT`] where a key names no image.
///
/// # Safety
/// `rotation` must be null or point to four readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_environment_map(
    camera: u64,
    diffuse: i32,
    specular: i32,
    intensity: f32,
    rotation: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, diffuse, specular, intensity, rotation);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::math::Quat;

            let entity = Entity::from_bits(camera);

            let turn = if rotation.is_null() {
                Quat::IDENTITY
            } else {
                let parts = unsafe { core::slice::from_raw_parts(rotation, 4) };
                Quat::from_xyzw(parts[0], parts[1], parts[2], parts[3])
            };

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let diffuse = match crate::render::image_handle(world, diffuse) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                let specular = match crate::render::image_handle(world, specular) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                // Either one missing is a caller taking the lighting off, since a baked map is
                // the pair and half of one is not a weaker version of it.
                let (Some(diffuse), Some(specular)) = (diffuse, specular) else {
                    world
                        .entity_mut(entity)
                        .remove::<bevy::light::EnvironmentMapLight>();

                    return status::OK;
                };

                {
                    let mut pending = world.get_resource_or_init::<PendingCubemaps>();

                    pending.0.push(PendingCubemap {
                        image: diffuse.clone(),
                        light: None,
                    });

                    pending.0.push(PendingCubemap {
                        image: specular.clone(),
                        light: None,
                    });
                }

                world
                    .get_resource_or_init::<PendingEnvironments>()
                    .0
                    .push(PendingEnvironment {
                        target: entity,
                        diffuse,
                        specular,
                        intensity,
                        rotation: turn,
                    });

                status::OK
            })
        }
    })
}

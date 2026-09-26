//! What a camera does to the picture once the scene has been drawn.

use crate::interop::{status, BcsAtmosphereConfig, BcsEffectsConfig, BcsPostConfig, BcsReflectionConfig};

#[cfg(feature = "render")]
use super::{image_handle, refuse_unless_camera};
#[cfg(feature = "render")]
use crate::state::with_world;

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

    entity.remove::<(TemporalAntiAliasing, TemporalJitter, MipBias)>();

    // Kept where the game asked for depth itself, which a shader pass reading it does.
    if !entity
        .get::<RequestedPrepass>()
        .is_some_and(|requested| requested.0 & 1 != 0)
    {
        entity.remove::<DepthPrepass>();
    }

    if !entity.contains::<MotionBlur>() && !asked_for_motion(entity) {
        entity.remove::<MotionVectorPrepass>();
    }
}

/// Whether the game asked for motion vectors itself, which a pass or a compute shader reading them
/// does, so that taking an effect off does not take them away.
#[cfg(feature = "render")]
pub fn asked_for_motion(entity: &bevy::ecs::world::EntityWorldMut) -> bool {
    entity
        .get::<RequestedPrepass>()
        .is_some_and(|requested| requested.0 & 4 != 0)
}

/// Which prepasses a game asked a camera for, as the flags it gave.
///
/// Remembered so that what something else took off with it, as temporal antialiasing does with
/// the depth prepass when it goes, can be left where the game still wants it.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
pub struct RequestedPrepass(pub u32);

/// Asks a camera to draw depth, normals and motion vectors before the scene, for its shader passes
/// and compute shaders to read.
///
/// `flags` is a bit each: `1` depth, `2` normals, `4` motion vectors, `8` Bevy's G-buffer, which
/// also turns deferred rendering on, and `16` keeping the previous frame's depth and G-buffer beside
/// this frame's, which does nothing without one of them. A bit left clear takes that prepass off again, unless
/// something else on the camera needs it, which is temporal antialiasing for depth and motion,
/// motion blur for motion, and screen-space reflections for depth and the G-buffer.
///
/// A prepass draws the scene a second time, so it is only worth asking for when something reads
/// what it draws. A multisampled camera draws them multisampled, which a pass cannot bind, so a
/// camera read this way wants `Msaa` of one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_prepass(camera: u64, flags: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, flags);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::anti_alias::taa::TemporalAntiAliasing;
            use bevy::core_pipeline::prepass::{
                DeferredPrepass, DeferredPrepassDoubleBuffer, DepthPrepass,
                DepthPrepassDoubleBuffer, MotionVectorPrepass, NormalPrepass,
            };
            use bevy::pbr::ScreenSpaceReflections;
            use bevy::post_process::motion_blur::MotionBlur;

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = refuse_unless_camera(world, entity) {
                    return refusal;
                }

                // Deferred is a switch for every camera's materials as well as a prepass on this
                // one, and is left on when this camera stops asking, since others may be reading it.
                if flags & 8 != 0 {
                    set_deferred(world, true);
                }

                let mut camera = world.entity_mut(entity);
                camera.insert(RequestedPrepass(flags));
                let reflecting = camera.contains::<ScreenSpaceReflections>();

                // The G-buffer is drawn over the depth the depth prepass leaves, so it brings that.
                if flags & (1 | 8) != 0 {
                    camera.insert(DepthPrepass);
                } else if !camera.contains::<TemporalAntiAliasing>() && !reflecting {
                    camera.remove::<DepthPrepass>();
                }

                if flags & 8 != 0 {
                    camera.insert(DeferredPrepass);
                } else if !reflecting {
                    camera.remove::<DeferredPrepass>();
                }

                // Last frame's depth, and G-buffer where there is one, kept beside this frame's.
                // Removed first either way, since each requires its prepass and would put it back.
                camera.remove::<(DepthPrepassDoubleBuffer, DeferredPrepassDoubleBuffer)>();

                if flags & 16 != 0 && flags & (1 | 8) != 0 {
                    camera.insert(DepthPrepassDoubleBuffer);

                    if flags & 8 != 0 {
                        camera.insert(DeferredPrepassDoubleBuffer);
                    }
                }

                if flags & 2 != 0 {
                    camera.insert(NormalPrepass);
                } else {
                    camera.remove::<NormalPrepass>();
                }

                if flags & 4 != 0 {
                    camera.insert(MotionVectorPrepass);
                } else if !camera.contains::<TemporalAntiAliasing>() && !camera.contains::<MotionBlur>() {
                    camera.remove::<MotionVectorPrepass>();
                }

                status::OK
            })
        }
    })
}

/// Sets what a camera does to the picture after the scene has been drawn.
///
/// Every effect is applied on every call, so a config describes the whole pipeline rather than
/// one change to it, so an effect the config leaves off is removed from the camera if it was
/// there.
/// That keeps a settings screen honest, since turning bloom off is the same call as turning it on.
///
/// Bloom reads a high dynamic range target, so asking for it without `hdr` gets a picture where
/// nothing is bright enough to scatter. The two are left to the caller rather than forced
/// together, because a game may want the range without the glow.
///
/// Temporal antialiasing is the one arm that can be refused. It resolves the whole picture from
/// past frames, which a multisampled target has not got, and Bevy answers the pair by warning once
/// a frame and drawing nothing, so a config asking for both is reported as
/// [`status::INVALID_STATE`] and the camera is left as it was. It also wants a 3D camera, because
/// the jitter it reads back is only applied to one, and on a 2D camera the pass finds nothing to
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

/// Lights the scene from the sky the camera is already scattering.
///
/// An environment map is what makes a surface pick up the color of what is around it rather than
/// only what a lamp points at it, and this derives one from the atmosphere instead of from a file
/// somebody baked. It needs [`bcs_render_set_atmosphere`] on the same camera, because what it
/// filters is the sky that is being drawn.
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
/// the map from the atmosphere, which covers an outdoor scene; this takes a picture, which is what
/// an indoor one or a scene lit from a photograph needs.
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
/// Two images rather than one, so both have to be a cube before the light can be inserted, which
/// is why this waits on its own rather than riding on [`PendingCubemap`]. On a camera the pair
/// lights everything it sees; on a light probe it lights what is inside the probe's box, which is
/// what Bevy calls a reflection probe.
#[cfg(feature = "render")]
pub struct PendingEnvironment {
    /// The camera or light probe to light.
    pub target: bevy::ecs::entity::Entity,
    /// The blurred map, which is what a rough surface reflects.
    pub diffuse: bevy::asset::Handle<bevy::image::Image>,
    /// The sharp one, which is what a polished surface reflects.
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

/// Turns each loaded image on the list into a cubemap, and forgets it.
///
/// Six faces stacked vertically, which is the layout every cubemap texture on the web is in and the
/// one Bevy's own examples use. An image that is not six times as tall as it is wide is refused by
/// Bevy rather than by this, and stays on the list doing nothing, which is what a loud failure
/// would cost a frame instead of once.
#[cfg(feature = "render")]
pub fn reinterpret_cubemaps(
    mut commands: bevy::ecs::system::Commands,
    mut pending: bevy::ecs::system::ResMut<PendingCubemaps>,
    mut environments: bevy::ecs::system::ResMut<PendingEnvironments>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
) {
    use bevy::light::GeneratedEnvironmentMapLight;
    use bevy::render::render_resource::{TextureViewDescriptor, TextureViewDimension};

    pending.0.retain(|waiting| {
        let Some(mut image) = images.get_mut(&waiting.image) else {
            // Still loading. Asking again next frame is the whole of the wait.
            return true;
        };

        // Six layers is what makes it a cube, and an image that already has them was asked for
        // twice, which is not worth refusing.
        if image.texture_descriptor.size.depth_or_array_layers == 1
            && image.reinterpret_stacked_2d_as_array(6).is_err()
        {
            bevy::log::warn!(
                "An image asked to be a cubemap is not six square faces stacked vertically, so \
                 whatever asked for it will not draw."
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
/// it is four floats, `x, y, z, w`, which is what puts a Z-up cubemap the right way round in a
/// Y-up world.
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

/// Grades the picture a camera drew, after tonemapping.
///
/// What a game gives an artist rather than a player. The three tonal ranges are shadows, midtones
/// and highlights, and what separates them is the luminance range on the config, so a grade that
/// warms the shadows and cools the highlights is two sections and one number saying where one ends.
///
/// A null `config` puts the camera back to Bevy's own grading, which changes nothing about the
/// picture.
///
/// # Safety
/// `camera` must be a live camera entity; `config` must be null or point to a readable
/// [`BcsGradingConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_grading(
    camera: u64,
    config: *const crate::interop::BcsGradingConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::render::view::{ColorGrading, ColorGradingGlobal, ColorGradingSection};

            let config = if config.is_null() {
                None
            } else {
                Some(unsafe { *config })
            };

            let entity = Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let section = |part: crate::interop::BcsGradingSection| ColorGradingSection {
                    saturation: part.saturation,
                    contrast: part.contrast,
                    gamma: part.gamma,
                    gain: part.gain,
                    lift: part.lift,
                };

                let grading = match config {
                    None => ColorGrading::default(),
                    Some(config) => ColorGrading {
                        global: ColorGradingGlobal {
                            exposure: config.exposure,
                            temperature: config.temperature,
                            tint: config.tint,
                            hue: config.hue,
                            post_saturation: config.post_saturation,
                            midtones_range: config.midtones_from..config.midtones_to,
                        },
                        shadows: section(config.shadows),
                        midtones: section(config.midtones),
                        highlights: section(config.highlights),
                    },
                };

                world.entity_mut(entity).insert(grading);
                status::OK
            })
        }
    })
}

/// Sets the exposure a camera meters the scene at, in EV-100.
///
/// The number a photographer would set, and the base an auto exposure pass corrects rather than an
/// alternative to it. Bevy's own default is what a Blender scene assumes; sunlight is around 15,
/// an overcast day around 12 and an interior around 7, so a scene lit in physical units and
/// metered wrongly is far too bright or far too dark rather than subtly off.
///
/// # Safety
/// `camera` must be a live camera entity.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_exposure(camera: u64, ev100: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, ev100);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::Exposure;
            use bevy::ecs::entity::Entity;

            let entity = Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                world.entity_mut(entity).insert(Exposure { ev100 });
                status::OK
            })
        }
    })
}

/// Sets a camera's exposure from the lens it is standing in for.
///
/// The same three numbers a photographer sets, which is what a scene lit in real units wants to be
/// metered by. `aperture` is the f-stop, `shutter` the shutter speed in seconds, and `sensitivity`
/// the ISO. Bevy works the EV-100 out from them, so this and [`bcs_render_set_exposure`] set the
/// same thing two ways, and the same numbers are what a lens's depth of field is described by.
///
/// A zero or negative in any of them keeps Bevy's own value for it, which is f/1, 1/125 and ISO
/// 100.
///
/// Returns [`status::NOT_PRESENT`] where the entity is not a camera, as every other camera call
/// does.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_lens_exposure(
    camera: u64,
    aperture: f32,
    shutter: f32,
    sensitivity: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, aperture, shutter, sensitivity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::{Exposure, PhysicalCameraParameters};
            use bevy::ecs::entity::Entity;

            let entity = Entity::from_bits(camera);
            let stock = PhysicalCameraParameters::default();

            let lens = PhysicalCameraParameters {
                aperture_f_stops: if aperture > 0.0 {
                    aperture
                } else {
                    stock.aperture_f_stops
                },
                shutter_speed_s: if shutter > 0.0 {
                    shutter
                } else {
                    stock.shutter_speed_s
                },
                sensitivity_iso: if sensitivity > 0.0 {
                    sensitivity
                } else {
                    stock.sensitivity_iso
                },
                ..stock
            };

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                world.entity_mut(entity).insert(Exposure {
                    ev100: lens.ev100(),
                });

                status::OK
            })
        }
    })
}

/// Draws Bevy's own opaque materials deferred, into a G-buffer lit afterward, or forward, lit as they
/// are drawn, which is the default.
///
/// Deferred is what screen-space reflections read, and what makes many lights cheap. It needs a
/// camera drawn once a pixel, and it applies to Bevy's own materials only: a material a Slang
/// program draws is always forward, since it writes its color rather than a surface description.
/// Every Bevy material is prepared again, which is what moves one already drawn to the other method.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_deferred(on: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = on;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                set_deferred(world, on != 0);
                status::OK
            })
        }
    })
}

/// Whether Bevy's own materials are drawn deferred, as last asked.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource)]
struct Deferred(bool);

/// Switches Bevy's own materials to deferred or forward, preparing every one of them again.
#[cfg(feature = "render")]
fn set_deferred(world: &mut bevy::ecs::world::World, on: bool) {
    use bevy::pbr::{DefaultOpaqueRendererMethod, StandardMaterial};

    // Kept beside Bevy's resource, whose value cannot be read back, and forward until asked
    // otherwise, which is Bevy's own default.
    if world.get_resource::<Deferred>().is_some_and(|deferred| deferred.0 == on)
        || (!on && world.get_resource::<Deferred>().is_none())
    {
        return;
    }

    world.insert_resource(Deferred(on));
    world.insert_resource(if on {
        DefaultOpaqueRendererMethod::deferred()
    } else {
        DefaultOpaqueRendererMethod::forward()
    });

    if let Some(mut materials) = world.get_resource_mut::<bevy::asset::Assets<StandardMaterial>>() {
        let ids: Vec<_> = materials.ids().collect();

        for id in ids {
            if let Some(material) = materials.get_mut(id) {
                material.into_inner();
            }
        }
    }
}

/// Turns Bevy's screen-space reflections on a camera on, with `config`, or off where it is null.
///
/// Reflections are traced against the depth buffer and read the lit picture, so they show only what
/// is on screen, fading out at its edges, on surfaces smoother than the roughness ranges say. They
/// read Bevy's deferred G-buffer, so turning them on also draws Bevy's own materials deferred (see
/// [`bcs_render_set_deferred`]), and asks the camera for the depth and deferred prepasses. A camera
/// drawn once a pixel is what they work on.
///
/// # Safety
/// `config` must point to a readable [`BcsReflectionConfig`] or be null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_screen_space_reflections(
    camera: u64,
    config: *const BcsReflectionConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::core_pipeline::prepass::{DeferredPrepass, DepthPrepass};
            use bevy::ecs::entity::Entity;
            use bevy::pbr::ScreenSpaceReflections;

            let entity = Entity::from_bits(camera);
            let config = (!config.is_null()).then(|| unsafe { *config });

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let Some(config) = config else {
                    let mut camera = world.entity_mut(entity);

                    if camera.take::<ScreenSpaceReflections>().is_some() {
                        let asked = camera.get::<RequestedPrepass>().map_or(0, |asked| asked.0);

                        if asked & 8 == 0 {
                            camera.remove::<DeferredPrepass>();
                        }

                        if asked & (1 | 8) == 0
                            && !camera.contains::<bevy::anti_alias::taa::TemporalAntiAliasing>()
                        {
                            camera.remove::<DepthPrepass>();
                        }
                    }

                    return status::OK;
                };

                set_deferred(world, true);

                world.entity_mut(entity).insert(ScreenSpaceReflections {
                    min_perceptual_roughness: config.min_roughness_start..config.min_roughness_full,
                    max_perceptual_roughness: config.max_roughness_start..config.max_roughness_end,
                    thickness: config.thickness,
                    linear_steps: config.linear_steps.max(1),
                    linear_march_exponent: config.linear_exponent,
                    edge_fadeout: config.edge_gone..config.edge_full,
                    bisection_steps: config.bisection_steps,
                    use_secant: config.use_secant != 0,
                });

                status::OK
            })
        }
    })
}

/// Sets the ambient light, which is what lights a surface from every direction at once and what
/// ambient occlusion darkens.
///
/// `camera` names a camera to give its own, or is zero for the one every camera without its own
/// uses. `brightness` is in candela per square meter, which is what Bevy's lights are in, and a
/// negative one on a camera takes that camera's own away again.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_ambient_light(camera: u64, r: f32, g: f32, b: f32, brightness: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, r, g, b, brightness);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::ecs::entity::Entity;
            use bevy::light::{AmbientLight, GlobalAmbientLight};

            let color = Color::linear_rgb(r, g, b);

            with_world(|world| {
                if camera == 0 {
                    let Some(mut global) = world.get_resource_mut::<GlobalAmbientLight>() else {
                        return status::UNSUPPORTED;
                    };

                    global.color = color;
                    global.brightness = brightness.max(0.0);
                    return status::OK;
                }

                let entity = Entity::from_bits(camera);

                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut camera = world.entity_mut(entity);

                if brightness < 0.0 {
                    camera.remove::<AmbientLight>();
                } else {
                    camera.insert(AmbientLight {
                        color,
                        brightness,
                        ..Default::default()
                    });
                }

                status::OK
            })
        }
    })
}

/// Turns Bevy's screen-space ambient occlusion on or off for a camera.
///
/// `quality` is `0` low, `1` medium, `2` high and `3` ultra, or below zero to take it off, and
/// `thickness` how thick Bevy assumes what it sees to be, in world units, with zero keeping Bevy's
/// own. The result darkens the ambient light Bevy's own materials receive, which is where occlusion
/// belongs, rather than the finished picture.
///
/// It is also the way in for a package's own ambient occlusion. While it is on, a compute shader on
/// the camera sees the texture Bevy's materials read under the name `ambient_occlusion`, and one
/// that writes it at [`super::views::FramePoint::AfterPrepass`] replaces Bevy's answer with its
/// own before anything is lit. Bevy computes its own first either way, so a camera replacing it
/// sets the lowest quality.
///
/// It needs depth and normals, which it asks for itself, and a camera drawn once a pixel: Bevy
/// leaves it off on a multisampled camera, with a warning.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_ambient_occlusion(camera: u64, quality: i32, thickness: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, quality, thickness);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::core_pipeline::prepass::{DepthPrepass, NormalPrepass};
            use bevy::ecs::entity::Entity;
            use bevy::pbr::{ScreenSpaceAmbientOcclusion, ScreenSpaceAmbientOcclusionQualityLevel};

            let entity = Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut camera = world.entity_mut(entity);

                if quality < 0 {
                    if camera.take::<ScreenSpaceAmbientOcclusion>().is_some() {
                        // The prepasses it brought in go with it, unless the game asked for them,
                        // or something else on the camera reads them.
                        let asked = camera.get::<RequestedPrepass>().map_or(0, |asked| asked.0);

                        if asked & 1 == 0
                            && !camera.contains::<bevy::anti_alias::taa::TemporalAntiAliasing>()
                        {
                            camera.remove::<DepthPrepass>();
                        }

                        if asked & 2 == 0 {
                            camera.remove::<NormalPrepass>();
                        }
                    }

                    return status::OK;
                }

                let level = match quality {
                    0 => ScreenSpaceAmbientOcclusionQualityLevel::Low,
                    1 => ScreenSpaceAmbientOcclusionQualityLevel::Medium,
                    2 => ScreenSpaceAmbientOcclusionQualityLevel::High,
                    _ => ScreenSpaceAmbientOcclusionQualityLevel::Ultra,
                };

                camera.insert(ScreenSpaceAmbientOcclusion {
                    quality_level: level,
                    constant_object_thickness: if thickness > 0.0 {
                        thickness
                    } else {
                        ScreenSpaceAmbientOcclusion::default().constant_object_thickness
                    },
                });

                status::OK
            })
        }
    })
}

/// Turns order-independent transparency on or off for a camera.
///
/// What fixes transparent surfaces drawn in the wrong order. Ordinary alpha blending sorts whole
/// objects by distance, so two panes of glass crossing each other, or one mesh whose own faces
/// overlap, come out wrong from some angles and right from others. This sorts the fragments
/// instead, at the cost of a buffer the size of the screen times `layers`.
///
/// `layers` is how many fragments a pixel sorts exactly before the rest are merged approximately,
/// `average` is how many it budgets for on average, and `threshold` is the alpha below which a
/// fragment is dropped rather than stored. A zero in any of them keeps Bevy's own number. `on` at
/// zero takes it off again.
///
/// Returns [`status::NOT_PRESENT`] where the entity is not a camera, as every other camera call
/// does.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_sorted_transparency(
    camera: u64,
    on: i32,
    layers: u32,
    average: f32,
    threshold: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, on, layers, average, threshold);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::core_pipeline::oit::OrderIndependentTransparencySettings;
            use bevy::ecs::entity::Entity;

            let entity = Entity::from_bits(camera);
            let stock = OrderIndependentTransparencySettings::default();

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                if on == 0 {
                    world
                        .entity_mut(entity)
                        .remove::<OrderIndependentTransparencySettings>();

                    return status::OK;
                }

                world
                    .entity_mut(entity)
                    .insert(OrderIndependentTransparencySettings {
                        sorted_fragment_max_count: if layers > 0 {
                            layers
                        } else {
                            stock.sorted_fragment_max_count
                        },
                        fragments_per_pixel_average: if average > 0.0 {
                            average
                        } else {
                            stock.fragments_per_pixel_average
                        },
                        alpha_threshold: if threshold > 0.0 {
                            threshold
                        } else {
                            stock.alpha_threshold
                        },
                    });

                status::OK
            })
        }
    })
}

/// Sets the lens effects a camera draws through.
///
/// Beside [`bcs_render_set_post`] rather than part of it, because that call is the pipeline a
/// settings screen owns, and these are what a scene does for a moment. The same rule holds, so a
/// config is the whole set rather than one change to it and an effect left off is taken off the
/// camera.
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
                    if !entity_mut.contains::<TemporalAntiAliasing>() && !asked_for_motion(&entity_mut) {
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
/// The sun is whichever directional light is in the scene, so the sky is scattered from its
/// direction and color, so moving that light moves the sun and a scene without one gets a
/// night sky.
///
/// The planet is meters across, and Bevy places it so the ground sits at the origin. A scene
/// measured in something other than meters says so with `scale` rather than by moving anything.
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
            use bevy::ecs::entity::Entity;
            use bevy::ecs::query::With;
            use bevy::light::atmosphere::{Atmosphere, ScatteringMedium};
            use bevy::math::Vec3;
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

                let mut atmosphere = Atmosphere::earth(media.add(medium));

                // How much light the ground bounces back into the air, which is what makes the
                // underside of a cloud bright over snow and dark over sea.
                if config.ground_albedo > 0.0 {
                    atmosphere.ground_albedo = Vec3::splat(config.ground_albedo);
                }

                let scale = if config.scale > 0.0 { config.scale } else { 1.0 };

                // One planet, so the existing one is rewritten rather than joined by another, since
                // Bevy renders whichever is nearest and two would be a coin toss.
                let existing = world
                    .query_filtered::<Entity, With<Atmosphere>>()
                    .iter(world)
                    .next();

                let planet = match existing {
                    Some(planet) => {
                        world.entity_mut(planet).insert(atmosphere);
                        planet
                    }
                    // Spawned without a transform of its own, so Bevy's own hook drops the
                    // planet below the origin and the ground ends up where the scene is.
                    None => world.spawn(atmosphere).id(),
                };

                if scale != 1.0 {
                    world
                        .entity_mut(planet)
                        .insert(Transform::from_scale(Vec3::splat(scale)));
                }

                let mut settings = AtmosphereSettings::default();
                if config.haze_distance > 0.0 {
                    settings.aerial_view_lut_max_distance = config.haze_distance;
                }

                // One number rather than the dozen Bevy exposes, because every one of them trades
                // the same thing and a caller setting them apart is tuning a renderer rather than
                // describing a sky. The sky is the same either way; what changes is banding in a
                // gradient and how much of a frame it costs.
                match config.quality {
                    1 => {
                        settings.transmittance_lut_samples /= 2;
                        settings.multiscattering_lut_dirs /= 2;
                        settings.multiscattering_lut_samples /= 2;
                        settings.sky_view_lut_samples /= 2;
                        settings.aerial_view_lut_samples /= 2;
                        settings.sky_max_samples /= 2;
                    }
                    2 => {
                        settings.transmittance_lut_samples *= 2;
                        settings.multiscattering_lut_dirs *= 2;
                        settings.multiscattering_lut_samples *= 2;
                        settings.sky_view_lut_samples *= 2;
                        settings.aerial_view_lut_samples *= 2;
                        settings.sky_max_samples *= 2;
                    }
                    _ => {}
                }

                // `AtmosphereSettings` requires `Hdr`, and Bevy's insert brings it, but a camera
                // that had it removed would otherwise keep drawing without one.
                world.entity_mut(entity).insert((Hdr, settings));
                status::OK
            })
        }
    })
}

/// Lights the scene from a pair of cubemaps somebody baked earlier.
///
/// The other end of [`bcs_render_set_image_lighting`], which filters one cubemap on the GPU every
/// time the app starts. This takes the two maps a tool produced, which costs nothing at startup
/// and is what a shipped game wants, especially for an environment too large to filter again.
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

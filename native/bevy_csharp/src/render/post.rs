//! What a camera does to the picture once the scene has been drawn.

use crate::interop::{status, BcsAtmosphereConfig, BcsEffectsConfig, BcsPostConfig};

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

    entity.remove::<(TemporalAntiAliasing, TemporalJitter, MipBias, DepthPrepass)>();

    if !entity.contains::<MotionBlur>() {
        entity.remove::<MotionVectorPrepass>();
    }
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

/// The images asked to become cubemaps, waiting for their pixels to arrive.
///
/// An image loads as one tall picture and has to be told it is six square faces stacked on top of
/// each other, which can only happen once the file has been decoded. A handle asked for before then
/// waits here until it has.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct PendingCubemaps(Vec<bevy::asset::Handle<bevy::image::Image>>);

/// Turns each loaded image on the list into a cubemap, and forgets it.
///
/// Six faces stacked vertically, which is the layout every cubemap texture on the web is in and the
/// one Bevy's own examples use. An image that is not six times as tall as it is wide is refused by
/// Bevy rather than by this, and stays on the list doing nothing, which is what a loud failure
/// would cost a frame instead of once.
#[cfg(feature = "render")]
pub fn reinterpret_cubemaps(
    mut pending: bevy::ecs::system::ResMut<PendingCubemaps>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
) {
    use bevy::render::render_resource::{TextureViewDescriptor, TextureViewDimension};

    pending.0.retain(|handle| {
        let Some(mut image) = images.get_mut(handle) else {
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
                 the skybox using it will not draw."
            );

            return false;
        }

        image.texture_view_descriptor = Some(TextureViewDescriptor {
            dimension: Some(TextureViewDimension::Cube),
            ..Default::default()
        });

        false
    });
}

/// Draws a cubemap behind everything a camera draws.
///
/// The image is a column of six square faces, in the order every cubemap texture uses, and it is
/// reinterpreted as a cube once it has loaded. `brightness` scales the samples into the units the
/// rest of the scene is lit in, which are candelas per square metre, so the useful numbers are in
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
                        .push(handle);
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
/// The sun is whichever directional light is in the scene, so the sky is scattered from its
/// direction and color, so moving that light moves the sun and a scene without one gets a
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

                let atmosphere = Atmosphere::earth(media.add(medium));

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

                // `AtmosphereSettings` requires `Hdr`, and Bevy's insert brings it, but a camera
                // that had it removed would otherwise keep drawing without one.
                world.entity_mut(entity).insert((Hdr, settings));
                status::OK
            })
        }
    })
}

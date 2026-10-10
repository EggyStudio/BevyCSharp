//! The grade, the exposure and the lens effects a camera draws through.

use crate::interop::{status, BcsEffectsConfig};

#[cfg(feature = "render")]
use super::{asked_for_motion, keep_traced};
#[cfg(feature = "render")]
use crate::render::{image_handle, refuse_unless_camera};
#[cfg(feature = "render")]
use crate::state::with_world;

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
/// alternative to it. Bevy's own default matches what a Blender scene assumes; sunlight is around
/// 15, an overcast day around 12 and an interior around 7, so a scene lit in physical units and
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
/// The same three numbers a photographer sets, which suit a scene lit in real units. `aperture` is
/// the f-stop, `shutter` the shutter speed in seconds, and `sensitivity` the ISO. Bevy works the
/// EV-100 out from them, so this and [`bcs_render_set_exposure`] set the same thing two ways, and a
/// lens's depth of field is described by the same numbers.
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

/// Sets the lens effects a camera draws through.
///
/// Beside [`bcs_render_set_post`](super::bcs_render_set_post) rather than part of it, because that call is the pipeline a
/// settings screen owns, and a scene sets these for a moment. The same rule holds, so a config is
/// the whole set rather than one change to it and an effect left off is taken off the camera.
///
/// Depth of field needs a perspective camera, because focus has no meaning without one, and Bevy
/// drops the effect rather than reporting it. Auto exposure needs compute shaders, which every
/// desktop backend has and WebGL2 does not, and a high dynamic range target, which it brings with
/// it. That target belongs to [`bcs_render_set_post`](super::bcs_render_set_post), so a later call there without `hdr` takes
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
                        bevy::curve::cubic_splines::LinearSpline::new((0..points).map(|i| {
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
                        keep_traced(&mut entity_mut);
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
                    // Its pass is taken off the camera's view in the render world by
                    // `crate::render::exposure`, which Bevy leaves running once the effect is gone.
                    entity_mut.remove::<AutoExposure>();
                }

                status::OK
            })
        }
    })
}

//! How a camera's scene is lit: deferred or forward, contact shadows, screen-space reflections, the
//! ambient light and its occlusion, sorted transparency and the sky's atmosphere.

use crate::interop::{status, BcsAtmosphereConfig, BcsReflectionConfig};

#[cfg(feature = "render")]
use super::{keep_traced, RequestedPrepass};
#[cfg(feature = "render")]
use crate::render::refuse_unless_camera;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Draws Bevy's own opaque materials deferred, into a G-buffer lit afterward, or forward, lit as they
/// are drawn, which is the default.
///
/// Screen-space reflections read the deferred G-buffer, and deferred makes many lights cheap. It
/// needs a camera drawn once a pixel, and it applies to Bevy's own materials only, since a material
/// a Slang program draws is always forward, since it writes its color rather than a surface
/// description. Every Bevy material is prepared again, which moves one already drawn to the other
/// method.
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

/// Whether Bevy's materials are drawn deferred in this app, because a camera asked for the G-buffer
/// or because Solari, which draws every material that way, is running.
#[cfg(feature = "render")]
pub fn materials_deferred(world: &bevy::ecs::world::World) -> bool {
    world.get_resource::<Deferred>().is_some_and(|deferred| deferred.0) || crate::render::solari::running()
}

/// Switches Bevy's own materials to deferred or forward, preparing every one of them again.
#[cfg(feature = "render")]
pub(super) fn set_deferred(world: &mut bevy::ecs::world::World, on: bool) {
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

/// Turns contact shadows on a camera on, or off where `steps` is zero.
///
/// A contact shadow is traced from each pixel toward a light through the depth buffer, a short
/// way, for the shadow a shadow map is too coarse to hold where two things touch. Only lights
/// spawned with contact shadows cast them. The camera draws depth before the scene for it, which
/// the component asks for itself. `thickness` is how thick a surface in the depth buffer is taken
/// to be, and `length` how far a ray goes, both in world units.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_contact_shadows(camera: u64, steps: u32, thickness: f32, length: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, steps, thickness, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::pbr::ContactShadows;

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut camera = world.entity_mut(entity);

                if steps == 0 {
                    camera.remove::<ContactShadows>();
                } else {
                    camera.insert(ContactShadows {
                        linear_steps: steps,
                        thickness: thickness.max(0.0),
                        length: length.max(0.0),
                    });
                }

                status::OK
            })
        }
    })
}

/// Turns Bevy's screen-space reflections on a camera on, with `config`, or off where it is null.
///
/// Reflections are traced against the depth buffer and read the lit picture, so they show only what
/// is on screen, fading out at its edges, on surfaces smoother than the roughness ranges say. They
/// read Bevy's deferred G-buffer, so turning them on also draws Bevy's own materials deferred (see
/// [`bcs_render_set_deferred`]), and asks the camera for the depth and deferred prepasses. They
/// work on a camera drawn once a pixel.
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

                        keep_traced(&mut camera);
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

/// Sets the ambient light, which lights a surface from every direction at once and which ambient
/// occlusion darkens.
///
/// `camera` names a camera to give its own, or is zero for the one every camera without its own
/// uses. `brightness` is in candela per square meter, the unit of Bevy's lights, and a negative one
/// on a camera takes that camera's own away again.
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
/// that writes it at [`crate::render::views::FramePoint::AfterPrepass`] replaces Bevy's answer with its
/// own before anything is lit. Bevy computes its own first either way, so a camera replacing it
/// sets the lowest quality.
///
/// It needs depth and normals, which it asks for itself, and a camera drawn once a pixel, since
/// Bevy leaves it off on a multisampled camera, with a warning.
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

                        keep_traced(&mut camera);
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
                    // Bevy's own reach, the distance it samples over, which 0.20 made a setting.
                    ..ScreenSpaceAmbientOcclusion::default()
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

                // Earth's air. Mars is the other medium Bevy ships and it is not offered here,
                // because its dust phase comes from a texture the caller would have to supply, and
                // one that is not supplied leaves a sky that cannot be built at all.
                let density = if config.density > 0.0 { config.density } else { 1.0 };
                let medium = ScatteringMedium::earth(256, 256).with_density_multiplier(density);

                let Some(mut media) = world.get_resource_mut::<Assets<ScatteringMedium>>() else {
                    return status::INVALID_STATE;
                };

                let mut atmosphere = Atmosphere::earth(media.add(medium));

                // How much light the ground bounces back into the air, which makes the underside of
                // a cloud bright over snow and dark over sea.
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

//! The sky a planet's air scatters, earth's or Mars's, and the disk of the sun a directional light
//! stands for in it.

use crate::interop::{status, BcsAtmosphereConfig};

#[cfg(feature = "render")]
use crate::render::refuse_unless_camera;
#[cfg(feature = "render")]
use crate::state::with_world;

/// The planet [`bcs_render_set_atmosphere`] reads for `planet` 1, whose air is Mars's.
#[cfg(feature = "render")]
const MARS: i32 = 1;

/// Draws the sky a planet's air scatters, seen from `camera`.
///
/// Two things make a sky, a planet, which is an entity the size of a world with the air described
/// on it, and a camera told to sample it. This call keeps at most one planet in the world and
/// points the camera at it, because a scene has one sky and a second planet would be picked
/// between by distance rather than by intent.
///
/// The sun is whichever directional light is in the scene, so the sky is scattered from its
/// direction and color, so moving that light moves the sun and a scene without one gets a
/// night sky.
///
/// The planet is meters across, earth's or Mars's, and placed so its ground sits at the origin
/// whichever it is, and a scene measured in something other than meters says so with `scale`
/// rather than by moving anything. Mars's dust scatters light by a picture of its phase the caller
/// loads, Bevy's `textures/mars_mie_phase.ktx2`, and a sky of Mars without one is refused with
/// [`status::NULL_ARG`], since there would be no sky to build at all.
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
            use bevy::pbr::{AtmosphereMode, AtmosphereSettings};

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };
            let entity = crate::ecs::entity_from(camera);

            if !(0..=MARS).contains(&config.planet) || !(0..=1).contains(&config.method) {
                return status::NULL_ARG;
            }

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

                // Mars's dust scatters by the picture of its phase, which has to be there before
                // the medium is made.
                let dust_phase = if config.planet == MARS {
                    let phase = crate::assets::clone_handle(world, config.dust_phase)
                        .and_then(|handle| handle.try_typed::<bevy::image::Image>().ok());
                    let Some(phase) = phase else {
                        return status::NULL_ARG;
                    };
                    Some(phase)
                } else {
                    None
                };

                let density = if config.density > 0.0 { config.density } else { 1.0 };
                let medium = match dust_phase {
                    Some(phase) => ScatteringMedium::mars(256, 256, phase),
                    None => ScatteringMedium::earth(256, 256),
                }
                .with_density_multiplier(density);

                let Some(mut media) = world.get_resource_mut::<Assets<ScatteringMedium>>() else {
                    return status::INVALID_STATE;
                };
                let medium = media.add(medium);

                let mut atmosphere = if config.planet == MARS {
                    Atmosphere::mars(medium)
                } else {
                    Atmosphere::earth(medium)
                };

                // How much light the ground bounces back into the air, which makes the underside of
                // a cloud bright over snow and dark over sea.
                if config.ground_albedo > 0.0 {
                    atmosphere.ground_albedo = Vec3::splat(config.ground_albedo);
                }

                let scale = if config.scale > 0.0 { config.scale } else { 1.0 };
                let placed = planet_transform(atmosphere.inner_radius, scale);

                // One planet, so the existing one is rewritten rather than joined by another, since
                // Bevy renders whichever is nearest and two would be a coin toss. It is placed every
                // time, since Bevy places a planet only as it first arrives, and Mars after earth
                // would keep earth's center, deep under a smaller planet's ground.
                let existing = world
                    .query_filtered::<Entity, With<Atmosphere>>()
                    .iter(world)
                    .next();

                match existing {
                    Some(planet) => {
                        world.entity_mut(planet).insert((atmosphere, placed));
                    }
                    None => {
                        world.spawn((atmosphere, placed));
                    }
                }

                let mut settings = AtmosphereSettings::default();
                if config.haze_distance > 0.0 {
                    settings.aerial_view_lut_max_distance = config.haze_distance;
                }

                // From lookup textures, Bevy's own and quick for a scene inside the air, or by
                // marching rays through it, slower and right from far off and for the sharp shadows
                // of fog and clouds.
                settings.rendering_method = if config.method == 1 {
                    AtmosphereMode::Raymarched
                } else {
                    AtmosphereMode::LookupTexture
                };

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

/// Where a planet of `inner_radius` meters sits, `scale` times its size, for its ground to be at
/// the origin under the scene, its center straight below by the radius it has in the world.
#[cfg(feature = "render")]
fn planet_transform(inner_radius: f32, scale: f32) -> bevy::transform::components::Transform {
    use bevy::math::Vec3;

    bevy::transform::components::Transform::from_translation(-Vec3::Y * inner_radius * scale)
        .with_scale(Vec3::splat(scale))
}

/// Gives the directional light `light` the disk of the sun it stands for, `angular_size` radians
/// across as seen from the scene and `intensity` times as bright as Bevy draws one, which an
/// atmosphere draws in its sky where the light comes from. Bevy draws earth's sun for a light with
/// none, so a sky of Mars, whose sun looks smaller, sets Mars's, and a size and intensity of zero
/// draw none.
///
/// Answers [`status::INVALID_STATE`] for an entity that is not a directional light, since Bevy's
/// disk would make it one, and [`status::NULL_ARG`] for a size or an intensity below zero.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_sun_disk(light: u64, angular_size: f32, intensity: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (light, angular_size, intensity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::{DirectionalLight, SunDisk};

            if !(angular_size >= 0.0 && intensity >= 0.0) {
                return status::NULL_ARG;
            }

            let entity = crate::ecs::entity_from(light);
            with_world(|world| {
                let Ok(mut light) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };
                if !light.contains::<DirectionalLight>() {
                    return status::INVALID_STATE;
                }

                light.insert(SunDisk { angular_size, intensity });
                status::OK
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use crate::interop::BcsAtmosphereConfig;

    /// The fields the managed side writes, at the places it writes them.
    #[test]
    fn the_config_is_laid_out_as_the_managed_side_writes_it() {
        assert_eq!(core::mem::offset_of!(BcsAtmosphereConfig, ground_albedo), 20);
        assert_eq!(core::mem::offset_of!(BcsAtmosphereConfig, planet), 24);
        assert_eq!(core::mem::offset_of!(BcsAtmosphereConfig, dust_phase), 28);
        assert_eq!(core::mem::offset_of!(BcsAtmosphereConfig, method), 32);
        assert_eq!(core::mem::size_of::<BcsAtmosphereConfig>(), 36);
    }

    /// Earth and Mars each sit with their ground at the origin, at any scale.
    #[cfg(feature = "render")]
    #[test]
    fn a_planet_sits_with_its_ground_at_the_origin() {
        for (radius, scale) in [(6_360_000.0, 1.0), (3_389_500.0, 1.0), (3_389_500.0, 0.001)] {
            let placed = super::planet_transform(radius, scale);
            let top = placed.translation.y + radius * placed.scale.y;
            assert!(top.abs() < radius * scale * 1e-6, "the ground is at {top} for {radius} at {scale}");
        }
    }
}

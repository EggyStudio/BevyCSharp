//! Lights and their shadows, a light spawned, the shadow maps' sizes, a directional light's
//! cascades, a spot light's cookie, soft shadows and how a camera filters them.

use crate::interop::{status, BcsLightConfig};

#[cfg(feature = "render")]
use crate::state::{with_world, with_world_opt};

/// Spawns a light and returns its entity, or `0` on a headless build.
///
/// Position and aim it by writing its `Transform`; a directional or spot light shines down its own
/// negative Z, which `Transform.LookingAt` produces.
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
            let shadows = config.shadows & 1 != 0;
            // Traced a short way through the depth buffer, for the small shadows a map misses where
            // things touch, on a camera that asks for them with `bcs_render_set_contact_shadows`.
            let contact = config.shadows & 2 != 0;

            with_world_opt(|world| match config.kind {
                0 => world
                    .spawn((
                        DirectionalLight {
                            color,
                            illuminance: config.intensity,
                            shadow_maps_enabled: shadows,
                            contact_shadows_enabled: contact,
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
                            contact_shadows_enabled: contact,
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
                            contact_shadows_enabled: contact,
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

/// Sets how a directional light divides its shadows across the distance.
///
/// A directional light covers the whole scene, so one shadow map stretched over all of it is
/// coarse near the camera where it is looked at closest. Cascades split the range into a few maps,
/// each covering a nearer and smaller slice, which is why a shadow that looks blocky at arm's
/// length is a cascade setting rather than a resolution one.
///
/// `cascades` is how many maps to split it into, `minimum` and `maximum` are the nearest and
/// furthest distances that receive a shadow at all, `first_bound` is where the first cascade ends,
/// and `overlap` is how much of each cascade is blended into the next, as a proportion. A zero in
/// any of them keeps Bevy's own number for it.
///
/// Returns [`status::NO_COMPONENT`] where the entity is gone or is not a directional light.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_shadow_cascades(
    light: u64,
    cascades: i32,
    minimum: f32,
    maximum: f32,
    first_bound: f32,
    overlap: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (light, cascades, minimum, maximum, first_bound, overlap);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::{CascadeShadowConfigBuilder, DirectionalLight};

            crate::state::with_world(|world| {
                let entity = bevy::ecs::entity::Entity::from_bits(light);

                // The component says this is the kind of light cascades mean anything to, so a
                // point light asked for them is refused rather than silently given a component
                // nothing reads.
                if world.get::<DirectionalLight>(entity).is_none() {
                    return status::NO_COMPONENT;
                }

                let stock = CascadeShadowConfigBuilder::default();

                let config = CascadeShadowConfigBuilder {
                    num_cascades: if cascades > 0 {
                        (cascades as usize).min(4)
                    } else {
                        stock.num_cascades
                    },
                    minimum_distance: if minimum > 0.0 {
                        minimum
                    } else {
                        stock.minimum_distance
                    },
                    maximum_distance: if maximum > 0.0 {
                        maximum
                    } else {
                        stock.maximum_distance
                    },
                    first_cascade_far_bound: if first_bound > 0.0 {
                        first_bound
                    } else {
                        stock.first_cascade_far_bound
                    },
                    overlap_proportion: if overlap > 0.0 {
                        overlap.clamp(0.0, 1.0)
                    } else {
                        stock.overlap_proportion
                    },
                }
                .build();

                world.entity_mut(entity).insert(config);

                status::OK
            })
        }
    })
}

/// Shapes a spot light's beam with a picture, the way a gobo shapes a stage light.
///
/// Only the red channel is read, so the image says how much light gets through and not what color
/// it is. Its border should be black, or the light leaks past the edge of the picture. A negative
/// `image` takes the shaping off again and leaves a plain cone.
///
/// Returns [`status::NO_COMPONENT`] where the entity is gone or is not a spot light.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_light_cookie(light: u64, image: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (light, image);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::{SpotLight, SpotLightTexture};

            let handle = if image >= 0 {
                match crate::state::with_world_opt(|world| {
                    crate::assets::clone_handle(world, image)
                })
                .flatten()
                {
                    Some(handle) => Some(handle.typed::<bevy::image::Image>()),
                    None => return status::NOT_PRESENT,
                }
            } else {
                None
            };

            crate::state::with_world(|world| {
                let entity = bevy::ecs::entity::Entity::from_bits(light);

                if world.get::<SpotLight>(entity).is_none() {
                    return status::NO_COMPONENT;
                }

                match handle {
                    Some(image) => {
                        world.entity_mut(entity).insert(SpotLightTexture { image });
                    }
                    None => {
                        world.entity_mut(entity).remove::<SpotLightTexture>();
                    }
                }

                status::OK
            })
        }
    })
}

/// Softens a light's shadow the farther it falls from what casts it, as a light of `size` world
/// units across does, or with a size of zero or less makes it hard again.
///
/// Percentage-closer soft shadows. The penumbra grows with the distance between the caster and the
/// surface, which a blur of fixed width cannot do. For a point or a spot light the size is its
/// radius, which this sets. It is noisy, so it suits a camera filtering its shadows temporally
/// with temporal antialiasing on (see [`bcs_render_set_shadow_filtering`]), and it costs a good
/// deal more than a hard shadow.
///
/// Returns [`status::NO_COMPONENT`] where the entity is not a light.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_soft_shadows(light: u64, size: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (light, size);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::{DirectionalLight, PointLight, SpotLight};

            let soft = size > 0.0;

            crate::state::with_world(|world| {
                let entity = bevy::ecs::entity::Entity::from_bits(light);
                let Ok(mut light) = world.get_entity_mut(entity) else {
                    return status::NO_COMPONENT;
                };

                if let Some(mut directional) = light.get_mut::<DirectionalLight>() {
                    directional.soft_shadow_size = soft.then_some(size);
                } else if let Some(mut point) = light.get_mut::<PointLight>() {
                    point.soft_shadows_enabled = soft;
                    if soft {
                        point.radius = size;
                    }
                } else if let Some(mut spot) = light.get_mut::<SpotLight>() {
                    spot.soft_shadows_enabled = soft;
                    if soft {
                        spot.radius = size;
                    }
                } else {
                    return status::NO_COMPONENT;
                }

                status::OK
            })
        }
    })
}

/// Sets how a camera filters the shadow maps it reads: `0` a hardware two-by-two, `1` a Gaussian,
/// which is Bevy's default, and `2` temporal, which varies the pattern each frame for temporal
/// antialiasing to average, and which soft shadows need to be quiet.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_shadow_filtering(camera: u64, method: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, method);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::light::ShadowFilteringMethod;

            let method = match method {
                0 => ShadowFilteringMethod::Hardware2x2,
                1 => ShadowFilteringMethod::Gaussian,
                2 => ShadowFilteringMethod::Temporal,
                _ => return status::NULL_ARG,
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                world.entity_mut(entity).insert(method);
                status::OK
            })
        }
    })
}

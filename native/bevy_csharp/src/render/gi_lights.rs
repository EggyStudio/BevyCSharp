//! The lights global illumination's probes are lit by, which the managed side reads each frame and
//! writes into the buffer the probes' compute shaders read as `bcs_gi`'s `GiLights`.
//!
//! Each light is given as the light a white surface facing it returns, as 3DEngine's renderer gives
//! the probes a light, which in Bevy's units is a directional light's illuminance over pi, a point or
//! spot light's intensity, Bevy's luminous power over four pi, over pi again, and an ambient light's
//! brightness times the share Bevy's ambient term returns ([`AMBIENT_RETURNED`]), each times the
//! camera's exposure, as Bevy's lighting applies it before the picture's half floats. The probes'
//! light then lies in the units of the picture they are added to, where a sun of a hundred thousand
//! lux taken unexposed would pass the largest half float.

use crate::interop::status;

/// The most point and spot lights the probes are lit by, the nearest the camera.
pub const MOST_LIGHTS: usize = 16;

/// The share of an ambient light's brightness Bevy's lighting returns from a white surface, its
/// diffuse share as its ambient term works it out, `EnvBRDFApprox` at a perceptual roughness of one,
/// 0.4524 of the light less 0.0024. The light from all around is given as that, as the sun and the
/// lamps are given as what a white surface facing them returns, so a floor under an open sky reads
/// with global illumination as it reads under Bevy's ambient light alone.
pub const AMBIENT_RETURNED: f32 = 0.45;

/// A point or spot light, as `bcs_gi`'s `GiLight` lays it out.
#[repr(C)]
#[derive(Clone, Copy, Default, Debug)]
pub struct BcsGiLight {
    /// xyz: where it is. w: 1 for a point light, 2 for a spot light.
    pub position_and_kind: [f32; 4],
    /// xyz: the way a spot light points. w: how far it reaches.
    pub direction_and_range: [f32; 4],
    /// rgb: the light a white surface facing it a unit away returns, exposed. w: 1 where it casts
    /// shadows, which the probes' rays march toward.
    pub color_and_shadow: [f32; 4],
    /// x: a spot light's scale and y its offset, which Bevy takes the cosine of the way toward a
    /// point times and adds before squaring, its falloff from the inner cone to the outer. zw: unused.
    pub cone: [f32; 4],
}

/// The sun, the light from all around and the point and spot lights, as [`bcs_gi_lights`] writes
/// them, laid out as the start of `bcs_gi`'s `GiLights`.
#[repr(C)]
#[derive(Clone, Copy, Default, Debug)]
pub struct BcsGiLights {
    /// xyz: the way toward the sun. w: 1 where there is a sun.
    pub sun_direction: [f32; 4],
    /// rgb: the light a white surface facing the sun returns, exposed. w: 1 where it casts shadows.
    pub sun_color: [f32; 4],
    /// rgb: the light from all around, which a ray meeting nothing brings back, exposed.
    pub sky: [f32; 4],
    /// x: how many lights. yzw: unused, which the managed side fills.
    pub counts: [f32; 4],
    pub lights: [BcsGiLight; MOST_LIGHTS],
}

/// Writes the lights the probes are lit by to `out`, the sun, a directional light that casts
/// shadows before one that does not, the ambient light of the camera the field lies around, or
/// the world's where the camera has none, and the point and spot lights nearest that camera.
///
/// # Safety
/// `out` must be writable for a [`BcsGiLights`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gi_lights(out: *mut BcsGiLights) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| {
            let lights = gather(world);
            unsafe { out.write(lights) };
            status::OK
        })
    })
}

#[cfg(feature = "render")]
fn gather(world: &mut bevy::ecs::world::World) -> BcsGiLights {
    use bevy::camera::Exposure;
    use bevy::camera::visibility::InheritedVisibility;
    use bevy::color::ColorToComponents;
    use bevy::light::{AmbientLight, DirectionalLight, GlobalAmbientLight, PointLight, SpotLight};
    use bevy::math::Vec3;
    use bevy::transform::components::GlobalTransform;
    use core::f32::consts::PI;

    let mut lights = BcsGiLights::default();
    let view = crate::render::scene_counts::view_camera(world);
    let (eye, exposure) = match view {
        Some((camera, place, ..)) => (place, world.get::<Exposure>(camera).copied().unwrap_or_default().exposure()),
        None => (Vec3::ZERO, Exposure::default().exposure()),
    };
    let shown = |visibility: Option<&InheritedVisibility>| visibility.is_none_or(|visibility| visibility.get());

    // The sun, one that casts shadows before one that does not.
    let mut suns = world.query::<(&DirectionalLight, &GlobalTransform, Option<&InheritedVisibility>)>();
    if let Some((sun, place, _)) =
        suns.iter(world).filter(|(_, _, visibility)| shown(*visibility)).max_by_key(|(light, ..)| light.shadow_maps_enabled)
    {
        let color = sun.color.to_linear().to_vec3() * (sun.illuminance / PI * exposure);
        lights.sun_direction = place.back().as_vec3().extend(1.0).to_array();
        lights.sun_color = color.extend(if sun.shadow_maps_enabled { 1.0 } else { 0.0 }).to_array();
    }

    // The camera's ambient light, or the world's, as much of it as Bevy's lighting returns from a
    // white surface, the light from all around in the same units.
    let ambient = view.and_then(|(camera, ..)| world.get::<AmbientLight>(camera).map(|light| (light.color, light.brightness)));
    let (color, brightness) = ambient
        .or_else(|| world.get_resource::<GlobalAmbientLight>().map(|light| (light.color, light.brightness)))
        .unwrap_or((bevy::color::Color::BLACK, 0.0));
    lights.sky = (color.to_linear().to_vec3() * (brightness * AMBIENT_RETURNED * exposure)).extend(0.0).to_array();

    // The point and spot lights, the nearest the camera first, as Bevy gives them its lighting.
    let mut found: Vec<(f32, BcsGiLight)> = Vec::new();
    let mut points = world.query::<(&PointLight, &GlobalTransform, Option<&InheritedVisibility>)>();
    for (light, place, visibility) in points.iter(world) {
        if !shown(visibility) {
            continue;
        }
        let candela = light.intensity / (4.0 * PI);
        let at = place.translation();
        found.push((
            at.distance_squared(eye),
            BcsGiLight {
                position_and_kind: at.extend(1.0).to_array(),
                direction_and_range: [0.0, 0.0, 0.0, light.range],
                color_and_shadow: (light.color.to_linear().to_vec3() * (candela / PI * exposure))
                    .extend(if light.shadow_maps_enabled { 1.0 } else { 0.0 })
                    .to_array(),
                cone: [0.0; 4],
            },
        ));
    }
    let mut spots = world.query::<(&SpotLight, &GlobalTransform, Option<&InheritedVisibility>)>();
    for (light, place, visibility) in spots.iter(world) {
        if !shown(visibility) {
            continue;
        }
        let candela = light.intensity / (4.0 * PI);
        let at = place.translation();
        let (inner, outer) = (light.inner_angle.cos(), light.outer_angle.cos());
        let scale = 1.0 / (inner - outer).max(0.001);
        found.push((
            at.distance_squared(eye),
            BcsGiLight {
                position_and_kind: at.extend(2.0).to_array(),
                direction_and_range: place.forward().as_vec3().extend(light.range).to_array(),
                color_and_shadow: (light.color.to_linear().to_vec3() * (candela / PI * exposure))
                    .extend(if light.shadow_maps_enabled { 1.0 } else { 0.0 })
                    .to_array(),
                cone: [scale, -outer * scale, 0.0, 0.0],
            },
        ));
    }
    found.sort_by(|a, b| a.0.total_cmp(&b.0));
    for (slot, (_, light)) in lights.lights.iter_mut().zip(found.iter()) {
        *slot = *light;
    }
    lights.counts[0] = found.len().min(MOST_LIGHTS) as f32;
    lights
}

#[cfg(test)]
mod tests {
    use super::{BcsGiLight, BcsGiLights};

    /// The offsets the managed side writes out and the shaders read, which a field moved here must
    /// move there too.
    #[test]
    fn the_lights_lie_where_the_shaders_read_them() {
        assert_eq!(std::mem::size_of::<BcsGiLight>(), 64);
        assert_eq!(std::mem::offset_of!(BcsGiLights, sky), 32);
        assert_eq!(std::mem::offset_of!(BcsGiLights, counts), 48);
        assert_eq!(std::mem::offset_of!(BcsGiLights, lights), 64);
        assert_eq!(std::mem::size_of::<BcsGiLights>(), 64 + 16 * 64);
    }
}

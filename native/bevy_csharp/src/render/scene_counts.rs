//! What the debug overlay reads of the scene, the meshes some view sees and their triangles, the
//! lights by kind with those casting shadow maps, and the camera the view is drawn by, its place,
//! the way it faces and its field of view.
//!
//! Counted in the game's world, where the meshes' data and the lights' settings are, from the
//! visibility Bevy worked out for the last frame, a few times a second while the overlay shows.
//! A mesh whose data the game's world does not keep, one made for the render world alone, is
//! counted with no triangles.

use crate::interop::status;

/// The scene's counts and its camera, as [`bcs_render_scene_counts`] writes them.
///
/// The offsets are fixed and written out on the managed side, which a test of both sides holds to
/// the same numbers.
#[repr(C)]
#[derive(Clone, Copy, Default, Debug)]
pub struct BcsSceneCounts {
    /// 3D meshes some view saw in the last frame.
    pub meshes: u32,
    /// Nothing, so the triangles start on eight bytes.
    pub _pad: u32,
    /// Their triangles, a list's indices or vertices in threes and a strip's less two.
    pub triangles: u64,
    /// Directional lights.
    pub directional: u32,
    /// Point lights.
    pub point: u32,
    /// Spot lights.
    pub spot: u32,
    /// Lights of the three kinds casting shadow maps.
    pub shadowed: u32,
    /// The camera the view is drawn by, its bits, or zero where there is none.
    pub camera: u64,
    /// Its vertical field of view in radians, zero for a projection that has none.
    pub fov: f32,
    /// Where it is.
    pub position: [f32; 3],
    /// The way it faces, a length of one.
    pub forward: [f32; 3],
    /// Nothing, so the struct is a whole number of eight bytes.
    pub _end: u32,
}

/// Writes the scene's counts and its camera to `out`. The camera is the active one of the highest
/// order, a 3D one before any other, which is the one a window shows on top.
///
/// # Safety
/// `out` must be writable for a [`BcsSceneCounts`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_scene_counts(out: *mut BcsSceneCounts) -> i32 {
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
            let counts = count(world);
            unsafe { out.write(counts) };
            status::OK
        })
    })
}

#[cfg(feature = "render")]
fn count(world: &mut bevy::ecs::world::World) -> BcsSceneCounts {
    use bevy::asset::Assets;
    use bevy::camera::visibility::ViewVisibility;
    use bevy::camera::{Camera, Camera3d, Projection};
    use bevy::light::{DirectionalLight, PointLight, SpotLight};
    use bevy::mesh::{Mesh, Mesh3d, PrimitiveTopology};
    use bevy::transform::components::GlobalTransform;

    let mut counts = BcsSceneCounts::default();

    let mut meshes = world.query::<(&Mesh3d, &ViewVisibility)>();
    let data = world.get_resource::<Assets<Mesh>>();
    for (mesh, visibility) in meshes.iter(world) {
        if !visibility.get() {
            continue;
        }

        counts.meshes += 1;
        let Some(mesh) = data.and_then(|data| data.get(&mesh.0)) else {
            continue;
        };

        // A mesh whose data went to the render world keeps a husk here, which the fallible reads
        // answer for rather than panicking as the others do.
        let corners = match mesh.try_indices_option() {
            Ok(Some(indices)) => indices.len(),
            Ok(None) => match mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) {
                Ok(Some(positions)) => positions.len(),
                _ => continue,
            },
            Err(_) => continue,
        } as u64;
        counts.triangles += match mesh.primitive_topology() {
            PrimitiveTopology::TriangleList => corners / 3,
            PrimitiveTopology::TriangleStrip => corners.saturating_sub(2),
            _ => 0,
        };
    }

    let mut directional = world.query::<&DirectionalLight>();
    for light in directional.iter(world) {
        counts.directional += 1;
        counts.shadowed += u32::from(light.shadow_maps_enabled);
    }

    let mut point = world.query::<&PointLight>();
    for light in point.iter(world) {
        counts.point += 1;
        counts.shadowed += u32::from(light.shadow_maps_enabled);
    }

    let mut spot = world.query::<&SpotLight>();
    for light in spot.iter(world) {
        counts.spot += 1;
        counts.shadowed += u32::from(light.shadow_maps_enabled);
    }

    // The active camera of the highest order, a 3D one first, as a window shows the last drawn on
    // top and a game's interface camera, a 2D one, draws over its world's.
    let mut cameras = world.query::<(bevy::ecs::entity::Entity, &Camera, Option<&Camera3d>, &GlobalTransform, Option<&Projection>)>();
    let chosen = cameras
        .iter(world)
        .filter(|(_, camera, ..)| camera.is_active)
        .max_by_key(|(_, camera, three, ..)| (three.is_some(), camera.order));

    if let Some((entity, _, _, place, projection)) = chosen {
        counts.camera = entity.to_bits();
        counts.position = place.translation().to_array();
        counts.forward = place.forward().as_vec3().to_array();
        counts.fov = match projection {
            Some(Projection::Perspective(perspective)) => perspective.fov,
            _ => 0.0,
        };
    }

    counts
}

#[cfg(test)]
mod tests {
    use super::BcsSceneCounts;

    /// The offsets the managed side writes out, which a field moved here must move there too.
    #[test]
    fn the_counts_lie_where_the_managed_side_reads_them() {
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, triangles), 8);
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, directional), 16);
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, shadowed), 28);
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, camera), 32);
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, fov), 40);
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, position), 44);
        assert_eq!(std::mem::offset_of!(BcsSceneCounts, forward), 56);
        assert_eq!(std::mem::size_of::<BcsSceneCounts>(), 72);
    }
}

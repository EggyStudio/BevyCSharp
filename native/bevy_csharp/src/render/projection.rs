//! A camera projection of a game's own, as Bevy's custom_projection declares one.
//!
//! Bevy takes a projection of a game's own as a Rust type implementing its `CameraProjection`,
//! which C# cannot declare. What such a projection most often is, and Bevy's example is, is Bevy's
//! perspective with some of its matrix changed, an oblique one shearing the view as a portal or a
//! water's reflection does, or an off-axis one for a screen seen from aside. So the bridge declares
//! that one projection, Bevy's perspective with entries of its clip-from-view matrix set by the
//! game, which keeps following the window's aspect as a perspective does.

use crate::interop::status;

/// Bevy's perspective with the game's entries written over its matrix, each by its column and row.
#[cfg(feature = "render")]
#[derive(Debug, Clone)]
struct AdjustedPerspective {
    perspective: bevy::camera::PerspectiveProjection,
    entries: Vec<(usize, usize, f32)>,
}

#[cfg(feature = "render")]
impl AdjustedPerspective {
    fn adjusted(&self, mut matrix: bevy::math::Mat4) -> bevy::math::Mat4 {
        for &(column, row, value) in &self.entries {
            matrix.col_mut(column)[row] = value;
        }
        matrix
    }
}

#[cfg(feature = "render")]
impl bevy::camera::CameraProjection for AdjustedPerspective {
    fn get_clip_from_view(&self) -> bevy::math::Mat4 {
        self.adjusted(self.perspective.get_clip_from_view())
    }

    fn get_clip_from_view_for_sub(&self, sub_view: &bevy::camera::SubCameraView) -> bevy::math::Mat4 {
        self.adjusted(self.perspective.get_clip_from_view_for_sub(sub_view))
    }

    fn update(&mut self, width: f32, height: f32) {
        self.perspective.update(width, height);
    }

    fn far(&self) -> f32 {
        self.perspective.far
    }

    fn get_frustum_corners(&self, z_near: f32, z_far: f32) -> [bevy::math::Vec3A; 8] {
        self.perspective.get_frustum_corners(z_near, z_far)
    }
}

/// Gives `camera` Bevy's perspective of `fov` radians from `near` to `far`, with the `count`
/// entries at `entries` written over its clip-from-view matrix, each three floats, its column, its
/// row and its value, as Bevy's oblique projection writes column 2's first two rows.
///
/// A column or row past 3 is refused with [`status::NULL_ARG`], and an entity that is no camera
/// with [`status::NO_COMPONENT`].
///
/// # Safety
/// `entries` must be readable for `count` times three floats, or null where `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_adjusted_perspective(
    camera: u64,
    fov: f32,
    near: f32,
    far: f32,
    entries: *const f32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, fov, near, far, entries, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::{Camera, PerspectiveProjection, Projection};

            if count < 0 || (entries.is_null() && count > 0) {
                return status::NULL_ARG;
            }
            let raw = unsafe { crate::interop::opt_slice(entries, count * 3) };
            let mut adjusted = Vec::with_capacity(count as usize);
            for entry in raw.chunks_exact(3) {
                let (column, row) = (entry[0] as usize, entry[1] as usize);
                if column > 3 || row > 3 || entry[0] < 0.0 || entry[1] < 0.0 {
                    return status::NULL_ARG;
                }
                adjusted.push((column, row, entry[2]));
            }

            crate::state::with_world(|world| {
                let Ok(mut entity) = world.get_entity_mut(crate::ecs::entity_from(camera)) else {
                    return status::NO_ENTITY;
                };
                if !entity.contains::<Camera>() {
                    return status::NO_COMPONENT;
                }

                let perspective = PerspectiveProjection { fov, near, far, ..Default::default() };
                entity.insert(Projection::custom(AdjustedPerspective { perspective, entries: adjusted }));
                status::OK
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::camera::{CameraProjection, PerspectiveProjection};

    use super::AdjustedPerspective;

    /// The entries are written over the perspective's own matrix, which keeps every other entry
    /// and follows the aspect a resize gives it, as Bevy's oblique projection does.
    #[test]
    fn the_entries_are_written_over_the_perspectives_matrix() {
        let mut projection = AdjustedPerspective { perspective: PerspectiveProjection::default(), entries: vec![(2, 0, 0.2), (2, 1, 0.6)] };
        projection.update(1280.0, 720.0);

        let mut plain = PerspectiveProjection::default();
        plain.update(1280.0, 720.0);
        let (adjusted, plain) = (projection.get_clip_from_view(), plain.get_clip_from_view());

        assert_eq!((adjusted.col(2)[0], adjusted.col(2)[1]), (0.2, 0.6));
        assert_eq!(adjusted.col(0), plain.col(0));
        assert_eq!(adjusted.col(2)[3], plain.col(2)[3]);
    }
}

//! Debug drawing, reachable from C#.
//!
//! Bevy draws gizmos through a `Gizmos` system parameter, which a C# system cannot hold: every
//! managed system is an exclusive one, handed the whole world rather than a set of parameters. So
//! calls from C# are recorded in a queue, and one ordinary Bevy system drains it each frame with
//! the real parameter in hand.
//!
//! Gizmos are immediate: what is drawn lasts one frame, so a shape that should stay on screen has
//! to be asked for again every frame. That is what makes them useful for watching a value change
//! and useless for building anything.

use crate::interop::{status, BcsGizmoConfig};

/// One recorded draw call, waiting for the frame's drain.
#[derive(Clone, Copy)]
pub struct QueuedGizmo {
    /// `0` line, `1` sphere, `2` axes.
    pub kind: i32,
    /// Line start, sphere centre, or the position axes are drawn at.
    pub start: [f32; 3],
    /// Line end. Unused by the other two.
    pub end: [f32; 3],
    /// Orientation for a sphere or a set of axes, as a quaternion.
    pub rotation: [f32; 4],
    /// Sphere radius, or the length of each axis.
    pub radius: f32,
    /// Colour, linear RGBA. Axes use their own red, green and blue.
    pub color: [f32; 4],
}

/// What C# has asked to be drawn this frame.
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct GizmoQueue(pub Vec<QueuedGizmo>);

/// Draws everything the queue holds, then empties it.
#[cfg(feature = "render")]
pub fn drain(mut queue: bevy::ecs::system::ResMut<GizmoQueue>, mut gizmos: bevy::gizmos::gizmos::Gizmos) {
    use bevy::color::Color;
    use bevy::math::{Isometry3d, Quat, Vec3};
    use bevy::transform::components::Transform;

    for shape in queue.0.drain(..) {
        let position = Vec3::new(shape.start[0], shape.start[1], shape.start[2]);
        let rotation = Quat::from_xyzw(
            shape.rotation[0],
            shape.rotation[1],
            shape.rotation[2],
            shape.rotation[3],
        );
        let color = Color::linear_rgba(
            shape.color[0],
            shape.color[1],
            shape.color[2],
            shape.color[3],
        );

        match shape.kind {
            1 => {
                gizmos.sphere(Isometry3d::new(position, rotation), shape.radius, color);
            }
            2 => {
                gizmos.axes(
                    Transform::from_translation(position).with_rotation(rotation),
                    shape.radius,
                );
            }
            _ => {
                let end = Vec3::new(shape.end[0], shape.end[1], shape.end[2]);
                gizmos.line(position, end, color);
            }
        }
    }
}

/// Puts every gizmo in front of the scene, once, as the app starts.
///
/// `depth_bias` is how a gizmo is moved towards or away from the camera before it is depth tested;
/// `-1` is as far towards it as the range allows, which is another way of saying that nothing in
/// the scene can hide it.
#[cfg(feature = "render")]
pub fn draw_in_front(mut store: bevy::ecs::system::ResMut<bevy::gizmos::config::GizmoConfigStore>) {
    let (config, _) = store.config_mut::<bevy::gizmos::config::DefaultGizmoConfigGroup>();
    config.depth_bias = -1.0;
}

/// Records one shape to draw this frame.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on: gizmos need the renderer
/// and a window, because the plugin that draws them comes with both.
///
/// # Safety
/// `config` must point to a readable [`BcsGizmoConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gizmo_draw(config: *const BcsGizmoConfig) -> i32 {
    crate::interop::guard(|| {
        if config.is_null() {
            return status::NULL_ARG;
        }
        let config = unsafe { *config };

        crate::state::with_world(|world| {
            let Some(mut queue) = world.get_resource_mut::<GizmoQueue>() else {
                return status::UNSUPPORTED;
            };

            queue.0.push(QueuedGizmo {
                kind: config.kind,
                start: config.start,
                end: config.end,
                rotation: config.rotation,
                radius: config.radius,
                color: config.color,
            });
            status::OK
        })
    })
}

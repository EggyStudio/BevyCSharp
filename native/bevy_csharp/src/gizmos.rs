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
    /// `0` line, `1` sphere, `2` axes, `3` a line fading from one colour to another.
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
    /// What a fading line reaches at its far end.
    pub end_color: [f32; 4],
    /// Whether the scene can hide it. `0` is depth tested; anything else draws over everything.
    pub in_front: i32,
}

/// What C# has asked to be drawn this frame.
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct GizmoQueue(pub Vec<QueuedGizmo>);

/// The group whose shapes nothing in the scene can hide.
///
/// Two groups, because a gizmo is asked for with one of two intentions and there is no third. A
/// handle, an outline or a marker is a control: it is drawn *about* the scene and has to be
/// reachable, so it wins the depth test outright and lives here. A grid, a path or a wireframe is
/// drawn *in* the scene and has to be behind what is in front of it, or it is not describing the
/// scene at all — that is the default group, left exactly as the engine set it up.
#[cfg(feature = "render")]
#[derive(Default, bevy::reflect::Reflect, bevy::gizmos::config::GizmoConfigGroup)]
pub struct FrontGizmos;

/// Draws everything the queue holds, then empties it.
#[cfg(feature = "render")]
pub fn drain(
    mut queue: bevy::ecs::system::ResMut<GizmoQueue>,
    mut behind: bevy::gizmos::gizmos::Gizmos,
    mut gizmos: bevy::gizmos::gizmos::Gizmos<FrontGizmos>,
) {
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
        let fades_to = Color::linear_rgba(
            shape.end_color[0],
            shape.end_color[1],
            shape.end_color[2],
            shape.end_color[3],
        );

        // Written twice rather than through a trait object: `Gizmos<T>` is a system parameter
        // and the two are different types, so the only thing they can share is the shape of the
        // call.
        if shape.in_front != 0 {
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
                3 => {
                    let end = Vec3::new(shape.end[0], shape.end[1], shape.end[2]);
                    gizmos.line_gradient(position, end, color, fades_to);
                }
                _ => {
                    let end = Vec3::new(shape.end[0], shape.end[1], shape.end[2]);
                    gizmos.line(position, end, color);
                }
            }

            continue;
        }

        match shape.kind {
            1 => {
                behind.sphere(Isometry3d::new(position, rotation), shape.radius, color);
            }
            2 => {
                behind.axes(
                    Transform::from_translation(position).with_rotation(rotation),
                    shape.radius,
                );
            }
            3 => {
                let end = Vec3::new(shape.end[0], shape.end[1], shape.end[2]);
                behind.line_gradient(position, end, color, fades_to);
            }
            _ => {
                let end = Vec3::new(shape.end[0], shape.end[1], shape.end[2]);
                behind.line(position, end, color);
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
    // Only this one is touched. The default group keeps the engine's own settings, which are what
    // anything drawn as part of the scene wants: depth tested, like the scene.
    let (config, _) = store.config_mut::<FrontGizmos>();
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
                end_color: config.end_color,
                in_front: config.in_front,
            });
            status::OK
        })
    })
}

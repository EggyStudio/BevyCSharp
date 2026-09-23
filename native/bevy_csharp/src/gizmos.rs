//! Debug drawing, reachable from C#.
//!
//! Bevy draws gizmos through a `Gizmos` system parameter, which a C# system cannot hold, because
//! every managed system is an exclusive one, handed the whole world rather than a set of
//! parameters. So calls from C# are recorded in a queue, and one ordinary Bevy system drains it
//! each frame with the real parameter in hand.
//!
//! Gizmos are immediate, so what is drawn lasts one frame and a shape that should stay on screen
//! has to be asked for again every frame. That is what makes them useful for watching a value
//! change and useless for building anything.

use crate::interop::{status, BcsGizmoConfig};

/// One recorded draw call, waiting for the frame's drain.
///
/// Every shape is described by the same handful of numbers, and which of them a shape reads is
/// what `kind` decides. A struct per shape would be a struct per arm of one match, and the
/// managed side would need a mirror of each.
#[derive(Clone, Copy)]
pub struct QueuedGizmo {
    /// Which shape. See [`crate::interop::BcsGizmoConfig::kind`] for the list.
    pub kind: i32,
    /// Where the shape sits: a line's start, or the centre of everything else.
    pub start: [f32; 3],
    /// A line's far end, and for the shapes that need two or three more numbers, whatever
    /// [`crate::interop::BcsGizmoConfig::end`] says they are.
    pub end: [f32; 3],
    /// Which way it faces, as a quaternion. A line and an arrow have two ends instead.
    pub rotation: [f32; 4],
    /// How large: a radius, an axis length, or a grid's cell spacing.
    pub radius: f32,
    /// Color, linear RGBA. Axes use their own red, green and blue.
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
/// handle, an outline or a marker is a control. It is drawn *about* the scene and has to be
/// reachable, so it wins the depth test outright and lives here. A grid, a path or a wireframe is
/// drawn *in* the scene and has to be behind what is in front of it, or it is not describing the
/// scene at all. That is the default group, left exactly as the engine set it up.
#[cfg(feature = "render")]
#[derive(Default, bevy::reflect::Reflect, bevy::gizmos::config::GizmoConfigGroup)]
pub struct FrontGizmos;

/// Draws one recorded shape, with whichever set of gizmos it belongs to.
///
/// A macro rather than a function, because `Gizmos` and `Gizmos<FrontGizmos>` are different types
/// and the only thing they share is the shape of the call. Written once all the same, because the
/// alternative is every new shape being added in two places and eventually in only one.
#[cfg(feature = "render")]
macro_rules! draw_shape {
    ($gizmos:expr, $shape:expr, $position:expr, $rotation:expr, $color:expr, $fades_to:expr) => {{
        use bevy::math::primitives::{Capsule3d, Cone, Cuboid, Cylinder, Torus};
        use bevy::math::{Isometry2d, Isometry3d, Rot2, UVec2, Vec2, Vec3};
        use bevy::transform::components::Transform;

        let shape = $shape;
        let position = $position;
        let rotation = $rotation;
        let far = Vec3::new(shape.end[0], shape.end[1], shape.end[2]);
        let at = Isometry3d::new(position, rotation);

        // The flat half of the same numbers. A 2D shape sits on the XY plane and turns about Z,
        // which is the one angle a quaternion in that plane can carry, so nothing needs a second
        // way of arriving.
        let flat = Vec2::new(position.x, position.y);
        let turn = Isometry2d::new(
            flat,
            Rot2::radians(rotation.to_euler(bevy::math::EulerRot::ZYX).0),
        );

        match shape.kind {
            1 => {
                $gizmos.sphere(at, shape.radius, $color);
            }
            2 => {
                $gizmos.axes(
                    Transform::from_translation(position).with_rotation(rotation),
                    shape.radius,
                );
            }
            3 => {
                $gizmos.line_gradient(position, far, $color, $fades_to);
            }
            4 => {
                $gizmos.rect(at, Vec2::new(far.x, far.y), $color);
            }
            5 => {
                $gizmos.circle(at, shape.radius, $color);
            }
            6 => {
                // The angle is in radians, and the arc starts where the isometry's own X axis
                // points, so turning the shape is how an arc is aimed.
                $gizmos.arc_3d(far.x, shape.radius, at, $color);
            }
            7 => {
                $gizmos.arrow(position, far, $color);
            }
            8 => {
                // Counts rather than a size, because a grid is described by how many cells it has
                // and how large one is. Negative or absurd counts would be a caller's mistake, so
                // they are clamped rather than refused: a gizmo call answers nothing.
                let cells = UVec2::new(
                    far.x.clamp(0.0, 4096.0) as u32,
                    far.y.clamp(0.0, 4096.0) as u32,
                );

                $gizmos.grid(at, cells, Vec2::splat(shape.radius), $color);
            }
            9 => {
                $gizmos.primitive_3d(
                    &Cuboid::new(far.x, far.y, far.z),
                    at,
                    $color,
                );
            }

            // The rest of the primitives take a radius and one more number, which is what `end.x`
            // carries for each of them.
            10 => {
                $gizmos.primitive_3d(
                    &Capsule3d::new(shape.radius, far.x),
                    at,
                    $color,
                );
            }
            11 => {
                $gizmos.primitive_3d(
                    &Cone {
                        radius: shape.radius,
                        height: far.x,
                    },
                    at,
                    $color,
                );
            }
            12 => {
                $gizmos.primitive_3d(
                    &Cylinder::new(shape.radius, far.x),
                    at,
                    $color,
                );
            }
            13 => {
                // The major radius is the ring's own, and the minor one is how thick the ring is,
                // which is the number every other shape here spends on a length.
                $gizmos.primitive_3d(
                    &Torus {
                        minor_radius: far.x,
                        major_radius: shape.radius,
                    },
                    at,
                    $color,
                );
            }
            // The flat shapes. Each is the one above it seen from a 2D camera, drawn with Bevy's
            // own 2D call rather than with the 3D one at zero depth, because the two differ in
            // more than a coordinate once a line has width and a grid has a plane.
            14 => {
                $gizmos.rect_2d(turn, Vec2::new(far.x, far.y), $color);
            }
            15 => {
                $gizmos.circle_2d(turn, shape.radius, $color);
            }
            16 => {
                $gizmos.line_gradient_2d(flat, Vec2::new(far.x, far.y), $color, $fades_to);
            }
            17 => {
                $gizmos.arrow_2d(flat, Vec2::new(far.x, far.y), $color);
            }
            18 => {
                $gizmos.arc_2d(turn, far.x, shape.radius, $color);
            }
            19 => {
                let cells = UVec2::new(
                    far.x.clamp(0.0, 4096.0) as u32,
                    far.y.clamp(0.0, 4096.0) as u32,
                );

                $gizmos.grid_2d(turn, cells, Vec2::splat(shape.radius), $color);
            }
            _ => {
                $gizmos.line(position, far, $color);
            }
        }
    }};
}

/// Draws everything the queue holds, then empties it.
#[cfg(feature = "render")]
pub fn drain(
    mut queue: bevy::ecs::system::ResMut<GizmoQueue>,
    mut behind: bevy::gizmos::gizmos::Gizmos,
    mut gizmos: bevy::gizmos::gizmos::Gizmos<FrontGizmos>,
) {
    use bevy::color::Color;
    use bevy::gizmos::primitives::dim3::GizmoPrimitive3d;
    use bevy::math::{Quat, Vec3};

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

        if shape.in_front != 0 {
            draw_shape!(gizmos, shape, position, rotation, color, fades_to);
        } else {
            draw_shape!(behind, shape, position, rotation, color, fades_to);
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

/// Sets how gizmos are drawn, for both groups at once.
///
/// `width` is the line thickness in pixels, and `layers` is the render layer mask deciding which
/// cameras see gizmos at all; a mask of `0` is Bevy's own default of layer zero. Both groups take
/// the same values, because the two exist to answer whether the scene may hide a shape and nothing
/// else. `enabled` at zero stops the drawing without the caller having to stop asking for it,
/// which is what a debug overlay bound to a key wants.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gizmo_configure(width: f32, layers: u32, enabled: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, layers, enabled);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::visibility::RenderLayers;
            use bevy::gizmos::config::{DefaultGizmoConfigGroup, GizmoConfigStore};

            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };

                // Zero means "whatever Bevy starts with" rather than "no layers at all", because a
                // caller that wanted gizmos on no camera would turn them off instead, and a
                // zeroed struct arriving from the managed side should change nothing.
                let layers = if layers == 0 {
                    RenderLayers::default()
                } else {
                    crate::render::scene::layers_from(layers).unwrap_or_default()
                };

                // One group at a time, because the store hands out a borrow of itself per group
                // and the two are wanted with the same values rather than at the same moment.
                let apply = |config: &mut bevy::gizmos::config::GizmoConfig| {
                    // A width of zero is a caller leaving it alone rather than asking for lines
                    // with no thickness, which would draw nothing and read as a broken bridge.
                    if width > 0.0 {
                        config.line.width = width;
                    }

                    config.render_layers = layers.clone();
                    config.enabled = enabled != 0;
                };

                apply(store.config_mut::<DefaultGizmoConfigGroup>().0);
                apply(store.config_mut::<FrontGizmos>().0);

                status::OK
            })
        }
    })
}

/// Sets what a gizmo line looks like, for both groups at once.
///
/// `style` is `0` solid, `1` dotted and `2` dashed, where `gap_scale` and `line_scale` are the
/// lengths of the gap and of the visible run, both measured in line widths. `joint` is `0` none,
/// `1` mitred, `2` round and `3` bevelled, and `joint_resolution` is how many triangles a round
/// joint is drawn with. `perspective` at non-zero makes the width a size at the near plane rather
/// than a size on screen, so a line further away is drawn thinner, which only a 3D camera with a
/// perspective projection can honour.
///
/// Separate from [`bcs_gizmo_configure`] because how thick a line is and who can see it is one
/// decision and what the line looks like is another, and the first is what most callers set.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gizmo_style(
    style: i32,
    gap_scale: f32,
    line_scale: f32,
    joint: i32,
    joint_resolution: u32,
    perspective: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (style, gap_scale, line_scale, joint, joint_resolution, perspective);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::gizmos::config::{
                DefaultGizmoConfigGroup, GizmoConfigStore, GizmoLineJoint, GizmoLineStyle,
            };

            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };

                // Bevy's own defaults where a caller passed nothing, so a zeroed request asks for
                // the dashes it would have drawn rather than for a line of zero-length dashes,
                // which is a line that draws nothing.
                let style = match style {
                    1 => GizmoLineStyle::Dotted,
                    2 => GizmoLineStyle::Dashed {
                        gap_scale: if gap_scale > 0.0 { gap_scale } else { 1.0 },
                        line_scale: if line_scale > 0.0 { line_scale } else { 3.0 },
                    },
                    _ => GizmoLineStyle::Solid,
                };

                let joint = match joint {
                    1 => GizmoLineJoint::Miter,
                    2 => GizmoLineJoint::Round(if joint_resolution > 0 {
                        joint_resolution
                    } else {
                        4
                    }),
                    3 => GizmoLineJoint::Bevel,
                    _ => GizmoLineJoint::None,
                };

                let apply = |config: &mut bevy::gizmos::config::GizmoConfig| {
                    config.line.style = style;
                    config.line.joints = joint;
                    config.line.perspective = perspective != 0;
                };

                apply(store.config_mut::<DefaultGizmoConfigGroup>().0);
                apply(store.config_mut::<FrontGizmos>().0);

                status::OK
            })
        }
    })
}

/// Records one shape to draw this frame.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on, since gizmos need the
/// renderer and a window and the plugin that draws them comes with both.
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

//! Debug drawing, reachable from C#.
//!
//! Bevy draws gizmos through a `Gizmos` system parameter, which a C# system cannot hold, because
//! every managed system is an exclusive one, handed the whole world rather than a set of
//! parameters. So calls from C# are recorded in a queue, and one ordinary Bevy system drains it
//! each frame with the real parameter in hand.
//!
//! Gizmos are immediate, so what is drawn lasts one frame and a shape that should stay on screen
//! has to be asked for again every frame. That makes them useful for watching a value change and
//! useless for building anything.

use crate::interop::{status, BcsGizmoConfig};

/// One recorded draw call, waiting for the frame's drain.
///
/// Every shape is described by the same handful of numbers, and `kind` decides which of them a
/// shape reads. A struct per shape would be a struct per arm of one match, and the managed side
/// would need a mirror of each.
#[derive(Clone, Copy)]
pub struct QueuedGizmo {
    /// Which shape. See [`crate::interop::BcsGizmoConfig::kind`] for the list.
    pub kind: i32,
    /// Where the shape sits: a line's start, or the center of everything else.
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
///
/// Text is kept beside the shapes rather than among them, since a shape is a handful of numbers
/// that copy as they are and a run of text owns its string.
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct GizmoQueue(pub Vec<QueuedGizmo>, pub Vec<QueuedText>, pub Option<Vec<QueuedGizmo>>);

impl GizmoQueue {
    /// Where a shape asked for now goes, into the recording when one is open, so it is kept in an
    /// asset rather than drawn once, and otherwise into this frame's drawing.
    fn shapes(&mut self) -> &mut Vec<QueuedGizmo> {
        match &mut self.2 {
            Some(recording) => recording,
            None => &mut self.0,
        }
    }
}

/// One run of text to draw this frame, in Bevy's stroke font.
pub struct QueuedText {
    pub text: String,
    pub settings: BcsGizmoText,
}

/// Where and how a run of text is drawn, as C# describes it.
///
/// The text lies in the plane its rotation turns the XY plane to, its size the height of a capital
/// letter in world units, and `anchor` the point of its bounds that sits at `position`, from minus
/// a half to a half on each axis, zero being the middle.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGizmoText {
    pub position: [f32; 3],
    pub rotation: [f32; 4],
    pub size: f32,
    pub anchor: [f32; 2],
    /// Linear RGBA.
    pub color: [f32; 4],
    /// Whether the scene can hide it, as for a shape.
    pub in_front: i32,
}

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

/// A grid's cell size, the radius across and the third number down where it is given, which
/// draws a grid of cells longer one way than the other. Zero there is a square cell.
#[cfg(feature = "render")]
fn spacing(shape: &QueuedGizmo) -> bevy::math::Vec2 {
    let down = if shape.end[2] > 0.0 { shape.end[2] } else { shape.radius };
    bevy::math::Vec2::new(shape.radius, down)
}

/// Draws one recorded shape, with whichever set of gizmos it belongs to.
///
/// A macro rather than a function, because `Gizmos` and `Gizmos<FrontGizmos>` are different types
/// and the only thing they share is the shape of the call. Written once all the same, because the
/// alternative is every new shape being added in two places and eventually in only one.
#[cfg(feature = "render")]
macro_rules! draw_shape {
    ($gizmos:expr, $shape:expr, $position:expr, $rotation:expr, $color:expr, $fades_to:expr) => {{
        use bevy::math::primitives::{Capsule3d, ConicalFrustum, Cone, Cuboid, Cylinder, Torus};
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
                // A resolution of zero is Bevy's own, which every shape that has one reads the
                // same way, since a caller passing nothing asks for the default.
                let mut sphere = $gizmos.sphere(at, shape.radius, $color);
                if far.x >= 1.0 {
                    sphere = sphere.resolution(far.x as u32);
                }
                drop(sphere);
            }
            2 => {
                // Scaled as the transform is, so an arm's length is measured in the entity's own
                // units, as Bevy's own axes are. A scale of zero is one left unsaid.
                let scale = if far == Vec3::ZERO { Vec3::ONE } else { far };
                $gizmos.axes(
                    Transform::from_translation(position)
                        .with_rotation(rotation)
                        .with_scale(scale),
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
                let mut circle = $gizmos.circle(at, shape.radius, $color);
                if far.x >= 1.0 {
                    circle = circle.resolution(far.x as u32);
                }
                drop(circle);
            }
            6 => {
                // The angle is in radians, and the arc starts where the isometry's own X axis
                // points, so turning the shape is how an arc is aimed.
                let mut arc = $gizmos.arc_3d(far.x, shape.radius, at, $color);
                if far.y >= 1.0 {
                    arc = arc.resolution(far.y as u32);
                }
                drop(arc);
            }
            7 | 25 => {
                // A tip length of zero is Bevy's own, a tenth of the arrow. `25` has a head at
                // both ends, for a span measured between two points.
                let mut arrow = $gizmos.arrow(position, far, $color);
                if shape.radius > 0.0 {
                    arrow = arrow.with_tip_length(shape.radius);
                }
                if shape.kind == 25 {
                    arrow = arrow.with_double_end();
                }
                drop(arrow);
            }
            8 => {
                // Counts rather than a size, because a grid is described by how many cells it has
                // and how large one is. Negative or absurd counts would be a caller's mistake, so
                // they are clamped rather than refused, since a gizmo call answers nothing.
                let cells = UVec2::new(
                    far.x.clamp(0.0, 4096.0) as u32,
                    far.y.clamp(0.0, 4096.0) as u32,
                );

                $gizmos.grid(at, cells, spacing(&shape), $color);
            }
            9 => {
                $gizmos.primitive_3d(
                    &Cuboid::new(far.x, far.y, far.z),
                    at,
                    $color,
                );
            }

            // The rest of the primitives take a radius and one more number, which `end.x` carries
            // for each of them.
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
            20 => {
                // A cone with its point cut off, which is the one remaining primitive whose
                // numbers fit the queue: a radius at each end and a height between them.
                $gizmos.primitive_3d(
                    &ConicalFrustum {
                        radius_bottom: shape.radius,
                        radius_top: far.x,
                        height: far.y,
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
                let mut circle = $gizmos.circle_2d(turn, shape.radius, $color);
                if far.x >= 1.0 {
                    circle = circle.resolution(far.x as u32);
                }
                drop(circle);
            }
            16 => {
                $gizmos.line_gradient_2d(flat, Vec2::new(far.x, far.y), $color, $fades_to);
            }
            17 | 26 => {
                let mut arrow = $gizmos.arrow_2d(flat, Vec2::new(far.x, far.y), $color);
                if shape.radius > 0.0 {
                    arrow = arrow.with_tip_length(shape.radius);
                }
                if shape.kind == 26 {
                    arrow = arrow.with_double_end();
                }
                drop(arrow);
            }
            18 => {
                let mut arc = $gizmos.arc_2d(turn, far.x, shape.radius, $color);
                if far.y >= 1.0 {
                    arc = arc.resolution(far.y as u32);
                }
                drop(arc);
            }
            19 => {
                let cells = UVec2::new(
                    far.x.clamp(0.0, 4096.0) as u32,
                    far.y.clamp(0.0, 4096.0) as u32,
                );

                $gizmos.grid_2d(turn, cells, spacing(&shape), $color);
            }

            // An ellipse is a circle with a half size on each axis, which `end` carries, and its
            // resolution after them.
            21 => {
                let mut ellipse = $gizmos.ellipse(at, Vec2::new(far.x, far.y), $color);
                if far.z >= 1.0 {
                    ellipse = ellipse.resolution(far.z as u32);
                }
                drop(ellipse);
            }
            22 => {
                let mut ellipse = $gizmos.ellipse_2d(turn, Vec2::new(far.x, far.y), $color);
                if far.z >= 1.0 {
                    ellipse = ellipse.resolution(far.z as u32);
                }
                drop(ellipse);
            }

            // Rounded boxes, sized by `end` and rounded by the radius. Not a number is Bevy's own
            // rounding, a tenth of the shortest side, since zero is a box with square corners and
            // a negative radius turns the corners inward, which Bevy draws as well. How many
            // segments each rounded corner has rides in the second color, which a box of one
            // color has no other use for, zero being Bevy's own.
            23 => {
                let mut rect = $gizmos.rounded_rect_2d(turn, Vec2::new(far.x, far.y), $color);
                if !shape.radius.is_nan() {
                    rect = rect.corner_radius(shape.radius);
                }
                if shape.end_color[0] >= 1.0 {
                    rect = rect.arc_resolution(shape.end_color[0] as u32);
                }
                drop(rect);
            }
            24 => {
                let mut cuboid = $gizmos.rounded_cuboid(at, far, $color);
                if !shape.radius.is_nan() {
                    cuboid = cuboid.edge_radius(shape.radius);
                }
                if shape.end_color[0] >= 1.0 {
                    cuboid = cuboid.arc_resolution(shape.end_color[0] as u32);
                }
                drop(cuboid);
            }
            27 => {
                let mut rect = $gizmos.rounded_rect(at, Vec2::new(far.x, far.y), $color);
                if !shape.radius.is_nan() {
                    rect = rect.corner_radius(shape.radius);
                }
                if shape.end_color[0] >= 1.0 {
                    rect = rect.arc_resolution(shape.end_color[0] as u32);
                }
                drop(rect);
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

    for queued in queue.1.drain(..) {
        let settings = queued.settings;
        let at = bevy::math::Isometry3d::new(
            Vec3::from_array(settings.position),
            Quat::from_array(settings.rotation),
        );
        let anchor = bevy::math::Vec2::from_array(settings.anchor);
        let color = Color::linear_rgba(settings.color[0], settings.color[1], settings.color[2], settings.color[3]);

        if settings.in_front != 0 {
            gizmos.text(at, &queued.text, settings.size, anchor, color);
        } else {
            behind.text(at, &queued.text, settings.size, anchor, color);
        }
    }
}

/// Puts every gizmo in front of the scene, once, as the app starts.
///
/// `depth_bias` is how a gizmo is moved toward or away from the camera before it is depth tested;
/// `-1` is as far toward it as the range allows, which is another way of saying that nothing in
/// the scene can hide it.
#[cfg(feature = "render")]
pub fn draw_in_front(mut store: bevy::ecs::system::ResMut<bevy::gizmos::config::GizmoConfigStore>) {
    // Only this one is touched. The default group keeps the engine's own settings, which suit
    // anything drawn as part of the scene, since they are depth tested like the scene.
    let (config, _) = store.config_mut::<FrontGizmos>();
    config.depth_bias = -1.0;
}

/// Starts keeping the shapes asked for, rather than drawing them this frame, until
/// [`bcs_gizmo_record_end`] makes them an asset.
///
/// For lines that do not change, a level's outline or a model's skeleton, which an entity then
/// draws every frame from the asset with nothing asked for again, at a fraction of the cost of
/// asking. Text is not kept and is drawn the frame it is asked for. Returns
/// [`status::INVALID_STATE`] while a recording is already open, since they do not nest.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gizmo_record_begin() -> i32 {
    crate::interop::guard(|| {
        crate::state::with_world(|world| {
            let Some(mut queue) = world.get_resource_mut::<GizmoQueue>() else {
                return status::UNSUPPORTED;
            };
            if queue.2.is_some() {
                return status::INVALID_STATE;
            }

            queue.2 = Some(Vec::new());
            status::OK
        })
    })
}

/// Ends the recording [`bcs_gizmo_record_begin`] opened, draws what it kept into a new
/// `GizmoAsset`, and writes the key of its handle to `out`.
///
/// Every shape lands in the one asset whichever group it was asked in, since an entity's `Gizmo`
/// component carries its own line settings and depth bias. Returns [`status::INVALID_STATE`]
/// where no recording is open.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gizmo_record_end(out: *mut i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        crate::state::with_world(|world| {
            let Some(mut queue) = world.get_resource_mut::<GizmoQueue>() else {
                return status::UNSUPPORTED;
            };
            let Some(recorded) = queue.2.take() else {
                return status::INVALID_STATE;
            };

            #[cfg(not(feature = "render"))]
            {
                let _ = recorded;
                status::UNSUPPORTED
            }

            #[cfg(feature = "render")]
            {
                use bevy::color::Color;
                use bevy::gizmos::GizmoAsset;
                use bevy::gizmos::primitives::dim3::GizmoPrimitive3d;
                use bevy::math::{Quat, Vec3};

                let mut asset = GizmoAsset::new();
                for shape in recorded {
                    let position = Vec3::from_array(shape.start);
                    let rotation = Quat::from_array(shape.rotation);
                    let color = Color::linear_rgba(shape.color[0], shape.color[1], shape.color[2], shape.color[3]);
                    let fades_to = Color::linear_rgba(
                        shape.end_color[0],
                        shape.end_color[1],
                        shape.end_color[2],
                        shape.end_color[3],
                    );
                    draw_shape!(asset, shape, position, rotation, color, fades_to);
                }

                let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<GizmoAsset>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = assets.add(asset);
                let key = crate::assets::insert_handle(world, handle.untyped());
                unsafe { out.write(key) };
                status::OK
            }
        })
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

            queue.shapes().push(queued(&config));
            status::OK
        })
    })
}

/// Records a whole array of shapes to draw this frame.
///
/// One crossing for all of them, for a wireframe, a path or a grid. A shape costs about four
/// percent of a frame at a few hundred a frame and the same again at a few thousand, so the point
/// of this is not the saving at the sizes drawn today. It is that the cost stops growing with the
/// number of lines.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on, and
/// [`status::NULL_ARG`] for a null array with a count above zero.
///
/// # Safety
/// `configs` must point at `count` shapes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gizmo_draw_many(configs: *const BcsGizmoConfig, count: i32) -> i32 {
    crate::interop::guard(|| {
        if count <= 0 {
            // Nothing to draw is not a mistake, and a caller looping over an empty list should not
            // have to check before asking.
            return status::OK;
        }

        if configs.is_null() {
            return status::NULL_ARG;
        }

        let described = unsafe { core::slice::from_raw_parts(configs, count as usize) };

        crate::state::with_world(|world| {
            let Some(mut queue) = world.get_resource_mut::<GizmoQueue>() else {
                return status::UNSUPPORTED;
            };

            let shapes = queue.shapes();
            shapes.reserve(described.len());
            shapes.extend(described.iter().map(queued));

            status::OK
        })
    })
}

/// Records a run of text to draw this frame, in Bevy's stroke font.
///
/// The font has the printable ASCII characters and draws any other as a space, and a line break
/// starts a new line below. Returns [`status::UNSUPPORTED`] where there is nothing to draw on.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string, and `settings` point to a readable
/// [`BcsGizmoText`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gizmo_text(text: *const core::ffi::c_char, settings: *const BcsGizmoText) -> i32 {
    crate::interop::guard(|| {
        if settings.is_null() {
            return status::NULL_ARG;
        }
        let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
            return status::NULL_ARG;
        };
        let settings = unsafe { *settings };

        crate::state::with_world(|world| {
            let Some(mut queue) = world.get_resource_mut::<GizmoQueue>() else {
                return status::UNSUPPORTED;
            };

            queue.1.push(QueuedText { text, settings });
            status::OK
        })
    })
}

/// One described shape as the queue holds it.
fn queued(config: &BcsGizmoConfig) -> QueuedGizmo {
    QueuedGizmo {
        kind: config.kind,
        start: config.start,
        end: config.end,
        rotation: config.rotation,
        radius: config.radius,
        color: config.color,
        end_color: config.end_color,
        in_front: config.in_front,
    }
}

//! The pointer's input to the pan-orbit camera, the left button panning, the right orbiting and
//! the wheel zooming about the point under the pointer.
//!
//! Bevy's pan-orbit camera moves by what a program feeds it, a motion started at a point, the
//! pointer's movement and the wheel while it lasts, and the motion ended, and leaves the mapping
//! from a device to those to the program. Its examples share one mapping, in
//! `examples/camera/pan_orbit_camera_custom_input_plugin.rs` of Bevy 0.20.0, by Bevy's contributors
//! under MIT or Apache-2.0, which is this module with its names changed and its viewport test
//! answering for an offscreen run, so a C# program asking for the controller moves it as Bevy's
//! examples do.

use bevy::app::prelude::*;
use bevy::camera::{Camera, Projection, RenderTarget};
use bevy::camera_controller::pan_orbit_camera::prelude::component::PanOrbitCamera;
use bevy::ecs::prelude::*;
use bevy::input::mouse::{MouseScrollUnit, MouseWheel};
use bevy::input::prelude::*;
use bevy::math::{DVec2, DVec3, Vec2};
use bevy::picking::pointer::{Location, PointerAction, PointerId, PointerInput, PointerInteraction, PointerLocation, PointerMap};
use bevy::platform::collections::HashMap;
use bevy::transform::components::GlobalTransform;
use bevy::window::{PrimaryWindow, Window};

/// Which of the three motions a pointer started.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
enum MotionKind {
    OrbitZoom,
    PanZoom,
    Zoom,
}

/// Maps the mouse to the pan-orbit cameras, after picking has found what is under it and before
/// the controller moves the cameras.
pub struct PointerInputPlugin;

impl Plugin for PointerInputPlugin {
    fn build(&self, app: &mut App) {
        app.add_message::<PanOrbitInput>()
            .init_resource::<CameraPointerMap>()
            .add_systems(
                PreUpdate,
                (read_inputs, PanOrbitInput::receive, PanOrbitInput::send_pointer_inputs)
                    .chain()
                    .after(bevy::picking::PickingSystems::Last)
                    .before(PanOrbitCamera::update_camera_positions),
            );
    }
}

/// Starts a motion when a button is pressed or the wheel turns over a camera's viewport, and ends
/// it when the button is let go or a zoom has run down.
fn read_inputs(
    pointers: Query<(&PointerId, &PointerLocation)>,
    pointer_map: Res<CameraPointerMap>,
    mut controller: MessageWriter<PanOrbitInput>,
    mut mouse_wheel: MessageReader<MouseWheel>,
    mouse_input: Res<ButtonInput<MouseButton>>,
    cameras: Query<(Entity, &Camera, &RenderTarget, &PanOrbitCamera)>,
    primary_window: Query<Entity, With<PrimaryWindow>>,
) {
    let orbit_start = MouseButton::Right;
    let pan_start = MouseButton::Left;
    let zoom_stop = 0.0;

    if let Some(&camera) = pointer_map.get(&PointerId::Mouse) {
        let camera_query = cameras.get(camera).ok();
        let is_in_zoom_mode = camera_query
            .map(|(.., editor_cam)| editor_cam.current_motion.is_zooming_only())
            .unwrap_or_default();
        let zoom_amount_abs = camera_query
            .and_then(|(.., editor_cam)| {
                editor_cam
                    .current_motion
                    .inputs()
                    .map(|inputs| inputs.zoom_velocity_abs(editor_cam.smoothing.zoom.mul_f32(2.0)))
            })
            .unwrap_or(0.0);
        let should_zoom_end = is_in_zoom_mode && zoom_amount_abs <= zoom_stop;

        if mouse_input.any_just_released([orbit_start, pan_start]) || should_zoom_end {
            controller.write(PanOrbitInput::End { camera });
        }
    }

    for (&pointer, pointer_location) in pointers
        .iter()
        .filter_map(|(id, loc)| loc.location().map(|loc| (id, loc)))
    {
        match pointer {
            PointerId::Mouse => {
                let primary = primary_window.single().ok();
                let Some((camera, ..)) = cameras
                    .iter()
                    .find(|(_, camera, render_target, _)| in_viewport(pointer_location, camera, render_target, primary))
                else {
                    continue;
                };

                if mouse_input.just_pressed(orbit_start) {
                    controller.write(PanOrbitInput::Start { kind: MotionKind::OrbitZoom, camera, pointer });
                } else if mouse_input.just_pressed(pan_start) {
                    controller.write(PanOrbitInput::Start { kind: MotionKind::PanZoom, camera, pointer });
                } else if mouse_wheel.read().map(|mw| mw.y.abs()).sum::<f32>() > 0.0 {
                    // The wheel's sum and not whether it sent anything, since a wheel sends
                    // messages of no movement, which would start a zoom that goes nowhere.
                    controller.write(PanOrbitInput::Start { kind: MotionKind::Zoom, camera, pointer });
                }
            }
            PointerId::Touch(_) | PointerId::Custom(_) => continue,
        }
    }

    // Read in some frames and not others, so the wheel is cleared by hand.
    mouse_wheel.clear();
}

/// Whether the pointer is over the camera's viewport, as Bevy's `Location::is_in_viewport` answers,
/// but for a camera drawing into an image in a run with no window, which Bevy's answers no for
/// whatever the image, since it asks for the primary window first. An offscreen run draws into an
/// image and has no window, and its pointer is put on that image.
fn in_viewport(location: &Location, camera: &Camera, render_target: &RenderTarget, primary: Option<Entity>) -> bool {
    render_target.normalize(primary).as_ref() == Some(&location.target)
        && camera.logical_viewport_rect().is_some_and(|rect| rect.contains(location.position))
}

/// Which camera each pointer is moving, so its movement goes on reaching the camera once the
/// motion has started.
#[derive(Debug, Clone, Default, Resource, bevy::prelude::Deref, bevy::prelude::DerefMut)]
struct CameraPointerMap(HashMap<PointerId, Entity>);

/// A motion of a pan-orbit camera started or ended.
#[derive(Debug, Clone, Message)]
enum PanOrbitInput {
    /// A motion starts, about the point under `pointer`, which then moves `camera` until the
    /// motion ends.
    Start { kind: MotionKind, camera: Entity, pointer: PointerId },
    /// The motion moving `camera` ends.
    End { camera: Entity },
}

impl PanOrbitInput {
    fn camera(&self) -> Entity {
        match self {
            PanOrbitInput::Start { camera, .. } | PanOrbitInput::End { camera } => *camera,
        }
    }

    /// Starts and ends the motions, a motion's point being where the pointer hit something, or
    /// where its ray meets the depth the camera last orbited at.
    fn receive(
        mut events: MessageReader<Self>,
        mut controllers: Query<(&mut PanOrbitCamera, &GlobalTransform)>,
        mut camera_map: ResMut<CameraPointerMap>,
        pointer_map: Res<PointerMap>,
        pointer_interactions: Query<&PointerInteraction>,
        pointer_locations: Query<&PointerLocation>,
        cameras: Query<(&Camera, &Projection)>,
        windows: Query<&Window>,
    ) {
        for event in events.read() {
            let Ok((mut controller, cam_transform)) = controllers.get_mut(event.camera()) else {
                continue;
            };

            match event {
                PanOrbitInput::Start { kind, pointer, .. } => {
                    if controller.is_actively_controlled() {
                        continue;
                    }
                    let anchor = pointer_map
                        .get_entity(*pointer)
                        .and_then(|entity| pointer_interactions.get(entity).ok())
                        .and_then(|interaction| interaction.get_nearest_hit())
                        // A hit on a window is in the viewport's coordinates, not the world's.
                        .filter(|(entity, _hit)| !windows.contains(*entity))
                        .and_then(|(_, hit)| hit.position)
                        .map(|world_space_hit| {
                            cam_transform
                                .to_matrix()
                                .as_dmat4()
                                .inverse()
                                .transform_point3(world_space_hit.into())
                        })
                        .filter(|p| p.is_finite())
                        .or_else(|| {
                            let camera = cameras.get(event.camera()).ok();
                            let pointer_location = pointer_map
                                .get_entity(*pointer)
                                .and_then(|entity| pointer_locations.get(entity).ok())
                                .and_then(|l| l.location());
                            if let Some(((camera, proj), pointer_location)) = camera.zip(pointer_location) {
                                screen_to_view_space(camera, proj, &controller, pointer_location.position)
                            } else {
                                None
                            }
                        })
                        .filter(|p| p.is_finite());

                    match kind {
                        MotionKind::OrbitZoom => controller.start_orbit(anchor),
                        MotionKind::PanZoom => controller.start_pan(anchor),
                        MotionKind::Zoom => controller.start_zoom(anchor),
                    }
                    camera_map.insert(*pointer, event.camera());
                }
                PanOrbitInput::End { .. } => {
                    controller.end_move();
                    if let Some(pointer) = camera_map
                        .iter()
                        .find(|(.., camera)| **camera == event.camera())
                        .map(|(&pointer, ..)| pointer)
                    {
                        camera_map.remove(&pointer);
                    }
                }
            }
        }
    }

    /// Hands each moving camera the pointer's movement on the screen and the wheel's, which the
    /// controller reads in pixels to keep the point under the pointer under it.
    fn send_pointer_inputs(
        camera_map: Res<CameraPointerMap>,
        mut camera_controllers: Query<&mut PanOrbitCamera>,
        mut mouse_wheel: MessageReader<MouseWheel>,
        mut moves: MessageReader<PointerInput>,
    ) {
        let moves_list: Vec<_> = moves.read().collect();
        for (pointer, camera) in camera_map.iter() {
            let Ok(mut camera_controller) = camera_controllers.get_mut(*camera) else {
                continue;
            };

            let screenspace_input: Vec2 = moves_list
                .iter()
                .filter(|m| m.pointer_id.eq(pointer))
                .filter_map(|m| match m.action {
                    PointerAction::Move { delta } => Some(delta),
                    _ => None,
                })
                .sum();

            let zoom_amount = match pointer {
                PointerId::Mouse => mouse_wheel
                    .read()
                    .map(|mw| {
                        let scroll_multiplier = match mw.unit {
                            MouseScrollUnit::Line => 150.0,
                            MouseScrollUnit::Pixel => 1.0,
                        };
                        mw.y * scroll_multiplier
                    })
                    .sum::<f32>(),
                _ => 0.0,
            };

            camera_controller.send_screenspace_input(screenspace_input);
            camera_controller.send_zoom_input(zoom_amount);
        }
        mouse_wheel.clear();
    }
}

/// Where the pointer's ray meets the depth the camera last orbited at, in the camera's space.
fn screen_to_view_space(
    camera: &Camera,
    proj: &Projection,
    controller: &PanOrbitCamera,
    target_position: Vec2,
) -> Option<DVec3> {
    let mut viewport_position = if let Some(rect) = camera.logical_viewport_rect() {
        target_position.as_dvec2() - rect.min.as_dvec2()
    } else {
        target_position.as_dvec2()
    };
    let target_size = camera.logical_viewport_size()?.as_dvec2();
    // The screen counts down from the top, and the projection up from the bottom.
    viewport_position.y = target_size.y - viewport_position.y;
    let ndc = viewport_position * 2. / target_size - DVec2::ONE;
    let ndc_to_view = proj.get_clip_from_view().as_dmat4().inverse();
    let view_near_plane = ndc_to_view.project_point3(ndc.extend(1.));
    match &proj {
        Projection::Perspective(_) | Projection::Custom(_) => {
            // The far plane at the least depth above zero, since zero itself gives no number.
            let view_far_plane = ndc_to_view.project_point3(ndc.extend(f64::EPSILON));
            let direction = (view_far_plane - view_near_plane).normalize();
            Some((direction / direction.z) * controller.last_anchor_depth())
        }
        Projection::Orthographic(_) => Some(DVec3::new(
            view_near_plane.x,
            view_near_plane.y,
            controller.last_anchor_depth(),
        )),
    }
}

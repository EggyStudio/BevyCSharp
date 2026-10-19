//! Bevy's camera controllers, the free camera, the pan camera and the pan-orbit camera, with the
//! calls C# sets and reads them through.
//!
//! Each controller is a plugin Bevy leaves out of its defaults and a component on the camera it
//! moves, and an app asks for the plugins it uses with `Config.CameraControllers`. The free and the
//! pan camera's components reflect nothing and hold Bevy's key codes, so each is copied to and from
//! one plain struct here, a key travelling as its place in [`crate::input`]'s table, as the managed
//! `Key` names it. The pan-orbit camera reflects, so C# writes it as any reflected component, and
//! the two messages its extensions read, a dolly zoom to another projection and a turn to face a
//! direction, are sent from here.
//!
//! The pan-orbit camera moves by what a program feeds it, and Bevy's own examples feed it from the
//! pointer with an input plugin of their own, which [`pointer`] carries, so the controller asked for
//! moves the way Bevy's examples show it.

#[cfg(feature = "render")]
mod pointer;

use crate::interop::status;

/// The free camera, Bevy's `FreeCameraPlugin`.
pub const FREE: u32 = 1;
/// The pan camera for a 2D view, Bevy's `PanCameraPlugin`.
pub const PAN: u32 = 2;
/// The pan-orbit camera with its extensions and the pointer's input, Bevy's
/// `DefaultPanOrbitCameraPlugins`.
pub const PAN_ORBIT: u32 = 4;

/// The free camera's settings, Bevy's `FreeCamera` field for field, but for the keys, which are
/// places in the key table.
///
/// The managed side's `NativeFreeCamera`, field for field.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsFreeCamera {
    /// How fast the mouse turns the camera.
    pub sensitivity: f32,
    /// How fast it moves, in units a second.
    pub walk_speed: f32,
    /// How fast it moves with the run key held.
    pub run_speed: f32,
    /// How much a step of the wheel changes both speeds, as the power of e it multiplies them by.
    pub scroll_factor: f32,
    /// How fast the camera stops once no key moves it.
    pub friction: f32,
    /// How fast it turns to an axis a key snaps it to, in radians a second.
    pub rotation_speed: f32,
    /// Zero to move up and down along the world's up, one along the camera's.
    pub vertical_movement: i32,
    /// The mouse button held to turn the camera, as the managed `MouseButton` numbers it.
    pub grab_button: i32,
    /// Forward, back, left, right, up, down, run, the key that toggles the grab, the one that
    /// reverses a snap, and the three that snap to the top, the right and the front, each a place
    /// in the key table, or -1 for a key outside it.
    pub keys: [i32; 12],
}

/// What the free camera holds as it runs, Bevy's `FreeCameraState` but for the snap in progress.
///
/// The managed side's `NativeFreeCameraState`, field for field.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsFreeCameraState {
    /// Non-zero while the controller moves the camera.
    pub enabled: u32,
    /// The pitch it has turned the camera to.
    pub pitch: f32,
    /// The yaw it has turned the camera to.
    pub yaw: f32,
    /// What the wheel has multiplied the speeds by.
    pub speed_multiplier: f32,
    /// How fast the camera moves, in the camera's own axes.
    pub velocity: [f32; 3],
}

/// The pan camera's settings, Bevy's `PanCamera` field for field, but for the keys, which are
/// places in the key table.
///
/// The managed side's `NativePanCamera`, field for field.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsPanCamera {
    /// Non-zero while the controller moves the camera.
    pub enabled: u32,
    /// How far the view is zoomed, the camera's scale.
    pub zoom_factor: f32,
    /// The least the zoom goes to.
    pub min_zoom: f32,
    /// The most the zoom goes to.
    pub max_zoom: f32,
    /// How much a step of the wheel or a frame of a zoom key zooms.
    pub zoom_speed: f32,
    /// Non-zero to zoom about the pointer, zero about the middle of the window.
    pub zoom_to_cursor: u32,
    /// How fast the keys move the view, in units a second.
    pub pan_speed: f32,
    /// How fast the keys turn the view, in radians a second.
    pub rotation_speed: f32,
    /// Non-zero to move the view by dragging it.
    pub mouse_pan: u32,
    /// The button that drags it, as the managed `MouseButton` numbers it, left, right or middle.
    pub mouse_pan_button: i32,
    /// Zoom in, zoom out, up, down, left, right, turn counterclockwise and turn clockwise, each a
    /// place in the key table, or -1 for none.
    pub keys: [i32; 8],
}

/// Adds the controllers `controllers` asks for, a bit each.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App, controllers: u32) {
    use bevy::camera_controller::{free_camera, pan_camera, pan_orbit_camera};

    if controllers & FREE != 0 {
        app.add_plugins(free_camera::FreeCameraPlugin);
    }
    if controllers & PAN != 0 {
        app.add_plugins(pan_camera::PanCameraPlugin);
    }
    if controllers & PAN_ORBIT != 0 {
        app.add_plugins((pan_orbit_camera::DefaultPanOrbitCameraPlugins, pointer::PointerInputPlugin));
    }
}

/// The reflection data Bevy leaves off the pan-orbit camera's components, which reflect without
/// saying they are components, so neither the schema nor a C# program would find them. Registered
/// whatever controllers the app asks for, so the schema is the same from every app.
#[cfg(feature = "render")]
pub(crate) fn register_types(app: &mut bevy::app::App) {
    use bevy::camera_controller::pan_orbit_camera::controller::component::PanOrbitCamera;
    use bevy::camera_controller::pan_orbit_camera::extensions::anchor_indicator::AnchorIndicator;
    use bevy::camera_controller::pan_orbit_camera::extensions::independent_skybox::IndependentSkybox;
    use bevy::ecs::reflect::ReflectComponent;
    use bevy::reflect::std_traits::ReflectDefault;

    app.register_type::<PanOrbitCamera>();
    app.register_type_data::<PanOrbitCamera, ReflectComponent>();
    app.register_type_data::<PanOrbitCamera, ReflectDefault>();
    app.register_type::<AnchorIndicator>();
    app.register_type_data::<AnchorIndicator, ReflectComponent>();
    app.register_type_data::<AnchorIndicator, ReflectDefault>();
    app.register_type::<IndependentSkybox>();
    app.register_type_data::<IndependentSkybox, ReflectComponent>();
    app.register_type_data::<IndependentSkybox, ReflectDefault>();
}

#[cfg(feature = "render")]
mod convert {
    use super::*;

    use bevy::camera_controller::free_camera::{FreeCamera, FreeCameraState, VerticalMovementAxis};
    use bevy::camera_controller::pan_camera::{MousePanSettings, PanCamera, ZoomTarget};
    use bevy::input::keyboard::KeyCode;
    use bevy::input::mouse::MouseButton;
    use bevy::math::Vec3;
    use bevy::picking::pointer::PointerButton;

    fn key_place(key: KeyCode) -> i32 {
        crate::input::key_bit(key).map_or(-1, |place| place as i32)
    }

    fn key_at(place: i32) -> Option<KeyCode> {
        usize::try_from(place).ok().and_then(crate::input::key_from)
    }

    fn button_place(button: MouseButton) -> i32 {
        match button {
            MouseButton::Left => 0,
            MouseButton::Right => 1,
            MouseButton::Middle => 2,
            MouseButton::Back => 3,
            MouseButton::Forward => 4,
            MouseButton::Other(_) => -1,
        }
    }

    fn button_at(place: i32) -> MouseButton {
        match place {
            1 => MouseButton::Right,
            2 => MouseButton::Middle,
            3 => MouseButton::Back,
            4 => MouseButton::Forward,
            _ => MouseButton::Left,
        }
    }

    pub fn free_out(camera: &FreeCamera) -> BcsFreeCamera {
        BcsFreeCamera {
            sensitivity: camera.sensitivity,
            walk_speed: camera.walk_speed,
            run_speed: camera.run_speed,
            scroll_factor: camera.scroll_factor,
            friction: camera.friction,
            rotation_speed: camera.rotation_speed,
            vertical_movement: match camera.vertical_movement_axis {
                VerticalMovementAxis::World => 0,
                VerticalMovementAxis::Local => 1,
            },
            grab_button: button_place(camera.mouse_key_cursor_grab),
            keys: [
                camera.key_forward,
                camera.key_back,
                camera.key_left,
                camera.key_right,
                camera.key_up,
                camera.key_down,
                camera.key_run,
                camera.keyboard_key_toggle_cursor_grab,
                camera.key_snap_reverse,
                camera.axis_top,
                camera.axis_right,
                camera.axis_front,
            ]
            .map(key_place),
        }
    }

    /// The settings as Bevy holds them, a key outside the table keeping Bevy's own for that move.
    pub fn free_in(settings: &BcsFreeCamera) -> FreeCamera {
        let bevy = FreeCamera::default();
        let key = |slot: usize, otherwise: KeyCode| key_at(settings.keys[slot]).unwrap_or(otherwise);
        FreeCamera {
            sensitivity: settings.sensitivity,
            key_forward: key(0, bevy.key_forward),
            key_back: key(1, bevy.key_back),
            key_left: key(2, bevy.key_left),
            key_right: key(3, bevy.key_right),
            key_up: key(4, bevy.key_up),
            key_down: key(5, bevy.key_down),
            key_run: key(6, bevy.key_run),
            mouse_key_cursor_grab: button_at(settings.grab_button),
            keyboard_key_toggle_cursor_grab: key(7, bevy.keyboard_key_toggle_cursor_grab),
            key_snap_reverse: key(8, bevy.key_snap_reverse),
            axis_top: key(9, bevy.axis_top),
            axis_right: key(10, bevy.axis_right),
            axis_front: key(11, bevy.axis_front),
            walk_speed: settings.walk_speed,
            run_speed: settings.run_speed,
            scroll_factor: settings.scroll_factor,
            friction: settings.friction,
            rotation_speed: settings.rotation_speed,
            vertical_movement_axis: if settings.vertical_movement == 1 {
                VerticalMovementAxis::Local
            } else {
                VerticalMovementAxis::World
            },
        }
    }

    pub fn state_out(state: &FreeCameraState) -> BcsFreeCameraState {
        BcsFreeCameraState {
            enabled: state.enabled as u32,
            pitch: state.pitch,
            yaw: state.yaw,
            speed_multiplier: state.speed_multiplier,
            velocity: state.velocity.to_array(),
        }
    }

    /// Writes what C# may set, leaving the snap in progress and whether the controller has read
    /// the camera's first turn as they are.
    pub fn state_in(settings: &BcsFreeCameraState, state: &mut FreeCameraState) {
        state.enabled = settings.enabled != 0;
        state.pitch = settings.pitch;
        state.yaw = settings.yaw;
        state.speed_multiplier = settings.speed_multiplier;
        state.velocity = Vec3::from_array(settings.velocity);
    }

    pub fn pan_out(camera: &PanCamera) -> BcsPanCamera {
        let key = |key: Option<KeyCode>| key.map_or(-1, key_place);
        BcsPanCamera {
            enabled: camera.enabled as u32,
            zoom_factor: camera.zoom_factor,
            min_zoom: camera.min_zoom,
            max_zoom: camera.max_zoom,
            zoom_speed: camera.zoom_speed,
            zoom_to_cursor: (camera.zoom_target == ZoomTarget::Cursor) as u32,
            pan_speed: camera.pan_speed,
            rotation_speed: camera.rotation_speed,
            mouse_pan: camera.mouse_pan_settings.enabled as u32,
            mouse_pan_button: match camera.mouse_pan_settings.button {
                PointerButton::Primary => 0,
                PointerButton::Secondary => 1,
                PointerButton::Middle => 2,
            },
            keys: [
                key(camera.key_zoom_in),
                key(camera.key_zoom_out),
                key(camera.key_up),
                key(camera.key_down),
                key(camera.key_left),
                key(camera.key_right),
                key(camera.key_rotate_ccw),
                key(camera.key_rotate_cw),
            ],
        }
    }

    pub fn pan_in(settings: &BcsPanCamera) -> PanCamera {
        PanCamera {
            enabled: settings.enabled != 0,
            zoom_factor: settings.zoom_factor,
            min_zoom: settings.min_zoom,
            max_zoom: settings.max_zoom,
            zoom_speed: settings.zoom_speed,
            zoom_target: if settings.zoom_to_cursor != 0 { ZoomTarget::Cursor } else { ZoomTarget::Center },
            key_zoom_in: key_at(settings.keys[0]),
            key_zoom_out: key_at(settings.keys[1]),
            pan_speed: settings.pan_speed,
            key_up: key_at(settings.keys[2]),
            key_down: key_at(settings.keys[3]),
            key_left: key_at(settings.keys[4]),
            key_right: key_at(settings.keys[5]),
            rotation_speed: settings.rotation_speed,
            key_rotate_ccw: key_at(settings.keys[6]),
            key_rotate_cw: key_at(settings.keys[7]),
            mouse_pan_settings: MousePanSettings {
                enabled: settings.mouse_pan != 0,
                button: match settings.mouse_pan_button {
                    1 => PointerButton::Secondary,
                    2 => PointerButton::Middle,
                    _ => PointerButton::Primary,
                },
            },
        }
    }
}

/// Runs `f` with the world and the camera, refusing an entity that is not a camera, or answers
/// [`status::UNSUPPORTED`] on a bridge without the renderer, which has no controllers. Each entry
/// point runs it under its own guard.
fn on_camera(camera: u64, f: impl FnOnce(&mut bevy::ecs::world::World, bevy::ecs::entity::Entity) -> i32) -> i32 {
    #[cfg(not(feature = "render"))]
    {
        let _ = (camera, f);
        status::UNSUPPORTED
    }

    #[cfg(feature = "render")]
    {
        let entity = bevy::ecs::entity::Entity::from_bits(camera);
        crate::state::with_world(|world| {
            if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                return refusal;
            }
            f(world, entity)
        })
    }
}

/// Writes Bevy's default free camera settings into `out`, which needs no app.
///
/// # Safety
/// `out` must be valid for writes of one [`BcsFreeCamera`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_free_camera_default(out: *mut BcsFreeCamera) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            unsafe { *out = convert::free_out(&Default::default()) };
            status::OK
        }
    })
}

/// Puts the free camera on `camera` with `settings`, or changes the settings of the one there,
/// keeping what it holds as it runs.
///
/// The camera moves only where the app asked for the free camera's plugin, and only while it is
/// the one camera carrying the controller, since Bevy's controller moves a single camera.
///
/// # Safety
/// `settings` must be valid for reads of one [`BcsFreeCamera`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_free_camera_set(camera: u64, settings: *const BcsFreeCamera) -> i32 {
    crate::interop::guard(|| {
        if settings.is_null() {
            return status::NULL_ARG;
        }
        let settings = unsafe { *settings };

        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            _world.entity_mut(_entity).insert(convert::free_in(&settings));
            #[cfg(not(feature = "render"))]
            let _ = settings;
            status::OK
        })
    })
}

/// Reads the free camera's settings off `camera` into `out`, or answers
/// [`status::NOT_PRESENT`] where it has none.
///
/// # Safety
/// `out` must be valid for writes of one [`BcsFreeCamera`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_free_camera_get(camera: u64, out: *mut BcsFreeCamera) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::free_camera::FreeCamera;
                let Some(free) = _world.get::<FreeCamera>(_entity) else {
                    return status::NOT_PRESENT;
                };
                unsafe { *out = convert::free_out(free) };
            }
            status::OK
        })
    })
}

/// Takes the free camera off `camera`, with what it held as it ran.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_free_camera_remove(camera: u64) -> i32 {
    crate::interop::guard(|| {
        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::free_camera::{FreeCamera, FreeCameraState};
                _world.entity_mut(_entity).remove::<(FreeCamera, FreeCameraState)>();
            }
            status::OK
        })
    })
}

/// Reads what the free camera on `camera` holds as it runs into `out`.
///
/// # Safety
/// `out` must be valid for writes of one [`BcsFreeCameraState`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_free_camera_state(camera: u64, out: *mut BcsFreeCameraState) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::free_camera::FreeCameraState;
                let Some(state) = _world.get::<FreeCameraState>(_entity) else {
                    return status::NOT_PRESENT;
                };
                unsafe { *out = convert::state_out(state) };
            }
            status::OK
        })
    })
}

/// Writes what the free camera on `camera` holds as it runs, whether it is on, its turn, its
/// speed's multiplier and its velocity.
///
/// # Safety
/// `state` must be valid for reads of one [`BcsFreeCameraState`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_free_camera_set_state(camera: u64, state: *const BcsFreeCameraState) -> i32 {
    crate::interop::guard(|| {
        if state.is_null() {
            return status::NULL_ARG;
        }
        let state = unsafe { *state };

        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::free_camera::FreeCameraState;
                let Some(mut held) = _world.get_mut::<FreeCameraState>(_entity) else {
                    return status::NOT_PRESENT;
                };
                convert::state_in(&state, &mut held);
            }
            #[cfg(not(feature = "render"))]
            let _ = state;
            status::OK
        })
    })
}

/// Writes Bevy's description of the free camera's controls on `camera` into `out`, the text its
/// example shows, and returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_free_camera_text(camera: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::free_camera::FreeCamera;
                let Some(free) = _world.get::<FreeCamera>(_entity) else {
                    return status::NOT_PRESENT;
                };
                unsafe { crate::interop::write_text(&free.to_string(), out, capacity) }
            }
            #[cfg(not(feature = "render"))]
            {
                let _ = (out, capacity);
                status::UNSUPPORTED
            }
        })
    })
}

/// Writes Bevy's default pan camera settings into `out`, which needs no app.
///
/// # Safety
/// `out` must be valid for writes of one [`BcsPanCamera`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pan_camera_default(out: *mut BcsPanCamera) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            unsafe { *out = convert::pan_out(&Default::default()) };
            status::OK
        }
    })
}

/// Puts the pan camera on `camera` with `settings`, or changes the settings of the one there.
///
/// # Safety
/// `settings` must be valid for reads of one [`BcsPanCamera`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pan_camera_set(camera: u64, settings: *const BcsPanCamera) -> i32 {
    crate::interop::guard(|| {
        if settings.is_null() {
            return status::NULL_ARG;
        }
        let settings = unsafe { *settings };

        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            _world.entity_mut(_entity).insert(convert::pan_in(&settings));
            #[cfg(not(feature = "render"))]
            let _ = settings;
            status::OK
        })
    })
}

/// Reads the pan camera's settings off `camera` into `out`, the zoom it has come to among them,
/// or answers [`status::NOT_PRESENT`] where it has none.
///
/// # Safety
/// `out` must be valid for writes of one [`BcsPanCamera`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pan_camera_get(camera: u64, out: *mut BcsPanCamera) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::pan_camera::PanCamera;
                let Some(pan) = _world.get::<PanCamera>(_entity) else {
                    return status::NOT_PRESENT;
                };
                unsafe { *out = convert::pan_out(pan) };
            }
            status::OK
        })
    })
}

/// Takes the pan camera off `camera`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_pan_camera_remove(camera: u64) -> i32 {
    crate::interop::guard(|| {
        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::pan_camera::PanCamera;
                _world.entity_mut(_entity).remove::<PanCamera>();
            }
            status::OK
        })
    })
}

/// Writes Bevy's description of the pan camera's controls on `camera` into `out`, the text its
/// example shows, and returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pan_camera_text(camera: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::pan_camera::PanCamera;
                let Some(pan) = _world.get::<PanCamera>(_entity) else {
                    return status::NOT_PRESENT;
                };
                unsafe { crate::interop::write_text(&pan.to_string(), out, capacity) }
            }
            #[cfg(not(feature = "render"))]
            {
                let _ = (out, capacity);
                status::UNSUPPORTED
            }
        })
    })
}

/// Moves the pan-orbit camera `camera` to another projection over Bevy's dolly zoom, its field of
/// view narrowing as it backs away until it is orthographic, or the other way, keeping the point it
/// orbits the same size on the screen. `orthographic` is non-zero for Bevy's default orthographic
/// projection in 3D and zero for its default perspective.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_pan_orbit_dolly_zoom(camera: u64, orthographic: i32) -> i32 {
    crate::interop::guard(|| {
        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera::{OrthographicProjection, PerspectiveProjection, Projection};
                use bevy::camera_controller::pan_orbit_camera::extensions::dolly_zoom::DollyZoomTrigger;

                if !_world.contains_resource::<bevy::ecs::message::Messages<DollyZoomTrigger>>() {
                    return status::INVALID_STATE;
                }
                let target_projection = if orthographic != 0 {
                    Projection::Orthographic(OrthographicProjection::default_3d())
                } else {
                    Projection::Perspective(PerspectiveProjection::default())
                };
                _world.write_message(DollyZoomTrigger { target_projection, camera: _entity });
            }
            #[cfg(not(feature = "render"))]
            let _ = orthographic;
            status::OK
        })
    })
}

/// Turns the pan-orbit camera `camera` to face `(x, y, z)` over Bevy's look-to animation, about
/// the point it orbits, with its up the axis nearest the camera's own, as Bevy's
/// `LookToTrigger::auto_snap_up_direction` chooses it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_pan_orbit_look_to(camera: u64, x: f32, y: f32, z: f32) -> i32 {
    crate::interop::guard(|| {
        on_camera(camera, |_world, _entity| {
            #[cfg(feature = "render")]
            {
                use bevy::camera_controller::pan_orbit_camera::controller::component::PanOrbitCamera;
                use bevy::camera_controller::pan_orbit_camera::extensions::look_to::LookToTrigger;
                use bevy::math::DVec3;
                use bevy::transform::components::Transform;

                if !_world.contains_resource::<bevy::ecs::message::Messages<LookToTrigger>>() {
                    return status::INVALID_STATE;
                }
                let entity = _world.entity(_entity);
                let (Some(controller), Some(transform)) = (entity.get::<PanOrbitCamera>(), entity.get::<Transform>()) else {
                    return status::NOT_PRESENT;
                };
                let trigger = LookToTrigger::auto_snap_up_direction(
                    DVec3::new(x as f64, y as f64, z as f64),
                    _entity,
                    &transform.rotation.as_dquat(),
                    controller,
                );
                _world.write_message(trigger);
            }
            #[cfg(not(feature = "render"))]
            let _ = (x, y, z);
            status::OK
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_settings_sit_where_the_managed_side_reads_them() {
        assert_eq!(core::mem::offset_of!(BcsFreeCamera, vertical_movement), 24);
        assert_eq!(core::mem::offset_of!(BcsFreeCamera, keys), 32);
        assert_eq!(core::mem::size_of::<BcsFreeCamera>(), 80);
        assert_eq!(core::mem::offset_of!(BcsFreeCameraState, velocity), 16);
        assert_eq!(core::mem::size_of::<BcsFreeCameraState>(), 28);
        assert_eq!(core::mem::offset_of!(BcsPanCamera, mouse_pan_button), 36);
        assert_eq!(core::mem::offset_of!(BcsPanCamera, keys), 40);
        assert_eq!(core::mem::size_of::<BcsPanCamera>(), 72);
    }

    #[cfg(feature = "render")]
    #[test]
    fn bevys_defaults_come_back_as_they_went() {
        use bevy::camera_controller::free_camera::FreeCamera;
        use bevy::camera_controller::pan_camera::PanCamera;

        let free = convert::free_out(&FreeCamera::default());
        assert!(free.keys.iter().all(|&key| key >= 0), "a default key outside the table");
        assert_eq!(convert::free_out(&convert::free_in(&free)).keys, free.keys);

        let pan = convert::pan_out(&PanCamera::default());
        assert_eq!(convert::pan_out(&convert::pan_in(&pan)).keys, pan.keys);
        assert_eq!(pan.mouse_pan_button, 0);
    }
}

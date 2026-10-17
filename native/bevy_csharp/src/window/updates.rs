//! How often the app updates, by winit's settings, and the two ways of asking for an update the
//! settings would not give yet.
//!
//! Winit runs the loop of an app with a window, and by Bevy's default updates it as fast as it can
//! while the window has focus. A desktop tool updates only when something happens instead, which
//! winit's reactive modes give, and a redraw requested or a wake sent to its loop updates it once
//! more. An offscreen run has no winit, its loop the schedule runner's, so the settings are kept
//! and change nothing there, and a wake has no loop to reach.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// The mode answering one of winit's two moments, focused or not, continuous where `continuous`
/// is non-zero, and otherwise reactive, waiting `wait` seconds at most between updates and woken
/// sooner by the kinds of event `reacts` names, `1` the devices' own (a mouse moved anywhere), `2`
/// the app's own, as a wake, and `4` the window's.
#[cfg(feature = "render")]
fn mode(continuous: u32, wait: f64, reacts: u32) -> Option<bevy::winit::UpdateMode> {
    if continuous != 0 {
        return Some(bevy::winit::UpdateMode::Continuous);
    }

    Some(bevy::winit::UpdateMode::Reactive {
        wait: core::time::Duration::try_from_secs_f64(wait).ok()?,
        react_to_device_events: reacts & 1 != 0,
        react_to_user_events: reacts & 2 != 0,
        react_to_window_events: reacts & 4 != 0,
    })
}

/// Sets how often the app updates while its window has focus and while it does not, each mode as
/// [`mode`] reads it, in place of Bevy's default of updating continuously while focused.
///
/// A wait that is negative or not a number is refused with [`status::NULL_ARG`].
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_update_modes(
    focused_continuous: u32,
    focused_wait: f64,
    focused_reacts: u32,
    unfocused_continuous: u32,
    unfocused_wait: f64,
    unfocused_reacts: u32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (focused_continuous, focused_wait, focused_reacts, unfocused_continuous, unfocused_wait, unfocused_reacts);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let (Some(focused_mode), Some(unfocused_mode)) = (
                mode(focused_continuous, focused_wait, focused_reacts),
                mode(unfocused_continuous, unfocused_wait, unfocused_reacts),
            ) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                world.insert_resource(bevy::winit::WinitSettings { focused_mode, unfocused_mode });
                status::OK
            })
        }
    })
}

/// Asks for one more update, as Bevy's `RequestRedraw` does, for an app in a reactive mode with
/// something still moving that no event would wake it for, as an animation playing out. A run with
/// no window plugin, as a headless one, has the message unregistered and no loop that waits, so the
/// call does nothing there.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_request_redraw() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                if world.contains_resource::<bevy::ecs::message::Messages<bevy::window::RequestRedraw>>() {
                    world.write_message(bevy::window::RequestRedraw);
                }
                status::OK
            })
        }
    })
}

/// Wakes winit's loop for one more update, as Bevy's `WinitUserEvent::WakeUp` sent through its
/// event loop's proxy does, which is how something outside the app's own updates, as a thread
/// that finished its work, asks for one. A run with no winit, as an offscreen one, never waits and
/// has nothing to wake, so the call does nothing there.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_wake_up() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                if let Some(proxy) = world.get_resource::<bevy::winit::EventLoopProxyWrapper>() {
                    let _ = proxy.send_event(bevy::winit::WinitUserEvent::WakeUp);
                }
                status::OK
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::prelude::World;
    use bevy::winit::{UpdateMode, WinitSettings};

    use crate::interop::status;
    use crate::state::loan_world;

    /// The modes land in winit's settings as Bevy's desktop preset makes them, a bad wait is
    /// refused, and a wake with no loop to reach and a redraw with no message to carry it do
    /// nothing.
    #[test]
    fn the_update_modes_land_in_winits_settings() {
        let mut world = World::new();
        let set = |world: &mut World, wait| loan_world(world, || super::bcs_window_set_update_modes(0, 5.0, 7, 0, wait, 6));

        assert_eq!(set(&mut world, f64::NAN), status::NULL_ARG);
        assert_eq!(set(&mut world, 60.0), status::OK);

        let settings = world.resource::<WinitSettings>();
        let desktop = WinitSettings::desktop_app();
        assert_eq!((settings.focused_mode, settings.unfocused_mode), (desktop.focused_mode, desktop.unfocused_mode));
        assert_eq!(loan_world(&mut world, || super::bcs_window_set_update_modes(1, 0.0, 0, 1, 0.0, 0)), status::OK);
        assert_eq!(world.resource::<WinitSettings>().focused_mode, UpdateMode::Continuous);

        assert_eq!(loan_world(&mut world, || super::bcs_window_wake_up()), status::OK);
        assert_eq!(loan_world(&mut world, || super::bcs_window_request_redraw()), status::OK);
    }
}

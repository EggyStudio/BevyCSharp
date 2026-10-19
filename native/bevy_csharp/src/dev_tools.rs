//! Bevy's dev tools, the FPS overlay with its frame time graph, the infinite grid and the debug
//! overlay of picking, with the call C# sets picking's debug mode through.
//!
//! Each is a plugin Bevy leaves out of its defaults, and an app asks for those it uses with
//! `Config.DevTools`. The overlay's settings are the reflected `FpsOverlayConfig` resource and the
//! grid is the reflected `InfiniteGrid` component with its `InfiniteGridSettings`, so C# reaches
//! both as it reaches any reflected type. Picking's debug mode is a resource that reflects nothing,
//! so it is set and read here, as a number the managed `PickingDebugMode` names.

use crate::interop::status;

/// The FPS overlay and its frame time graph, Bevy's `FpsOverlayPlugin`.
pub const FPS_OVERLAY: u32 = 1;
/// The infinite grid, Bevy's `InfiniteGridPlugin`.
pub const INFINITE_GRID: u32 = 2;
/// The overlay and log of what picking finds under each pointer, Bevy's `DebugPickingPlugin`.
pub const PICKING_DEBUG: u32 = 4;

/// Adds the dev tools `tools` asks for, a bit each.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App, tools: u32) {
    use bevy::dev_tools::{fps_overlay::FpsOverlayPlugin, infinite_grid::InfiniteGridPlugin, picking_debug::DebugPickingPlugin};

    if tools & FPS_OVERLAY != 0 {
        app.add_plugins(FpsOverlayPlugin::default());
    }
    if tools & INFINITE_GRID != 0 {
        app.add_plugins(InfiniteGridPlugin);
    }
    if tools & PICKING_DEBUG != 0 {
        app.add_plugins(DebugPickingPlugin);
    }
}

/// Picking's debug mode, 0 for disabled, 1 for normal and 2 for noisy, as the managed
/// `PickingDebugMode` numbers Bevy's `DebugPickingMode`.
#[cfg(feature = "render")]
mod mode {
    use bevy::dev_tools::picking_debug::DebugPickingMode;

    pub fn to_number(mode: DebugPickingMode) -> i32 {
        match mode {
            DebugPickingMode::Disabled => 0,
            DebugPickingMode::Normal => 1,
            DebugPickingMode::Noisy => 2,
        }
    }

    pub fn from_number(number: i32) -> Option<DebugPickingMode> {
        match number {
            0 => Some(DebugPickingMode::Disabled),
            1 => Some(DebugPickingMode::Normal),
            2 => Some(DebugPickingMode::Noisy),
            _ => None,
        }
    }
}

/// Sets picking's debug mode, which shows the overlay and logs picking's events while it is normal
/// or noisy, the noisy one logging the moves and drags as well.
///
/// `NULL_ARG` for a number that names no mode, `INVALID_STATE` where the app did not ask for the
/// picking debug tool.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dev_tools_set_picking_debug(mode: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = mode;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::dev_tools::picking_debug::DebugPickingMode;

            let Some(mode) = mode::from_number(mode) else {
                return status::NULL_ARG;
            };
            crate::state::with_world(|world| match world.get_resource_mut::<DebugPickingMode>() {
                Some(mut current) => {
                    *current = mode;
                    status::OK
                }
                None => status::INVALID_STATE,
            })
        }
    })
}

/// Reads picking's debug mode into `out`, as [`bcs_dev_tools_set_picking_debug`] numbers it.
///
/// `INVALID_STATE` where the app did not ask for the picking debug tool.
///
/// # Safety
/// `out` must be valid for writes of one `i32`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dev_tools_picking_debug(out: *mut i32) -> i32 {
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
            use bevy::dev_tools::picking_debug::DebugPickingMode;

            crate::state::with_world(|world| match world.get_resource::<DebugPickingMode>() {
                Some(current) => {
                    unsafe { *out = mode::to_number(*current) };
                    status::OK
                }
                None => status::INVALID_STATE,
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use super::mode::{from_number, to_number};

    #[test]
    fn every_mode_comes_back_as_its_number() {
        for number in 0..3 {
            assert_eq!(number, to_number(from_number(number).unwrap()));
        }
        assert!(from_number(3).is_none());
        assert!(from_number(-1).is_none());
    }
}

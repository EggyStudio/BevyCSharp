//! The first window of a drawing app as the config C# filled asks for it, given to Bevy's window
//! plugin, which winit makes it from before it is shown, so its size, place, mode, screen, edges,
//! vsync and alpha are its own from its first frame.

use bevy::window::{
    CompositeAlphaMode, MonitorSelection, PresentMode, VideoModeSelection, Window, WindowMode, WindowPosition,
    WindowResolution,
};

use crate::interop::BcsConfig;

/// The first window the config asks for, titled `title` or the engine's name.
pub fn primary(config: &BcsConfig, title: Option<String>) -> Window {
    // The monitor a window covering its screen opens on, the one the settings name or the primary
    // one, since winit has no current monitor for a window it has not made.
    let start_monitor = if config.has_monitor != 0 {
        MonitorSelection::Index(config.monitor as usize)
    } else {
        MonitorSelection::Primary
    };

    Window {
        title: title.unwrap_or_else(|| "BevyCSharp".to_string()),
        resolution: if config.scale_factor > 0.0 {
            WindowResolution::new(config.width, config.height).with_scale_factor_override(config.scale_factor)
        } else {
            (config.width, config.height).into()
        },
        // On the monitor the settings name, or where it was last closed when the game asks for
        // that, so it does not open where the platform chooses and then jump.
        position: if config.has_monitor != 0 {
            WindowPosition::Centered(start_monitor)
        } else if config.has_position != 0 {
            WindowPosition::At(bevy::math::IVec2::new(config.x, config.y))
        } else {
            WindowPosition::Automatic
        },
        // In the mode the settings ask for, which Bevy gives the window as winit makes it, before
        // the window is shown.
        mode: match config.mode {
            1 => WindowMode::BorderlessFullscreen(start_monitor),
            2 => WindowMode::Fullscreen(start_monitor, VideoModeSelection::Current),
            _ => WindowMode::Windowed,
        },
        resizable: config.fixed_size == 0,
        present_mode: if config.vsync != 0 {
            PresentMode::AutoVsync
        } else {
            PresentMode::AutoNoVsync
        },
        // Composited with alpha, where asked for, which the platform has to know before the window
        // exists. Premultiplied, since a camera and the interface write that. Where the surface
        // cannot, the window module falls back before the surface is made.
        transparent: config.transparent != 0,
        composite_alpha_mode: if config.transparent != 0 {
            CompositeAlphaMode::PreMultiplied
        } else {
            CompositeAlphaMode::Auto
        },
        ..Default::default()
    }
}

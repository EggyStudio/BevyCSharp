//! The first window of a drawing app as the config C# filled asks for it, given to Bevy's window
//! plugin, which winit makes it from before it is shown, so its size, place, mode, screen, edges,
//! frame, vsync, alpha and the picture its cameras draw are its own from its first frame.

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
        // On the monitor the settings name, in its middle without a frame where the settings ask
        // for borderless, or where it was last closed when the game asks for that, so it does not
        // open where the platform chooses and then jump.
        position: if config.has_monitor != 0 || config.frameless != 0 {
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
        decorations: config.frameless == 0,
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

/// Gives the first window the picture the config asks for, its size or share, shape, fit and
/// filter, before its first frame is drawn (see [`super::picture`]), and the run the cap on its
/// frames a second.
pub fn scale(app: &mut bevy::app::App, config: &BcsConfig) {
    use bevy::ecs::query::With;
    use bevy::window::PrimaryWindow;

    super::pace::set(config.frame_limit);

    let numbers = (config.aspect_width, config.aspect_height);
    let Ok(Some(picture)) = super::picture::WindowPicture::from_numbers(
        config.picture_width,
        config.picture_height,
        config.picture_share,
        numbers,
        config.fit,
        config.filter,
    ) else {
        return;
    };
    let world = app.world_mut();
    let Some(window) = world.query_filtered::<bevy::ecs::entity::Entity, With<PrimaryWindow>>().iter(world).next() else {
        return;
    };
    world.entity_mut(window).insert(picture);
}

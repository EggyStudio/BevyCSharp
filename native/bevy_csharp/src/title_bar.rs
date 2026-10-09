//! The title bar the desktop draws, chosen before the window opens.

/// Opens the window through XWayland on GNOME under Wayland, so the desktop draws its title bar in
/// its own style rather than the window drawing an imitation of an older one.
///
/// winit picks Wayland when `WAYLAND_DISPLAY` is set and X11 otherwise, so taking it out of this
/// process's environment before the event loop is made is the whole of choosing X11. Only when
/// there is an X server to go to (`DISPLAY`), and only on GNOME, the one desktop that leaves a
/// Wayland window to draw its own frame. Said on standard error, so a window that looks different
/// says why.
#[cfg(feature = "render")]
pub(crate) fn prefer_desktop_title_bar() {
    #[cfg(all(unix, not(any(target_os = "macos", target_os = "ios", target_os = "android"))))]
    {
        let desktop = std::env::var("XDG_CURRENT_DESKTOP").unwrap_or_default();
        let gnome = desktop.split(':').any(|part| part.eq_ignore_ascii_case("GNOME"));
        let wayland = std::env::var_os("WAYLAND_DISPLAY").is_some();
        let x11 = std::env::var_os("DISPLAY").is_some();

        if gnome && wayland && x11 {
            // SAFETY: called before the event loop and the renderer start any thread that could
            // read the environment at the same time.
            unsafe { std::env::remove_var("WAYLAND_DISPLAY") };

            // Written straight out, since this runs before Bevy's own logging is set up.
            eprintln!("[bcs] the window opens through XWayland, so GNOME draws its title bar");
        }
    }
}


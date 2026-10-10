//! Whether a window that asked to be see-through can be, settled before its surface is made.

/// What each window that asked to be see-through was given, by its entity, once decided.
#[cfg(feature = "render")]
static ALPHA: std::sync::Mutex<Option<std::collections::HashMap<bevy::ecs::entity::Entity, bevy::window::CompositeAlphaMode>>> =
    std::sync::Mutex::new(None);

/// Adds what checks a see-through window can be one before its surface is made.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::ecs::schedule::IntoScheduleConfigs;

    if let Ok(mut alpha) = ALPHA.lock() {
        *alpha = None;
    }

    let Some(render_app) = app.get_sub_app_mut(bevy::render::RenderApp) else {
        return;
    };

    render_app.add_systems(
        bevy::render::Render,
        settle_alpha.before(bevy::render::view::window::create_surfaces),
    );
}

/// The mode a see-through window is composited in, out of those its surface offers: premultiplied,
/// since a camera and the interface write that, then straight, then opaque.
///
/// Opaque last and always, because Bevy configures a surface with whatever mode the window asked
/// for without asking the surface first, and a mode the surface lacks is a validation error that
/// ends the app. A window that cannot be see-through is drawn opaque, where what would have been
/// clear is black.
#[cfg(feature = "render")]
pub fn pick_alpha(offered: &[wgpu::CompositeAlphaMode]) -> bevy::window::CompositeAlphaMode {
    use bevy::window::CompositeAlphaMode;

    if offered.contains(&wgpu::CompositeAlphaMode::PreMultiplied) {
        CompositeAlphaMode::PreMultiplied
    } else if offered.contains(&wgpu::CompositeAlphaMode::PostMultiplied) {
        CompositeAlphaMode::PostMultiplied
    } else {
        CompositeAlphaMode::Opaque
    }
}

/// Settles the compositing mode of every window that asked to be see-through, before its surface
/// is made.
///
/// Asked of a surface made for the question and dropped straight after, since the one Bevy keeps
/// is made and configured in the same step, and a surface's capabilities are only known once it
/// exists. A surface with no swapchain on it is allowed alongside the real one on every backend.
/// Written into the extracted window every frame, since extraction copies the main world's
/// request over it each time, though only the frame the surface is made reads it.
#[cfg(feature = "render")]
fn settle_alpha(
    mut windows: bevy::ecs::system::Query<(
        bevy::ecs::entity::Entity,
        &mut bevy::render::view::ExtractedWindow,
        &bevy::window::RawHandleWrapper,
    )>,
    instance: bevy::ecs::system::Res<bevy::render::renderer::RenderInstance>,
    adapter: bevy::ecs::system::Res<bevy::render::renderer::RenderAdapter>,
) {
    use bevy::window::CompositeAlphaMode;

    let Ok(mut settled) = ALPHA.lock() else { return };
    let settled = settled.get_or_insert_with(Default::default);

    for (entity, mut window, handle) in &mut windows {
        if !matches!(
            window.alpha_mode,
            CompositeAlphaMode::PreMultiplied | CompositeAlphaMode::PostMultiplied
        ) {
            continue;
        }

        let mode = *settled.entry(entity).or_insert_with(|| {
            let target = wgpu::SurfaceTargetUnsafe::RawHandle {
                raw_display_handle: Some(handle.get_display_handle()),
                raw_window_handle: handle.get_window_handle(),
            };

            // SAFETY: the handles come from a window that is open, as the ones Bevy makes its own
            // surface from do, and the surface is dropped before this returns.
            let offered = match unsafe { instance.create_surface_unsafe(target) } {
                Ok(surface) => surface.get_capabilities(&adapter).alpha_modes,
                Err(_) => Vec::new(),
            };

            let mode = pick_alpha(&offered);

            if mode == CompositeAlphaMode::Opaque {
                bevy::log::warn!(
                    "The window asked to be see-through, and its surface offers only {offered:?}, so it is drawn opaque and what would have been clear is black."
                );
            } else {
                bevy::log::info!("The window is see-through, composited as {mode:?}.");
            }

            mode
        });

        window.alpha_mode = mode;
    }
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::window::CompositeAlphaMode;

    #[test]
    fn a_see_through_window_takes_premultiplied_then_straight_then_opaque() {
        use wgpu::CompositeAlphaMode as Offered;

        assert_eq!(
            super::pick_alpha(&[Offered::Opaque, Offered::PostMultiplied, Offered::PreMultiplied]),
            CompositeAlphaMode::PreMultiplied
        );
        assert_eq!(
            super::pick_alpha(&[Offered::Opaque, Offered::PostMultiplied]),
            CompositeAlphaMode::PostMultiplied
        );
        assert_eq!(super::pick_alpha(&[Offered::Opaque, Offered::Inherit]), CompositeAlphaMode::Opaque);
        assert_eq!(super::pick_alpha(&[]), CompositeAlphaMode::Opaque);
    }
}

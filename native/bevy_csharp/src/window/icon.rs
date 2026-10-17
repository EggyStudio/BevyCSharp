//! The picture the platform shows for a window, in its title bar and its task switcher.
//!
//! Bevy's `Window` carries no icon, so it is set on winit's window itself, which lives on the main
//! thread and is made a frame or so after the app starts, from an image that loads in the
//! meantime. A request is therefore kept until both are there, and put on by a system that runs
//! on the main thread. Windows and X11 show it. Wayland takes an app's icon from its desktop file
//! and macOS from its bundle, so the request is taken and changes nothing there.

use crate::interop::status;

/// What was asked and not yet put on, each window's entity and the image for it.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
struct Pending(Vec<(bevy::ecs::entity::Entity, bevy::asset::Handle<bevy::image::Image>)>);

/// Adds what puts each requested icon on its window once both have arrived.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    app.init_resource::<Pending>();
    app.add_systems(bevy::app::Last, put_on);
}

/// Puts each requested icon on its window where the image has loaded and the window exists,
/// keeping the rest for a later frame, and drops one whose image will not convert.
#[cfg(feature = "render")]
fn put_on(
    mut pending: bevy::ecs::system::ResMut<Pending>,
    images: bevy::ecs::system::Res<bevy::asset::Assets<bevy::image::Image>>,
    _main: bevy::ecs::system::NonSendMarker,
) {
    if pending.0.is_empty() {
        return;
    }

    pending.0.retain(|(window, handle)| {
        let Some(image) = images.get(handle) else {
            return true;
        };

        let opened = bevy::winit::WINIT_WINDOWS.with_borrow(|windows| windows.get_window(*window).is_some());
        if !opened {
            return true;
        }

        let Ok(picture) = image.clone().try_into_dynamic() else {
            bevy::log::warn!("[BevyCSharp] a window icon's image is in a format that cannot be read as pixels, so the window keeps its own");
            return false;
        };
        let rgba = picture.to_rgba8();
        let (width, height) = rgba.dimensions();
        let icon = match winit::window::Icon::from_rgba(rgba.into_raw(), width, height) {
            Ok(icon) => icon,
            Err(error) => {
                bevy::log::warn!("[BevyCSharp] a window icon was refused by the platform's windowing: {error}");
                return false;
            }
        };

        bevy::winit::WINIT_WINDOWS.with_borrow(|windows| {
            if let Some(opened) = windows.get_window(*window) {
                opened.set_window_icon(Some(icon));
            }
        });
        false
    });
}

/// Asks for a window to be shown by an image, as the platform's icon for it, put on once the
/// image has loaded and the window has opened. `window` is as the rest of the window's calls take
/// it, [`super::PRIMARY`] for the primary one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_window_set_icon(window: u64, image: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (window, image);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };
                let Some(entity) = super::window_entity(world, window) else {
                    return status::NOT_PRESENT;
                };
                let Some(mut pending) = world.get_resource_mut::<Pending>() else {
                    // An offscreen run opens no window to show one on.
                    return status::NOT_PRESENT;
                };

                pending.0.retain(|(asked, _)| *asked != entity);
                pending.0.push((entity, handle));
                status::OK
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::asset::Handle;
    use bevy::image::Image;
    use bevy::prelude::World;
    use bevy::window::{PrimaryWindow, Window};

    use super::Pending;
    use crate::interop::status;
    use crate::state::loan_world;

    /// An icon asked for a window waits for it, one a window at a time, and a run with no window to
    /// show one on, or a request naming no window, is refused.
    #[test]
    fn an_icon_is_kept_for_its_window_until_it_can_be_put_on() {
        let mut world = World::new();
        let image: Handle<Image> = Handle::default();
        let key = crate::assets::key_for(&mut world, image.untyped());
        let window = world.spawn((Window::default(), PrimaryWindow)).id();

        // No icons kept, as an offscreen run has none.
        assert_eq!(loan_world(&mut world, || super::bcs_window_set_icon(super::super::PRIMARY, key)), status::NOT_PRESENT);

        world.init_resource::<Pending>();
        assert_eq!(loan_world(&mut world, || super::bcs_window_set_icon(super::super::PRIMARY, key)), status::OK);
        assert_eq!(loan_world(&mut world, || super::bcs_window_set_icon(window.to_bits(), key)), status::OK);
        assert_eq!(world.resource::<Pending>().0.len(), 1, "a second icon replaces the first");

        let plain = world.spawn_empty().id();
        assert_eq!(loan_world(&mut world, || super::bcs_window_set_icon(plain.to_bits(), key)), status::NOT_PRESENT);
    }
}

//! The interface, as a page.
//!
//! One document holds the whole interface, the way a page holds a whole application. What used to
//! be a panel with a document of its own, a layer, a placement and a rebuild whenever another one
//! opened, is a piece of markup put into that page. Where things sit is CSS; what they look like is
//! CSS; which one is in front is CSS. None of it is arithmetic on this side any more.
//!
//! What is here is the join between that page and the engine: the window's size, the window's
//! input, and the page's pixels.

#[cfg(feature = "editor")]
use crate::interop::status;

/// What the managed side is told happened, and to which element.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsDomEvent {
    /// Which kind of thing happened, as `event_kind` names them.
    pub kind: i32,

    /// The element it happened to.
    pub node: u64,
}

/// The kinds of report the page makes.
pub mod event_kind {
    /// An element was clicked.
    pub const CLICK: i32 = 0;

    /// An element's value changed.
    pub const INPUT: i32 = 1;

    /// An element took the keyboard.
    pub const FOCUS: i32 = 2;
}

#[cfg(feature = "editor")]
pub use live::install;

#[cfg(feature = "editor")]
mod live {
    use bcs_dom::{Button, Interface, Report};
    use bevy::asset::RenderAssetUsages;
    use bevy::image::Image;
    use bevy::prelude::*;
    use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat};
    use bevy::window::PrimaryWindow;

    /// The page, its canvas, and what it has reported.
    ///
    /// Not a `Resource`, because a document is not `Send`: Stylo, Taffy and Parley all keep
    /// thread-local state, and a browser engine has no reason to be moved between threads. Bevy
    /// calls that a non-send resource and pins every system touching it to the main thread, which
    /// is the same thread the managed side calls in on.
    #[derive(Default)]
    pub struct Page {
        /// The document, once one has been opened.
        pub interface: Option<Interface>,

        /// The picture it is painted into.
        pub canvas: Handle<Image>,

        /// What it reported since the managed side last looked.
        pub reports: Vec<Report>,
    }

    /// Puts the page into an app.
    pub fn install(app: &mut App) {
        app.init_non_send::<Page>();

        app.add_systems(Startup, spawn_canvas);
        app.add_systems(
            Update,
            (follow_window, feed_pointer, repaint)
                .chain()
                .after(bevy::input::InputSystems),
        );
    }

    /// The camera and the single node the page is drawn on.
    fn spawn_canvas(
        mut commands: Commands,
        mut images: ResMut<Assets<Image>>,
        mut page: NonSendMut<Page>,
        windows: Query<&Window, With<PrimaryWindow>>,
    ) {
        let (width, height) = windows
            .single()
            .map(|window| (window.physical_width().max(1), window.physical_height().max(1)))
            .unwrap_or((1, 1));

        let canvas = images.add(blank(width, height));
        page.canvas = canvas.clone();

        // A camera of the interface's own, over whatever the scene drew, clearing nothing.
        commands.spawn((
            Camera2d,
            Camera {
                order: 2,
                clear_color: ClearColorConfig::None,
                ..default()
            },
            Msaa::Off,
            Name::new("Interface camera"),
        ));

        // One node, the size of the window, showing the page. Everything the interface is, is
        // inside the picture: this is the only entity the engine knows about.
        commands.spawn((
            ImageNode::new(canvas),
            Node {
                position_type: PositionType::Absolute,
                left: Val::Px(0.0),
                top: Val::Px(0.0),
                width: Val::Percent(100.0),
                height: Val::Percent(100.0),
                ..default()
            },
            Pickable::IGNORE,
            Name::new("Interface"),
        ));
    }

    /// An empty picture of a given size.
    fn blank(width: u32, height: u32) -> Image {
        Image::new_fill(
            Extent3d {
                width,
                height,
                depth_or_array_layers: 1,
            },
            TextureDimension::D2,
            &[0, 0, 0, 0],
            TextureFormat::Rgba8UnormSrgb,
            RenderAssetUsages::MAIN_WORLD | RenderAssetUsages::RENDER_WORLD,
        )
    }

    /// Keeps the page and its canvas the size of the window.
    fn follow_window(
        mut images: ResMut<Assets<Image>>,
        mut page: NonSendMut<Page>,
        windows: Query<&Window, With<PrimaryWindow>>,
    ) {
        let Ok(window) = windows.single() else {
            return;
        };

        let width = window.physical_width().max(1);
        let height = window.physical_height().max(1);

        let Some(image) = images.get(&page.canvas) else {
            return;
        };

        if image.width() == width && image.height() == height {
            return;
        }

        let canvas = images.add(blank(width, height));
        page.canvas = canvas;

        if let Some(interface) = page.interface.as_mut() {
            interface.resize(width, height);
        }
    }

    /// Hands the window's pointer to the page.
    fn feed_pointer(
        mut page: NonSendMut<Page>,
        buttons: Res<ButtonInput<MouseButton>>,
        mut wheel: MessageReader<bevy::input::mouse::MouseWheel>,
        windows: Query<&Window, With<PrimaryWindow>>,
    ) {
        let Ok(window) = windows.single() else {
            return;
        };

        let Some(interface) = page.interface.as_mut() else {
            return;
        };

        if let Some(at) = window.cursor_position() {
            interface.moved(at.x, at.y);
        }

        for (bevy, ours) in [
            (MouseButton::Left, Button::Left),
            (MouseButton::Right, Button::Right),
            (MouseButton::Middle, Button::Middle),
        ] {
            if buttons.just_pressed(bevy) {
                interface.pressed(ours);
            }

            if buttons.just_released(bevy) {
                interface.released(ours);
            }
        }

        // A line is worth this many pixels, which is what a browser assumes of a wheel with
        // detents and what makes one notch move a list by about one row.
        const LINE: f64 = 20.0;

        for rolled in wheel.read() {
            let scale = match rolled.unit {
                bevy::input::mouse::MouseScrollUnit::Line => LINE,
                bevy::input::mouse::MouseScrollUnit::Pixel => 1.0,
            };

            // Away from the hand scrolls a list up, which is the opposite sign to the one the
            // wheel reports.
            interface.scroll(f64::from(rolled.x) * -scale, f64::from(rolled.y) * -scale);
        }

        let reported = interface.drain();
        page.reports.extend(reported);
    }

    /// Lays the page out and paints it, when anything has changed.
    fn repaint(mut images: ResMut<Assets<Image>>, mut page: NonSendMut<Page>) {
        let canvas = page.canvas.clone();

        let Some(interface) = page.interface.as_mut() else {
            return;
        };

        if !interface.is_dirty() {
            return;
        }

        interface.resolve();

        let Some(mut image) = images.get_mut(&canvas) else {
            return;
        };

        let Some(pixels) = image.data.as_mut() else {
            return;
        };

        if pixels.len() != interface.pixels() {
            return;
        }

        interface.paint(pixels);
    }
}

// ---------------------------------------------------------------------------------------------
// The ABI.
//
// Every entry point exists in every profile, answering `UNSUPPORTED` where the page is not built,
// because `LibraryImport` resolves lazily and a missing symbol would throw at first call rather
// than at load.
//
// A node is handed over as its index plus one, so that zero means no node. The document itself is
// index zero and nothing on the managed side has any business addressing it.
// ---------------------------------------------------------------------------------------------

/// Turns a node into a handle.
#[cfg(feature = "editor")]
fn handle(node: usize) -> u64 {
    node as u64 + 1
}

/// Turns a handle back into a node, refusing zero.
#[cfg(feature = "editor")]
fn node(handle: u64) -> Option<usize> {
    (handle > 0).then(|| handle as usize - 1)
}

/// Runs something against the open page.
#[cfg(feature = "editor")]
fn with_page<F: FnOnce(&mut bcs_dom::Interface) -> i32>(f: F) -> i32 {
    crate::state::with_world(|world| {
        let Some(mut page) = world.get_non_send_mut::<live::Page>() else {
            return status::INVALID_STATE;
        };

        let Some(interface) = page.interface.as_mut() else {
            return status::NOT_PRESENT;
        };

        f(interface)
    })
}

/// Like [`with_page`], for the calls that answer a handle rather than a status.
#[cfg(feature = "editor")]
fn ask_page<F: FnOnce(&mut bcs_dom::Interface) -> u64>(f: F) -> u64 {
    crate::state::with_world_opt(|world| {
        let mut page = world.get_non_send_mut::<live::Page>()?;
        let interface = page.interface.as_mut()?;
        Some(f(interface))
    })
    .flatten()
    .unwrap_or(0)
}

/// Opens the page.
///
/// The whole interface is one document, so this is called once. Markup rather than a path, because
/// the managed side knows where its assets are and because an interface a game builds at runtime
/// never was a file.
///
/// # Safety
/// `html` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_open(html: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = html;
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(html) = (unsafe { crate::interop::cstr_to_string(html) }) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let (width, height) = {
                    let Some(page) = world.get_non_send::<live::Page>() else {
                        return status::INVALID_STATE;
                    };

                    let images = world.resource::<bevy::asset::Assets<bevy::image::Image>>();
                    match images.get(&page.canvas) {
                        Some(image) => (image.width(), image.height()),
                        None => (1, 1),
                    }
                };

                let opened = bcs_dom::Interface::open(&html, width, height);

                let Some(mut page) = world.get_non_send_mut::<live::Page>() else {
                    return status::INVALID_STATE;
                };

                page.interface = Some(opened);
                page.reports.clear();
                status::OK
            })
        }
    })
}

/// Takes the page down.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_close() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            crate::state::with_world(|world| {
                let Some(mut page) = world.get_non_send_mut::<live::Page>() else {
                    return status::INVALID_STATE;
                };

                page.interface = None;
                page.reports.clear();
                status::OK
            })
        }
    })
}

/// The element carrying an id, or zero.
///
/// # Safety
/// `id` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_element(id: *const core::ffi::c_char) -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = id;
            0
        }

        #[cfg(feature = "editor")]
        {
            let Some(id) = (unsafe { crate::interop::cstr_to_string(id) }) else {
                return 0;
            };

            ask_page(|page| page.element(&id).map(handle).unwrap_or(0))
        }
    })
}

/// The first element a selector matches, or zero.
///
/// # Safety
/// `selector` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_select(selector: *const core::ffi::c_char) -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = selector;
            0
        }

        #[cfg(feature = "editor")]
        {
            let Some(selector) = (unsafe { crate::interop::cstr_to_string(selector) }) else {
                return 0;
            };

            ask_page(|page| page.select(&selector).map(handle).unwrap_or(0))
        }
    })
}

/// Makes an element, unattached until it is appended.
///
/// # Safety
/// `tag` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_create(tag: *const core::ffi::c_char) -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = tag;
            0
        }

        #[cfg(feature = "editor")]
        {
            let Some(tag) = (unsafe { crate::interop::cstr_to_string(tag) }) else {
                return 0;
            };

            ask_page(|page| handle(page.create(&tag)))
        }
    })
}

/// Puts an element inside another, at the end.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_append(parent: u64, child: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (parent, child);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let (Some(parent), Some(child)) = (node(parent), node(child)) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.append(parent, child);
                status::OK
            })
        }
    })
}

/// Takes an element out of the page.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_remove(element: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = element;
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.remove(element);
                status::OK
            })
        }
    })
}

/// Puts markup inside an element, replacing what was there.
///
/// How a panel arrives and how it leaves: one page holds the interface, and a panel is a piece of
/// markup put into it.
///
/// # Safety
/// `html` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_set_html(element: u64, html: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, html);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            let Some(html) = (unsafe { crate::interop::cstr_to_string(html) }) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.set_html(element, &html);
                status::OK
            })
        }
    })
}

/// An element's text.
///
/// # Safety
/// `out` must point to `capacity` writable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_get_text(element: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, out, capacity);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            let text = crate::state::with_world_opt(|world| {
                let mut page = world.get_non_send_mut::<live::Page>()?;
                let interface = page.interface.as_mut()?;
                Some(interface.text(element))
            })
            .flatten();

            let Some(text) = text else {
                return status::NOT_PRESENT;
            };

            unsafe { crate::interop::write_text(&text, out, capacity) }
        }
    })
}

/// Sets an element's text.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_set_text(element: u64, text: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, text);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.set_text(element, &text);
                status::OK
            })
        }
    })
}

/// Sets an attribute.
///
/// This is how state reaches the interface: a class changes, and every rule written against that
/// class applies. Nothing on this side works out what that should look like.
///
/// # Safety
/// `name` and `value` must be NUL-terminated UTF-8 strings.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_set_attribute(
    element: u64,
    name: *const core::ffi::c_char,
    value: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, name, value);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            let (Some(name), Some(value)) = (unsafe { crate::interop::cstr_to_string(name) }, unsafe {
                crate::interop::cstr_to_string(value)
            }) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.set_attribute(element, &name, &value);
                status::OK
            })
        }
    })
}

/// What an attribute says.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string, and `out` must point to `capacity` writable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_get_attribute(
    element: u64,
    name: *const core::ffi::c_char,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, name, out, capacity);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            let value = crate::state::with_world_opt(|world| {
                let page = world.get_non_send::<live::Page>()?;
                let interface = page.interface.as_ref()?;
                interface.attribute(element, &name)
            })
            .flatten();

            // An attribute that is not there reads as nothing rather than as a failure, because
            // asking whether a class list is empty is an ordinary question.
            let value = value.unwrap_or_default();

            unsafe { crate::interop::write_text(&value, out, capacity) }
        }
    })
}

/// Takes an attribute off.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_clear_attribute(
    element: u64,
    name: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, name);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.clear_attribute(element, &name);
                status::OK
            })
        }
    })
}

/// The box an element was laid out in, as left, top, width and height.
///
/// # Safety
/// `out` must point to four writable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_rect(element: u64, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (element, out);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            if out.is_null() {
                return status::NULL_ARG;
            }

            with_page(|page| {
                // Whatever was asked for is answered against the current tree, not the one that was
                // laid out last frame.
                page.resolve();

                let Some((left, top, width, height)) = page.rect(element) else {
                    return status::NOT_PRESENT;
                };

                unsafe {
                    out.write(left);
                    out.add(1).write(top);
                    out.add(2).write(width);
                    out.add(3).write(height);
                }

                status::OK
            })
        }
    })
}

/// Gives an element the keyboard.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_focus(element: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = element;
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let Some(element) = node(element) else {
                return status::NULL_ARG;
            };

            with_page(|page| {
                page.focus(element);
                status::OK
            })
        }
    })
}

/// Takes the keyboard away from whatever holds it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_blur() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            with_page(|page| {
                page.blur();
                status::OK
            })
        }
    })
}

/// Whatever holds the keyboard, or zero.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_focused() -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            0
        }

        #[cfg(feature = "editor")]
        {
            ask_page(|page| page.focused().map(handle).unwrap_or(0))
        }
    })
}

/// The element at a point, or zero.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_dom_hit(x: f32, y: f32) -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (x, y);
            0
        }

        #[cfg(feature = "editor")]
        {
            ask_page(|page| {
                page.resolve();
                page.hit(x, y).map(handle).unwrap_or(0)
            })
        }
    })
}

/// Takes what the page reported since the last call.
///
/// # Safety
/// `out` must point to `capacity` writable [`BcsDomEvent`] values.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_dom_events(out: *mut BcsDomEvent, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (out, capacity);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            if out.is_null() || capacity <= 0 {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let Some(mut page) = world.get_non_send_mut::<live::Page>() else {
                    return status::INVALID_STATE;
                };

                let taken = page.reports.len().min(capacity as usize);

                for (slot, report) in page.reports.drain(..taken).enumerate() {
                    let (kind, element) = match report {
                        bcs_dom::Report::Click(element) => (event_kind::CLICK, element),
                        bcs_dom::Report::Input(element) => (event_kind::INPUT, element),
                        bcs_dom::Report::Focus(element) => (event_kind::FOCUS, element),
                    };

                    unsafe {
                        out.add(slot).write(BcsDomEvent {
                            kind,
                            node: handle(element),
                        });
                    }
                }

                taken as i32
            })
        }
    })
}

/// Moves, presses or releases the pointer, as though a hand had.
///
/// What a test drives the interface with. `action` is 0 to move, 1 to press and 2 to release;
/// `button` is 0 for left, 1 for right and 2 for middle.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_input_pointer(x: f32, y: f32, action: i32, button: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (x, y, action, button);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            let button = match button {
                1 => bcs_dom::Button::Right,
                2 => bcs_dom::Button::Middle,
                _ => bcs_dom::Button::Left,
            };

            with_page(|page| {
                // Moved first whatever the action is, because a press somewhere the pointer has
                // never been is a press on whatever it was last over.
                page.moved(x, y);

                match action {
                    1 => page.pressed(button),
                    2 => page.released(button),
                    _ => {}
                }

                status::OK
            })
        }
    })
}

/// Rolls the wheel where the pointer is, in the lines a wheel with detents reports.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_input_wheel(x: f32, y: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (x, y);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            // A line is worth this many pixels, which is what a browser assumes of a wheel with
            // detents and what makes one notch move a list by about one row.
            const LINE: f64 = 20.0;

            with_page(|page| {
                page.scroll(f64::from(x) * LINE, f64::from(y) * LINE);
                status::OK
            })
        }
    })
}

//! Shared C ABI vocabulary: status codes, POD structs and the panic guard.
//!
//! Every `extern "C"` entry point in this crate funnels through [`guard`] so a Rust
//! panic can never unwind across the FFI boundary into the .NET runtime (which is
//! undefined behavior). Failures surface to C# as a negative [`BcsStatus`].

use core::ffi::c_char;
use core::panic::AssertUnwindSafe;
use core::slice;

// The structs of the render, the window and the interface, kept in modules of their own and named
// here, where the rest of the bridge reaches every struct of the boundary.
pub use crate::interop_render::*;
pub use crate::interop_window::*;

/// Result codes returned by the C ABI. Non-negative values are successes; a
/// function that returns a count returns the count itself on success.
pub mod status {
    /// Call succeeded.
    pub const OK: i32 = 0;
    /// A Rust panic was caught at the boundary.
    pub const PANIC: i32 = -1;
    /// A required pointer argument was null.
    pub const NULL_ARG: i32 = -2;
    /// The call needs a live `&mut World`, but no system callback is active on this thread.
    pub const NO_WORLD: i32 = -3;
    /// The referenced entity does not exist (or was already despawned).
    pub const NO_ENTITY: i32 = -4;
    /// The referenced component id was never registered.
    pub const NO_COMPONENT: i32 = -5;
    /// The entity does not carry the requested component.
    pub const NOT_PRESENT: i32 = -6;
    /// The output buffer was too small; the required length is reported separately.
    pub const BUFFER_TOO_SMALL: i32 = -7;
    /// The app was already consumed by a previous `bcs_app_run`.
    pub const ALREADY_RUNNING: i32 = -8;
    /// The operation is not valid at this point in the app lifecycle.
    pub const INVALID_STATE: i32 = -9;
    /// The requested feature is not compiled into this build of the native library.
    pub const UNSUPPORTED: i32 = -10;
}

/// One contiguous run of a component's storage, handed to C# for zero-copy iteration.
///
/// `entities` and `data` point directly into Bevy's table storage and stay valid only
/// for the duration of the system callback that requested them; any structural change
/// (spawn, despawn, insert, remove) invalidates every outstanding chunk.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsChunk {
    /// `len` entity handles. Bevy's `Entity` is `repr(C, align(8))` and documented to be
    /// bit-equivalent to the `u64` produced by `Entity::to_bits`, so C# reads these directly.
    pub entities: *const u64,
    /// `len * stride` bytes of tightly packed component data, writable in place.
    pub data: *mut u8,
    /// Number of entities in this chunk.
    pub len: u32,
    /// Size in bytes of one component, matching the registered layout.
    pub stride: u32,
}

impl BcsChunk {
    /// An empty chunk, used to zero-fill unused slots in the caller's buffer.
    pub const EMPTY: BcsChunk = BcsChunk {
        entities: core::ptr::null(),
        data: core::ptr::null_mut(),
        len: 0,
        stride: 0,
    };
}

/// Frame-scoped snapshot of Bevy's `Time` mirrored into C#.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsTime {
    /// Seconds since app start.
    pub elapsed_seconds: f64,
    /// Seconds since the previous frame, clamped by Bevy's max delta.
    pub delta_seconds: f64,
    /// Unclamped seconds since the previous frame.
    pub raw_delta_seconds: f64,
    /// Frames rendered since app start.
    pub frame_count: u64,
    /// Seconds one `FixedUpdate` step covers. Constant unless the rate is changed, so it is
    /// meaningful whenever it is read, including from a system outside that schedule.
    pub fixed_delta_seconds: f64,
}

/// Frame-scoped snapshot of Bevy's input state mirrored into C#.
///
/// Keyboard state is a bitset over Bevy `KeyCode` discriminants; C# owns the
/// `Key` -> `KeyCode` mapping and indexes the same bits.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsInput {
    /// Cursor position in physical window pixels.
    pub mouse_x: f32,
    /// Cursor position in physical window pixels.
    pub mouse_y: f32,
    /// Cursor movement since the previous frame.
    pub mouse_delta_x: f32,
    /// Cursor movement since the previous frame.
    pub mouse_delta_y: f32,
    /// Horizontal scroll since the previous frame.
    pub wheel_x: f32,
    /// Vertical scroll since the previous frame.
    pub wheel_y: f32,
    /// Bit `n` set while key `n` is held.
    pub keys_down: [u64; crate::input::KEY_WORDS],
    /// Bit `n` set on the frame key `n` went down.
    pub keys_pressed: [u64; crate::input::KEY_WORDS],
    /// Bit `n` set on the frame key `n` went up.
    pub keys_released: [u64; crate::input::KEY_WORDS],
    /// Bit `n` set while mouse button `n` is held.
    pub mouse_down: u32,
    /// Bit `n` set on the frame mouse button `n` went down.
    pub mouse_pressed: u32,
    /// Bit `n` set on the frame mouse button `n` went up.
    pub mouse_released: u32,
    /// Bytes of `text` that are in use.
    pub text_len: u32,
    /// Number of entries of `touches` that are in use.
    pub touch_count: u32,
    /// What the keyboard produced this frame, as UTF-8. See [`TEXT_CAPACITY`].
    pub text: [u8; TEXT_CAPACITY],
    /// Touches in progress, however many of them fit.
    pub touches: [BcsTouch; TOUCH_CAPACITY],
}

/// How many bytes of typed text one frame can carry.
///
/// A frame holds at most a few keystrokes, so this is generous. Text past it is dropped rather
/// than split, because half a UTF-8 sequence is worse than a missing character.
pub const TEXT_CAPACITY: usize = 32;

/// How many simultaneous touches one frame can carry.
pub const TOUCH_CAPACITY: usize = 8;

/// One finger on a touchscreen.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsTouch {
    /// Identifies this finger for as long as it stays down.
    pub id: u64,
    /// Position in physical window pixels.
    pub x: f32,
    /// Position in physical window pixels.
    pub y: f32,
    /// `0` held, `1` started this frame, `2` ended this frame.
    pub phase: i32,
    /// Padding, so the array strides evenly on every target.
    pub _pad: i32,
}

impl Default for BcsInput {
    fn default() -> Self {
        Self {
            mouse_x: 0.0,
            mouse_y: 0.0,
            mouse_delta_x: 0.0,
            mouse_delta_y: 0.0,
            wheel_x: 0.0,
            wheel_y: 0.0,
            keys_down: [0; crate::input::KEY_WORDS],
            keys_pressed: [0; crate::input::KEY_WORDS],
            keys_released: [0; crate::input::KEY_WORDS],
            mouse_down: 0,
            mouse_pressed: 0,
            mouse_released: 0,
            text_len: 0,
            touch_count: 0,
            text: [0; TEXT_CAPACITY],
            touches: [BcsTouch::default(); TOUCH_CAPACITY],
        }
    }
}

/// Everything C# needs to refresh its mirrored resources at the top of a frame.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsFrameState {
    /// Timing snapshot.
    pub time: BcsTime,
    /// Input snapshot.
    pub input: BcsInput,
}

/// Window/app configuration passed from C# at construction time.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsConfig {
    /// UTF-8 window title. May be null in headless builds.
    pub title: *const c_char,
    /// Requested window width in logical pixels.
    pub width: u32,
    /// Requested window height in logical pixels.
    pub height: u32,
    /// Non-zero to present with vsync.
    pub vsync: u32,
    /// Non-zero to build the app without a window even in a `render` build.
    pub headless: u32,
    /// Frames per second cap for headless runs; `0` runs as fast as possible.
    pub headless_fps: u32,
    /// Number of frames to run before exiting; `0` runs until an exit is requested.
    /// Used by tests to drive a deterministic number of ticks.
    pub headless_frames: u32,
    /// Graphics API to pin the renderer to. `0` leaves the choice to wgpu; see
    /// `GraphicsBackend` on the managed side for the rest. Ignored when headless.
    pub backend: u32,
    /// How many times a second the `FixedUpdate` schedule should run. `0` keeps Bevy's own
    /// default, which is 64 Hz.
    pub fixed_hz: f64,
    /// Directory assets are loaded from, or null for Bevy's default of `assets` beside the
    /// executable. May be null in any build.
    pub asset_root: *const c_char,
    /// Non-zero to reload an asset when its file changes on disk. Needs a build whose profile
    /// carries the watcher, and costs a thread watching the asset directory.
    pub watch_assets: u32,
    /// Non-zero to build in the HTML and CSS interface. Needs the editor profile, and brings a
    /// camera and a set of systems that an app not using it has no reason to carry.
    pub gui: u32,
    /// Non-zero to draw with no window, into an image that a capture reads back. Needs a `render`
    /// build and is ignored when `headless` is set, which asks for no renderer at all. `width` and
    /// `height` size the image the way they would size the window.
    pub offscreen: u32,
    /// How many world units a meter is, for every spatial sound that does not say otherwise. `0`
    /// keeps Bevy's own of one, for a world measured in meters.
    pub spatial_scale: f32,
    /// How many meshlet clusters the GPU keeps room for at once, or `0` for no meshlets. Needs a
    /// build with the `meshlet` feature and a GPU with 64-bit texture atomics.
    pub meshlet_clusters: u32,
    /// Non-zero to measure how long every render pass takes. Costs timestamps written around each
    /// pass and read back every frame.
    pub gpu_timings: u32,
    /// Non-zero to light with Bevy's Solari where a camera asks, which needs a build with the
    /// `solari` feature and an adapter that traces rays.
    pub ray_traced_lighting: u32,
    /// Non-zero to make the window see-through where what is drawn has no alpha, where the
    /// platform allows it. Ignored without a window.
    pub transparent: u32,
    /// Non-zero to have the desktop draw the window's title bar where it only does so for X11
    /// windows, which is GNOME on Wayland. See `title_bar::prefer_desktop_title_bar`.
    pub desktop_title_bar: u32,
    /// The player's own directory, registered with Bevy as the `user` asset source so a texture or
    /// a model the game wrote there loads as `user://…`, or null for no such source.
    pub user_root: *const c_char,
    /// Non-zero to open the window at `x` and `y` rather than where the platform puts it, which a
    /// game reopening where it was closed asks for. Wayland places every window itself and
    /// ignores it.
    pub has_position: u32,
    /// Where the window's top left corner opens, in physical pixels from the desktop's.
    pub x: i32,
    /// See `x`.
    pub y: i32,
    /// Non-zero to add Bevy's wireframe plugins, for 3D meshes and 2D ones, which an app that
    /// draws a mesh as its edges asks for. Each looks at every mesh of its kind every frame
    /// whether or not one is drawn so, which an app with no wireframes should not pay for.
    pub wireframes: u32,
    /// Non-zero to add Bevy's frame time diagnostics and the plugin that logs every diagnostic once
    /// a second, which Bevy's stress tests add to say how fast their frames run.
    pub log_frame_times: u32,
    /// The window's scale factor in place of the display's, or `0` for the display's. Bevy's stress
    /// tests hold it at one, so their window is as many pixels as they say on a display that scales.
    pub scale_factor: f32,
    /// Seconds each frame advances the clock by, in place of the machine's, or `0` for the
    /// machine's. Bevy's `TimeUpdateStrategy::ManualDuration`, so a test or a capture runs the same
    /// on every machine, a frame at a time.
    pub frame_seconds: f64,
    /// Non-zero to add Bevy's mesh picking, which finds the mesh under a pointer for its events.
    /// Needs the `render` feature, and costs a ray cast at every mesh as the pointer moves.
    pub mesh_picking: u32,
    /// Non-zero to add the weather, a sky with clouds, fog, rain, snow and thunder around Bevy's
    /// atmosphere, on every camera marked as a weather camera. Needs the `render` feature, and is
    /// kept out where meshlets run. See `render::weather`.
    pub weather: u32,
}

/// Where the window is and how large, as `bcs_window_place` reads it back.
///
/// One struct for what a game keeps between runs, so it is read in one call rather than three.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsWindowPlace {
    /// Non-zero when the platform has said where the window is, which Wayland never does.
    pub has_position: i32,
    /// The window's top left corner in physical pixels from the desktop's, when `has_position`.
    pub x: i32,
    /// See `x`.
    pub y: i32,
    /// The window's size in logical pixels.
    pub width: u32,
    /// See `width`.
    pub height: u32,
    /// Non-zero while the window is maximized.
    pub maximized: i32,
}

// The frame snapshot is written straight into memory C# owns, so both sides have to agree on its
// shape exactly. These pin the sizes down here; `InputTests` asserts the same numbers on the
// managed side, so changing either half without the other stops the build or fails a test rather
// than quietly reading input from the wrong bytes.
const _: () = assert!(core::mem::size_of::<BcsTime>() == 40);
const _: () = assert!(core::mem::size_of::<BcsTouch>() == 24);
const _: () = assert!(core::mem::size_of::<BcsInput>() == 320);
const _: () = assert!(core::mem::size_of::<BcsFrameState>() == 360);

/// Copies a string out to a caller's buffer, and reports how long it is.
///
/// The convention every text-returning entry point follows, because C# cannot know how long a
/// string is before asking for it. The return value is the length in bytes, whether or not it
/// fitted, so a caller that guessed too small learns the right size and asks again rather than
/// receiving a truncated answer. Nothing is written when the buffer is too small, and the bytes
/// written are never NUL-terminated, and the length is the answer.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
pub unsafe fn write_text(text: &str, out: *mut u8, capacity: i32) -> i32 {
    let bytes = text.as_bytes();
    let needed = bytes.len() as i32;

    if capacity < needed || out.is_null() {
        return needed;
    }

    unsafe { core::ptr::copy_nonoverlapping(bytes.as_ptr(), out, bytes.len()) };
    needed
}

/// Runs `f`, converting any panic into [`status::PANIC`] instead of unwinding into .NET.
///
/// What the panic said is kept for the managed side to read with `bcs_last_panic`, and is not
/// written as a crash, since it comes back as a status (`crash.rs`).
pub fn guard<F: FnOnce() -> i32>(f: F) -> i32 {
    match crate::profile::crossing(|| crate::crash::guarded(|| std::panic::catch_unwind(AssertUnwindSafe(f)))) {
        Ok(v) => v,
        Err(_) => status::PANIC,
    }
}

/// Runs `f`, converting any panic into `fallback`.
pub fn guard_with<T, F: FnOnce() -> T>(fallback: T, f: F) -> T {
    match crate::profile::crossing(|| crate::crash::guarded(|| std::panic::catch_unwind(AssertUnwindSafe(f)))) {
        Ok(v) => v,
        Err(_) => fallback,
    }
}

/// Borrows a caller-provided array, treating a null pointer or non-positive length as empty.
///
/// # Safety
/// `ptr` must be valid for `len` reads of `T` when both are non-trivial.
pub unsafe fn opt_slice<'a, T>(ptr: *const T, len: i32) -> &'a [T] {
    if ptr.is_null() || len <= 0 {
        &[]
    } else {
        unsafe { slice::from_raw_parts(ptr, len as usize) }
    }
}

/// Copies a NUL-terminated UTF-8 C string into an owned `String`, lossily.
///
/// # Safety
/// `ptr` must be null or point to a NUL-terminated byte string.
pub unsafe fn cstr_to_string(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        return None;
    }
    let c = unsafe { core::ffi::CStr::from_ptr(ptr) };
    Some(c.to_string_lossy().into_owned())
}

#[cfg(test)]
mod tests {
    use super::*;

    /// Calls [`write_text`] with a buffer of `capacity`, reporting what it needed and wrote.
    fn probe(text: &str, capacity: usize) -> (i32, Vec<u8>) {
        let mut buffer = vec![0xAAu8; capacity];
        let needed = unsafe { write_text(text, buffer.as_mut_ptr(), capacity as i32) };
        (needed, buffer)
    }

    #[test]
    fn text_reports_what_it_needs_before_it_writes_anything() {
        // The convention every entry point carrying text follows: call with nothing, learn the
        // length, call again with a buffer that size.
        let needed = unsafe { write_text("hello", core::ptr::null_mut(), 0) };
        assert_eq!(5, needed);

        let (needed, buffer) = probe("hello", 5);
        assert_eq!(5, needed);
        assert_eq!(b"hello", &buffer[..]);
    }

    #[test]
    fn a_buffer_that_is_too_small_is_left_alone() {
        // Reporting the length and writing a truncated answer would both be defensible, but a
        // caller that ignored the length would then be holding half a string with no way to
        // tell. Nothing is written instead.
        let (needed, buffer) = probe("hello", 4);
        assert_eq!(5, needed);
        assert_eq!(vec![0xAA; 4], buffer);
    }

    #[test]
    fn text_is_measured_in_bytes_rather_than_characters() {
        // A caller sizing its buffer by counting characters would come back one call short on
        // anything outside ASCII, so the length reported is the one `memcpy` needs.
        let text = "café";
        assert_eq!(4, text.chars().count());

        let (needed, buffer) = probe(text, 5);
        assert_eq!(5, needed);
        assert_eq!(text.as_bytes(), &buffer[..]);
    }

    #[test]
    fn empty_text_needs_no_buffer() {
        assert_eq!(0, unsafe { write_text("", core::ptr::null_mut(), 0) });
    }

    #[test]
    fn an_absent_array_reads_as_an_empty_one() {
        // Both are how C# spells "nothing to pass", and neither should reach a `from_raw_parts`.
        let empty: &[u32] = unsafe { opt_slice(core::ptr::null::<u32>(), 7) };
        assert!(empty.is_empty());

        let values = [1u32, 2, 3];
        assert!(unsafe { opt_slice(values.as_ptr(), 0) }.is_empty());
        assert!(unsafe { opt_slice(values.as_ptr(), -1) }.is_empty());
        assert_eq!(&values[..], unsafe { opt_slice(values.as_ptr(), 3) });
    }

    #[test]
    fn an_absent_string_reads_as_none() {
        assert_eq!(None, unsafe { cstr_to_string(core::ptr::null()) });

        let text = c"scenes";
        assert_eq!(
            Some("scenes".to_string()),
            unsafe { cstr_to_string(text.as_ptr()) });
    }

    #[test]
    fn a_panic_becomes_a_status_rather_than_an_unwind() {
        // Unwinding into the .NET runtime is undefined behavior, so the guard stands between a bug
        // on this side and a process that dies without saying why.
        // Quietly, so the panics are not kept as the last one said while the crash log's tests
        // read it on threads of their own.
        assert_eq!(status::OK, guard(|| status::OK));
        assert_eq!(-42, crate::crash::quietly(|| guard_with(-42, || panic!("a bug on this side"))));

        let caught = crate::crash::quietly(|| guard(|| panic!("a bug on this side")));

        assert_eq!(status::PANIC, caught);
    }
}

#[cfg(test)]
mod layout {
    use super::BcsInput;

    /// The offsets `NativeInput` on the C# side mirrors, field by field.
    ///
    /// A managed struct that is the right *size* but has a field in the wrong place reads whatever
    /// its neighbor wrote, and no check based on size catches it. One padding field of four bytes
    /// in the mirror is enough to put `text_len` where `touch_count` is while both sides still
    /// measure 320 bytes, and the symptom is that typed text never arrives.
    #[test]
    fn the_input_snapshot_is_laid_out_where_the_mirror_expects() {
        assert_eq!(core::mem::offset_of!(BcsInput, mouse_x), 0);
        assert_eq!(core::mem::offset_of!(BcsInput, wheel_y), 20);
        assert_eq!(core::mem::offset_of!(BcsInput, keys_down), 24);
        assert_eq!(core::mem::offset_of!(BcsInput, keys_pressed), 40);
        assert_eq!(core::mem::offset_of!(BcsInput, keys_released), 56);
        assert_eq!(core::mem::offset_of!(BcsInput, mouse_down), 72);
        assert_eq!(core::mem::offset_of!(BcsInput, mouse_pressed), 76);
        assert_eq!(core::mem::offset_of!(BcsInput, mouse_released), 80);
        assert_eq!(core::mem::offset_of!(BcsInput, text_len), 84);
        assert_eq!(core::mem::offset_of!(BcsInput, touch_count), 88);
        assert_eq!(core::mem::offset_of!(BcsInput, text), 92);
        assert_eq!(core::mem::offset_of!(BcsInput, touches), 128);
        assert_eq!(core::mem::size_of::<BcsInput>(), 320);
    }
}


//! Shared C ABI vocabulary: status codes, POD structs and the panic guard.
//!
//! Every `extern "C"` entry point in this crate funnels through [`guard`] so a Rust
//! panic can never unwind across the FFI boundary into the .NET runtime (which is
//! undefined behavior). Failures surface to C# as a negative [`BcsStatus`].

use core::ffi::c_char;
use core::panic::AssertUnwindSafe;
use core::slice;

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
}

/// How a camera should see, passed from C# when one is spawned.
///
/// A struct rather than an argument list because the list was already going to be ten long, and a
/// camera gains parameters as the renderer is bridged further.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsCameraConfig {
    /// `0` for perspective, `1` for orthographic.
    pub projection: i32,
    /// Vertical field of view in degrees. Perspective only.
    pub fov_degrees: f32,
    /// How much of the world fits vertically, in world units. Orthographic only.
    pub ortho_height: f32,
    /// Nearest visible distance.
    pub near: f32,
    /// Furthest visible distance. Ignored by an orthographic camera, which has no horizon.
    pub far: f32,
    /// `0` uses the world's clear colour, `1` the one below, `2` draws over what is already there.
    pub clear_mode: i32,
    /// Clear colour, used when `clear_mode` is `1`.
    pub clear: [f32; 4],
    /// Draw order. A camera with a higher order draws over one with a lower.
    pub order: i32,
    /// Non-zero to draw into part of the window rather than all of it.
    pub has_viewport: i32,
    /// Left, top, width and height of that part, in physical pixels.
    pub viewport: [u32; 4],
    /// Which render layers this camera sees, as a bit per layer. `0` means the default layer.
    pub layers: u32,
}

/// What a monitor is and where it sits.
///
/// The name is left out: it is the one field that is text, and nothing else in the bridge hands
/// a string back. A monitor is identified by its index here.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsMonitor {
    /// Width in physical pixels.
    pub width: u32,
    /// Height in physical pixels.
    pub height: u32,
    /// Where its top-left corner sits in the desktop's coordinate space.
    pub x: i32,
    /// The same, vertically.
    pub y: i32,
    /// Refresh rate in millihertz, or `0` when the platform does not report one.
    pub refresh_millihertz: u32,
    /// Physical pixels per logical pixel.
    pub scale_factor: f32,
}

/// One thing the window reported.
///
/// A tagged triple rather than six structs, because they cross the boundary as one array and the
/// payloads are at most two numbers each.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsWindowEvent {
    /// `0` resized, `1` focus changed, `2` close requested, `3` scale changed, `4` cursor
    /// entered, `5` cursor left.
    pub kind: i32,
    /// Width for a resize, `1` or `0` for focus, the scale factor for a scale change.
    pub a: f32,
    /// Height for a resize, and nothing for the rest.
    pub b: f32,
}

/// How a sound should be played.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsAudioConfig {
    /// `0` play once, `1` loop, `2` play once and despawn the entity afterwards.
    pub mode: i32,
    /// Loudness, where `1` is the sound as recorded and `0` is silence.
    pub volume: f32,
    /// Playback rate, which changes pitch with it. `1` is as recorded.
    pub speed: f32,
    /// Non-zero to start paused.
    pub paused: i32,
}

/// One debug shape to draw this frame.
///
/// Which fields matter depends on `kind`, because the three shapes take different arguments and
/// one struct crossing the boundary is simpler than three exports.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGizmoConfig {
    /// `0` line, `1` sphere, `2` axes.
    pub kind: i32,
    /// Line start, sphere centre, or where the axes are drawn.
    pub start: [f32; 3],
    /// Line end, and nothing for the other two.
    pub end: [f32; 3],
    /// Orientation of a sphere or a set of axes, as a quaternion.
    pub rotation: [f32; 4],
    /// Sphere radius, or the length of each axis.
    pub radius: f32,
    /// Linear RGBA. Axes colour themselves red, green and blue.
    pub color: [f32; 4],
}

/// How a sprite is drawn.
///
/// A sprite is a picture in the world rather than on the screen: it has a `Transform` like any
/// other entity, and a 2D camera decides what a world unit is worth in pixels.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsSpriteConfig {
    /// Asset key of the image to draw.
    pub image: i32,
    /// Tint, multiplied with the image. Linear RGBA, white for the image unchanged.
    pub color: [f32; 4],
    /// Non-zero to draw at `size` rather than at the image's own dimensions.
    pub has_size: i32,
    /// Width and height in world units, used when `has_size` is set.
    pub size: [f32; 2],
    /// Non-zero to draw only the part of the image `rect` names.
    pub has_rect: i32,
    /// Left, top, right and bottom of that part, in pixels.
    pub rect: [f32; 4],
    /// Non-zero to mirror horizontally.
    pub flip_x: i32,
    /// Non-zero to mirror vertically.
    pub flip_y: i32,
    /// Asset key of the atlas layout naming the frames, or a negative for a whole image.
    pub atlas: i32,
    /// Which frame of that layout to draw.
    pub atlas_index: u32,
    /// Non-zero to move the sprite's origin to `anchor` rather than leaving it centred.
    pub has_anchor: i32,
    /// Where the transform sits on the sprite, from `-0.5` to `0.5` on each axis.
    pub anchor: [f32; 2],
    /// How the picture meets `size`: `0` its own, `1` sliced, `2` tiled.
    pub mode: i32,
    /// Left, top, right and bottom insets of the nine-slice border, in pixels.
    pub slice_border: [f32; 4],
    /// How far a sliced corner may be scaled up. `0` takes Bevy's default of one.
    pub corner_scale: f32,
    /// Non-zero to repeat horizontally when tiled.
    pub tile_x: i32,
    /// Non-zero to repeat vertically when tiled.
    pub tile_y: i32,
    /// How far the picture stretches before a tile repeats. `0` takes Bevy's default of one.
    pub tile_stretch: f32,
}

/// The picture a UI node draws inside itself.
///
/// Separate from the node's own config because an image is attached to a node that already
/// exists, the way a sprite is attached to an entity: the layout is one decision and what fills
/// it is another.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsUiImageConfig {
    /// Asset key of the image to draw.
    pub image: i32,
    /// Tint, multiplied with the image. Linear RGBA, white for the image unchanged.
    pub color: [f32; 4],
    /// Non-zero to draw only the part of the image `rect` names.
    pub has_rect: i32,
    /// Left, top, right and bottom of that part, in pixels.
    pub rect: [f32; 4],
    /// Non-zero to mirror horizontally.
    pub flip_x: i32,
    /// Non-zero to mirror vertically.
    pub flip_y: i32,
    /// How the picture meets the node's size: `0` its own, `1` stretched, `2` sliced, `3` tiled.
    pub mode: i32,
    /// Left, top, right and bottom insets of the nine-slice border, in pixels.
    pub slice_border: [f32; 4],
    /// How far a sliced corner may be scaled up. `0` takes Bevy's default of one.
    pub corner_scale: f32,
    /// Non-zero to repeat horizontally when tiled.
    pub tile_x: i32,
    /// Non-zero to repeat vertically when tiled.
    pub tile_y: i32,
    /// How far the picture stretches before a tile repeats. `0` takes Bevy's default of one.
    pub tile_stretch: f32,
}

/// Where a UI node sits and how large it is.
///
/// Each length is a value and a unit, because Bevy's `Val` is an enum and a bare float cannot say
/// whether it means pixels, a percentage, or "work it out". `0` is auto, `1` pixels, `2` percent.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsUiNodeConfig {
    /// Non-zero to place the node against its parent's edges rather than in its flow.
    pub absolute: i32,
    /// Non-zero to give the node an `Interaction`, so the pointer is reported over it.
    pub interactive: i32,
    /// Distance from the parent's left edge.
    pub left: f32,
    /// Unit of `left`.
    pub left_unit: i32,
    /// Distance from the parent's top edge.
    pub top: f32,
    /// Unit of `top`.
    pub top_unit: i32,
    /// Distance from the parent's right edge.
    pub right: f32,
    /// Unit of `right`.
    pub right_unit: i32,
    /// Distance from the parent's bottom edge.
    pub bottom: f32,
    /// Unit of `bottom`.
    pub bottom_unit: i32,
    /// How wide the node is.
    pub width: f32,
    /// Unit of `width`.
    pub width_unit: i32,
    /// How tall the node is.
    pub height: f32,
    /// Unit of `height`.
    pub height_unit: i32,
    /// Space between the node's edge and its contents: left, top, right, bottom.
    pub padding: [f32; 4],
    /// Units of `padding`, in the same order.
    pub padding_units: [i32; 4],
    /// Space outside the node's edge: left, top, right, bottom.
    pub margin: [f32; 4],
    /// Units of `margin`, in the same order.
    pub margin_units: [i32; 4],
    /// Thickness of the node's border: left, top, right, bottom.
    pub border: [f32; 4],
    /// Units of `border`, in the same order.
    pub border_units: [i32; 4],
    /// How the node lays its children out at all: `0` flex, `1` block, `2` not at all.
    pub display: i32,
    /// Which way the node's children are stacked: `0` row, `1` column, `2` and `3` reversed.
    pub direction: i32,
    /// Whether children run onto more lines: `0` one line, `1` wrap, `2` wrap backwards.
    pub wrap: i32,
    /// How this node sits across its parent's axis, overriding the parent's own alignment.
    pub align_self: i32,
    /// Share of the parent's leftover space this node takes.
    pub grow: f32,
    /// Share of the parent's overflow this node gives up.
    pub shrink: f32,
    /// Size along the parent's axis before growing or shrinking.
    pub basis: f32,
    /// Unit of `basis`.
    pub basis_unit: i32,
    /// Smallest the node may be.
    pub min_width: f32,
    /// Unit of `min_width`.
    pub min_width_unit: i32,
    /// Smallest the node may be.
    pub min_height: f32,
    /// Unit of `min_height`.
    pub min_height_unit: i32,
    /// Largest the node may be.
    pub max_width: f32,
    /// Unit of `max_width`.
    pub max_width_unit: i32,
    /// Largest the node may be.
    pub max_height: f32,
    /// Unit of `max_height`.
    pub max_height_unit: i32,
    /// What happens to contents past the left and right edges: `0` shown, `1` clipped, `2` hidden,
    /// `3` scrolled.
    pub overflow_x: i32,
    /// The same for the top and bottom edges.
    pub overflow_y: i32,
    /// How the children are spread along that axis, in the order `JustifyContent` declares.
    pub justify: i32,
    /// How the children sit across it, in the order `AlignItems` declares.
    pub align: i32,
    /// Space between rows of children.
    pub row_gap: f32,
    /// Unit of `row_gap`.
    pub row_gap_unit: i32,
    /// Space between columns of children.
    pub column_gap: f32,
    /// Unit of `column_gap`.
    pub column_gap_unit: i32,
    /// Background colour for a node, or the text colour for a run of text. Linear RGBA.
    pub color: [f32; 4],
    /// Colour of the border, on every side. Linear RGBA, and transparent draws nothing.
    pub border_color: [f32; 4],
}

/// How an image should be sampled, and how its bytes should be read.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsImageConfig {
    /// `0` clamp to edge, `1` repeat, `2` mirror. Applied to U.
    pub address_u: i32,
    /// The same, for V.
    pub address_v: i32,
    /// `0` nearest, `1` linear. Used when the texture is drawn larger than it is.
    pub mag_filter: i32,
    /// The same, for when it is drawn smaller.
    pub min_filter: i32,
    /// The same, for blending between mip levels.
    pub mipmap_filter: i32,
    /// Maximum anisotropic samples. `1` disables it.
    pub anisotropy: u32,
    /// Non-zero to read the file as sRGB, which is right for colour and wrong for data.
    pub srgb: i32,
}

/// Everything a physically based material is made of.
///
/// Texture fields are asset keys, or `-1` for none. An image bound here is used as-is; combining
/// one with the matching factor is what the renderer already does, so a white base colour with a
/// base colour map shows the map unchanged.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsMaterialConfig {
    /// Base colour, as linear sRGB with alpha.
    pub base_color: [f32; 4],
    /// How metallic the surface is, from dielectric at zero to metal at one.
    pub metallic: f32,
    /// How rough, from a mirror near zero to fully diffuse at one.
    pub roughness: f32,
    /// Light the surface gives off, which is not affected by any lamp.
    pub emissive: [f32; 4],
    /// `0` opaque, `1` cut out at `alpha_cutoff`, `2` blended, `3` added to what is behind.
    pub alpha_mode: i32,
    /// Where a cut-out material stops drawing, used when `alpha_mode` is `1`.
    pub alpha_cutoff: f32,
    /// Non-zero to draw back faces as well as front ones.
    pub double_sided: i32,
    /// Non-zero to show the base colour flat, with no lighting at all.
    pub unlit: i32,
    /// Asset key of the base colour map, or `-1`.
    pub base_color_texture: i32,
    /// Asset key of the tangent-space normal map, or `-1`.
    pub normal_map: i32,
    /// Asset key of the combined metallic and roughness map, or `-1`.
    pub metallic_roughness_texture: i32,
    /// Asset key of the emissive map, or `-1`.
    pub emissive_texture: i32,
    /// Asset key of the ambient occlusion map, or `-1`.
    pub occlusion_texture: i32,
    /// How many times the texture repeats across the surface, in U and V.
    pub uv_scale: [f32; 2],
    /// Radians the texture is turned by.
    pub uv_rotation: f32,
    /// How far the texture is shifted, in UV units.
    pub uv_offset: [f32; 2],
}

/// What kind of light to spawn and how it behaves.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsLightConfig {
    /// `0` directional, `1` point, `2` spot.
    pub kind: i32,
    /// Illuminance in lux for a directional light, luminous power in lumens for the other two.
    pub intensity: f32,
    /// Linear RGB.
    pub color: [f32; 3],
    /// How far the light reaches. Point and spot only.
    pub range: f32,
    /// Radius of the emitting sphere, which softens the shadow edge. Point and spot only.
    pub radius: f32,
    /// Non-zero to cast shadows.
    pub shadows: i32,
    /// Radians from the axis within which a spot light is at full brightness.
    pub inner_angle: f32,
    /// Radians from the axis at which a spot light has fallen to nothing.
    pub outer_angle: f32,
    /// How far along its own normal a surface is pushed before it is tested against the shadow
    /// map. Trades shadow acne for a shadow that starts slightly late.
    pub shadow_depth_bias: f32,
    /// The same, along the surface normal, which handles a surface lit at a glancing angle.
    pub shadow_normal_bias: f32,
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
/// written are never NUL-terminated: the length is the answer.
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
pub fn guard<F: FnOnce() -> i32>(f: F) -> i32 {
    match std::panic::catch_unwind(AssertUnwindSafe(f)) {
        Ok(v) => v,
        Err(_) => status::PANIC,
    }
}

/// Runs `f`, converting any panic into `fallback`.
pub fn guard_with<T, F: FnOnce() -> T>(fallback: T, f: F) -> T {
    match std::panic::catch_unwind(AssertUnwindSafe(f)) {
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

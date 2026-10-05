//! The structs the window, the interface and sound pass across: monitors and their video modes,
//! window and interface events, audio, and the interface's text, images, nodes and grids.

/// What a monitor is and where it sits.
///
/// The name is left out, because it is the one field that is text and nothing else in the bridge
/// hands a string back. A monitor is identified by its index here.
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

/// One video mode a monitor can be driven at.
///
/// A resolution, a color depth and a refresh rate together, which exclusive fullscreen takes the
/// screen over with. A monitor offers a fixed list of these and can be driven at no others, so a
/// settings screen offers what the list holds rather than a pair of number boxes.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsVideoMode {
    /// Width in physical pixels.
    pub width: u32,
    /// Height in physical pixels.
    pub height: u32,
    /// Bits per pixel.
    pub bit_depth: u32,
    /// Refresh rate in millihertz.
    pub refresh_millihertz: u32,
}

/// One thing a UI widget reported.
///
/// A tagged pair rather than an event per kind, because they cross the boundary as one array and
/// none of them carries a payload beyond which element it happened to.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsUiEvent {
    /// `0` click, `1` value changed, `2` submit, `3` focus.
    pub kind: i32,
    /// The element it happened to.
    pub entity: u64,
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
    /// Non-zero to place the sound at its entity's transform and attenuate it with distance.
    pub spatial: i32,
    /// Scale applied to the distance between the sound and the listener. `0` takes Bevy's own.
    pub spatial_scale: f32,
    /// Where in the clip to start, in seconds. `0` starts at the beginning.
    pub start_seconds: f32,
    /// How much of the clip to play from there, in seconds. `0` plays to the end.
    pub play_seconds: f32,
}

/// How a run of UI text is set: its size, and what happens to it at the edges of its node.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsUiTextConfig {
    /// Asset key of the font to set the text in, or a negative for the one built into Bevy.
    pub font: i32,
    /// Height of the glyphs in logical pixels.
    pub font_size: f32,
    /// How the lines sit against each other: `0` left, `1` centered, `2` right, `3` justified,
    /// `4` and `5` follow the writing direction.
    pub justify: i32,
    /// Where a line may be broken: `0` at word boundaries, `1` at any character, `2` at words
    /// falling back to characters, `3` never.
    pub linebreak: i32,
    /// How far apart the lines sit. `0` keeps the font's own spacing.
    pub line_height: f32,
    /// What `line_height` is measured in: `0` multiples of the font size, `1` logical pixels.
    pub line_height_unit: i32,
    /// How much room is added between the letters. `0` leaves the font's own fit alone, and a
    /// negative value pulls them together.
    pub letter_spacing: f32,
    /// What `letter_spacing` is measured in: `0` multiples of the font size, `1` logical pixels.
    pub letter_spacing_unit: i32,
    /// Whether the glyphs are smoothed: `0` antialiased, `1` not, for a pixel font.
    pub font_smoothing: i32,
    /// How far a shadow is cast behind the text, in logical pixels: across, then down.
    pub shadow_offset: [f32; 2],
    /// The shadow's color, linear RGBA. An alpha of zero is no shadow.
    pub shadow_color: [f32; 4],
}

/// The picture a UI node draws inside itself.
///
/// Separate from the node's own config because an image is attached to a node that already
/// exists, the way a sprite is attached to an entity, where the layout is one decision and what
/// fills it is another.
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
    /// Which parts of a sliced picture tile rather than stretch, as a mask: `1` the sides, `2` the
    /// middle, `3` both, `0` neither. Read only when the picture is sliced, and the repeat is
    /// measured by `tile_stretch` the way a tiled picture's is.
    pub slice_tiling: i32,
    /// Asset key of a layout that cuts the image into frames, or a negative for none.
    pub atlas: i32,
    /// Which frame of that layout to draw, counted from zero.
    pub atlas_index: u32,
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
    /// Background color for a node, or the text color for a run of text. Linear RGBA.
    pub color: [f32; 4],
    /// Color of the border, on every side. Linear RGBA, and transparent draws nothing.
    pub border_color: [f32; 4],
    /// How the lines of a wrapped node are spread across it, the way `justify` spreads the
    /// children within one line. Same numbering as `justify`.
    pub align_content: i32,
    /// The other axis as a multiple of the one that is known, or `0` to size both independently.
    pub aspect_ratio: f32,
    /// Which box a node that clips its overflow clips at: `0` content, `1` padding, `2` border.
    pub clip_box: i32,
    /// How far outside that box the clipping is pushed, in logical pixels.
    pub clip_margin: f32,
    /// How far each corner is rounded: top left, top right, bottom right, bottom left.
    pub corners: [f32; 4],
    /// Units of `corners`, in the same order.
    pub corner_units: [i32; 4],
    /// What `width` and the rest measure: `0` the border box, `1` the content box.
    pub box_sizing: i32,
    /// Which camera draws this node, as entity bits, or `0` for whichever one draws to the window.
    pub camera: u64,
}

/// One track of a grid, and how many times it repeats.
///
/// A row or a column, described the way a stylesheet describes one. A grid is a list of them, and a
/// flat config has no room for a list, which is why a grid arrives through its own call with a
/// pointer to an array rather than as more fields on the node.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGridTrack {
    /// How the track is sized: `0` to what it holds, `1` logical pixels, `2` a percentage of the
    /// grid, `3` a share of whatever is left over, `4` the smallest its contents can be, `5` the
    /// largest they can be, `6` a share as `3` is that may also be narrower than its contents.
    pub kind: i32,
    /// The number `kind` reads, where it reads one.
    pub value: f32,
    /// How many times this track repeats: a count, `-1` to fill the grid, `-2` to fill it and drop
    /// the tracks nothing landed in. Filling is only offered for a track sized in pixels or in
    /// percent, because the rest have no size to divide the room by, and a fill asked for on one of
    /// those is read as once.
    pub repeat: i32,
}

/// The tracks a grid is laid out on.
///
/// Every list is a pointer and a count, and a null list leaves that part of the grid as it was.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsUiGridConfig {
    /// Which way an item with no place of its own is put next: `0` along the row, `1` down the
    /// column, `2` and `3` the same while backfilling any gap it fits in.
    pub auto_flow: i32,
    /// The rows stated up front.
    pub rows: *const BcsGridTrack,
    /// How many of them.
    pub row_count: i32,
    /// The columns stated up front.
    pub columns: *const BcsGridTrack,
    /// How many of them.
    pub column_count: i32,
    /// The rows made for items placed past the ones stated, used in turn.
    pub auto_rows: *const BcsGridTrack,
    /// How many of them.
    pub auto_row_count: i32,
    /// The columns made the same way.
    pub auto_columns: *const BcsGridTrack,
    /// How many of them.
    pub auto_column_count: i32,
    /// How an item sits across its cell, in the order `JustifyItems` declares.
    pub justify_items: i32,
}

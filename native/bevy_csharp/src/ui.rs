//! Bevy's UI, reachable from C#.
//!
//! A UI node is a `Node` component carrying a whole layout description: two dozen fields, several
//! of them enums with payloads. Mirroring that byte for byte is not possible the way `Transform`
//! is, so the bridge takes a description and builds the components on this side, the way
//! [`crate::render`] does for meshes and materials.
//!
//! Everything here needs a render build. A windowless one reports that rather than spawning
//! entities that would never draw.

use crate::interop::{status, BcsUiImageConfig, BcsUiNodeConfig, BcsUiTextConfig};
#[cfg(feature = "render")]
use crate::state::with_world;
#[cfg(feature = "render")]
use crate::state::with_world_opt;

/// Translates one length from the managed side, where `0` is auto, `1` pixels and `2` percent.
#[cfg(feature = "render")]
fn length(value: f32, unit: i32) -> bevy::ui::Val {
    match unit {
        1 => bevy::ui::Val::Px(value),
        2 => bevy::ui::Val::Percent(value),
        _ => bevy::ui::Val::Auto,
    }
}

/// Translates one of the four-sided measurements, in the order left, top, right, bottom.
#[cfg(feature = "render")]
fn rect(values: &[f32; 4], units: &[i32; 4]) -> bevy::ui::UiRect {
    bevy::ui::UiRect {
        left: length(values[0], units[0]),
        top: length(values[1], units[1]),
        right: length(values[2], units[2]),
        bottom: length(values[3], units[3]),
    }
}

/// Translates whether the node lays out at all, and by which model.
#[cfg(feature = "render")]
fn display(value: i32) -> bevy::ui::Display {
    use bevy::ui::Display;

    match value {
        1 => Display::Block,
        2 => Display::None,
        3 => Display::Grid,
        _ => Display::Flex,
    }
}

/// Translates whether children run onto more lines.
#[cfg(feature = "render")]
fn flex_wrap(value: i32) -> bevy::ui::FlexWrap {
    use bevy::ui::FlexWrap;

    match value {
        1 => FlexWrap::Wrap,
        2 => FlexWrap::WrapReverse,
        _ => FlexWrap::NoWrap,
    }
}

/// Translates one node's own answer to its parent's alignment.
#[cfg(feature = "render")]
fn align_self(value: i32) -> bevy::ui::AlignSelf {
    use bevy::ui::AlignSelf;

    match value {
        1 => AlignSelf::Start,
        2 => AlignSelf::End,
        3 => AlignSelf::FlexStart,
        4 => AlignSelf::FlexEnd,
        5 => AlignSelf::Center,
        6 => AlignSelf::Baseline,
        7 => AlignSelf::Stretch,
        _ => AlignSelf::Auto,
    }
}

/// Translates what happens to contents past one edge of the node.
#[cfg(feature = "render")]
fn overflow_axis(value: i32) -> bevy::ui::OverflowAxis {
    use bevy::ui::OverflowAxis;

    match value {
        1 => OverflowAxis::Clip,
        2 => OverflowAxis::Hidden,
        3 => OverflowAxis::Scroll,
        _ => OverflowAxis::Visible,
    }
}

/// Translates the axis children are stacked along.
#[cfg(feature = "render")]
fn flex_direction(value: i32) -> bevy::ui::FlexDirection {
    use bevy::ui::FlexDirection;

    match value {
        1 => FlexDirection::Column,
        2 => FlexDirection::RowReverse,
        3 => FlexDirection::ColumnReverse,
        _ => FlexDirection::Row,
    }
}

/// Translates how children are spread along the main axis.
#[cfg(feature = "render")]
fn justify_content(value: i32) -> bevy::ui::JustifyContent {
    use bevy::ui::JustifyContent;

    match value {
        1 => JustifyContent::Start,
        2 => JustifyContent::End,
        3 => JustifyContent::FlexStart,
        4 => JustifyContent::FlexEnd,
        5 => JustifyContent::Center,
        6 => JustifyContent::Stretch,
        7 => JustifyContent::SpaceBetween,
        8 => JustifyContent::SpaceEvenly,
        9 => JustifyContent::SpaceAround,
        _ => JustifyContent::Default,
    }
}

/// Translates how children sit across the main axis.
#[cfg(feature = "render")]
fn align_items(value: i32) -> bevy::ui::AlignItems {
    use bevy::ui::AlignItems;

    match value {
        1 => AlignItems::Start,
        2 => AlignItems::End,
        3 => AlignItems::FlexStart,
        4 => AlignItems::FlexEnd,
        5 => AlignItems::Center,
        6 => AlignItems::Baseline,
        7 => AlignItems::Stretch,
        _ => AlignItems::Default,
    }
}

/// Builds the `Node` a config describes.
///
/// An unknown code for one of the three enums takes Bevy's default rather than being refused,
/// because the managed side is what names them, and a bridge older than the assembly calling it
/// should lay a screen out plainly rather than not at all.
#[cfg(feature = "render")]
fn node_from(config: &BcsUiNodeConfig) -> bevy::ui::Node {
    use bevy::ui::{BoxSizing, PositionType};

    bevy::ui::Node {
        // Bevy's own default is the border box, which is the one people expect, so a zeroed
        // config asks for what it would have got anyway.
        box_sizing: if config.box_sizing == 1 {
            BoxSizing::ContentBox
        } else {
            BoxSizing::BorderBox
        },
        // Rounded corners are a field on the node rather than a component beside it, so a zeroed
        // config asks for the square ones it would have had anyway.
        border_radius: bevy::ui::BorderRadius {
            top_left: length(config.corners[0], config.corner_units[0]),
            top_right: length(config.corners[1], config.corner_units[1]),
            bottom_right: length(config.corners[2], config.corner_units[2]),
            bottom_left: length(config.corners[3], config.corner_units[3]),
        },
        position_type: if config.absolute != 0 {
            PositionType::Absolute
        } else {
            PositionType::Relative
        },
        left: length(config.left, config.left_unit),
        top: length(config.top, config.top_unit),
        right: length(config.right, config.right_unit),
        bottom: length(config.bottom, config.bottom_unit),
        width: length(config.width, config.width_unit),
        height: length(config.height, config.height_unit),
        padding: rect(&config.padding, &config.padding_units),
        margin: rect(&config.margin, &config.margin_units),
        border: rect(&config.border, &config.border_units),
        display: display(config.display),
        flex_direction: flex_direction(config.direction),
        flex_wrap: flex_wrap(config.wrap),
        justify_content: justify_content(config.justify),
        align_items: align_items(config.align),
        align_self: align_self(config.align_self),
        flex_grow: config.grow,
        flex_shrink: config.shrink,
        flex_basis: length(config.basis, config.basis_unit),
        min_width: length(config.min_width, config.min_width_unit),
        min_height: length(config.min_height, config.min_height_unit),
        max_width: length(config.max_width, config.max_width_unit),
        max_height: length(config.max_height, config.max_height_unit),
        overflow: bevy::ui::Overflow {
            x: overflow_axis(config.overflow_x),
            y: overflow_axis(config.overflow_y),
        },
        row_gap: length(config.row_gap, config.row_gap_unit),
        column_gap: length(config.column_gap, config.column_gap_unit),
        align_content: align_content(config.align_content),

        // Zero leaves the two axes independent rather than asking for a node with no height,
        // which is what a ratio of zero would otherwise mean.
        aspect_ratio: (config.aspect_ratio > 0.0).then_some(config.aspect_ratio),
        overflow_clip_margin: bevy::ui::OverflowClipMargin {
            visual_box: visual_box(config.clip_box),
            margin: config.clip_margin,
        },
        ..Default::default()
    }
}

/// Translates how the lines of a wrapped node are spread across it.
///
/// The same vocabulary as `justify_content`, because it is the same question asked of the lines a
/// wrap produced rather than of the children within one line.
#[cfg(feature = "render")]
fn align_content(value: i32) -> bevy::ui::AlignContent {
    use bevy::ui::AlignContent;

    match value {
        1 => AlignContent::Start,
        2 => AlignContent::End,
        3 => AlignContent::FlexStart,
        4 => AlignContent::FlexEnd,
        5 => AlignContent::Center,
        6 => AlignContent::Stretch,
        7 => AlignContent::SpaceBetween,
        8 => AlignContent::SpaceEvenly,
        9 => AlignContent::SpaceAround,
        _ => AlignContent::Default,
    }
}

/// Translates which box a node that clips its overflow clips at.
#[cfg(feature = "render")]
fn visual_box(value: i32) -> bevy::ui::VisualBox {
    use bevy::ui::VisualBox;

    match value {
        0 => VisualBox::ContentBox,
        2 => VisualBox::BorderBox,
        _ => VisualBox::PaddingBox,
    }
}

/// Translates how much room is added between the letters of a run of text.
///
/// Only called for a value other than zero, because zero is both the default and no change, so a
/// caller leaving the field alone and one asking for the fit the font already has want the same
/// thing, which is no component at all.
#[cfg(feature = "render")]
fn letter_spacing(value: f32, unit: i32) -> bevy::text::LetterSpacing {
    use bevy::text::LetterSpacing;

    match unit {
        1 => LetterSpacing::Px(value),
        _ => LetterSpacing::Rem(value),
    }
}

/// Translates how far apart the lines of a run of text sit.
///
/// Only called for a height above zero, because zero is a caller leaving the field alone and the
/// font's own spacing is what Bevy uses when the component is absent.
#[cfg(feature = "render")]
fn line_height(value: f32, unit: i32) -> bevy::text::LineHeight {
    use bevy::text::LineHeight;

    match unit {
        1 => LineHeight::Px(value),
        _ => LineHeight::RelativeToFont(value),
    }
}

/// Translates how the lines of a run of text sit against each other.
#[cfg(feature = "render")]
fn justify(value: i32) -> bevy::text::Justify {
    use bevy::text::Justify;

    match value {
        1 => Justify::Center,
        2 => Justify::Right,
        3 => Justify::Justified,
        4 => Justify::Start,
        5 => Justify::End,
        _ => Justify::Left,
    }
}

/// Translates where a line of text may be broken.
#[cfg(feature = "render")]
fn linebreak(value: i32) -> bevy::text::LineBreak {
    use bevy::text::LineBreak;

    match value {
        1 => LineBreak::AnyCharacter,
        2 => LineBreak::WordOrCharacter,
        3 => LineBreak::NoWrap,
        _ => LineBreak::WordBoundary,
    }
}

/// Builds the border color a config describes.
///
/// Always inserted, because transparent is what a node with no border draws and the component is
/// four floats either way. Leaving it off would make a border that is set later invisible.
#[cfg(feature = "render")]
fn border_color_from(config: &BcsUiNodeConfig) -> bevy::ui::BorderColor {
    bevy::ui::BorderColor::all(bevy::color::Color::linear_rgba(
        config.border_color[0],
        config.border_color[1],
        config.border_color[2],
        config.border_color[3],
    ))
}

/// Gives a node the components the pointer is tracked with, when its config asks for them.
///
/// `Button` rather than `Interaction` alone, because it is the marker that requires both, and it
/// brings `FocusPolicy::Block` with it, so an interactive node captures the pointer instead of
/// letting it reach whatever sits behind it. A node left plain carries neither, which keeps the
/// focus system's work proportional to the number of things that react rather than to the whole
/// screen.
#[cfg(feature = "render")]
fn make_interactive(entity: &mut bevy::ecs::world::EntityWorldMut, config: &BcsUiNodeConfig) {
    if config.interactive != 0 {
        entity.insert(bevy::ui::widget::Button);
    }
}

/// Points a node at the camera that should draw it, when it was given one.
///
/// Without this, Bevy picks whichever camera draws to a window, so a run that draws into an image
/// has no camera the interface can find and lays out nothing at all. Naming one is what lets a
/// screen be drawn on a machine with no display, and what lets a test look at the result.
///
/// It propagates to children, so the root of a screen is the only node that has to be told.
#[cfg(feature = "render")]
fn target_camera(entity: &mut bevy::ecs::world::EntityWorldMut, config: &BcsUiNodeConfig) {
    if config.camera == 0 {
        return;
    }

    entity.insert(bevy::ui::UiTargetCamera(
        bevy::ecs::entity::Entity::from_bits(config.camera),
    ));
}

/// Spawns a rectangle, and returns its entity or `0`.
///
/// The building block everything else sits in or on: a panel, a bar, a backdrop. Parent one to
/// another with `bcs_ecs_set_parent` to lay them out.
///
/// # Safety
/// `config` must point to a readable [`BcsUiNodeConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_spawn_node(config: *const BcsUiNodeConfig) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::ui::BackgroundColor;

            if config.is_null() {
                return 0;
            }
            let config = unsafe { *config };

            with_world_opt(|world| {
                let mut entity = world.spawn((
                    node_from(&config),
                    BackgroundColor(Color::linear_rgba(
                        config.color[0],
                        config.color[1],
                        config.color[2],
                        config.color[3],
                    )),
                    border_color_from(&config),
                ));
                make_interactive(&mut entity, &config);
                target_camera(&mut entity, &config);
                entity.id().to_bits()
            })
            .unwrap_or(0)
        }
    })
}

/// Spawns a run of text, and returns its entity or `0`.
///
/// The font is Bevy's own, compiled into the library, so no asset has to be loaded to put words
/// on the screen. The text config carries the size in logical pixels, and how the run is broken
/// and aligned when it does not fit on one line.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string; `config` must point to a readable
/// [`BcsUiNodeConfig`] and `text_config` to a readable [`BcsUiTextConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_spawn_text(
    text: *const core::ffi::c_char,
    config: *const BcsUiNodeConfig,
    text_config: *const BcsUiTextConfig,
) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = (text, config, text_config);
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::text::{Font, FontSize, FontSource, TextColor, TextFont, TextLayout};
            use bevy::ui::widget::Text;

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return 0;
            };
            if config.is_null() || text_config.is_null() {
                return 0;
            }
            let config = unsafe { *config };
            let text_config = unsafe { *text_config };

            with_world_opt(|world| {
                // A negative key is the font compiled into Bevy, which is what keeps text
                // working with no asset at all. A key that names nothing is a mistake rather
                // than a reason to fall back quietly, so it refuses.
                //
                // `FontSource` can also name a generic family, which is not offered here, because
                // that path needs Bevy's `system_font_discovery`, and on Linux the crate behind it
                // links against fontconfig at build time. Text renders nothing at all without the
                // feature, so the choice is a font of your own or Bevy's.
                let font = if text_config.font >= 0 {
                    match crate::assets::clone_handle(world, text_config.font) {
                        Some(handle) => FontSource::Handle(handle.typed::<Font>()),
                        None => return 0,
                    }
                } else {
                    FontSource::default()
                };

                let mut entity = world.spawn((
                    Text(text.clone()),
                    TextFont {
                        font,
                        font_size: FontSize::Px(text_config.font_size),
                        font_smoothing: if text_config.font_smoothing == 1 {
                            bevy::text::FontSmoothing::None
                        } else {
                            bevy::text::FontSmoothing::AntiAliased
                        },
                        ..Default::default()
                    },
                    // What keeps a long string inside its node. The width it breaks against is
                    // the layout's, so a node free to grow never wraps however this is set.
                    TextLayout::new(
                        justify(text_config.justify),
                        linebreak(text_config.linebreak),
                    ),
                    // The node's color is the text's here, because a run of text has no background
                    // of its own, and giving it one would need a second entity behind it.
                    TextColor(Color::linear_rgba(
                        config.color[0],
                        config.color[1],
                        config.color[2],
                        config.color[3],
                    )),
                    node_from(&config),
                    border_color_from(&config),
                ));
                make_interactive(&mut entity, &config);
                target_camera(&mut entity, &config);

                // The spacing between lines is a component of its own rather than part of the
                // font, so it is inserted beside it.
                if text_config.line_height > 0.0 {
                    entity.insert(line_height(
                        text_config.line_height,
                        text_config.line_height_unit,
                    ));
                }

                // The room between letters is another component beside the font. Zero is both the
                // default and no change, so leaving it alone and asking for the fit the font
                // already has are the same request and neither inserts anything.
                if text_config.letter_spacing != 0.0 {
                    entity.insert(letter_spacing(
                        text_config.letter_spacing,
                        text_config.letter_spacing_unit,
                    ));
                }

                // A shadow is a component beside the text rather than part of its font, and an
                // invisible one is a caller leaving it alone rather than asking for a shadow that
                // cannot be seen.
                if text_config.shadow_color[3] > 0.0 {
                    entity.insert(bevy::ui::widget::TextShadow {
                        offset: bevy::math::Vec2::new(
                            text_config.shadow_offset[0],
                            text_config.shadow_offset[1],
                        ),
                        color: Color::linear_rgba(
                            text_config.shadow_color[0],
                            text_config.shadow_color[1],
                            text_config.shadow_color[2],
                            text_config.shadow_color[3],
                        ),
                    });
                }

                entity.id().to_bits()
            })
            .unwrap_or(0)
        }
    })
}

/// Adds a run of text to an existing one, set in its own font and color.
///
/// A paragraph with a bold word in it is one `Text` entity with a child per run rather than markup
/// inside a string, because each run carries its own font, size and color as components and the
/// parent's layout breaks and aligns the lot as one block. Spans read in the order they were added.
///
/// `color` points at four linear RGBA floats, kept out of [`BcsUiTextConfig`] because a span has no
/// node of its own to take a color from, where a whole text takes the node's.
///
/// Returns `0` where the parent is gone, the font names nothing, or there is no renderer.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string, `text_config` must point at one
/// [`BcsUiTextConfig`], and `color` at four floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_spawn_text_span(
    parent: u64,
    text: *const core::ffi::c_char,
    text_config: *const BcsUiTextConfig,
    color: *const f32,
) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = (parent, text, text_config, color);
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::text::{Font, FontSize, FontSource, TextColor, TextFont, TextSpan};

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return 0;
            };
            if text_config.is_null() || color.is_null() {
                return 0;
            }
            let text_config = unsafe { *text_config };
            let color = unsafe { core::slice::from_raw_parts(color, 4) };

            with_world_opt(|world| {
                let parent = bevy::ecs::entity::Entity::from_bits(parent);
                if world.get_entity(parent).is_err() {
                    return None;
                }

                let font = if text_config.font >= 0 {
                    match crate::assets::clone_handle(world, text_config.font) {
                        Some(handle) => FontSource::Handle(handle.typed::<Font>()),
                        None => return None,
                    }
                } else {
                    FontSource::default()
                };

                let mut entity = world.spawn((
                    TextSpan(text),
                    TextFont {
                        font,
                        font_size: FontSize::Px(text_config.font_size),
                        font_smoothing: if text_config.font_smoothing == 1 {
                            bevy::text::FontSmoothing::None
                        } else {
                            bevy::text::FontSmoothing::AntiAliased
                        },
                        ..Default::default()
                    },
                    TextColor(Color::linear_rgba(color[0], color[1], color[2], color[3])),
                ));

                // The spacing components are the span's own, the way the font is, so a run set
                // wider than the sentence around it is one entity's business.
                if text_config.line_height > 0.0 {
                    entity.insert(line_height(
                        text_config.line_height,
                        text_config.line_height_unit,
                    ));
                }

                if text_config.letter_spacing != 0.0 {
                    entity.insert(letter_spacing(
                        text_config.letter_spacing,
                        text_config.letter_spacing_unit,
                    ));
                }

                let span = entity.id();

                // Last, because a span is only text once it is under something carrying `Text`,
                // and until then it is an entity holding a string.
                world.entity_mut(parent).add_child(span);

                Some(span.to_bits())
            })
            .flatten()
            .unwrap_or(0)
        }
    })
}

/// Replaces what a text entity says.
///
/// Written in place rather than by respawning, because a score or a timer changes every frame and
/// the entity behind it should not.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_text(entity: u64, text: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, text);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ui::widget::Text;

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };
                let Some(mut value) = entity_mut.get_mut::<Text>() else {
                    return status::NOT_PRESENT;
                };

                value.0 = text.clone();
                status::OK
            })
        }
    })
}

/// Reports how the pointer stands on a node: `0` none, `1` hovered, `2` pressed.
///
/// `Interaction` is a Rust enum, so the managed side holds it as a name-only handle and reads its
/// value here instead of mirroring the bytes. The three codes are the bridge's own, and stay put
/// whatever Bevy's discriminants do.
///
/// A node spawned without `interactive` carries no `Interaction` at all, which is reported as
/// `NOT_PRESENT` rather than as `0`: "nothing is touching it" and "it was never set up to notice"
/// are different answers, and a button that silently never fires is the harder one to find.
///
/// Pressed lasts from the frame the pointer goes down until it is released, so a click is the
/// edge into it rather than a state of its own.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_interaction(entity: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ui::Interaction;

            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(interaction) = entity_ref.get::<Interaction>() else {
                    return status::NOT_PRESENT;
                };

                match *interaction {
                    Interaction::None => 0,
                    Interaction::Hovered => 1,
                    Interaction::Pressed => 2,
                }
            })
        }
    })
}

/// Draws a picture inside a node, or replaces the one it draws.
///
/// The node keeps its layout, so the image fills what the layout gave it, which is what `mode` is
/// about. `Auto` takes the picture's own size, so a node with no width or height of its own ends
/// up the size of the image; the other three fit it to the node instead.
///
/// # Safety
/// `config` must point to a readable [`BcsUiImageConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_image(entity: u64, config: *const BcsUiImageConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::image::Image;
            use bevy::math::{Rect, Vec2};
            use bevy::sprite::{BorderRect, SliceScaleMode, TextureSlicer};
            use bevy::ui::widget::{ImageNode, NodeImageMode};

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(image) = crate::assets::clone_handle(world, config.image) else {
                    return status::NO_COMPONENT;
                };

                let slicer = TextureSlicer {
                    border: BorderRect {
                        min_inset: Vec2::new(config.slice_border[0], config.slice_border[1]),
                        max_inset: Vec2::new(config.slice_border[2], config.slice_border[3]),
                    },
                    center_scale_mode: SliceScaleMode::Stretch,
                    sides_scale_mode: SliceScaleMode::Stretch,
                    max_corner_scale: if config.corner_scale > 0.0 {
                        config.corner_scale
                    } else {
                        1.0
                    },
                };

                let image_mode = match config.mode {
                    1 => NodeImageMode::Stretch,
                    2 => NodeImageMode::Sliced(slicer),
                    3 => NodeImageMode::Tiled {
                        tile_x: config.tile_x != 0,
                        tile_y: config.tile_y != 0,
                        stretch_value: if config.tile_stretch > 0.0 {
                            config.tile_stretch
                        } else {
                            1.0
                        },
                    },
                    _ => NodeImageMode::Auto,
                };

                // A frame of a sheet rather than the whole picture, cut by the same layout asset
                // a sprite uses, so one sheet serves the world and the interface. A key that names
                // nothing is a mistake rather than a default, so it is refused.
                let atlas = if config.atlas < 0 {
                    None
                } else {
                    let Some(layout) = crate::assets::clone_handle(world, config.atlas) else {
                        return status::NO_COMPONENT;
                    };

                    Some(bevy::image::TextureAtlas {
                        layout: layout.typed::<bevy::image::TextureAtlasLayout>(),
                        index: config.atlas_index as usize,
                    })
                };

                let node_image = ImageNode {
                    image: image.typed::<Image>(),
                    color: Color::linear_rgba(
                        config.color[0],
                        config.color[1],
                        config.color[2],
                        config.color[3],
                    ),
                    flip_x: config.flip_x != 0,
                    flip_y: config.flip_y != 0,
                    rect: (config.has_rect != 0).then(|| {
                        Rect::new(config.rect[0], config.rect[1], config.rect[2], config.rect[3])
                    }),
                    image_mode,
                    texture_atlas: atlas,
                    ..Default::default()
                };

                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                // Bevy's own insert, so the components an image node requires arrive with it.
                entity_mut.insert(node_image);
                status::OK
            })
        }
    })
}

/// Moves a node's contents inside it, for a list that scrolls.
///
/// Only means anything on a node whose overflow is set to scroll, because that is what clips the
/// contents to the node, and this is how far they have been pushed, in logical pixels from the top
/// left. Bevy has no scrolling input of its own, so a wheel or a drag is read like any other input
/// and turned into a call here.
///
/// A node is required, and anything else is refused. `ScrollPosition` is a bare component, unlike
/// `ImageNode`, which brings a `Node` with it. Nothing would make the entity a node, so the
/// component would sit there being read by no layout while the call reported success. The overflow
/// is not checked, because the two are set independently and either order has to work.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_set_scroll(entity: u64, x: f32, y: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::math::Vec2;
            use bevy::ui::{Node, ScrollPosition};

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                if !entity_mut.contains::<Node>() {
                    return status::NOT_PRESENT;
                }

                entity_mut.insert(ScrollPosition(Vec2::new(x, y)));
                status::OK
            })
        }
    })
}

/// Turns one described track into Bevy's, repeated as many times as it asked for.
#[cfg(feature = "render")]
fn grid_track(track: &crate::interop::BcsGridTrack) -> bevy::ui::RepeatedGridTrack {
    use bevy::ui::{GridTrackRepetition, RepeatedGridTrack};

    // A count of at least one, because a track repeated no times is a track that is not there and
    // a caller asking for that would leave it out of the list.
    let count = track.repeat.clamp(1, u16::MAX as i32) as u16;

    // Filling the grid is only offered where a track has a size to divide the room by, which is
    // pixels and percent. Asked for on any other track it reads as once, because the alternative
    // is refusing a whole layout over one number.
    let repetition = match track.repeat {
        -1 => GridTrackRepetition::AutoFill,
        -2 => GridTrackRepetition::AutoFit,
        _ => GridTrackRepetition::Count(count),
    };

    match track.kind {
        1 => RepeatedGridTrack::px(repetition, track.value),
        2 => RepeatedGridTrack::percent(repetition, track.value),
        3 => RepeatedGridTrack::fr(count, track.value),
        4 => RepeatedGridTrack::min_content(count),
        5 => RepeatedGridTrack::max_content(count),
        _ => RepeatedGridTrack::auto(count),
    }
}

/// Turns one described track into Bevy's, ignoring how many times it said it repeats.
///
/// What a grid makes on demand for an item placed past the tracks it was given. Bevy takes these
/// as plain tracks rather than repeated ones, because the list is cycled through as often as it is
/// needed and a repeat inside it would say the same thing twice.
#[cfg(feature = "render")]
fn auto_track(track: &crate::interop::BcsGridTrack) -> bevy::ui::GridTrack {
    use bevy::ui::GridTrack;

    match track.kind {
        1 => GridTrack::px(track.value),
        2 => GridTrack::percent(track.value),
        3 => GridTrack::fr(track.value),
        4 => GridTrack::min_content(),
        5 => GridTrack::max_content(),
        _ => GridTrack::auto(),
    }
}

/// Reads one of the config's lists as tracks made on demand, or nothing where it named none.
///
/// # Safety
/// `tracks` must point at `count` tracks, or be null.
#[cfg(feature = "render")]
unsafe fn auto_tracks(
    tracks: *const crate::interop::BcsGridTrack,
    count: i32,
) -> Option<Vec<bevy::ui::GridTrack>> {
    if tracks.is_null() || count <= 0 {
        return None;
    }

    let described = unsafe { core::slice::from_raw_parts(tracks, count as usize) };

    Some(described.iter().map(auto_track).collect())
}

/// Reads one of the config's lists, or nothing where it named none.
///
/// # Safety
/// `tracks` must point at `count` tracks, or be null.
#[cfg(feature = "render")]
unsafe fn grid_tracks(
    tracks: *const crate::interop::BcsGridTrack,
    count: i32,
) -> Option<Vec<bevy::ui::RepeatedGridTrack>> {
    if tracks.is_null() || count <= 0 {
        return None;
    }

    let described = unsafe { core::slice::from_raw_parts(tracks, count as usize) };

    Some(described.iter().map(grid_track).collect())
}

/// How an item sits across the cell it was placed in.
#[cfg(feature = "render")]
fn justify_items(value: i32) -> bevy::ui::JustifyItems {
    use bevy::ui::JustifyItems;

    match value {
        1 => JustifyItems::Start,
        2 => JustifyItems::End,
        3 => JustifyItems::Center,
        4 => JustifyItems::Baseline,
        5 => JustifyItems::Stretch,
        _ => JustifyItems::Default,
    }
}

/// Lays a node's children out on a grid.
///
/// Applied to a node that already exists rather than passed with one, because the tracks are lists
/// and a list has no room in the flat config every other field arrives in. The node is also told to
/// lay out as a grid here, since a grid with no `Display::Grid` is a set of numbers nothing reads.
///
/// Returns [`status::NO_COMPONENT`] where the entity is gone or carries no node.
///
/// # Safety
/// `config` must point at one [`BcsUiGridConfig`], whose lists must each point at as many tracks as
/// their count says, or be null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_grid(
    entity: u64,
    config: *const crate::interop::BcsUiGridConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            let rows = unsafe { grid_tracks(config.rows, config.row_count) };
            let columns = unsafe { grid_tracks(config.columns, config.column_count) };
            let auto_rows = unsafe { auto_tracks(config.auto_rows, config.auto_row_count) };
            let auto_columns =
                unsafe { auto_tracks(config.auto_columns, config.auto_column_count) };

            crate::state::with_world(|world| {
                use bevy::ui::{Display, GridAutoFlow, Node};

                let entity = bevy::ecs::entity::Entity::from_bits(entity);
                let Some(mut node) = world.get_mut::<Node>(entity) else {
                    return status::NO_COMPONENT;
                };

                node.display = Display::Grid;
                node.grid_auto_flow = match config.auto_flow {
                    1 => GridAutoFlow::Column,
                    2 => GridAutoFlow::RowDense,
                    3 => GridAutoFlow::ColumnDense,
                    _ => GridAutoFlow::Row,
                };
                node.justify_items = justify_items(config.justify_items);

                // A list left out keeps what the node had, so a caller setting the columns alone
                // does not silently clear the rows it set a moment ago.
                if let Some(rows) = rows {
                    node.grid_template_rows = rows;
                }
                if let Some(columns) = columns {
                    node.grid_template_columns = columns;
                }
                if let Some(auto_rows) = auto_rows {
                    node.grid_auto_rows = auto_rows;
                }
                if let Some(auto_columns) = auto_columns {
                    node.grid_auto_columns = auto_columns;
                }

                status::OK
            })
        }
    })
}

/// Places one child on its parent's grid.
///
/// `row` and `column` are grid lines, counted from one, where a negative counts back from the far
/// edge and `0` leaves the item where the flow would have put it. `row_span` and `column_span` are
/// how many tracks it covers, and `0` is one track. `justify_self` overrides how this one item sits
/// across its cell, in the order `JustifySelf` declares.
///
/// Returns [`status::NO_COMPONENT`] where the entity is gone or carries no node.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_set_grid_placement(
    entity: u64,
    row: i32,
    row_span: i32,
    column: i32,
    column_span: i32,
    justify_self: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, row, row_span, column, column_span, justify_self);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                use bevy::ui::{GridPlacement, JustifySelf, Node};

                let entity = bevy::ecs::entity::Entity::from_bits(entity);
                let Some(mut node) = world.get_mut::<Node>(entity) else {
                    return status::NO_COMPONENT;
                };

                let place = |line: i32, span: i32| {
                    let mut placement = GridPlacement::default();

                    if line != 0 {
                        placement = placement.set_start(line.clamp(
                            i16::MIN as i32 + 1,
                            i16::MAX as i32,
                        ) as i16);
                    }

                    if span > 0 {
                        placement = placement.set_span(span.min(u16::MAX as i32) as u16);
                    }

                    placement
                };

                node.grid_row = place(row, row_span);
                node.grid_column = place(column, column_span);
                node.justify_self = match justify_self {
                    1 => JustifySelf::Start,
                    2 => JustifySelf::End,
                    3 => JustifySelf::Center,
                    4 => JustifySelf::Baseline,
                    5 => JustifySelf::Stretch,
                    _ => JustifySelf::Auto,
                };

                status::OK
            })
        }
    })
}

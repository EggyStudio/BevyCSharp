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
/// An unknown code for one of the three enums takes Bevy's default rather than being refused: the
/// managed side is what names them, and a bridge older than the assembly calling it should lay a
/// screen out plainly rather than not at all.
#[cfg(feature = "render")]
fn node_from(config: &BcsUiNodeConfig) -> bevy::ui::Node {
    use bevy::ui::PositionType;

    bevy::ui::Node {
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
        ..Default::default()
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

/// Builds the border colour a config describes.
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
/// `Button` rather than `Interaction` alone: it is the marker that requires both, and it brings
/// `FocusPolicy::Block` with it, so an interactive node captures the pointer instead of letting
/// it reach whatever sits behind it. A node left plain carries neither, which keeps the focus
/// system's work proportional to the number of things that react rather than to the whole screen.
#[cfg(feature = "render")]
fn make_interactive(entity: &mut bevy::ecs::world::EntityWorldMut, config: &BcsUiNodeConfig) {
    if config.interactive != 0 {
        entity.insert(bevy::ui::widget::Button);
    }
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
                // `FontSource` can also name a generic family, which is not offered here: that
                // path needs Bevy's `system_font_discovery`, and on Linux the crate behind it
                // links against fontconfig at build time. Text renders nothing at all without
                // the feature, so the choice is a font of your own or Bevy's.
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
                        ..Default::default()
                    },
                    // What keeps a long string inside its node. The width it breaks against is
                    // the layout's, so a node free to grow never wraps however this is set.
                    TextLayout::new(
                        justify(text_config.justify),
                        linebreak(text_config.linebreak),
                    ),
                    // The node's colour is the text's here: a run of text has no background
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
                entity.id().to_bits()
            })
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
/// The node keeps its layout: the image fills what the layout gave it, which is what `mode` is
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
/// Only means anything on a node whose overflow is set to scroll: that is what clips the contents
/// to the node, and this is how far they have been pushed, in logical pixels from the top left.
/// Bevy has no scrolling input of its own, so a wheel or a drag is read like any other input and
/// turned into a call here.
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
            use bevy::ui::ScrollPosition;

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                entity_mut.insert(ScrollPosition(Vec2::new(x, y)));
                status::OK
            })
        }
    })
}

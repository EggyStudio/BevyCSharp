//! Runs of text, spawning one and its spans and replacing what one says.

use super::*;

/// Translates how much room is added between the letters of a run of text.
///
/// Only called for a value other than zero, because zero is both the default and no change, so a
/// caller leaving the field alone and one asking for the fit the font already has need the same
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
/// Only called for a height above zero, because zero is a caller leaving the field alone and Bevy
/// uses the font's own spacing when the component is absent.
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
                // A negative key is the font compiled into Bevy, which keeps text working with no
                // asset at all. A key that names nothing is a mistake rather than a reason to fall
                // back quietly, so it refuses.
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

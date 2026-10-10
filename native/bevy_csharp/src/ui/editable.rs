//! Bevy's editable text, a field the player types into, read and replaced from C#.

use super::*;

/// How an editable text field behaves, as C# describes it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsEditableTextConfig {
    /// The most characters it holds, or zero for no limit.
    pub max_characters: i32,
    /// How many glyphs wide it is, or zero to be as wide as its node lays it out.
    pub visible_width: f32,
    /// How many lines tall it is, or zero for one.
    pub visible_lines: f32,
    /// Non-zero to let Enter start a new line.
    pub allow_newlines: i32,
    /// `0` typed into, `1` read only, its text selected and copied but not changed, `2` shown
    /// alone, neither changed nor selected, Bevy's `TextReadWriteMode`.
    pub mode: i32,
}

/// Makes a UI node a text field the player types into, holding `text` to begin with.
///
/// Bevy's `EditableText` keeps its text in an editor that moves a cursor through it by Unicode's
/// rules, which is no value reflection can carry, so it is made here, with Bevy's cursor style and,
/// where `allowed` names characters, a filter refusing any other. Typing reaches whichever field
/// has the input focus, which a click on it gives it.
///
/// # Safety
/// `config` must point to a readable [`BcsEditableTextConfig`], `text` must be a NUL-terminated
/// UTF-8 string, and `allowed` one or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_editable_text(
    entity: u64,
    config: *const BcsEditableTextConfig,
    text: *const core::ffi::c_char,
    allowed: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config, text, allowed);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::text::{EditableText, EditableTextFilter, TextCursorStyle, TextEdit, TextReadWriteMode};

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };
            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return status::NULL_ARG;
            };
            let allowed = if allowed.is_null() {
                None
            } else {
                unsafe { crate::interop::cstr_to_string(allowed) }.filter(|allowed| !allowed.is_empty())
            };

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                let max_characters = (config.max_characters > 0).then_some(config.max_characters as usize);
                let visible_width = (config.visible_width > 0.0).then_some(config.visible_width);
                let visible_lines = Some(if config.visible_lines > 0.0 { config.visible_lines } else { 1.0 });
                let allow_newlines = config.allow_newlines != 0;

                // A field set again is changed where it stands, its text replaced as
                // `bcs_ui_set_editable_value` replaces it. Inserting a new one over it left Bevy's
                // laid out text behind, so the field held its text and drew none of it.
                if let Some(mut editable) = entity_mut.get_mut::<EditableText>() {
                    editable.max_characters = max_characters;
                    editable.visible_width = visible_width;
                    editable.visible_lines = visible_lines;
                    editable.allow_newlines = allow_newlines;
                    editable.clear();
                    editable.editor_mut().set_text(&text);
                    editable.queue_edit(TextEdit::TextEnd(false));
                } else {
                    let mut editable = EditableText::new(text);
                    editable.max_characters = max_characters;
                    editable.visible_width = visible_width;
                    editable.visible_lines = visible_lines;
                    editable.allow_newlines = allow_newlines;
                    // Bevy 0.20's text input widget, which takes the keys a focused field is
                    // typed into and requires the editable text it edits.
                    entity_mut.insert((editable, TextCursorStyle::default(), bevy::ui_widgets::TextInput));
                }
                entity_mut.insert(match config.mode {
                    1 => TextReadWriteMode::ReadOnly,
                    2 => TextReadWriteMode::Static,
                    _ => TextReadWriteMode::Editable,
                });
                match allowed {
                    Some(allowed) => {
                        let allowed: Vec<char> = allowed.chars().collect();
                        entity_mut.insert(EditableTextFilter::new(move |c| allowed.contains(&c)));
                    }
                    None => {
                        entity_mut.remove::<EditableTextFilter>();
                    }
                }

                status::OK
            })
        }
    })
}

/// Writes what a text field holds, and returns its length in bytes, or [`status::NOT_PRESENT`]
/// for an entity that is no field.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_editable_text(entity: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(editable) = entity_ref.get::<bevy::text::EditableText>() else {
                    return status::NOT_PRESENT;
                };

                let text = editable.value().to_string();
                unsafe { crate::interop::write_text(&text, out, capacity) }
            })
        }
    })
}

/// Replaces what a text field holds, the cursor at its end, and drops any edit not yet applied.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_editable_value(entity: u64, text: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, text);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::text::{EditableText, TextEdit};

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(mut editable) = entity_mut.get_mut::<EditableText>() else {
                    return status::NOT_PRESENT;
                };

                editable.clear();
                editable.editor_mut().set_text(&text);
                editable.queue_edit(TextEdit::TextEnd(false));
                status::OK
            })
        }
    })
}

/// A text field's cursor and selection as C# describes them, Bevy's `TextCursorStyle`, every color
/// linear red, green, blue and alpha.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsTextCursor {
    pub color: [f32; 4],
    /// Behind the selected text while the field has the focus.
    pub selection: [f32; 4],
    /// Behind it while the field has not.
    pub unfocused_selection: [f32; 4],
    /// The selected text's own color, where `has_selected_text` is non-zero.
    pub selected_text: [f32; 4],
    pub has_selected_text: i32,
    /// How round the selection's corners are, a fraction of its height up to a half.
    pub selection_radius: f32,
}

/// Gives a text field the cursor and selection `cursor` describes.
///
/// Bevy does not reflect its cursor style, which no wrapper can then reach, so it is set here.
/// Returns [`status::NOT_PRESENT`] for an entity that is no field.
///
/// # Safety
/// `cursor` must point to a readable [`BcsTextCursor`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_text_cursor(entity: u64, cursor: *const BcsTextCursor) -> i32 {
    crate::interop::guard(|| {
        if cursor.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::text::{EditableText, TextCursorStyle};

            let cursor = unsafe { *cursor };
            let color = |rgba: [f32; 4]| bevy::color::Color::linear_rgba(rgba[0], rgba[1], rgba[2], rgba[3]);

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                if !entity_mut.contains::<EditableText>() {
                    return status::NOT_PRESENT;
                }

                entity_mut.insert(TextCursorStyle {
                    color: color(cursor.color),
                    selection_color: color(cursor.selection),
                    unfocused_selection_color: color(cursor.unfocused_selection),
                    selected_text_color: (cursor.has_selected_text != 0).then(|| color(cursor.selected_text)),
                    selection_radius: cursor.selection_radius.clamp(0.0, 0.5),
                });
                status::OK
            })
        }
    })
}

/// Writes the part of a text field's text it shows, in the text's own layout units: where its top
/// left corner is, across and down, then how wide and how tall it is. Returns
/// [`status::NOT_PRESENT`] for an entity that is no field.
///
/// Bevy keeps a field's scroll as this viewport rather than as a node's scroll position, which a
/// game drawing a scrollbar of its own for a field reads beside the text's laid out size.
///
/// # Safety
/// `out` must be writable for four floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_editable_viewport(entity: u64, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Ok(entity_ref) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(editable) = entity_ref.get::<bevy::text::EditableText>() else {
                    return status::NOT_PRESENT;
                };

                let viewport = &editable.viewport;
                let values = [viewport.offset.x, viewport.offset.y, viewport.size.x, viewport.size.y];
                unsafe { core::ptr::copy_nonoverlapping(values.as_ptr(), out, 4) };
                status::OK
            })
        }
    })
}

/// Scrolls a text field to show its text from `x` across and `y` down, in the text's layout units.
/// Returns [`status::NOT_PRESENT`] for an entity that is no field.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_scroll_editable(entity: u64, x: f32, y: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, x, y);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(mut editable) = entity_mut.get_mut::<bevy::text::EditableText>() else {
                    return status::NOT_PRESENT;
                };

                editable.viewport.offset = bevy::math::Vec2::new(x, y);
                status::OK
            })
        }
    })
}

/// Sets how many lines tall a text field is, its text and cursor as they were, where setting the
/// field again would replace its text. Returns [`status::NOT_PRESENT`] for an entity that is no
/// field.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_set_editable_lines(entity: u64, lines: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, lines);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(mut editable) = entity_mut.get_mut::<bevy::text::EditableText>() else {
                    return status::NOT_PRESENT;
                };

                editable.visible_lines = Some(if lines > 0.0 { lines } else { 1.0 });
                status::OK
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    /// The managed mirror is checked against these same numbers.
    #[test]
    fn the_field_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(core::mem::offset_of!(BcsEditableTextConfig, allow_newlines), 12);
        assert_eq!(core::mem::offset_of!(BcsEditableTextConfig, mode), 16);
        assert_eq!(core::mem::size_of::<BcsEditableTextConfig>(), 20);
    }

    #[test]
    fn the_cursor_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(core::mem::offset_of!(BcsTextCursor, selected_text), 48);
        assert_eq!(core::mem::offset_of!(BcsTextCursor, has_selected_text), 64);
        assert_eq!(core::mem::size_of::<BcsTextCursor>(), 72);
    }
}

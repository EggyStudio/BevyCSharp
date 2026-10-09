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
            use bevy::text::{EditableText, EditableTextFilter, TextCursorStyle, TextEdit};

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

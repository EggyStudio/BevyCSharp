//! HTML and CSS driven UI, reachable from C#.
//!
//! Needs the `editor` feature, which layers `bevy_extended_ui` on top of the render profile. A
//! build without it keeps every entry point here so the managed side links either way, and they
//! report [`status::UNSUPPORTED`].
//!
//! The division of labour: `bevy_extended_ui` parses the documents and owns the widgets, which
//! are ordinary `bevy_ui` entities. This module resolves an element to its entity and reads or
//! writes the one value that element carries, so the managed side never holds a widget type.
//! That containment is deliberate. The crate's own API is in flux, and keeping every mention of
//! it inside this file means an upstream change lands here and nowhere else.
//!
//! An element is found by its CSS id, which is global rather than per document, so ids have to
//! be unique across every document that is open at once. That is the rule HTML already has for
//! ids; the whole open UI is one document as far as the crate is concerned.

use crate::interop::{status, BcsUiEvent};

/// Kinds reported by [`bcs_xui_events`], matching `Bevy.UiEventKind`.
pub mod event_kind {
    /// A widget was clicked.
    pub const CLICK: i32 = 0;
    /// A widget's value changed.
    pub const CHANGE: i32 = 1;
    /// A form was submitted. Not reported yet, and reserved so the numbering does not move.
    pub const SUBMIT: i32 = 2;
    /// A widget took focus.
    pub const FOCUS: i32 = 3;
}

#[cfg(feature = "editor")]
mod live {
    use bevy::ecs::resource::Resource;

    /// The documents C# has opened, by the id it holds.
    ///
    /// The crate keys its registry by name, so the id is translated on every call rather than
    /// handed over. Ids are never reused, which is what stops a stale one naming a document that
    /// was opened after it closed.
    #[derive(Resource, Default)]
    pub struct Documents {
        pub open: Vec<(i32, String)>,
        pub next: i32,
    }

    /// What the widgets reported since the managed side last looked.
    ///
    /// Observers cannot be read from an exclusive system, and every C# system is one, so they
    /// push here and the drain hands the queue over a frame at a time. The same arrangement the
    /// gizmo queue uses, for the same reason.
    #[derive(Resource, Default)]
    pub struct UiEvents(pub Vec<crate::interop::BcsUiEvent>);
}

/// What an element was last seen holding, so a real change can be told from a repaint.
#[cfg(feature = "editor")]
#[derive(Default, PartialEq)]
struct Snapshot {
    number: f32,
    text: String,
    checked: bool,
    focused: bool,
}

/// Installs the UI plugin and the reporting the managed side drains.
///
/// The crate has an event layer of its own, and this deliberately does not use it. Its events
/// only fire for elements that named a handler in an HTML attribute, `onclick="something"`, and
/// that handler is resolved through a Rust macro registry that C# cannot join. Going that way
/// would mean every element had to declare a name in the markup that then had to agree with a
/// name on the managed side, for no gain.
///
/// So clicks come from Bevy's own picking, which is what the crate builds its own layer on top
/// of, and value changes come from watching what the widgets hold. An element reports only if it
/// has a CSS id, because an id is how the managed side addresses it and an element it cannot
/// name is one it cannot have asked about.
#[cfg(feature = "editor")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::picking::events::{Click, Pointer};
    use bevy::prelude::*;
    use bevy_extended_ui::styles::CssID;
    use bevy_extended_ui::widgets::{InputField, Slider, UIWidgetState};
    use bevy_extended_ui::ExtendedUiPlugin;
    use std::collections::HashMap;

    use crate::interop::BcsUiEvent;
    use live::UiEvents;

    app.add_plugins(ExtendedUiPlugin);
    app.init_resource::<live::Documents>();
    app.init_resource::<UiEvents>();

    app.add_observer(
        |click: On<Pointer<Click>>,
         addressable: Query<(), With<CssID>>,
         parents: Query<&ChildOf>,
         mut events: ResMut<UiEvents>| {
            // Picking reports the deepest thing under the pointer, which for a button is the
            // text inside it rather than the button. Walk up until something is addressable.
            let mut entity = click.entity;
            bevy::log::info!("[diag] raw click target {:?}", entity);
            loop {
                if addressable.get(entity).is_ok() {
                    events.0.push(BcsUiEvent {
                        kind: event_kind::CLICK,
                        entity: entity.to_bits(),
                    });
                    return;
                }

                let Ok(parent) = parents.get(entity) else {
                    return;
                };
                entity = parent.parent();
            }
        },
    );

    // Compared against what was there last frame rather than driven by change detection, because
    // hovering an element rewrites its state without changing anything the caller asked about.
    app.add_systems(
        Update,
        move |elements: Query<(
            Entity,
            Option<&Slider>,
            Option<&InputField>,
            Option<&UIWidgetState>,
        ), With<CssID>>,
              mut seen: Local<HashMap<Entity, Snapshot>>,
              mut events: ResMut<UiEvents>| {
            let mut alive = Vec::with_capacity(seen.len());

            for (entity, slider, field, state) in elements.iter() {
                alive.push(entity);

                let now = Snapshot {
                    number: slider.map(|s| s.value).unwrap_or_default(),
                    text: field.map(|f| f.text.clone()).unwrap_or_default(),
                    checked: state.is_some_and(|s| s.checked),
                    focused: state.is_some_and(|s| s.focused),
                };

                let Some(before) = seen.get(&entity) else {
                    // First sight is not a change. Recording it without reporting is what stops
                    // every element announcing itself on the frame its document finishes loading.
                    seen.insert(entity, now);
                    continue;
                };

                if now.number != before.number
                    || now.text != before.text
                    || now.checked != before.checked
                {
                    events.0.push(BcsUiEvent {
                        kind: event_kind::CHANGE,
                        entity: entity.to_bits(),
                    });
                }

                if now.focused && !before.focused {
                    events.0.push(BcsUiEvent {
                        kind: event_kind::FOCUS,
                        entity: entity.to_bits(),
                    });
                }

                seen.insert(entity, now);
            }

            // A document that closed takes its elements with it, and a stale snapshot would
            // report a change against whatever later reused the slot.
            if seen.len() > alive.len() {
                seen.retain(|entity, _| alive.contains(entity));
            }
        },
    );
}

/// Opens an HTML document and returns the id C# holds it by, or a negative status.
///
/// The path is relative to the asset root, as every other asset path is. A stylesheet the
/// document links is resolved relative to the document rather than to the asset root, so a
/// `<link href="theme.css">` beside `panels/thing.html` is `panels/theme.css` on disk.
///
/// The document also needs a `<meta name="...">` tag in its `<head>`. The parser refuses one
/// without it, and reports that on the log rather than through this call.
///
/// # Safety
/// `path` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_open(path: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = path;
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy::asset::AssetServer;
            use bevy_extended_ui::html::HtmlSource;
            use bevy_extended_ui::io::HtmlAsset;
            use bevy_extended_ui::old::registry::UiRegistry;

            let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let Some(assets) = world.get_resource::<AssetServer>() else {
                    return status::INVALID_STATE;
                };
                let handle = assets.load::<HtmlAsset>(path.clone());

                let id = {
                    let Some(mut documents) = world.get_resource_mut::<live::Documents>() else {
                        return status::INVALID_STATE;
                    };
                    documents.next += 1;
                    let id = documents.next;
                    documents.open.push((id, path.clone()));
                    id
                };

                let Some(mut registry) = world.get_resource_mut::<UiRegistry>() else {
                    return status::INVALID_STATE;
                };

                #[allow(deprecated)]
                registry.add_and_use(path, HtmlSource::from_handle(handle));
                id
            })
        }
    })
}

/// Takes a document back off the screen.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_close(document: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = document;
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::old::registry::UiRegistry;

            crate::state::with_world(|world| {
                let name = {
                    let Some(mut documents) = world.get_resource_mut::<live::Documents>() else {
                        return status::INVALID_STATE;
                    };
                    let Some(index) = documents.open.iter().position(|(id, _)| *id == document)
                    else {
                        return status::NO_ENTITY;
                    };
                    documents.open.remove(index).1
                };

                let Some(mut registry) = world.get_resource_mut::<UiRegistry>() else {
                    return status::INVALID_STATE;
                };

                #[allow(deprecated)]
                registry.remove(&name);
                status::OK
            })
        }
    })
}

/// Resolves a CSS id to the entity carrying that element, or `0` when nothing has it.
///
/// # Safety
/// `css_id` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_element(css_id: *const core::ffi::c_char) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = css_id;
            0
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::styles::CssID;

            let Some(wanted) = (unsafe { crate::interop::cstr_to_string(css_id) }) else {
                return 0;
            };

            crate::state::with_world_opt(|world| {
                world
                    .query::<(bevy::ecs::entity::Entity, &CssID)>()
                    .iter(world)
                    .find(|(_, id)| id.0 == wanted)
                    .map(|(entity, _)| entity.to_bits())
                    .unwrap_or(0)
            })
            .unwrap_or(0)
        }
    })
}

/// Writes an element's text into `out`, returning the length in bytes it needs.
///
/// An input field answers with what was typed into it; anything else answers with its inner
/// text. Follows the convention every entry point returning text does: call with no buffer to
/// learn the length, then call again with one that size.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_get_text(entity: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::html::HtmlInnerContent;
            use bevy_extended_ui::widgets::InputField;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };

                let text = if let Some(field) = entity_ref.get::<InputField>() {
                    field.text.clone()
                } else if let Some(content) = entity_ref.get::<HtmlInnerContent>() {
                    content.inner_text().to_string()
                } else {
                    return status::NOT_PRESENT;
                };

                unsafe { crate::interop::write_text(&text, out, capacity) }
            })
        }
    })
}

/// Replaces an element's text.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_set_text(entity: u64, text: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, text);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::html::HtmlInnerContent;
            use bevy_extended_ui::widgets::InputField;

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                if let Some(mut field) = entity_mut.get_mut::<InputField>() {
                    field.text = text;
                    return status::OK;
                }

                if let Some(mut content) = entity_mut.get_mut::<HtmlInnerContent>() {
                    content.set_inner_text(text);
                    return status::OK;
                }

                status::NOT_PRESENT
            })
        }
    })
}

/// Reads the number an element carries, which today means a slider's value.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_get_number(entity: u64, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::widgets::Slider;

            if out.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(slider) = entity_ref.get::<Slider>() else {
                    return status::NOT_PRESENT;
                };

                unsafe { out.write(slider.value) };
                status::OK
            })
        }
    })
}

/// Moves a slider to `value`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_set_number(entity: u64, value: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, value);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::widgets::Slider;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(mut slider) = entity_mut.get_mut::<Slider>() else {
                    return status::NOT_PRESENT;
                };

                slider.value = value.clamp(slider.range_start, slider.range_end);
                status::OK
            })
        }
    })
}

/// Reads whether an element is ticked, which covers a checkbox, a switch and a toggle.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_get_flag(entity: u64, out: *mut i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::widgets::UIWidgetState;

            if out.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(state) = entity_ref.get::<UIWidgetState>() else {
                    return status::NOT_PRESENT;
                };

                unsafe { out.write(i32::from(state.checked)) };
                status::OK
            })
        }
    })
}

/// Ticks or unticks an element.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_set_flag(entity: u64, value: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, value);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy_extended_ui::widgets::UIWidgetState;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(mut state) = entity_mut.get_mut::<UIWidgetState>() else {
                    return status::NOT_PRESENT;
                };

                state.checked = value != 0;
                status::OK
            })
        }
    })
}

/// Copies what the widgets reported since the last call into `out`.
///
/// Returns how many were written, or a negative status. A full buffer is not an error: the rest
/// stay queued and arrive on the next call, because dropping a click silently would leave a
/// button that sometimes does nothing.
///
/// # Safety
/// `out` must be writable for `capacity` events.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_events(out: *mut BcsUiEvent, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            if out.is_null() && capacity > 0 {
                return status::NULL_ARG;
            }
            let capacity = capacity.max(0) as usize;

            crate::state::with_world(|world| {
                let Some(mut queue) = world.get_resource_mut::<live::UiEvents>() else {
                    return status::UNSUPPORTED;
                };

                let taken = queue.0.len().min(capacity);
                for (index, event) in queue.0.drain(..taken).enumerate() {
                    // SAFETY: `index < taken <= capacity`, and `out` is valid for `capacity`.
                    unsafe { out.add(index).write(event) };
                }

                taken as i32
            })
        }
    })
}

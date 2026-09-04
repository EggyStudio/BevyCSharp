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
    /// The documents were rebuilt after one changed on disk. Reported against no element,
    /// because every element is a new one.
    pub const RELOADED: i32 = 4;
    /// A document changed on disk and a rebuild has been asked for. Reported against no element,
    /// and followed by [`RELOADED`] once the new widgets are up.
    pub const RELOADING: i32 = 5;
    /// A widget was clicked with the secondary button, which is what asks for a context menu.
    pub const CONTEXT: i32 = 6;
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
        /// Whether the list changed since the registry was last told about it.
        pub dirty: bool,
    }

    /// What the widgets reported since the managed side last looked.
    ///
    /// Observers cannot be read from an exclusive system, and every C# system is one, so they
    /// push here and the drain hands the queue over a frame at a time. The same arrangement the
    /// gizmo queue uses, for the same reason.
    #[derive(Resource, Default)]
    pub struct UiEvents(pub Vec<crate::interop::BcsUiEvent>);
}

/// The distinct paths of every document C# currently has open, in the order they were opened.
///
/// What the registry has to be told each time one opens or closes: it keeps a list of the
/// documents showing, and setting that list is the only way to have more than one.
#[cfg(feature = "editor")]
fn open_documents(world: &mut bevy::ecs::world::World) -> Vec<String> {
    let Some(documents) = world.get_resource::<live::Documents>() else {
        return Vec::new();
    };

    let mut names: Vec<String> = Vec::with_capacity(documents.open.len());
    for (_, path) in &documents.open {
        if !names.iter().any(|seen| seen == path) {
            names.push(path.clone());
        }
    }

    names
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
            // Which button, because asking for a context menu is a different thing from pressing
            // a button and the two arrive through the same event.
            let kind = if click.event().button == bevy::picking::pointer::PointerButton::Secondary
            {
                event_kind::CONTEXT
            } else {
                event_kind::CLICK
            };

            // Picking reports the deepest thing under the pointer, which for a button is the
            // text inside it rather than the button. Walk up until something is addressable.
            let mut entity = click.entity;
            loop {
                if addressable.get(entity).is_ok() {
                    events.0.push(BcsUiEvent {
                        kind,
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

    // What is showing, applied once a frame. Every open and close marks the list dirty rather
    // than telling the registry, because telling it rebuilds every widget of every document, and
    // doing that once for four panels opening together is both faster and steadier: each rebuild
    // releases every live widget's id back to a pool, and the fewer of those there are the fewer
    // chances there are for two widgets to end up sharing one.
    app.add_systems(
        bevy::app::PreUpdate,
        |mut documents: ResMut<live::Documents>,
         mut registry: ResMut<bevy_extended_ui::old::registry::UiRegistry>| {
            if !documents.dirty {
                return;
            }

            documents.dirty = false;

            let mut names: Vec<String> = Vec::with_capacity(documents.open.len());
            for (_, path) in &documents.open {
                if !names.iter().any(|seen| seen == path) {
                    names.push(path.clone());
                }
            }

            #[allow(deprecated)]
            registry.use_uis(names);
        },
    );

    // The crate draws its interface through a camera of its own, and two cameras on one window
    // have to agree about what they are drawing into. A camera's high dynamic range setting and
    // its multisampling both decide that, so a disagreement about either is not a compositing
    // question with a wrong answer: they are two different targets, and whichever writes the
    // window last wins. The interface camera is ordered last, so the scene disappears behind it.
    //
    // The crate asks for high dynamic range through its config, which rebuilds the camera, and
    // fixes multisampling once when it spawns and never looks again. So one is set there and the
    // other here, and the config is only written when it differs, or the camera would be rebuilt
    // every frame.
    //
    // Kept in step here rather than left to the caller. Nobody putting a panel over a scene
    // should have to know any of this to find out why the scene went black.
    app.add_systems(
        Update,
        |scene: Query<
            (bevy::ecs::query::Has<bevy::camera::Hdr>, &Msaa),
            (
                bevy::ecs::query::With<bevy::camera::Camera3d>,
                bevy::ecs::query::Without<bevy_extended_ui::UiCamera>,
            ),
        >,
         interface: Query<
            (bevy::ecs::entity::Entity, &Msaa),
            bevy::ecs::query::With<bevy_extended_ui::UiCamera>,
        >,
         mut config: ResMut<bevy_extended_ui::ExtendedUiConfiguration>,
         mut commands: Commands| {
            let Some((hdr, msaa)) = scene.iter().next() else {
                return;
            };

            if config.hdr_support != hdr {
                config.hdr_support = hdr;
            }

            for (entity, theirs) in interface.iter() {
                if theirs != msaa {
                    commands.entity(entity).insert(*msaa);
                }
            }
        },
    );

    // Bevy reloads a document whose file changed, but nothing rebuilds the widgets from it: the
    // crate watches its stylesheets and not its documents, so a CSS edit reaches the screen and
    // an HTML edit does not. Asking the registry for a rebuild is what closes that.
    app.add_systems(
        Update,
        |mut changes: MessageReader<bevy::asset::AssetEvent<bevy_extended_ui::io::HtmlAsset>>,
         mut registry: ResMut<bevy_extended_ui::old::registry::UiRegistry>,
         mut events: ResMut<UiEvents>| {
            let changed = changes
                .read()
                .any(|change| matches!(change, bevy::asset::AssetEvent::Modified { .. }));

            if !changed {
                return;
            }

            registry.ui_update = true;

            // Said as soon as the rebuild is asked for, because the widgets are about to be
            // despawned and anything still reading them would be reading the dead.
            events.0.push(BcsUiEvent {
                kind: event_kind::RELOADING,
                entity: 0,
            });
        },
    );

    // A rebuild respawns every widget, so everything the managed side is holding goes stale at
    // once. Reported when the widgets are up rather than when the rebuild was asked for: the two
    // are several frames apart, and a caller that dropped its entities in between would look them
    // up again, find the ones still standing, and cache those instead.
    app.add_systems(
        Update,
        |mut spawned: MessageReader<bevy_extended_ui::html::HtmlAllWidgetsSpawned>,
         mut events: ResMut<UiEvents>| {
            if spawned.read().next().is_none() {
                return;
            }

            events.0.push(BcsUiEvent {
                kind: event_kind::RELOADED,
                entity: 0,
            });
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
                registry.add(path, HtmlSource::from_handle(handle));

                // Which documents are showing is applied once a frame rather than here. Telling
                // the registry rebuilds every widget on screen, so opening four panels in one
                // breath would do that four times, and each rebuild is a chance for the crate to
                // hand two live widgets the same id.
                if let Some(mut documents) = world.get_resource_mut::<live::Documents>() {
                    documents.dirty = true;
                }

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

                let showing = open_documents(world);

                let Some(mut registry) = world.get_resource_mut::<UiRegistry>() else {
                    return status::INVALID_STATE;
                };

                #[allow(deprecated)]
                {
                    // Removed from the registry only when nothing else has it open, since two
                    // panels may share a document.
                    if !showing.iter().any(|open| open == &name) {
                        registry.remove(&name);
                    }
                }

                if let Some(mut documents) = world.get_resource_mut::<live::Documents>() {
                    documents.dirty = true;
                }

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
            use bevy_extended_ui::widgets::{Button, Headline, InputField, Paragraph};

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };

                // Each widget keeps the text it draws in a field of its own, and the parsed
                // content is a separate thing that is only read when the widget is built. Asking
                // the widget first is what makes a read agree with what is on the screen.
                let text = if let Some(field) = entity_ref.get::<InputField>() {
                    field.text.clone()
                } else if let Some(paragraph) = entity_ref.get::<Paragraph>() {
                    paragraph.text.clone()
                } else if let Some(headline) = entity_ref.get::<Headline>() {
                    headline.text.clone()
                } else if let Some(button) = entity_ref.get::<Button>() {
                    button.text.clone()
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
            use bevy_extended_ui::widgets::{Button, Headline, InputField, Paragraph};

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                // The widget's own field, for the same reason as the read: writing the parsed
                // content changes what the document said rather than what is being drawn, so it
                // is the last resort rather than the first.
                if let Some(mut field) = entity_mut.get_mut::<InputField>() {
                    field.text = text;
                    return status::OK;
                }

                if let Some(mut paragraph) = entity_mut.get_mut::<Paragraph>() {
                    paragraph.text = text;
                    return status::OK;
                }

                if let Some(mut headline) = entity_mut.get_mut::<Headline>() {
                    headline.text = text;
                    return status::OK;
                }

                if let Some(mut button) = entity_mut.get_mut::<Button>() {
                    button.text = text;
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
            use bevy::picking::backend::HitData;
            use bevy::picking::events::{Click, Pointer};
            use bevy::picking::pointer::{Location, PointerButton, PointerId};
            use bevy::camera::NormalizedRenderTarget;
            use bevy_extended_ui::widgets::UIWidgetState;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);

                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(state) = entity_ref.get::<UIWidgetState>() else {
                    return status::NOT_PRESENT;
                };

                let wanted = value != 0;
                if state.checked == wanted {
                    return status::OK;
                }

                // Ticked by asking the widget to toggle rather than by writing the flag, because
                // the flag is not what is drawn. The tick is a child entity the crate spawns
                // when its own click handler runs, and writing the state behind that handler
                // leaves the mark where it was and the widget's own copy of the flag disagreeing
                // with this one. The next real click then toggles from the wrong value and
                // spawns a second mark beside the first.
                //
                // A widget that has no such handler is left to the plain write below, which is
                // all a switch or a toggle needs.
                //
                // The crate's handler also takes focus, so setting a checkbox from code focuses
                // it as a click would. That is a side effect of borrowing its logic rather than
                // imitating it, and the better trade: imitating it means spawning the mark here,
                // which needs the widget's image, its laid-out size and its stylesheet, and would
                // be wrong again the moment any of those changed upstream.
                let Some(window) = world
                    .query_filtered::<bevy::ecs::entity::Entity, bevy::ecs::query::With<bevy::window::PrimaryWindow>>()
                    .iter(world)
                    .next()
                else {
                    return status::INVALID_STATE;
                };

                world.trigger(Pointer::new(
                    PointerId::Mouse,
                    Location {
                        target: NormalizedRenderTarget::Window(
                            bevy::window::WindowRef::Primary
                                .normalize(Some(window))
                                .unwrap(),
                        ),
                        position: bevy::math::Vec2::ZERO,
                    },
                    Click {
                        button: PointerButton::Primary,
                        hit: HitData::new(entity, 0.0, None, None),
                        duration: core::time::Duration::ZERO,
                        count: 1,
                    },
                    entity,
                ));

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

// -- Placement
//
// Where a panel sits, as something the panel decides rather than something its stylesheet does.
//
// A stylesheet is the right place for what a window looks like and the wrong place for where it
// is: a layout that can be described, saved and rearranged has to be data the editor holds, and
// a rule inside a CSS file is neither readable nor writable from the side doing the arranging.
// So the chrome stays in CSS and the rectangle comes from here.
//
// These write the ordinary `bevy_ui` components on the element the crate spawned, which is what
// makes them work at all: an extended-ui widget is a `Node` like any other once it is up.

/// Places an element at an absolute rectangle, in logical pixels.
///
/// Any of the four may be `NaN`, which leaves that one exactly as it was. That is what makes the
/// division of labour work: the stylesheet says how wide a panel is and this says where it goes,
/// and a rectangle with two of its numbers missing does not quietly throw the stylesheet's
/// answer away.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_set_rect(entity: u64, left: f32, top: f32, width: f32, height: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, left, top, width, height);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy::ui::{Node, PositionType, Val};

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(mut node) = entity_mut.get_mut::<Node>() else {
                    return status::NOT_PRESENT;
                };

                // Absolute regardless, because a rectangle means nothing to a node the flex
                // layout is still placing. Saying so here rather than in the stylesheet is what
                // lets one document be a docked panel in one layout and a flyout in another.
                node.position_type = PositionType::Absolute;

                if !left.is_nan() {
                    node.left = Val::Px(left);
                }
                if !top.is_nan() {
                    node.top = Val::Px(top);
                }
                if !width.is_nan() {
                    node.width = Val::Px(width);
                }
                if !height.is_nan() {
                    node.height = Val::Px(height);
                }

                status::OK
            })
        }
    })
}

/// Reads whether an element is on screen, writing `1` or `0`.
///
/// The stylesheet is reapplied to a widget whenever the interface restyles it, which puts its
/// display back to whatever the CSS says. A tool that hides a row has to be able to notice that
/// and hide it again, and it cannot notice what it cannot read.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_get_visible(entity: u64, out: *mut i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy::ui::{Display, Node};

            if out.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(node) = entity_ref.get::<Node>() else {
                    return status::NOT_PRESENT;
                };

                unsafe { out.write(i32::from(node.display != Display::None)) };
                status::OK
            })
        }
    })
}

/// Shows or hides an element, and everything under it.
///
/// Hidden by `Display::None` rather than by visibility, so a hidden panel takes no space and its
/// neighbours close up, which is what a flyout being dismissed should look like.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_set_visible(entity: u64, visible: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, visible);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy::ui::{Display, Node};

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };
                let Some(mut node) = entity_mut.get_mut::<Node>() else {
                    return status::NOT_PRESENT;
                };

                node.display = if visible != 0 {
                    Display::Flex
                } else {
                    Display::None
                };

                status::OK
            })
        }
    })
}

/// Puts an element in front of or behind everything else on screen.
///
/// Global rather than among its siblings, because a panel is the root of its own document and an
/// order that only counts within one document cannot put a menu over a panel. What is drawn on
/// top of what is a question about the whole screen.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_set_layer(entity: u64, layer: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, layer);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy::ui::GlobalZIndex;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                entity_mut.insert(GlobalZIndex(layer));
                status::OK
            })
        }
    })
}

/// The element the keyboard is going to, or `0` when nothing has focus.
///
/// What a tool needs to know before writing to a text field: a panel that shows the world writes
/// its values out every frame, and doing that to the field somebody is typing in replaces what
/// they have typed so far with what the world still says.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_xui_focused() -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "editor"))]
        {
            0u64
        }

        #[cfg(feature = "editor")]
        {
            use bevy::ecs::entity::Entity;
            use bevy_extended_ui::styles::CssID;
            use bevy_extended_ui::widgets::UIWidgetState;

            crate::state::with_world_opt(|world| {
                let mut query = world.query::<(Entity, &UIWidgetState, &CssID)>();

                for (entity, state, _) in query.iter(world) {
                    if state.focused {
                        return entity.to_bits();
                    }
                }

                0u64
            })
            .unwrap_or(0)
        }
    })
}

/// Reads where an element ended up: `x`, `y`, `width`, `height` in logical pixels.
///
/// The rectangle the layout produced rather than the one that was asked for, which is the only
/// one worth testing a cursor against. A window that sized itself to its contents, or that a
/// stylesheet placed, answers here exactly as one this side positioned.
///
/// # Safety
/// `out` must be writable for four floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_xui_rect(entity: u64, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (entity, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            use bevy::ui::{ComputedNode, UiGlobalTransform};

            if out.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                let Ok(entity_ref) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };
                let (Some(computed), Some(transform)) = (
                    entity_ref.get::<ComputedNode>(),
                    entity_ref.get::<UiGlobalTransform>(),
                ) else {
                    return status::NOT_PRESENT;
                };

                // Both are in physical pixels, and everything on the managed side works in the
                // logical ones the cursor is reported in, so the scale comes off here rather
                // than in four places over there.
                let scale = computed.inverse_scale_factor();
                let size = computed.size * scale;
                let centre = transform.translation * scale;

                unsafe {
                    out.write(centre.x - size.x * 0.5);
                    out.add(1).write(centre.y - size.y * 0.5);
                    out.add(2).write(size.x);
                    out.add(3).write(size.y);
                }

                status::OK
            })
        }
    })
}


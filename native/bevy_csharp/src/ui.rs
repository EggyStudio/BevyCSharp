//! Bevy's UI, reachable from C#.
//!
//! A UI node is a `Node` component carrying a whole layout description: two dozen fields, several
//! of them enums with payloads. Mirroring that byte for byte is not possible the way `Transform`
//! is, so the bridge takes a description and builds the components on this side, the way
//! [`crate::render`] does for meshes and materials.
//!
//! Everything here needs a render build. A windowless one reports that rather than spawning
//! entities that would never draw.

use crate::interop::{status, BcsUiNodeConfig};
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

/// Builds the `Node` a config describes.
#[cfg(feature = "render")]
fn node_from(config: &BcsUiNodeConfig) -> bevy::ui::Node {
    use bevy::ui::{PositionType, UiRect};

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
        padding: UiRect::all(length(config.padding, config.padding_unit)),
        ..Default::default()
    }
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
/// on the screen. `font_size` is in logical pixels.
///
/// # Safety
/// `text` must be a NUL-terminated UTF-8 string; `config` must point to a readable
/// [`BcsUiNodeConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_spawn_text(
    text: *const core::ffi::c_char,
    config: *const BcsUiNodeConfig,
    font_size: f32,
) -> u64 {
    crate::interop::guard_with(0u64, || {
        #[cfg(not(feature = "render"))]
        {
            let _ = (text, config, font_size);
            0
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::text::{FontSize, TextColor, TextFont};
            use bevy::ui::widget::Text;

            let Some(text) = (unsafe { crate::interop::cstr_to_string(text) }) else {
                return 0;
            };
            if config.is_null() {
                return 0;
            }
            let config = unsafe { *config };

            with_world_opt(|world| {
                let mut entity = world.spawn((
                    Text(text.clone()),
                    TextFont {
                        font_size: FontSize::Px(font_size),
                        ..Default::default()
                    },
                    // The node's colour is the text's here: a run of text has no background
                    // of its own, and giving it one would need a second entity behind it.
                    TextColor(Color::linear_rgba(
                        config.color[0],
                        config.color[1],
                        config.color[2],
                        config.color[3],
                    )),
                    node_from(&config),
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

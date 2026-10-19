//! Feathers, Bevy's styled widgets, spawned from C# one control at a time.
//!
//! Bevy 0.20 builds each Feathers control as a scene component, a scene made from props that a
//! program writes in Bevy's scene notation, a caption among them being a scene of its own. C# has
//! no scene notation, so each control is spawned here from plain props and a caption's text, through
//! the control's own `SceneComponent::scene`, which holds the component, its marker and everything
//! the notation's `@FeathersButton` would give. The control is spawned under the parent C# names,
//! and what it reports reaches C# as the widgets' events do (`crate::widget`).
//!
//! A control's children are its scene's, so what C# adds under one afterwards follows them, and
//! the theme's colors reach a node of C#'s own through [`bcs_feathers_theme_color`].

use crate::interop::status;

/// A button, Bevy's `FeathersButton`.
pub const BUTTON: i32 = 0;
/// A square button for an icon or a symbol, `FeathersToolButton`.
pub const TOOL_BUTTON: i32 = 1;
/// A checkbox with its caption, `FeathersCheckbox`.
pub const CHECKBOX: i32 = 2;
/// A radio button with its caption, `FeathersRadio`, chosen among the others under its group.
pub const RADIO: i32 = 3;
/// A toggle switch, `FeathersToggleSwitch`, a checkbox drawn as a switch.
pub const TOGGLE_SWITCH: i32 = 4;
/// A slider showing its value, `FeathersSlider`.
pub const SLIDER: i32 = 5;
/// The text inside a control, `display::caption`.
pub const CAPTION: i32 = 6;
/// A label beside a control, `display::label`.
pub const LABEL: i32 = 7;
/// A dimmer label, `display::label_dim`.
pub const LABEL_DIM: i32 = 8;
/// A smaller label, `display::label_small`.
pub const LABEL_SMALL: i32 = 9;

/// What a control is made with, each kind reading the fields it has and leaving the rest.
///
/// The managed side's `NativeFeathersProps`, field for field.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsFeathersProps {
    /// A button's look, zero normal, one primary and two plain, its background shown only while
    /// the pointer is over it or it is pressed.
    pub variant: i32,
    /// Which corners are rounded, in the order of Bevy's `RoundedCorners`, none, all, top left,
    /// top right, bottom right, bottom left, top, right, bottom and left.
    pub corners: i32,
    /// A slider's least value.
    pub min: f32,
    /// A slider's greatest value.
    pub max: f32,
}

/// Adds Feathers' plugin and Bevy's dark theme, its one theme, where the app asks for Feathers.
/// The tab navigation it would add with it is the bridge's already.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::feathers::dark_theme::create_dark_theme;
    use bevy::feathers::theme::UiTheme;

    app.add_plugins(bevy::feathers::FeathersCorePlugin);
    app.insert_resource(UiTheme(create_dark_theme()));
}

#[cfg(feature = "render")]
mod spawn {
    use super::*;

    use bevy::ecs::entity::Entity;
    use bevy::ecs::hierarchy::ChildOf;
    use bevy::ecs::world::World;
    use bevy::feathers::controls::{
        ButtonVariant, FeathersButton, FeathersButtonProps, FeathersCheckbox, FeathersCheckboxProps, FeathersRadio,
        FeathersRadioProps, FeathersSlider, FeathersSliderProps, FeathersToggleSwitch, FeathersToolButton,
    };
    use bevy::feathers::display::{caption, label, label_dim, label_small};
    use bevy::feathers::rounded_corners::RoundedCorners;
    use bevy::scene::prelude::*;

    fn corners(place: i32) -> RoundedCorners {
        match place {
            0 => RoundedCorners::None,
            2 => RoundedCorners::TopLeft,
            3 => RoundedCorners::TopRight,
            4 => RoundedCorners::BottomRight,
            5 => RoundedCorners::BottomLeft,
            6 => RoundedCorners::Top,
            7 => RoundedCorners::Right,
            8 => RoundedCorners::Bottom,
            9 => RoundedCorners::Left,
            _ => RoundedCorners::All,
        }
    }

    fn variant(place: i32) -> ButtonVariant {
        match place {
            1 => ButtonVariant::Primary,
            2 => ButtonVariant::Plain,
            _ => ButtonVariant::Normal,
        }
    }

    /// The caption a control holds, none where the text is empty.
    fn captioned(text: &str) -> Box<dyn SceneList> {
        if text.is_empty() { Box::new(bsn_list! {}) } else { Box::new(vec![caption(text.to_string())]) }
    }

    /// Applies `scene` to a new entity under `parent` at once, so what C# writes on the control
    /// afterwards is not written over, or as soon as what it needs has loaded where something has
    /// not, Bevy's queue then applying it.
    fn spawn_under<S: Scene>(world: &mut World, parent: Option<Entity>, scene: impl Fn() -> S) -> Entity {
        let mut spawned = world.spawn_empty();
        if let Some(parent) = parent {
            spawned.insert(ChildOf(parent));
        }
        if spawned.apply_scene(scene()).is_err() {
            spawned.queue_apply_scene(scene());
        }
        spawned.id()
    }

    pub fn control(world: &mut World, kind: i32, parent: Option<Entity>, props: &BcsFeathersProps, text: &str) -> Option<Entity> {
        let entity = match kind {
            BUTTON => spawn_under(
                world,
                parent,
                || <FeathersButton as SceneComponent>::scene(FeathersButtonProps {
                    caption: captioned(text),
                    variant: variant(props.variant),
                    corners: corners(props.corners),
                }),
            ),
            TOOL_BUTTON => spawn_under(
                world,
                parent,
                || <FeathersToolButton as SceneComponent>::scene(FeathersButtonProps {
                    caption: captioned(text),
                    variant: variant(props.variant),
                    corners: corners(props.corners),
                }),
            ),
            CHECKBOX => spawn_under(
                world,
                parent,
                || <FeathersCheckbox as SceneComponent>::scene(FeathersCheckboxProps { caption: captioned(text) }),
            ),
            RADIO => spawn_under(
                world,
                parent,
                || <FeathersRadio as SceneComponent>::scene(FeathersRadioProps { caption: captioned(text) }),
            ),
            TOGGLE_SWITCH => spawn_under(world, parent, || <FeathersToggleSwitch as SceneComponent>::scene(())),
            SLIDER => spawn_under(
                world,
                parent,
                || <FeathersSlider as SceneComponent>::scene(FeathersSliderProps { min: props.min, max: props.max }),
            ),
            CAPTION => spawn_under(world, parent, || caption(text.to_string())),
            LABEL => spawn_under(world, parent, || label(text.to_string())),
            LABEL_DIM => spawn_under(world, parent, || label_dim(text.to_string())),
            LABEL_SMALL => spawn_under(world, parent, || label_small(text.to_string())),
            _ => return None,
        };
        Some(entity)
    }
}

/// Spawns a Feathers control of `kind` under `parent`, or at the root for `0`, with `props` and
/// the caption `text`, and writes the new entity into `out`.
///
/// `props` may be null for Bevy's defaults. The control is whole once its scene has been applied,
/// which happens as it is spawned where the scene needs nothing loaded, and so before what C#
/// adds under it. Answers [`status::INVALID_STATE`] where the app did not ask for Feathers, and
/// [`status::NULL_ARG`] for a kind there is none of.
///
/// # Safety
/// `props` must be null or valid for reads of one [`BcsFeathersProps`], `text` valid for `len`
/// bytes of UTF-8, and `out` valid for writes of one `u64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_spawn(
    kind: i32,
    parent: u64,
    props: *const BcsFeathersProps,
    text: *const u8,
    len: i32,
    out: *mut u64,
) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() || (text.is_null() && len != 0) || len < 0 {
            return status::NULL_ARG;
        }
        let props = if props.is_null() { BcsFeathersProps::default() } else { unsafe { *props } };
        let text: &[u8] = if len == 0 { &[] } else { unsafe { core::slice::from_raw_parts(text, len as usize) } };
        let Ok(text) = core::str::from_utf8(text) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, parent, props, text);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;

            crate::state::with_world(|world| {
                if !world.contains_resource::<bevy::feathers::theme::UiTheme>() {
                    return status::INVALID_STATE;
                }
                let parent = if parent == 0 { None } else { Some(Entity::from_bits(parent)) };
                if let Some(parent) = parent
                    && world.get_entity(parent).is_err()
                {
                    return status::NO_ENTITY;
                }
                let Some(entity) = spawn::control(world, kind, parent, &props, text) else {
                    return status::NULL_ARG;
                };
                unsafe { *out = entity.to_bits() };
                status::OK
            })
        }
    })
}

/// Colors a node of C#'s own from the theme by the token `token` names, such as
/// `feathers.window.bg`, as Bevy's `ThemeBackgroundColor`, `ThemeBorderColor`,
/// `InheritableThemeTextColor` and `ThemeTextColor` do for `slot` zero to three. The color follows
/// the theme as it changes.
///
/// # Safety
/// `token` must be valid for `len` bytes of UTF-8.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_theme_color(entity: u64, slot: i32, token: *const u8, len: i32) -> i32 {
    crate::interop::guard(|| {
        if token.is_null() || len <= 0 {
            return status::NULL_ARG;
        }
        let Ok(token) = core::str::from_utf8(unsafe { core::slice::from_raw_parts(token, len as usize) }) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, slot, token);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::theme::{
                InheritableThemeTextColor, ThemeBackgroundColor, ThemeBorderColor, ThemeTextColor, ThemeToken,
            };

            crate::state::with_world(|world| {
                let Ok(mut node) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                let token = ThemeToken::new(token.into());
                match slot {
                    0 => node.insert(ThemeBackgroundColor(token)),
                    1 => node.insert(ThemeBorderColor(token)),
                    2 => node.insert(InheritableThemeTextColor(token)),
                    3 => node.insert(ThemeTextColor(token)),
                    _ => return status::NULL_ARG,
                };
                status::OK
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_props_sit_where_the_managed_side_reads_them() {
        assert_eq!(core::mem::offset_of!(BcsFeathersProps, min), 8);
        assert_eq!(core::mem::size_of::<BcsFeathersProps>(), 16);
    }
}

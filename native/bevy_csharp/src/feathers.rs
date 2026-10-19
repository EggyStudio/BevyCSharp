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
//! the theme's colors reach a node of C#'s own through [`bcs_feathers_theme_color`]. A number
//! input's value, limits and units are Bevy components its reflection does not reach, so each has
//! a call of its own.

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
/// A number input, `FeathersNumberInput`, its caption the letter at its left, such as an axis's.
pub const NUMBER_INPUT: i32 = 10;
/// A virtual keyboard, `VirtualKeyboard`, its caption its keys, a line a row and a tab between
/// two keys.
pub const VIRTUAL_KEYBOARD: i32 = 11;

/// What a control is made with, each kind reading the fields it has and leaving the rest.
///
/// The managed side's `NativeFeathersProps`, field for field.
#[repr(C)]
#[derive(Clone, Copy)]
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
    /// A theme token a control is colored by, a number input's sigil along its left edge, as
    /// UTF-8, or null for the control's own.
    pub token: *const u8,
    /// How many bytes `token` holds.
    pub token_len: i32,
}

impl Default for BcsFeathersProps {
    fn default() -> Self {
        Self { variant: 0, corners: 1, min: 0.0, max: 1.0, token: core::ptr::null(), token_len: 0 }
    }
}

/// A key of a virtual keyboard, its text with where it stands, so the key reported to C# is named
/// by its row and its place in the row, which C# reads back as the text it gave.
#[cfg(feature = "render")]
#[derive(Clone, bevy::reflect::Reflect)]
pub struct VirtualKey {
    /// The row it is on, from the top.
    pub row: u32,
    /// Its place in its row, from the left.
    pub column: u32,
    /// What it says.
    pub text: String,
}

#[cfg(feature = "render")]
impl AsRef<str> for VirtualKey {
    fn as_ref(&self) -> &str {
        &self.text
    }
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
    use bevy::feathers::controls::{FeathersNumberInput, FeathersNumberInputProps, VirtualKeyboard, VirtualKeyboardProps};
    use bevy::feathers::display::{caption, label, label_dim, label_small};
    use bevy::feathers::theme::ThemeToken;
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

    /// A number input's letter, kept for as long as the app runs, since Bevy's props hold it as
    /// a static string. Each text is kept once however many inputs show it.
    fn kept(text: &str) -> &'static str {
        use std::collections::HashSet;
        use std::sync::{Mutex, OnceLock};

        static KEPT: OnceLock<Mutex<HashSet<&'static str>>> = OnceLock::new();
        let mut kept = KEPT.get_or_init(Default::default).lock().unwrap_or_else(|poisoned| poisoned.into_inner());
        if let Some(found) = kept.get(text) {
            return found;
        }
        let leaked: &'static str = Box::leak(text.to_string().into_boxed_str());
        kept.insert(leaked);
        leaked
    }

    /// A keyboard's keys from a line a row and a tab between two keys.
    fn keys(text: &str) -> Vec<Vec<VirtualKey>> {
        text.lines()
            .enumerate()
            .map(|(row, line)| {
                line.split('\t')
                    .enumerate()
                    .map(|(column, key)| VirtualKey { row: row as u32, column: column as u32, text: key.to_string() })
                    .collect()
            })
            .collect()
    }

    pub fn control(
        world: &mut World,
        kind: i32,
        parent: Option<Entity>,
        props: &BcsFeathersProps,
        text: &str,
        token: Option<&str>,
    ) -> Option<Entity> {
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
            NUMBER_INPUT => spawn_under(world, parent, || {
                let mut number = FeathersNumberInputProps::default();
                if let Some(token) = token {
                    number.sigil_color = ThemeToken::new(token.into());
                }
                number.label_text = (!text.is_empty()).then(|| kept(text));
                <FeathersNumberInput as SceneComponent>::scene(number)
            }),
            VIRTUAL_KEYBOARD => spawn_under(world, parent, || {
                <VirtualKeyboard<VirtualKey> as SceneComponent>::scene(VirtualKeyboardProps { keys: keys(text) })
            }),
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
        let token = if props.token.is_null() || props.token_len <= 0 {
            None
        } else {
            match core::str::from_utf8(unsafe { core::slice::from_raw_parts(props.token, props.token_len as usize) }) {
                Ok(token) => Some(token),
                Err(_) => return status::NULL_ARG,
            }
        };
        let text: &[u8] = if len == 0 { &[] } else { unsafe { core::slice::from_raw_parts(text, len as usize) } };
        let Ok(text) = core::str::from_utf8(text) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, parent, props, text, token);
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
                let Some(entity) = spawn::control(world, kind, parent, &props, text, token) else {
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

/// Writes a number input's value, of `kind` zero `f32`, one `f64`, two `i32` and three `i64`,
/// from `value` for the first two and `whole` for the last two. The kind is the one its changes
/// are then reported in.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_feathers_number_set(entity: u64, kind: i32, value: f64, whole: i64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, kind, value, whole);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::NumberInputValue;

            let number = match kind {
                0 => NumberInputValue::F32(value as f32),
                1 => NumberInputValue::F64(value),
                2 => NumberInputValue::I32(whole as i32),
                3 => NumberInputValue::I64(whole),
                _ => return status::NULL_ARG,
            };
            crate::state::with_world(|world| {
                let Ok(mut input) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                input.insert(number);
                status::OK
            })
        }
    })
}

/// Reads a number input's value into `kind`, `value` and `whole`, as
/// [`bcs_feathers_number_set`] writes them, or answers [`status::NOT_PRESENT`] where it holds none.
///
/// # Safety
/// `kind`, `value` and `whole` must each be valid for one write.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_number_get(entity: u64, kind: *mut i32, value: *mut f64, whole: *mut i64) -> i32 {
    crate::interop::guard(|| {
        if kind.is_null() || value.is_null() || whole.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::NumberInputValue;

            crate::state::with_world(|world| {
                let Ok(input) = world.get_entity(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(number) = input.get::<NumberInputValue>() else {
                    return status::NOT_PRESENT;
                };
                let (read, as_float, as_whole) = match *number {
                    NumberInputValue::F32(v) => (0, v as f64, 0),
                    NumberInputValue::F64(v) => (1, v, 0),
                    NumberInputValue::I32(v) => (2, v as f64, v as i64),
                    NumberInputValue::I64(v) => (3, v as f64, v),
                };
                unsafe {
                    *kind = read;
                    *value = as_float;
                    *whole = as_whole;
                }
                status::OK
            })
        }
    })
}

/// Bounds a number input's value, Bevy's `HardLimit` where `hard` is non-zero, which typing and
/// dragging cannot pass, and its `SoftLimit` otherwise, which a drag stays within and typing can
/// pass. The range is of `kind` as [`bcs_feathers_number_set`] numbers it, from `min` to `max` for
/// the floats and `min_whole` to `max_whole` for the integers.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_feathers_number_limit(
    entity: u64,
    hard: i32,
    kind: i32,
    min: f64,
    max: f64,
    min_whole: i64,
    max_whole: i64,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, hard, kind, min, max, min_whole, max_whole);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::{HardLimit, NumberInputRange, SoftLimit};

            let range = match kind {
                0 => NumberInputRange::F32(min as f32..=max as f32),
                1 => NumberInputRange::F64(min..=max),
                2 => NumberInputRange::I32(min_whole as i32..=max_whole as i32),
                3 => NumberInputRange::I64(min_whole..=max_whole),
                _ => return status::NULL_ARG,
            };
            crate::state::with_world(|world| {
                let Ok(mut input) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                if hard != 0 {
                    input.insert(HardLimit(range));
                } else {
                    input.insert(SoftLimit(range));
                }
                status::OK
            })
        }
    })
}

/// Has a number input show its value in units, Bevy's `NumberInputUnits` by the id its registry
/// knows the units under, such as `length_meters`, `time_seconds` or `angle_degrees`, where typing
/// a value takes it in any unit of that dimension and the value is held in the first.
///
/// # Safety
/// `units` must be valid for `len` bytes of UTF-8.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_number_units(entity: u64, units: *const u8, len: i32) -> i32 {
    crate::interop::guard(|| {
        if units.is_null() || len <= 0 {
            return status::NULL_ARG;
        }
        let Ok(units) = core::str::from_utf8(unsafe { core::slice::from_raw_parts(units, len as usize) }) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, units);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::feathers::controls::NumberInputUnits;

            crate::state::with_world(|world| {
                let Ok(mut input) = world.get_entity_mut(Entity::from_bits(entity)) else {
                    return status::NO_ENTITY;
                };
                input.insert(NumberInputUnits(units.into()));
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
        assert_eq!(core::mem::offset_of!(BcsFeathersProps, token), 16);
        assert_eq!(core::mem::offset_of!(BcsFeathersProps, token_len), 24);
        assert_eq!(core::mem::size_of::<BcsFeathersProps>(), 32);
    }
}

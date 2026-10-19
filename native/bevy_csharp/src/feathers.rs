//! Feathers, Bevy's styled widgets, spawned from C# one control at a time.
//!
//! Bevy 0.20 builds each Feathers control as a scene component, a scene made from props that a
//! program writes in Bevy's scene notation, a caption among them being a scene of its own. C# has
//! no scene notation, so each control is spawned here from plain props and a caption's text, through
//! the control's own `SceneComponent::scene`, which holds the component, its marker and everything
//! the notation's `@FeathersButton` would give. The control is spawned under the parent C# names,
//! and what it reports reaches C# as the widgets' events do (`crate::widget`).
//!
//! Where Bevy's props hold a scene or a closure, the bridge takes a plain form of it:
//!
//! - a caption is its text, made Bevy's `caption` scene here
//! - a select's options and a list view's rows are their text, a line each, with the place of the
//!   one chosen, made rows as Bevy's `list_rows_from_strings` makes them
//! - a virtual keyboard's keys are their text, a line a row and a tab between two keys
//! - a number input's letter is its text, kept for as long as the app runs since Bevy holds it as
//!   a static string
//! - a dialog's contents are spawned by C# under the node [`bcs_feathers_contents`] names
//! - a lazy menu, whose popup a closure builds as the menu opens, is not offered, and a menu's
//!   popup is spawned with it instead
//!
//! A control's children are its scene's, so what C# adds under one afterwards follows them, and
//! the theme's colors reach a node of C#'s own through [`bcs_feathers_theme_color`]. A number
//! input's value, limits and units are Bevy components its reflection does not reach, so each has
//! a call of its own.

use crate::interop::status;

mod number;

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
/// A menu, `FeathersMenu`, holding its button and its popup.
pub const MENU: i32 = 12;
/// A menu's button, `FeathersMenuButton`, with its caption and an arrow where `flag` is non-zero.
pub const MENU_BUTTON: i32 = 13;
/// A menu's square button, `FeathersMenuToolButton`.
pub const MENU_TOOL_BUTTON: i32 = 14;
/// A menu's popup, `FeathersMenuPopup`, holding its items.
pub const MENU_POPUP: i32 = 15;
/// A menu's item, `FeathersMenuItem`, with its caption.
pub const MENU_ITEM: i32 = 16;
/// A line between a menu's items, `FeathersMenuDivider`.
pub const MENU_DIVIDER: i32 = 17;
/// A select, `FeathersSelect`, its caption its options, a line each, `selected` the one chosen.
pub const SELECT: i32 = 18;
/// A list view, `FeathersListView`, its caption its rows, a line each, `selected` the one chosen.
pub const LIST_VIEW: i32 = 19;
/// A row of a list view, `FeathersListRow`, with its caption.
pub const LIST_ROW: i32 = 20;
/// A toggle drawn as a chevron, `FeathersDisclosureToggle`.
pub const DISCLOSURE_TOGGLE: i32 = 21;
/// The frame around a text input, `FeathersTextInputContainer`.
pub const TEXT_INPUT_CONTAINER: i32 = 22;
/// A text input, `FeathersTextInput`, `extent` the characters it shows and `count` the most it
/// takes, either zero for no bound.
pub const TEXT_INPUT: i32 = 23;
/// A color swatch, `FeathersColorSwatch`, `value` the percentage of it shown opaque.
pub const COLOR_SWATCH: i32 = 24;
/// A color plane, `FeathersColorPlane`, `count` its two axes in the order of Bevy's enum.
pub const COLOR_PLANE: i32 = 25;
/// A color slider, `FeathersColorSlider`, `value` its value and `count` its channel in the order
/// of Bevy's `ColorChannel`.
pub const COLOR_SLIDER: i32 = 26;
/// A color input, `FeathersColorInput`, `value` the percentage of its swatch shown opaque.
pub const COLOR_INPUT: i32 = 27;
/// An editor's pane, `containers::pane`, and its header, the divider in its header, and its body.
pub const PANE: i32 = 28;
/// See [`PANE`].
pub const PANE_HEADER: i32 = 29;
/// See [`PANE`].
pub const PANE_HEADER_DIVIDER: i32 = 30;
/// See [`PANE`].
pub const PANE_BODY: i32 = 31;
/// A pane within a pane, `containers::subpane`, and its header and body.
pub const SUBPANE: i32 = 32;
/// See [`SUBPANE`].
pub const SUBPANE_HEADER: i32 = 33;
/// See [`SUBPANE`].
pub const SUBPANE_BODY: i32 = 34;
/// A group of properties, `containers::group`, and its header and body.
pub const GROUP: i32 = 35;
/// See [`GROUP`].
pub const GROUP_HEADER: i32 = 36;
/// See [`GROUP`].
pub const GROUP_BODY: i32 = 37;
/// A node that takes the room left in its row, `containers::flex_spacer`.
pub const FLEX_SPACER: i32 = 38;
/// An icon, `display::icon`, its caption the image's path, tinted as text unless `flag` is
/// non-zero.
pub const ICON: i32 = 39;
/// A modal dialog over the window, `FeathersDialog`, `extent` its width, zero for its contents'.
pub const DIALOG: i32 = 40;
/// A dialog that floats over the window and is dragged by its title, `FeathersFloatingDialog`,
/// its caption its title, `extent` its width, and `left` and `top` where it stands.
pub const FLOATING_DIALOG: i32 = 41;
/// A dialog's header, body, footer and close button, `FeathersDialogHeader` and its kin.
pub const DIALOG_HEADER: i32 = 42;
/// See [`DIALOG_HEADER`].
pub const DIALOG_BODY: i32 = 43;
/// See [`DIALOG_HEADER`].
pub const DIALOG_FOOTER: i32 = 44;
/// See [`DIALOG_HEADER`].
pub const DIALOG_CLOSE: i32 = 45;

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
    /// A color slider's value, or the percentage of a swatch or a color input shown opaque.
    pub value: f32,
    /// A text input's width in characters, a dialog's width in pixels, or a swatch's corner radius,
    /// zero for the control's own.
    pub extent: f32,
    /// A select's rows shown at once, a text input's most characters, a color slider's channel or a
    /// color plane's axes, zero for the control's own where it has one.
    pub count: i32,
    /// The row chosen among a select's or a list view's, or -1 for none.
    pub selected: i32,
    /// A menu button's arrow, or an icon left untinted, where non-zero.
    pub flag: u32,
    /// A floating dialog's left edge, in pixels.
    pub left: f32,
    /// A floating dialog's top edge, in pixels.
    pub top: f32,
}

impl Default for BcsFeathersProps {
    fn default() -> Self {
        Self {
            variant: 0,
            corners: 1,
            min: 0.0,
            max: 1.0,
            token: core::ptr::null(),
            token_len: 0,
            value: 0.0,
            extent: 0.0,
            count: 0,
            selected: -1,
            flag: 1,
            left: 120.0,
            top: 120.0,
        }
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
    use bevy::feathers::containers::{
        flex_spacer, group, group_body, group_header, pane, pane_body, pane_header, pane_header_divider, subpane,
        subpane_body, subpane_header,
    };
    use bevy::feathers::controls::{
        list_rows_from_strings, ColorChannel, FeathersColorInput, FeathersColorInputProps, FeathersColorPlane,
        FeathersColorSlider, FeathersColorSliderProps, FeathersColorSwatch, FeathersColorSwatchProps, FeathersDialog,
        FeathersDialogBody, FeathersDialogClose, FeathersDialogFooter, FeathersDialogHeader, FeathersDialogProps,
        FeathersDisclosureToggle, FeathersFloatingDialog, FeathersFloatingDialogProps, FeathersListRow,
        FeathersListView, FeathersListViewProps, FeathersMenu, FeathersMenuButton, FeathersMenuButtonProps,
        FeathersMenuDivider, FeathersMenuItem, FeathersMenuItemProps, FeathersMenuPopup, FeathersMenuToolButton,
        FeathersNumberInput, FeathersNumberInputProps, FeathersSelect, FeathersSelectProps, FeathersTextInput,
        FeathersTextInputContainer, FeathersTextInputProps, VirtualKeyboard, VirtualKeyboardProps,
    };
    use bevy::feathers::display::{icon, icon_untinted};
    use bevy::ui::{px, Val};
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

    fn channel(place: i32) -> ColorChannel {
        match place {
            0 => ColorChannel::Red,
            1 => ColorChannel::Green,
            2 => ColorChannel::Blue,
            3 => ColorChannel::HslHue,
            4 => ColorChannel::HslSaturation,
            5 => ColorChannel::HslLightness,
            7 => ColorChannel::OkhslHue,
            8 => ColorChannel::OkhslSaturation,
            9 => ColorChannel::OkhslLightness,
            _ => ColorChannel::Alpha,
        }
    }

    fn plane(place: i32) -> FeathersColorPlane {
        match place {
            1 => FeathersColorPlane::RedBlue,
            2 => FeathersColorPlane::GreenBlue,
            3 => FeathersColorPlane::HueSaturation,
            4 => FeathersColorPlane::HueLightness,
            5 => FeathersColorPlane::OkhslHueSaturation,
            6 => FeathersColorPlane::OkhslHueLightness,
            _ => FeathersColorPlane::RedGreen,
        }
    }

    /// A width in pixels, or the content's where it is zero.
    fn width(extent: f32) -> Val {
        if extent > 0.0 { px(extent) } else { Val::Auto }
    }

    /// The rows of a select or a list view from a line each, `selected` the one marked chosen.
    fn rows(text: &str, selected: i32) -> Box<dyn SceneList> {
        list_rows_from_strings(text.lines(), usize::try_from(selected).ok())
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
            MENU => spawn_under(world, parent, || <FeathersMenu as SceneComponent>::scene(())),
            MENU_BUTTON => spawn_under(world, parent, || {
                <FeathersMenuButton as SceneComponent>::scene(FeathersMenuButtonProps {
                    caption: captioned(text),
                    corners: corners(props.corners),
                    arrow: props.flag != 0,
                })
            }),
            MENU_TOOL_BUTTON => spawn_under(world, parent, || {
                <FeathersMenuToolButton as SceneComponent>::scene(FeathersMenuButtonProps {
                    caption: captioned(text),
                    corners: corners(props.corners),
                    arrow: props.flag != 0,
                })
            }),
            MENU_POPUP => spawn_under(world, parent, || <FeathersMenuPopup as SceneComponent>::scene(())),
            MENU_ITEM => spawn_under(world, parent, || {
                <FeathersMenuItem as SceneComponent>::scene(FeathersMenuItemProps { caption: captioned(text) })
            }),
            MENU_DIVIDER => spawn_under(world, parent, || <FeathersMenuDivider as SceneComponent>::scene(())),
            SELECT => spawn_under(world, parent, || {
                <FeathersSelect as SceneComponent>::scene(FeathersSelectProps {
                    options: rows(text, props.selected),
                    corners: corners(props.corners),
                    max_visible: if props.count > 0 { props.count as usize } else { 8 },
                })
            }),
            LIST_VIEW => spawn_under(world, parent, || {
                <FeathersListView as SceneComponent>::scene(FeathersListViewProps { rows: rows(text, props.selected) })
            }),
            LIST_ROW => {
                let row = spawn_under(world, parent, || <FeathersListRow as SceneComponent>::scene(()));
                if !text.is_empty() {
                    spawn_under(world, Some(row), || caption(text.to_string()));
                }
                row
            }
            DISCLOSURE_TOGGLE => spawn_under(world, parent, || <FeathersDisclosureToggle as SceneComponent>::scene(())),
            TEXT_INPUT_CONTAINER => {
                spawn_under(world, parent, || <FeathersTextInputContainer as SceneComponent>::scene(()))
            }
            TEXT_INPUT => spawn_under(world, parent, || {
                <FeathersTextInput as SceneComponent>::scene(FeathersTextInputProps {
                    visible_width: (props.extent > 0.0).then_some(props.extent),
                    max_characters: usize::try_from(props.count).ok().filter(|count| *count > 0),
                })
            }),
            COLOR_SWATCH => spawn_under(world, parent, || {
                <FeathersColorSwatch as SceneComponent>::scene(FeathersColorSwatchProps {
                    opaque_color_percentage: props.value,
                    corners: corners(props.corners),
                    border_radius: if props.extent > 0.0 { props.extent } else { 5.0 },
                })
            }),
            // Bevy's scene gives the plane its first axes, and its notation patches the variant
            // over it, as this does once the scene is applied.
            COLOR_PLANE => {
                let entity = spawn_under(world, parent, || <FeathersColorPlane as SceneComponent>::scene(()));
                world.entity_mut(entity).insert(plane(props.count));
                entity
            }
            COLOR_SLIDER => spawn_under(world, parent, || {
                <FeathersColorSlider as SceneComponent>::scene(FeathersColorSliderProps {
                    value: props.value,
                    channel: channel(props.count),
                })
            }),
            COLOR_INPUT => spawn_under(world, parent, || {
                <FeathersColorInput as SceneComponent>::scene(FeathersColorInputProps {
                    opaque_color_percentage: props.value,
                })
            }),
            PANE => spawn_under(world, parent, pane),
            PANE_HEADER => spawn_under(world, parent, pane_header),
            PANE_HEADER_DIVIDER => spawn_under(world, parent, pane_header_divider),
            PANE_BODY => spawn_under(world, parent, pane_body),
            SUBPANE => spawn_under(world, parent, subpane),
            SUBPANE_HEADER => spawn_under(world, parent, subpane_header),
            SUBPANE_BODY => spawn_under(world, parent, subpane_body),
            GROUP => spawn_under(world, parent, group),
            GROUP_HEADER => spawn_under(world, parent, group_header),
            GROUP_BODY => spawn_under(world, parent, group_body),
            FLEX_SPACER => spawn_under(world, parent, flex_spacer),
            ICON => {
                let path = kept(text);
                if props.flag != 0 {
                    spawn_under(world, parent, || icon_untinted(path))
                } else {
                    spawn_under(world, parent, || icon(path))
                }
            }
            DIALOG => spawn_under(world, parent, || {
                <FeathersDialog as SceneComponent>::scene(FeathersDialogProps { contents: Box::new(bsn_list! {}), width: width(props.extent) })
            }),
            FLOATING_DIALOG => spawn_under(world, parent, || {
                <FeathersFloatingDialog as SceneComponent>::scene(FeathersFloatingDialogProps {
                    title: text.to_string(),
                    contents: Box::new(bsn_list! {}),
                    width: width(props.extent),
                    left: px(props.left),
                    top: px(props.top),
                })
            }),
            DIALOG_HEADER => spawn_under(world, parent, || <FeathersDialogHeader as SceneComponent>::scene(())),
            DIALOG_BODY => spawn_under(world, parent, || <FeathersDialogBody as SceneComponent>::scene(())),
            DIALOG_FOOTER => spawn_under(world, parent, || <FeathersDialogFooter as SceneComponent>::scene(())),
            DIALOG_CLOSE => spawn_under(world, parent, || <FeathersDialogClose as SceneComponent>::scene(())),
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

/// Writes into `out` the node a control's contents go under, where Bevy's props would hold them as
/// a scene. A modal dialog's panel, a floating dialog's body, and the scrolling part of a list
/// view or of a select's list, the first such node below the control, so what C# spawns there is
/// laid out as Bevy's props would have it. Answers [`status::NOT_PRESENT`] for a control with no
/// such node.
///
/// # Safety
/// `out` must be valid for one write.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_feathers_contents(entity: u64, out: *mut u64) -> i32 {
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
            use bevy::ecs::entity::Entity;
            use bevy::ecs::hierarchy::Children;
            use bevy::ecs::world::EntityRef;
            use bevy::feathers::controls::{FeathersDialog, FeathersDialogBody, FeathersFloatingDialog};
            use bevy::ui_widgets::{ModalDialog, ScrollArea};
            use std::collections::VecDeque;

            crate::state::with_world(|world| {
                let root = Entity::from_bits(entity);
                let Ok(control) = world.get_entity(root) else {
                    return status::NO_ENTITY;
                };
                let wanted: fn(&EntityRef) -> bool = if control.contains::<FeathersDialog>() {
                    |node| node.contains::<ModalDialog>()
                } else if control.contains::<FeathersFloatingDialog>() {
                    |node| node.contains::<FeathersDialogBody>()
                } else {
                    |node| node.contains::<ScrollArea>()
                };

                // Breadth first, so the nearest such node is the one taken.
                let mut waiting = VecDeque::from([root]);
                while let Some(next) = waiting.pop_front() {
                    let Ok(node) = world.get_entity(next) else { continue };
                    if next != root && wanted(&node) {
                        unsafe { *out = next.to_bits() };
                        return status::OK;
                    }
                    if let Some(children) = node.get::<Children>() {
                        waiting.extend(children.iter());
                    }
                }
                status::NOT_PRESENT
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
        assert_eq!(core::mem::offset_of!(BcsFeathersProps, value), 28);
        assert_eq!(core::mem::offset_of!(BcsFeathersProps, count), 36);
        assert_eq!(core::mem::offset_of!(BcsFeathersProps, top), 52);
        assert_eq!(core::mem::size_of::<BcsFeathersProps>(), 56);
    }
}

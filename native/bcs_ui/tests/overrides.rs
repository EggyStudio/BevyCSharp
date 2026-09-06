//! What a program decides about an element, against what the stylesheet says about it.
//!
//! The stylesheet is reapplied whenever anything restyles a widget, and it rewrites the whole node
//! from the rules it matched. So a panel that had hidden a row, placed a flyout or painted a patch
//! of color would find its decision quietly undone a frame later. The override is what a program
//! writes instead, and it is applied after the sheet; these check that it is applied at all, that
//! it only touches what was decided, and that a color survives alongside the rest.

use bcs_ui::styles::StyleOverride;
use bevy::color::Color;
use bevy::ui::{Display, Node, PositionType, Val};

#[test]
fn nothing_decided_changes_nothing() {
    let over = StyleOverride::default();
    assert!(over.is_empty());

    let mut node = Node {
        width: Val::Px(120.0),
        display: Display::Flex,
        ..Default::default()
    };

    over.apply(&mut node);

    assert_eq!(node.width, Val::Px(120.0));
    assert_eq!(node.display, Display::Flex);
}

#[test]
fn what_was_decided_wins_and_the_rest_is_left_alone() {
    let over = StyleOverride {
        display: Some(Display::None),
        left: Some(Val::Px(40.0)),
        flex_grow: Some(2.0),
        ..Default::default()
    };

    assert!(!over.is_empty());

    let mut node = Node {
        display: Display::Flex,
        position_type: PositionType::Relative,
        left: Val::Px(0.0),
        width: Val::Px(80.0),
        flex_grow: 1.0,
        ..Default::default()
    };

    over.apply(&mut node);

    assert_eq!(node.display, Display::None);
    assert_eq!(node.left, Val::Px(40.0));
    assert_eq!(node.flex_grow, 2.0);

    // Untouched: the sheet is in charge of everything nobody decided.
    assert_eq!(node.width, Val::Px(80.0));
    assert_eq!(node.position_type, PositionType::Relative);
}

#[test]
fn a_color_is_decided_apart_from_the_node() {
    let painted = Color::srgba(0.85, 0.35, 0.15, 1.0);

    let over = StyleOverride {
        background_color: Some(painted),
        ..Default::default()
    };

    // Not part of the node: a color lives on its own component, and the sheet writes it from a
    // different place. Both are applied after the sheet, which is the point of the override.
    assert!(!over.is_empty());
    assert_eq!(over.color(), Some(painted));

    let mut node = Node::default();
    let before = node.clone();
    over.apply(&mut node);

    assert_eq!(node.width, before.width);
    assert_eq!(node.display, before.display);
}

#[test]
fn deciding_nothing_about_a_color_leaves_the_sheet_in_charge() {
    let over = StyleOverride {
        width: Some(Val::Px(10.0)),
        ..Default::default()
    };

    assert_eq!(over.color(), None);
}

//! What the stylesheet parser and the selector matcher make of a rule.
//!
//! These are the questions the editor kept having to answer by experiment: does a rule naming two
//! classes match an element carrying both, and does a rule reach an element that carries the class
//! it names among others. Answering them here is cheaper than answering them on screen.

use bcs_ui::services::css_service::matches_css_selector_token;
use bcs_ui::styles::parser::load_css;
use bcs_ui::styles::{CssClass, CssID, TagName};

#[test]
fn a_rule_naming_two_classes_is_kept_as_written() {
    let parsed = load_css(".panel.stats { width: 10px; }");

    let keys: Vec<&String> = parsed.styles.keys().collect();
    assert!(
        keys.iter().any(|key| key.contains(".panel") && key.contains(".stats")),
        "the two classes survive parsing, got {keys:?}"
    );
}

#[test]
fn an_element_matches_a_rule_naming_two_of_its_classes() {
    let classes = CssClass(vec![String::from("panel"), String::from("stats")]);

    assert!(matches_css_selector_token(".panel.stats", None, Some(&classes), None));
    assert!(matches_css_selector_token(".stats", None, Some(&classes), None));
    assert!(matches_css_selector_token(".panel", None, Some(&classes), None));
    assert!(!matches_css_selector_token(".panel.menu", None, Some(&classes), None));
}

#[test]
fn a_tag_and_an_id_narrow_a_rule_the_same_way() {
    let id = CssID(String::from("data"));
    let tag = TagName(String::from("div"));
    let classes = CssClass(vec![String::from("column")]);

    assert!(matches_css_selector_token("div#data", Some(&id), None, Some(&tag)));
    assert!(matches_css_selector_token("div.column", None, Some(&classes), Some(&tag)));
    assert!(!matches_css_selector_token("p.column", None, Some(&classes), Some(&tag)));
}

/// What a rule says about how a box arranges what is in it.
///
/// `align-content` was parsed nowhere and applied nowhere, so a wrapping box spread its lines
/// down whatever height it had and nothing could say otherwise. These are the properties an
/// asset grid needs to hold its rows together.
#[test]
fn the_alignment_properties_are_read() {
    let parsed = load_css(
        ".grid { align-content: flex-start; align-self: center; align-items: stretch; }",
    );

    let pair = parsed
        .styles
        .get(".grid")
        .expect("the rule is there");

    let style = &pair.normal;

    assert_eq!(style.align_content, Some(bevy::ui::AlignContent::FlexStart));
    assert_eq!(style.align_self, Some(bevy::ui::AlignSelf::Center));
    assert_eq!(style.align_items, Some(bevy::ui::AlignItems::Stretch));
}

#[test]
fn a_rule_naming_two_classes_beats_one_naming_either() {
    use bcs_ui::services::style_service::selector_weight;

    // What settles the cascade between two rules that both match. Counting only the first name in
    // a compound made these equal, and equal rules are settled by whichever the map hands over
    // first: a color that is right on some frames and wrong on others.
    assert!(selector_weight(".field-note.warn") > selector_weight(".field-note"));
    assert!(selector_weight("#main") > selector_weight(".panel.stats"));
    assert!(selector_weight("div.panel") > selector_weight("div"));
    assert_eq!(selector_weight("div"), 1);
    assert_eq!(selector_weight(".panel"), 10);
    assert_eq!(selector_weight(".panel.stats"), 20);
    assert_eq!(selector_weight("div.panel.stats"), 21);
    assert_eq!(selector_weight("*"), 0);
}

#[test]
fn a_descendant_selector_counts_every_step() {
    use bcs_ui::services::style_service::selector_weight;

    assert_eq!(selector_weight(".column .field-name"), 20);
    assert_eq!(selector_weight(".column > .field-name"), 20);
}

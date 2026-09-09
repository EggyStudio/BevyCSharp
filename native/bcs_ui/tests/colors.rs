//! What a color written in a modern stylesheet comes out as.
//!
//! Every syntax CSS has for a color lands in one parser and converts to sRGB, so a rule written
//! with `oklch()` or `hsl()` is a rule that works rather than one that silently does nothing.
//! These pin the conversions a stylesheet actually depends on.

use bcs_ui::styles::parser::convert_to_color;
use bevy::color::{Color, Srgba};

/// What the color came out as, in sRGB bytes, so a comparison reads as a color.
fn bytes(value: &str) -> Option<(u8, u8, u8, u8)> {
    let color = convert_to_color(value.to_string())?;
    let Srgba {
        red,
        green,
        blue,
        alpha,
    } = Srgba::from(color);

    Some((
        (red * 255.0).round() as u8,
        (green * 255.0).round() as u8,
        (blue * 255.0).round() as u8,
        (alpha * 255.0).round() as u8,
    ))
}

#[test]
fn the_old_syntaxes_still_read() {
    assert_eq!(bytes("#ff0000"), Some((255, 0, 0, 255)));
    assert_eq!(bytes("rgb(0, 128, 255)"), Some((0, 128, 255, 255)));
    assert_eq!(bytes("red"), Some((255, 0, 0, 255)));
    assert_eq!(bytes("transparent"), None);
}

#[test]
fn the_modern_syntaxes_read_too() {
    // Space separated, with the alpha after a slash, which is how a stylesheet written today
    // spells what `rgba()` used to.
    assert_eq!(bytes("rgb(0 128 255 / 50%)"), Some((0, 128, 255, 128)));

    // Named spaces. `hsl` is everywhere, and `oklch` is what a palette generated today is written
    // in: Tailwind's entire theme is oklch and nothing else.
    assert_eq!(bytes("hsl(0, 100%, 50%)"), Some((255, 0, 0, 255)));

    let (red, green, blue, alpha) = bytes("oklch(62.8% 0.258 29.23)").expect("oklch reads");

    assert!(red > 230, "oklch red is red, got {red}");
    assert!(green < 40 && blue < 40, "and not much else, got {green} {blue}");
    assert_eq!(alpha, 255);

    // A color in an explicit space, converted to the one the screen is in.
    let (red, green, blue, _) = bytes("color(display-p3 1 0 0)").expect("display-p3 reads");

    assert!(red > 230 && green < 90 && blue < 90, "got {red} {green} {blue}");
}

#[test]
fn a_color_nobody_can_read_is_nothing_rather_than_black() {
    // The distinction matters: a property left alone keeps whatever it had, where a black is a
    // decision nobody made.
    assert_eq!(bytes("not-a-color"), None);
    assert_eq!(bytes("rgb(banana)"), None);
}

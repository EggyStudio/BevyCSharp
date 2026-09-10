//! What the editor's own stylesheet lays out.
//!
//! The point of these is not that Stylo works: it styles Firefox. It is that the editor's page is
//! ordinary markup and ordinary CSS, that the shell it describes measures the way it reads, and
//! that everything a modern stylesheet is written with works here.

use bcs_dom::Interface;

/// The editor's stylesheet, read from the assets it ships with.
fn editor_css() -> String {
    std::fs::read_to_string("../../BevyCSharp.Editor/assets/ui/editor.css")
        .expect("the editor's stylesheet ships with the editor")
}

/// The editor's shell, as the page ships it.
const SHELL: &str = r#"
    <div class="app">
      <header class="toolbar" id="toolbar"></header>
      <div class="middle">
        <aside class="panel" id="hierarchy-panel"><header>World</header>
          <div class="body" id="hierarchy"></div></aside>
        <section class="viewport" id="viewport"></section>
        <aside class="panel" id="inspector-panel"><header>Details</header>
          <div class="body" id="inspector"></div></aside>
      </div>
      <footer class="status" id="status"></footer>
    </div>
"#;

/// A document using the editor's own classes.
fn page(body: &str) -> String {
    format!(
        "<html><head><style>{}</style></head><body>{}</body></html>",
        editor_css(),
        body
    )
}

#[test]
fn the_editors_stylesheet_lays_the_shell_out() {
    let document = Interface::open(&page(SHELL), 1600, 900);

    let box_of = |id: &str| {
        let node = document.element(id).expect("the part is in the page");
        document.rect(node).expect("and it was laid out")
    };

    let (_, _, hierarchy, _) = box_of("hierarchy-panel");
    let (left, _, viewport, _) = box_of("viewport");
    let (_, _, inspector, _) = box_of("inspector-panel");

    // The two panels are the widths the stylesheet gives them and the viewport is the rest, which
    // is what a grid of `260px 1fr 320px` means. Nothing worked any of that out on the way in.
    assert_eq!(hierarchy, 260.0);
    assert_eq!(inspector, 320.0);
    assert_eq!(viewport, 1600.0 - 260.0 - 320.0);
    assert_eq!(left, 260.0);
}

#[test]
fn the_viewport_is_a_hole_the_scene_shows_through() {
    let document = Interface::open(&page(SHELL), 1600, 900);

    let (_, top, _, height) = {
        let node = document.element("viewport").expect("the viewport is in the page");
        document.rect(node).expect("and it was laid out")
    };

    // Between the toolbar and the status strip, both of which are as tall as the stylesheet says.
    assert_eq!(top, 34.0);
    assert_eq!(height, 900.0 - 34.0 - 22.0);
}

#[test]
fn the_rules_the_old_reader_could_not_match_now_apply() {
    // Everything the hand-written matcher refused, in the editor's own idiom.
    let css = r#"
        @layer base, app;
        @layer app { .row:where(.picked) { width: 120px; } }
        @layer base { .row:where(.picked) { width: 10px; } }
        .row + .row { height: 40px; }
        [data-open="yes"] { width: 77px; }
    "#;

    let document = Interface::open(
        &format!(
            "<html><head><style>{css}</style></head><body>\
             <div class=\"row picked\" id=\"a\"></div>\
             <div class=\"row\" id=\"b\"></div>\
             <div data-open=\"yes\" id=\"c\"></div>\
             </body></html>"
        ),
        800,
        600,
    );

    let width = |id: &str| {
        let node = document.element(id).expect("the element is there");
        document.rect(node).expect("and laid out").2
    };

    let height = |id: &str| {
        let node = document.element(id).expect("the element is there");
        document.rect(node).expect("and laid out").3
    };

    // A layer written earlier in the file wins over one written later, which is what layers are
    // for and what no amount of specificity juggling can express.
    assert_eq!(width("a"), 120.0, "the app layer wins over base");

    // The sibling combinator, which the old reader had no notion of at all.
    assert_eq!(height("b"), 40.0, "the second row is the tall one");

    // And an attribute selector, which is how every component library says what state a thing is
    // in.
    assert_eq!(width("c"), 77.0);
}

#[test]
fn a_document_paints() {
    let document = Interface::open(&page(SHELL), 320, 200);

    let mut pixels = vec![0u8; document.pixels()];
    document.paint(&mut pixels);

    // Something was drawn: the panel's own grey, somewhere inside it.
    let opaque = pixels.chunks_exact(4).filter(|pixel| pixel[3] > 0).count();

    assert!(opaque > 1000, "the panel covers some of the page, got {opaque}");
}

#[test]
fn a_document_answers_a_click_on_the_element_that_was_clicked() {
    use bcs_dom::{Button, Report};

    let mut document = Interface::open(
        r#"<html><head><style>
             #a { width: 100px; height: 40px; }
             #b { width: 100px; height: 40px; }
           </style></head>
           <body><div id="a"></div><div id="b"></div></body></html>"#,
        400,
        300,
    );

    let b = document.element("b").expect("the second box is there");

    // Where the second box ended up, rather than a guess at where it should be: the layout engine
    // is the authority on that, and asking it is what makes a test like this mean anything.
    let (x, y, width, height) = document.rect(b).expect("and laid out");

    document.moved(x + (width / 2.0), y + (height / 2.0));
    document.pressed(Button::Left);
    document.released(Button::Left);

    let reports = document.drain();

    assert!(
        reports.contains(&Report::Click(b)),
        "the click landed on the box under the pointer, got {reports:?}"
    );
}

#[test]
fn changing_the_document_changes_the_layout() {
    let mut document = Interface::open(
        r#"<html><head><style>
             .box { width: 50px; height: 10px; }
             .box.big { width: 200px; }
           </style></head><body><div class="box" id="x">hello</div></body></html>"#,
        400,
        300,
    );

    let node = document.element("x").expect("the box is there");
    assert_eq!(document.rect(node).unwrap().2, 50.0);

    // A class written while the program runs, which is how a program says what state something is
    // in and lets the stylesheet decide what that looks like.
    document.set_attribute(node, "class", "box big");
    document.resolve();

    assert_eq!(document.rect(node).unwrap().2, 200.0);

    document.set_text(node, "something else");
    document.resolve();

    assert_eq!(document.text(node), "something else");
}

#[test]
fn a_panel_is_a_piece_of_markup_put_into_the_page() {
    let mut document = Interface::open(
        r#"<html><head><style>
             .panel { width: 200px; height: 100px; }
             .row { height: 20px; }
           </style></head><body><div id="shell"></div></body></html>"#,
        800,
        600,
    );

    let shell = document.element("shell").expect("the shell is there");

    // What used to be a document of its own, with a layer, a placement and a rebuild of every
    // other document when it opened. It is a subtree.
    document.set_html(
        shell,
        r#"<div class="panel" id="p"><div class="row" id="r">Hierarchy</div></div>"#,
    );

    document.resolve();

    let panel = document.element("p").expect("the panel arrived");
    let row = document.element("r").expect("and its row");

    assert_eq!(document.rect(panel).unwrap().2, 200.0);
    assert_eq!(document.rect(row).unwrap().3, 20.0);
    assert_eq!(document.text(row), "Hierarchy");

    // And it can be taken out again, which is what closing a panel is.
    document.set_html(shell, "");
    document.resolve();

    assert!(document.element("p").is_none(), "the panel went away");
}

#[test]
fn a_field_holds_what_was_typed_into_it() {
    let mut document = Interface::open(
        r#"<html><body><input id="name" type="text" value="Cube" /></body></html>"#,
        400,
        200,
    );

    let field = document.element("name").expect("the field is there");

    // A field keeps its value in an editor of its own rather than in its children, so reading it
    // the way an ordinary element is read answers nothing.
    assert_eq!(document.value(field).as_deref(), Some("Cube"));

    document.set_value(field, "Crate");
    document.resolve();

    assert_eq!(document.value(field).as_deref(), Some("Crate"));

    // And it can be drawn afterwards. A field's text has a layout of its own, and text put into it
    // without building that layout leaves the painter with nothing to draw and no way to say so.
    let mut pixels = vec![0u8; document.pixels()];
    document.paint(&mut pixels);
}

#[test]
fn a_field_that_was_hidden_draws_when_it_is_shown() {
    // What the console does: it is in the page all along, out of the way, and a key brings it in.
    let mut document = Interface::open(
        r#"<html><head><style>
             .hidden { display: none; }
             .sheet { position: absolute; top: 0; left: 0; right: 0; height: 45vh; }
             #entry { height: 26px; font-family: var(--mono); font-size: 12px; }
             :root { --mono: "DejaVu Sans Mono", ui-monospace, monospace; }
           </style></head>
           <body><div class="sheet hidden" id="sheet">
             <div id="lines"></div>
             <input id="entry" type="text" />
           </div></body></html>"#,
        400,
        200,
    );

    let sheet = document.element("sheet").expect("the sheet is there");
    let entry = document.element("entry").expect("the field came with it");

    document.set_attribute(sheet, "class", "sheet");

    // Focused as it appears, which is what a console does: it is no use if it has to be clicked
    // before it can be typed in. A focused field draws a caret, and a caret needs the layout that
    // a field which was never on screen has not built.
    document.focus(entry);
    document.resolve();

    let mut pixels = vec![0u8; document.pixels()];
    document.paint(&mut pixels);

    assert_eq!(document.value(entry).as_deref(), Some(" "));
}

#[test]
fn a_page_reads_pictures_only_from_the_assets_it_was_opened_with() {
    let assets = std::path::Path::new("../../BevyCSharp.Editor/assets");

    let mut document = Interface::open_from(
        r#"<html><body><img id="mark" src="icons/ui/camera.png" /></body></html>"#,
        400,
        200,
        Some(assets),
    );

    // A fetch answers on its own, so what arrived is taken up the next time the page is laid out.
    document.touch();
    document.resolve();
    document.touch();
    document.resolve();

    let picture = document.element("mark").expect("the picture is in the page");
    let (_, _, width, height) = document.rect(picture).expect("and it was laid out");

    // A picture that was read has the size it was drawn at; one that was not is a box of nothing.
    assert!(
        width > 1.0 && height > 1.0,
        "the picture was read and laid out, got {width}x{height}"
    );
}

#[test]
fn a_click_lands_on_the_innermost_thing_and_walks_up_to_a_name() {
    let mut document = Interface::open(
        r#"<html><head><style>
             .button { width: 60px; height: 24px; }
             .icon { width: 14px; height: 14px; }
           </style></head>
           <body><div class="button" id="menu"><span class="icon" id="mark">x</span></div></body></html>"#,
        200,
        100,
    );

    document.resolve();

    let hit = document.hit(8.0, 8.0).expect("something is under the pointer");
    let mark = document.element("mark").expect("the picture is in the page");

    // The picture, not the button: a click reports the innermost thing under the pointer, which is
    // why what was clicked has to be walked up to.
    assert_eq!(hit, mark);

    let mut walked = hit;
    let mut named = None;

    for _ in 0..8 {
        if let Some(id) = document.attribute(walked, "id")
            && id == "menu"
        {
            named = Some(walked);
            break;
        }

        match document.parent(walked) {
            Some(above) => walked = above,
            None => break,
        }
    }

    assert_eq!(named, document.element("menu"), "the button is above the picture");
}

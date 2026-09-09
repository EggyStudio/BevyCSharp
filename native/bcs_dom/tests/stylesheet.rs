//! What a real style system makes of the editor's own stylesheet.
//!
//! The point of these is not that Stylo works: it styles Firefox. It is that the editor's markup
//! and stylesheet, written against a hand-rolled subset of CSS, are ordinary CSS that a real
//! engine reads the same way, and that the things the old reader could not do now happen.

use bcs_dom::Interface;

/// The editor's stylesheet, read from the assets it ships with.
fn editor_css() -> String {
    std::fs::read_to_string("../../BevyCSharp.Editor/assets/panels/editor.css")
        .expect("the editor's stylesheet is where it has always been")
}

/// A document using the editor's own classes.
fn page(body: &str) -> String {
    format!(
        "<html><head><style>{}</style></head><body>{}</body></html>",
        editor_css(),
        body
    )
}

#[test]
fn the_editors_stylesheet_lays_a_panel_out() {
    let document = Interface::open(
        &page(r#"<div class="column" id="panel"><p class="title" id="t">World</p></div>"#),
        1600,
        900,
    );

    let panel = document.element("panel").expect("the panel is in the document");
    let (_, _, width, _) = document.rect(panel).expect("and it was laid out");

    // Three hundred, and then the padding and the borders on top of it, because that is what a
    // width means in CSS: the box is the content, and what surrounds it is extra.
    //
    // The old engine measured the other way round, since the interface it drew into takes a width
    // as the whole box. Every rule in this stylesheet was written against that, which is the first
    // thing a real style system finds. What fixes it is one line saying `box-sizing: border-box`,
    // which is the same line every stylesheet on the web starts with and the first thing in
    // Tailwind's own reset.
    assert!(
        (width - 318.0).abs() < 1.0,
        "a column is its width plus its padding and border, got {width}"
    );
}

#[test]
fn one_line_puts_the_old_measurements_back() {
    let markup = format!(
        "<html><head><style>* {{ box-sizing: border-box; }}{}</style></head>{}</html>",
        editor_css(),
        r#"<body><div class="column" id="panel"></div></body>"#
    );

    let document = Interface::open(&markup, 1600, 900);

    let panel = document.element("panel").expect("the panel is in the document");
    let (_, _, width, _) = document.rect(panel).expect("and it was laid out");

    assert!(
        (width - 300.0).abs() < 1.0,
        "with border-box the width is the whole box again, got {width}"
    );
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
    let document = Interface::open(
        &page(r#"<div class="column" id="panel"><p class="title">World</p></div>"#),
        320,
        200,
    );

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

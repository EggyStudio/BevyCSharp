//! An interface document: the tree, its styles, its layout, its events and its pixels.
//!
//! Nothing here is written by us that somebody else has written better. [Stylo] resolves the
//! cascade, [Taffy] lays the boxes out, [Parley] shapes the text, and [Blitz] holds the three
//! together as a document. What is written here is only what is ours to write: opening a document,
//! asking it questions, changing it, feeding it what the engine's window reports, and handing its
//! pixels back.
//!
//! It replaces a stylesheet reader written by hand, which matched selectors as text and colors as
//! text. That is why a rule naming two classes weighed the same as one naming either, why `oklch`
//! was not a color, and why `@layer`, `:where`, `+` and `[attr]` did nothing at all. None of those
//! were decisions anybody made: they are what a hand-written subset of CSS looks like from the
//! inside, and no amount of adding to it would have reached the whole language.
//!
//! [Stylo]: https://github.com/servo/stylo
//! [Taffy]: https://github.com/DioxusLabs/taffy
//! [Parley]: https://github.com/linebender/parley
//! [Blitz]: https://github.com/DioxusLabs/blitz

use anyrender::ImageRenderer;
use anyrender_vello_cpu::VelloCpuImageRenderer;
use blitz_dom::{BaseDocument, DocumentConfig, DocumentMutator, EventDriver, EventHandler};
use blitz_html::HtmlDocument;
use blitz_traits::events::{
    BlitzKeyEvent, BlitzMouseButtonEvent, DomEvent, DomEventData, EventState, MouseEventButton,
    MouseEventButtons, UiEvent,
};

// Re-exported, because a caller sending a key has to name one and there is no reason for a second
// spelling of what a key is.
pub use blitz_traits::events::KeyState;
pub use keyboard_types::{Code, Key, Location, Modifiers};
use blitz_traits::shell::{ColorScheme, Viewport};

/// What a document reports back: something happened, and to which element.
///
/// The names are the ones every interface has used since the browser: a click, a value that
/// changed, focus arriving. What the host does with them is its business.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Report {
    /// The element was clicked.
    Click(usize),

    /// The element's value changed, which is what a box being typed in reports.
    Input(usize),

    /// The element took the keyboard.
    Focus(usize),

    /// A key was pressed while the element held the keyboard.
    ///
    /// Enter and Escape, which is what a field being finished with and a field being abandoned
    /// are. Everything else a key does inside a field the document does for itself.
    Key(usize, Ending),
}

/// How somebody finished with a field.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Ending {
    /// Enter: what was typed is meant.
    Accepted,

    /// Escape: it is not.
    Abandoned,
}

/// Which mouse button, in the words the engine's own input uses.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Button {
    /// The one that presses things.
    Left,

    /// The one that asks what else there is.
    Right,

    /// The one in the middle.
    Middle,
}

impl Button {
    /// The same button, as the document names it.
    fn named(self) -> MouseEventButton {
        match self {
            Self::Left => MouseEventButton::Main,
            Self::Right => MouseEventButton::Secondary,
            Self::Middle => MouseEventButton::Auxiliary,
        }
    }

    /// The same button, as a set of held buttons.
    fn held(self) -> MouseEventButtons {
        match self {
            Self::Left => MouseEventButtons::Primary,
            Self::Right => MouseEventButtons::Secondary,
            Self::Middle => MouseEventButtons::Auxiliary,
        }
    }
}

/// Collects what the document decided, once it has done its own part.
///
/// The document handles what a document handles: focus follows a click, a box being typed in takes
/// the characters. What reaches here is what is left for a program to answer.
struct Collector<'a> {
    reports: &'a mut Vec<Report>,
}

impl EventHandler for Collector<'_> {
    fn handle_event(
        &mut self,
        _chain: &[usize],
        event: &mut DomEvent,
        _mutr: &mut DocumentMutator<'_>,
        _state: &mut EventState,
    ) {
        let report = match event.data {
            DomEventData::Click(_) => Report::Click(event.target),
            DomEventData::Input(_) => Report::Input(event.target),
            DomEventData::KeyDown(ref key) => match key.key {
                Key::Enter => Report::Key(event.target, Ending::Accepted),
                Key::Escape => Report::Key(event.target, Ending::Abandoned),
                _ => return,
            },
            _ => return,
        };

        self.reports.push(report);
    }
}

/// What a fetch left for the document.
type Arrived = std::sync::Arc<std::sync::Mutex<Vec<blitz_dom::net::Resource>>>;

/// Reads what a page asks for out of one directory.
///
/// A page may read pictures, fonts and stylesheets from the assets it was opened with, and nothing
/// else: no network, and nothing outside that directory. A document is markup somebody wrote, and
/// markup that can read any file on the machine is markup that can read a private key.
struct Files {
    root: std::path::PathBuf,
    arrived: Arrived,
}

impl blitz_traits::net::NetProvider<blitz_dom::net::Resource> for Files {
    fn fetch(
        &self,
        doc_id: usize,
        request: blitz_traits::net::Request,
        handler: blitz_traits::net::BoxedHandler<blitz_dom::net::Resource>,
    ) {
        let Ok(path) = request.url.to_file_path() else {
            return;
        };

        // Inside the assets directory, whatever the page asked for. `..` in a source would
        // otherwise reach the rest of the disk.
        let Ok(path) = path.canonicalize() else {
            return;
        };

        let Ok(root) = self.root.canonicalize() else {
            return;
        };

        if !path.starts_with(&root) {
            return;
        }

        let Ok(bytes) = std::fs::read(&path) else {
            return;
        };

        let waiting = self.arrived.clone();

        handler.bytes(
            doc_id,
            blitz_traits::net::Bytes::from(bytes),
            std::sync::Arc::new(move |_document, result| {
                if let Ok(resource) = result
                    && let Ok(mut waiting) = waiting.lock()
                {
                    waiting.push(resource);
                }
            }),
        );
    }
}

/// One open document.
pub struct Interface {
    document: HtmlDocument,
    reports: Vec<Report>,

    /// What the document asked for and got, waiting to be handed to it.
    ///
    /// A fetch answers on whatever thread asked, and a document may not be touched from two places
    /// at once, so what came back is put here and taken up the next time the page is laid out.
    arrived: Arrived,
    width: u32,
    height: u32,
    pointer: (f32, f32),
    dirty: bool,
}

impl Interface {
    /// Opens a document from its markup, at a size.
    pub fn open(html: &str, width: u32, height: u32) -> Self {
        Self::open_from(html, width, height, None)
    }

    /// Opens a document that can reach files, for the pictures in it.
    ///
    /// `assets` is the directory a `src` is resolved against. Without one an `<img>` is a box with
    /// nothing in it, because the document has no way to fetch anything and no business inventing
    /// one: what a page may read is the program's decision, not the page's.
    pub fn open_from(html: &str, width: u32, height: u32, assets: Option<&std::path::Path>) -> Self {
        // Made absolute, because a relative directory is not a base a URL can be built from and a
        // page whose base is missing panics the first time it resolves a relative source.
        let assets = assets.and_then(|root| root.canonicalize().ok());
        let assets = assets.as_deref();

        // The parser is handed to the document as well as used to build it, so that markup written
        // later can be put into an element the way `innerHTML` does. That is how a panel arrives:
        // as a piece of a page rather than as a page of its own.
        let arrived = std::sync::Arc::new(std::sync::Mutex::new(Vec::new()));

        let config = DocumentConfig {
            html_parser_provider: Some(std::sync::Arc::new(blitz_html::HtmlProvider)),
            net_provider: assets.map(|root| -> std::sync::Arc<dyn blitz_traits::net::NetProvider<blitz_dom::net::Resource>> {
                std::sync::Arc::new(Files {
                    root: root.to_path_buf(),
                    arrived: arrived.clone(),
                })
            }),
            // A directory, with the trailing slash a base needs: without it the last segment is
            // taken for a file name and every relative source resolves one level too high. There
            // is always a base, even with no assets to read, because resolving a relative source
            // against nothing is a panic rather than a miss.
            base_url: Some(
                assets
                    .and_then(|root| url::Url::from_directory_path(root).ok())
                    .or_else(|| {
                        std::env::current_dir()
                            .ok()
                            .and_then(|here| url::Url::from_directory_path(here).ok())
                    })
                    .map(|url| url.to_string())
                    .unwrap_or_else(|| "file:///".to_string()),
            ),
            ..Default::default()
        };

        let mut document = HtmlDocument::from_html(html, config);

        document.set_viewport(Viewport::new(width, height, 1.0, ColorScheme::Dark));

        let mut interface = Self {
            document,
            reports: Vec::new(),
            arrived,
            width,
            height,
            pointer: (0.0, 0.0),
            dirty: true,
        };

        interface.resolve();
        interface
    }

    /// The document underneath, for what this crate does not wrap.
    pub fn document(&self) -> &BaseDocument {
        &self.document
    }

    /// The document underneath, to change.
    pub fn document_mut(&mut self) -> &mut BaseDocument {
        &mut self.document
    }

    /// Restyles and lays the document out, if anything has changed since it last was.
    pub fn resolve(&mut self) {
        // Whatever arrived since the last look, first: a picture that has just been read is part
        // of this layout rather than of the one after it.
        let arrived: Vec<_> = match self.arrived.lock() {
            Ok(mut waiting) => waiting.drain(..).collect(),
            Err(_) => Vec::new(),
        };

        for resource in arrived {
            self.document.load_resource(resource);
            self.dirty = true;
        }

        if !self.dirty {
            return;
        }

        self.document.resolve(0.0);
        self.dirty = false;
    }

    /// Says something has changed, so the next resolve does the work.
    pub fn touch(&mut self) {
        self.dirty = true;
    }

    /// Whether anything has changed since the document was last laid out.
    pub fn is_dirty(&self) -> bool {
        self.dirty
    }

    /// How large the document is, in logical pixels.
    pub fn size(&self) -> (u32, u32) {
        (self.width, self.height)
    }

    /// Says how large the window is, and lays out again for it.
    pub fn resize(&mut self, width: u32, height: u32) {
        if self.width == width && self.height == height {
            return;
        }

        self.width = width;
        self.height = height;
        self.document
            .set_viewport(Viewport::new(width, height, 1.0, ColorScheme::Dark));

        self.dirty = true;
        self.resolve();
    }

    /// The element carrying an id, or nothing.
    pub fn element(&self, id: &str) -> Option<usize> {
        self.document.get_element_by_id(id)
    }

    /// The first element a selector matches, or nothing.
    ///
    /// The whole of CSS is available here, because the thing doing the matching is the thing that
    /// matches for Firefox.
    pub fn select(&self, selector: &str) -> Option<usize> {
        self.document.query_selector(selector).ok().flatten()
    }

    /// Every element a selector matches.
    pub fn select_all(&self, selector: &str) -> Vec<usize> {
        self.document
            .query_selector_all(selector)
            .map(|found| found.to_vec())
            .unwrap_or_default()
    }

    /// Where an element ended up, in logical pixels: x, y, width, height.
    pub fn rect(&self, node: usize) -> Option<(f32, f32, f32, f32)> {
        let node = self.document.get_node(node)?;
        let layout = node.final_layout;

        // Where it is on the page, not where it is inside whatever holds it. A caller asking this
        // is putting something over it, hit testing it, or drawing into it, and all three are in
        // the page's coordinates.
        let at = node.absolute_position(0.0, 0.0);

        Some((at.x, at.y, layout.size.width, layout.size.height))
    }

    /// What a field holds, for the elements that hold text of their own.
    ///
    /// An `<input>` keeps its value in an editor rather than in its children, so reading its text
    /// the way an ordinary element is read answers nothing.
    pub fn value(&self, node: usize) -> Option<String> {
        let element = self.document.get_node(node)?.element_data()?;

        if let Some(field) = element.text_input_data() {
            return Some(field.editor.text().to_string());
        }

        element
            .attrs
            .iter()
            .find(|attribute| attribute.name == qual_name("value"))
            .map(|attribute| attribute.value.to_string())
    }

    /// Puts text into a field, replacing what was there.
    ///
    /// Through the attribute rather than into the editor directly. A field's text has a layout of
    /// its own, built with the document's fonts, and text set without building it leaves a field
    /// that the painter reaches with nothing to draw.
    pub fn set_value(&mut self, node: usize, value: &str) {
        self.set_attribute(node, "value", value);
    }

    /// What an element says.
    pub fn text(&self, node: usize) -> String {
        self.document
            .get_node(node)
            .map(|node| node.text_content())
            .unwrap_or_default()
    }

    /// Says what an element says.
    ///
    /// The whole of its text, replaced, which is what `textContent` means. A text node is written
    /// in place where there is one already, because the alternative is throwing away a node and
    /// making another one on every frame that writes a value, and a panel showing a live number
    /// writes one every frame.
    pub fn set_text(&mut self, node: usize, text: &str) {
        let children = self.document.mutate().child_ids(node);

        let only_text = match children.as_slice() {
            [only] => self
                .document
                .get_node(*only)
                .is_some_and(|child| child.text_data().is_some())
                .then_some(*only),
            _ => None,
        };

        if let Some(existing) = only_text {
            self.mutate(|mutator| mutator.set_node_text(existing, text));
            return;
        }

        self.mutate(|mutator| {
            mutator.remove_and_drop_all_children(node);

            let written = mutator.create_text_node(text);
            mutator.append_children(node, &[written]);
        });
    }

    /// Sets an attribute, which is how a document says what state a thing is in.
    pub fn set_attribute(&mut self, node: usize, name: &str, value: &str) {
        let name = qual_name(name);
        self.mutate(|mutator| mutator.set_attribute(node, name, value));
    }

    /// What holds an element, or nothing for the root.
    ///
    /// A click lands on the innermost thing under the pointer, which for a button with a picture
    /// in it is the picture. Walking up from there is how a document answers "what was clicked",
    /// and it is the caller's to decide how far up to look.
    pub fn parent(&self, node: usize) -> Option<usize> {
        self.document.get_node(node)?.parent
    }

    /// What an attribute says, or nothing.
    ///
    /// Read out of the document rather than remembered beside it, so that markup replaced
    /// wholesale answers for itself instead of from a copy that is one panel out of date.
    pub fn attribute(&self, node: usize, name: &str) -> Option<String> {
        let element = self.document.get_node(node)?.element_data()?;
        let name = qual_name(name);

        element
            .attrs
            .iter()
            .find(|attribute| attribute.name == name)
            .map(|attribute| attribute.value.to_string())
    }

    /// Takes an attribute off again.
    pub fn clear_attribute(&mut self, node: usize, name: &str) {
        let name = qual_name(name);
        self.mutate(|mutator| mutator.clear_attribute(node, name));
    }

    /// Sets one style property on an element, over whatever the stylesheet said.
    ///
    /// The escape hatch a program needs and a stylesheet cannot give it: where a panel goes is
    /// decided while the program runs. Everything about how it looks belongs in the stylesheet.
    pub fn set_style(&mut self, node: usize, property: &str, value: &str) {
        self.mutate(|mutator| mutator.set_style_property(node, property, value));
    }

    /// Puts markup inside an element, replacing whatever was there.
    ///
    /// What `innerHTML` does, and the whole of how a panel arrives: one page holds the interface,
    /// and a panel is a piece of markup put into it. Everything that used to be a separate
    /// document with its own layer, its own placement and its own rebuild is a subtree here.
    pub fn set_html(&mut self, node: usize, html: &str) {
        self.mutate(|mutator| mutator.set_inner_html(node, html));
    }

    /// Makes an element of a given kind, outside the document until it is put somewhere.
    pub fn create(&mut self, tag: &str) -> usize {
        let name = qual_name(tag);
        let mut mutator = self.document.mutate();
        let node = mutator.create_element(name, Vec::new());
        drop(mutator);

        self.dirty = true;
        node
    }

    /// Puts an element inside another, after whatever is already there.
    pub fn append(&mut self, parent: usize, child: usize) {
        self.mutate(|mutator| mutator.append_children(parent, &[child]));
    }

    /// Takes an element out of the document and forgets it.
    pub fn remove(&mut self, node: usize) {
        self.mutate(|mutator| {
            mutator.remove_and_drop_node(node);
        });
    }

    /// Gives an element the keyboard.
    pub fn focus(&mut self, node: usize) {
        if self.document.set_focus_to(node) {
            self.dirty = true;
        }
    }

    /// Takes the keyboard away from whatever has it.
    pub fn blur(&mut self) {
        self.document.clear_focus();
        self.dirty = true;
    }

    /// What has the keyboard, or nothing.
    pub fn focused(&self) -> Option<usize> {
        self.document.get_focussed_node_id()
    }

    /// Which element is under a point, if any.
    pub fn hit(&self, x: f32, y: f32) -> Option<usize> {
        self.document.hit(x, y).map(|hit| hit.node_id)
    }

    /// Tells the document the pointer moved.
    pub fn moved(&mut self, x: f32, y: f32) {
        self.pointer = (x, y);
        self.send(UiEvent::MouseMove(self.mouse(Button::Left)));
    }

    /// Tells the document a button went down.
    pub fn pressed(&mut self, button: Button) {
        self.send(UiEvent::MouseDown(self.mouse(button)));
    }

    /// And came up again, which is what makes a click.
    pub fn released(&mut self, button: Button) {
        self.send(UiEvent::MouseUp(self.mouse(button)));
    }

    /// Tells the document a key went down, and what it would type.
    ///
    /// A key reaches whatever holds the keyboard, which is how typing into a field works and why
    /// nothing here has to know which element that is. `text` is what the key produces, which is
    /// the platform's answer rather than something derived from the key: a layout, a dead key and
    /// a modifier all change it.
    pub fn key_down(&mut self, key: Key, text: Option<&str>, modifiers: Modifiers) {
        let event = self.key(key, text, modifiers, KeyState::Pressed);
        self.send(UiEvent::KeyDown(event));
    }

    /// And came up again.
    pub fn key_up(&mut self, key: Key, modifiers: Modifiers) {
        let event = self.key(key, None, modifiers, KeyState::Released);
        self.send(UiEvent::KeyUp(event));
    }

    /// One key event, filled in the way a browser fills one.
    fn key(
        &self,
        key: Key,
        text: Option<&str>,
        modifiers: Modifiers,
        state: KeyState,
    ) -> BlitzKeyEvent {
        BlitzKeyEvent {
            key,
            // The physical key, which nothing in the document reads and which cannot be worked
            // out from a logical key without knowing the layout.
            code: Code::Unidentified,
            modifiers,
            location: Location::Standard,
            is_auto_repeating: false,
            is_composing: false,
            state,
            text: text.map(smol_str::SmolStr::new),
        }
    }

    /// Rolls the wheel where the pointer is.
    ///
    /// Whatever is under the pointer scrolls, and when it has no more to give the scroll carries
    /// up to whatever holds it and finally to the page. That is what `overflow` means, and it is
    /// the document's to work out rather than something measured on the way in.
    pub fn scroll(&mut self, x: f64, y: f64) {
        let target = self.hit(self.pointer.0, self.pointer.1).unwrap_or(0);
        self.document.scroll_node_by(target, x, y);
        self.dirty = true;
    }

    /// What the document reported since this was last asked, oldest first.
    pub fn drain(&mut self) -> Vec<Report> {
        std::mem::take(&mut self.reports)
    }

    /// Paints the document into a buffer of `width * height` pixels, four bytes each.
    ///
    /// A picture rather than a set of engine entities, because what CSS can express is what a
    /// painter draws: rounded corners, gradients, shadows, shaped text. Translating those into
    /// another interface's boxes is where the fidelity a real style system buys would go again.
    pub fn paint(&self, into: &mut [u8]) {
        let mut renderer = <VelloCpuImageRenderer as ImageRenderer>::new(self.width, self.height);

        renderer.render(
            |scene| blitz_paint::paint_scene(scene, &self.document, 1.0, self.width, self.height),
            into,
        );
    }

    /// How many bytes a painting of this document takes.
    pub fn pixels(&self) -> usize {
        (self.width as usize) * (self.height as usize) * 4
    }

    /// Changes the document, and says it needs laying out again.
    fn mutate(&mut self, change: impl FnOnce(&mut DocumentMutator<'_>)) {
        let mut mutator = self.document.mutate();
        change(&mut mutator);
        drop(mutator);

        self.dirty = true;
    }

    /// The pointer where it is, as a mouse event.
    fn mouse(&self, button: Button) -> BlitzMouseButtonEvent {
        BlitzMouseButtonEvent {
            x: self.pointer.0,
            y: self.pointer.1,
            button: button.named(),
            buttons: button.held(),
            mods: Default::default(),
        }
    }

    /// Hands one event to the document and keeps what it decided.
    fn send(&mut self, event: UiEvent) {
        let mut reports = std::mem::take(&mut self.reports);

        {
            let mutator = self.document.mutate();
            let mut driver = EventDriver::new(mutator, Collector { reports: &mut reports });
            driver.handle_ui_event(event);
        }

        self.reports = reports;
        self.dirty = true;
    }
}

/// An attribute's name.
///
/// With no namespace at all, which is what an HTML attribute has: `class` and `id` are not in the
/// HTML namespace, they are in none. Putting them in one makes a second attribute of the same name
/// beside the one the parser made, and the document goes on reading the first.
fn qual_name(name: &str) -> blitz_dom::QualName {
    blitz_dom::QualName::new(None, blitz_dom::ns!(), name.into())
}

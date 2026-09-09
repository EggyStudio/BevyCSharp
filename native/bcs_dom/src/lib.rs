//! An interface document: the tree, its styles, its layout, and its pixels.
//!
//! The whole of this crate is a thin joining of parts that are already the best of their kind.
//! Stylo resolves the cascade, Taffy lays the boxes out, Parley shapes the text, and Blitz holds
//! the three together as a document. What is written here is only what is ours to write: opening a
//! document, asking it questions, and handing it to the engine to draw.
//!
//! It replaces a stylesheet reader written by hand. That reader matched selectors as text and
//! colours as text, which is why a rule naming two classes weighed the same as one naming either,
//! why `oklch` was not a colour, and why `@layer`, `:where`, `+` and `[attr]` did nothing at all.
//! None of those are decisions anybody made; they are what a hand-written subset of CSS looks like
//! from the inside.

use anyrender::ImageRenderer;
use anyrender_vello_cpu::VelloCpuImageRenderer;
use blitz_dom::{BaseDocument, DocumentConfig};
use blitz_html::HtmlDocument;
use blitz_traits::shell::{ColorScheme, Viewport};

/// One open document.
pub struct Interface {
    document: HtmlDocument,
    width: u32,
    height: u32,
}

impl Interface {
    /// Opens a document from its markup, at a size.
    pub fn open(html: &str, width: u32, height: u32) -> Self {
        let mut document = HtmlDocument::from_html(html, DocumentConfig::default());

        document.set_viewport(Viewport::new(width, height, 1.0, ColorScheme::Dark));

        let mut interface = Self {
            document,
            width,
            height,
        };

        interface.resolve();
        interface
    }

    /// The document underneath, for whatever this crate does not wrap yet.
    pub fn document(&self) -> &BaseDocument {
        &self.document
    }

    /// Restyles and lays the document out again.
    pub fn resolve(&mut self) {
        self.document.resolve(0.0);
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

        self.resolve();
    }

    /// The element carrying an id, or nothing.
    pub fn element(&self, id: &str) -> Option<usize> {
        self.document.get_element_by_id(id)
    }

    /// Where an element ended up, in logical pixels: x, y, width, height.
    pub fn rect(&self, node: usize) -> Option<(f32, f32, f32, f32)> {
        let node = self.document.get_node(node)?;
        let layout = node.final_layout;

        Some((
            layout.location.x,
            layout.location.y,
            layout.size.width,
            layout.size.height,
        ))
    }

    /// Paints the document into a buffer of `width * height` pixels, four bytes each.
    ///
    /// A picture rather than a set of engine entities, because what CSS can express is what a
    /// painter draws: rounded corners, gradients, shadows, shaped text. Translating those into
    /// another interface's boxes is where the fidelity a real style system buys would go again.
    pub fn paint(&self, into: &mut [u8]) {
        let mut renderer =
            <VelloCpuImageRenderer as ImageRenderer>::new(self.width, self.height);

        renderer.render(
            |scene| blitz_paint::paint_scene(scene, &self.document, 1.0, self.width, self.height),
            into,
        );
    }

    /// How many bytes a painting of this document takes.
    pub fn pixels(&self) -> usize {
        (self.width as usize) * (self.height as usize) * 4
    }
}

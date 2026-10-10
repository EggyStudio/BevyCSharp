//! Which stage of which pipeline a shader fills.

use super::Family;
use super::slang;

/// Which stage of which pipeline a shader fills.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum Role {
    Vertex = 0,
    Fragment = 1,
    PrepassVertex = 2,
    PrepassFragment = 3,
    Compute = 4,
    Pass = 5,
    /// The vertex shader of geometry a program draws on a camera itself, out of buffers, rather
    /// than a mesh Bevy draws with a material.
    DrawVertex = 6,
    /// The fragment shader of the same.
    DrawFragment = 7,
    /// The fragment shader a material draws into Bevy's deferred buffers with, on a camera that
    /// draws deferred, which writes the surface Bevy's deferred lighting pass lights rather than a
    /// color.
    Deferred = 8,
    /// The fragment shader geometry drawn on a camera is drawn into shadow maps with, writing its
    /// depth itself, for geometry its vertex shader does not place, such as a surface its fragment
    /// shader finds by marching a ray.
    DrawShadow = 9,
    /// The vertex shader of a material drawn on a 2D mesh by a 2D camera.
    Vertex2d = 10,
    /// The fragment shader of the same.
    Fragment2d = 11,
}

/// How many roles a program has.
pub const ROLE_COUNT: usize = 12;

impl Role {
    pub const ALL: [Role; ROLE_COUNT] = [
        Role::Vertex,
        Role::Fragment,
        Role::PrepassVertex,
        Role::PrepassFragment,
        Role::Compute,
        Role::Pass,
        Role::DrawVertex,
        Role::DrawFragment,
        Role::Deferred,
        Role::DrawShadow,
        Role::Vertex2d,
        Role::Fragment2d,
    ];

    /// The roles a material is drawn with.
    pub const MATERIAL: [Role; 5] = [
        Role::Vertex,
        Role::Fragment,
        Role::PrepassVertex,
        Role::PrepassFragment,
        Role::Deferred,
    ];

    /// The entry point a stage has when the program does not name one, the name Bevy's own shaders
    /// use.
    pub(super) fn default_entry(self) -> &'static str {
        match self {
            Role::Vertex | Role::PrepassVertex | Role::DrawVertex | Role::Vertex2d => "vertex",
            Role::Fragment | Role::PrepassFragment | Role::Pass | Role::DrawFragment | Role::Fragment2d => {
                "fragment"
            }
            Role::Deferred => "deferred",
            Role::DrawShadow => "shadow",
            Role::Compute => "main",
        }
    }

    pub(super) fn stage(self) -> slang::Stage {
        match self {
            Role::Vertex | Role::PrepassVertex | Role::DrawVertex | Role::Vertex2d => slang::Stage::Vertex,
            Role::Fragment
            | Role::PrepassFragment
            | Role::Pass
            | Role::DrawFragment
            | Role::Deferred
            | Role::DrawShadow
            | Role::Fragment2d => slang::Stage::Fragment,
            Role::Compute => slang::Stage::Compute,
        }
    }

    /// Where the stage's own globals go, which differs between a material, a pass and a dispatch.
    pub fn family(self) -> Family {
        match self {
            Role::Vertex
            | Role::Fragment
            | Role::PrepassVertex
            | Role::PrepassFragment
            | Role::Deferred => Family::Material,
            // Drawing on a camera reads what a pass does, the camera's inputs in group one and its
            // own values in group zero, so it is laid out the way a pass is.
            Role::Pass | Role::DrawVertex | Role::DrawFragment | Role::DrawShadow => Family::Pass,
            Role::Compute => Family::Compute,
            Role::Vertex2d | Role::Fragment2d => Family::Material2d,
        }
    }

    pub(super) fn describe(self) -> &'static str {
        match self {
            Role::Vertex => "vertex",
            Role::Fragment => "fragment",
            Role::PrepassVertex => "prepass vertex",
            Role::PrepassFragment => "prepass fragment",
            Role::Compute => "compute",
            Role::Pass => "pass",
            Role::DrawVertex => "draw vertex",
            Role::DrawFragment => "draw fragment",
            Role::Deferred => "deferred",
            Role::DrawShadow => "draw shadow",
            Role::Vertex2d => "2D vertex",
            Role::Fragment2d => "2D fragment",
        }
    }
}

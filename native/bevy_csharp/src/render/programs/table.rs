//! The render side's half of the programs, the table it builds pipelines and bind groups from and
//! the compute pipelines it has built, kept outside the world since the render side has no main
//! world to look in.

use std::borrow::Cow;
use std::sync::{Arc, RwLock};

use bevy::asset::Handle;
use bevy::shader::Shader;

use super::ROLE_COUNT;
use crate::render::reflect::Layout;

/// One stage as a pipeline sees it, a shader and the entry point in it.
#[derive(Clone, Debug)]
pub struct StageBinding {
    pub shader: Handle<Shader>,
    pub entry: Cow<'static, str>,
    /// The stage's SPIR-V, for one compiled to it, which the bridge builds a compute pipeline from
    /// itself (see [`super::spirv_compute`]).
    pub spirv: Option<Arc<[u32]>>,
    /// The pipeline constants the stage declares, by name, each beside the key a pipeline sets it
    /// by. A stage's own, since a pipeline handed one its stage does not declare is refused.
    pub constants: Arc<[(String, String)]>,
}

/// What the render side needs of a program.
#[derive(Clone, Debug, Default)]
pub struct PipelineProgram {
    pub stages: [Option<StageBinding>; ROLE_COUNT],
    /// The layout of a material's own group, merged from every material stage, once each has a
    /// result to be merged.
    pub material: Option<Arc<Layout>>,
    pub pass: Option<Arc<Layout>>,
    pub compute: Option<Arc<Layout>>,
    /// The layout of what a program drawing on a camera declares, merged from both of its stages.
    pub draw: Option<Arc<Layout>>,
    /// The layout of a 2D material's own group, merged from both of its stages.
    pub material2d: Option<Arc<Layout>>,
    /// The layout of an interface material's own group, merged from both of its stages.
    pub material_ui: Option<Arc<Layout>>,
    /// Moves on every time a stage is replaced, and a pipeline or a bind group made from an older
    /// version checks itself against it.
    pub generation: u32,
    /// Its first stage's file name, which its GPU time is recorded under.
    pub name: Arc<str>,
}

/// Every program the running app has made, by number.
///
/// Process-wide because the render side has no main world to look in. Emptied when an app is built,
/// so the numbers a new app hands out never name a program of an app before it, whose shader
/// handles belong to an asset server that no longer exists.
pub(super) static TABLE: RwLock<Vec<PipelineProgram>> = RwLock::new(Vec::new());

/// Looks a program up.
pub fn lookup(id: u32) -> Option<PipelineProgram> {
    TABLE.read().ok()?.get(id as usize).cloned()
}

/// What a program's GPU time is recorded under, `shader` and its file's name, or its number where
/// it has not compiled yet.
pub fn label(id: u32) -> std::borrow::Cow<'static, str> {
    match lookup(id).filter(|program| !program.name.is_empty()) {
        Some(program) => format!("shader {}", program.name).into(),
        None => format!("shader {id}").into(),
    }
}

/// How many programs there are, which is where the render side's look through them stops.
pub fn table_len() -> u32 {
    TABLE.read().map(|table| table.len() as u32).unwrap_or(0)
}

/// The programs whose compute pipeline for their current generation has been built.
///
/// Written by the render side, which is the only side that can see the pipeline cache, and read by
/// [`state`], because a dispatch made before its pipeline exists does nothing, and a program is not
/// ready while that is still true of it.
static COMPUTE_READY: RwLock<Vec<u32>> = RwLock::new(Vec::new());

/// Says that a program's compute pipeline has been built for `generation`.
pub fn mark_compute_ready(id: u32, generation: u32) {
    if let Ok(mut ready) = COMPUTE_READY.write() {
        let index = id as usize;

        if ready.len() <= index {
            ready.resize(index + 1, u32::MAX);
        }

        ready[index] = generation;
    }
}

pub(super) fn compute_ready(id: usize, generation: u32) -> bool {
    COMPUTE_READY
        .read()
        .map(|ready| ready.get(id).copied() == Some(generation))
        .unwrap_or(false)
}

/// Forgets every program, so a new app starts from none.
pub fn forget_all() {
    if let Ok(mut table) = TABLE.write() {
        table.clear();
    }

    if let Ok(mut ready) = COMPUTE_READY.write() {
        ready.clear();
    }
}

//! Shader programs: which Slang files draw a material, run over a camera's picture or run over
//! buffers, compiled and kept current while the app runs.
//!
//! A program names up to six stages, each a file (or source text) and an entry point: the vertex
//! and fragment shaders of a material's main pass and of its prepass, a full-screen pass over a
//! camera's picture, and a compute shader. A program is a number, and a material, a pass or a
//! dispatch says which number it runs, so there is no limit on how many a game has.
//!
//! **What a shader declares is what it is handed.** Every compile is read back (see
//! [`super::reflect`]) into the layout of the shader's own globals, and that layout, not a table
//! fixed in advance, is what a material's bind group is built in. A program's material stages share
//! one layout, merged from each stage's view of it.
//!
//! **Why a table outside the world.** The render side builds pipelines and bind groups from a
//! program, and it has no main world to look in. [`TABLE`] is what it reads. The world keeps the
//! rest ([`ShaderPrograms`]): the compile jobs, the files to watch and what went wrong.
//!
//! **Hot reload.** A Slang file is recompiled here, off the main thread, whenever it or anything it
//! imported changes. A successful compile is a new shader asset and a new layout, and the program's
//! generation moves on, which is what makes materials build their bind groups again, by name, and
//! pipelines be built from the new shader. A new asset rather than a replaced one, because an old
//! pipeline recompiled from new code against an old layout would be a pipeline that fails.
//!
//! **When a file fails.** The last version that compiled stays, so a typo in a running game
//! changes nothing on screen and says what is wrong in the log. A stage that has never compiled
//! draws with a fallback instead of drawing nothing: magenta for a fragment shader or a pass, which
//! is conspicuous on purpose, Bevy's own behavior for a vertex shader, and nothing for a compute
//! shader. The fallback has the entry point the stage asked for, because the pipeline names it.

#![cfg(feature = "render")]

use std::borrow::Cow;
use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::sync::mpsc::{Receiver, Sender, channel};
use std::sync::{Arc, Mutex, RwLock};
use std::time::{Duration, Instant};

use bevy::asset::{Assets, Handle};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::ResMut;
use bevy::ecs::world::World;
use bevy::shader::Shader;

use super::reflect::{Family, Layout, Reflected, reflect};
use super::slang;
use crate::interop::status;

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
}

/// How many roles a program has.
pub const ROLE_COUNT: usize = 8;

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
    ];

    /// The roles a material is drawn with.
    pub const MATERIAL: [Role; 4] = [
        Role::Vertex,
        Role::Fragment,
        Role::PrepassVertex,
        Role::PrepassFragment,
    ];

    /// The entry point a stage has when the program does not name one, which is what Bevy's own
    /// shaders call theirs.
    fn default_entry(self) -> &'static str {
        match self {
            Role::Vertex | Role::PrepassVertex | Role::DrawVertex => "vertex",
            Role::Fragment | Role::PrepassFragment | Role::Pass | Role::DrawFragment => "fragment",
            Role::Compute => "main",
        }
    }

    fn stage(self) -> slang::Stage {
        match self {
            Role::Vertex | Role::PrepassVertex | Role::DrawVertex => slang::Stage::Vertex,
            Role::Fragment | Role::PrepassFragment | Role::Pass | Role::DrawFragment => {
                slang::Stage::Fragment
            }
            Role::Compute => slang::Stage::Compute,
        }
    }

    /// Where the stage's own globals go, which differs between a material, a pass and a dispatch.
    pub fn family(self) -> Family {
        match self {
            Role::Vertex | Role::Fragment | Role::PrepassVertex | Role::PrepassFragment => {
                Family::Material
            }
            // Drawing on a camera reads what a pass does, the camera's inputs in group one and its
            // own values in group zero, so it is laid out the way a pass is.
            Role::Pass | Role::DrawVertex | Role::DrawFragment => Family::Pass,
            Role::Compute => Family::Compute,
        }
    }

    fn describe(self) -> &'static str {
        match self {
            Role::Vertex => "vertex",
            Role::Fragment => "fragment",
            Role::PrepassVertex => "prepass vertex",
            Role::PrepassFragment => "prepass fragment",
            Role::Compute => "compute",
            Role::Pass => "pass",
            Role::DrawVertex => "draw vertex",
            Role::DrawFragment => "draw fragment",
        }
    }
}

/// One stage as a pipeline sees it: a shader and the entry point in it.
#[derive(Clone, Debug)]
pub struct StageBinding {
    pub shader: Handle<Shader>,
    pub entry: Cow<'static, str>,
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
    /// Moves on every time a stage is replaced, which is what a pipeline or a bind group made from
    /// an older version checks itself against.
    pub generation: u32,
}

/// Every program the running app has made, by number.
///
/// Process-wide because the render side has no main world to look in. Emptied when an app is built,
/// so the numbers a new app hands out never name a program of an app before it, whose shader
/// handles belong to an asset server that no longer exists.
static TABLE: RwLock<Vec<PipelineProgram>> = RwLock::new(Vec::new());

/// Looks a program up.
pub fn lookup(id: u32) -> Option<PipelineProgram> {
    TABLE.read().ok()?.get(id as usize).cloned()
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

fn compute_ready(id: usize, generation: u32) -> bool {
    COMPUTE_READY
        .read()
        .map(|ready| ready.get(id).copied() == Some(generation))
        .unwrap_or(false)
}

/// Forgets every program, which is what a new app starts from.
pub fn forget_all() {
    if let Ok(mut table) = TABLE.write() {
        table.clear();
    }

    if let Ok(mut ready) = COMPUTE_READY.write() {
        ready.clear();
    }
}

// -- The world's half

/// A define as the caller gave it.
#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub enum Define {
    Bool(String, bool),
    Int(String, i32),
    UInt(String, u32),
}

impl Define {
    fn name(&self) -> &str {
        match self {
            Define::Bool(name, _) | Define::Int(name, _) | Define::UInt(name, _) => name,
        }
    }

    /// The same define as `slangc` takes it, or `None` for a false one, because `#ifdef` asks
    /// whether a name is present rather than what it holds, so a false boolean would read as on.
    fn to_slang(&self) -> Option<(String, String)> {
        match self {
            Define::Bool(_, false) => None,
            Define::Bool(name, true) => Some((name.clone(), "1".into())),
            Define::Int(name, value) => Some((name.clone(), value.to_string())),
            Define::UInt(name, value) => Some((name.clone(), value.to_string())),
        }
    }
}

/// Where a stage's code is, as the caller gave it.
#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub enum StageFile {
    /// A `.slang` file under the asset root.
    Path(String),
    /// Slang source, handed over as text.
    Source(String),
}

impl StageFile {
    /// What messages call it: the path, or a name made from the text's hash.
    fn describe(&self) -> String {
        match self {
            StageFile::Path(path) => path.clone(),
            StageFile::Source(source) => {
                format!("inline Slang {:08x}", slang::fnv(source.as_bytes()) as u32)
            }
        }
    }
}

/// A program as the caller described it.
#[derive(Clone, Debug, Default, PartialEq, Eq, Hash)]
pub struct ProgramDescription {
    /// Where the code is and the entry point in it, per role, where the program has one.
    pub stages: [Option<(StageFile, Option<String>)>; ROLE_COUNT],
    pub defines: Vec<Define>,
}

/// What the world remembers of one program.
struct Program {
    /// The compile unit filling each role.
    stages: [Option<usize>; ROLE_COUNT],
    description: String,
    /// Why the stages' layouts could not be merged, where they could not.
    problem: String,
    generation: u32,
}

/// Whether a compile has an answer yet, and which.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
enum UnitState {
    Compiling,
    Ready,
    Failed,
}

/// One entry point of one Slang file under one set of defines, which is what `slangc` compiles.
///
/// Shared between programs that ask for the same thing, so ten programs naming one shader cost one
/// compile rather than ten.
struct Unit {
    request: slang::Request,
    role: Role,
    /// What messages call it.
    path: String,
    /// The shader as last compiled, or the fallback, or `None` before either.
    shader: Option<Handle<Shader>>,
    layout: Layout,
    state: UnitState,
    diagnostics: String,
    /// Whether a compile has ever succeeded, which decides between keeping the last result and
    /// drawing with the fallback.
    compiled: bool,
    busy: bool,
    /// A file changed while a compile was running, so another is owed when it finishes.
    stale: bool,
    /// The files the last result was built from, with what each held.
    watched: Vec<(PathBuf, Option<u64>)>,
    /// Which programs use it, which are the ones to bring up to date when it changes.
    programs: Vec<usize>,
    /// Counts compiles, which keeps each shader asset's name its own.
    compiles: u32,
}

/// A finished compile, on its way back to the main thread.
struct Finished {
    unit: usize,
    result: Result<(slang::Compiled, Reflected), String>,
}

/// The programs, their compiles and the files they are built from.
#[derive(Resource)]
pub struct ShaderPrograms {
    programs: Vec<Program>,
    by_description: HashMap<ProgramDescription, i32>,
    units: Vec<Unit>,
    unit_by_key: HashMap<(PathBuf, Role, String, Vec<Define>), usize>,
    root: PathBuf,
    sender: Sender<Finished>,
    receiver: Mutex<Receiver<Finished>>,
    last_poll: Instant,
    /// Programs whose generation moved since this was last taken, whose materials have to build
    /// their bind groups again.
    changed: Vec<u32>,
}

impl ShaderPrograms {
    /// Programs whose files are found under `root`.
    pub fn new(root: PathBuf) -> Self {
        let (sender, receiver) = channel();

        Self {
            programs: Vec::new(),
            by_description: HashMap::new(),
            units: Vec::new(),
            unit_by_key: HashMap::new(),
            root,
            sender,
            receiver: Mutex::new(receiver),
            last_poll: Instant::now(),
            changed: Vec::new(),
        }
    }

    /// The programs whose generation moved since this was last asked.
    pub fn take_changed(&mut self) -> Vec<u32> {
        std::mem::take(&mut self.changed)
    }
}

/// How often the files are looked at.
///
/// Four times a second, which is quicker than anybody switches from the editor to the game and
/// slow enough that reading a handful of small files costs nothing measurable.
const POLL_INTERVAL: Duration = Duration::from_millis(250);

/// What a file holds, as a hash, or `None` where it cannot be read.
///
/// A hash of the contents rather than the modification time, because some filesystems keep the
/// time to the second or two, and an edit made within the same second as the last compile would
/// go unnoticed.
fn fingerprint(path: &Path) -> Option<u64> {
    std::fs::read(path).ok().map(|bytes| slang::fnv(&bytes))
}

/// Makes a program, or finds the one already made from the same description.
///
/// Returns the program's number, or a negative status where the description names none of a
/// fragment shader, a pass or a compute shader, or names a file that is not Slang.
pub fn create(world: &mut World, description: ProgramDescription) -> i32 {
    let usable = [Role::Fragment, Role::Compute, Role::Pass, Role::DrawFragment]
        .iter()
        .any(|role| description.stages[*role as usize].is_some());

    if !usable {
        return status::NULL_ARG;
    }

    for (file, _) in description.stages.iter().flatten() {
        if let StageFile::Path(path) = file
            && !path.to_ascii_lowercase().ends_with(".slang")
        {
            return status::NO_COMPONENT;
        }
    }

    for define in &description.defines {
        if define.name().is_empty() || define.name().contains(char::is_whitespace) {
            return status::NULL_ARG;
        }
    }

    let Some(mut programs) = world.remove_resource::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let answer = create_in(&mut programs, description);
    world.insert_resource(programs);
    answer
}

fn create_in(programs: &mut ShaderPrograms, description: ProgramDescription) -> i32 {
    if let Some(&id) = programs.by_description.get(&description) {
        return id;
    }

    let id = programs.programs.len();
    let mut stages: [Option<usize>; ROLE_COUNT] = Default::default();

    for role in Role::ALL {
        let Some((file, entry)) = &description.stages[role as usize] else {
            continue;
        };

        let entry = entry
            .clone()
            .filter(|entry| !entry.is_empty())
            .unwrap_or_else(|| role.default_entry().to_string());

        let path = match file {
            StageFile::Path(path) => programs.root.join(path),
            // Written out, because slangc compiles files, and compiled like any other file from
            // there. Its imports are looked for under the asset root, as a file's are.
            StageFile::Source(text) => match slang::inline_file(text) {
                Ok(path) => path,
                Err(message) => {
                    bevy::log::error!("{message}");
                    return status::INVALID_STATE;
                }
            },
        };

        let unit = unit_for(programs, role, path, file.describe(), &entry, &description.defines);
        programs.units[unit].programs.push(id);
        stages[role as usize] = Some(unit);
    }

    let text = Role::ALL
        .iter()
        .filter_map(|role| {
            description.stages[*role as usize]
                .as_ref()
                .map(|(file, _)| format!("{} {}", role.describe(), file.describe()))
        })
        .collect::<Vec<_>>()
        .join(", ");

    let Ok(mut table) = TABLE.write() else {
        return status::INVALID_STATE;
    };

    // The table and the world's list grow together, so a number is an index into both.
    debug_assert_eq!(table.len(), programs.programs.len());
    table.push(PipelineProgram::default());
    drop(table);

    programs.programs.push(Program {
        stages,
        description: text,
        problem: String::new(),
        generation: 0,
    });

    programs.by_description.insert(description, id as i32);

    // A unit shared with a program made earlier may have its result already.
    rebuild(programs, id);
    id as i32
}

/// Finds the unit compiling this entry point, or starts one.
fn unit_for(
    programs: &mut ShaderPrograms,
    role: Role,
    file: PathBuf,
    path: String,
    entry: &str,
    defines: &[Define],
) -> usize {
    let key = (file.clone(), role, entry.to_string(), defines.to_vec());

    if let Some(&unit) = programs.unit_by_key.get(&key) {
        return unit;
    }

    let request = slang::Request {
        file: file.clone(),
        root: programs.root.clone(),
        stage: role.stage(),
        entry: entry.to_string(),
        defines: defines.iter().filter_map(Define::to_slang).collect(),
    };

    let index = programs.units.len();

    programs.units.push(Unit {
        request,
        role,
        path,
        shader: None,
        layout: Layout {
            group: role.family().own_group(),
            ..Default::default()
        },
        state: UnitState::Compiling,
        diagnostics: String::new(),
        compiled: false,
        busy: false,
        stale: false,
        watched: vec![(file, None)],
        programs: Vec::new(),
        compiles: 0,
    });

    programs.unit_by_key.insert(key, index);
    start(programs, index);
    index
}

/// Starts compiling a unit on a thread of its own.
///
/// A thread rather than a task on one of Bevy's pools, because the work is waiting on a process,
/// and a pool thread parked on one is a thread the pool's other work cannot use. The reading of
/// the result happens there too, since it parses the whole shader.
fn start(programs: &mut ShaderPrograms, index: usize) {
    let unit = &mut programs.units[index];

    // What the files hold now, before the compile reads them, so an edit made while it runs is
    // seen as a change afterwards rather than taken as already compiled.
    for (path, seen) in &mut unit.watched {
        *seen = fingerprint(path);
    }

    unit.busy = true;
    unit.stale = false;

    let request = unit.request.clone();
    let family = unit.role.family();
    let sender = programs.sender.clone();

    let spawned = std::thread::Builder::new()
        .name("bcs-slangc".into())
        .spawn(move || {
            let result = slang::compile(&request).and_then(|compiled| {
                let reflected = reflect(&compiled.wgsl, &compiled.reflection, family)?;
                Ok((compiled, reflected))
            });

            let _ = sender.send(Finished {
                unit: index,
                result,
            });
        });

    if let Err(error) = spawned {
        let unit = &mut programs.units[index];
        unit.busy = false;
        unit.state = UnitState::Failed;
        unit.diagnostics = format!("a thread to compile on could not be started: {error}");
    }
}

/// Takes in finished compiles, and looks for files that changed.
pub fn update(mut programs: ResMut<ShaderPrograms>, mut shaders: ResMut<Assets<Shader>>) {
    let finished = match programs.receiver.lock() {
        Ok(receiver) => receiver.try_iter().collect::<Vec<_>>(),
        Err(_) => Vec::new(),
    };

    for done in finished {
        finish(&mut programs, &mut shaders, done);
    }

    if programs.last_poll.elapsed() < POLL_INTERVAL {
        return;
    }

    programs.last_poll = Instant::now();
    poll(&mut programs);
}

/// Puts a finished compile where the pipelines will find it.
fn finish(programs: &mut ShaderPrograms, shaders: &mut Assets<Shader>, done: Finished) {
    let Some(unit) = programs.units.get_mut(done.unit) else {
        return;
    };

    unit.busy = false;
    unit.compiles += 1;

    // Named after the file, the role, the entry point and the compile, because naga_oil registers
    // a shader under its name, and two different shaders under one name would be taken for the
    // same module.
    let name = format!(
        "{}#{}:{}:{}",
        unit.path,
        unit.role.describe().replace(' ', "_"),
        unit.request.entry,
        unit.compiles
    );

    match done.result {
        Ok((compiled, reflected)) => {
            unit.shader = Some(shaders.add(Shader::from_wgsl(reflected.wgsl, name)));
            unit.layout = reflected.layout;

            if unit.compiled {
                bevy::log::info!("Recompiled {} ({})", unit.path, unit.role.describe());
            }

            if !compiled.warnings.is_empty() {
                bevy::log::warn!("{}: {}", unit.path, compiled.warnings);
            }

            unit.state = UnitState::Ready;
            unit.compiled = true;
            unit.diagnostics = compiled.warnings;

            let mut watched = vec![unit.request.file.clone()];
            watched.extend(compiled.dependencies);

            // Kept from before the compile where the file was already known, so an edit made
            // while it ran still reads as a change.
            unit.watched = watched
                .into_iter()
                .map(|path| {
                    let seen = unit
                        .watched
                        .iter()
                        .find(|(known, _)| *known == path)
                        .map(|(_, seen)| *seen)
                        .unwrap_or_else(|| fingerprint(&path));
                    (path, seen)
                })
                .collect();
        }

        Err(message) => {
            bevy::log::error!(
                "{} ({}) did not compile. {}",
                unit.path,
                unit.role.describe(),
                if unit.compiled {
                    "The last version that did stays in use."
                } else {
                    "It draws with a fallback until it does."
                }
            );
            bevy::log::error!("{message}");

            if !unit.compiled {
                let fallback = fallback_source(unit.role, &unit.request.entry);
                unit.shader = Some(shaders.add(Shader::from_wgsl(fallback, name)));
                unit.layout = Layout {
                    group: unit.role.family().own_group(),
                    ..Default::default()
                };
            }

            unit.state = UnitState::Failed;
            unit.diagnostics = message;
        }
    }

    let stale = unit.stale;
    let users = unit.programs.clone();

    for program in users {
        rebuild(programs, program);
    }

    if stale {
        start(programs, done.unit);
    }
}

/// Brings a program's entry in the table up to date with its units.
///
/// Nothing is published until every stage has a result, compiled or fallback, so the render side
/// never builds from half a program.
fn rebuild(programs: &mut ShaderPrograms, id: usize) {
    let program = &programs.programs[id];

    let pending = program
        .stages
        .iter()
        .flatten()
        .any(|unit| programs.units[*unit].shader.is_none());

    if pending {
        return;
    }

    let mut entry = PipelineProgram::default();
    let mut material: Option<Layout> = None;
    let mut draw: Option<Layout> = None;
    let mut problem = String::new();

    for role in Role::ALL {
        let Some(unit) = program.stages[role as usize].map(|unit| &programs.units[unit]) else {
            continue;
        };

        entry.stages[role as usize] = Some(StageBinding {
            shader: unit.shader.clone().expect("every stage has a shader"),
            entry: Cow::Owned(unit.request.entry.clone()),
        });

        // Both stages of a material, and both of a draw, share one group, so their layouts merge.
        let merged_into = match role {
            Role::DrawVertex | Role::DrawFragment => Some(&mut draw),
            _ if role.family() == Family::Material => Some(&mut material),
            _ => None,
        };

        match merged_into {
            Some(slot) => match slot.as_mut() {
                None => *slot = Some(unit.layout.clone()),
                Some(merged) => {
                    if let Err(message) = merged.merge(&unit.layout) {
                        problem = message;
                    }
                }
            },
            None if role.family() == Family::Pass => {
                entry.pass = Some(Arc::new(unit.layout.clone()))
            }
            None => entry.compute = Some(Arc::new(unit.layout.clone())),
        }
    }

    if problem.is_empty() {
        entry.material = material.map(Arc::new);
        entry.draw = draw.map(Arc::new);
    } else {
        bevy::log::error!("{}: {problem}", program.description);
    }

    let generation = program.generation + 1;
    entry.generation = generation;

    let program = &mut programs.programs[id];
    program.generation = generation;
    program.problem = problem;

    if let Ok(mut table) = TABLE.write()
        && let Some(slot) = table.get_mut(id)
    {
        *slot = entry;
    }

    programs.changed.push(id as u32);
}

/// Recompiles every unit a file of which has changed.
fn poll(programs: &mut ShaderPrograms) {
    for index in 0..programs.units.len() {
        let unit = &mut programs.units[index];

        let changed = unit
            .watched
            .iter()
            .any(|(path, seen)| fingerprint(path) != *seen);

        if !changed {
            continue;
        }

        if unit.busy {
            unit.stale = true;
        } else {
            start(programs, index);
        }
    }
}

// -- Fallbacks

/// A shader standing in for a stage that has never compiled, with the entry point it asked for.
fn fallback_source(role: Role, entry: &str) -> String {
    match role {
        // Magenta, reading nothing but the position, so it is valid after any vertex shader and
        // in a pass as well as in a material.
        Role::Fragment | Role::Pass | Role::DrawFragment => format!(
            "@fragment\nfn {entry}(@builtin(position) position: vec4<f32>) -> @location(0) vec4<f32> {{\n    \
             let checker = (u32(position.x / 8.0) + u32(position.y / 8.0)) % 2u;\n    \
             return select(vec4<f32>(1.0, 0.0, 1.0, 1.0), vec4<f32>(0.1, 0.0, 0.1, 1.0), checker == 1u);\n}}\n"
        ),

        Role::Vertex => format!(
            r#"#import bevy_pbr::{{
    mesh_functions,
    forward_io::{{Vertex, VertexOutput}},
    view_transformations::position_world_to_clip,
}}

@vertex
fn {entry}(vertex: Vertex) -> VertexOutput {{
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(vertex.position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
#ifdef VERTEX_NORMALS
    out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
#endif
#ifdef VERTEX_UVS_A
    out.uv = vertex.uv;
#endif
#ifdef VERTEX_UVS_B
    out.uv_b = vertex.uv_b;
#endif
#ifdef VERTEX_TANGENTS
    out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
#endif
#ifdef VERTEX_COLORS
    out.color = vertex.color;
#endif
#ifdef VERTEX_OUTPUT_INSTANCE_INDEX
    out.instance_index = vertex.instance_index;
#endif
    return out;
}}
"#
        ),

        Role::PrepassVertex => format!(
            r#"#import bevy_pbr::{{
    mesh_functions,
    prepass_io::{{Vertex, VertexOutput}},
    view_transformations::position_world_to_clip,
}}

@vertex
fn {entry}(vertex: Vertex) -> VertexOutput {{
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    out.world_position = mesh_functions::mesh_position_local_to_world(world_from_local, vec4<f32>(vertex.position, 1.0));
    out.position = position_world_to_clip(out.world_position.xyz);
#ifdef UNCLIPPED_DEPTH_ORTHO_EMULATION
    out.unclipped_depth = out.position.z;
    out.position.z = min(out.position.z, 1.0);
#endif
#ifdef VERTEX_UVS_A
    out.uv = vertex.uv;
#endif
#ifdef VERTEX_UVS_B
    out.uv_b = vertex.uv_b;
#endif
#ifdef NORMAL_PREPASS_OR_DEFERRED_PREPASS
#ifdef VERTEX_NORMALS
    out.world_normal = mesh_functions::mesh_normal_local_to_world(vertex.normal, vertex.instance_index);
#endif
#ifdef VERTEX_TANGENTS
    out.world_tangent = mesh_functions::mesh_tangent_local_to_world(world_from_local, vertex.tangent, vertex.instance_index);
#endif
#endif
#ifdef MOTION_VECTOR_PREPASS_OR_DEFERRED_PREPASS
    let previous_world_from_local = mesh_functions::get_previous_world_from_local(vertex.instance_index);
    out.previous_world_position = mesh_functions::mesh_position_local_to_world(previous_world_from_local, vec4<f32>(vertex.position, 1.0));
#endif
#ifdef VERTEX_OUTPUT_INSTANCE_INDEX
    out.instance_index = vertex.instance_index;
#endif
    return out;
}}
"#
        ),

        // Every vertex at one point, which draws nothing, since what a draw's vertex shader reads
        // to place its geometry is not known here.
        Role::DrawVertex => format!(
            "@vertex\nfn {entry}(@builtin(vertex_index) index: u32) -> @builtin(position) vec4<f32> {{\n    \
             return vec4<f32>(0.0, 0.0, 0.0, 1.0);\n}}\n"
        ),

        // Does nothing, which is the only thing a compute shader can safely do without knowing
        // what the buffers it was handed hold.
        Role::Compute => {
            format!("@compute @workgroup_size(1)\nfn {entry}() {{\n}}\n")
        }

        // What Bevy's own prepass writes, which is a normal and a motion vector where the camera
        // asked for them, and nothing where it did not.
        Role::PrepassFragment => format!(
            r#"#import bevy_pbr::{{
    prepass_io::{{VertexOutput, FragmentOutput}},
    prepass_bindings,
    mesh_view_bindings::view,
}}

#ifdef PREPASS_FRAGMENT
@fragment
fn {entry}(in: VertexOutput) -> FragmentOutput {{
    var out: FragmentOutput;
#ifdef NORMAL_PREPASS
    out.normal = vec4(in.world_normal * 0.5 + vec3(0.5), 1.0);
#endif
#ifdef UNCLIPPED_DEPTH_ORTHO_EMULATION
    out.frag_depth = in.unclipped_depth;
#endif
#ifdef MOTION_VECTOR_PREPASS
    let clip_position_t = view.unjittered_clip_from_world * in.world_position;
    let clip_position = clip_position_t.xy / clip_position_t.w;
    let previous_clip_position_t = prepass_bindings::previous_view_uniforms.clip_from_world * in.previous_world_position;
    let previous_clip_position = previous_clip_position_t.xy / previous_clip_position_t.w;
    out.motion_vector = (clip_position - previous_clip_position) * vec2(0.5, -0.5);
#endif
    return out;
}}
#else
@fragment
fn {entry}(@builtin(position) position: vec4<f32>) {{
}}
#endif
"#
        ),
    }
}

// -- What callers ask

/// The program at a number, where there is one.
fn program(world: &World, id: i32) -> Result<(&ShaderPrograms, &Program), i32> {
    let programs = world
        .get_resource::<ShaderPrograms>()
        .ok_or(status::UNSUPPORTED)?;

    let program = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
        .ok_or(status::NO_COMPONENT)?;

    Ok((programs, program))
}

/// Whether a program can run yet: `0` still compiling, `1` ready, `2` failed.
///
/// A failed program may still run, with the last version that compiled or with a fallback, and
/// failed is still the answer, because what is on disk is not what is on screen.
pub fn state(world: &World, id: i32) -> i32 {
    let (programs, program) = match program(world, id) {
        Ok(found) => found,
        Err(refusal) => return refusal,
    };

    let mut compiling = false;
    let mut failed = !program.problem.is_empty();

    for unit in program.stages.iter().flatten() {
        match programs.units[*unit].state {
            UnitState::Compiling => compiling = true,
            UnitState::Failed => failed = true,
            UnitState::Ready => {}
        }
    }

    // A compute stage whose pipeline has not been built counts as compiling, however the shader
    // itself stands, because that is what a dispatch sees.
    if program.stages[Role::Compute as usize].is_some()
        && !compute_ready(id as usize, program.generation)
    {
        compiling = true;
    }

    if failed {
        2
    } else if compiling {
        0
    } else {
        1
    }
}

/// What went wrong with a program, or what its compiler warned about, one stage per paragraph.
pub fn diagnostics(world: &World, id: i32) -> Option<String> {
    let (programs, program) = program(world, id).ok()?;
    let mut text = Vec::new();

    for unit in program.stages.iter().flatten() {
        let unit = &programs.units[*unit];

        if !unit.diagnostics.is_empty() {
            text.push(format!(
                "{} ({}):\n{}",
                unit.path,
                unit.role.describe(),
                unit.diagnostics
            ));
        }
    }

    if !program.problem.is_empty() {
        text.push(program.problem.clone());
    }

    Some(text.join("\n\n"))
}

/// Which files a program is made of.
pub fn describe(world: &World, id: i32) -> Option<String> {
    Some(program(world, id).ok()?.1.description.clone())
}

/// What a program's shaders declare, a line each: its binding, name and kind.
pub fn describe_layout(id: i32) -> Option<String> {
    let entry = lookup(u32::try_from(id).ok()?)?;
    let mut lines = Vec::new();

    for (title, layout) in [
        ("material", &entry.material),
        ("pass", &entry.pass),
        ("compute", &entry.compute),
    ] {
        let Some(layout) = layout else {
            continue;
        };

        lines.push(format!("{title}, group {}:", layout.group));

        for (number, binding) in &layout.bindings {
            match &binding.kind {
                super::reflect::BindingKind::Uniform { fields, size } => {
                    lines.push(format!("  {number}: numbers, {size} bytes"));
                    let prefix = if binding.name == super::reflect::LOOSE {
                        String::new()
                    } else {
                        format!("{}.", binding.name)
                    };
                    for field in fields {
                        lines.push(format!(
                            "     {prefix}{} {} at {}",
                            field.name,
                            field.ty.describe(),
                            field.offset
                        ));
                    }
                }
                _ => lines.push(format!("  {number}: {} {}", binding.name, binding.describe())),
            }
        }
    }

    Some(lines.join("\n"))
}

/// How many programs there are.
pub fn count(world: &World) -> i32 {
    world
        .get_resource::<ShaderPrograms>()
        .map(|programs| programs.programs.len() as i32)
        .unwrap_or(0)
}

/// How many times the program's shaders have been replaced, counting the first time.
///
/// Only ever grows, so a caller that wants to know when an edit has reached the pipelines reads it
/// before the edit and waits for it to move.
pub fn generation(world: &World, id: i32) -> i32 {
    match program(world, id) {
        Ok((_, program)) => program.generation.min(i32::MAX as u32) as i32,
        Err(refusal) => refusal,
    }
}

/// Compiles every stage of a program again now, whether or not anything changed.
pub fn reload(world: &mut World, id: i32) -> i32 {
    let Some(mut programs) = world.get_resource_mut::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let Some(program) = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
    else {
        return status::NO_COMPONENT;
    };

    let units: Vec<usize> = program.stages.iter().flatten().copied().collect();

    for unit in units {
        if programs.units[unit].busy {
            programs.units[unit].stale = true;
        } else {
            start(&mut programs, unit);
        }
    }

    status::OK
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_false_define_is_left_out() {
        assert_eq!(Define::Bool("A".into(), false).to_slang(), None);
        assert_eq!(
            Define::Bool("A".into(), true).to_slang(),
            Some(("A".into(), "1".into()))
        );
        assert_eq!(
            Define::Int("B".into(), -3).to_slang(),
            Some(("B".into(), "-3".into()))
        );
    }

    #[test]
    fn every_fallback_has_the_entry_point_it_was_asked_for() {
        for role in Role::ALL {
            assert!(fallback_source(role, "custom_entry").contains("fn custom_entry("));
        }
    }

    #[test]
    fn each_role_puts_its_globals_where_its_pipeline_binds_them() {
        assert_eq!(Role::Fragment.family().own_group(), 3);
        assert_eq!(Role::PrepassVertex.family().own_group(), 3);
        assert_eq!(Role::Pass.family().own_group(), 0);
        assert_eq!(Role::Compute.family().own_group(), 0);
    }
}

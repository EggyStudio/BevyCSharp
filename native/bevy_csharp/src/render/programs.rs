//! Shader programs: which shaders draw a material, compiled and kept current while the app runs.
//!
//! A program names up to five stages, each a file and an entry point: the vertex and fragment
//! shaders of the main pass, the vertex and fragment shaders of the prepass that draws depth for
//! shadows, and a compute shader that runs outside of any picture. A stage left out is Bevy's own. A program is a number, and a material says which
//! number draws it, so there is no limit on how many a game has.
//!
//! **Why a table outside the world.** Bevy asks a material's type rather than the material for
//! its shaders, and the one place it hands over something of the instance is the pipeline key
//! passed to `Material::specialize`. That function is static and runs on the render side, so what
//! it looks the program up in has to be reachable without a world, which is [`TABLE`]. The world
//! keeps the rest ([`ShaderPrograms`]): the compile jobs, the files to watch and what went wrong.
//!
//! **Hot reload.** Bevy's pipeline cache rebuilds every pipeline using a shader when that shader's
//! asset changes, so reloading is a matter of replacing the asset. A WGSL file is reloaded through
//! the asset server, which is what its watcher does in the editor profile and what [`update`]
//! does by polling in the others. A Slang file is recompiled here, off the main thread, and the
//! result replaces the asset. Every file a Slang shader imported is watched too, which the asset
//! server alone could not do, since it never saw the imports.
//!
//! **When a Slang file fails.** The last good result stays, so a typo in a running game changes
//! nothing on screen and says what is wrong in the log. A stage that has never compiled draws with
//! a fallback instead of drawing nothing: magenta for a fragment shader, which is conspicuous on
//! purpose, and Bevy's own behavior for a vertex shader. The fallback has the entry point the stage
//! asked for, because the pipeline is built naming it and is not rebuilt when the shader changes.

#![cfg(feature = "render")]

use std::borrow::Cow;
use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::sync::mpsc::{Receiver, Sender, channel};
use std::sync::{Mutex, RwLock};
use std::time::{Duration, Instant};

use bevy::asset::{AssetEvent, AssetServer, Assets, Handle, LoadState};
use bevy::ecs::message::MessageReader;
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Res, ResMut};
use bevy::ecs::world::World;
use bevy::shader::{Shader, ShaderDefVal};

use super::slang;
use crate::interop::status;

/// Which stage of which pass a shader fills.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum Role {
    Vertex = 0,
    Fragment = 1,
    PrepassVertex = 2,
    PrepassFragment = 3,
    Compute = 4,
}

/// How many roles a program has.
pub const ROLE_COUNT: usize = 5;

impl Role {
    const ALL: [Role; ROLE_COUNT] = [
        Role::Vertex,
        Role::Fragment,
        Role::PrepassVertex,
        Role::PrepassFragment,
        Role::Compute,
    ];

    /// The entry point a stage has when the program does not name one, which is what Bevy's own
    /// shaders call theirs.
    fn default_entry(self) -> &'static str {
        match self {
            Role::Vertex | Role::PrepassVertex => "vertex",
            Role::Fragment | Role::PrepassFragment => "fragment",
            Role::Compute => "main",
        }
    }

    fn stage(self) -> slang::Stage {
        match self {
            Role::Vertex | Role::PrepassVertex => slang::Stage::Vertex,
            Role::Fragment | Role::PrepassFragment => slang::Stage::Fragment,
            Role::Compute => slang::Stage::Compute,
        }
    }

    fn describe(self) -> &'static str {
        match self {
            Role::Vertex => "vertex",
            Role::Fragment => "fragment",
            Role::PrepassVertex => "prepass vertex",
            Role::PrepassFragment => "prepass fragment",
            Role::Compute => "compute",
        }
    }
}

/// One stage as the pipeline sees it: a shader and the entry point in it.
#[derive(Clone, Debug)]
pub struct StageBinding {
    pub shader: Handle<Shader>,
    pub entry: Cow<'static, str>,
}

/// What `specialize` needs of a program.
#[derive(Clone, Debug, Default)]
pub struct PipelineProgram {
    pub stages: [Option<StageBinding>; ROLE_COUNT],
    pub defs: Vec<ShaderDefVal>,
}

/// Every program the running app has made, by number.
///
/// Process-wide because `specialize` is a static function with no world to look in. Emptied when
/// an app is built, so the numbers a new app hands out never name a program of an app before it,
/// whose shader handles belong to an asset server that no longer exists.
static TABLE: RwLock<Vec<PipelineProgram>> = RwLock::new(Vec::new());

/// Looks a program up for a pipeline.
pub fn lookup(id: u32) -> Option<PipelineProgram> {
    TABLE.read().ok()?.get(id as usize).cloned()
}

/// How many programs there are, which is where the render side's look through them stops.
pub fn table_len() -> u32 {
    TABLE.read().map(|table| table.len() as u32).unwrap_or(0)
}

/// The programs whose compute pipeline has been built.
///
/// Written by the render side, which is the only side that can see the pipeline cache, and read by
/// [`state`], because a dispatch made before its pipeline exists does nothing, and a program is not
/// ready while that is still true of it.
static COMPUTE_READY: RwLock<Vec<bool>> = RwLock::new(Vec::new());

/// Says that a program's compute pipeline has been built.
pub fn mark_compute_ready(id: u32) {
    if let Ok(mut ready) = COMPUTE_READY.write() {
        let index = id as usize;

        if ready.len() <= index {
            ready.resize(index + 1, false);
        }

        ready[index] = true;
    }
}

fn compute_ready(id: usize) -> bool {
    COMPUTE_READY
        .read()
        .map(|ready| ready.get(id).copied().unwrap_or(false))
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

    /// The same define as naga_oil takes it, or `None` for a false one.
    ///
    /// Left out rather than passed as false, because naga_oil's `#ifdef` asks whether a name is
    /// present and not what it holds, so a false boolean would read as defined. Leaving it out is
    /// what Bevy does with its own, and it makes a define mean the same to WGSL as to Slang.
    fn to_shader_def(&self) -> Option<ShaderDefVal> {
        match self {
            Define::Bool(_, false) => None,
            Define::Bool(name, true) => Some(ShaderDefVal::Bool(name.clone(), true)),
            Define::Int(name, value) => Some(ShaderDefVal::Int(name.clone(), *value)),
            Define::UInt(name, value) => Some(ShaderDefVal::UInt(name.clone(), *value)),
        }
    }

    /// The same define as `slangc` takes it, or `None` for a false one, for the same reason as
    /// [`Define::to_shader_def`].
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
    /// A `.wgsl` or `.slang` file under the asset root.
    Path(String),
    /// WGSL source, handed over as text.
    Wgsl(String),
    /// Slang source, handed over as text.
    Slang(String),
}

impl StageFile {
    /// What messages call it: the path, or a name made from the text's hash.
    fn describe(&self) -> String {
        match self {
            StageFile::Path(path) => path.clone(),
            StageFile::Wgsl(source) => format!("inline WGSL {:08x}", slang::fnv(source.as_bytes()) as u32),
            StageFile::Slang(source) => {
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

/// Where a stage's shader comes from.
enum Source {
    /// Compiled here, by the unit at this index.
    Slang(usize),
    /// Made from WGSL handed over as text, which there is nothing to load or watch for.
    Inline(Handle<Shader>),
    /// Loaded by the asset server, from this path.
    Wgsl(String, Handle<Shader>),
}

/// What the world remembers of one program.
struct Program {
    stages: [Option<Source>; ROLE_COUNT],
    description: String,
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
/// Shared between programs that ask for the same thing, so ten materials naming one shader cost
/// one compile rather than ten.
struct Unit {
    request: slang::Request,
    role: Role,
    /// The path the program named it by, which is what messages call it.
    path: String,
    handle: Handle<Shader>,
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
}

/// A finished compile, on its way back to the main thread.
struct Finished {
    unit: usize,
    result: Result<slang::Compiled, String>,
}

/// A WGSL file the asset server loaded, with what it held when last looked at.
struct WatchedWgsl {
    file: PathBuf,
    fingerprint: Option<u64>,
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
    wgsl: HashMap<bevy::asset::AssetId<Shader>, WatchedWgsl>,
    last_poll: Instant,
    /// How many times each shader has been replaced, which is what a caller waiting for a reload
    /// can watch.
    generations: HashMap<bevy::asset::AssetId<Shader>, u32>,
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
            wgsl: HashMap::new(),
            last_poll: Instant::now(),
            generations: HashMap::new(),
        }
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
/// Returns the program's number, or a negative status where the description names neither a
/// fragment shader nor a compute shader, or names a file that is not a shader.
pub fn create(world: &mut World, description: ProgramDescription) -> i32 {
    if description.stages[Role::Fragment as usize].is_none()
        && description.stages[Role::Compute as usize].is_none()
    {
        return status::NULL_ARG;
    }

    let Some(server) = world.get_resource::<AssetServer>().cloned() else {
        return status::UNSUPPORTED;
    };

    let Some(mut programs) = world.remove_resource::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let answer = {
        let Some(mut shaders) = world.get_resource_mut::<Assets<Shader>>() else {
            world.insert_resource(programs);
            return status::UNSUPPORTED;
        };

        create_in(&mut programs, &server, &mut shaders, description)
    };

    world.insert_resource(programs);
    answer
}

fn create_in(
    programs: &mut ShaderPrograms,
    server: &AssetServer,
    shaders: &mut Assets<Shader>,
    description: ProgramDescription,
) -> i32 {
    if let Some(&id) = programs.by_description.get(&description) {
        return id;
    }

    // Names are checked before anything is started, so a program that is refused leaves no
    // compile running behind it.
    for (file, _) in description.stages.iter().flatten() {
        let StageFile::Path(path) = file else {
            continue;
        };

        let lower = path.to_ascii_lowercase();

        if !lower.ends_with(".slang") && !lower.ends_with(".wgsl") {
            return status::NO_COMPONENT;
        }
    }

    for define in &description.defines {
        if define.name().is_empty() || define.name().contains(char::is_whitespace) {
            return status::NULL_ARG;
        }
    }

    let mut sources: [Option<Source>; ROLE_COUNT] = Default::default();
    let mut bindings: [Option<StageBinding>; ROLE_COUNT] = Default::default();

    for role in Role::ALL {
        let Some((file, entry)) = &description.stages[role as usize] else {
            continue;
        };

        let entry = entry
            .clone()
            .filter(|entry| !entry.is_empty())
            .unwrap_or_else(|| role.default_entry().to_string());

        let (source, handle) = match file {
            StageFile::Path(path) if path.to_ascii_lowercase().ends_with(".slang") => {
                let unit = unit_for(
                    programs,
                    shaders,
                    role,
                    programs.root.join(path),
                    path.clone(),
                    &entry,
                    &description.defines,
                );
                (Source::Slang(unit), programs.units[unit].handle.clone())
            }
            StageFile::Path(path) => {
                let handle: Handle<Shader> = server.load(path.clone());
                (Source::Wgsl(path.clone(), handle.clone()), handle)
            }
            StageFile::Wgsl(text) => {
                // Named after its hash, because naga_oil registers a shader under its name and two
                // different texts under one name would be taken for the same module.
                let name = format!("bevy_csharp/inline/{:016x}.wgsl", slang::fnv(text.as_bytes()));
                let handle = shaders.add(Shader::from_wgsl(text.clone(), name));
                (Source::Inline(handle.clone()), handle)
            }
            StageFile::Slang(text) => {
                // Written out, because slangc compiles files, and compiled like any other file
                // from there. Its imports are looked for under the asset root, as a file's are.
                let path = match slang::inline_file(text) {
                    Ok(path) => path,
                    Err(message) => {
                        bevy::log::error!("{message}");
                        return status::INVALID_STATE;
                    }
                };

                let unit = unit_for(
                    programs,
                    shaders,
                    role,
                    path,
                    file.describe(),
                    &entry,
                    &description.defines,
                );
                (Source::Slang(unit), programs.units[unit].handle.clone())
            }
        };

        sources[role as usize] = Some(source);
        bindings[role as usize] = Some(StageBinding {
            shader: handle,
            entry: Cow::Owned(entry),
        });
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

    let defs = description
        .defines
        .iter()
        .filter_map(Define::to_shader_def)
        .collect();

    let Ok(mut table) = TABLE.write() else {
        return status::INVALID_STATE;
    };

    // The table and the world's list grow together, so a number is an index into both.
    let id = programs.programs.len() as i32;
    debug_assert_eq!(table.len(), programs.programs.len());

    table.push(PipelineProgram {
        stages: bindings,
        defs,
    });

    programs.programs.push(Program {
        stages: sources,
        description: text,
    });

    programs.by_description.insert(description, id);
    id
}

/// Finds the unit compiling this entry point, or starts one.
fn unit_for(
    programs: &mut ShaderPrograms,
    shaders: &mut Assets<Shader>,
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
        // Reserved rather than filled, because there is nothing to fill it with until the compile
        // finishes. A pipeline naming it waits, which is what a pipeline does for any shader that
        // has not loaded.
        handle: shaders.reserve_handle(),
        state: UnitState::Compiling,
        diagnostics: String::new(),
        compiled: false,
        busy: false,
        stale: false,
        watched: vec![(file, None)],
    });

    programs.unit_by_key.insert(key, index);
    start(programs, index);
    index
}

/// Starts compiling a unit on a thread of its own.
///
/// A thread rather than a task on one of Bevy's pools, because the work is waiting on a process,
/// and a pool thread parked on one is a thread the pool's other work cannot use.
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
    let sender = programs.sender.clone();

    let spawned = std::thread::Builder::new()
        .name("bcs-slangc".into())
        .spawn(move || {
            let result = slang::compile(&request);
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
pub fn update(
    mut programs: ResMut<ShaderPrograms>,
    mut shaders: ResMut<Assets<Shader>>,
    server: Res<AssetServer>,
    mut events: MessageReader<AssetEvent<Shader>>,
) {
    for event in events.read() {
        if let AssetEvent::Added { id } | AssetEvent::Modified { id } = event {
            *programs.generations.entry(*id).or_default() += 1;
        }
    }

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
    poll_slang(&mut programs);

    // With a watcher the asset server reloads on its own, and doing it here as well would reload
    // every edit twice.
    if !server.watching_for_changes() {
        poll_wgsl(&mut programs, &shaders, &server);
    }
}

/// Puts a finished compile where the pipelines will find it.
fn finish(programs: &mut ShaderPrograms, shaders: &mut Assets<Shader>, done: Finished) {
    let Some(unit) = programs.units.get_mut(done.unit) else {
        return;
    };

    unit.busy = false;

    // A result whose bindings disagree with what the bridge binds is a failure like any other,
    // because a pipeline built from it would fail where nothing could say which shader was wrong.
    let stage = unit.request.stage;
    let result = done.result.and_then(|compiled| {
        super::check::check_bindings(&compiled.wgsl, stage).map(|()| compiled)
    });

    match result {
        Ok(compiled) => {
            // Named after the file, the role and the entry point, because naga_oil registers a
            // shader under its path and two different shaders under one name would be taken for
            // the same module.
            let name = format!(
                "{}#{}:{}",
                unit.path,
                unit.role.describe().replace(' ', "_"),
                unit.request.entry
            );

            let _ = shaders.insert(unit.handle.id(), Shader::from_wgsl(compiled.wgsl, name));

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
                let name = format!("{}#fallback:{}", unit.path, unit.request.entry);
                let _ = shaders.insert(unit.handle.id(), Shader::from_wgsl(fallback, name));
            }

            unit.state = UnitState::Failed;
            unit.diagnostics = message;
        }
    }

    if unit.stale {
        start(programs, done.unit);
    }
}

/// Recompiles every Slang unit a file of which has changed.
fn poll_slang(programs: &mut ShaderPrograms) {
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

/// Reloads every WGSL shader loaded from a file that has changed.
///
/// Every one the asset server holds rather than only the ones a program named, because a program's
/// shader may import another by path, and an edit to that one has to reach the pipeline too.
fn poll_wgsl(programs: &mut ShaderPrograms, shaders: &Assets<Shader>, server: &AssetServer) {
    for (id, _) in shaders.iter() {
        if programs.wgsl.contains_key(&id) {
            continue;
        }

        let Some(path) = server.get_path(id) else {
            continue;
        };

        // Only files, which leaves out every shader Bevy embeds in itself.
        if !matches!(path.source(), bevy::asset::io::AssetSourceId::Default) {
            continue;
        }

        let file = programs.root.join(path.path());
        let seen = fingerprint(&file);

        programs.wgsl.insert(
            id,
            WatchedWgsl {
                file,
                fingerprint: seen,
            },
        );
    }

    let mut reload = Vec::new();

    for (id, watched) in &mut programs.wgsl {
        let now = fingerprint(&watched.file);

        if now != watched.fingerprint {
            watched.fingerprint = now;
            reload.push(*id);
        }
    }

    for id in reload {
        if let Some(path) = server.get_path(id) {
            bevy::log::info!("Reloading {path}");
            server.reload(path.into_owned());
        }
    }
}

// -- Fallbacks

/// A shader standing in for a stage that has never compiled, with the entry point it asked for.
fn fallback_source(role: Role, entry: &str) -> String {
    match role {
        // Magenta, reading nothing but the position, so it is valid after any vertex shader and
        // in a pass as well as in a material.
        Role::Fragment => format!(
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

/// Whether a program can draw yet: `0` still compiling or loading, `1` ready, `2` failed.
///
/// A failed program may still draw, with the last version that compiled or with a fallback, and
/// failed is still the answer, because the files on disk are not what is on screen.
pub fn state(world: &World, id: i32) -> i32 {
    let Some(programs) = world.get_resource::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let Some(program) = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
    else {
        return status::NO_COMPONENT;
    };

    let server = world.get_resource::<AssetServer>();

    // A compute stage whose pipeline has not been built counts as compiling, however the shader
    // itself stands, because that is what a dispatch sees.
    let mut worst = if program.stages[Role::Compute as usize].is_some() && !compute_ready(id as usize)
    {
        0
    } else {
        1
    };

    for source in program.stages.iter().flatten() {
        let this = match source {
            Source::Slang(unit) => match programs.units[*unit].state {
                UnitState::Compiling => 0,
                UnitState::Ready => 1,
                UnitState::Failed => 2,
            },
            // Text handed over is there from the moment it is, and a mistake in it is the
            // pipeline cache's to report.
            Source::Inline(_) => 1,
            Source::Wgsl(_, handle) => match server.map(|server| server.load_state(handle.id())) {
                Some(LoadState::Loaded) => 1,
                Some(LoadState::Failed(_)) => 2,
                _ => 0,
            },
        };

        // Failed outranks compiling, which outranks ready.
        worst = match (worst, this) {
            (2, _) | (_, 2) => 2,
            (0, _) | (_, 0) => 0,
            _ => 1,
        };
    }

    worst
}

/// What went wrong with a program, or what its compiler warned about, one stage per paragraph.
pub fn diagnostics(world: &World, id: i32) -> Option<String> {
    let programs = world.get_resource::<ShaderPrograms>()?;
    let program = programs.programs.get(usize::try_from(id).ok()?)?;
    let server = world.get_resource::<AssetServer>();

    let mut text = Vec::new();

    for source in program.stages.iter().flatten() {
        match source {
            Source::Slang(unit) => {
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
            Source::Inline(_) => {}
            Source::Wgsl(path, handle) => {
                if let Some(LoadState::Failed(error)) =
                    server.map(|server| server.load_state(handle.id()))
                {
                    text.push(format!("{path}:\n{error}"));
                }
            }
        }
    }

    Some(text.join("\n\n"))
}

/// Which files a program is made of.
pub fn describe(world: &World, id: i32) -> Option<String> {
    let programs = world.get_resource::<ShaderPrograms>()?;
    let program = programs.programs.get(usize::try_from(id).ok()?)?;
    Some(program.description.clone())
}

/// How many programs there are.
pub fn count(world: &World) -> i32 {
    world
        .get_resource::<ShaderPrograms>()
        .map(|programs| programs.programs.len() as i32)
        .unwrap_or(0)
}

/// How many times the program's shaders have been replaced, counting the first time each loaded.
///
/// Only ever grows, so a caller that wants to know when an edit has reached the pipelines reads it
/// before the edit and waits for it to move.
pub fn generation(world: &World, id: i32) -> i32 {
    let Some(programs) = world.get_resource::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let Some(program) = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
    else {
        return status::NO_COMPONENT;
    };

    let total: u32 = program
        .stages
        .iter()
        .flatten()
        .map(|source| {
            let id = match source {
                Source::Slang(unit) => programs.units[*unit].handle.id(),
                Source::Wgsl(_, handle) | Source::Inline(handle) => handle.id(),
            };
            programs.generations.get(&id).copied().unwrap_or(0)
        })
        .sum();

    total.min(i32::MAX as u32) as i32
}

/// Compiles or reloads every stage of a program now, whether or not anything changed.
pub fn reload(world: &mut World, id: i32) -> i32 {
    let server = world.get_resource::<AssetServer>().cloned();

    let Some(mut programs) = world.get_resource_mut::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let Some(program) = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
    else {
        return status::NO_COMPONENT;
    };

    let mut units = Vec::new();
    let mut paths = Vec::new();

    for source in program.stages.iter().flatten() {
        match source {
            Source::Slang(unit) => units.push(*unit),
            Source::Wgsl(path, _) => paths.push(path.clone()),
            Source::Inline(_) => {}
        }
    }

    for unit in units {
        if programs.units[unit].busy {
            programs.units[unit].stale = true;
        } else {
            start(&mut programs, unit);
        }
    }

    if let Some(server) = server {
        for path in paths {
            server.reload(path);
        }
    }

    status::OK
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_false_define_is_left_out_of_both_languages() {
        assert!(Define::Bool("A".into(), false).to_shader_def().is_none());
        assert!(Define::Bool("A".into(), true).to_shader_def().is_some());
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
}

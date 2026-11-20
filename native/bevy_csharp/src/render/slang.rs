//! Shaders are written in Slang and compiled to WGSL, which Bevy's pipeline cache takes.
//!
//! WGSL rather than SPIR-V, because Bevy takes WGSL with no extra feature, as WESL where the bridge
//! puts Bevy's imports in front of it, it runs on every backend including WebGPU, and a pipeline
//! built from it is rebuilt when the shader asset changes. Slang's WGSL backend keeps explicit
//! bindings, and it numbers stage inputs and outputs by semantic index, which lets a Slang fragment
//! shader follow Bevy's own vertex shader.
//!
//! A compute shader can ask for SPIR-V instead, for what WGSL cannot say, such as a ray query.
//! That is compiled the same way with a different target, and the binary is handed to the driver
//! as it is (see [`super::reflect::reflect_spirv`]).
//!
//! Every compile also asks `slangc` for its reflection, which names each parameter the shader
//! declares and where in a uniform buffer each number goes. That lets a shader declare whatever it
//! likes and be handed it by name (see [`super::reflect`]).
//!
//! The compiler is `slangc`, run as a process. Linking Slang instead would add a large C++ library
//! to every build of the bridge for the benefit of the builds that compile shaders, and a process
//! is also what keeps a compiler crash out of the game.
//!
//! **Where `slangc` comes from.** `BCS_SLANGC` names it outright, and otherwise it is looked up on
//! the `PATH`, and then in `build/tools/slang/bin` of the checkout the running program or the
//! working directory is in, which is where `build/fetch-slang.sh` puts it. A machine with none of
//! those still
//! runs a game whose shaders were compiled once, because every successful compile is written to a
//! cache, beside the assets unless the managed side names another folder, and read back when there
//! is no compiler. A hash of the source and of
//! every file it imported decides whether a cached result still applies, so a shipped game with
//! its cache needs no compiler, and a stale entry is never used.
//!
//! Built in every profile, although only a render build compiles shaders, because it needs nothing
//! but the standard library and its tests are then run by the test job, which builds headless.

#![cfg_attr(not(feature = "render"), allow(dead_code))]

use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::{Mutex, OnceLock, PoisonError};
use std::sync::atomic::{AtomicU64, Ordering};

mod cache;

pub use cache::fnv;

use cache::{cache_key, read_cache, write_cache};

/// What every Slang shader can import as `bcs`.
pub const PRELUDE: &str = include_str!("bcs.slang");

/// What a Slang shader drawing a 2D mesh can import as `bcs2d`.
pub const PRELUDE_2D: &str = include_str!("bcs2d.slang");

/// What a Slang shader drawing an interface node can import as `bcs_ui`.
pub const UI_PRELUDE: &str = include_str!("bcs_ui.slang");

/// What a Slang shader drawing a sprite can import as `bcs_sprite`, the sprite's image, color and
/// cutting, with Bevy's functions for them.
pub const SPRITE_PRELUDE: &str = include_str!("bcs_sprite.slang");

/// What a Slang shader pass can import as `bcs_pass`.
pub const PASS_PRELUDE: &str = include_str!("bcs_pass.slang");

/// The part of the pass module about the deferred buffers and a shadow stage's depth, which
/// `bcs_pass.slang` includes.
pub const PASS_DEFERRED: &str = include_str!("bcs_pass_deferred.slang");

/// The part of the pass module about Bevy's light probes as its lighting reads them, which
/// `bcs_pass.slang` includes.
pub const PASS_PROBES: &str = include_str!("bcs_pass_probes.slang");

/// What a Slang compute shader can import as `bcs_compute`.
pub const COMPUTE_PRELUDE: &str = include_str!("bcs_compute.slang");

/// What any Slang shader can import as `bcs_scene`, the layout of what the engine writes about the
/// scene into buffers, such as instance transforms.
pub const SCENE_PRELUDE: &str = include_str!("bcs_scene.slang");

/// What a compute shader compiled to SPIR-V can import as `bcs_ray`, Solari's scene, to trace rays
/// against.
pub const RAY_PRELUDE: &str = include_str!("bcs_ray.slang");

/// What any Slang shader can import as `bcs_field`, the scene's distance field as a shader that
/// reads it sees it.
pub const FIELD_PRELUDE: &str = include_str!("bcs_field.slang");

/// What any Slang shader can import as `bcs_gi`, what the passes of global illumination share, the
/// octahedron of directions, rays through the field and the light the world's probes hold.
pub const GI_PRELUDE: &str = include_str!("bcs_gi.slang");

/// What any Slang shader can import as `bcs_glow`, the small emissive surfaces global illumination
/// carries as lights of their own, their light in closed form and the marches that shadow it.
pub const GLOW_PRELUDE: &str = include_str!("bcs_glow.slang");

/// What `bcs_gi` imports as `rc_octahedron`, the probes' directions on the octahedron folded flat,
/// the rays a texel sends and the marks its rays leave, as the RadianceCascades module writes them
/// once for both engines, read from its checkout under `modules`.
pub const RC_OCTAHEDRON: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_octahedron.slang");

/// What `bcs_field` imports as `rc_field_code`, the way a face looks in eight bits and the codes a
/// cell of the field keeps, as the RadianceCascades module writes them once for both engines.
pub const RC_FIELD_CODE: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_field_code.slang");

/// What `bcs_field` imports as `rc_field_paint`, a face's gloss in the bits above its way and the
/// field's paint as the RadianceCascades module reads them once for both engines.
pub const RC_FIELD_PAINT: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_field_paint.slang");

/// The RadianceCascades module's field read, probes, merge and passes, which the probes' kernels
/// import as `rc_probe_passes` and it imports in turn, written once there for both engines.
pub const RC_FIELD_READ: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_field_read.slang");
pub const RC_PROBES: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_probes.slang");
pub const RC_CASCADE_MERGE: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_cascade_merge.slang");
pub const RC_PROBE_PASSES: &str = include_str!("../../../../modules/RadianceCascades/RadianceCascades/Shaders/rc_probe_passes.slang");

/// Every module the bridge writes out, by file name, which `import` finds them by.
const MODULES: [(&str, &str); 20] = [
    ("bcs.slang", PRELUDE),
    ("bcs2d.slang", PRELUDE_2D),
    ("bcs_ui.slang", UI_PRELUDE),
    ("bcs_sprite.slang", SPRITE_PRELUDE),
    ("bcs_pass.slang", PASS_PRELUDE),
    ("bcs_pass_deferred.slang", PASS_DEFERRED),
    ("bcs_pass_probes.slang", PASS_PROBES),
    ("bcs_compute.slang", COMPUTE_PRELUDE),
    ("bcs_scene.slang", SCENE_PRELUDE),
    ("bcs_ray.slang", RAY_PRELUDE),
    ("bcs_field.slang", FIELD_PRELUDE),
    ("bcs_gi.slang", GI_PRELUDE),
    ("bcs_glow.slang", GLOW_PRELUDE),
    ("rc_octahedron.slang", RC_OCTAHEDRON),
    ("rc_field_code.slang", RC_FIELD_CODE),
    ("rc_field_paint.slang", RC_FIELD_PAINT),
    ("rc_field_read.slang", RC_FIELD_READ),
    ("rc_probes.slang", RC_PROBES),
    ("rc_cascade_merge.slang", RC_CASCADE_MERGE),
    ("rc_probe_passes.slang", RC_PROBE_PASSES),
];

/// A hash of every module the bridge writes, which a cache entry and the scratch directory are
/// both named after, so a change to any of them is a different compile.
fn modules_hash() -> u64 {
    let mut text = String::new();

    for (name, source) in MODULES {
        text.push_str(name);
        text.push('\n');
        text.push_str(source);
    }

    fnv(text.as_bytes())
}

/// The name of the cache directory, under the asset root.
pub const CACHE_DIRECTORY: &str = ".slang-cache";

/// The cache's folder the managed side named for the apps made after, in place of the asset
/// root's.
static CACHE_FOLDER: Mutex<Option<PathBuf>> = Mutex::new(None);

/// The folder compiled shaders are cached in for an app whose assets are under `root`.
pub fn cache_folder(root: &Path) -> PathBuf {
    let named = CACHE_FOLDER.lock().unwrap_or_else(PoisonError::into_inner).clone();
    named.unwrap_or_else(|| root.join(CACHE_DIRECTORY))
}

/// Names the folder compiled shaders are cached in for the apps made after, or the asset root's
/// `.slang-cache` again where `path` is null or empty.
///
/// A program reading its project's assets folder, as one built in Debug does, keeps the cache in
/// its project's build folder, so the assets folder holds only what was put there.
///
/// # Safety
/// `path` must be null or a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_cache_folder(path: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        let path = unsafe { crate::interop::cstr_to_string(path) }.filter(|path| !path.is_empty());
        *CACHE_FOLDER.lock().unwrap_or_else(PoisonError::into_inner) = path.map(PathBuf::from);
        crate::interop::status::OK
    })
}

/// The first line of a cache entry, which says which layout the rest of it has, before the hash
/// of the bridge's modules it was compiled against.
///
/// Two since entries carry reflection, in a file of their own beside the WGSL, three since
/// matrices are compiled row by row, which changes the WGSL a shader reading one compiles to, and
/// four since the modules' hash is on the first line, where clearing out stale entries reads it.
const CACHE_HEADER: &str = "// bevy_csharp slang cache 4";

/// The whole first line of an entry this bridge writes and reads.
fn cache_header() -> String {
    format!("{CACHE_HEADER} {:016x}", modules_hash())
}

/// Which stage of a pipeline an entry point is compiled for.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum Stage {
    Vertex,
    Fragment,
    Compute,
    /// A mesh pipeline's task shader, which Slang calls an amplification shader, as HLSL does.
    Task,
    Mesh,
}

impl Stage {
    /// The name `slangc` takes after `-stage`.
    fn slang_name(self) -> &'static str {
        match self {
            Stage::Vertex => "vertex",
            Stage::Fragment => "fragment",
            Stage::Compute => "compute",
            Stage::Task => "amplification",
            Stage::Mesh => "mesh",
        }
    }
}

/// One entry point of one file, with the defines it is compiled under.
#[derive(Clone, Debug)]
pub struct Request {
    /// The file, as a path on disk.
    pub file: PathBuf,
    /// The asset root, which is searched for imports.
    pub root: PathBuf,
    /// The folder the compile is cached in ([`cache_folder`]).
    pub cache: PathBuf,
    pub stage: Stage,
    pub entry: String,
    /// Name and value pairs, handed to `slangc` as `-D`.
    pub defines: Vec<(String, String)>,
    /// Compiled to SPIR-V rather than WGSL.
    pub spirv: bool,
}

/// What a successful compile produced.
#[derive(Clone, Debug)]
pub struct Compiled {
    /// The WGSL, or empty where the compile was to SPIR-V.
    pub wgsl: String,
    /// The SPIR-V, or empty where the compile was to WGSL.
    pub spirv: Vec<u8>,
    /// What `slangc -reflection-json` wrote, every parameter, its binding and its layout.
    pub reflection: String,
    /// Every file the result depends on besides the source, so that a change to any of them
    /// triggers a recompile.
    pub dependencies: Vec<PathBuf>,
    /// What `slangc` said on the way to succeeding, which is usually nothing.
    pub warnings: String,
    /// Whether this came from the cache rather than from `slangc`.
    pub cached: bool,
}

/// The compiler, or `None` where there is none to run.
///
/// Resolved once per process. Asking whether a program exists means starting it, which is not
/// something to repeat for every shader.
pub fn compiler() -> Option<&'static Path> {
    static FOUND: OnceLock<Option<PathBuf>> = OnceLock::new();

    FOUND
        .get_or_init(|| {
            let named = std::env::var_os("BCS_SLANGC")
                .filter(|value| !value.is_empty())
                .map(PathBuf::from);

            let candidates = named
                .into_iter()
                .chain(std::iter::once(PathBuf::from(executable_name("slangc"))))
                .chain(fetched_candidates());

            // `-v` prints the version and exits, which is the cheapest thing that proves the
            // program starts. A binary that is there but cannot load its libraries fails here
            // rather than on the first real compile.
            candidates.into_iter().find(|candidate| {
                Command::new(candidate)
                    .arg("-v")
                    .output()
                    .is_ok_and(|output| output.status.success())
            })
        })
        .as_deref()
}

/// A program's file name on this platform.
fn executable_name(name: &str) -> String {
    if cfg!(windows) {
        format!("{name}.exe")
    } else {
        name.to_string()
    }
}

/// The file that marks the folder a checkout of the engine stands in.
const CHECKOUT_MARKER: &str = "BevyCSharp.slnx";

/// Where `build/fetch-slang.sh` puts the compiler, in the checkout the running program is in and
/// the one the working directory is in.
///
/// Both, because a test host runs from a project's `bin` and a tool may be run from anywhere in the
/// checkout, and either way the checkout's `build/tools` is some number of directories up. The
/// checkout is the nearest folder above holding [`CHECKOUT_MARKER`], and no folder above it is
/// looked in, so a game installed outside a checkout finds no compiler this way, and a
/// `build/tools` another account made where every account can make folders, the root of a disk or
/// a shared temporary folder, is not run beside a game below it. On Unix a compiler is taken only
/// where the user running the program owns it and no other account can write it.
fn fetched_candidates() -> Vec<PathBuf> {
    let starts = [
        std::env::current_exe()
            .ok()
            .and_then(|exe| exe.parent().map(Path::to_path_buf)),
        std::env::current_dir().ok(),
    ];

    let mut found = Vec::new();

    for candidate in starts.iter().flatten().filter_map(|start| fetched_in(start)) {
        if !found.contains(&candidate) {
            found.push(candidate);
        }
    }

    found
}

/// The compiler `build/fetch-slang.sh` put in the checkout `start` is in, where there is one the
/// user running the program owns.
fn fetched_in(start: &Path) -> Option<PathBuf> {
    let checkout = start
        .ancestors()
        .find(|directory| directory.join(CHECKOUT_MARKER).is_file())?;

    let candidate = checkout
        .join("build")
        .join("tools")
        .join("slang")
        .join("bin")
        .join(executable_name("slangc"));

    (candidate.is_file() && owned_here(&candidate)).then_some(candidate)
}

/// Whether the user running the program owns a file and no other account can write it, told by
/// the owner of the scratch folder this process uses, which only that user can have made.
#[cfg(unix)]
fn owned_here(path: &Path) -> bool {
    use std::os::unix::fs::MetadataExt;

    let mine = scratch_directory().ok().and_then(|scratch| std::fs::metadata(scratch).ok());
    let file = std::fs::metadata(path).ok();
    matches!((file, mine), (Some(file), Some(mine)) if file.uid() == mine.uid() && file.mode() & 0o002 == 0)
}

/// Windows names no owner the standard library reads, and an account there can make a folder at
/// the root of a disk but not a file, so a planted checkout's marker is the guard.
#[cfg(not(unix))]
fn owned_here(_: &Path) -> bool {
    true
}

/// Compiles one entry point, or reads it from the cache when there is no compiler.
///
/// Blocking, because it waits on a process, which is why the caller runs it off the main thread.
pub fn compile(request: &Request) -> Result<Compiled, String> {
    let source = std::fs::read(&request.file)
        .map_err(|error| format!("{} could not be read: {error}", request.file.display()))?;

    let key = cache_key(request);

    let Some(slangc) = compiler() else {
        return read_cache(request, key, &source).ok_or_else(|| {
            format!(
                "{} could not be compiled, because there is no slangc here and no cached result \
                 that still matches it. In a BevyCSharp checkout build/fetch-slang.sh downloads \
                 one; elsewhere put slangc on the PATH or name it with BCS_SLANGC.",
                request.file.display()
            )
        });
    };

    let scratch = scratch_directory()?;
    let unique = next_unique();
    let output = scratch.join(format!("{unique}.{}", if request.spirv { "spv" } else { "wgsl" }));
    let depfile = scratch.join(format!("{unique}.d"));
    let reflection_file = scratch.join(format!("{unique}.json"));

    let mut command = Command::new(slangc);
    command
        .arg(&request.file)
        .args(["-target", if request.spirv { "spirv" } else { "wgsl" }])
        .args(["-stage", request.stage.slang_name()])
        .args(["-entry", &request.entry])
        .arg("-o")
        .arg(&output)
        .arg("-depfile")
        .arg(&depfile)
        .arg("-reflection-json")
        .arg(&reflection_file)
        // A matrix is held row by row, which is how `System.Numerics.Matrix4x4` holds one, so the
        // bytes C# hands over are the matrix the shader multiplies by. Slang's default holds it
        // column by column, which reads every matrix transposed.
        .arg("-matrix-layout-row-major")
        // The bridge's modules first, so `import bcs;` finds the one this bridge wrote rather
        // than a stale copy somebody left in their asset folder.
        .arg("-I")
        .arg(&scratch)
        .arg("-I")
        .arg(&request.root);

    if let Some(directory) = request.file.parent() {
        command.arg("-I").arg(directory);
    }

    for (name, value) in &request.defines {
        command.arg(format!("-D{name}={value}"));
    }

    // SPIR-V names its entry point `main` unless told to keep the one written, and a pipeline
    // handed SPIR-V untouched names the entry point it runs.
    if request.spirv {
        command.arg("-fvk-use-entrypoint-name");
    }

    let result = command
        .output()
        .map_err(|error| format!("slangc could not be started: {error}"))?;

    let said = [
        String::from_utf8_lossy(&result.stderr).trim().to_string(),
        String::from_utf8_lossy(&result.stdout).trim().to_string(),
    ]
    .into_iter()
    .filter(|text| !text.is_empty())
    .collect::<Vec<_>>()
    .join("\n");

    if !result.status.success() {
        let _ = std::fs::remove_file(&output);
        let _ = std::fs::remove_file(&depfile);
        let _ = std::fs::remove_file(&reflection_file);

        return Err(if said.is_empty() {
            format!("slangc failed on {} and said nothing", request.file.display())
        } else {
            said
        });
    }

    let (wgsl, spirv) = if request.spirv {
        let bytes = std::fs::read(&output).map_err(|error| {
            format!("slangc succeeded but its output could not be read: {error}")
        })?;
        (String::new(), bytes)
    } else {
        let text = std::fs::read_to_string(&output).map_err(|error| {
            format!("slangc succeeded but its output could not be read: {error}")
        })?;
        (text, Vec::new())
    };

    let reflection = std::fs::read_to_string(&reflection_file).map_err(|error| {
        format!("slangc succeeded but its reflection could not be read: {error}")
    })?;

    let dependencies = std::fs::read_to_string(&depfile)
        .map(|text| parse_depfile(&text))
        .unwrap_or_default()
        .into_iter()
        .filter(|path| !same_file(path, &request.file) && !path.starts_with(&scratch))
        .collect::<Vec<_>>();

    let _ = std::fs::remove_file(&output);
    let _ = std::fs::remove_file(&depfile);
    let _ = std::fs::remove_file(&reflection_file);

    write_cache(request, key, &source, &dependencies, &wgsl, &spirv, &reflection);

    Ok(Compiled {
        wgsl,
        spirv,
        reflection,
        dependencies,
        warnings: said,
        cached: false,
    })
}

/// Where the bridge's modules are written and where `slangc` writes its output.
///
/// Named after a hash of the modules, so two bridges of different versions running at once never
/// hand each other's to a compile, in a folder of the user's own ([`private_root`]), so no other
/// account can stand a folder there first, which would fail every compile or let that account swap
/// the modules every shader imports. Where the user has no such folder it is a new one in the
/// temporary folder, made for this process alone.
fn scratch_directory() -> Result<PathBuf, String> {
    static READY: OnceLock<Result<PathBuf, String>> = OnceLock::new();

    READY
        .get_or_init(|| {
            let name = format!("bevy_csharp_slang_{:016x}", modules_hash());
            let directory = match private_root() {
                Some(root) => {
                    let directory = root.join(name);
                    private_folder(true).create(&directory).map_err(|error| {
                        format!("{} could not be created: {error}", directory.display())
                    })?;
                    directory
                }
                None => fresh_folder(&std::env::temp_dir(), &name)?,
            };

            // Written to a temporary name and renamed, so a compile in another process never
            // reads half a file.
            for (name, source) in MODULES {
                let module = directory.join(name);
                let staging = directory.join(format!("{name}.{}", std::process::id()));

                std::fs::write(&staging, source)
                    .and_then(|()| std::fs::rename(&staging, &module))
                    .map_err(|error| {
                        format!("{} could not be written: {error}", module.display())
                    })?;
            }

            Ok(directory)
        })
        .clone()
}

/// A folder of the user's own that no other account can write in, for the scratch folder.
///
/// Windows and macOS give each account a temporary folder of its own. Elsewhere every account
/// shares one, so it is the runtime folder the session gives the user, then the user's cache
/// folder, or nothing where neither is named.
fn private_root() -> Option<PathBuf> {
    if cfg!(any(windows, target_os = "macos")) {
        return Some(std::env::temp_dir());
    }

    let named = |variable: &str| {
        std::env::var_os(variable)
            .map(PathBuf::from)
            .filter(|path| path.is_absolute())
    };

    named("XDG_RUNTIME_DIR")
        .or_else(|| named("XDG_CACHE_HOME"))
        .or_else(|| named("HOME").map(|home| home.join(".cache")))
}

/// Makes folders that only their owner reads or writes, on Unix, with the folders above them where
/// `parents` is set.
fn private_folder(parents: bool) -> std::fs::DirBuilder {
    let mut builder = std::fs::DirBuilder::new();
    builder.recursive(parents);

    #[cfg(unix)]
    std::os::unix::fs::DirBuilderExt::mode(&mut builder, 0o700);

    builder
}

/// Makes a folder in `parent` that did not stand before, named `name` and this process's number,
/// with a count after where that name is taken, so a folder another account made first is passed
/// over rather than written in.
fn fresh_folder(parent: &Path, name: &str) -> Result<PathBuf, String> {
    for attempt in 0..64 {
        let directory = parent.join(format!("{name}_{}_{attempt}", std::process::id()));
        match private_folder(false).create(&directory) {
            Ok(()) => return Ok(directory),
            Err(error) if error.kind() == std::io::ErrorKind::AlreadyExists => continue,
            Err(error) => return Err(format!("{} could not be created: {error}", directory.display())),
        }
    }

    Err(format!("no folder for {name} could be made in {}", parent.display()))
}

/// Writes Slang handed over as text to a file `slangc` can compile, and answers where.
///
/// Named after the text's hash, so the same text is one file however often it is handed over, and
/// kept beside the bridge's own modules, which is where every compile looks first.
pub fn inline_file(source: &str) -> Result<PathBuf, String> {
    let directory = scratch_directory()?.join("inline");

    std::fs::create_dir_all(&directory)
        .map_err(|error| format!("{} could not be created: {error}", directory.display()))?;

    let path = directory.join(format!("{:016x}.slang", fnv(source.as_bytes())));

    if std::fs::read_to_string(&path).ok().as_deref() != Some(source) {
        let staging = path.with_extension(format!("slang.{}", next_unique()));

        std::fs::write(&staging, source)
            .and_then(|()| std::fs::rename(&staging, &path))
            .map_err(|error| format!("{} could not be written: {error}", path.display()))?;
    }

    Ok(path)
}

/// A name no other compile in this process is using.
fn next_unique() -> String {
    static COUNTER: AtomicU64 = AtomicU64::new(0);

    format!(
        "{}-{}",
        std::process::id(),
        COUNTER.fetch_add(1, Ordering::Relaxed)
    )
}

/// Whether two paths name the same file, allowing for one being relative or unresolved.
fn same_file(a: &Path, b: &Path) -> bool {
    match (a.canonicalize(), b.canonicalize()) {
        (Ok(a), Ok(b)) => a == b,
        _ => a == b,
    }
}

/// Reads the files out of a Make-style dependency file.
///
/// `target: first second \` and so on, with a space inside a path written as `\ `. The target is
/// everything before the first colon followed by a space, which leaves a Windows drive letter
/// alone.
pub fn parse_depfile(text: &str) -> Vec<PathBuf> {
    let joined = text.replace("\\\r\n", " ").replace("\\\n", " ");

    let Some(split) = joined.find(": ") else {
        return Vec::new();
    };

    let mut files = Vec::new();
    let mut current = String::new();
    let mut chars = joined[split + 2..].chars().peekable();

    while let Some(c) = chars.next() {
        match c {
            '\\' if chars.peek() == Some(&' ') => {
                current.push(' ');
                chars.next();
            }
            c if c.is_whitespace() => {
                if !current.is_empty() {
                    files.push(PathBuf::from(std::mem::take(&mut current)));
                }
            }
            c => current.push(c),
        }
    }

    if !current.is_empty() {
        files.push(PathBuf::from(current));
    }

    files
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_depfile_lists_every_file_after_the_target() {
        let files = parse_depfile("out.wgsl: /a/b.slang /a/c.slang \\\n  /a/d\\ e.slang\n");

        assert_eq!(
            files,
            vec![
                PathBuf::from("/a/b.slang"),
                PathBuf::from("/a/c.slang"),
                PathBuf::from("/a/d e.slang"),
            ]
        );
    }

    #[test]
    fn a_drive_letter_is_not_taken_for_the_target() {
        let files = parse_depfile("C:\\out.wgsl: C:\\a\\b.slang\n");
        assert_eq!(files, vec![PathBuf::from("C:\\a\\b.slang")]);
    }
    /// A compiler planted above a checkout is never looked at, and the checkout's own is found from
    /// anywhere below it, unless any account can write it.
    #[test]
    fn only_the_checkouts_own_compiler_is_found() {
        let parent = std::env::temp_dir().join(format!("bcs_slang_search_{}", next_unique()));
        let checkout = parent.join("checkout");
        let start = checkout.join("games").join("bin");
        let compiler = |root: &Path| {
            let bin = root.join("build").join("tools").join("slang").join("bin");
            std::fs::create_dir_all(&bin).unwrap();
            let file = bin.join(executable_name("slangc"));
            std::fs::write(&file, b"").unwrap();
            file
        };

        std::fs::create_dir_all(&start).unwrap();
        std::fs::write(parent.join(CHECKOUT_MARKER), b"").unwrap();
        std::fs::write(checkout.join(CHECKOUT_MARKER), b"").unwrap();
        compiler(&parent);
        assert_eq!(fetched_in(&start), None, "found above the checkout");

        let own = compiler(&checkout);
        assert_eq!(fetched_in(&start), Some(own.clone()));

        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            std::fs::set_permissions(&own, std::fs::Permissions::from_mode(0o777)).unwrap();
            assert_eq!(fetched_in(&start), None, "found where any account can write it");
        }

        let _ = std::fs::remove_dir_all(&parent);
    }

    /// The scratch folder is in a folder of the user's own, and a name another account took first
    /// in the temporary folder is passed over for one made for this process.
    #[test]
    fn the_scratch_folder_is_the_users_own() {
        let scratch = scratch_directory().unwrap();
        if let Some(root) = private_root() {
            assert_eq!(scratch.parent(), Some(root.as_path()));
        }

        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            assert_eq!(std::fs::metadata(&scratch).unwrap().permissions().mode() & 0o077, 0);
        }

        let parent = std::env::temp_dir().join(format!("bcs_slang_fresh_{}", next_unique()));
        let taken = parent.join(format!("taken_{}_0", std::process::id()));
        std::fs::create_dir_all(&taken).unwrap();
        assert_eq!(fresh_folder(&parent, "taken").unwrap(), parent.join(format!("taken_{}_1", std::process::id())));
        let _ = std::fs::remove_dir_all(&parent);
    }
}

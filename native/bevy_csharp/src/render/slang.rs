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
//! the `PATH`, and then in `build/tools/slang/bin` above the running program or the working
//! directory, which is where `build/fetch-slang.sh` puts it. A machine with none of those still
//! runs a game whose shaders were compiled once, because every successful compile is written to a
//! cache beside the assets and read back when there is no compiler. A hash of the source and of
//! every file it imported decides whether a cached result still applies, so a shipped game with
//! its cache needs no compiler, and a stale entry is never used.
//!
//! Built in every profile, although only a render build compiles shaders, because it needs nothing
//! but the standard library and its tests are then run by the test job, which builds headless.

#![cfg_attr(not(feature = "render"), allow(dead_code))]

use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::OnceLock;
use std::sync::atomic::{AtomicU64, Ordering};

mod cache;

pub use cache::fnv;

use cache::{cache_key, read_cache, write_cache};

/// What every Slang shader can import as `bcs`.
pub const PRELUDE: &str = include_str!("bcs.slang");

/// What a Slang shader drawing a 2D mesh can import as `bcs2d`.
pub const PRELUDE_2D: &str = include_str!("bcs2d.slang");

/// What a Slang shader drawing a sprite can import as `bcs_sprite`: the sprite's image, color and
/// cutting, with Bevy's functions for them.
pub const SPRITE_PRELUDE: &str = include_str!("bcs_sprite.slang");

/// What a Slang shader pass can import as `bcs_pass`.
pub const PASS_PRELUDE: &str = include_str!("bcs_pass.slang");

/// What a Slang compute shader can import as `bcs_compute`.
pub const COMPUTE_PRELUDE: &str = include_str!("bcs_compute.slang");

/// What any Slang shader can import as `bcs_scene`: the layout of what the engine writes about the
/// scene into buffers, such as instance transforms.
pub const SCENE_PRELUDE: &str = include_str!("bcs_scene.slang");

/// What a compute shader compiled to SPIR-V can import as `bcs_ray`: Solari's scene, to trace rays
/// against.
pub const RAY_PRELUDE: &str = include_str!("bcs_ray.slang");

/// Every module the bridge writes out, by file name, which `import` finds them by.
const MODULES: [(&str, &str); 7] = [
    ("bcs.slang", PRELUDE),
    ("bcs2d.slang", PRELUDE_2D),
    ("bcs_sprite.slang", SPRITE_PRELUDE),
    ("bcs_pass.slang", PASS_PRELUDE),
    ("bcs_compute.slang", COMPUTE_PRELUDE),
    ("bcs_scene.slang", SCENE_PRELUDE),
    ("bcs_ray.slang", RAY_PRELUDE),
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
}

impl Stage {
    /// The name `slangc` takes after `-stage`.
    fn slang_name(self) -> &'static str {
        match self {
            Stage::Vertex => "vertex",
            Stage::Fragment => "fragment",
            Stage::Compute => "compute",
        }
    }
}

/// One entry point of one file, with the defines it is compiled under.
#[derive(Clone, Debug)]
pub struct Request {
    /// The file, as a path on disk.
    pub file: PathBuf,
    /// The asset root, which is searched for imports and holds the cache.
    pub root: PathBuf,
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
    /// What `slangc -reflection-json` wrote: every parameter, its binding and its layout.
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

/// Where `build/fetch-slang.sh` puts the compiler, looked for above the running program and above
/// the working directory.
///
/// Both, because a test host runs from a project's `bin` and a tool may be run from anywhere in the
/// checkout, and either way the checkout's `build/tools` is some number of directories up.
fn fetched_candidates() -> Vec<PathBuf> {
    let starts = [
        std::env::current_exe()
            .ok()
            .and_then(|exe| exe.parent().map(Path::to_path_buf)),
        std::env::current_dir().ok(),
    ];

    let mut found = Vec::new();

    for start in starts.into_iter().flatten() {
        for directory in start.ancestors() {
            let candidate = directory
                .join("build")
                .join("tools")
                .join("slang")
                .join("bin")
                .join(executable_name("slangc"));

            if candidate.is_file() && !found.contains(&candidate) {
                found.push(candidate);
            }
        }
    }

    found
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
/// hand each other's to a compile.
fn scratch_directory() -> Result<PathBuf, String> {
    static READY: OnceLock<Result<PathBuf, String>> = OnceLock::new();

    READY
        .get_or_init(|| {
            let directory = std::env::temp_dir()
                .join(format!("bevy_csharp_slang_{:016x}", modules_hash()));

            std::fs::create_dir_all(&directory).map_err(|error| {
                format!("{} could not be created: {error}", directory.display())
            })?;

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
}

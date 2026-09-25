//! Shaders are written in Slang and compiled to WGSL, which is what Bevy's pipeline cache takes.
//!
//! WGSL rather than SPIR-V, because Bevy takes WGSL through naga_oil with no extra feature, it runs
//! on every backend including WebGPU, and a pipeline built from it is rebuilt when the shader asset
//! changes. Slang's WGSL backend keeps explicit bindings, and it numbers stage inputs and outputs by
//! semantic index, which is what lets a Slang fragment shader follow Bevy's own vertex shader.
//!
//! Every compile also asks `slangc` for its reflection, which names each parameter the shader
//! declares and where in a uniform buffer each number goes. That is what lets a shader declare
//! whatever it likes and be handed it by name (see [`super::reflect`]).
//!
//! The compiler is `slangc`, run as a process. Linking Slang instead would add a large C++ library
//! to every build of the bridge for the benefit of the builds that compile shaders, and a process
//! is also what keeps a compiler crash out of the game.
//!
//! **Where `slangc` comes from.** `BCS_SLANGC` names it outright, and otherwise it is looked up on
//! the `PATH`, and then in `build/tools/slang/bin` above the running program or the working
//! directory, which is where `build/fetch-slang.sh` puts it. A machine with none of those still
//! runs a game whose shaders were compiled once, because every successful compile is written to a
//! cache beside the assets and read back when there is no compiler. What decides whether a cached result still applies is a hash of the source and of
//! every file it imported, so a shipped game with its cache is a game that needs no compiler, and a
//! stale entry is never used.
//!
//! Built in every profile, although only a render build compiles shaders, because it needs nothing
//! but the standard library and its tests are then run by the test job, which builds headless.

#![cfg_attr(not(feature = "render"), allow(dead_code))]

use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::OnceLock;
use std::sync::atomic::{AtomicU64, Ordering};

/// What every Slang shader can import as `bcs`.
pub const PRELUDE: &str = include_str!("bcs.slang");

/// What a Slang shader pass can import as `bcs_pass`.
pub const PASS_PRELUDE: &str = include_str!("bcs_pass.slang");

/// What a Slang compute shader can import as `bcs_compute`.
pub const COMPUTE_PRELUDE: &str = include_str!("bcs_compute.slang");

/// Every module the bridge writes out, by file name, which is what `import` finds them by.
const MODULES: [(&str, &str); 3] = [
    ("bcs.slang", PRELUDE),
    ("bcs_pass.slang", PASS_PRELUDE),
    ("bcs_compute.slang", COMPUTE_PRELUDE),
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

/// The first line of a cache entry, which also says which layout the rest of it has.
///
/// Two since entries carry reflection, in a file of their own beside the WGSL.
const CACHE_HEADER: &str = "// bevy_csharp slang cache 2";

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
}

/// What a successful compile produced.
#[derive(Clone, Debug)]
pub struct Compiled {
    pub wgsl: String,
    /// What `slangc -reflection-json` wrote: every parameter, its binding and its layout.
    pub reflection: String,
    /// Every file the result depends on besides the source, which is what a change to has to
    /// trigger a recompile.
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
    let output = scratch.join(format!("{unique}.wgsl"));
    let depfile = scratch.join(format!("{unique}.d"));
    let reflection_file = scratch.join(format!("{unique}.json"));

    let mut command = Command::new(slangc);
    command
        .arg(&request.file)
        .args(["-target", "wgsl"])
        .args(["-stage", request.stage.slang_name()])
        .args(["-entry", &request.entry])
        .arg("-o")
        .arg(&output)
        .arg("-depfile")
        .arg(&depfile)
        .arg("-reflection-json")
        .arg(&reflection_file)
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

    let wgsl = std::fs::read_to_string(&output)
        .map_err(|error| format!("slangc succeeded but its output could not be read: {error}"))?;

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

    write_cache(request, key, &source, &dependencies, &wgsl, &reflection);

    Ok(Compiled {
        wgsl,
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

// -- The cache

/// FNV-1a, 64 bits.
///
/// Written out rather than taken from the standard library, because the standard hasher is not
/// promised to give the same answer from one Rust release to the next, and the cache is read by a
/// bridge built later than the one that wrote it.
pub fn fnv(bytes: &[u8]) -> u64 {
    let mut hash: u64 = 0xcbf2_9ce4_8422_2325;

    for byte in bytes {
        hash ^= *byte as u64;
        hash = hash.wrapping_mul(0x0000_0100_0000_01b3);
    }

    hash
}

/// What names a cache entry: the file, the entry point, the defines and the bridge's modules.
///
/// The file by its path under the root rather than on disk, so the cache still applies after the
/// game has been moved somewhere else.
fn cache_key(request: &Request) -> u64 {
    let mut text = String::new();

    text.push_str(&relative_to_root(&request.file, &request.root));
    text.push('\n');
    text.push_str(request.stage.slang_name());
    text.push('\n');
    text.push_str(&request.entry);

    for (name, value) in &request.defines {
        text.push('\n');
        text.push_str(name);
        text.push('=');
        text.push_str(value);
    }

    text.push_str(&format!("\n{:016x}", modules_hash()));

    fnv(text.as_bytes())
}

/// A path under the root, with forward slashes, or the path as it is when it is not under it.
fn relative_to_root(path: &Path, root: &Path) -> String {
    let path = path.canonicalize().unwrap_or_else(|_| path.to_path_buf());
    let root = root.canonicalize().unwrap_or_else(|_| root.to_path_buf());

    match path.strip_prefix(&root) {
        Ok(relative) => relative.to_string_lossy().replace('\\', "/"),
        Err(_) => path.to_string_lossy().into_owned(),
    }
}

fn cache_path(request: &Request, key: u64) -> PathBuf {
    request
        .root
        .join(CACHE_DIRECTORY)
        .join(format!("{key:016x}.wgsl"))
}

/// Writes what a compile produced, and what it was produced from.
///
/// Failing to write is not an error, because the cache only matters to a machine without a
/// compiler, and this one has one.
fn write_cache(
    request: &Request,
    key: u64,
    source: &[u8],
    dependencies: &[PathBuf],
    wgsl: &str,
    reflection: &str,
) {
    let mut text = String::new();

    text.push_str(CACHE_HEADER);
    text.push('\n');
    text.push_str(&format!("// source {:016x}\n", fnv(source)));

    for dependency in dependencies {
        let Ok(bytes) = std::fs::read(dependency) else {
            // A dependency that cannot be read now cannot be checked later either, so an entry
            // written without it would be one that is never right to trust.
            return;
        };

        text.push_str(&format!(
            "// dependency {:016x} {}\n",
            fnv(&bytes),
            relative_to_root(dependency, &request.root)
        ));
    }

    text.push_str("// end\n");
    text.push_str(wgsl);

    let path = cache_path(request, key);

    if let Some(directory) = path.parent() {
        let _ = std::fs::create_dir_all(directory);
    }

    // The reflection first, so a reader that finds the WGSL finds the reflection beside it. The
    // WGSL is what says the entry exists, and it carries the hashes both are checked by.
    let written = [
        (path.with_extension("json"), reflection.to_string()),
        (path.clone(), text),
    ];

    for (target, contents) in written {
        let staging = target.with_extension(format!("part.{}", next_unique()));

        if std::fs::write(&staging, contents).is_ok() {
            let _ = std::fs::rename(&staging, &target);
        } else {
            let _ = std::fs::remove_file(&staging);
            return;
        }
    }
}

/// Reads a cache entry back, if there is one and everything it was made from is unchanged.
fn read_cache(request: &Request, key: u64, source: &[u8]) -> Option<Compiled> {
    let text = std::fs::read_to_string(cache_path(request, key)).ok()?;
    let mut lines = text.lines();

    if lines.next()? != CACHE_HEADER {
        return None;
    }

    let expected = lines.next()?.strip_prefix("// source ")?;

    if u64::from_str_radix(expected, 16).ok()? != fnv(source) {
        return None;
    }

    let mut dependencies = Vec::new();

    for line in lines.by_ref() {
        if line == "// end" {
            break;
        }

        let rest = line.strip_prefix("// dependency ")?;
        let (hash, path) = rest.split_once(' ')?;

        let path = if Path::new(path).is_absolute() {
            PathBuf::from(path)
        } else {
            request.root.join(path)
        };

        if u64::from_str_radix(hash, 16).ok()? != fnv(&std::fs::read(&path).ok()?) {
            return None;
        }

        dependencies.push(path);
    }

    let body = text.split_once("// end\n")?.1.to_string();
    let reflection = std::fs::read_to_string(cache_path(request, key).with_extension("json")).ok()?;

    Some(Compiled {
        wgsl: body,
        reflection,
        dependencies,
        warnings: String::new(),
        cached: true,
    })
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

    #[test]
    fn the_hash_is_the_published_fnv() {
        // The FNV-1a test vectors, so the cache format stays readable by any later build.
        assert_eq!(fnv(b""), 0xcbf2_9ce4_8422_2325);
        assert_eq!(fnv(b"a"), 0xaf63_dc4c_8601_ec8c);
    }

    #[test]
    fn a_cache_entry_is_used_only_while_its_sources_are_unchanged() {
        let root = std::env::temp_dir().join(format!("bcs_slang_cache_test_{}", next_unique()));
        std::fs::create_dir_all(&root).unwrap();

        let file = root.join("a.slang");
        let dependency = root.join("b.slang");
        std::fs::write(&file, "one").unwrap();
        std::fs::write(&dependency, "two").unwrap();

        let request = Request {
            file: file.clone(),
            root: root.clone(),
            stage: Stage::Fragment,
            entry: "fragment".into(),
            defines: vec![("A".into(), "1".into())],
        };

        let key = cache_key(&request);
        write_cache(
            &request,
            key,
            b"one",
            std::slice::from_ref(&dependency),
            "fn f() {}",
            "{}",
        );

        let read = read_cache(&request, key, b"one").expect("an unchanged entry is used");
        assert_eq!(read.wgsl, "fn f() {}");
        assert_eq!(read.reflection, "{}");
        assert_eq!(read.dependencies.len(), 1);

        assert!(read_cache(&request, key, b"changed").is_none());

        std::fs::write(&dependency, "changed").unwrap();
        assert!(read_cache(&request, key, b"one").is_none());

        let _ = std::fs::remove_dir_all(&root);
    }
}

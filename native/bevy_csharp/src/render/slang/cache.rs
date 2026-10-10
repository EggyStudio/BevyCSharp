//! The cache of compiled shaders beside the assets, which a machine without a compiler reads.
//!
//! An entry is named by the file, the entry point, the defines and the bridge's modules, and holds
//! the hash of the source and of every file it imported, so it is used only while all of them are
//! unchanged.

use std::path::{Path, PathBuf};

use super::{CACHE_DIRECTORY, Compiled, Request, cache_header, modules_hash, next_unique};

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
pub(super) fn cache_key(request: &Request) -> u64 {
    let mut text = String::new();

    text.push_str(&relative_to_root(&request.file, &request.root));
    text.push('\n');
    text.push_str(request.stage.slang_name());
    text.push('\n');
    text.push_str(&request.entry);

    // Only where it is set, so every entry a WGSL compile wrote before SPIR-V existed keeps its key.
    if request.spirv {
        text.push_str("\nspirv");
    }

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
pub(super) fn write_cache(
    request: &Request,
    key: u64,
    source: &[u8],
    dependencies: &[PathBuf],
    wgsl: &str,
    spirv: &[u8],
    reflection: &str,
) {
    let mut text = String::new();

    text.push_str(&cache_header());
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
        clear_stale_entries(directory);
    }

    // The reflection and any SPIR-V first, so a reader that finds the WGSL finds both beside it.
    // The WGSL file says the entry exists, and it carries the hashes all of them are checked by.
    // For a SPIR-V compile its body is empty and the binary sits beside it.
    let mut written = vec![(path.with_extension("json"), reflection.as_bytes().to_vec())];

    if !spirv.is_empty() {
        written.push((path.with_extension("spv"), spirv.to_vec()));
    }

    written.push((path.clone(), text.into_bytes()));

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

/// Deletes the entries in a cache directory that no bridge of this version can use, once per
/// directory per run.
///
/// An entry's key includes the hash of the bridge's modules, so every change to them leaves every
/// entry before it unreachable, and nothing else would ever remove one. Only a machine that
/// compiles clears them, since it is the one that writes the entries that replace them; a
/// machine without a compiler leaves a shipped cache exactly as it came. What is removed is an
/// entry written for other modules or in an older layout, and the reflection or SPIR-V beside an
/// entry that is gone.
fn clear_stale_entries(directory: &Path) {
    static CLEARED: std::sync::Mutex<Vec<PathBuf>> = std::sync::Mutex::new(Vec::new());

    {
        let Ok(mut cleared) = CLEARED.lock() else { return };
        if cleared.iter().any(|done| done == directory) {
            return;
        }
        cleared.push(directory.to_path_buf());
    }

    let Ok(entries) = std::fs::read_dir(directory) else { return };
    let header = cache_header();
    let mut kept = std::collections::HashSet::new();
    let mut others = Vec::new();

    for entry in entries.flatten() {
        let path = entry.path();

        match path.extension().and_then(|extension| extension.to_str()) {
            Some("wgsl") => {
                let current = std::fs::read_to_string(&path)
                    .is_ok_and(|text| text.lines().next() == Some(header.as_str()));

                if current {
                    kept.insert(path.with_extension(""));
                } else {
                    let _ = std::fs::remove_file(&path);
                }
            }
            Some("json" | "spv") => others.push(path),
            _ => {}
        }
    }

    // A compile on another thread writes an entry's reflection and SPIR-V before its WGSL, so one
    // written a moment ago may be waiting for its WGSL rather than left behind by a stale one.
    let recent = |path: &Path| {
        std::fs::metadata(path)
            .and_then(|metadata| metadata.modified())
            .ok()
            .and_then(|modified| modified.elapsed().ok())
            .is_some_and(|age| age.as_secs() < 60)
    };

    for path in others {
        if !kept.contains(&path.with_extension("")) && !recent(&path) {
            let _ = std::fs::remove_file(&path);
        }
    }
}

/// Reads a cache entry back, if there is one and everything it was made from is unchanged.
pub(super) fn read_cache(request: &Request, key: u64, source: &[u8]) -> Option<Compiled> {
    let text = std::fs::read_to_string(cache_path(request, key)).ok()?;
    let mut lines = text.lines();

    if lines.next()? != cache_header() {
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

    let spirv = if request.spirv {
        std::fs::read(cache_path(request, key).with_extension("spv")).ok()?
    } else {
        Vec::new()
    };

    Some(Compiled {
        wgsl: body,
        spirv,
        reflection,
        dependencies,
        warnings: String::new(),
        cached: true,
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::render::slang::Stage;

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
            spirv: false,
        };

        let key = cache_key(&request);
        write_cache(
            &request,
            key,
            b"one",
            std::slice::from_ref(&dependency),
            "fn f() {}",
            &[],
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

    /// An entry written for other modules, and what sat beside it, is cleared the first time this
    /// run writes to the directory, and an entry of this bridge's is kept.
    #[test]
    fn stale_entries_are_cleared_and_current_ones_kept() {
        let root = std::env::temp_dir().join(format!("bcs_slang_clear_test_{}", next_unique()));
        let directory = root.join(CACHE_DIRECTORY);
        std::fs::create_dir_all(&directory).unwrap();

        let old = std::time::SystemTime::now() - std::time::Duration::from_secs(3600);
        let written = |name: &str, bytes: &[u8]| {
            let path = directory.join(name);
            std::fs::write(&path, bytes).unwrap();
            std::fs::File::options().write(true).open(&path).unwrap().set_modified(old).unwrap();
        };

        written("aaaa.wgsl", b"// bevy_csharp slang cache 3\n");
        written("aaaa.json", b"{}");
        written("bbbb.wgsl", format!("{}\n", cache_header()).as_bytes());
        written("bbbb.json", b"{}");
        written("cccc.spv", &[1, 2, 3, 4]);

        // Written a moment ago, as by a compile still on its way to writing the WGSL.
        std::fs::write(directory.join("dddd.json"), "{}").unwrap();

        clear_stale_entries(&directory);

        assert!(!directory.join("aaaa.wgsl").exists());
        assert!(!directory.join("aaaa.json").exists());
        assert!(!directory.join("cccc.spv").exists());
        assert!(directory.join("dddd.json").exists());
        assert!(directory.join("bbbb.wgsl").exists());
        assert!(directory.join("bbbb.json").exists());

        let _ = std::fs::remove_dir_all(&root);
    }

    /// A SPIR-V compile comes back as the bytes it was written with, under a key of its own, so the
    /// same entry point compiled both ways is two entries.
    #[test]
    fn a_spirv_entry_keeps_its_bytes_and_its_own_key() {
        let root = std::env::temp_dir().join(format!("bcs_slang_spirv_test_{}", next_unique()));
        std::fs::create_dir_all(&root).unwrap();

        let file = root.join("a.slang");
        std::fs::write(&file, "one").unwrap();

        let wgsl = Request {
            file: file.clone(),
            root: root.clone(),
            stage: Stage::Compute,
            entry: "main".into(),
            defines: Vec::new(),
            spirv: false,
        };

        let spirv = Request {
            spirv: true,
            ..wgsl.clone()
        };

        assert_ne!(cache_key(&wgsl), cache_key(&spirv));

        let key = cache_key(&spirv);
        let bytes = [0x03, 0x02, 0x23, 0x07, 1, 2, 3, 4];
        write_cache(&spirv, key, b"one", &[], "", &bytes, "{}");

        let read = read_cache(&spirv, key, b"one").expect("an unchanged entry is used");
        assert_eq!(read.spirv, bytes);
        assert!(read.wgsl.is_empty());

        let _ = std::fs::remove_dir_all(&root);
    }
}

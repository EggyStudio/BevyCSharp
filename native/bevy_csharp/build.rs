//! Lists a game's assets for the `embed` feature to compile into the library, from the folder
//! `BEVY_ASSET_PATH` names, which `build/build-native.sh --embed` sets. Without the feature it does
//! nothing, so every other build costs only this script's compile.
//!
//! Taken from the build script of `bevy_embedded_assets` 0.16, by François Mockers, under MIT or
//! Apache-2.0, which the bridge depended on until that crate's last release stayed on Bevy 0.19.
//! The bridge always names the folder, so the crate's search for an `assets` folder beside the
//! build's target is left out, and a build script prints its own directives, so `cargo-emit` is
//! too.

use std::env;
use std::fs;
use std::path::{Path, PathBuf};

const ASSET_PATH_VAR: &str = "BEVY_ASSET_PATH";

fn main() {
    println!("cargo::rerun-if-env-changed={ASSET_PATH_VAR}");

    if env::var_os("CARGO_FEATURE_EMBED").is_none() {
        return;
    }

    let Some(folder) = env::var_os(ASSET_PATH_VAR).map(PathBuf::from).filter(|path| path.is_dir())
    else {
        panic!(
            "the embed feature compiles in the folder ${ASSET_PATH_VAR} names, and it names none \
             that is there; build/build-native.sh --embed <folder> sets it"
        );
    };

    println!("cargo::rerun-if-changed={}", folder.display());

    // Written with forward slashes, the way an asset path is, unless the library is built for
    // Windows, so a build on Windows for another system finds its files under the same paths.
    let forward = env::var("CARGO_CFG_TARGET_OS").is_ok_and(|os| os != "windows");

    let mut files = Vec::new();
    visit(&folder, &mut files);
    files.sort();

    let mut out = String::from(
        "/// Hands every file of the embedded folder to `insert`, by its path in the folder.\n\
         fn include_all_assets(mut insert: impl FnMut(&'static str, &'static [u8])) {\n",
    );

    for file in files {
        let mut path = file.strip_prefix(&folder).unwrap().to_string_lossy().to_string();
        if forward {
            path = path.replace(std::path::MAIN_SEPARATOR, "/");
        }

        println!("cargo::rerun-if-changed={}", file.display());
        out.push_str(&format!("    insert({path:?}, include_bytes!({:?}));\n", file.to_string_lossy()));
    }

    out.push_str("}\n");

    let destination = Path::new(&env::var_os("OUT_DIR").unwrap()).join("include_all_assets.rs");
    fs::write(destination, out).unwrap();
}

/// Every file under a folder, at any depth.
fn visit(folder: &Path, found: &mut Vec<PathBuf>) {
    for entry in fs::read_dir(folder).unwrap() {
        let path = entry.unwrap().path();
        if path.is_dir() {
            visit(&path, found);
        } else {
            found.push(path);
        }
    }
}

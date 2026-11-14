//! The folders Bevy reads assets from, each with a watcher of the bridge's making.
//!
//! Bevy's platform default source builds its watcher with a panic where the system refuses one, a
//! limit on watches or watchers met or a folder under the root that cannot be read, and the panic
//! falls inside the app's creation, so an app asked to watch its assets would not start at all.
//! The watcher here says the system's reason in a warning and leaves the source unwatched, so the
//! app reads its files as it would without watching.
//!
//! It also hands Bevy's watcher the root resolved through any link, off Windows. Bevy strips the
//! root it was given from each changed file's path and panics where the path does not begin with
//! it, and macOS reports a change by the resolved path, so a root reached through a link, which
//! every temporary folder there is, would read no change again.

use bevy::asset::io::{AssetSource, AssetSourceBuilder};

/// A source over the folder `root`, read and written as Bevy's platform default is, with a watcher
/// of the bridge's making where the build carries Bevy's.
///
/// A relative `root` is under Bevy's base path, the executable's folder, as Bevy's own reader
/// takes it.
pub fn source(root: &str) -> AssetSourceBuilder {
    let builder = AssetSourceBuilder::new(AssetSource::get_default_reader(root.to_string()))
        .with_writer(AssetSource::get_default_writer(root.to_string()));

    #[cfg(feature = "render")]
    let builder = {
        let root = root.to_string();
        builder.with_watcher(move |sender| {
            let path = watched_root(&root)?;
            match bevy::asset::io::file::FileWatcher::new(path.clone(), sender, DEBOUNCE) {
                Ok(watcher) => Some(Box::new(watcher) as Box<dyn bevy::asset::io::AssetWatcher>),
                Err(error) => {
                    bevy::log::warn!(
                        "{} cannot be watched, so its files are not read again as they change: {error}",
                        path.display()
                    );
                    None
                }
            }
        })
    };

    builder
}

/// How long a changed file sits still before Bevy is told of it, Bevy's own wait, which gathers
/// the several writes an editor's one save makes into one change.
#[cfg(feature = "render")]
const DEBOUNCE: std::time::Duration = std::time::Duration::from_millis(300);

/// The folder a source's watcher watches, `root` under Bevy's base path and resolved through any
/// link off Windows, or nothing where there is no folder there.
///
/// Windows reports a change by the path the watch was asked for, and resolving gives the `\\?\`
/// form there, so the root is left as given.
#[cfg(feature = "render")]
fn watched_root(root: &str) -> Option<std::path::PathBuf> {
    let path = bevy::asset::io::file::FileAssetReader::get_base_path().join(root);
    if !path.is_dir() {
        bevy::log::info!("{} is no folder, so nothing in it is read again as it changes.", path.display());
        return None;
    }

    #[cfg(not(windows))]
    let path = std::fs::canonicalize(&path).unwrap_or(path);
    Some(path)
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use std::path::{Path, PathBuf};
    use std::time::{Duration, Instant};

    use bevy::asset::io::{AssetSourceEvent, AssetSourceId};

    use super::*;

    /// A folder of its own under the system's temporary folder.
    fn folder(name: &str) -> PathBuf {
        let path = std::env::temp_dir().join(format!("bcs_watching_{name}_{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&path);
        std::fs::create_dir_all(&path).unwrap();
        path
    }

    /// A source over `root` built as the asset server builds it for an app asked to watch.
    fn built(root: &Path) -> bevy::asset::io::AssetSource {
        source(root.to_str().unwrap()).build(AssetSourceId::Default, true, false)
    }

    /// Whether an event says the file at `path` under the root was added or changed, which a
    /// system may report a new file as.
    fn names(event: &AssetSourceEvent, path: &str) -> bool {
        matches!(event, AssetSourceEvent::AddedAsset(at) | AssetSourceEvent::ModifiedAsset(at) if at == Path::new(path))
    }

    /// A file written under a watched folder reaches the source's receiver by its path under the
    /// root, which on macOS, whose temporary folder is reached through a link, takes the root the
    /// link leads to.
    #[test]
    fn a_file_written_under_the_folder_is_said_by_its_path_under_it() {
        let root = folder("written");
        let source = built(&root);
        let receiver = source.event_receiver().expect("the folder is watched");

        std::fs::write(root.join("swatch.txt"), "red").unwrap();

        let until = Instant::now() + Duration::from_secs(10);
        let mut said = Vec::new();
        while Instant::now() < until && !said.iter().any(|event| names(event, "swatch.txt")) {
            match receiver.try_recv() {
                Ok(event) => said.push(event),
                Err(_) => std::thread::sleep(Duration::from_millis(20)),
            }
        }

        drop(source);
        let _ = std::fs::remove_dir_all(&root);
        assert!(said.iter().any(|event| names(event, "swatch.txt")), "the file was not said, only {said:?}");
    }

    /// A root reached through a link is watched at the folder the link leads to, which is the path
    /// macOS reports its changes by.
    #[cfg(unix)]
    #[test]
    fn a_root_through_a_link_is_watched_at_the_folder_it_leads_to() {
        let target = folder("target");
        let link = std::env::temp_dir().join(format!("bcs_watching_link_{}", std::process::id()));
        let _ = std::fs::remove_file(&link);
        std::os::unix::fs::symlink(&target, &link).unwrap();

        let watched = watched_root(link.to_str().unwrap());

        let _ = std::fs::remove_file(&link);
        let _ = std::fs::remove_dir_all(&target);
        assert_eq!(watched, Some(std::fs::canonicalize(std::env::temp_dir()).unwrap().join(target.file_name().unwrap())));
    }

    /// A folder under the root that the system will not watch leaves the source unwatched, where
    /// Bevy's own builder panics, and the app reads its files all the same.
    ///
    /// On Linux, where the system's watcher watches each folder and needs to read it. Where the
    /// test runs with the right to read any folder, as root in a container does, the folder reads
    /// anyway and the test has nothing to show.
    #[cfg(target_os = "linux")]
    #[test]
    fn a_folder_the_system_will_not_watch_leaves_the_source_unwatched() {
        use std::os::unix::fs::PermissionsExt;

        let root = folder("refused");
        let shut = root.join("shut");
        std::fs::create_dir_all(&shut).unwrap();
        std::fs::set_permissions(&shut, std::fs::Permissions::from_mode(0o000)).unwrap();

        if std::fs::read_dir(&shut).is_ok() {
            std::fs::set_permissions(&shut, std::fs::Permissions::from_mode(0o755)).unwrap();
            let _ = std::fs::remove_dir_all(&root);
            eprintln!("passed over: this process reads a folder with no read permission");
            return;
        }

        let source = std::panic::catch_unwind(|| built(&root));

        std::fs::set_permissions(&shut, std::fs::Permissions::from_mode(0o755)).unwrap();
        let _ = std::fs::remove_dir_all(&root);
        let source = source.expect("building the source panicked");
        assert!(source.event_receiver().is_none(), "a folder the system refused to watch was said to be watched");
    }
}

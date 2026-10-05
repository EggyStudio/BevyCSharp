//! Assets the game's own assembly carries, read by Bevy through the managed side.
//!
//! A shipped game can carry its assets inside the binary in two ways. A bridge built for that game
//! with `--embed` compiles them into itself, which costs a bridge build per game. The other way,
//! here, leaves the bridge shared. The assets are compiled into the game's managed assembly as
//! resources, and Bevy's default asset source reads them back through a function the managed side
//! hands over before the app is built. One copy of each file then feeds both sides, since the
//! managed side reads its scenes and data assets from the same resources.
//!
//! The managed side gives the list of paths it carries along with the function, so whether a file
//! or a folder is there is answered here without a call across, and only reading a file's bytes
//! crosses. A file on disk is read first, as `AssetFiles` reads first from disk on the managed
//! side, so a file beside a shipped game replaces the one it carries.

use std::collections::{BTreeSet, HashSet};
use std::path::{Path, PathBuf};
use std::sync::{Arc, Mutex};

use bevy::asset::io::{
    AssetReader, AssetReaderError, ErasedAssetReader, PathStream, Reader, VecReader,
};
use bevy::tasks::futures_lite::StreamExt;

use crate::interop::status;

/// What the managed side's reader is called through, with a path under the asset root as UTF-8 and
/// its length, then where to copy the file and how much room there is. It answers the file's length,
/// copying it only when the room is enough, or a negative number when it carries no such file.
///
/// Asked once with no room to learn the length and once more with room for it, so the bytes are
/// copied into memory the bridge owns and nothing allocated on one side is freed on the other.
pub type ReadCarried =
    unsafe extern "C" fn(path: *const u8, path_len: usize, dest: *mut u8, dest_len: usize) -> i64;

/// The managed side's reader and the paths it carries, taken by the next app built.
#[derive(Clone)]
struct Carried {
    read: ReadCarried,
    files: Arc<HashSet<PathBuf>>,
}

/// What [`bcs_assets_carried`] was last given, process-wide since the asset source is built once
/// as an app is, before any handle to it exists.
static CARRIED: Mutex<Option<Carried>> = Mutex::new(None);

/// Hands over the managed side's reader and the paths it carries, or takes them back with a null
/// reader.
///
/// `paths` is every file the assembly carries, relative to the asset root, as UTF-8 joined by
/// newlines. Read by the next app built, which reads its assets through the reader from then on,
/// so this is called before `bcs_app_create`.
///
/// # Safety
/// `paths` must point to `paths_len` readable bytes when the reader is not null, and the reader
/// must stay callable for as long as any app built after this runs.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_assets_carried(
    read: Option<ReadCarried>,
    paths: *const u8,
    paths_len: usize,
) -> i32 {
    // Guarded as every entry point is, so a panic in reading the list, or in the lock, is a status
    // to the managed side rather than an unwind across the boundary.
    crate::interop::guard(|| {
        let Some(read) = read else {
            *CARRIED.lock().unwrap_or_else(|poisoned| poisoned.into_inner()) = None;
            return status::OK;
        };

        if paths.is_null() && paths_len > 0 {
            return status::NULL_ARG;
        }

        let text = if paths_len == 0 {
            ""
        } else {
            // SAFETY: the caller promises `paths_len` readable bytes at `paths`.
            match std::str::from_utf8(unsafe { std::slice::from_raw_parts(paths, paths_len) }) {
                Ok(text) => text,
                Err(_) => return status::NULL_ARG,
            }
        };

        let files = text
            .split('\n')
            .filter(|line| !line.is_empty())
            .map(PathBuf::from)
            .collect();

        *CARRIED.lock().unwrap_or_else(|poisoned| poisoned.into_inner()) = Some(Carried {
            read,
            files: Arc::new(files),
        });

        status::OK
    })
}

/// Replaces the default asset source with one that reads disk first and the managed side's
/// resources after, when the managed side handed a reader over. Called before the asset plugin is
/// added, which builds the default source only where none was registered.
///
/// The writer and the watcher are the disk's, so an app that asked to watch its assets still sees
/// a file changed beside it.
#[cfg_attr(feature = "embed", allow(dead_code))]
pub fn install(app: &mut bevy::app::App, root: &str) {
    use bevy::asset::AssetApp;
    use bevy::asset::io::{AssetSource, AssetSourceBuilder, AssetSourceId};

    let Some(carried) = CARRIED
        .lock()
        .unwrap_or_else(|poisoned| poisoned.into_inner())
        .clone()
    else {
        return;
    };

    let mut disk = AssetSource::get_default_reader(root.to_string());

    app.register_asset_source(
        AssetSourceId::Default,
        AssetSourceBuilder::platform_default(root, None).with_reader(move || {
            Box::new(CarriedReader {
                disk: disk(),
                carried: carried.clone(),
            })
        }),
    );
}

/// Reads a file from disk, or from what the managed side carries when the disk has none.
struct CarriedReader {
    disk: Box<dyn ErasedAssetReader>,
    carried: Carried,
}

impl CarriedReader {
    /// A carried file's bytes, asked of the managed side, or `NotFound` when it carries none.
    fn bytes(&self, path: &Path) -> Result<Vec<u8>, AssetReaderError> {
        let not_found = || AssetReaderError::NotFound(path.to_path_buf());
        if !self.carried.files.contains(path) {
            return Err(not_found());
        }

        let name = path.to_string_lossy().replace('\\', "/");

        // SAFETY: the name is valid for its length, and the reader was promised to stay callable.
        let length = unsafe { (self.carried.read)(name.as_ptr(), name.len(), std::ptr::null_mut(), 0) };
        if length < 0 {
            return Err(not_found());
        }

        let mut bytes = vec![0u8; length as usize];

        // SAFETY: as above, with room for exactly the length just answered.
        let copied =
            unsafe { (self.carried.read)(name.as_ptr(), name.len(), bytes.as_mut_ptr(), bytes.len()) };

        // A length that changed between the two calls means the file is not the one measured,
        // which a resource compiled into an assembly never does, so it is taken as an error
        // rather than read short.
        if copied != length {
            return Err(AssetReaderError::Io(Arc::new(std::io::Error::other(format!(
                "{} changed size while it was read",
                path.display()
            )))));
        }

        Ok(bytes)
    }

    /// The files and folders directly inside `path` among those carried.
    fn children(&self, path: &Path) -> BTreeSet<PathBuf> {
        self.carried
            .files
            .iter()
            .filter_map(|file| file.strip_prefix(path).ok())
            .filter_map(|rest| rest.components().next())
            .map(|first| path.join(first))
            .collect()
    }
}

/// Whether a read failed only because the file was not there, which is when the carried copy is
/// asked for. Any other failure is the disk's and is passed on.
fn missing(result: &Result<impl Sized, AssetReaderError>) -> bool {
    matches!(result, Err(AssetReaderError::NotFound(_)))
}

impl AssetReader for CarriedReader {
    async fn read<'a>(&'a self, path: &'a Path) -> Result<impl Reader + 'a, AssetReaderError> {
        let disk = self.disk.read(path).await;
        if !missing(&disk) {
            return disk;
        }

        let reader: Box<dyn Reader + 'a> = Box::new(VecReader::new(self.bytes(path)?));
        Ok(reader)
    }

    async fn read_meta<'a>(&'a self, path: &'a Path) -> Result<impl Reader + 'a, AssetReaderError> {
        let disk = self.disk.read_meta(path).await;
        if !missing(&disk) {
            return disk;
        }

        let mut meta = path.as_os_str().to_owned();
        meta.push(".meta");

        let reader: Box<dyn Reader + 'a> = Box::new(VecReader::new(self.bytes(Path::new(&meta))?));
        Ok(reader)
    }

    async fn read_directory<'a>(
        &'a self,
        path: &'a Path,
    ) -> Result<Box<PathStream>, AssetReaderError> {
        // A folder can be partly on disk and partly carried, as when a file beside the game is
        // added to a folder it carries, so the two are merged rather than one chosen.
        let mut found = self.children(path);

        match self.disk.read_directory(path).await {
            Ok(stream) => found.extend(stream.collect::<Vec<_>>().await),
            Err(AssetReaderError::NotFound(_)) if !found.is_empty() => {}
            Err(error) => return Err(error),
        }

        let stream: Box<PathStream> = Box::new(bevy::tasks::futures_lite::stream::iter(found));
        Ok(stream)
    }

    async fn is_directory<'a>(&'a self, path: &'a Path) -> Result<bool, AssetReaderError> {
        // A carried file is answered here, since the disk would answer that it has no such path.
        if self.carried.files.contains(path) {
            return Ok(false);
        }

        if !self.children(path).is_empty() {
            return Ok(true);
        }

        self.disk.is_directory(path).await
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    /// Two files, one at the root and one in a folder, as the managed side would serve them.
    unsafe extern "C" fn serve(path: *const u8, len: usize, dest: *mut u8, room: usize) -> i64 {
        let name = unsafe { std::str::from_utf8(std::slice::from_raw_parts(path, len)) }.unwrap();
        let bytes: &[u8] = match name {
            "top.txt" => b"top",
            "folder/inner.txt" => b"inner",
            _ => return -1,
        };

        if room >= bytes.len() {
            unsafe { std::ptr::copy_nonoverlapping(bytes.as_ptr(), dest, bytes.len()) };
        }

        bytes.len() as i64
    }

    fn reader(disk: &Path) -> CarriedReader {
        CarriedReader {
            disk: bevy::asset::io::AssetSource::get_default_reader(disk.to_string_lossy().into())(),
            carried: Carried {
                read: serve,
                files: Arc::new(
                    ["top.txt", "folder/inner.txt"]
                        .into_iter()
                        .map(PathBuf::from)
                        .collect(),
                ),
            },
        }
    }

    fn read(reader: &CarriedReader, path: &str) -> Result<Vec<u8>, AssetReaderError> {
        bevy::tasks::block_on(async {
            let mut file = AssetReader::read(reader, Path::new(path)).await?;
            let mut bytes = Vec::new();
            file.read_to_end(&mut bytes).await?;
            Ok(bytes)
        })
    }

    #[test]
    fn a_carried_file_is_read_where_the_disk_has_none_and_the_disk_wins() {
        let disk = std::env::temp_dir().join(format!("bcs-carried-{}", std::process::id()));
        std::fs::create_dir_all(&disk).unwrap();
        let reader = reader(&disk);

        assert_eq!(read(&reader, "folder/inner.txt").unwrap(), b"inner");
        assert!(matches!(read(&reader, "absent.txt"), Err(AssetReaderError::NotFound(_))));

        std::fs::write(disk.join("top.txt"), b"beside").unwrap();
        assert_eq!(read(&reader, "top.txt").unwrap(), b"beside");

        // A folder only the assembly carries is still a folder, listed by its direct children.
        assert!(bevy::tasks::block_on(AssetReader::is_directory(&reader, Path::new("folder"))).unwrap());
        assert!(!bevy::tasks::block_on(AssetReader::is_directory(&reader, Path::new("top.txt"))).unwrap());

        let listed: Vec<PathBuf> = bevy::tasks::block_on(async {
            AssetReader::read_directory(&reader, Path::new(""))
                .await
                .unwrap()
                .collect()
                .await
        });

        assert!(listed.contains(&PathBuf::from("folder")));
        assert!(listed.contains(&PathBuf::from("top.txt")));
        assert!(!listed.contains(&PathBuf::from("folder/inner.txt")));

        std::fs::remove_dir_all(&disk).unwrap();
    }
}

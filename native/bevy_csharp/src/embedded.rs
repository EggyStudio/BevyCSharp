//! A game's assets compiled into the library, read as Bevy's default asset source, behind the
//! `embed` feature.
//!
//! A shipped game is then its executable and this library, with no folder beside them to lose or
//! change, and every load finds the embedded file under the path it had on disk. The files are the
//! ones `build.rs` listed from the folder `BEVY_ASSET_PATH` names, so a bridge built this way
//! serves one game alone.
//!
//! Taken from the asset reader of `bevy_embedded_assets` 0.16, by François Mockers, under MIT or
//! Apache-2.0, in the mode that replaces the default source, the one the bridge used. Each file is
//! read through Bevy's own `SliceReader`, which also seeks where the crate's reader did not, and
//! the crate's other modes, its fallback to the disk and its public reader are left out.

use std::collections::HashMap;
use std::path::Path;

use bevy::app::App;
use bevy::asset::AssetApp;
use bevy::asset::io::{
    AssetReader, AssetReaderError, AssetSourceBuilder, AssetSourceId, PathStream, Reader,
    SliceReader,
};

include!(concat!(env!("OUT_DIR"), "/include_all_assets.rs"));

/// Makes the embedded files the default asset source. Called before the asset plugin is added,
/// which builds the default source only where none was registered.
pub fn install(app: &mut App) {
    app.register_asset_source(
        AssetSourceId::Default,
        AssetSourceBuilder::new(|| Box::new(EmbeddedAssetReader::preloaded()))
            .with_processed_reader(|| Box::new(EmbeddedAssetReader::preloaded())),
    );
}

/// The embedded files by their paths in the folder.
struct EmbeddedAssetReader {
    loaded: HashMap<&'static Path, &'static [u8]>,
}

impl EmbeddedAssetReader {
    /// One holding every file the build script listed.
    fn preloaded() -> Self {
        let mut reader = Self::new();
        include_all_assets(|path, bytes| reader.add_asset(Path::new(path), bytes));
        reader
    }

    fn new() -> Self {
        Self { loaded: HashMap::new() }
    }

    fn add_asset(&mut self, path: &'static Path, bytes: &'static [u8]) {
        self.loaded.insert(path, bytes);
    }

    fn bytes(&self, path: &Path) -> Result<&'static [u8], AssetReaderError> {
        self.loaded
            .get(path)
            .copied()
            .ok_or_else(|| AssetReaderError::NotFound(path.to_path_buf()))
    }

    /// Whether any file lies under the path, which is then a folder.
    fn is_directory_sync(&self, path: &Path) -> bool {
        let as_folder = path.join("");
        self.loaded
            .keys()
            .any(|loaded| loaded.starts_with(&as_folder) && *loaded != path)
    }

    /// Every file under a folder, at any depth, as the crate listed them, which Bevy's folder
    /// loading takes as it takes a folder's files and subfolders.
    fn read_directory_sync(&self, path: &Path) -> Result<Vec<std::path::PathBuf>, AssetReaderError> {
        if !self.is_directory_sync(path) {
            return Err(AssetReaderError::NotFound(path.to_path_buf()));
        }

        Ok(self
            .loaded
            .keys()
            .filter(|loaded| loaded.starts_with(path))
            .map(|loaded| loaded.to_path_buf())
            .collect())
    }
}

/// The file Bevy reads an asset's settings from, the asset's path with `.meta` after it, as Bevy's
/// own readers find it.
fn meta_path(path: &Path) -> std::path::PathBuf {
    let mut extension = path.extension().unwrap_or_default().to_os_string();
    if !extension.is_empty() {
        extension.push(".");
    }
    extension.push("meta");
    path.with_extension(extension)
}

impl AssetReader for EmbeddedAssetReader {
    async fn read<'a>(&'a self, path: &'a Path) -> Result<impl Reader + 'a, AssetReaderError> {
        self.bytes(path).map(SliceReader::new)
    }

    async fn read_meta<'a>(&'a self, path: &'a Path) -> Result<impl Reader + 'a, AssetReaderError> {
        self.bytes(&meta_path(path)).map(SliceReader::new)
    }

    async fn read_directory<'a>(
        &'a self,
        path: &'a Path,
    ) -> Result<Box<PathStream>, AssetReaderError> {
        let paths = self.read_directory_sync(path)?;
        let stream: Box<PathStream> = Box::new(bevy::tasks::futures_lite::stream::iter(paths));
        Ok(stream)
    }

    async fn is_directory<'a>(&'a self, path: &'a Path) -> Result<bool, AssetReaderError> {
        Ok(self.is_directory_sync(path))
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::tasks::block_on;
    use bevy::tasks::futures_lite::StreamExt;

    #[test]
    fn a_file_is_read_by_its_path_and_no_other() {
        let mut embedded = EmbeddedAssetReader::new();
        embedded.add_asset(Path::new("asset.png"), &[1, 2, 3]);
        embedded.add_asset(Path::new("other_asset.png"), &[4, 5, 6]);

        let mut bytes = Vec::new();
        let mut reader = block_on(embedded.read(Path::new("asset.png"))).unwrap();
        block_on(Reader::read_to_end(&mut reader, &mut bytes)).unwrap();
        assert_eq!(bytes, [1, 2, 3]);

        assert!(block_on(embedded.read(Path::new("asset"))).is_err());
        assert!(block_on(embedded.read(Path::new("other"))).is_err());
    }

    #[test]
    fn a_folder_is_a_path_with_files_under_it() {
        let mut embedded = EmbeddedAssetReader::new();
        embedded.add_asset(Path::new("asset.png"), &[]);
        embedded.add_asset(Path::new("directory/asset.png"), &[]);

        assert!(!embedded.is_directory_sync(Path::new("asset.png")));
        assert!(!embedded.is_directory_sync(Path::new("asset")));
        assert!(embedded.is_directory_sync(Path::new("directory")));
        assert!(embedded.is_directory_sync(Path::new("directory/")));
        assert!(!embedded.is_directory_sync(Path::new("directory/asset")));
    }

    #[test]
    fn a_folder_lists_the_files_under_it() {
        let mut embedded = EmbeddedAssetReader::new();
        embedded.add_asset(Path::new("asset.png"), &[]);
        embedded.add_asset(Path::new("directory/asset.png"), &[]);
        embedded.add_asset(Path::new("directory/asset2.png"), &[]);

        assert!(block_on(embedded.read_directory(Path::new("asset.png"))).is_err());

        let stream = block_on(embedded.read_directory(Path::new("directory"))).unwrap();
        let mut listed: Vec<String> = block_on(stream.collect::<Vec<_>>())
            .iter()
            .map(|path| path.to_string_lossy().to_string())
            .collect();
        listed.sort();
        assert_eq!(listed, ["directory/asset.png", "directory/asset2.png"]);
    }

    #[test]
    fn an_assets_settings_are_read_from_its_meta_file() {
        assert_eq!(meta_path(Path::new("a/b.png")), Path::new("a/b.png.meta"));
        assert_eq!(meta_path(Path::new("a/b")), Path::new("a/b.meta"));
    }

    #[test]
    fn every_file_of_the_folder_is_compiled_in() {
        let embedded = EmbeddedAssetReader::preloaded();
        let folder = std::path::PathBuf::from(std::env::var("BEVY_ASSET_PATH").unwrap());

        for (path, bytes) in &embedded.loaded {
            assert_eq!(std::fs::read(folder.join(path)).unwrap(), *bytes, "{}", path.display());
        }
        assert!(!embedded.loaded.is_empty());
    }
}

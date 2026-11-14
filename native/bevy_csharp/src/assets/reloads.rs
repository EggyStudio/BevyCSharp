//! The assets Bevy's watcher read again from their files, for the managed side to be told.
//!
//! Bevy says an asset changed with `AssetEvent::Modified`, which it sends as well when code edits
//! an asset in place, a material's color set or an image's pixels written, so a modified asset is
//! not on its own a file read again. A load from a file ends with `LoadedWithDependencies`, which
//! code editing an asset never sends, so an id whose load ends a second time is one Bevy read again
//! from its file. Each system keeps the ids whose load has ended and forgets one Bevy removes, so
//! an asset dropped and loaded afresh later is a first load again.
//!
//! Each asset type has its messages of its own, so each kind the managed side loads gets a system
//! reading them with a cursor of its own. A type a profile does not register has no messages, and
//! its system finds none and does nothing, so one list serves every profile.

use std::collections::HashSet;

use bevy::app::{App, Last};
use bevy::asset::io::AssetSourceId;
use bevy::asset::{Asset, AssetEvent, AssetId, AssetServer};
use bevy::ecs::message::{MessageCursor, Messages};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Local, Res, ResMut};

use crate::interop::status;
use crate::state::with_world;

/// The assets read again since the managed side last drained them, and the ones it is reading.
#[derive(Resource, Default)]
pub struct AssetReloads {
    /// Each one's path and kind, waiting for the next drain.
    waiting: Vec<(String, &'static str)>,
    /// What the last drain took, read one text at a time by [`bcs_asset_reload_path`] and
    /// [`bcs_asset_reload_kind`].
    drained: Vec<(String, &'static str)>,
}

/// Adds the queue and a system for each kind of asset the managed side loads by path.
pub fn install(app: &mut App) {
    app.init_resource::<AssetReloads>();
    app.add_systems(
        Last,
        (
            note::<bevy::image::Image>("Image"),
            note::<bevy::mesh::Mesh>("Mesh"),
            note::<bevy::world_serialization::WorldAsset>("Scene"),
        ),
    );

    #[cfg(feature = "render")]
    app.add_systems(
        Last,
        (
            note::<bevy::pbr::StandardMaterial>("StandardMaterial"),
            note::<bevy::gltf::Gltf>("Gltf"),
            note::<bevy::animation::AnimationClip>("AnimationClip"),
            note::<bevy::audio::AudioSource>("Audio"),
            note::<bevy::text::Font>("Font"),
        ),
    );

    #[cfg(feature = "meshlet")]
    app.add_systems(
        Last,
        note::<bevy::pbr::experimental::meshlet::MeshletMesh>("MeshletMesh"),
    );
}

/// A system noting the assets of type `A` read again from their files this frame, under `kind`,
/// the name the managed side loads them by.
fn note<A: Asset>(
    kind: &'static str,
) -> impl FnMut(
    Option<Res<Messages<AssetEvent<A>>>>,
    Local<(MessageCursor<AssetEvent<A>>, HashSet<AssetId<A>>)>,
    Res<AssetServer>,
    ResMut<AssetReloads>,
) {
    move |messages, mut kept, server, mut reloads| {
        let Some(messages) = messages else {
            return;
        };
        let (cursor, loaded) = &mut *kept;

        for event in cursor.read(&messages) {
            match event {
                AssetEvent::LoadedWithDependencies { id } => {
                    if loaded.insert(*id) {
                        continue;
                    }
                    if let Some(path) = server.get_path(*id) {
                        reloads.waiting.push((path.to_string(), kind));
                    }
                }
                AssetEvent::Removed { id } => {
                    loaded.remove(id);
                }
                _ => {}
            }
        }
    }
}

/// Collects the assets read again from their files since the last call, and reports how many.
///
/// The path and the kind are read afterwards with [`bcs_asset_reload_path`] and
/// [`bcs_asset_reload_kind`]. A file holding several assets, a glTF's meshes, materials and scenes,
/// is one entry for each, the path naming the asset within the file after a `#`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_reloads_drain() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Some(mut reloads) = world.get_resource_mut::<AssetReloads>() else {
                return 0;
            };
            let reloads = &mut *reloads;
            reloads.drained = std::mem::take(&mut reloads.waiting);
            reloads.drained.len() as i32
        })
    })
}

/// Writes the path of one drained reload into `out`, and returns its length in bytes.
///
/// The usual text convention. Pass null with a capacity of zero to learn the length, then call
/// again with a buffer that large.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_reload_path(index: i32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| read(index, out, capacity, Text::Path))
}

/// Writes the kind of one drained reload into `out`, the name the managed side loads it by, and
/// returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_reload_kind(index: i32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| read(index, out, capacity, Text::Kind))
}

/// Reports whether Bevy watches the default asset source, the asset root, for changes, `1` when it
/// does.
///
/// Bevy gives a source the receiver of its watcher's changes only where it built the watcher,
/// which takes a build carrying one, a config asking and a folder the system let it watch
/// ([`super::watching`]). The server's own flag is only what the config asked, and says yes on a
/// build with no watcher at all.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_assets_watching() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| match world.get_resource::<AssetServer>() {
            Some(server) => server
                .get_source(AssetSourceId::Default)
                .is_ok_and(|source| source.event_receiver().is_some())
                .into(),
            None => 0,
        })
    })
}

/// Which of a reload's two texts to write.
enum Text {
    Path,
    Kind,
}

/// Writes one text of a drained reload.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
fn read(index: i32, out: *mut u8, capacity: i32, which: Text) -> i32 {
    let Ok(index) = usize::try_from(index) else {
        return status::NULL_ARG;
    };

    with_world(|world| {
        let Some(reloads) = world.get_resource::<AssetReloads>() else {
            return status::UNSUPPORTED;
        };
        let Some((path, kind)) = reloads.drained.get(index) else {
            return status::NO_ENTITY;
        };
        let text = match which {
            Text::Path => path.as_str(),
            Text::Kind => kind,
        };

        unsafe { crate::interop::write_text(text, out, capacity) }
    })
}

#[cfg(test)]
mod tests {
    use std::path::{Path, PathBuf};
    use std::time::Duration;

    use bevy::app::{App, Last, TaskPoolPlugin};
    use bevy::asset::io::Reader;
    use bevy::asset::{Asset, AssetApp, AssetLoader, AssetPlugin, AssetServer, Handle, LoadContext};
    use bevy::reflect::TypePath;

    use super::{AssetReloads, note};

    /// A file's text, the smallest asset a file can be read into.
    #[derive(Asset, TypePath)]
    struct Text(String);

    /// Reads a `.txt` file into a [`Text`].
    #[derive(TypePath)]
    struct TextLoader;

    impl AssetLoader for TextLoader {
        type Asset = Text;
        type Settings = ();
        type Error = std::io::Error;

        async fn load(&self, reader: &mut dyn Reader, _: &(), _: &mut LoadContext<'_>) -> Result<Text, std::io::Error> {
            let mut bytes = Vec::new();
            reader.read_to_end(&mut bytes).await?;
            Ok(Text(String::from_utf8_lossy(&bytes).into_owned()))
        }

        fn extensions(&self) -> &[&str] {
            &["txt"]
        }
    }

    /// A folder of its own holding `note.txt`.
    fn folder(name: &str) -> PathBuf {
        let path = std::env::temp_dir().join(format!("bcs_reloads_{name}_{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&path);
        std::fs::create_dir_all(&path).unwrap();
        std::fs::write(path.join("note.txt"), "one").unwrap();
        path
    }

    /// An app reading assets from `root` with no watcher, noting texts read again as the bridge's
    /// apps note their assets.
    fn app(root: &Path) -> App {
        let mut app = App::new();
        app.add_plugins((
            TaskPoolPlugin::default(),
            AssetPlugin {
                file_path: root.to_string_lossy().into_owned(),
                watch_for_changes_override: Some(false),
                ..Default::default()
            },
        ));
        app.init_asset::<Text>().register_asset_loader(TextLoader);
        app.init_resource::<AssetReloads>();
        app.add_systems(Last, note::<Text>("Text"));
        app
    }

    /// Runs the app's frames until `done` holds, and a few more so the frame's messages are read.
    fn until(app: &mut App, what: &str, done: impl Fn(&App) -> bool) {
        for _ in 0..1000 {
            app.update();
            if done(app) {
                for _ in 0..3 {
                    app.update();
                }
                return;
            }
            std::thread::sleep(Duration::from_millis(5));
        }
        panic!("{what} did not happen within the frames given");
    }

    fn noted(app: &App) -> Vec<(String, &'static str)> {
        app.world().resource::<AssetReloads>().waiting.clone()
    }

    /// A file's first load is no reload, and the load Bevy's watcher asks for when the file
    /// changes, a load of the same path forced again, is noted once with its path and kind.
    #[test]
    fn a_file_loaded_again_is_noted_once_and_its_first_load_is_not() {
        let root = folder("again");
        let mut app = app(&root);

        let handle: Handle<Text> = app.world().resource::<AssetServer>().load("note.txt");
        until(&mut app, "the first load", |app| app.world().resource::<AssetServer>().is_loaded_with_dependencies(&handle));
        let first = noted(&app);

        std::fs::write(root.join("note.txt"), "two").unwrap();
        app.world().resource::<AssetServer>().reload("note.txt");
        until(&mut app, "the load again", |app| !noted(app).is_empty());
        let read = app.world().resource::<bevy::asset::Assets<Text>>().get(&handle).map(|text| text.0.clone());

        let _ = std::fs::remove_dir_all(&root);
        assert!(first.is_empty(), "a first load was noted as a reload, {first:?}");
        assert_eq!(noted(&app), vec![("note.txt".to_string(), "Text")]);
        assert_eq!(read.as_deref(), Some("two"), "the file was not read again");
    }

    /// An asset dropped and loaded afresh from the same file is a first load again, since Bevy
    /// removes the asset the last handle let go of.
    #[test]
    fn an_asset_dropped_and_loaded_afresh_is_a_first_load() {
        let root = folder("afresh");
        let mut app = app(&root);

        let handle: Handle<Text> = app.world().resource::<AssetServer>().load("note.txt");
        until(&mut app, "the first load", |app| app.world().resource::<AssetServer>().is_loaded_with_dependencies(&handle));
        let id = handle.id();
        drop(handle);
        until(&mut app, "the asset's removal", |app| !app.world().resource::<bevy::asset::Assets<Text>>().contains(id));

        let handle: Handle<Text> = app.world().resource::<AssetServer>().load("note.txt");
        until(&mut app, "the second load", |app| app.world().resource::<AssetServer>().is_loaded_with_dependencies(&handle));

        let _ = std::fs::remove_dir_all(&root);
        assert_eq!(noted(&app), vec![], "an asset loaded afresh was noted as a reload");
    }
}

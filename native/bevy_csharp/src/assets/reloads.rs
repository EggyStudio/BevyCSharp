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

/// Reports whether Bevy's asset server watches its sources for changes, `1` when it does.
///
/// It does where the build carries the watcher and the config asked for it, and nowhere else,
/// since the bridge says no outright where the config did not ask.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_assets_watching() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| match world.get_resource::<AssetServer>() {
            Some(server) if server.watching_for_changes() => 1,
            _ => 0,
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

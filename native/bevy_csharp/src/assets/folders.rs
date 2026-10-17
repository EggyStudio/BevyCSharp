//! A folder of assets loaded at once, as Bevy's `AssetServer::load_folder` loads one, every file in
//! it loaded in parallel, and the folder an asset of its own holding their handles, loaded once all
//! of them are.

use bevy::asset::{AssetServer, Assets, LoadedFolder};

use crate::interop::status;
use crate::state::with_world;

/// Starts loading every asset in a folder, and its folders, and answers the folder's handle C#
/// refers to it by.
///
/// # Safety
/// `path` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_load_folder(path: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
            return status::NULL_ARG;
        };

        with_world(|world| {
            // A clone of the server, which shares its state, so the world is free for the key.
            let Some(server) = world.get_resource::<AssetServer>().cloned() else {
                return status::UNSUPPORTED;
            };

            let handle = server.load_folder(path).untyped();
            super::key_for(world, handle)
        })
    })
}

/// Writes the keys of the assets a loaded folder holds to `out`, as many as `capacity` allows, and
/// answers how many it holds, so a caller with too little room asks again with enough.
///
/// Answers [`status::NOT_PRESENT`] while the folder or any asset in it is still loading, and
/// [`status::INVALID_STATE`] for a key that names something other than a folder.
///
/// # Safety
/// `out` must be writable for `capacity` `i32`s, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_folder_handles(folder: i32, out: *mut i32, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let Some(handle) = super::clone_handle(world, folder) else {
                return status::NOT_PRESENT;
            };
            let Ok(handle) = handle.try_typed::<LoadedFolder>() else {
                return status::INVALID_STATE;
            };
            let Some(handles) = world
                .get_resource::<Assets<LoadedFolder>>()
                .and_then(|folders| folders.get(&handle))
                .map(|folder| folder.handles.clone())
            else {
                return status::NOT_PRESENT;
            };

            for (index, handle) in handles.iter().enumerate() {
                let key = super::key_for(world, handle.clone());
                if (index as i32) < capacity {
                    unsafe { out.add(index).write(key) };
                }
            }
            handles.len() as i32
        })
    })
}

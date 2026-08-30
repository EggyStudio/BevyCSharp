//! Asset loading, and the table that lets C# hold on to what it loaded.
//!
//! Bevy's `Handle<T>` is generic and reference counted. Dropping one decrements a count, and
//! keeping one alive is what stops the asset being unloaded. Neither survives a trip through a C
//! ABI, so managed code never sees a handle. It gets an index into a table on this side that
//! owns the real thing, and hands the index back when it wants to use or release it.
//!
//! The table stores `UntypedHandle` rather than one slab per asset type. An untyped handle is
//! still a strong reference, so it keeps the asset loaded, and its load state can be queried
//! without knowing what it points at. Retyping it is only needed at the point an asset is
//! attached to an entity.

use bevy::asset::{AssetServer, LoadState, UntypedHandle};
use bevy::ecs::resource::Resource;
use bevy::ecs::world::World;

use crate::interop::status;
use crate::state::{with_world, with_world_opt};

/// Load states as C# sees them. Mirrors `Bevy.AssetLoadState`.
pub mod load_state {
    /// The handle is not known to this app.
    pub const UNKNOWN: i32 = 0;
    /// Queued, but not started.
    pub const NOT_LOADED: i32 = 1;
    /// In progress.
    pub const LOADING: i32 = 2;
    /// Available.
    pub const LOADED: i32 = 3;
    /// Gave up. The reason is on Bevy's log.
    pub const FAILED: i32 = 4;
}

/// The handles C# is currently holding.
///
/// Freed slots are reused, so an index alone would be ambiguous after a release: a stale index
/// could name a slot that has since been handed to something else. Each slot therefore carries a
/// generation, and the value C# holds packs both, in the same spirit as an `Entity`.
#[derive(Resource, Default)]
pub struct AssetHandles {
    slots: Vec<Slot>,
    free: Vec<u32>,
}

#[derive(Default)]
struct Slot {
    handle: Option<UntypedHandle>,
    generation: u32,
}

/// Packs a slot index and generation into the single integer C# holds.
fn pack(index: u32, generation: u32) -> i32 {
    // 20 bits of index, 11 of generation, and the sign bit left clear so a valid handle is never
    // mistaken for one of the negative status codes.
    ((generation & 0x7FF) << 20 | (index & 0xF_FFFF)) as i32
}

/// Splits the integer C# holds back into a slot index and generation.
fn unpack(packed: i32) -> Option<(usize, u32)> {
    if packed < 0 {
        return None;
    }
    let packed = packed as u32;
    Some(((packed & 0xF_FFFF) as usize, (packed >> 20) & 0x7FF))
}

impl AssetHandles {
    /// Takes ownership of a handle and returns the value C# will refer to it by.
    fn insert(&mut self, handle: UntypedHandle) -> i32 {
        if let Some(index) = self.free.pop() {
            let slot = &mut self.slots[index as usize];
            slot.handle = Some(handle);
            return pack(index, slot.generation);
        }

        if self.slots.len() >= 0xF_FFFF {
            return status::INVALID_STATE;
        }

        let index = self.slots.len() as u32;
        self.slots.push(Slot {
            handle: Some(handle),
            generation: 0,
        });
        pack(index, 0)
    }

    /// Borrows a handle, rejecting one whose slot has since been reused.
    fn get(&self, packed: i32) -> Option<&UntypedHandle> {
        let (index, generation) = unpack(packed)?;
        let slot = self.slots.get(index)?;
        if slot.generation != generation {
            return None;
        }
        slot.handle.as_ref()
    }

    /// Drops a handle and frees its slot for reuse.
    fn remove(&mut self, packed: i32) -> bool {
        let Some((index, generation)) = unpack(packed) else {
            return false;
        };
        let Some(slot) = self.slots.get_mut(index) else {
            return false;
        };
        if slot.generation != generation || slot.handle.is_none() {
            return false;
        }

        slot.handle = None;
        // Wrapping keeps the packing width honest. A slot would have to be recycled two thousand
        // times before a stale value could collide, by which point it is long discarded.
        slot.generation = slot.generation.wrapping_add(1) & 0x7FF;
        self.free.push(index as u32);
        true
    }

    /// How many handles C# is holding.
    fn live(&self) -> i32 {
        self.slots.iter().filter(|s| s.handle.is_some()).count() as i32
    }
}

/// Takes ownership of a handle and returns the value C# will refer to it by.
///
/// Used by assets built in memory rather than loaded from a file.
pub(crate) fn insert_handle(world: &mut World, handle: UntypedHandle) -> i32 {
    world.get_resource_or_init::<AssetHandles>().insert(handle)
}

/// Clones the handle behind a key, or returns `None` if the key names nothing.
///
/// A clone rather than a borrow, because the caller needs the world back to insert it.
pub(crate) fn clone_handle(world: &World, key: i32) -> Option<UntypedHandle> {
    world.get_resource::<AssetHandles>()?.get(key).cloned()
}

/// Starts loading `path` as the asset type named by `kind`.
///
/// The kind is a name rather than a type because C# has no way to name a Rust type. Which names
/// are accepted depends on what this build was compiled with: a headless build has the data-only
/// asset types, a render build adds the ones that need the GPU pipeline.
///
/// Scenes are absent. In 0.19 `Scene` became a trait rather than a loadable asset, and glTF
/// scenes come from `bevy_gltf`, which this crate does not depend on yet.
fn load(world: &mut World, kind: &str, path: &str) -> i32 {
    let Some(server) = world.get_resource::<AssetServer>() else {
        return status::UNSUPPORTED;
    };

    let handle: UntypedHandle = match kind {
        "Mesh" => server.load::<bevy::mesh::Mesh>(path.to_string()).untyped(),
        "Image" => server.load::<bevy::image::Image>(path.to_string()).untyped(),
        #[cfg(feature = "render")]
        "StandardMaterial" => server
            .load::<bevy::pbr::StandardMaterial>(path.to_string())
            .untyped(),
        #[cfg(feature = "render")]
        "Shader" => server.load::<bevy::shader::Shader>(path.to_string()).untyped(),
        _ => return status::NO_COMPONENT,
    };

    world
        .get_resource_or_init::<AssetHandles>()
        .insert(handle)
}

/// Starts loading an asset and returns the handle C# refers to it by.
///
/// Returns a negative status on failure: [`status::NO_COMPONENT`] when the kind is not one this
/// build knows, [`status::UNSUPPORTED`] when the app has no asset server.
///
/// # Safety
/// `kind` and `path` must be NUL-terminated UTF-8 strings.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_load(
    kind: *const core::ffi::c_char,
    path: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        let Some(kind) = (unsafe { crate::interop::cstr_to_string(kind) }) else {
            return status::NULL_ARG;
        };
        let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
            return status::NULL_ARG;
        };

        with_world(|world| load(world, &kind, &path))
    })
}

/// Reports how far along a load is. See the `load_state` module for the values.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_load_state(handle: i32) -> i32 {
    crate::interop::guard(|| {
        with_world_opt(|world| {
            let Some(handles) = world.get_resource::<AssetHandles>() else {
                return load_state::UNKNOWN;
            };
            let Some(id) = handles.get(handle).map(|h| h.id()) else {
                return load_state::UNKNOWN;
            };
            let Some(server) = world.get_resource::<AssetServer>() else {
                return load_state::UNKNOWN;
            };

            match server.get_load_state(id) {
                Some(LoadState::NotLoaded) => load_state::NOT_LOADED,
                Some(LoadState::Loading) => load_state::LOADING,
                Some(LoadState::Loaded) => load_state::LOADED,
                Some(LoadState::Failed(_)) => load_state::FAILED,
                None => load_state::UNKNOWN,
            }
        })
        .unwrap_or(load_state::UNKNOWN)
    })
}

/// Reports whether a handle still names something this app is holding.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_is_valid(handle: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let valid = world
                .get_resource::<AssetHandles>()
                .and_then(|handles| handles.get(handle))
                .is_some();
            i32::from(valid)
        })
    })
}

/// Releases a handle. The asset stays loaded while anything else still refers to it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_release(handle: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let released = world
                .get_resource_mut::<AssetHandles>()
                .is_some_and(|mut handles| handles.remove(handle));
            i32::from(released)
        })
    })
}

/// How many handles C# is currently holding, for leak checks in tests.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_live_count() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            world
                .get_resource::<AssetHandles>()
                .map_or(0, AssetHandles::live)
        })
    })
}

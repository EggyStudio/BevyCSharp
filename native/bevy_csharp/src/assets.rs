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

use bevy::app::App;
use bevy::asset::{Asset, AssetApp, AssetServer, Assets, LoadState, UntypedHandle};
use bevy::ecs::resource::Resource;
use bevy::ecs::world::World;

use crate::interop::status;
use crate::state::{with_world, with_world_opt};

/// Registers an asset type unless the app already has one, and reports whether it did.
///
/// `App::init_asset` is destructive rather than idempotent. It builds a fresh `Assets<A>`, gives
/// the asset server a second handle provider for the type, inserts the new store over whatever
/// was there and adds another copy of the per-frame asset systems. Handles minted before that
/// point come from a different id space than the one anything reads afterwards, so they resolve
/// to nothing while every call still reports success: on a windowed build this showed up as
/// every mesh and material drawing with the fallback and no error anywhere.
///
/// Which types a profile has to register is a question about plugins, and it is answered in
/// [`crate::app`]. This is the guard that keeps a wrong answer inert.
pub fn init_asset_once<A: Asset>(app: &mut App) -> bool {
    if app.world().contains_resource::<Assets<A>>() {
        return false;
    }

    app.init_asset::<A>();
    true
}

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
/// Used by assets built in memory rather than loaded from a file: a mesh, a material, an atlas
/// layout.
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
/// A path may name a sub-asset, which is how one glTF file yields many assets: everything after a
/// `#` is a label Bevy resolves against the file. `ship.gltf#Mesh0/Primitive0` is a `Mesh` and
/// `ship.gltf#Material0` a `StandardMaterial`, so a glTF part arrives as an ordinary handle of the
/// kind it already is, and needs no bridge of its own. `Gltf` itself loads the whole file, which
/// is the description rather than anything drawable.
///
/// Whole scenes are absent. In 0.19 a glTF scene is a `WorldAsset` rather than the old `Scene`
/// asset, which is a different mechanism from anything else here and needs its own bridge.
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
        #[cfg(feature = "render")]
        "Gltf" => server.load::<bevy::gltf::Gltf>(path.to_string()).untyped(),
        #[cfg(feature = "render")]
        "Audio" => server
            .load::<bevy::audio::AudioSource>(path.to_string())
            .untyped(),
        #[cfg(feature = "render")]
        "Font" => server.load::<bevy::text::Font>(path.to_string()).untyped(),
        "Scene" => server
            .load::<bevy::world_serialization::WorldAsset>(path.to_string())
            .untyped(),
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

/// Starts loading an image with the sampler it should be drawn with.
///
/// The plain [`bcs_asset_load`] takes Bevy's default sampler, which clamps at the edges. A
/// texture meant to tile has to say so, and so does one whose bytes are data rather than colour:
/// a normal map read as sRGB has every direction in it bent.
///
/// # Safety
/// `path` must be a NUL-terminated UTF-8 string; `config` must point to a readable
/// [`BcsImageConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_load_image(
    path: *const core::ffi::c_char,
    config: *const crate::interop::BcsImageConfig,
) -> i32 {
    crate::interop::guard(|| {
        use bevy::image::{
            ImageAddressMode, ImageFilterMode, ImageLoaderSettings, ImageSampler,
            ImageSamplerDescriptor,
        };

        let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
            return status::NULL_ARG;
        };
        if config.is_null() {
            return status::NULL_ARG;
        }
        let config = unsafe { *config };

        let address = |mode: i32| match mode {
            1 => ImageAddressMode::Repeat,
            2 => ImageAddressMode::MirrorRepeat,
            _ => ImageAddressMode::ClampToEdge,
        };
        let filter = |mode: i32| match mode {
            1 => ImageFilterMode::Linear,
            _ => ImageFilterMode::Nearest,
        };

        let mag_filter = filter(config.mag_filter);
        let min_filter = filter(config.min_filter);
        let mipmap_filter = filter(config.mipmap_filter);

        // wgpu rejects anisotropy above one unless all three filters are linear, and rejects it
        // as a validation failure rather than by ignoring it. Asking for both is a mistake worth
        // absorbing here rather than turning into a crash at draw time.
        let anisotropy = if mag_filter == ImageFilterMode::Linear
            && min_filter == ImageFilterMode::Linear
            && mipmap_filter == ImageFilterMode::Linear
        {
            config.anisotropy.max(1)
        } else {
            1
        };

        let descriptor = ImageSamplerDescriptor {
            address_mode_u: address(config.address_u),
            address_mode_v: address(config.address_v),
            mag_filter,
            min_filter,
            mipmap_filter,
            anisotropy_clamp: anisotropy as u16,
            ..Default::default()
        };
        let srgb = config.srgb != 0;

        with_world(|world| {
            let Some(server) = world.get_resource::<AssetServer>() else {
                return status::UNSUPPORTED;
            };

            let handle = server
                .load_builder()
                .with_settings(move |settings: &mut ImageLoaderSettings| {
                    settings.sampler = ImageSampler::Descriptor(descriptor.clone());
                    settings.is_srgb = srgb;
                })
                .load::<bevy::image::Image>(path.to_string())
                .untyped();

            world.get_resource_or_init::<AssetHandles>().insert(handle)
        })
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
                // The server only tracks what it loaded from a path. An asset built in memory
                // has no record, and reaching here means the table still holds a strong handle
                // to it, so it exists and is ready.
                None => load_state::LOADED,
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

/// Spawns a scene asset and returns the entity it was spawned under, or `0` on failure.
///
/// The entity comes back immediately; the scene beneath it does not. Bevy spawns the world as
/// children of this entity once the asset has loaded, so an entity with no children yet is the
/// normal answer on the first frame. `WorldInstance` appears on it when the spawn has happened,
/// which is what makes the wait observable.
///
/// This is what a glTF scene and a `.scn.ron` file have in common: both load as a `WorldAsset`,
/// and both spawn by pointing an entity at one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_scene_spawn(asset: i32) -> u64 {
    crate::interop::guard_with(0u64, || {
        use bevy::world_serialization::{WorldAsset, WorldAssetRoot};

        with_world_opt(|world| {
            let Some(handle) = clone_handle(world, asset) else {
                return 0;
            };

            world
                .spawn(WorldAssetRoot(handle.typed::<WorldAsset>()))
                .id()
                .to_bits()
        })
        .unwrap_or(0)
    })
}

/// Builds an atlas layout over a grid of equal tiles, and returns its key or a negative status.
///
/// The layout is a list of rectangles and nothing else: it names where each frame sits, while the
/// image it describes stays a separate asset. That is why no image is passed here, and why one
/// layout serves every sheet cut the same way.
///
/// `padding` is the gap between neighbouring tiles and `offset` the margin before the first one,
/// both in pixels, both zero for a sheet cut flush to its edges.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_atlas_create(
    tile_width: u32,
    tile_height: u32,
    columns: u32,
    rows: u32,
    padding_x: u32,
    padding_y: u32,
    offset_x: u32,
    offset_y: u32,
) -> i32 {
    crate::interop::guard(|| {
        use bevy::asset::Assets;
        use bevy::image::TextureAtlasLayout;
        use bevy::math::UVec2;

        if tile_width == 0 || tile_height == 0 || columns == 0 || rows == 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let layout = TextureAtlasLayout::from_grid(
                UVec2::new(tile_width, tile_height),
                columns,
                rows,
                Some(UVec2::new(padding_x, padding_y)),
                Some(UVec2::new(offset_x, offset_y)),
            );

            let Some(mut layouts) = world.get_resource_mut::<Assets<TextureAtlasLayout>>() else {
                return status::INVALID_STATE;
            };

            let handle = layouts.add(layout).untyped();
            insert_handle(world, handle)
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::mesh::{Mesh, MeshBuilder, Meshable};
    use bevy::math::primitives::Cuboid;

    /// An app with assets and nothing else, which is all these tests need.
    fn asset_app() -> App {
        let mut app = App::new();
        app.add_plugins(bevy::app::TaskPoolPlugin::default());
        app.add_plugins(bevy::asset::AssetPlugin::default());
        app
    }

    fn a_mesh() -> Mesh {
        Cuboid::new(1.0, 1.0, 1.0).mesh().build()
    }

    #[test]
    fn a_slot_survives_the_round_trip_through_one_integer() {
        for index in [0u32, 1, 0xF_FFFF] {
            for generation in [0u32, 1, 0x7FF] {
                let packed = pack(index, generation);
                assert!(packed >= 0, "{index}/{generation} packed to a status code");
                assert_eq!(Some((index as usize, generation)), unpack(packed));
            }
        }
    }

    #[test]
    fn a_negative_handle_names_nothing() {
        // Every failure the C ABI reports is a negative integer, so one arriving back as a
        // handle is a caller that did not check rather than a slot to look up.
        assert_eq!(None, unpack(-1));
        assert_eq!(None, unpack(status::NO_COMPONENT));
    }

    #[test]
    fn registering_an_asset_type_twice_keeps_the_handles_from_the_first_time() {
        let mut app = asset_app();

        assert!(init_asset_once::<Mesh>(&mut app), "the first call registers");

        let handle = app.world_mut().resource_mut::<Assets<Mesh>>().add(a_mesh());

        assert!(!init_asset_once::<Mesh>(&mut app), "the second call declines");
        assert!(
            app.world().resource::<Assets<Mesh>>().get(&handle).is_some(),
            "the mesh went missing, so the store was replaced");
    }

    #[test]
    fn registering_an_asset_type_twice_through_bevy_loses_them() {
        // Pins the behaviour `init_asset_once` exists to guard, so that if Bevy ever makes
        // `init_asset` idempotent this fails and says the guard can go. Nothing reports an
        // error here, which is what made the original failure so hard to place: the handle is
        // valid, the call succeeded, and the mesh is gone.
        let mut app = asset_app();
        app.init_asset::<Mesh>();

        let handle = app.world_mut().resource_mut::<Assets<Mesh>>().add(a_mesh());
        app.init_asset::<Mesh>();

        assert!(app.world().resource::<Assets<Mesh>>().get(&handle).is_none());
    }
}

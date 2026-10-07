//! Keys released once nothing but the table holds their asset.
//!
//! The table keeps every asset C# names alive until C# releases the key, which suits a mesh a game
//! made once to spawn with again and again, and does not suit the meshes and materials a scene file
//! makes for its entities. A level loaded again makes them again, and the ones the last load made
//! stay held by keys that nothing will release, a leak the length of the game. A key marked here
//! belongs to whatever uses its asset instead. Once only the table holds it, the key is released,
//! as Bevy lets an asset go when its last handle does, and C# is told which keys went so it can
//! forget what it kept for them.
//!
//! An asset is held outside the table by every strong handle cloned from it, a component drawing
//! with it among them. Several keys can hold one asset, a file loaded twice taking a key each
//! time, so an asset counts as unused when its handles are as many as the keys holding it.

use std::collections::HashMap;
use std::sync::Arc;

use bevy::asset::UntypedHandle;

use super::{next, pack, AssetHandles};
use crate::interop::status;
use crate::state::with_world;

/// How many sweeps in a row must find a marked key's asset unused before the key goes.
///
/// More than one, since an asset is made a moment before the component that draws with it is put
/// on, and a sweep falling between the two would release a key about to be used.
const IDLE_SWEEPS: u8 = 2;

impl AssetHandles {
    /// Marks a key to go once nothing outside the table holds its asset, reporting whether the
    /// key named anything.
    fn release_when_unused(&mut self, packed: i32) -> bool {
        let Some((index, generation)) = super::unpack(packed) else {
            return false;
        };
        match self.slots.get_mut(index) {
            Some(slot) if slot.generation == generation && slot.handle.is_some() => {
                slot.goes = true;
                slot.idle = 0;
                true
            }
            _ => false,
        }
    }

    /// Releases every marked key whose asset has been unused for [`IDLE_SWEEPS`] sweeps, and every
    /// marked key another key already holds its asset for, adding the keys released to
    /// [`AssetHandles::released`].
    ///
    /// A file loaded again takes another key to the same asset, a model placed by each load of a
    /// level among them, and the asset stays in use by what the latest load placed, so the keys the
    /// earlier loads took would never be found unused. One key to an asset is all C# needs, so a
    /// marked one beside another goes at once. The key kept is the first in the table, the one a
    /// handle read back off a component is given, so a key read back is never the one that goes.
    fn sweep(&mut self) {
        if !self.slots.iter().any(|slot| slot.goes) {
            return;
        }

        // How many keys hold each asset and the first of them, by the address of its shared count.
        let mut keys: HashMap<*const (), (usize, usize)> = HashMap::new();
        for (index, slot) in self.slots.iter().enumerate() {
            if let Some(UntypedHandle::Strong(strong)) = &slot.handle {
                keys.entry(Arc::as_ptr(strong).cast()).or_insert((0, index)).0 += 1;
            }
        }

        for (index, slot) in self.slots.iter_mut().enumerate() {
            if !slot.goes {
                continue;
            }
            let Some(UntypedHandle::Strong(strong)) = &slot.handle else {
                continue;
            };

            let (held, first) = keys[&Arc::as_ptr(strong).cast()];
            if index == first {
                let unused = Arc::strong_count(strong) <= held;
                slot.idle = if unused { slot.idle.saturating_add(1) } else { 0 };
                if slot.idle < IDLE_SWEEPS {
                    continue;
                }
            }

            self.released.push(pack(index as u32, slot.generation));
            slot.handle = None;
            slot.generation = next(slot.generation);
            slot.goes = false;
            slot.idle = 0;
            self.free.push(index as u32);
        }
    }
}

/// Marks a key to be released once nothing but the table holds its asset, as a component drawing
/// with it does until it is despawned.
///
/// Returns `1` when the key was marked and `0` when it names nothing.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_release_when_unused(handle: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let marked = world
                .get_resource_mut::<AssetHandles>()
                .is_some_and(|mut handles| handles.release_when_unused(handle));
            i32::from(marked)
        })
    })
}

/// Releases the marked keys whose assets have gone unused, returning how many released keys wait
/// to be taken by [`bcs_asset_take_released`].
///
/// Called once a frame, since a key goes only after [`IDLE_SWEEPS`] sweeps have found its asset
/// unused, and a sweep made more often would shorten the frames that allows.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_asset_sweep() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let Some(mut handles) = world.get_resource_mut::<AssetHandles>() else {
                return 0;
            };
            handles.sweep();
            handles.released.len() as i32
        })
    })
}

/// Writes the keys released by [`bcs_asset_sweep`] that have not been taken into `out`, at most
/// `capacity` of them, returning how many were written, so a caller asks again while a buffer is
/// filled.
///
/// # Safety
/// `out` must be writable for `capacity` keys, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_take_released(out: *mut i32, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let capacity = capacity.max(0) as usize;
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }

        with_world(|world| {
            let Some(mut handles) = world.get_resource_mut::<AssetHandles>() else {
                return 0;
            };
            let taken = handles.released.len().min(capacity);
            for (i, key) in handles.released.drain(..taken).enumerate() {
                unsafe { *out.add(i) = key };
            }
            taken as i32
        })
    })
}

#[cfg(test)]
mod tests {
    use bevy::asset::{AssetApp, Assets};
    use bevy::mesh::Mesh;
    use bevy::prelude::*;

    use super::super::AssetHandles;

    fn app() -> App {
        let mut app = App::new();
        app.add_plugins((TaskPoolPlugin::default(), AssetPlugin::default()));
        app.init_asset::<Mesh>();
        app
    }

    #[test]
    fn a_marked_key_goes_once_nothing_draws_with_its_asset_and_an_unmarked_one_stays() {
        let mut app = app();
        let world = app.world_mut();
        let drawn = world.resource_mut::<Assets<Mesh>>().add(Mesh::from(Cuboid::default()));
        let kept = world.resource_mut::<Assets<Mesh>>().add(Mesh::from(Sphere::default()));

        let mut handles = AssetHandles::default();
        let marked = handles.insert(drawn.clone().untyped());
        let unmarked = handles.insert(kept.untyped());
        assert!(handles.release_when_unused(marked));

        // Drawn with, by the clone the test keeps standing in for a component.
        for _ in 0..3 {
            handles.sweep();
        }
        assert!(handles.released.is_empty(), "a key went while its asset was drawn with");

        drop(drawn);
        handles.sweep();
        assert!(handles.released.is_empty(), "a key went on the first sweep that found it unused");
        handles.sweep();
        assert_eq!(handles.released, vec![marked]);
        assert!(handles.get(marked).is_none());
        assert!(handles.get(unmarked).is_some(), "an unmarked key went");
    }

    #[test]
    fn a_marked_key_beside_the_first_key_to_its_asset_goes_at_once_while_the_asset_is_used() {
        let mut app = app();
        let mesh = app.world_mut().resource_mut::<Assets<Mesh>>().add(Mesh::from(Cuboid::default()));

        // Two loads of one file, the asset drawn with by what the second placed.
        let mut handles = AssetHandles::default();
        let first = handles.insert(mesh.clone().untyped());
        let second = handles.insert(mesh.clone().untyped());
        handles.release_when_unused(first);
        handles.release_when_unused(second);

        handles.sweep();
        assert_eq!(handles.released, vec![second]);
        assert!(handles.get(first).is_some(), "the first key, the one read back, went");

        for _ in 0..3 {
            handles.sweep();
        }
        assert_eq!(handles.released, vec![second], "the first key went while its asset was drawn with");
        drop(mesh);
    }

    #[test]
    fn an_asset_held_by_two_keys_goes_once_neither_is_used() {
        let mut app = app();
        let mesh = app.world_mut().resource_mut::<Assets<Mesh>>().add(Mesh::from(Cuboid::default()));

        let mut handles = AssetHandles::default();
        let first = handles.insert(mesh.clone().untyped());
        let second = handles.insert(mesh.untyped());
        handles.release_when_unused(first);
        handles.release_when_unused(second);

        handles.sweep();
        handles.sweep();
        let mut released = handles.released.clone();
        released.sort_unstable();
        let mut both = vec![first, second];
        both.sort_unstable();
        assert_eq!(released, both);
    }
}

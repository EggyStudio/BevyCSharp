//! What the bridge holds, read at intervals while a game is played, so that a value which keeps
//! climbing shows as the leak it is. That is the bytes Rust's allocator has handed out and not had
//! back, the entities Bevy keeps, and how many assets of each kind it holds.
//!
//! The bytes are counted by an allocator wrapped around the system's, which every allocation in
//! this library goes through, Bevy's world, its assets and the renderer's copies on the CPU's side
//! among them. What a graphics driver allocates for itself is not Rust's and is not counted here,
//! which the process's resident size, read on the managed side, takes in.

use core::cell::Cell;
use std::alloc::{GlobalAlloc, Layout, System};
use std::collections::BTreeMap;
use std::fmt::Write as _;
use std::sync::atomic::{AtomicI64, AtomicUsize, Ordering};

use bevy::asset::ReflectAsset;
use bevy::prelude::*;

use crate::state::with_world_opt;

/// How many counters the bytes are spread over.
///
/// One counter that every thread adds to would pass its cache line from core to core on each
/// allocation, and Bevy's render and task threads allocate many times a frame. A thread adds to the
/// counter it was given on its first allocation, so threads rarely share one, and a reading sums
/// them all. A block freed on another thread than the one that made it takes its bytes from a
/// different counter, so one counter alone can go below zero while the sum stays right.
const SHARDS: usize = 16;

/// One thread's share of the count, on a cache line of its own.
#[repr(align(64))]
struct Shard {
    bytes: AtomicI64,
    blocks: AtomicI64,
}

impl Shard {
    const fn new() -> Self {
        Self { bytes: AtomicI64::new(0), blocks: AtomicI64::new(0) }
    }
}

static COUNTS: [Shard; SHARDS] = [const { Shard::new() }; SHARDS];
static NEXT_SHARD: AtomicUsize = AtomicUsize::new(0);

thread_local! {
    /// The counter this thread adds to, given on its first allocation.
    ///
    /// Initialized by a constant and with nothing to drop, so reading it never allocates, which an
    /// allocator's own bookkeeping must not, and never registers a destructor.
    static SHARD: Cell<usize> = const { Cell::new(usize::MAX) };
}

/// The counter for the calling thread, or the first one while the thread is being torn down and
/// its own can no longer be read.
#[inline]
fn shard() -> &'static Shard {
    let index = SHARD
        .try_with(|slot| {
            let mut index = slot.get();
            if index == usize::MAX {
                index = NEXT_SHARD.fetch_add(1, Ordering::Relaxed) % SHARDS;
                slot.set(index);
            }
            index
        })
        .unwrap_or(0);
    &COUNTS[index]
}

#[inline]
fn count(bytes: i64, blocks: i64) {
    let shard = shard();
    shard.bytes.fetch_add(bytes, Ordering::Relaxed);
    shard.blocks.fetch_add(blocks, Ordering::Relaxed);
}

/// The system's allocator, counting what it hands out and has back.
struct Counting;

unsafe impl GlobalAlloc for Counting {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        let block = unsafe { System.alloc(layout) };
        if !block.is_null() {
            count(layout.size() as i64, 1);
        }
        block
    }

    unsafe fn alloc_zeroed(&self, layout: Layout) -> *mut u8 {
        let block = unsafe { System.alloc_zeroed(layout) };
        if !block.is_null() {
            count(layout.size() as i64, 1);
        }
        block
    }

    unsafe fn dealloc(&self, block: *mut u8, layout: Layout) {
        unsafe { System.dealloc(block, layout) };
        count(-(layout.size() as i64), -1);
    }

    unsafe fn realloc(&self, block: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        let moved = unsafe { System.realloc(block, layout, new_size) };

        // A failed reallocation leaves the old block as it was, so nothing changed hands.
        if !moved.is_null() {
            count(new_size as i64 - layout.size() as i64, 0);
        }
        moved
    }
}

#[global_allocator]
static ALLOCATOR: Counting = Counting;

/// The bytes and the blocks the allocator has handed out and not had back.
pub fn held() -> (i64, i64) {
    COUNTS.iter().fold((0, 0), |(bytes, blocks), shard| {
        (bytes + shard.bytes.load(Ordering::Relaxed), blocks + shard.blocks.load(Ordering::Relaxed))
    })
}

/// Writes what the bridge holds into `out` as name and number pairs, returning the length in bytes
/// it needs.
///
/// The bytes and blocks Rust's allocator holds come first, then the entities spawned and the
/// entity indices handed out (a despawned entity's index is given again, so the second grows only
/// when more are alive at once than ever before), then the assets of each kind, as
/// `asset.<kind> <count>`, in name order, the kinds with none left out. Without an app running,
/// the bytes and blocks alone.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_memory_describe(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let (bytes, blocks) = held();
        let mut text = format!("nativeBytes {bytes} nativeBlocks {blocks}");

        with_world_opt(|world| {
            let world: &World = world;
            let entities = world.entities();
            let _ = write!(text, " entities {} entityIds {}", entities.count_spawned(), entities.len());

            for (kind, held) in assets(world).into_iter().filter(|&(_, held)| held > 0) {
                let _ = write!(text, " asset.{kind} {held}");
            }
        });

        unsafe { crate::interop::write_text(&text, out, capacity) }
    })
}

/// How many assets of each kind the world holds, by the kind's name.
///
/// Every kind whose reflection says it is an asset, which takes in kinds a plugin registers that
/// the bridge never names, and then the kinds the bridge loads, counted by their own stores, since
/// an app built without the plugins that register their reflection, as a headless one is, still
/// holds them. A kind found both ways is counted once.
fn assets(world: &World) -> BTreeMap<String, usize> {
    let mut kinds = BTreeMap::new();

    if let Some(registry) = world.get_resource::<AppTypeRegistry>() {
        for (registration, asset) in registry.read().iter_with_data::<ReflectAsset>() {
            kinds.insert(name(registration.type_info().type_path_table().short_path()), asset.len(world));
        }
    }

    fn count<A: Asset>(world: &World, kinds: &mut BTreeMap<String, usize>) {
        if let Some(assets) = world.get_resource::<Assets<A>>() {
            kinds.insert(name(A::short_type_path()), assets.len());
        }
    }

    count::<Mesh>(world, &mut kinds);
    count::<Image>(world, &mut kinds);
    count::<bevy::world_serialization::WorldAsset>(world, &mut kinds);

    #[cfg(feature = "render")]
    {
        count::<StandardMaterial>(world, &mut kinds);
        count::<AudioSource>(world, &mut kinds);
        count::<Font>(world, &mut kinds);
        count::<Shader>(world, &mut kinds);
        count::<AnimationClip>(world, &mut kinds);
        count::<bevy::gltf::Gltf>(world, &mut kinds);
    }

    kinds
}

/// A kind's name as one word, since a generic kind's short path holds spaces and commas, which
/// would break a pair.
fn name(path: &str) -> String {
    path.chars().filter(|c| c.is_alphanumeric() || *c == '_').collect()
}

#[cfg(test)]
mod tests {
    use super::held;

    /// A block much larger than anything the other tests allocate beside it, so the count moves by
    /// its size whatever they do at the same time.
    #[test]
    fn a_block_is_counted_while_it_is_held_and_not_after() {
        const SIZE: usize = 64 << 20;
        const SLACK: i64 = 8 << 20;

        let (before, _) = held();
        let block = vec![1u8; SIZE];
        let (during, _) = held();
        drop(block);
        let (after, _) = held();

        assert!(during - before > SIZE as i64 - SLACK, "held {} more while the block was", during - before);
        assert!(during - after > SIZE as i64 - SLACK, "held {} less after it went", during - after);
    }
}

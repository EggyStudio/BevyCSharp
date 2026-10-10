//! The entities that lost a component since the running C# system last asked, as Bevy's
//! `RemovedComponents<T>` gives a Rust system.
//!
//! Bevy writes a message for each component an entity loses, whether by a removal or a despawn,
//! into a buffer per component that it keeps for two frames, and a Rust system reads them through
//! a cursor of its own, so each removal reaches each system once. A C# system is a closure Bevy
//! runs, which holds a cursor for each component it has asked about and makes them the running
//! ones while it runs, so [`bcs_ecs_removed`] reads from the cursor of whichever system called it.

use std::cell::RefCell;
use std::collections::HashMap;

use bevy::ecs::component::ComponentId;

use crate::interop::status;
use crate::state::with_world;

/// A C# system's cursors, the count of removal messages of each component it has read up to.
#[derive(Default)]
pub struct RemovalCursors(HashMap<ComponentId, usize>);

thread_local! {
    /// The running system's cursors, which a system's run lends here and takes back.
    static RUNNING: RefCell<Option<RemovalCursors>> = const { RefCell::new(None) };
}

/// Runs `body` with `cursors` as the running system's, and gives them back after, whatever `body`
/// does, so a system's cursors are its own from one run to the next.
pub fn running<R>(cursors: &mut RemovalCursors, body: impl FnOnce() -> R) -> R {
    let lent = std::mem::take(cursors);
    let outer = RUNNING.with(|running| running.borrow_mut().replace(lent));

    struct Return<'a> {
        cursors: &'a mut RemovalCursors,
        outer: Option<RemovalCursors>,
    }

    impl Drop for Return<'_> {
        fn drop(&mut self) {
            let lent = RUNNING.with(|running| std::mem::replace(&mut *running.borrow_mut(), self.outer.take()));
            *self.cursors = lent.unwrap_or_default();
        }
    }

    let _back = Return { cursors, outer };
    body()
}

/// Lists the entities that lost `component`, by a removal or a despawn, since the running system
/// last asked about it, oldest first, and moves the system's cursor on past them. Only valid inside
/// a C# system.
///
/// Bevy keeps a removal for two frames, so a system asking less often than that misses the older
/// ones, as a Rust system reading `RemovedComponents` does. A system asking for the first time is
/// given every removal Bevy still holds. An entity may already have been despawned, or its index
/// given to a new one.
///
/// The return value is how many there are, whether or not they fitted, and the cursor moves on
/// only when they did, so a caller with too small a buffer asks again with a larger one. Reports
/// [`status::INVALID_STATE`] outside a C# system, which has no last run to count from.
///
/// # Safety
/// `out` must be writable for `capacity` entity ids, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_removed(component: i32, out: *mut u64, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let Some(component) = crate::ecs::component_from(component) else {
            return status::NO_COMPONENT;
        };

        RUNNING.with(|running| {
            let mut running = running.borrow_mut();
            let Some(cursors) = running.as_mut() else {
                return status::INVALID_STATE;
            };

            with_world(|world| {
                let Some(messages) = world.removed_components().get(component) else {
                    return 0;
                };

                let oldest = messages.oldest_message_count();
                let newest = oldest + messages.len();
                let from = cursors.0.get(&component).copied().unwrap_or(0).max(oldest);
                let total = newest.saturating_sub(from);
                if total > capacity.max(0) as usize {
                    return total as i32;
                }

                for (at, id) in (from..newest).enumerate() {
                    let Some((removed, _)) = messages.get_message(id) else { continue };
                    let entity: bevy::ecs::entity::Entity = removed.clone().into();
                    // SAFETY: `at < total <= capacity`, and `out` is valid for `capacity` writes.
                    unsafe { out.add(at).write(entity.to_bits()) };
                }

                cursors.0.insert(component, newest);
                total as i32
            })
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::app::App;
    use bevy::ecs::component::Component;

    use crate::state::loan_world;

    #[derive(Component)]
    struct Health;

    #[test]
    fn a_system_sees_a_removal_and_a_despawn_once() {
        let mut app = App::new();
        let world = app.world_mut();
        let id = world.register_component::<Health>();
        let component = id.index() as i32;
        let (kept, stripped, gone) = (world.spawn(Health).id(), world.spawn(Health).id(), world.spawn(Health).id());
        let mut cursors = RemovalCursors::default();
        let mut out = [0u64; 8];

        let read = |world: &mut bevy::ecs::world::World, cursors: &mut RemovalCursors, out: &mut [u64; 8]| {
            loan_world(world, || running(cursors, || unsafe { bcs_ecs_removed(component, out.as_mut_ptr(), out.len() as i32) }))
        };

        assert_eq!(0, read(world, &mut cursors, &mut out));
        world.entity_mut(stripped).remove::<Health>();
        world.despawn(gone);
        assert_eq!(2, read(world, &mut cursors, &mut out));
        assert_eq!([stripped.to_bits(), gone.to_bits()], [out[0], out[1]]);
        assert_eq!(0, read(world, &mut cursors, &mut out), "each removal is seen once");

        // Another system, asking for the first time, sees both, and outside any system none.
        assert_eq!(2, read(world, &mut RemovalCursors::default(), &mut out));
        assert_eq!(status::INVALID_STATE, loan_world(world, || unsafe { bcs_ecs_removed(component, out.as_mut_ptr(), 8) }));
        let _ = kept;
    }

    #[test]
    fn too_small_a_buffer_moves_nothing_on() {
        let mut app = App::new();
        let world = app.world_mut();
        let id = world.register_component::<Health>();
        let component = id.index() as i32;
        for _ in 0..3 {
            let entity = world.spawn(Health).id();
            world.despawn(entity);
        }

        let mut cursors = RemovalCursors::default();
        let mut few = [0u64; 1];
        let mut all = [0u64; 3];
        loan_world(world, || {
            running(&mut cursors, || {
                assert_eq!(3, unsafe { bcs_ecs_removed(component, few.as_mut_ptr(), 1) });
                assert_eq!(3, unsafe { bcs_ecs_removed(component, all.as_mut_ptr(), 3) });
                assert_eq!(0, unsafe { bcs_ecs_removed(component, all.as_mut_ptr(), 3) });
            })
        });
    }
}

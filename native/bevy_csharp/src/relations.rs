//! Relationships of a game's own between entities, as Bevy's relationships example declares one
//! with its `Targeting` and `TargetedBy`.
//!
//! A relationship in Bevy is a pair of Rust types, one on the entity that relates and one Bevy
//! keeps on the entity related to, listing every entity relating to it, and C# cannot declare a
//! Rust type. So the bridge declares a fixed set of pairs, as it declares state slots, and each C#
//! relationship claims one by number. Bevy's own hooks keep each list as relations are made,
//! changed and taken away, and an entity's relations go with it when it is despawned.

use bevy::ecs::entity::Entity;
use bevy::ecs::world::World;

use crate::interop::status;
use crate::state::with_world;

/// Declares the relationship slots and the dispatch that reaches them by number.
macro_rules! define_relations {
    ($($slot:literal => $relation:ident, $target:ident;)+) => {
        $(
            /// An entity's relation to another, in one slot.
            #[derive(bevy::ecs::component::Component, Debug, Clone, Copy, PartialEq, Eq)]
            #[relationship(relationship_target = $target)]
            pub struct $relation(pub Entity);

            /// The entities relating to this one, in one slot, which Bevy keeps.
            #[derive(bevy::ecs::component::Component, Debug, Default)]
            #[relationship_target(relationship = $relation)]
            pub struct $target(Vec<Entity>);
        )+

        /// How many slots there are.
        const SLOT_COUNT: i32 = [$($slot),+].len() as i32;

        fn relate(world: &mut World, slot: i32, source: Entity, target: Entity) -> i32 {
            let Ok(mut entity) = world.get_entity_mut(source) else {
                return status::NO_ENTITY;
            };
            match slot {
                $($slot => { entity.insert($relation(target)); })+
                _ => return status::NULL_ARG,
            }
            status::OK
        }

        fn unrelate(world: &mut World, slot: i32, source: Entity) -> i32 {
            let Ok(mut entity) = world.get_entity_mut(source) else {
                return status::NO_ENTITY;
            };
            match slot {
                $($slot => { entity.remove::<$relation>(); })+
                _ => return status::NULL_ARG,
            }
            status::OK
        }

        fn target_of(world: &World, slot: i32, source: Entity) -> Option<Entity> {
            let entity = world.get_entity(source).ok()?;
            match slot {
                $($slot => entity.get::<$relation>().map(|relation| relation.0),)+
                _ => None,
            }
        }

        fn sources_of(world: &World, slot: i32, target: Entity) -> Option<Vec<Entity>> {
            let entity = world.get_entity(target).ok()?;
            match slot {
                $($slot => Some(entity.get::<$target>().map(|related| related.0.clone()).unwrap_or_default()),)+
                _ => None,
            }
        }
    };
}

define_relations! {
    0 => Relation0, Related0;
    1 => Relation1, Related1;
    2 => Relation2, Related2;
    3 => Relation3, Related3;
    4 => Relation4, Related4;
    5 => Relation5, Related5;
    6 => Relation6, Related6;
    7 => Relation7, Related7;
}

/// Reports how many relationship slots this bridge provides.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_relation_slots() -> i32 {
    crate::interop::guard(|| SLOT_COUNT)
}

/// Makes `source` relate to `target` in `slot`, in place of whatever it related to there, and
/// adds it to `target`'s list, as inserting Bevy's relationship component does.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_relation_set(slot: i32, source: u64, target: u64) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let target = crate::ecs::entity_from(target);
            if world.get_entity(target).is_err() {
                return status::NO_ENTITY;
            }
            relate(world, slot, crate::ecs::entity_from(source), target)
        })
    })
}

/// Takes `source`'s relation in `slot` away, and `source` off its target's list.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_relation_remove(slot: i32, source: u64) -> i32 {
    crate::interop::guard(|| with_world(|world| unrelate(world, slot, crate::ecs::entity_from(source))))
}

/// Writes what `source` relates to in `slot` to `out`, and answers [`status::NOT_PRESENT`] where
/// it relates to nothing there.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_relation_target(slot: i32, source: u64, out: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }
        with_world(|world| match target_of(world, slot, crate::ecs::entity_from(source)) {
            Some(target) => {
                unsafe { out.write(target.to_bits()) };
                status::OK
            }
            None => status::NOT_PRESENT,
        })
    })
}

/// Writes the entities relating to `target` in `slot` to `out`, as many as `capacity` allows, in
/// the order they came to relate to it, and answers how many there are.
///
/// # Safety
/// `out` must be writable for `capacity` entities, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_relation_sources(slot: i32, target: u64, out: *mut u64, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() && capacity > 0 {
            return status::NULL_ARG;
        }
        with_world(|world| {
            let Some(sources) = sources_of(world, slot, crate::ecs::entity_from(target)) else {
                return status::NO_ENTITY;
            };
            for (place, source) in sources.iter().take(capacity.max(0) as usize).enumerate() {
                unsafe { out.add(place).write(source.to_bits()) };
            }
            sources.len() as i32
        })
    })
}

#[cfg(test)]
mod tests {
    use bevy::ecs::world::World;

    use crate::interop::status;
    use crate::state::loan_world;

    fn sources(world: &mut World, slot: i32, target: u64) -> Vec<u64> {
        let mut out = [0u64; 8];
        let count = loan_world(world, || unsafe { super::bcs_relation_sources(slot, target, out.as_mut_ptr(), 8) });
        out[..count as usize].to_vec()
    }

    /// Bevy's relationships in small, a relation made, moved to another target, and taken away,
    /// each target's list kept by Bevy as it goes, and the slots apart.
    #[test]
    fn a_target_lists_what_relates_to_it_as_relations_change() {
        let mut world = World::new();
        let [alice, bob, charlie] = [(); 3].map(|_| world.spawn_empty().id().to_bits());

        let set = |world: &mut World, slot, source, target| loan_world(world, || super::bcs_relation_set(slot, source, target));
        assert_eq!(set(&mut world, 0, bob, alice), status::OK);
        assert_eq!(set(&mut world, 0, charlie, alice), status::OK);
        assert_eq!(sources(&mut world, 0, alice), [bob, charlie]);
        assert_eq!(sources(&mut world, 1, alice), Vec::<u64>::new());

        assert_eq!(set(&mut world, 0, charlie, bob), status::OK);
        assert_eq!(sources(&mut world, 0, alice), [bob]);
        assert_eq!(sources(&mut world, 0, bob), [charlie]);

        let mut target = 0;
        assert_eq!(loan_world(&mut world, || unsafe { super::bcs_relation_target(0, charlie, &mut target) }), status::OK);
        assert_eq!(target, bob);

        assert_eq!(loan_world(&mut world, || super::bcs_relation_remove(0, charlie)), status::OK);
        assert_eq!(sources(&mut world, 0, bob), Vec::<u64>::new());
        assert_eq!(loan_world(&mut world, || unsafe { super::bcs_relation_target(0, charlie, &mut target) }), status::NOT_PRESENT);
        assert_eq!(set(&mut world, 8, bob, alice), status::NULL_ARG);
    }
}

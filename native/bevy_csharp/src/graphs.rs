//! Animation graphs built in code, and a player playing several of a graph's nodes at their weights.
//!
//! A graph is a tree of nodes, clips at its leaves and blends above them, each weighted, which a
//! player reads to mix the clips it plays. Bevy's examples build one node by node, play several of
//! its clips at once and move their weights, and mask parts of a body out of a clip by putting the
//! targets of each part into a group a clip's node can leave out. The managed side names a node by
//! its index, as Bevy's `AnimationNodeIndex`, and holds the graph by its key in the asset table.
//!
//! Needs the `render` feature, where the animation plugin is. The entry points exist in every
//! profile and report [`status::UNSUPPORTED`] without it.

use crate::interop::status;

#[cfg(feature = "render")]
mod built {
    use super::*;

    use bevy::animation::graph::{AnimationGraph, AnimationNodeIndex};
    use bevy::animation::AnimationPlayer;
    use bevy::asset::{Assets, Handle};
    use bevy::ecs::world::World;

    /// The graph behind a key.
    pub fn graph(world: &World, key: i32) -> Option<Handle<AnimationGraph>> {
        crate::assets::clone_handle(world, key).and_then(|handle| handle.try_typed::<AnimationGraph>().ok())
    }

    /// Runs `f` on the graph behind a key, or answers why it cannot.
    pub fn with_graph(key: i32, f: impl FnOnce(&mut AnimationGraph) -> i32) -> i32 {
        crate::state::with_world(|world| {
            let Some(handle) = graph(world, key) else {
                return status::NOT_PRESENT;
            };
            let Some(mut graphs) = world.get_resource_mut::<Assets<AnimationGraph>>() else {
                return status::UNSUPPORTED;
            };
            match graphs.get_mut(&handle) {
                Some(graph) => f(graph.into_inner()),
                None => status::NOT_PRESENT,
            }
        })
    }

    /// Runs `f` on an entity's player, or answers why it cannot.
    pub fn with_player(entity: u64, f: impl FnOnce(&mut AnimationPlayer) -> i32) -> i32 {
        crate::state::with_world(|world| {
            let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                return status::NO_ENTITY;
            };
            match entity_mut.get_mut::<AnimationPlayer>() {
                Some(mut player) => f(&mut player),
                None => status::NOT_PRESENT,
            }
        })
    }

    pub fn node(index: u32) -> AnimationNodeIndex {
        AnimationNodeIndex::new(index as usize)
    }
}

/// Makes an empty graph, its root a blend, and writes its key in the asset table and the root's
/// node.
///
/// # Safety
/// `graph` and `root` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_graph_create(graph: *mut i32, root: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if graph.is_null() || root.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::animation::graph::AnimationGraph;

            crate::state::with_world(|world| {
                let made = AnimationGraph::new();
                let top = made.root.index() as u32;
                let Some(mut graphs) = world.get_resource_mut::<bevy::asset::Assets<AnimationGraph>>() else {
                    return status::UNSUPPORTED;
                };
                let added = graphs.add(made);
                let key = crate::assets::insert_handle(world, added.untyped());
                if key < 0 {
                    return key;
                }
                unsafe {
                    graph.write(key);
                    root.write(top);
                }
                status::OK
            })
        }
    })
}

/// Adds a blend under `parent` at a weight, and writes its node. An additive one, where `additive`
/// is one, adds what is under it to what its siblings make rather than mixing it in.
///
/// # Safety
/// `node` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_graph_add_blend(graph: i32, weight: f32, parent: u32, additive: i32, node: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if node.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (graph, weight, parent, additive);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            built::with_graph(graph, |graph| {
                let added = if additive != 0 {
                    graph.add_additive_blend(weight, built::node(parent))
                } else {
                    graph.add_blend(weight, built::node(parent))
                };
                unsafe { node.write(added.index() as u32) };
                status::OK
            })
        }
    })
}

/// Adds a clip under `parent` at a weight, leaving out the mask groups `mask` has a bit for, and
/// writes its node.
///
/// # Safety
/// `node` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_graph_add_clip(graph: i32, clip: i32, mask: u64, weight: f32, parent: u32, node: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if node.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (graph, clip, mask, weight, parent);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(clip) = crate::state::with_world_opt(|world| {
                crate::assets::clone_handle(world, clip).and_then(|handle| handle.try_typed::<bevy::animation::AnimationClip>().ok())
            })
            .flatten() else {
                return status::NOT_PRESENT;
            };

            built::with_graph(graph, |graph| {
                let added = graph.add_clip_with_mask(clip, mask, weight, built::node(parent));
                unsafe { node.write(added.index() as u32) };
                status::OK
            })
        }
    })
}

/// Puts the target whose identifier's halves are given into a mask group, for a clip's node to
/// leave out with the group's bit.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_graph_add_to_mask_group(graph: i32, high: u64, low: u64, group: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (graph, high, low, group);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let target = bevy::animation::AnimationTargetId(bevy::asset::uuid::Uuid::from_u64_pair(high, low));
            built::with_graph(graph, |graph| {
                graph.add_target_to_mask_group(target, group);
                status::OK
            })
        }
    })
}

/// Sets which mask groups a node leaves out, as it plays.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_graph_set_mask(graph: i32, node: u32, mask: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (graph, node, mask);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            built::with_graph(graph, |graph| match graph.get_mut(built::node(node)) {
                Some(found) => {
                    found.mask = mask;
                    status::OK
                }
                None => status::NOT_PRESENT,
            })
        }
    })
}

/// Gives `entity` a graph to play from, and a player where it has none.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_set_graph(entity: u64, graph: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, graph);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::animation::graph::AnimationGraphHandle;
            use bevy::animation::AnimationPlayer;

            crate::state::with_world(|world| {
                let Some(handle) = built::graph(world, graph) else {
                    return status::NOT_PRESENT;
                };
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                if !entity_mut.contains::<AnimationPlayer>() {
                    entity_mut.insert(AnimationPlayer::default());
                }
                entity_mut.insert(AnimationGraphHandle(handle));
                status::OK
            })
        }
    })
}

/// Starts a node of the player's graph playing beside whatever it plays already, over and over
/// where `repeat` is one. A node already playing goes on as it was.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_play_node(entity: u64, node: u32, repeat: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, node, repeat);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            built::with_player(entity, |player| {
                let playing = player.play(built::node(node));
                if repeat != 0 {
                    playing.repeat();
                }
                status::OK
            })
        }
    })
}

/// Sets the weight a playing node is mixed in at, which [`status::NOT_PRESENT`] answers for one not
/// playing.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_node_weight(entity: u64, node: u32, weight: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, node, weight);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            built::with_player(entity, |player| match player.animation_mut(built::node(node)) {
                Some(playing) => {
                    playing.set_weight(weight);
                    status::OK
                }
                None => status::NOT_PRESENT,
            })
        }
    })
}

/// Writes the halves of the identifier an entity is aimed at by, Bevy's `AnimationTargetId`,
/// answering [`status::NOT_PRESENT`] for one that carries none.
///
/// # Safety
/// `high` and `low` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_target_of(entity: u64, high: *mut u64, low: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if high.is_null() || low.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let Ok(found) = world.get_entity(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                let Some(target) = found.get::<bevy::animation::AnimationTargetId>() else {
                    return status::NOT_PRESENT;
                };
                let (h, l) = target.0.as_u64_pair();
                unsafe {
                    high.write(h);
                    low.write(l);
                }
                status::OK
            })
        }
    })
}

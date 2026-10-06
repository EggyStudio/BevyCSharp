//! Moving the input focus between interface nodes by direction, Bevy's directional navigation, and
//! the map of edges a game draws between nodes where the nearest one is not the one meant.
//!
//! A node carrying Bevy's `AutoDirectionalNavigation` is reached from its neighbors by where it is
//! on the screen, the nearest in each of eight directions, and an edge in Bevy's
//! `DirectionalNavigationMap` goes first, so a row can wrap to the next or a direction be blocked.
//! The focus moves as Bevy's `AutoDirectionalNavigator` moves it, which a game calls on a key or a
//! pad's button of its choosing, as Bevy's examples call it on the arrows and the pad.
//!
//! A direction is a number, Bevy's `CompassOctant` counted clockwise from north as the window's
//! drag edges are. Needs the `render` feature, where the interface is. The entry points exist in
//! every profile and report [`status::UNSUPPORTED`] without it.

use crate::interop::status;

#[cfg(feature = "render")]
mod built {
    use bevy::ecs::world::World;
    use bevy::input_focus::directional_navigation::DirectionalNavigationMap;
    use bevy::math::CompassOctant;

    /// The direction a number names.
    pub fn octant(direction: i32) -> Option<CompassOctant> {
        Some(match direction {
            0 => CompassOctant::North,
            1 => CompassOctant::NorthEast,
            2 => CompassOctant::East,
            3 => CompassOctant::SouthEast,
            4 => CompassOctant::South,
            5 => CompassOctant::SouthWest,
            6 => CompassOctant::West,
            7 => CompassOctant::NorthWest,
            _ => return None,
        })
    }

    /// The map of edges, made where the app has none yet, as an app built before the plugin was.
    pub fn map(world: &mut World) -> bevy::ecs::world::Mut<'_, DirectionalNavigationMap> {
        world.get_resource_or_insert_with(DirectionalNavigationMap::default)
    }
}

/// Moves the input focus to the node beside the one holding it in a direction, by an edge of the
/// map where one leads that way and by where the nodes are on the screen otherwise, and writes the
/// node it moved to. Answers 1 where it moved and 0 where nothing lies that way, an edge blocks it
/// or nothing holds the focus.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_nav_move(direction: i32, out: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = direction;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::system::SystemState;
            use bevy::input_focus::directional_navigation::AutoNavigationConfig;
            use bevy::input_focus::InputFocus;
            use bevy::ui::auto_directional_navigation::AutoDirectionalNavigator;

            let Some(direction) = built::octant(direction) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                world.get_resource_or_insert_with(InputFocus::default);
                world.get_resource_or_insert_with(AutoNavigationConfig::default);
                built::map(world);

                let mut state = SystemState::<AutoDirectionalNavigator>::new(world);
                let Ok(mut navigator) = state.get_mut(world) else {
                    return 0;
                };
                match navigator.navigate(direction) {
                    Ok(next) => {
                        unsafe { out.write(next.to_bits()) };
                        1
                    }
                    Err(_) => 0,
                }
            })
        }
    })
}

/// Draws an edge of the map from one node to another in a direction, as `kind` says, 0 from the
/// first alone, 1 both ways, the way back the opposite direction, 2 a direction the first does not
/// leave by, and 3 that both ways, the second not leaving by the opposite direction.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_nav_edge(from: u64, to: u64, direction: i32, kind: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (from, to, direction, kind);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(direction) = built::octant(direction) else {
                return status::NULL_ARG;
            };
            let (from, to) = (crate::ecs::entity_from(from), crate::ecs::entity_from(to));

            crate::state::with_world(|world| {
                let mut map = built::map(world);
                match kind {
                    0 => map.add_edge(from, to, direction),
                    1 => map.add_symmetrical_edge(from, to, direction),
                    2 => map.block_edge(from, direction),
                    3 => map.block_symmetrical_edge(from, to, direction),
                    _ => return status::NULL_ARG,
                }
                status::OK
            })
        }
    })
}

/// Draws edges between nodes in their order in a direction, each to the next and back the opposite
/// way, and from the last to the first and back where `looping` is one.
///
/// # Safety
/// `entities` must hold `count` entities.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_nav_edges(entities: *const u64, count: i32, direction: i32, looping: i32) -> i32 {
    crate::interop::guard(|| {
        if entities.is_null() || count < 0 {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (direction, looping);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(direction) = built::octant(direction) else {
                return status::NULL_ARG;
            };
            let entities: Vec<_> = unsafe { core::slice::from_raw_parts(entities, count as usize) }
                .iter()
                .map(|&entity| crate::ecs::entity_from(entity))
                .collect();

            crate::state::with_world(|world| {
                let mut map = built::map(world);
                if looping != 0 {
                    map.add_looping_edges(&entities, direction);
                } else {
                    map.add_edges(&entities, direction);
                }
                status::OK
            })
        }
    })
}

/// Takes a node's edges out of the map, those leading from it and to it, or every edge where
/// `entity` is zero.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_nav_forget(entity: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let mut map = built::map(world);
                if entity == 0 {
                    map.clear();
                } else {
                    map.remove(crate::ecs::entity_from(entity));
                }
                status::OK
            })
        }
    })
}

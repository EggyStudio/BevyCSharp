//! A model's animation clips, played on the entity its scene was spawned under.
//!
//! A glTF file's clips are loaded by Bevy as `AnimationClip`s, and the scene it spawns puts an
//! `AnimationPlayer` on the node at the top of what they move. What Bevy leaves to the game is
//! the rest: a graph of the clips for the player to read, and which of them plays. This keeps one
//! of those per scene, made the first time something asks, so the managed side names a clip by the
//! name the artist gave it and plays it on the entity it spawned, with no graph or node index on
//! that side at all.
//!
//! Everything goes through `AnimationTransitions`, which fades the clip that was playing out while
//! the new one fades in over whatever time the caller gives, zero being a cut. A clip that plays
//! once and reaches its end is queued for the managed side, which posts it as a message.
//!
//! Needs the `render` feature, where the animation plugin is. The entry points exist in every
//! profile and report [`status::UNSUPPORTED`] without it.

use crate::interop::status;

/// What a scene's clips became, the players that play them and the node each clip is in the
/// graph, in the order the file lists them.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
pub struct BcsAnimator {
    /// Every entity below the scene's root carrying an `AnimationPlayer`, one for each top node of
    /// what the file animates.
    players: Vec<bevy::ecs::entity::Entity>,
    /// Each clip's name, in the file's order, so a clip's number is the same every run. A clip
    /// the file gives no name is called as Bevy labels it, `Animation` and its number.
    names: Vec<String>,
    /// The graph node of each clip, in the order of `names`.
    nodes: Vec<bevy::animation::graph::AnimationNodeIndex>,
    /// The clip playing as the main one, by its number, or `None`.
    playing: Option<usize>,
    /// Whether that clip's end has been reported, so it is reported once.
    reported: bool,
}

/// The file a scene came from, held while its clips are gathered.
///
/// Bevy frees a glTF file's own asset once nothing holds it, keeping only the meshes and scenes
/// asked for by label, so a scene spawned on its own leaves the file gone by the time its clips
/// are wanted. A load asked for and let go of at once would be dropped before it finished, so the
/// root holds the handle until the clips have been gathered.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
struct BcsAnimationSource(bevy::asset::Handle<bevy::gltf::Gltf>);

/// Clips that played once and reached their end, by the scene root they play on and the clip's
/// number, waiting for the managed side to take them.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct FinishedClips(Vec<(u64, i32)>);

/// Adds the system that notices a clip reaching its end.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::prelude::*;

    app.init_resource::<FinishedClips>();
    app.add_systems(PostUpdate, notice_finished);
}

/// Queues each main clip that has reached its end, once.
#[cfg(feature = "render")]
fn notice_finished(
    mut animators: bevy::prelude::Query<(bevy::prelude::Entity, &mut BcsAnimator)>,
    players: bevy::prelude::Query<&bevy::animation::AnimationPlayer>,
    mut finished: bevy::prelude::ResMut<FinishedClips>,
) {
    for (root, mut animator) in &mut animators {
        let Some(clip) = animator.playing else {
            continue;
        };

        if animator.reported {
            continue;
        }

        // Every player plays the clip together, so the first answers for them all.
        let done = animator
            .players
            .first()
            .and_then(|&player| players.get(player).ok())
            .and_then(|player| player.animation(animator.nodes[clip]))
            .is_some_and(|active| active.is_finished());

        if done {
            animator.reported = true;
            finished.0.push((root.to_bits(), clip as i32));
        }
    }
}

/// The scene root's animator, made the first time it is asked for, or the status that says why
/// there is none yet.
///
/// [`status::INVALID_STATE`] while the file or the scene below the root has not arrived, which is
/// the ordinary answer for the first frames after a spawn, and [`status::NOT_PRESENT`] for a scene
/// that has arrived with nothing to animate.
#[cfg(feature = "render")]
fn animator(world: &mut bevy::ecs::world::World, root: bevy::ecs::entity::Entity) -> Result<(), i32> {
    use bevy::animation::graph::{AnimationGraph, AnimationGraphHandle};
    use bevy::animation::{AnimationPlayer, transition::AnimationTransitions};
    use bevy::asset::{AssetServer, Assets};
    use bevy::ecs::hierarchy::Children;
    use bevy::gltf::Gltf;
    use bevy::world_serialization::{WorldAssetRoot, WorldInstance};

    if world.get::<BcsAnimator>(root).is_some() {
        return Ok(());
    }

    let Some(scene) = world.get::<WorldAssetRoot>(root) else {
        return Err(status::NOT_PRESENT);
    };

    // The file the scene came from, which holds the clips by name.
    let Some(path) = scene.0.path().map(|path| path.without_label().into_owned()) else {
        return Err(status::NOT_PRESENT);
    };

    let gltf = match world.get::<BcsAnimationSource>(root) {
        Some(source) => source.0.clone(),
        None => {
            let handle = world.resource::<AssetServer>().load::<Gltf>(path);
            world.entity_mut(root).insert(BcsAnimationSource(handle.clone()));
            handle
        }
    };

    let Some(file) = world.resource::<Assets<Gltf>>().get(&gltf) else {
        return Err(status::INVALID_STATE);
    };

    // Every clip, named or not, in the file's order, so one the artist left unnamed can still be
    // played by the name Bevy gives it.
    let clips: Vec<(String, bevy::asset::Handle<bevy::animation::AnimationClip>)> = file
        .animations
        .iter()
        .enumerate()
        .map(|(index, clip)| {
            let name = file
                .named_animations
                .iter()
                .find(|(_, named)| *named == clip)
                .map_or_else(|| format!("Animation{index}"), |(name, _)| name.to_string());
            (name, clip.clone())
        })
        .collect();

    // A player is on each node at the top of what the clips move, which glTF does not name, so
    // the hierarchy below the root is searched for all of them. A file animating two separate
    // things, such as a character and the prop it carries, has a player for each.
    let mut stack = vec![root];
    let mut players = Vec::new();

    while let Some(entity) = stack.pop() {
        if world.get::<AnimationPlayer>(entity).is_some() {
            players.push(entity);
        }

        if let Some(children) = world.get::<Children>(entity) {
            stack.extend(children.iter());
        }
    }

    if players.is_empty() {
        // Spawned and still without a player means the file animates nothing.
        return Err(if world.get::<WorldInstance>(root).is_some() && !clips.is_empty() {
            status::INVALID_STATE
        } else if world.get::<WorldInstance>(root).is_some() {
            status::NOT_PRESENT
        } else {
            status::INVALID_STATE
        });
    }

    // One graph of every clip for every player, so a clip moves whatever it targets under any of
    // them and leaves the rest alone.
    let (graph, nodes) = AnimationGraph::from_clips(clips.iter().map(|(_, clip)| clip.clone()));
    let graph = world.resource_mut::<Assets<AnimationGraph>>().add(graph);

    for &player in &players {
        world
            .entity_mut(player)
            .insert((AnimationGraphHandle(graph.clone()), AnimationTransitions::new()));
    }

    world.entity_mut(root).remove::<BcsAnimationSource>().insert(BcsAnimator {
        players,
        names: clips.into_iter().map(|(name, _)| name).collect(),
        nodes,
        playing: None,
        reported: false,
    });

    Ok(())
}

/// Runs `f` over a scene root's animator and each player it plays on, after making them, and
/// answers the first status that is not [`status::OK`], or that.
#[cfg(feature = "render")]
fn with_animator(
    root: u64,
    mut f: impl FnMut(
        &mut BcsAnimator,
        &mut bevy::animation::AnimationPlayer,
        &mut bevy::animation::transition::AnimationTransitions,
    ) -> i32,
) -> i32 {
    crate::state::with_world(|world| {
        let root = crate::ecs::entity_from(root);
        if world.get_entity(root).is_err() {
            return status::NO_ENTITY;
        }

        if let Err(why) = animator(world, root) {
            return why;
        }

        let players = world.get::<BcsAnimator>(root).map(|animator| animator.players.clone());
        let Some(players) = players else {
            return status::NOT_PRESENT;
        };

        // The animator is lifted off the root while the player is borrowed, since a query over
        // the player's components holds the world for as long as it is used.
        let Some(mut animator) = world.entity_mut(root).take::<BcsAnimator>() else {
            return status::NOT_PRESENT;
        };

        let mut playing = world.query::<(
            &mut bevy::animation::AnimationPlayer,
            &mut bevy::animation::transition::AnimationTransitions,
        )>();

        let mut answer = status::OK;
        for player in players {
            let said = match playing.get_mut(world, player) {
                Ok((mut player, mut transitions)) => f(&mut animator, &mut player, &mut transitions),
                // The scene was taken apart under it, which leaves nothing to play.
                Err(_) => status::NOT_PRESENT,
            };

            if answer == status::OK {
                answer = said;
            }
        }

        world.entity_mut(root).insert(animator);
        answer
    })
}

/// Writes the names of a scene's clips into `out`, one a line in the order their numbers count,
/// and returns the length in bytes.
///
/// The usual text convention. [`status::INVALID_STATE`] while the file or the scene has not
/// arrived, which a caller asks again about next frame, and [`status::NOT_PRESENT`] for a scene
/// with nothing to animate or an entity no scene was spawned under.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_clips(root: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (root, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mut names = String::new();
            let answer = with_animator(root, |animator, _, _| {
                names = animator.names.join("\n");
                status::OK
            });

            if answer != status::OK {
                return answer;
            }

            // SAFETY: the caller promises `capacity` writable bytes at `out`.
            unsafe { crate::interop::write_text(&names, out, capacity) }
        }
    })
}

/// Plays a scene's clip by its number, fading whatever played before out over `blend` seconds.
///
/// `repeat` non-zero plays it over and over, zero once, ending on its last pose. `speed` scales
/// time, one being as it was made and a negative number playing it backwards. A clip played again
/// while it plays starts over.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_play(root: u64, clip: i32, repeat: i32, speed: f32, blend: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (root, clip, repeat, speed, blend);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_animator(root, |animator, player, transitions| {
                let Some(&node) = usize::try_from(clip).ok().and_then(|clip| animator.nodes.get(clip)) else {
                    return status::NULL_ARG;
                };

                let fade = std::time::Duration::from_secs_f32(blend.max(0.0));
                let active = transitions.play(player, node, fade);
                active.set_speed(speed);

                if repeat != 0 {
                    active.repeat();
                }

                animator.playing = Some(clip as usize);
                animator.reported = false;
                status::OK
            })
        }
    })
}

/// Stops every clip on a scene, leaving it in whatever pose it was in.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_stop(root: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = root;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_animator(root, |animator, player, _| {
                player.stop_all();
                animator.playing = None;
                status::OK
            })
        }
    })
}

/// Holds every clip on a scene where it is, or lets them go on, by `paused`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_pause(root: u64, paused: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (root, paused);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_animator(root, |_, player, _| {
                if paused != 0 {
                    player.pause_all();
                } else {
                    player.resume_all();
                }
                status::OK
            })
        }
    })
}

/// Moves the clip playing on a scene to `seconds` from its start, or changes its speed, or both,
/// where `seconds` is not negative and `speed` is not NaN.
///
/// [`status::INVALID_STATE`] when nothing is playing, since there is no clip to move.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_adjust(root: u64, seconds: f32, speed: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (root, seconds, speed);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_animator(root, |animator, player, _| {
                let Some(clip) = animator.playing else {
                    return status::INVALID_STATE;
                };

                let Some(active) = player.animation_mut(animator.nodes[clip]) else {
                    return status::INVALID_STATE;
                };

                if seconds >= 0.0 {
                    active.seek_to(seconds);
                    animator.reported = false;
                }

                if !speed.is_nan() {
                    active.set_speed(speed);
                }

                status::OK
            })
        }
    })
}

/// What a scene is playing, as the managed side reads it.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsAnimationState {
    /// The main clip's number, or `-1` when nothing plays.
    pub clip: i32,
    /// Seconds into it.
    pub seconds: f32,
    /// How fast it plays.
    pub speed: f32,
    /// Non-zero while it is held.
    pub paused: i32,
    /// Non-zero once a clip that plays once has reached its end.
    pub finished: i32,
    /// How many times it has come round to its start, for one that repeats.
    pub completions: u32,
}

/// Writes what a scene is playing into `out`.
///
/// # Safety
/// `out` must be writable for one [`BcsAnimationState`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_state(root: u64, out: *mut BcsAnimationState) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = root;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mut state = BcsAnimationState {
                clip: -1,
                ..Default::default()
            };

            let answer = with_animator(root, |animator, player, _| {
                // The first player answers, since every one plays the clip together.
                if state.clip < 0
                    && let Some(clip) = animator.playing
                    && let Some(active) = player.animation(animator.nodes[clip])
                {
                    state = BcsAnimationState {
                        clip: clip as i32,
                        seconds: active.seek_time(),
                        speed: active.speed(),
                        paused: i32::from(active.is_paused()),
                        finished: i32::from(active.is_finished()),
                        completions: active.completions(),
                    };
                }

                status::OK
            });

            // SAFETY: the caller promises `out` is writable.
            unsafe { out.write(state) };
            answer
        }
    })
}

/// Copies the clips that reached their end since the last call into `roots` and `clips`, and
/// returns how many. Those that did not fit are kept for the next call.
///
/// # Safety
/// `roots` and `clips` must each be writable for `capacity` items, or null when it is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_finished(roots: *mut u64, clips: *mut i32, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (roots, clips, capacity);
            0
        }

        #[cfg(feature = "render")]
        {
            if capacity <= 0 || roots.is_null() || clips.is_null() {
                return 0;
            }

            crate::state::with_world(|world| {
                let Some(mut finished) = world.get_resource_mut::<FinishedClips>() else {
                    return 0;
                };

                let count = finished.0.len().min(capacity as usize);

                for (index, (root, clip)) in finished.0.drain(..count).enumerate() {
                    // SAFETY: `index` is below `capacity`, which the caller promised room for.
                    unsafe {
                        roots.add(index).write(root);
                        clips.add(index).write(clip);
                    }
                }

                count as i32
            })
        }
    })
}

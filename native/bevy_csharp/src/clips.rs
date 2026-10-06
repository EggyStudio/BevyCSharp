//! Animation clips built in code, the graph a player reads them from, and the entities they move.
//!
//! A glTF file's clips arrive made ([`crate::animation`]). A game also makes its own, as Bevy's
//! examples do, a curve for each property it moves, sampled at times or eased from one value to
//! another, aimed at an entity by the names on the path down to it. A graph made from the clip is
//! what a player reads, and each entity a curve is aimed at carries that target and the player that
//! moves it. The managed side holds the clip and the graph by the keys of the asset table, as it
//! holds a mesh.
//!
//! Needs the `render` feature, where the animation plugin is. The entry points exist in every
//! profile and report [`status::UNSUPPORTED`] without it.

use crate::interop::status;

/// One curve to add to a clip, the managed side's `NativeAnimationCurve` field for field.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsAnimationCurve {
    /// What it moves, `0` a transform's translation, `1` its rotation, `2` its scale, `3` an
    /// interface node's scale, `4` its rotation in radians and `5` a text's color.
    pub property: i32,
    /// `0` for values sampled at times and interpolated between, `1` for the first value eased to
    /// the second over `duration`.
    pub kind: i32,
    /// How many values there are, and times for sampled ones.
    pub count: i32,
    /// The easing, in the order of the managed `EaseKind`, with what `Steps` and `Elastic` carry.
    pub ease: i32,
    pub ease_steps: i32,
    pub ease_jump: i32,
    pub ease_omega: f32,
    /// How long an eased curve takes, from zero.
    pub duration: f32,
    /// One where an eased curve goes back to its start after reaching its end.
    pub ping_pong: u32,
}

#[cfg(feature = "render")]
mod built {
    use super::*;

    use core::any::TypeId;

    use bevy::animation::animation_curves::{AnimatableCurve, AnimatableKeyframeCurve, AnimatableProperty, AnimatedField, EvaluatorId};
    use bevy::animation::{animated_field, AnimationClip, AnimationEntityMut, AnimationEvaluationError, AnimationTargetId};
    use bevy::color::{Color, Srgba};
    use bevy::math::curve::easing::{EaseFunction, EasingCurve, JumpAt};
    use bevy::math::curve::{interval, CurveExt};
    use bevy::math::{Quat, Rot2, Vec2, Vec3};
    use bevy::text::TextColor;
    use bevy::transform::components::Transform;
    use bevy::ui::UiTransform;

    /// A text's color, which Bevy's animated_ui example animates through a property of its own,
    /// as no field macro reaches the color inside the enum. A color the managed side set is held
    /// linear, so it is taken to sRGB in place first, which the curve's values are in.
    #[derive(Clone)]
    pub struct TextColorProperty;

    impl AnimatableProperty for TextColorProperty {
        type Property = Srgba;

        fn evaluator_id(&self) -> EvaluatorId<'_> {
            EvaluatorId::Type(TypeId::of::<Self>())
        }

        fn get_mut<'a>(&self, entity: &'a mut AnimationEntityMut) -> Result<&'a mut Self::Property, AnimationEvaluationError> {
            let text_color = entity
                .get_mut::<TextColor>()
                .ok_or(AnimationEvaluationError::ComponentNotPresent(TypeId::of::<TextColor>()))?
                .into_inner();
            if !matches!(text_color.0, Color::Srgba(_)) {
                text_color.0 = Color::Srgba(text_color.0.to_srgba());
            }
            match text_color.0 {
                Color::Srgba(ref mut color) => Ok(color),
                _ => Err(AnimationEvaluationError::PropertyNotPresent(TypeId::of::<Srgba>())),
            }
        }
    }

    /// Bevy's easing function for the managed side's numbers, in the order of its `EaseKind`.
    pub fn ease(curve: &BcsAnimationCurve) -> Option<EaseFunction> {
        use EaseFunction::*;
        Some(match curve.ease {
            0 => Linear,
            1 => QuadraticIn,
            2 => QuadraticOut,
            3 => QuadraticInOut,
            4 => CubicIn,
            5 => CubicOut,
            6 => CubicInOut,
            7 => QuarticIn,
            8 => QuarticOut,
            9 => QuarticInOut,
            10 => QuinticIn,
            11 => QuinticOut,
            12 => QuinticInOut,
            13 => SmoothStepIn,
            14 => SmoothStepOut,
            15 => SmoothStep,
            16 => SmootherStepIn,
            17 => SmootherStepOut,
            18 => SmootherStep,
            19 => SineIn,
            20 => SineOut,
            21 => SineInOut,
            22 => CircularIn,
            23 => CircularOut,
            24 => CircularInOut,
            25 => ExponentialIn,
            26 => ExponentialOut,
            27 => ExponentialInOut,
            28 => ElasticIn,
            29 => ElasticOut,
            30 => ElasticInOut,
            31 => BackIn,
            32 => BackOut,
            33 => BackInOut,
            34 => BounceIn,
            35 => BounceOut,
            36 => BounceInOut,
            37 => Steps(
                curve.ease_steps.max(1) as usize,
                match curve.ease_jump {
                    1 => JumpAt::Start,
                    2 => JumpAt::None,
                    3 => JumpAt::Both,
                    _ => JumpAt::End,
                },
            ),
            38 => Elastic(curve.ease_omega),
            _ => return None,
        })
    }

    /// Adds the curve a description asks for, its values already read in the property's type.
    fn add_typed<P, T>(clip: &mut AnimationClip, target: AnimationTargetId, property: P, curve: &BcsAnimationCurve, times: &[f32], values: Vec<T>) -> i32
    where
        P: AnimatableProperty<Property = T> + Clone,
        T: bevy::animation::animatable::Animatable + bevy::math::curve::Ease + Clone + core::fmt::Debug + bevy::reflect::Reflectable + bevy::reflect::FromReflect,
    {
        match curve.kind {
            0 => {
                let Ok(sampled) = AnimatableKeyframeCurve::new(times.iter().copied().zip(values)) else {
                    return status::NULL_ARG;
                };
                clip.add_curve_to_target(target, AnimatableCurve::new(property, sampled));
            }
            1 => {
                let (Some(start), Some(end), Some(ease)) = (values.first().cloned(), values.get(1).cloned(), ease(curve)) else {
                    return status::NULL_ARG;
                };
                let Ok(domain) = interval(0.0, curve.duration.max(f32::EPSILON)) else {
                    return status::NULL_ARG;
                };
                let Ok(eased) = EasingCurve::new(start, end, ease).reparametrize_linear(domain) else {
                    return status::NULL_ARG;
                };
                if curve.ping_pong != 0 {
                    let Ok(both_ways) = eased.ping_pong() else {
                        return status::NULL_ARG;
                    };
                    clip.add_curve_to_target(target, AnimatableCurve::new(property, both_ways));
                } else {
                    clip.add_curve_to_target(target, AnimatableCurve::new(property, eased));
                }
            }
            _ => return status::NULL_ARG,
        }
        status::OK
    }

    /// Adds one described curve to a clip, reading its values as the property takes them.
    pub fn add(clip: &mut AnimationClip, target: AnimationTargetId, curve: &BcsAnimationCurve, times: &[f32], floats: &[f32]) -> i32 {
        let n = curve.count as usize;
        let read = |width: usize| -> Option<&[f32]> { (floats.len() >= n * width).then(|| &floats[..n * width]) };

        match curve.property {
            0 | 2 => {
                let Some(raw) = read(3) else { return status::NULL_ARG };
                let values: Vec<Vec3> = raw.chunks_exact(3).map(Vec3::from_slice).collect();
                if curve.property == 0 {
                    add_typed(clip, target, animated_field!(Transform::translation), curve, times, values)
                } else {
                    add_typed(clip, target, animated_field!(Transform::scale), curve, times, values)
                }
            }
            1 => {
                let Some(raw) = read(4) else { return status::NULL_ARG };
                let values: Vec<Quat> = raw.chunks_exact(4).map(|q| Quat::from_xyzw(q[0], q[1], q[2], q[3]).normalize()).collect();
                add_typed(clip, target, animated_field!(Transform::rotation), curve, times, values)
            }
            3 => {
                let Some(raw) = read(2) else { return status::NULL_ARG };
                let values: Vec<Vec2> = raw.chunks_exact(2).map(Vec2::from_slice).collect();
                add_typed(clip, target, animated_field!(UiTransform::scale), curve, times, values)
            }
            4 => {
                let Some(raw) = read(1) else { return status::NULL_ARG };
                let values: Vec<Rot2> = raw.iter().copied().map(Rot2::radians).collect();
                add_typed(clip, target, animated_field!(UiTransform::rotation), curve, times, values)
            }
            5 => {
                let Some(raw) = read(4) else { return status::NULL_ARG };
                let values: Vec<Srgba> = raw.chunks_exact(4).map(|c| Srgba::new(c[0], c[1], c[2], c[3])).collect();
                add_typed(clip, target, TextColorProperty, curve, times, values)
            }
            _ => status::NULL_ARG,
        }
    }
}

/// Makes an empty animation clip and returns its key in the asset table.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_clip_create() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let Some(mut clips) = world.get_resource_mut::<bevy::asset::Assets<bevy::animation::AnimationClip>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = clips.add(bevy::animation::AnimationClip::default());
                crate::assets::insert_handle(world, handle.untyped())
            })
        }
    })
}

/// Writes the id a clip aims a curve at for the entity at the end of a path of names, given one a
/// line, Bevy's `AnimationTargetId::from_names`, as its identifier's two halves.
///
/// # Safety
/// `names` must be a NUL-terminated UTF-8 string, and `high` and `low` writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_target(names: *const core::ffi::c_char, high: *mut u64, low: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if high.is_null() || low.is_null() {
            return status::NULL_ARG;
        }
        let Some(names) = (unsafe { crate::interop::cstr_to_string(names) }) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = names;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let path: Vec<bevy::ecs::name::Name> = names.split('\n').map(|name| bevy::ecs::name::Name::new(name.to_string())).collect();
            let (h, l) = bevy::animation::AnimationTargetId::from_names(path.iter()).0.as_u64_pair();
            unsafe {
                high.write(h);
                low.write(l);
            }
            status::OK
        }
    })
}

/// Adds a curve to a clip, aimed at the target whose identifier's halves are given.
///
/// # Safety
/// `curve` must point to a readable [`BcsAnimationCurve`], `times` to its count of floats where its
/// values are sampled, and `values` to its count of values, each as many floats as the property
/// takes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_clip_add_curve(
    clip: i32,
    high: u64,
    low: u64,
    curve: *const BcsAnimationCurve,
    times: *const f32,
    values: *const f32,
    value_count: i32,
) -> i32 {
    crate::interop::guard(|| {
        if curve.is_null() || values.is_null() || value_count < 0 {
            return status::NULL_ARG;
        }
        let curve = unsafe { *curve };
        if curve.count < 1 || (curve.kind == 0 && times.is_null()) {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (clip, high, low, times);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let times = if curve.kind == 0 { unsafe { core::slice::from_raw_parts(times, curve.count as usize) } } else { &[][..] };
            let floats = unsafe { core::slice::from_raw_parts(values, value_count as usize) };
            let target = bevy::animation::AnimationTargetId(bevy::asset::uuid::Uuid::from_u64_pair(high, low));

            crate::state::with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, clip).and_then(|handle| handle.try_typed::<bevy::animation::AnimationClip>().ok()) else {
                    return status::NOT_PRESENT;
                };
                let Some(mut clips) = world.get_resource_mut::<bevy::asset::Assets<bevy::animation::AnimationClip>>() else {
                    return status::UNSUPPORTED;
                };
                let Some(clip) = clips.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                built::add(clip.into_inner(), target, &curve, times, floats)
            })
        }
    })
}

/// Makes a graph holding one clip, as Bevy's `AnimationGraph::from_clip`, and writes its key in
/// the asset table and the clip's node in it.
///
/// # Safety
/// `graph` and `node` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_graph_from_clip(clip: i32, graph: *mut i32, node: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if graph.is_null() || node.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = clip;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::animation::graph::AnimationGraph;

            crate::state::with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, clip).and_then(|handle| handle.try_typed::<bevy::animation::AnimationClip>().ok()) else {
                    return status::NOT_PRESENT;
                };
                let (built, index) = AnimationGraph::from_clip(handle);
                let Some(mut graphs) = world.get_resource_mut::<bevy::asset::Assets<AnimationGraph>>() else {
                    return status::UNSUPPORTED;
                };
                let added = graphs.add(built);
                let key = crate::assets::insert_handle(world, added.untyped());
                if key < 0 {
                    return key;
                }
                unsafe {
                    graph.write(key);
                    node.write(index.index() as u32);
                }
                status::OK
            })
        }
    })
}

/// Makes `entity` a player of the graph, playing its node, over and over where `repeat` is one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_play_graph(entity: u64, graph: i32, node: u32, repeat: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, graph, node, repeat);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::animation::graph::{AnimationGraph, AnimationGraphHandle, AnimationNodeIndex};
            use bevy::animation::AnimationPlayer;

            crate::state::with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, graph).and_then(|handle| handle.try_typed::<AnimationGraph>().ok()) else {
                    return status::NOT_PRESENT;
                };
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                let mut player = entity_mut.take::<AnimationPlayer>().unwrap_or_default();
                let playing = player.play(AnimationNodeIndex::new(node as usize));
                if repeat != 0 {
                    playing.repeat();
                }
                entity_mut.insert((AnimationGraphHandle(handle), player));
                status::OK
            })
        }
    })
}

/// Makes `entity` the target a clip's curves are aimed at by the identifier whose halves are
/// given, moved by the player on `player`, Bevy's `AnimationTargetId` and `AnimatedBy`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_animate(entity: u64, high: u64, low: u64, player: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, high, low, player);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::animation::{AnimatedBy, AnimationTargetId};

            crate::state::with_world(|world| {
                let player = crate::ecs::entity_from(player);
                if world.get_entity(player).is_err() {
                    return status::NO_ENTITY;
                }
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                entity_mut.insert((AnimationTargetId(bevy::asset::uuid::Uuid::from_u64_pair(high, low)), AnimatedBy(player)));
                status::OK
            })
        }
    })
}

/// What C# is called with when a clip reaches an event placed on it, the number C# gave the event,
/// the entity it happens at, and the handle C# finds its handlers through.
pub type ClipEventCallback = unsafe extern "C" fn(event: u32, entity: u64, user: *mut core::ffi::c_void);

/// The callback an app's managed side installed to hear its clips' events, which a clip reaching
/// one calls with the world on loan.
#[cfg(feature = "render")]
#[derive(bevy::prelude::Resource, Clone, Copy)]
struct ClipEvents {
    callback: ClipEventCallback,
    user: *mut core::ffi::c_void,
}

// SAFETY: the pointers are only dereferenced by calling back into the .NET runtime, which is
// thread-safe itself, and Bevy needs the bound to keep them in a resource.
#[cfg(feature = "render")]
unsafe impl Send for ClipEvents {}
#[cfg(feature = "render")]
unsafe impl Sync for ClipEvents {}

/// Has every event placed on a clip in this app call `callback`, once the frame's commands are
/// applied, with the event's number and the entity it happens at.
///
/// On the app before it runs, or on the world from inside a system where `app` is null.
///
/// # Safety
/// `app` must be a live app or null, and `callback` callable for the app's life.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_observe_clip_events(
    app: *mut crate::state::BcsApp,
    callback: Option<ClipEventCallback>,
    user: *mut core::ffi::c_void,
) -> i32 {
    crate::interop::guard(|| {
        let Some(callback) = callback else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (app, callback, user);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let install = |world: &mut bevy::ecs::world::World| -> i32 {
                world.insert_resource(ClipEvents { callback, user });
                status::OK
            };
            if app.is_null() {
                return crate::state::with_world(install);
            }
            match unsafe { crate::state::app_mut(app) } {
                Some(app) => install(app.app.world_mut()),
                None => status::NULL_ARG,
            }
        }
    })
}

/// Sets how long a clip lasts, which one holding only events needs, since its curves otherwise give
/// it its length.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_clip_set_duration(clip: i32, seconds: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (clip, seconds);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_clip(clip, |clip| {
                clip.set_duration(seconds.max(0.0));
                status::OK
            })
        }
    })
}

/// Places an event on a clip at a time, which the clip reaching it reports to C# by `event`, the
/// number C# gave it. At the player where `targeted` is zero, and at the entity the target whose
/// identifier's halves are given names where it is one.
///
/// A loaded clip that has not arrived yet answers [`status::NOT_PRESENT`], for C# to place it again
/// next frame.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_animation_clip_add_event(clip: i32, time: f32, event: u32, targeted: i32, high: u64, low: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (clip, time, event, targeted, high, low);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::world::World;
            use bevy::prelude::{Commands, Entity};

            // Queued, as Bevy's own events are triggered from the commands the player's system
            // holds, so C# is called with the whole world once they are applied.
            let fire = move |commands: &mut Commands, at: Entity, _time: f32, _weight: f32| {
                commands.queue(move |world: &mut World| {
                    let Some(events) = world.get_resource::<ClipEvents>().copied() else {
                        return;
                    };
                    crate::state::loan_world(world, || unsafe { (events.callback)(event, at.to_bits(), events.user) });
                });
            };

            with_clip(clip, |clip| {
                if targeted == 0 {
                    clip.add_event_fn(time, fire);
                } else {
                    let target = bevy::animation::AnimationTargetId(bevy::asset::uuid::Uuid::from_u64_pair(high, low));
                    clip.add_event_fn_to_target(target, time, fire);
                }
                status::OK
            })
        }
    })
}

/// Runs `f` on the clip behind a key, or answers why it cannot.
#[cfg(feature = "render")]
fn with_clip(clip: i32, f: impl FnOnce(&mut bevy::animation::AnimationClip) -> i32) -> i32 {
    crate::state::with_world(|world| {
        let Some(handle) = crate::assets::clone_handle(world, clip).and_then(|handle| handle.try_typed::<bevy::animation::AnimationClip>().ok()) else {
            return status::NOT_PRESENT;
        };
        let Some(mut clips) = world.get_resource_mut::<bevy::asset::Assets<bevy::animation::AnimationClip>>() else {
            return status::UNSUPPORTED;
        };
        match clips.get_mut(&handle) {
            Some(clip) => f(clip.into_inner()),
            None => status::NOT_PRESENT,
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_curve_sits_where_the_managed_side_reads_it() {
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, property), 0);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, kind), 4);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, count), 8);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, ease), 12);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, ease_steps), 16);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, ease_jump), 20);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, ease_omega), 24);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, duration), 28);
        assert_eq!(core::mem::offset_of!(BcsAnimationCurve, ping_pong), 32);
        assert_eq!(core::mem::size_of::<BcsAnimationCurve>(), 36);
    }
}

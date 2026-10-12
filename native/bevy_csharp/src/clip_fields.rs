//! A curve on a field of any component Bevy reflects, named by the component's type path and a
//! reflect path, for a field no `animated_field!` in [`crate::clips`] reaches because its type is
//! known only by name when the curve is made.
//!
//! Needs the `render` feature, where the animation plugin is. The entry point exists in every
//! profile and reports [`status::UNSUPPORTED`] without it.

use crate::clips::BcsAnimationCurve;
use crate::interop::status;

#[cfg(feature = "render")]
mod built {
    use super::*;

    use core::any::TypeId;
    use core::hash::BuildHasher;
    use core::marker::PhantomData;
    use std::sync::Arc;

    use bevy::animation::animatable::Animatable;
    use bevy::animation::animation_curves::{AnimatableProperty, EvaluatorId};
    use bevy::animation::{AnimationClip, AnimationEntityMut, AnimationEvaluationError, AnimationTargetId};
    use bevy::ecs::component::ComponentId;
    use bevy::ecs::reflect::{AppTypeRegistry, ReflectFromWorld};
    use bevy::ecs::world::World;
    use bevy::math::{Quat, Vec2, Vec3, Vec4};
    use bevy::platform::hash::{FixedHasher, Hashed};
    use bevy::reflect::std_traits::ReflectDefault;
    use bevy::reflect::{GetPath, ParsedPath, ReflectFromPtr};

    use crate::clips::built::add_typed;

    /// A number, a vector or a rotation inside a component Bevy reflects, found by the component's
    /// id and a reflect path from its root, for a field whose type is known only by name when the
    /// curve is made, so no `animated_field!` reaches it from here.
    pub struct ReflectedField<T> {
        component: ComponentId,
        from_ptr: ReflectFromPtr,
        path: Arc<ParsedPath>,
        /// Bevy blends the curves of one evaluator together, so each component and path has its
        /// own, keyed as `animated_field!` keys a field, by the component's type and a number for
        /// the field, here a hash of the path with its top bit set, which no field's index has.
        evaluator: Hashed<(TypeId, usize)>,
        value: PhantomData<fn() -> T>,
    }

    impl<T> Clone for ReflectedField<T> {
        fn clone(&self) -> Self {
            Self {
                component: self.component,
                from_ptr: self.from_ptr.clone(),
                path: self.path.clone(),
                evaluator: self.evaluator.clone(),
                value: PhantomData,
            }
        }
    }

    impl<T: Animatable> AnimatableProperty for ReflectedField<T> {
        type Property = T;

        fn evaluator_id(&self) -> EvaluatorId<'_> {
            EvaluatorId::ComponentField(&self.evaluator)
        }

        fn get_mut<'a>(&self, entity: &'a mut AnimationEntityMut) -> Result<&'a mut Self::Property, AnimationEvaluationError> {
            // Taken as changed, so what draws the component sees the field move.
            let component = entity
                .get_mut_by_id::<ComponentId>(self.component)
                .ok_or(AnimationEvaluationError::ComponentNotPresent(self.from_ptr.type_id()))?
                .into_inner();

            // SAFETY: the pointer is to the component the id names, of the type the
            // `ReflectFromPtr` was taken from when the curve was made.
            let reflect = unsafe { self.from_ptr.ptr_as_reflect_mut(component) };
            reflect
                .reflect_path_mut(&*self.path)
                .ok()
                .and_then(|field| field.try_downcast_mut::<T>())
                .ok_or(AnimationEvaluationError::PropertyNotPresent(TypeId::of::<T>()))
        }
    }

    /// A reflected component's field, found and checked, for curves of any of the kinds a field
    /// may hold.
    pub struct FieldAt {
        component: ComponentId,
        from_ptr: ReflectFromPtr,
        path: Arc<ParsedPath>,
        evaluator: Hashed<(TypeId, usize)>,
    }

    impl FieldAt {
        fn typed<T>(&self) -> ReflectedField<T> {
            ReflectedField {
                component: self.component,
                from_ptr: self.from_ptr.clone(),
                path: self.path.clone(),
                evaluator: self.evaluator.clone(),
                value: PhantomData,
            }
        }
    }

    /// The type a field curve of a kind gives, `1` a number, `2` a `Vec2`, `3` a `Vec3`, `4` a
    /// `Vec4` and `5` a rotation.
    fn kind_type(kind: i32) -> Option<(TypeId, &'static str)> {
        Some(match kind {
            1 => (TypeId::of::<f32>(), "f32"),
            2 => (TypeId::of::<Vec2>(), "glam::Vec2"),
            3 => (TypeId::of::<Vec3>(), "glam::Vec3"),
            4 => (TypeId::of::<Vec4>(), "glam::Vec4"),
            5 => (TypeId::of::<Quat>(), "glam::Quat"),
            _ => return None,
        })
    }

    /// Finds a field of a reflected component by the component's type path and a reflect path,
    /// registering the component where nothing has yet, and checks it holds what the curve gives.
    ///
    /// The check reads a default of the component where Bevy can make one, so a path that leads
    /// nowhere or to another type is said when the curve is added rather than ignored each frame
    /// the clip plays. A component Bevy cannot make a default of is taken on trust.
    pub fn field(world: &mut World, type_path: &str, path: &str, kind: i32) -> Result<FieldAt, i32> {
        let Some((wanted, wanted_name)) = kind_type(kind) else {
            return Err(status::NULL_ARG);
        };
        let parsed = ParsedPath::parse(path).map_err(|error| crate::reflected::unfollowed(path, error))?;

        let registry = world.resource::<AppTypeRegistry>().clone();
        let registry = registry.read();
        let (registration, reflect) = crate::reflected::component_of(&registry, type_path)?;
        let Some(from_ptr) = registration.data::<ReflectFromPtr>().cloned() else {
            return Err(crate::reflected::fail(status::NO_COMPONENT, format!("'{type_path}' cannot be reached through a pointer, which a curve needs.")));
        };

        let made = match (registration.data::<ReflectDefault>(), registration.data::<ReflectFromWorld>()) {
            (Some(default), _) => Some(default.default()),
            (None, Some(from_world)) => Some(from_world.from_world(world)),
            (None, None) => None,
        };
        if let Some(made) = made {
            let found = made
                .reflect_path(&parsed)
                .map_err(|error| crate::reflected::unfollowed(path, error))?;
            let holds = found.try_as_reflect().map(|field| (field.type_id(), field.reflect_type_path().to_string()));
            if holds.as_ref().map(|(id, _)| *id) != Some(wanted) {
                let held = holds.map(|(_, name)| name).unwrap_or_else(|| "value with no type".to_string());
                return Err(crate::reflected::fail(
                    status::INVALID_STATE,
                    format!("The field '{path}' of '{type_path}' holds a {held}, and the curve gives a {wanted_name}."),
                ));
            }
        }

        let component = reflect.register_component(world);
        let field = FixedHasher.hash_one(path) as usize | (1usize << (usize::BITS - 1));
        Ok(FieldAt {
            component,
            from_ptr,
            path: Arc::new(parsed),
            evaluator: Hashed::new((registration.type_id(), field)),
        })
    }

    /// Adds a curve moving a field found by [`field`], its values read as the kind gives them.
    pub fn add_field(clip: &mut AnimationClip, target: AnimationTargetId, curve: &BcsAnimationCurve, times: &[f32], floats: &[f32], at: &FieldAt, kind: i32) -> i32 {
        let n = curve.count as usize;
        let read = |width: usize| -> Option<&[f32]> { (floats.len() >= n * width).then(|| &floats[..n * width]) };

        match kind {
            1 => {
                let Some(raw) = read(1) else { return status::NULL_ARG };
                add_typed(clip, target, at.typed::<f32>(), curve, times, raw.to_vec())
            }
            2 => {
                let Some(raw) = read(2) else { return status::NULL_ARG };
                add_typed(clip, target, at.typed::<Vec2>(), curve, times, raw.chunks_exact(2).map(Vec2::from_slice).collect())
            }
            3 => {
                let Some(raw) = read(3) else { return status::NULL_ARG };
                add_typed(clip, target, at.typed::<Vec3>(), curve, times, raw.chunks_exact(3).map(Vec3::from_slice).collect())
            }
            4 => {
                let Some(raw) = read(4) else { return status::NULL_ARG };
                add_typed(clip, target, at.typed::<Vec4>(), curve, times, raw.chunks_exact(4).map(Vec4::from_slice).collect())
            }
            5 => {
                let Some(raw) = read(4) else { return status::NULL_ARG };
                let values: Vec<Quat> = raw.chunks_exact(4).map(|q| Quat::from_xyzw(q[0], q[1], q[2], q[3]).normalize()).collect();
                add_typed(clip, target, at.typed::<Quat>(), curve, times, values)
            }
            _ => status::NULL_ARG,
        }
    }
}

/// Adds a curve moving a field of a component Bevy reflects to a clip, aimed at the target whose
/// identifier's halves are given. `kind` says what the field holds, `1` a number, `2` a `Vec2`,
/// `3` a `Vec3`, `4` a `Vec4` and `5` a rotation, and a failure leaves its reason for
/// `bcs_reflect_error`.
///
/// # Safety
/// As [`crate::clips::bcs_animation_clip_add_curve`], with `type_path` and `path` NUL-terminated
/// UTF-8 strings.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_animation_clip_add_field_curve(
    clip: i32,
    high: u64,
    low: u64,
    curve: *const BcsAnimationCurve,
    times: *const f32,
    values: *const f32,
    value_count: i32,
    type_path: *const core::ffi::c_char,
    path: *const core::ffi::c_char,
    kind: i32,
) -> i32 {
    crate::interop::guard(|| {
        if curve.is_null() || values.is_null() || value_count < 0 {
            return status::NULL_ARG;
        }
        let curve = unsafe { *curve };
        if curve.count < 1 || (curve.kind == 0 && times.is_null()) {
            return status::NULL_ARG;
        }
        let (Some(type_path), Some(path)) = (unsafe { crate::interop::cstr_to_string(type_path) }, unsafe { crate::interop::cstr_to_string(path) }) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (clip, high, low, times, type_path, path, kind);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let times = if curve.kind == 0 { unsafe { core::slice::from_raw_parts(times, curve.count as usize) } } else { &[][..] };
            let floats = unsafe { core::slice::from_raw_parts(values, value_count as usize) };
            let target = bevy::animation::AnimationTargetId(bevy::asset::uuid::Uuid::from_u64_pair(high, low));

            crate::state::with_world(|world| {
                // Found before the clip is borrowed, since finding it may register the component.
                let at = match built::field(world, &type_path, &path, kind) {
                    Ok(at) => at,
                    Err(code) => return code,
                };
                let Some(handle) = crate::assets::clone_handle(world, clip).and_then(|handle| handle.try_typed::<bevy::animation::AnimationClip>().ok()) else {
                    return status::NOT_PRESENT;
                };
                let Some(mut clips) = world.get_resource_mut::<bevy::asset::Assets<bevy::animation::AnimationClip>>() else {
                    return status::UNSUPPORTED;
                };
                let Some(clip) = clips.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                built::add_field(clip.into_inner(), target, &curve, times, floats, &at, kind)
            })
        }
    })
}

//! Calling back into C# when a pointer does something to an entity, through observers of Bevy's
//! picking events.
//!
//! Bevy's picking finds what is under each pointer, the interface's nodes, sprites and meshes, and
//! triggers an event at that entity for each of seventeen kinds of thing a pointer does. A
//! C# game observes them as any entity event, so the first C# observer of a kind asks for an
//! observer here, which copies the event into one shape every kind fits and queues a call into C#,
//! made with the whole world on loan once the trigger finishes, as [`crate::observe`] calls C# for
//! a component's coming and going.
//!
//! Bevy's pointer events go on up an entity's parents. The observer here watches every entity, so
//! it runs at each step, and calls C# at the first alone, the entity the pointer was over, since C#
//! takes an event it is handed up the parents itself, where its observers can stop it.

use core::ffi::c_void;

use crate::interop::status;

/// Something a pointer did, every kind in one shape, a field a kind has no use for being zero.
///
/// The managed side's `NativePointerEvent`, field for field.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsPointerEvent {
    pub entity: u64,
    /// Which finger for a touch, the last eight bytes of the identifier for a pointer of the game's
    /// own, which for one [`bcs_pointer_spawn`] made is its number.
    pub pointer_number: u64,
    pub hit_camera: u64,
    /// The entity dragged, or the one dropped.
    pub other: u64,
    /// How long a click's button was held, in seconds.
    pub duration: f64,
    /// Which of the seventeen kinds, in the order of [`spawn`]'s match.
    pub kind: i32,
    /// Zero for the mouse, one for a touch and two for a pointer of the game's own.
    pub pointer_kind: i32,
    /// Zero for the primary button, one for the secondary and two for the middle.
    pub button: i32,
    pub count: u32,
    pub position: [f32; 2],
    pub hit_depth: f32,
    /// One where the hit has a position, two where it has a normal, both added.
    pub hit_flags: u32,
    pub hit_position: [f32; 3],
    pub hit_normal: [f32; 3],
    pub delta: [f32; 2],
    pub distance: [f32; 2],
    pub scroll: [f32; 2],
    /// Zero for lines, one for pixels.
    pub scroll_unit: i32,
    /// Whether an enter is, or a leave was, in the entity's own bounds.
    pub in_bounds: u32,
}

/// What C# is called with, the event, valid only for the call, and the handle C# finds its
/// handlers through.
pub type PointerCallback = unsafe extern "C" fn(event: *const BcsPointerEvent, user: *mut c_void);

#[cfg(feature = "render")]
mod observers {
    use super::*;

    use bevy::ecs::observer::{Observer, On};
    use bevy::ecs::world::{DeferredWorld, World};
    use bevy::picking::backend::HitData;
    use bevy::picking::events::*;
    use bevy::picking::pointer::{PointerButton, PointerId};
    use bevy::prelude::Entity;

    use crate::state::loan_world;

    /// The callback and the handle C# finds its handlers through, carried into the observer.
    #[derive(Clone, Copy)]
    pub struct Target {
        pub callback: PointerCallback,
        pub user: *mut c_void,
    }

    // SAFETY: the pointers are only dereferenced by calling back into the .NET runtime, which is
    // thread-safe itself, and Bevy needs the bound to keep them in an observer.
    unsafe impl Send for Target {}
    unsafe impl Sync for Target {}

    impl Target {
        /// Calls C#, through the struct as a whole so a closure carries the struct and its
        /// bounds rather than the raw pointer inside it.
        fn call(&self, event: &BcsPointerEvent) {
            unsafe { (self.callback)(event, self.user) };
        }
    }

    fn hit(event: &mut BcsPointerEvent, hit: &HitData) {
        event.hit_camera = hit.camera.to_bits();
        event.hit_depth = hit.depth;
        if let Some(position) = hit.position {
            event.hit_flags |= 1;
            event.hit_position = position.to_array();
        }
        if let Some(normal) = hit.normal {
            event.hit_flags |= 2;
            event.hit_normal = normal.to_array();
        }
    }

    fn button(event: &mut BcsPointerEvent, button: PointerButton) {
        event.button = match button {
            PointerButton::Primary => 0,
            PointerButton::Secondary => 1,
            PointerButton::Middle => 2,
        };
    }

    /// What every kind shares, the entity, the pointer and where it is.
    fn common(kind: i32, entity: Entity, pointer: &Pointer) -> BcsPointerEvent {
        let (pointer_kind, pointer_number) = match pointer.id {
            PointerId::Mouse => (0, 0),
            PointerId::Touch(id) => (1, id),
            PointerId::Custom(uuid) => (2, uuid.as_u64_pair().1),
        };
        BcsPointerEvent {
            entity: entity.to_bits(),
            pointer_number,
            kind,
            pointer_kind,
            position: pointer.position.to_array(),
            ..Default::default()
        }
    }

    /// Queues the call into C# for the first step of an event, the entity the pointer was over.
    fn forward<E: PointerEvent>(
        pointer: &E,
        original: Entity,
        world: &mut DeferredWorld,
        target: Target,
        fill: impl FnOnce(&mut BcsPointerEvent, &E),
        kind: i32,
    ) {
        if pointer.event_target() != original {
            return;
        }

        let mut report = common(kind, pointer.event_target(), pointer.pointer());
        fill(&mut report, pointer);
        world.commands().queue(move |world: &mut World| {
            loan_world(world, || target.call(&report));
        });
    }

    /// An observer of one kind of thing a pointer does, by the numbers the managed side's
    /// `PointerEvents` gives them.
    pub fn spawn(world: &mut World, kind: i32, target: Target) -> Option<Entity> {
        macro_rules! observe {
            ($event:ty, $fill:expr) => {
                Observer::new(move |event: On<$event>, mut world: DeferredWorld| {
                    forward(event.event(), event.original_event_target(), &mut world, target, $fill, kind)
                })
            };
        }

        let observer = match kind {
            0 => observe!(PointerOver, |r, e: &PointerOver| hit(r, &e.hit)),
            1 => observe!(PointerOut, |r, e: &PointerOut| hit(r, &e.hit)),
            2 => observe!(PointerEnter, |r, e: &PointerEnter| {
                hit(r, &e.hit);
                r.in_bounds = e.is_in_bounds as u32;
            }),
            3 => observe!(PointerLeave, |r, e: &PointerLeave| {
                hit(r, &e.hit);
                r.in_bounds = e.was_in_bounds as u32;
            }),
            4 => observe!(PointerPress, |r, e: &PointerPress| {
                button(r, e.button);
                hit(r, &e.hit);
                r.count = e.count as u32;
            }),
            5 => observe!(PointerRelease, |r, e: &PointerRelease| {
                button(r, e.button);
                hit(r, &e.hit);
            }),
            6 => observe!(PointerClick, |r, e: &PointerClick| {
                button(r, e.button);
                hit(r, &e.hit);
                r.duration = e.duration.as_secs_f64();
                r.count = e.count as u32;
            }),
            7 => observe!(PointerMove, |r, e: &PointerMove| {
                hit(r, &e.hit);
                r.delta = e.delta.to_array();
            }),
            8 => observe!(PointerDragStart, |r, e: &PointerDragStart| {
                button(r, e.button);
                hit(r, &e.hit);
            }),
            9 => observe!(PointerDrag, |r, e: &PointerDrag| {
                button(r, e.button);
                r.distance = e.distance.to_array();
                r.delta = e.delta.to_array();
            }),
            10 => observe!(PointerDragEnd, |r, e: &PointerDragEnd| {
                button(r, e.button);
                r.distance = e.distance.to_array();
            }),
            11 => observe!(PointerDragEnter, |r, e: &PointerDragEnter| {
                button(r, e.button);
                r.other = e.dragged.to_bits();
                hit(r, &e.hit);
            }),
            12 => observe!(PointerDragOver, |r, e: &PointerDragOver| {
                button(r, e.button);
                r.other = e.dragged.to_bits();
                hit(r, &e.hit);
            }),
            13 => observe!(PointerDragLeave, |r, e: &PointerDragLeave| {
                button(r, e.button);
                r.other = e.dragged.to_bits();
                hit(r, &e.hit);
            }),
            14 => observe!(PointerDragDrop, |r, e: &PointerDragDrop| {
                button(r, e.button);
                r.other = e.dropped.to_bits();
                hit(r, &e.hit);
            }),
            15 => observe!(PointerScroll, |r, e: &PointerScroll| {
                r.scroll_unit = match e.unit {
                    bevy::input::mouse::MouseScrollUnit::Line => 0,
                    bevy::input::mouse::MouseScrollUnit::Pixel => 1,
                };
                r.scroll = [e.x, e.y];
                hit(r, &e.hit);
            }),
            16 => observe!(PointerCancel, |r, e: &PointerCancel| hit(r, &e.hit)),
            _ => return None,
        };

        Some(world.spawn(observer).id())
    }
}

/// Asks Bevy to report one kind of thing a pointer does to C#, through an observer it spawns, and
/// writes the observer's entity to `out`.
///
/// On the app before it runs, or on the world from inside a system where `app` is null. Returns
/// [`status::UNSUPPORTED`] in a build without the renderer, which has nothing drawn to point at,
/// and [`status::NULL_ARG`] for a kind it does not know.
///
/// # Safety
/// `out` must be writable, and `app` a live app or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_observe_pointer(
    app: *mut crate::state::BcsApp,
    kind: i32,
    callback: Option<PointerCallback>,
    user: *mut c_void,
    out: *mut u64,
) -> i32 {
    crate::interop::guard(|| {
        let Some(callback) = callback else {
            return status::NULL_ARG;
        };
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (app, kind, callback, user);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let target = observers::Target { callback, user };
            let watch = |world: &mut bevy::ecs::world::World| -> i32 {
                match observers::spawn(world, kind, target) {
                    Some(entity) => {
                        unsafe { out.write(entity.to_bits()) };
                        status::OK
                    }
                    None => status::NULL_ARG,
                }
            };

            if app.is_null() {
                return crate::state::with_world(watch);
            }
            match unsafe { crate::state::app_mut(app) } {
                Some(app) => watch(app.app.world_mut()),
                None => status::NULL_ARG,
            }
        }
    })
}

/// What the first eight bytes of a pointer this bridge makes say, so its identifier is its number
/// and nothing a game or Bevy makes with a random one is taken for it.
#[cfg(feature = "render")]
const OWN_POINTER: u64 = 0x6263_735f_706f_696e;

/// The number of the next pointer [`bcs_pointer_spawn`] makes.
#[cfg(feature = "render")]
static NEXT_POINTER: core::sync::atomic::AtomicU64 = core::sync::atomic::AtomicU64::new(1);

/// Spawns a pointer of the game's own, Bevy's `PointerId::Custom`, and writes its number.
///
/// A pointer a game drives itself, as one moving over an interface drawn into a texture on a
/// cube, which the game moves to where a ray from the mouse meets the cube. Bevy finds what it is
/// over as it finds what the mouse is over, wherever [`bcs_pointer_input`] puts it.
///
/// # Safety
/// `number` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pointer_spawn(number: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if number.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let next = NEXT_POINTER.fetch_add(1, core::sync::atomic::Ordering::Relaxed);
            crate::state::with_world(|world| {
                let id = bevy::picking::pointer::PointerId::Custom(bevy::asset::uuid::Uuid::from_u64_pair(OWN_POINTER, next));
                world.spawn(id);
                unsafe { number.write(next) };
                status::OK
            })
        }
    })
}

/// Moves, presses or releases a pointer [`bcs_pointer_spawn`] made, at `x` and `y` on the image
/// `image`, in its pixels from the top left.
///
/// Bevy's own `PointerInput`, which picking reads every pointer from, so what is drawn on the
/// image, an interface a camera draws there, is pointed at as the mouse points at a window.
/// `action` is 0 to move, 1 to press and 2 to release, `button` 0 for the primary button, 1 for
/// the secondary and 2 for the middle one. A move goes from where the pointer was.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_pointer_input(number: u64, image: i32, x: f32, y: f32, action: i32, button: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (number, image, x, y, action, button);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::NormalizedRenderTarget;
            use bevy::math::Vec2;
            use bevy::picking::pointer::{Location, PointerAction, PointerButton, PointerId, PointerInput, PointerLocation};

            crate::state::with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };
                let id = PointerId::Custom(bevy::asset::uuid::Uuid::from_u64_pair(OWN_POINTER, number));
                let at = Vec2::new(x, y);

                // How far it went, from where Bevy last put it.
                let mut pointers = world.query::<(&PointerId, &PointerLocation)>();
                let Some(was) = pointers.iter(world).find(|(found, _)| **found == id).map(|(_, location)| location.location().map(|place| place.position)) else {
                    return status::NO_ENTITY;
                };

                let location = Location {
                    target: NormalizedRenderTarget::Image(handle.into()),
                    position: at,
                };
                let button = match button {
                    1 => PointerButton::Secondary,
                    2 => PointerButton::Middle,
                    _ => PointerButton::Primary,
                };
                let act = match action {
                    1 => PointerAction::Press(button),
                    2 => PointerAction::Release(button),
                    _ => PointerAction::Move { delta: was.map_or(Vec2::ZERO, |was| at - was) },
                };

                world.write_message(PointerInput::new(id, location, act));
                status::OK
            })
        }
    })
}

/// The pointer Bevy knows by the kind and number C# reports it under: `0` the mouse, `1` a touch by
/// its finger, `2` a pointer [`bcs_pointer_spawn`] made, by its number.
#[cfg(feature = "render")]
fn pointer_id(kind: i32, number: u64) -> Option<bevy::picking::pointer::PointerId> {
    use bevy::picking::pointer::PointerId;

    Some(match kind {
        0 => PointerId::Mouse,
        1 => PointerId::Touch(number),
        2 => PointerId::Custom(bevy::asset::uuid::Uuid::from_u64_pair(OWN_POINTER, number)),
        _ => return None,
    })
}

/// Locks a pointer to an entity, Bevy's `PointerCaptureMap::capture`, so the entity is all the
/// pointer is over until the capture is released or the pointer's button is let go, as a slider's
/// thumb keeps the pointer through a drag that strays over other widgets.
///
/// The pointer reports the hit while it is held, given as the camera it was seen through, the depth
/// and, where `position` and `normal` are not null, three numbers each for where and which way.
/// Returns [`status::UNSUPPORTED`] in a build without the renderer, which has no picking.
///
/// # Safety
/// `position` and `normal` must each be null or point at three floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pointer_capture(
    kind: i32,
    number: u64,
    entity: u64,
    camera: u64,
    depth: f32,
    position: *const f32,
    normal: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, number, entity, camera, depth, position, normal);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::math::Vec3;
            use bevy::picking::backend::HitData;
            use bevy::picking::hover::PointerCaptureMap;

            let Some(pointer) = pointer_id(kind, number) else {
                return status::NULL_ARG;
            };
            let three = |at: *const f32| {
                (!at.is_null()).then(|| unsafe { Vec3::from_slice(core::slice::from_raw_parts(at, 3)) })
            };
            let hit = HitData::new(crate::ecs::entity_from(camera), depth, three(position), three(normal));

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);
                if world.get_entity(entity).is_err() {
                    return status::NO_ENTITY;
                }
                let Some(mut captures) = world.get_resource_mut::<PointerCaptureMap>() else {
                    return status::UNSUPPORTED;
                };
                captures.capture(pointer, entity, hit);
                status::OK
            })
        }
    })
}

/// Releases what a pointer was locked to by [`bcs_pointer_capture`], Bevy's
/// `PointerCaptureMap::release`, which does nothing where it was not captured.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_pointer_release_capture(kind: i32, number: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, number);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(pointer) = pointer_id(kind, number) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                let Some(mut captures) = world.get_resource_mut::<bevy::picking::hover::PointerCaptureMap>() else {
                    return status::UNSUPPORTED;
                };
                captures.release(pointer);
                status::OK
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn every_field_sits_where_the_managed_side_reads_it() {
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, entity), 0);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, pointer_number), 8);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, hit_camera), 16);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, other), 24);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, duration), 32);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, kind), 40);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, button), 48);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, count), 52);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, position), 56);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, hit_depth), 64);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, hit_flags), 68);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, hit_position), 72);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, hit_normal), 84);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, delta), 96);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, distance), 104);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, scroll), 112);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, scroll_unit), 120);
        assert_eq!(core::mem::offset_of!(BcsPointerEvent, in_bounds), 124);
        assert_eq!(core::mem::size_of::<BcsPointerEvent>(), 128);
    }
}

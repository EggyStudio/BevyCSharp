//! Calling back into C# when a pointer does something to an entity, through observers of Bevy's
//! picking events.
//!
//! Bevy's picking finds what is under each pointer, the interface's nodes, sprites and meshes, and
//! triggers a `Pointer<E>` at that entity for each of seventeen kinds of thing a pointer does. A
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
    /// Which finger for a touch, the first eight bytes of the identifier for a pointer of the game's own.
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
    fn common<E: core::fmt::Debug + Clone + bevy::reflect::Reflect>(kind: i32, pointer: &Pointer<E>) -> BcsPointerEvent {
        let (pointer_kind, pointer_number) = match pointer.pointer_id {
            PointerId::Mouse => (0, 0),
            PointerId::Touch(id) => (1, id),
            PointerId::Custom(uuid) => (2, uuid.as_u64_pair().0),
        };
        BcsPointerEvent {
            entity: pointer.entity.to_bits(),
            pointer_number,
            kind,
            pointer_kind,
            position: pointer.pointer_location.position.to_array(),
            ..Default::default()
        }
    }

    /// Queues the call into C# for the first step of an event, the entity the pointer was over.
    fn forward<E: core::fmt::Debug + Clone + bevy::reflect::Reflect>(
        event: &On<Pointer<E>>,
        world: &mut DeferredWorld,
        target: Target,
        fill: impl FnOnce(&mut BcsPointerEvent, &E),
        kind: i32,
    ) {
        let pointer = event.event();
        if pointer.entity != event.original_event_target() {
            return;
        }

        let mut report = common(kind, pointer);
        fill(&mut report, &pointer.event);
        world.commands().queue(move |world: &mut World| {
            loan_world(world, || target.call(&report));
        });
    }

    /// An observer of one kind of thing a pointer does, by the numbers the managed side's
    /// `PointerEvents` gives them.
    pub fn spawn(world: &mut World, kind: i32, target: Target) -> Option<Entity> {
        macro_rules! observe {
            ($event:ty, $fill:expr) => {
                Observer::new(move |event: On<Pointer<$event>>, mut world: DeferredWorld| {
                    forward(&event, &mut world, target, $fill, kind)
                })
            };
        }

        let observer = match kind {
            0 => observe!(Over, |r, e: &Over| hit(r, &e.hit)),
            1 => observe!(Out, |r, e: &Out| hit(r, &e.hit)),
            2 => observe!(Enter, |r, e: &Enter| {
                hit(r, &e.hit);
                r.in_bounds = e.is_in_bounds as u32;
            }),
            3 => observe!(Leave, |r, e: &Leave| {
                hit(r, &e.hit);
                r.in_bounds = e.was_in_bounds as u32;
            }),
            4 => observe!(Press, |r, e: &Press| {
                button(r, e.button);
                hit(r, &e.hit);
                r.count = e.count as u32;
            }),
            5 => observe!(Release, |r, e: &Release| {
                button(r, e.button);
                hit(r, &e.hit);
            }),
            6 => observe!(Click, |r, e: &Click| {
                button(r, e.button);
                hit(r, &e.hit);
                r.duration = e.duration.as_secs_f64();
                r.count = e.count as u32;
            }),
            7 => observe!(Move, |r, e: &Move| {
                hit(r, &e.hit);
                r.delta = e.delta.to_array();
            }),
            8 => observe!(DragStart, |r, e: &DragStart| {
                button(r, e.button);
                hit(r, &e.hit);
            }),
            9 => observe!(Drag, |r, e: &Drag| {
                button(r, e.button);
                r.distance = e.distance.to_array();
                r.delta = e.delta.to_array();
            }),
            10 => observe!(DragEnd, |r, e: &DragEnd| {
                button(r, e.button);
                r.distance = e.distance.to_array();
            }),
            11 => observe!(DragEnter, |r, e: &DragEnter| {
                button(r, e.button);
                r.other = e.dragged.to_bits();
                hit(r, &e.hit);
            }),
            12 => observe!(DragOver, |r, e: &DragOver| {
                button(r, e.button);
                r.other = e.dragged.to_bits();
                hit(r, &e.hit);
            }),
            13 => observe!(DragLeave, |r, e: &DragLeave| {
                button(r, e.button);
                r.other = e.dragged.to_bits();
                hit(r, &e.hit);
            }),
            14 => observe!(DragDrop, |r, e: &DragDrop| {
                button(r, e.button);
                r.other = e.dropped.to_bits();
                hit(r, &e.hit);
            }),
            15 => observe!(Scroll, |r, e: &Scroll| {
                r.scroll_unit = match e.unit {
                    bevy::input::mouse::MouseScrollUnit::Line => 0,
                    bevy::input::mouse::MouseScrollUnit::Pixel => 1,
                };
                r.scroll = [e.x, e.y];
                hit(r, &e.hit);
            }),
            16 => observe!(Cancel, |r, e: &Cancel| hit(r, &e.hit)),
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

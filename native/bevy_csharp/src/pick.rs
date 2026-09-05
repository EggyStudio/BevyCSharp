//! What was clicked in the scene, rather than in the interface.
//!
//! Needs the `editor` feature. The entry point exists in every profile and reports
//! [`status::UNSUPPORTED`] without it, the same as the rest of the editor surface.
//!
//! Bevy's picking is already compiled in, since the interface crate depends on it, but it only
//! knows about the interface: hitting a mesh needs `MeshPickingPlugin`, which raycasts the
//! meshes in the scene against the pointer. Adding it here is what turns a click on the viewport
//! into an entity, which is the half of selection a hierarchy list cannot give.
//!
//! Clicks are queued and drained rather than observed, for the reason every other report here is:
//! a C# system is handed the world and cannot hold an observer.

use crate::interop::status;

/// The scene entities clicked since the managed side last looked.
#[cfg(feature = "editor")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct Picks(pub Vec<u64>);

/// Adds mesh picking and the queue behind [`bcs_pick_events`].
#[cfg(feature = "editor")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::picking::events::{Click, Pointer};
    use bevy::prelude::*;

    app.add_plugins(bevy::picking::mesh_picking::MeshPickingPlugin);
    app.init_resource::<Picks>();

    app.add_observer(
        |click: On<Pointer<Click>>,
         meshes: Query<(), With<bevy::mesh::Mesh3d>>,
         mut picks: ResMut<Picks>| {
            // The primary button only. The secondary one steers the camera in every editor
            // there is, and a look that happens to begin over an object is not a choice to
            // select that object.
            if click.event().button != bevy::picking::pointer::PointerButton::Primary {
                return;
            }

            // Only meshes. Every widget in the interface is picked too, and those are reported
            // through the UI queue with the element that carries them; a click that hit a panel
            // is not also a click on whatever the panel is in front of.
            if meshes.get(click.entity).is_err() {
                return;
            }

            picks.0.push(click.entity.to_bits());
        },
    );
}

/// Copies the scene entities clicked since the last call, returning how many were written.
///
/// # Safety
/// `out` must be writable for `capacity` entities, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pick_events(out: *mut u64, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            if out.is_null() && capacity > 0 {
                return status::NULL_ARG;
            }
            let capacity = capacity.max(0) as usize;

            crate::state::with_world(|world| {
                let Some(mut picks) = world.get_resource_mut::<Picks>() else {
                    return status::UNSUPPORTED;
                };

                let taken = picks.0.len().min(capacity);
                for (index, entity) in picks.0.drain(..taken).enumerate() {
                    // SAFETY: `index < taken <= capacity`, and `out` is valid for `capacity`.
                    unsafe { out.add(index).write(entity) };
                }

                taken as i32
            })
        }
    })
}

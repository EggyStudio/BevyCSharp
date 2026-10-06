//! What was clicked in the scene, rather than in the interface, and the mesh a ray meets.
//!
//! Needs the `render` feature. The entry points exist in every profile and report
//! [`status::UNSUPPORTED`] without it.
//!
//! Bevy's picking knows the interface's nodes and sprites by default. Hitting a mesh needs
//! `MeshPickingPlugin`, which raycasts the meshes in the scene against the pointer, and which an app
//! asks for (`Config.MeshPicking`) and the editor has. Adding it here turns a click on the viewport
//! into an entity, which is the half of selection a hierarchy list cannot give, and gives a game's
//! observers of what a pointer does (see [`crate::pointer`]) the meshes too.
//!
//! The editor's clicks are queued and drained, an older way than the observers a game has.

use crate::interop::status;

/// The scene entities clicked since the managed side last looked.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct Picks(pub Vec<u64>);

/// Adds mesh picking and the queue behind [`bcs_pick_events`].
#[cfg(feature = "render")]
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
        #[cfg(not(feature = "render"))]
        {
            let _ = (out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
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

/// Casts a ray at the scene's meshes and writes the nearest one it meets, where, and which way the
/// surface there faces.
///
/// What a model dropped on the viewport is put on, rather than the ground plane under the
/// pointer. Bevy's `MeshRayCast` tests the ray against every mesh's triangles, nearest first.
/// Only meshes on the default render layer are met, so the editor's previews, which are drawn on
/// layers of their own, are never in the way.
///
/// Returns [`status::NOT_PRESENT`] when the ray meets nothing.
///
/// `uv` is where on the mesh's texture the ray met it, as Bevy's ray cast says from the mesh's
/// texture coordinates, or two NaNs for a mesh with none.
///
/// # Safety
/// `origin` and `direction` must hold three floats each. `entity` must be writable, `point` and
/// `normal` writable for three floats each and `uv` for two, or null to skip them.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_pick_ray(
    origin: *const f32,
    direction: *const f32,
    entity: *mut u64,
    point: *mut f32,
    normal: *mut f32,
    uv: *mut f32,
) -> i32 {
    crate::interop::guard(|| {
        if origin.is_null() || direction.is_null() || entity.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (point, normal, uv);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::visibility::RenderLayers;
            use bevy::ecs::system::SystemState;
            use bevy::math::{Dir3, Ray3d, Vec3};
            use bevy::picking::mesh_picking::ray_cast::{MeshRayCast, MeshRayCastSettings};

            let from = unsafe { Vec3::from_slice(std::slice::from_raw_parts(origin, 3)) };
            let towards = unsafe { Vec3::from_slice(std::slice::from_raw_parts(direction, 3)) };
            let Ok(towards) = Dir3::new(towards) else {
                return status::NULL_ARG;
            };

            crate::state::with_world(|world| {
                // Which entities sit off the default layer, gathered before the cast borrows the
                // world, since the filter it takes cannot ask the world itself.
                let mut layered = world.query::<(bevy::ecs::entity::Entity, &RenderLayers)>();
                let elsewhere: std::collections::HashSet<_> = layered
                    .iter(world)
                    .filter(|(_, layers)| !layers.intersects(&RenderLayers::layer(0)))
                    .map(|(found, _)| found)
                    .collect();

                let mut state = SystemState::<MeshRayCast>::new(world);
                let Ok(mut cast) = state.get_mut(world) else {
                    return status::NOT_PRESENT;
                };

                let keep = |found: bevy::ecs::entity::Entity| !elsewhere.contains(&found);
                let settings = MeshRayCastSettings::default().with_filter(&keep);

                let Some((hit, at)) = cast.cast_ray(Ray3d::new(from, towards), &settings).first().cloned() else {
                    return status::NOT_PRESENT;
                };

                unsafe {
                    entity.write(hit.to_bits());

                    if !point.is_null() {
                        std::ptr::copy_nonoverlapping(at.point.to_array().as_ptr(), point, 3);
                    }
                    if !normal.is_null() {
                        std::ptr::copy_nonoverlapping(at.normal.normalize_or_zero().to_array().as_ptr(), normal, 3);
                    }
                    if !uv.is_null() {
                        let at_uv = at.uv.map_or([f32::NAN; 2], |found| found.to_array());
                        std::ptr::copy_nonoverlapping(at_uv.as_ptr(), uv, 2);
                    }
                }

                status::OK
            })
        }
    })
}

//! The layer the rain's map of what stands over the ground draws, which no light stands on.
//!
//! Bevy makes one map of the point lights' shadows and one of the directional and spot lights' a
//! frame for every view's lights together, a light in them wherever any view sees it. The camera
//! that draws the roofs' depth for the rain (`Render.Precipitation.cs`) draws a few frames at a
//! time, as the eye moves away from where it last drew, and drawing the default layer, on which
//! the lights stand, it would bring the lamps it sees from above into the frame's lights for those
//! frames, the maps made again at another size each time it draws and each time it stops. It
//! draws a layer of its own, [`ROOF_LAYER`], past the 32 a game's layer masks name, and every mesh
//! on a layer the map draws takes that layer beside its own as it is spawned or moved to other
//! layers, so the map draws those meshes and sees no light.

use crate::interop::status;

/// The layer the roofs' cameras draw, the first past the 32 a game's masks name, so no game's
/// own camera or light stands on it.
#[cfg(feature = "render")]
pub const ROOF_LAYER: usize = 32;

/// The roofs' cameras and the layers each one's map draws, as a game's mask names them, zero
/// for the default layer.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
struct RoofCameras(std::collections::HashMap<bevy::ecs::entity::Entity, u32>);

/// Keeps the meshes the roofs' cameras draw on their layer, from zero cameras for a new app.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    use bevy::ecs::schedule::IntoScheduleConfigs;

    app.init_resource::<RoofCameras>();
    app.add_systems(
        bevy::app::PostUpdate,
        tag.before(bevy::camera::visibility::VisibilitySystems::CheckVisibility),
    );
}

/// The layers a mask names, the default layer for none.
#[cfg(feature = "render")]
fn named(mask: u32) -> bevy::camera::visibility::RenderLayers {
    crate::render::scene::layers_from(mask).unwrap_or_default()
}

/// Gives each mesh on a layer a roofs' camera draws the roofs' layer beside its own and takes it
/// from one on none, every mesh where the cameras changed and otherwise the meshes spawned or
/// moved to other layers since the frame before, a mesh's layers taken off it among them, since
/// a mesh on no layer of its own stands on the default one. A camera despawned leaves the set.
#[cfg(feature = "render")]
fn tag(
    mut commands: bevy::ecs::system::Commands,
    mut cameras: bevy::ecs::system::ResMut<RoofCameras>,
    alive: bevy::ecs::system::Query<(), bevy::ecs::query::With<bevy::camera::Camera>>,
    meshes: bevy::ecs::system::Query<
        (bevy::ecs::entity::Entity, Option<&bevy::camera::visibility::RenderLayers>),
        bevy::ecs::query::With<bevy::mesh::Mesh3d>,
    >,
    moved: bevy::ecs::system::Query<
        bevy::ecs::entity::Entity,
        bevy::ecs::query::Or<(
            bevy::ecs::query::Added<bevy::mesh::Mesh3d>,
            bevy::ecs::query::Changed<bevy::camera::visibility::RenderLayers>,
        )>,
    >,
    mut unlayered: bevy::ecs::lifecycle::RemovedComponents<bevy::camera::visibility::RenderLayers>,
    #[cfg(feature = "meshlet")] meshlets: bevy::ecs::system::Query<
        (bevy::ecs::entity::Entity, Option<&bevy::camera::visibility::RenderLayers>),
        bevy::ecs::query::With<bevy::pbr::experimental::meshlet::MeshletMesh3d>,
    >,
    #[cfg(feature = "meshlet")] added_meshlets: bevy::ecs::system::Query<
        bevy::ecs::entity::Entity,
        bevy::ecs::query::Added<bevy::pbr::experimental::meshlet::MeshletMesh3d>,
    >,
) {
    use bevy::camera::visibility::RenderLayers;
    use bevy::ecs::change_detection::DetectChanges;

    if cameras.0.keys().any(|camera| alive.get(*camera).is_err()) {
        cameras.0.retain(|camera, _| alive.get(*camera).is_ok());
    }

    let drawn = cameras
        .0
        .values()
        .fold(RenderLayers::none(), |drawn, mask| drawn.union(&named(*mask)));
    let roof = RenderLayers::layer(ROOF_LAYER);
    let mut keep = |entity, layers: Option<&RenderLayers>| {
        let layers = layers.cloned().unwrap_or_default();
        let wanted = layers.intersects(&drawn);
        if wanted != layers.intersects(&roof) {
            let layers = if wanted { layers.with(ROOF_LAYER) } else { layers.without(ROOF_LAYER) };
            commands.entity(entity).insert(layers);
        }
    };

    if cameras.is_changed() {
        for (entity, layers) in &meshes {
            keep(entity, layers);
        }
        #[cfg(feature = "meshlet")]
        for (entity, layers) in &meshlets {
            keep(entity, layers);
        }
        unlayered.clear();
        return;
    }

    if cameras.0.is_empty() {
        unlayered.clear();
        return;
    }

    #[cfg(feature = "meshlet")]
    let candidates = moved.iter().chain(added_meshlets.iter()).chain(unlayered.read());
    #[cfg(not(feature = "meshlet"))]
    let candidates = moved.iter().chain(unlayered.read());
    for entity in candidates {
        if let Ok((entity, layers)) = meshes.get(entity) {
            keep(entity, layers);
        }
        #[cfg(feature = "meshlet")]
        if let Ok((entity, layers)) = meshlets.get(entity) {
            keep(entity, layers);
        }
    }
}

/// Makes a camera one of the roofs' cameras, drawing the roofs' layer alone, and the meshes on the
/// layers `mask` names, the default layer for zero, the ones it draws there.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_roof_camera(camera: u64, mask: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, mask);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let camera = crate::ecs::entity_from(camera);
                let Ok(mut entity) = world.get_entity_mut(camera) else {
                    return status::NO_ENTITY;
                };
                entity.insert(bevy::camera::visibility::RenderLayers::layer(ROOF_LAYER));
                world.resource_mut::<RoofCameras>().0.insert(camera, mask);
                status::OK
            })
        }
    })
}

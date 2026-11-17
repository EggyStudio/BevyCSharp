//! A camera's exposure where Bevy's falls short, a glTF file's emission and auto exposure turned
//! off.
//!
//! glTF's emissive color times `KHR_materials_emissive_strength` is a luminance in nits, and
//! Bevy's glTF loader makes it a standard material's emissive with Bevy's
//! `emissive_exposure_weight` of 0, which shows it as written, unexposed. Bevy's own exposure of EV
//! 9.7 takes every other light down about a thousand times, so an emission shown as written stands
//! a thousand times as bright beside them, in the picture and in global illumination's cascades,
//! which take a material's emission as the picture shows it (`scene_field`). Here each material a
//! glTF file brings is made again after Bevy's loader made it, with a weight of 1, so its emission
//! takes the camera's exposure, drawn and in the cascades alike. A material made in code keeps the
//! weight its maker gives it.
//!
//! Bevy 0.20 forgets a camera's auto exposure by the camera's own entity where it keeps the pass
//! by the render world's, so once the effect is turned off its pass, kept on the camera's view in
//! the render world, goes on adjusting the picture, brighter and brighter in a dark room. Here the
//! pass's component is taken off every view without the effect, after Bevy has queued
//! it for the views that do.

#![cfg(feature = "render")]

use bevy::asset::{Handle, LoadContext};
use bevy::ecs::component::ComponentId;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::Local;
use bevy::ecs::world::World;
use bevy::gltf::extensions::{ErasedGltfExtensionHandler, GltfExtensionHandler, GltfExtensionHandlers};
use bevy::gltf::{gltf, GltfMaterial};
use bevy::pbr::gltf::standard_material_from_gltf_material;
use bevy::post_process::auto_exposure::AutoExposure;
use bevy::render::view::ExtractedView;
use bevy::render::{Render, RenderApp, RenderSystems};

/// Gives a glTF file's materials their emission exposed, and takes the pass off a view whose camera
/// has no auto exposure, each frame.
pub fn install(app: &mut bevy::app::App) {
    // After Bevy's own handler, which `PbrPlugin` put in the list as it was built, so each
    // material made here takes the place of Bevy's under the same label.
    if let Some(handlers) = app.world().get_resource::<GltfExtensionHandlers>() {
        handlers.0.write_blocking().push(Box::new(ExposedEmission));
    }

    if let Some(renderer) = app.get_sub_app_mut(RenderApp) {
        renderer.add_systems(Render, forget.after(RenderSystems::Queue).before(RenderSystems::Render));
    }
}

/// Makes each material of a glTF file again as Bevy's loader made it, its emission exposed.
#[derive(Default, Clone)]
struct ExposedEmission;

impl GltfExtensionHandler for ExposedEmission {
    fn dyn_clone(&self) -> Box<dyn ErasedGltfExtensionHandler> {
        Box::new(self.clone())
    }

    fn on_material(
        &mut self,
        load_context: &mut LoadContext<'_>,
        _gltf_material: &gltf::Material,
        _material: Handle<GltfMaterial>,
        material_asset: &GltfMaterial,
        material_label: &str,
    ) {
        // Bevy's handler gave the standard material this label, and a scene's meshes are given its
        // handle by the label, so the one added under it here is the one they draw with.
        let mut standard = standard_material_from_gltf_material(material_asset);
        standard.emissive_exposure_weight = 1.0;
        load_context.add_labeled_asset(format!("{material_label}/std"), standard);
    }
}

/// The component Bevy keeps its pass under on a view, found by its name, since Bevy keeps the
/// type private, and looked for until a camera has had the effect.
fn pass_component(world: &World) -> Option<ComponentId> {
    world
        .components()
        .iter_registered()
        .find(|(_, info)| info.name().to_string().ends_with("::ViewAutoExposurePipeline"))
        .map(|(id, _)| id)
}

fn forget(world: &mut World, mut pass: Local<Option<ComponentId>>) {
    if pass.is_none() {
        *pass = pass_component(world);
    }
    let Some(pass) = *pass else { return };

    let stale: Vec<Entity> = world
        .query_filtered::<Entity, (With<ExtractedView>, Without<AutoExposure>)>()
        .iter(world)
        .filter(|&view| world.entity(view).contains_id(pass))
        .collect();

    for view in stale {
        world.entity_mut(view).remove_by_id(pass);
    }
}

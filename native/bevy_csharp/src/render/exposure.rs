//! Auto exposure turned off on a camera, so the pass that adjusted the picture stops.
//!
//! Bevy 0.20 forgets a camera's auto exposure by the camera's own entity where it keeps the pass
//! by the render world's, so once the effect is turned off its pass, kept on the camera's view in
//! the render world, goes on adjusting the picture, brighter and brighter in a dark room. Here the
//! pass's component is taken off every view that no longer has the effect, after Bevy has queued
//! it for the views that do.

#![cfg(feature = "render")]

use bevy::ecs::component::ComponentId;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::Local;
use bevy::ecs::world::World;
use bevy::post_process::auto_exposure::AutoExposure;
use bevy::render::view::ExtractedView;
use bevy::render::{Render, RenderApp, RenderSystems};

/// Takes the pass off a view whose camera has no auto exposure, each frame.
pub fn install(app: &mut bevy::app::App) {
    if let Some(renderer) = app.get_sub_app_mut(RenderApp) {
        renderer.add_systems(Render, forget.after(RenderSystems::Queue).before(RenderSystems::Render));
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

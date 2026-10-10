//! What a camera owns for the shaders that run on it: images kept from frame to frame, compute run
//! at a point in its frame, and the inputs every pass and every such dispatch reads.
//!
//! Screen-space techniques (ambient occlusion, global illumination, reflections, anything temporal)
//! are a chain of compute and full-screen work over one camera's picture, reading what the camera
//! drew and what the chain itself left behind last frame. This is the camera's side of that chain,
//! so a technique is written as shaders and a list of images rather than as a system that finds
//! cameras, keeps textures per camera and works out when to run.
//!
//! **Images a camera owns.** A camera is given images by name, format and scale of its picture. The
//! engine makes them, makes them again when the picture changes size (which starts them over from
//! zeros), and binds them wherever a shader running on that camera declares the name. One marked as
//! history is two images that trade places every frame, so `name` is this frame's and
//! `name_previous` holds what `name` held last frame. One made with more than one mip level is also
//! reachable a level at a time as `name_mip0`, `name_mip1` and so on, which is how a depth pyramid
//! is built one level from the last, reading one level while writing the next.
//!
//! **Compute on a camera.** A dispatch attached to a camera runs every frame at one of four points:
//! after the prepass, after opaque geometry and before transparent, before tonemapping, or after
//! it. Its workgroups are either counted from the picture's size, a fixed number, or read from a
//! buffer another dispatch wrote, so a shader can decide on the GPU how much work follows.
//!
//! **The inputs.** Every pass and every dispatch on a camera reads the same second group, which
//! `bcs_pass` declares:
//!
//! | binding | holds |
//! |---|---|
//! | 0, 1 | the picture so far, and a linear sampler clamped at its edges |
//! | 2 | Bevy's globals, which is where time is |
//! | 3 | the view uniform, with the camera's matrices and viewport |
//! | 4 | the camera's depth, as a depth texture |
//! | 5 | the camera's normals |
//! | 6 | the camera's motion vectors, in UV units per frame |
//! | 7 | the previous frame's view matrices |
//! | 8 | the camera's lights: every directional light with its shadow cascades, and the ambient light |
//! | 9 | every point and spot light |
//! | 10 | the directional lights' shadow maps, a layer per cascade |
//! | 11 | the point lights' shadow maps, a cube per light |
//! | 12 | the comparison sampler shadow maps are read with |
//! | 13 | how many point and spot lights binding nine holds |
//!
//! The lights are Bevy's own, laid out as Bevy lays them out, so a shader running on a camera
//! lights and shadows what it finds the way Bevy's materials do. Global illumination shading a
//! ray's hit needs that, and could otherwise only get it by drawing the scene's lights again
//! itself.
//!
//! Depth, normals and motion come from the prepass, which a camera draws only when asked. What a
//! camera does not draw, or draws multisampled (which a plain texture binding cannot take), is a
//! stand-in: depth zero, which is the far plane in Bevy's reversed depth, white normals and zero
//! motion. A camera with no previous view data, which is every 2D camera, reads this frame's.

#![cfg(feature = "render")]

mod dispatches;
mod draw_shadows;
mod draws;
mod images;
mod inputs;
mod mesh_draws;

pub use dispatches::{BcsViewDispatches, FramePoint, PreparedViewDispatches, ViewDispatch, Workgroups};
pub use draws::{BcsViewDraws, DrawBlend, DrawCount, PreparedViewDraws, ViewDraw};
pub use mesh_draws::supported as mesh_shaders_supported;
pub use images::{BcsViewImages, ViewImageSpec, ViewImageTextures, scaled, view_names};
pub use inputs::{
    SceneLights, ViewEnvironment, ViewEnvironmentTextures, ViewInputSources, ViewInputs, ViewLights,
};

use dispatches::{ViewComputePipelines, forget_view_dispatches, prepare_view_dispatches, run_view_dispatches};
use draw_shadows::{run_shared_draw_shadows, run_view_draw_shadows};
use draws::{
    ViewDrawPipelines, copy_prepass_depth, forget_view_draws, prepare_view_draws, run_view_draws,
};
use images::{
    PictureCopyPipelines, copy_pictures, forget_view_images, level_view, prepare_picture_copies,
    prepare_view_images,
};
use inputs::{extract_view_environments, init_inputs, prepare_blue_noise, prepare_view_environments};

use bevy::app::App;
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems};
use bevy::core_pipeline::core_3d::{main_opaque_pass_3d, main_transparent_pass_3d};
use bevy::core_pipeline::deferred::node::late_deferred_prepass;
use bevy::core_pipeline::tonemapping::tonemapping;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::pbr::deferred::deferred_lighting;
use bevy::render::{Render, RenderApp, RenderStartup, RenderSystems};
use bevy::render::extract_component::ExtractComponentPlugin;
use bevy::render::renderer::{RenderContext, ViewQuery};

/// Clears to zero every image of a view that asked to start each frame empty, every mip level of
/// it, before anything on the camera runs.
fn clear_view_images(view: ViewQuery<&ViewImageTextures>, mut ctx: RenderContext) {
    use bevy::render::render_resource::{
        LoadOp, Operations, RenderPassColorAttachment, RenderPassDescriptor, StoreOp,
    };

    for slot in view.into_inner().slots.iter().filter(|slot| slot.spec.clear) {
        let texture = &slot.textures[slot.current];

        for mip in 0..texture.mip_level_count() {
            let level = level_view(texture, mip);

            ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
                label: Some("bcs_view_image_clear"),
                color_attachments: &[Some(RenderPassColorAttachment {
                    view: &level,
                    depth_slice: None,
                    resolve_target: None,
                    ops: Operations {
                        load: LoadOp::Clear(Default::default()),
                        store: StoreOp::Store,
                    },
                })],
                depth_stencil_attachment: None,
                timestamp_writes: None,
                occlusion_query_set: None,
                multiview_mask: None,
            });
        }
    }
}

/// Adds what gives cameras their images, their dispatches and the inputs their shaders read.
pub fn install(app: &mut App) {
    app.add_plugins((
        ExtractComponentPlugin::<BcsViewImages>::default(),
        ExtractComponentPlugin::<BcsViewDispatches>::default(),
        ExtractComponentPlugin::<BcsViewDraws>::default(),
    ));

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<ViewComputePipelines>()
        .init_resource::<PictureCopyPipelines>()
        .init_resource::<ViewDrawPipelines>()
        .add_systems(RenderStartup, init_inputs)
        .add_systems(bevy::render::ExtractSchedule, extract_view_environments)
        .add_systems(
            Render,
            (
                (
                    prepare_view_images,
                    forget_view_images,
                    prepare_picture_copies,
                    prepare_view_environments,
                    prepare_blue_noise,
                )
                    .in_set(RenderSystems::PrepareResources),
                (
                    prepare_view_dispatches,
                    forget_view_dispatches,
                    prepare_view_draws,
                    forget_view_draws,
                )
                    .in_set(RenderSystems::PrepareBindGroups),
            ),
        )
        .add_systems(
            Core3d,
            (
                // Into the shadow maps once Bevy's own casters are in them, and before anything
                // reads them.
                run_view_draw_shadows
                    .after(bevy::pbr::per_view_shadow_pass::<{ bevy::pbr::LATE_SHADOW_PASS }>)
                    .before(Core3dSystems::MainPass),
                run_shared_draw_shadows
                    .after(bevy::pbr::shared_shadow_pass::<{ bevy::pbr::LATE_SHADOW_PASS }>)
                    .before(Core3dSystems::MainPass),
                // Before the first point of the frame, which is inside the prepass, so an image a
                // draw there writes starts the frame empty too.
                clear_view_images
                    .in_set(Core3dSystems::Prepass)
                    .before(run_view_dispatches::<4>)
                    .before(run_view_draws::<4>),
                // Inside the prepass, once Bevy's geometry is in it, and before Bevy copies which
                // pixels its deferred lighting lights out of the buffers, which it does once the
                // prepass's set is done so that what draws there lands in time.
                run_view_dispatches::<4>
                    .in_set(Core3dSystems::Prepass)
                    .after(late_deferred_prepass),
                (run_view_draws::<4>, copy_prepass_depth)
                    .chain()
                    .after(run_view_dispatches::<4>)
                    .in_set(Core3dSystems::Prepass)
                    .after(late_deferred_prepass),
                // At the start of the main pass rather than between it and the prepass, which is
                // where Bevy's own ambient occlusion and shadows run, so a shader here sees them
                // done and can replace what Bevy's lighting is about to read.
                run_view_dispatches::<0>
                    .in_set(Core3dSystems::MainPass)
                    .before(deferred_lighting)
                    .before(main_opaque_pass_3d),
                run_view_dispatches::<1>
                    .after(main_opaque_pass_3d)
                    .before(main_transparent_pass_3d)
                    .in_set(Core3dSystems::MainPass),
                run_view_dispatches::<2>
                    .before(tonemapping)
                    .before(super::passes::BeforeTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                run_view_dispatches::<3>
                    .after(tonemapping)
                    .before(super::passes::AfterTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                // The picture copied in before each point's dispatches, so they read it as the
                // frame reached them. Not after the prepass, where there is no picture yet.
                copy_pictures::<1>
                    .before(run_view_dispatches::<1>)
                    .after(main_opaque_pass_3d)
                    .before(main_transparent_pass_3d)
                    .in_set(Core3dSystems::MainPass),
                copy_pictures::<2>
                    .before(run_view_dispatches::<2>)
                    .before(tonemapping)
                    .before(super::passes::BeforeTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                copy_pictures::<3>
                    .before(run_view_dispatches::<3>)
                    .after(tonemapping)
                    .before(super::passes::AfterTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                // Each point's draws after its dispatches, so what a dispatch wrote (the counts
                // of an indirect draw, a buffer of positions) is there to draw.
                run_view_draws::<0>
                    .after(run_view_dispatches::<0>)
                    .in_set(Core3dSystems::MainPass)
                    .before(deferred_lighting)
                    .before(main_opaque_pass_3d),
                run_view_draws::<1>
                    .after(run_view_dispatches::<1>)
                    .before(super::passes::AfterOpaquePasses)
                    .before(main_transparent_pass_3d)
                    .in_set(Core3dSystems::MainPass),
                run_view_draws::<2>
                    .after(run_view_dispatches::<2>)
                    .before(tonemapping)
                    .before(super::passes::BeforeTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                run_view_draws::<3>
                    .after(run_view_dispatches::<3>)
                    .after(tonemapping)
                    .before(super::passes::AfterTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
            ),
        )
        .add_systems(
            Core2d,
            (
                run_view_dispatches::<4>
                    .after(Core2dSystems::Prepass)
                    .before(run_view_dispatches::<0>),
                run_view_dispatches::<0>
                    .after(Core2dSystems::Prepass)
                    .before(Core2dSystems::MainPass),
                run_view_dispatches::<1>
                    .after(Core2dSystems::MainPass)
                    .before(Core2dSystems::EarlyPostProcess),
                run_view_dispatches::<2>
                    .before(tonemapping)
                    .before(super::passes::BeforeTonemappingPasses)
                    .in_set(Core2dSystems::PostProcess),
                run_view_dispatches::<3>
                    .after(tonemapping)
                    .before(super::passes::AfterTonemappingPasses)
                    .in_set(Core2dSystems::PostProcess),
            ),
        );
}

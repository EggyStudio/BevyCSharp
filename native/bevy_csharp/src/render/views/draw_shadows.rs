//! Geometry a camera draws out of buffers, drawn again into the shadow maps of the lights that
//! cast shadows, so it shadows what they light.

use bevy::core_pipeline::prepass::{
    PreviousViewUniformOffset, PreviousViewUniforms, ViewPrepassTextures,
};
use bevy::ecs::system::{Query, Res};
use bevy::render::globals::GlobalsBuffer;
use bevy::render::render_resource::PipelineCache;
use bevy::render::renderer::{RenderContext, ViewQuery};
use bevy::render::texture::FallbackImage;
use bevy::render::view::{ViewUniformOffset, ViewUniforms};

use super::draws::PreparedViewDraws;
use super::{SceneLights, ViewEnvironmentTextures, ViewInputSources, ViewInputs, ViewLights};

/// Draws every draw that casts shadows into each of the camera's directional shadow cascades,
/// depth alone, once Bevy has drawn its own casters there.
///
/// Each cascade is a view with its own matrices, so the draw's vertex shader, reading the view as
/// it always does, places its geometry as that cascade sees it. The shadow maps it writes are among
/// the camera's inputs, so its inputs bind the stand-ins for lights and shadows instead.
#[allow(clippy::too_many_arguments)]
pub(super) fn run_view_draw_shadows(
    view: ViewQuery<(
        &PreparedViewDraws,
        Option<&bevy::pbr::ViewLightEntities>,
        Option<&ViewPrepassTextures>,
        Option<&PreviousViewUniformOffset>,
        Option<&ViewEnvironmentTextures>,
    )>,
    light_views: Query<(&bevy::pbr::ShadowView, &ViewUniformOffset)>,
    inputs: Res<ViewInputs>,
    fallback: Res<FallbackImage>,
    cache: Res<PipelineCache>,
    globals: Res<GlobalsBuffer>,
    view_uniforms: Res<ViewUniforms>,
    previous_uniforms: Option<Res<PreviousViewUniforms>>,
    scene: SceneLights,
    mut ctx: RenderContext,
) {
    use bevy::render::render_resource::{RenderPassDescriptor, StoreOp};

    let (prepared, lights, prepass, previous, environment) = view.into_inner();

    if !prepared.0.iter().any(|draw| draw.shadow_pipeline.is_some()) {
        return;
    }

    let (Some(lights), Some(globals), Some(view_binding)) =
        (lights, globals.buffer.binding(), view_uniforms.uniforms.binding())
    else {
        return;
    };

    let sources = ViewInputSources::gather(
        &inputs,
        &fallback,
        prepass,
        previous_uniforms.as_deref().zip(previous),
        environment,
    );

    for light in &lights.lights {
        let Ok((shadow, offset)) = light_views.get(*light) else {
            continue;
        };

        let Some((group, offsets)) = sources.bind(
            ctx.render_device(),
            &cache,
            &inputs,
            &fallback.d2.texture_view,
            globals.clone(),
            view_binding.clone(),
            offset.offset,
            &scene,
            &ViewLights {
                offset: None,
                shadows: None,
            },
        ) else {
            continue;
        };

        let mut pass = ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
            label: Some("bcs_view_draw_shadow"),
            color_attachments: &[],
            depth_stencil_attachment: Some(shadow.depth_attachment.get_attachment(StoreOp::Store)),
            timestamp_writes: None,
            occlusion_query_set: None,
            multiview_mask: None,
        });

        for draw in &prepared.0 {
            let Some(pipeline) = draw.shadow_pipeline.and_then(|id| cache.get_render_pipeline(id)) else {
                continue;
            };

            pass.set_pipeline(pipeline);
            pass.set_bind_group(0, &draw.own, &[]);
            pass.set_bind_group(1, &group, &offsets);

            draw.count.draw(&mut pass);
        }
    }
}

/// Draws every camera's shadow-casting draws into a point or spot light's shadow map, which is a
/// view of its own that Bevy runs this schedule for, shared by every camera.
///
/// A point light's map is six views, one a face of its cube, and a spot light's is one; each runs
/// this once, with its own matrices in the view the draw's vertex shader reads.
#[allow(clippy::too_many_arguments)]
pub(super) fn run_shared_draw_shadows(
    view: ViewQuery<(&bevy::pbr::ShadowView, &ViewUniformOffset)>,
    cameras: Query<&PreparedViewDraws>,
    inputs: Res<ViewInputs>,
    fallback: Res<FallbackImage>,
    cache: Res<PipelineCache>,
    globals: Res<GlobalsBuffer>,
    view_uniforms: Res<ViewUniforms>,
    scene: SceneLights,
    mut ctx: RenderContext,
) {
    use bevy::render::render_resource::{RenderPassDescriptor, StoreOp};

    let (shadow, offset) = view.into_inner();

    if !cameras.iter().flat_map(|prepared| &prepared.0).any(|draw| draw.shadow_pipeline.is_some()) {
        return;
    }

    let (Some(globals), Some(view_binding)) = (globals.buffer.binding(), view_uniforms.uniforms.binding()) else {
        return;
    };

    let sources = ViewInputSources::gather(&inputs, &fallback, None, None, None);

    let Some((group, offsets)) = sources.bind(
        ctx.render_device(),
        &cache,
        &inputs,
        &fallback.d2.texture_view,
        globals,
        view_binding,
        offset.offset,
        &scene,
        &ViewLights {
            offset: None,
            shadows: None,
        },
    ) else {
        return;
    };

    let mut pass = ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
        label: Some("bcs_view_draw_shared_shadow"),
        color_attachments: &[],
        depth_stencil_attachment: Some(shadow.depth_attachment.get_attachment(StoreOp::Store)),
        timestamp_writes: None,
        occlusion_query_set: None,
        multiview_mask: None,
    });

    for draw in cameras.iter().flat_map(|prepared| &prepared.0) {
        let Some(pipeline) = draw.shadow_pipeline.and_then(|id| cache.get_render_pipeline(id)) else {
            continue;
        };

        pass.set_pipeline(pipeline);
        pass.set_bind_group(0, &draw.own, &[]);
        pass.set_bind_group(1, &group, &offsets);

        draw.count.draw(&mut pass);
    }
}

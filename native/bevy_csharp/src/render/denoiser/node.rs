//! The denoiser's node in the render world, with the textures each view keeps between frames, the
//! pipelines of the five stages, and the system that dispatches them after the main opaque pass.
//!
//! Solari's own system is private to its crate, so the node is ordered after the main opaque pass,
//! which comes after Solari, and before the transparent pass, which draws over the lit picture. The
//! main opaque pass draws only forward materials when Solari runs, every one of Bevy's being
//! deferred, and the shader leaves a pixel with no G-buffer texel as it is, so what it drew is not
//! filtered. Bevy's transmissive pass is private to its crate as well, so the node's order against
//! it is Bevy's to choose, and a transmissive material, which Solari does not light, may be
//! filtered with the surface behind it.

use std::sync::atomic::{AtomicU32, Ordering};

use bevy::app::App;
use bevy::asset::{Assets, Handle};
use bevy::core_pipeline::core_3d::{main_opaque_pass_3d, main_transparent_pass_3d};
use bevy::core_pipeline::prepass::{PreviousViewData, PreviousViewUniformOffset, PreviousViewUniforms, ViewPrepassTextures};
use bevy::core_pipeline::schedule::{Core3d, Core3dSystems};
use bevy::diagnostic::FrameCount;
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res};
use bevy::math::UVec2;
use bevy::render::camera::{ExtractedCamera, TemporalJitter};
use bevy::render::diagnostic::RecordDiagnostics as _;
use bevy::render::render_resource::binding_types::{texture_2d, texture_depth_2d, texture_storage_2d, uniform_buffer, uniform_buffer_sized};
use bevy::render::render_resource::{
    BindGroupEntries, BindGroupLayoutDescriptor, BindGroupLayoutEntries, BufferInitDescriptor, BufferUsages,
    CachedComputePipelineId, ComputePassDescriptor, ComputePipelineDescriptor, Extent3d, PipelineCache, ShaderStages,
    StorageTextureAccess, Texture, TextureDescriptor, TextureDimension, TextureFormat, TextureSampleType, TextureUsages,
    TextureView, TextureViewDescriptor,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::view::{ExtractedView, ViewTarget};
use bevy::render::{Render, RenderApp, RenderStartup, RenderSystems};
use bevy::shader::Shader;
use bevy::solari::realtime::SolariLighting;

use super::{ALBEDO_FLOOR, NORMAL_THRESHOLD, PLANE_DISTANCE, PRESENT, STEPS, VARIANCE_FRAMES};

/// The shader, made in the main world where shader assets live.
#[derive(Resource, Clone)]
struct DenoiserShader(Handle<Shader>);

/// The layouts of the frame's group and of each stage's, and the stages' pipelines.
#[derive(Resource)]
struct DenoiserPipelines {
    frame: BindGroupLayoutDescriptor,
    demodulate: (BindGroupLayoutDescriptor, CachedComputePipelineId),
    temporal: (BindGroupLayoutDescriptor, CachedComputePipelineId),
    variance: (BindGroupLayoutDescriptor, CachedComputePipelineId),
    atrous: (BindGroupLayoutDescriptor, CachedComputePipelineId),
    remodulate: (BindGroupLayoutDescriptor, CachedComputePipelineId),
}

/// A texture the stages write and read, with the view they bind.
struct Plane {
    _texture: Texture,
    view: TextureView,
}

/// What a view keeps between frames, the history and its moments, and what a frame writes on its
/// way through the stages, all the size of the view.
#[derive(Component)]
struct DenoiserTextures {
    size: UVec2,
    demodulated: Plane,
    accumulated: Plane,
    history: Plane,
    // Written and read by turns, since the temporal stage reads the previous frame's while it
    // writes this frame's, wherever the motion vectors take it.
    moments: [Plane; 2],
    // The à-trous passes' light and variance, ping-ponged.
    filtered: [Plane; 2],
    // How many frames the node has run for the view, whose parity says which moments are this
    // frame's, and the frame it last ran in, which a frame after a gap does not take history from.
    runs: AtomicU32,
    last_frame: AtomicU32,
}

/// The uniform the shader's stages share, laid out as the shader's `Params`.
#[repr(C)]
#[derive(Clone, Copy, bytemuck::Pod, bytemuck::Zeroable)]
struct Params {
    world_from_clip: [f32; 16],
    view_from_clip: [f32; 16],
    size: [f32; 2],
    exposure: f32,
    has_history: f32,
    history_limit: f32,
    sigma_depth: f32,
    sigma_normal: f32,
    sigma_luminance: f32,
    albedo_floor: f32,
    plane_distance: f32,
    normal_threshold: f32,
    variance_frames: f32,
}

/// Adds the denoiser's node over Solari, called where Solari was added.
pub fn install(app: &mut App) {
    let Some(mut shaders) = app.world_mut().get_resource_mut::<Assets<Shader>>() else {
        return;
    };

    let shader = shaders.add(Shader::from_wgsl(include_str!("../denoiser.wgsl"), "bcs_denoiser.wgsl"));

    // Kept in the main world as well, so the shader asset lives as long as the app.
    app.insert_resource(DenoiserShader(shader.clone()));

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .insert_resource(DenoiserShader(shader))
        .add_systems(RenderStartup, init_pipelines)
        .add_systems(Render, prepare_textures.in_set(RenderSystems::PrepareResources))
        .add_systems(
            Core3d,
            denoise
                .after(main_opaque_pass_3d)
                .before(main_transparent_pass_3d)
                .in_set(Core3dSystems::MainPass),
        );

    PRESENT.store(true, Ordering::Relaxed);
}

fn init_pipelines(mut commands: Commands, cache: Res<PipelineCache>, shader: Res<DenoiserShader>) {
    let float = || texture_2d(TextureSampleType::Float { filterable: false });
    let write = |format| texture_storage_2d(format, StorageTextureAccess::WriteOnly);

    let frame = BindGroupLayoutDescriptor::new(
        "bcs_denoiser_frame",
        &BindGroupLayoutEntries::sequential(
            ShaderStages::COMPUTE,
            (
                uniform_buffer_sized(false, None),
                texture_2d(TextureSampleType::Uint),
                texture_depth_2d(),
                float(),
                texture_2d(TextureSampleType::Uint),
                texture_depth_2d(),
                uniform_buffer::<PreviousViewData>(true),
            ),
        ),
    );

    let demodulate = BindGroupLayoutDescriptor::new(
        "bcs_denoiser_demodulate",
        &BindGroupLayoutEntries::with_indices(ShaderStages::COMPUTE, ((0, float()), (1, write(TextureFormat::Rgba16Float)))),
    );
    let temporal = BindGroupLayoutDescriptor::new(
        "bcs_denoiser_temporal",
        &BindGroupLayoutEntries::with_indices(
            ShaderStages::COMPUTE,
            (
                (10, float()),
                (11, float()),
                (12, float()),
                (13, write(TextureFormat::Rgba16Float)),
                (14, write(TextureFormat::Rgba32Float)),
            ),
        ),
    );
    let variance = BindGroupLayoutDescriptor::new(
        "bcs_denoiser_variance",
        &BindGroupLayoutEntries::with_indices(
            ShaderStages::COMPUTE,
            ((20, float()), (21, float()), (22, float()), (23, write(TextureFormat::Rgba32Float))),
        ),
    );
    let atrous = BindGroupLayoutDescriptor::new(
        "bcs_denoiser_atrous",
        &BindGroupLayoutEntries::with_indices(
            ShaderStages::COMPUTE,
            (
                (30, float()),
                (31, write(TextureFormat::Rgba32Float)),
                (32, write(TextureFormat::Rgba16Float)),
                (33, uniform_buffer_sized(false, None)),
            ),
        ),
    );
    let remodulate = BindGroupLayoutDescriptor::new(
        "bcs_denoiser_remodulate",
        &BindGroupLayoutEntries::with_indices(ShaderStages::COMPUTE, ((40, float()), (41, write(TextureFormat::Rgba16Float)))),
    );

    let queue = |stage: &'static str, layout: &BindGroupLayoutDescriptor| {
        cache.queue_compute_pipeline(ComputePipelineDescriptor {
            label: Some(format!("bcs_denoiser_{stage}").into()),
            layout: vec![frame.clone(), layout.clone()],
            immediate_size: 0,
            shader: shader.0.clone(),
            shader_defs: Vec::new(),
            entry_point: Some(stage.into()),
            zero_initialize_workgroup_memory: false,
            constants: Vec::new(),
        })
    };

    let pipelines = DenoiserPipelines {
        demodulate: (demodulate.clone(), queue("demodulate", &demodulate)),
        temporal: (temporal.clone(), queue("temporal", &temporal)),
        variance: (variance.clone(), queue("variance", &variance)),
        atrous: (atrous.clone(), queue("atrous", &atrous)),
        remodulate: (remodulate.clone(), queue("remodulate", &remodulate)),
        frame,
    };

    commands.insert_resource(pipelines);
}

/// Gives each view Solari lights its textures, made again at a new size with no history.
fn prepare_textures(
    views: Query<(Entity, &ExtractedCamera, Option<&DenoiserTextures>), With<SolariLighting>>,
    device: Res<RenderDevice>,
    mut commands: Commands,
) {
    for (entity, camera, known) in &views {
        let Some(size) = camera.physical_viewport_size else { continue };
        if size.x == 0 || size.y == 0 || known.is_some_and(|known| known.size == size) {
            continue;
        }

        let plane = |label: &'static str, format: TextureFormat| {
            let texture = device.create_texture(&TextureDescriptor {
                label: Some(label),
                size: Extent3d { width: size.x, height: size.y, depth_or_array_layers: 1 },
                mip_level_count: 1,
                sample_count: 1,
                dimension: TextureDimension::D2,
                format,
                usage: TextureUsages::STORAGE_BINDING | TextureUsages::TEXTURE_BINDING,
                view_formats: &[],
            });
            let view = texture.create_view(&TextureViewDescriptor::default());
            Plane { _texture: texture, view }
        };

        commands.entity(entity).insert(DenoiserTextures {
            size,
            demodulated: plane("bcs_denoiser_demodulated", TextureFormat::Rgba16Float),
            accumulated: plane("bcs_denoiser_accumulated", TextureFormat::Rgba16Float),
            history: plane("bcs_denoiser_history", TextureFormat::Rgba16Float),
            moments: [plane("bcs_denoiser_moments_a", TextureFormat::Rgba32Float), plane("bcs_denoiser_moments_b", TextureFormat::Rgba32Float)],
            filtered: [plane("bcs_denoiser_filtered_a", TextureFormat::Rgba32Float), plane("bcs_denoiser_filtered_b", TextureFormat::Rgba32Float)],
            runs: AtomicU32::new(0),
            last_frame: AtomicU32::new(u32::MAX),
        });
    }
}

/// Runs the five stages over a view Solari lit, where the denoiser is on.
#[allow(clippy::too_many_arguments)]
fn denoise(
    view: ViewQuery<
        (
            &ViewTarget,
            &ViewPrepassTextures,
            &ExtractedView,
            &ExtractedCamera,
            &PreviousViewUniformOffset,
            &DenoiserTextures,
            Option<&TemporalJitter>,
        ),
        With<SolariLighting>,
    >,
    pipelines: Option<Res<DenoiserPipelines>>,
    cache: Res<PipelineCache>,
    previous_views: Res<PreviousViewUniforms>,
    frame_count: Res<FrameCount>,
    mut ctx: RenderContext,
) {
    if !super::enabled() {
        return;
    }

    let (target, prepass, extracted, camera, previous_offset, textures, jitter) = view.into_inner();
    let Some(pipelines) = pipelines else { return };

    let (
        Some(demodulate),
        Some(temporal),
        Some(variance),
        Some(atrous),
        Some(remodulate),
        Some(gbuffer),
        Some(depth),
        Some(motion),
        Some(previous_view),
    ) = (
        cache.get_compute_pipeline(pipelines.demodulate.1),
        cache.get_compute_pipeline(pipelines.temporal.1),
        cache.get_compute_pipeline(pipelines.variance.1),
        cache.get_compute_pipeline(pipelines.atrous.1),
        cache.get_compute_pipeline(pipelines.remodulate.1),
        prepass.deferred_view(),
        prepass.depth_only_view(),
        prepass.motion_vectors_view(),
        previous_views.uniforms.binding(),
    )
    else {
        return;
    };

    // The previous frame's depth and G-buffer, which Solari double-buffers where ReSTIR is on, and
    // the history taken only where the node ran the frame before, so a history left while the
    // denoiser was off or the view was elsewhere is not reprojected.
    let previous = prepass.previous_deferred_view().zip(prepass.previous_depth_only_view());
    let frame = frame_count.0;
    let ran_last = textures.last_frame.swap(frame, Ordering::Relaxed) == frame.wrapping_sub(1);
    let has_history = previous.is_some() && ran_last;
    let (previous_gbuffer, previous_depth) = previous.unwrap_or((gbuffer, depth));

    let parity = (textures.runs.fetch_add(1, Ordering::Relaxed) & 1) as usize;
    let (moments_before, moments_now) = (&textures.moments[parity ^ 1], &textures.moments[parity]);

    // The matrices as Bevy's view uniform makes them, the projection jittered where the view is.
    let size = textures.size;
    let mut clip_from_view = extracted.clip_from_view;
    if let Some(jitter) = jitter {
        jitter.jitter_projection(&mut clip_from_view, size.as_vec2());
    }
    let view_from_clip = clip_from_view.inverse();
    let world_from_clip = extracted.world_from_view.to_matrix() * view_from_clip;

    let settings = super::settings();
    let params = Params {
        world_from_clip: world_from_clip.to_cols_array(),
        view_from_clip: view_from_clip.to_cols_array(),
        size: [size.x as f32, size.y as f32],
        exposure: camera.exposure,
        has_history: if has_history { 1.0 } else { 0.0 },
        history_limit: settings.history_limit.max(1.0),
        sigma_depth: settings.sigma_depth,
        sigma_normal: settings.sigma_normal,
        sigma_luminance: settings.sigma_luminance,
        albedo_floor: ALBEDO_FLOOR,
        plane_distance: PLANE_DISTANCE,
        normal_threshold: NORMAL_THRESHOLD,
        variance_frames: VARIANCE_FRAMES,
    };

    let device = ctx.render_device().clone();
    let uniform = |label: &'static str, contents: &[u8]| {
        device.create_buffer_with_data(&BufferInitDescriptor { label: Some(label), contents, usage: BufferUsages::UNIFORM })
    };
    let params = uniform("bcs_denoiser_params", bytemuck::bytes_of(&params));
    let steps: Vec<_> = STEPS
        .iter()
        .enumerate()
        .map(|(pass, step)| uniform("bcs_denoiser_step", bytemuck::cast_slice(&[*step, u32::from(pass == 0), 0, 0])))
        .collect();

    let layout = |descriptor: &BindGroupLayoutDescriptor| cache.get_bind_group_layout(descriptor);
    let frame_group = device.create_bind_group(
        "bcs_denoiser_frame",
        &layout(&pipelines.frame),
        &BindGroupEntries::sequential((
            params.as_entire_binding(),
            gbuffer,
            depth,
            motion,
            previous_gbuffer,
            previous_depth,
            previous_view,
        )),
    );

    let demodulate_group = device.create_bind_group(
        "bcs_denoiser_demodulate",
        &layout(&pipelines.demodulate.0),
        &BindGroupEntries::with_indices(((0, target.main_texture_view()), (1, &textures.demodulated.view))),
    );
    let temporal_group = device.create_bind_group(
        "bcs_denoiser_temporal",
        &layout(&pipelines.temporal.0),
        &BindGroupEntries::with_indices((
            (10, &textures.demodulated.view),
            (11, &textures.history.view),
            (12, &moments_before.view),
            (13, &textures.accumulated.view),
            (14, &moments_now.view),
        )),
    );
    let variance_group = device.create_bind_group(
        "bcs_denoiser_variance",
        &layout(&pipelines.variance.0),
        &BindGroupEntries::with_indices((
            (20, &textures.accumulated.view),
            (21, &moments_now.view),
            (22, &textures.demodulated.view),
            (23, &textures.filtered[0].view),
        )),
    );

    // Pass after pass from one filtered texture into the other, the first starting where the
    // variance stage wrote, so the fifth ends in the second.
    let atrous_groups: Vec<_> = steps
        .iter()
        .enumerate()
        .map(|(pass, step)| {
            device.create_bind_group(
                "bcs_denoiser_atrous",
                &layout(&pipelines.atrous.0),
                &BindGroupEntries::with_indices((
                    (30, &textures.filtered[pass % 2].view),
                    (31, &textures.filtered[(pass + 1) % 2].view),
                    (32, &textures.history.view),
                    (33, step.as_entire_binding()),
                )),
            )
        })
        .collect();
    let remodulate_group = device.create_bind_group(
        "bcs_denoiser_remodulate",
        &layout(&pipelines.remodulate.0),
        &BindGroupEntries::with_indices(((40, &textures.filtered[STEPS.len() % 2].view), (41, target.main_texture_view()))),
    );

    let diagnostics = ctx.diagnostic_recorder();
    let diagnostics = diagnostics.as_deref();
    let mut pass = ctx.command_encoder().begin_compute_pass(&ComputePassDescriptor {
        label: Some("bcs_denoiser"),
        timestamp_writes: None,
    });
    let span = diagnostics.time_span(&mut pass, "bcs_denoiser");

    let (x, y) = (size.x.div_ceil(8), size.y.div_ceil(8));
    pass.set_bind_group(0, &frame_group, &[previous_offset.offset]);

    for (pipeline, group) in [(demodulate, &demodulate_group), (temporal, &temporal_group), (variance, &variance_group)] {
        pass.set_pipeline(pipeline);
        pass.set_bind_group(1, group, &[]);
        pass.dispatch_workgroups(x, y, 1);
    }

    pass.set_pipeline(atrous);
    for group in &atrous_groups {
        pass.set_bind_group(1, group, &[]);
        pass.dispatch_workgroups(x, y, 1);
    }

    pass.set_pipeline(remodulate);
    pass.set_bind_group(1, &remodulate_group, &[]);
    pass.dispatch_workgroups(x, y, 1);

    span.end(&mut pass);
}

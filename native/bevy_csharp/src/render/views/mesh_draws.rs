//! Geometry a camera draws with mesh shaders, a program's task and mesh stages writing the
//! vertices and triangles its draw fragment shader colors, with no vertex buffer or vertex shader
//! between, as Bevy's `mesh_shader_intro` draws.
//!
//! Slang writes mesh shaders as SPIR-V and nothing else, so the three stages are compiled to it, and
//! the pipeline is built here rather than queued in Bevy's cache, as a SPIR-V compute pipeline is
//! (see [`crate::render::spirv_compute`]), because a module handed to the driver as it is has to
//! name its entry point, which Bevy's cache does not. Everything else about such a draw, its values,
//! its targets, its blending and its place in the frame, is any draw's on a camera
//! ([`super::draws`]).

use bevy::render::render_resource::{
    BindGroupLayoutDescriptor, ColorTargetState, DepthStencilState, MultisampleState, PipelineCache,
    RenderPipeline,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::settings::WgpuFeatures;

use crate::render::material::say_once;
use crate::render::programs::{PipelineProgram, Role};
use crate::render::spirv_compute;

/// Whether this device runs mesh shaders, which takes Vulkan, DirectX 12 or Metal on hardware that
/// has them.
pub fn supported(device: &RenderDevice) -> bool {
    device.features().contains(WgpuFeatures::EXPERIMENTAL_MESH_SHADER)
}

/// Whether a program draws with mesh shaders, which a mesh stage says.
pub(super) fn draws_meshes(program: &PipelineProgram) -> bool {
    program.stages[Role::DrawMesh as usize].is_some()
}

/// What a mesh pipeline is made with besides its program's stages.
pub(super) struct MeshPipeline<'a> {
    pub groups: &'a [BindGroupLayoutDescriptor],
    pub targets: Vec<Option<ColorTargetState>>,
    pub depth: Option<DepthStencilState>,
    pub samples: u32,
}

/// Builds the pipeline a program's mesh stages draw with, or answers nothing where the device runs
/// no mesh shaders, which it says, or a stage has no SPIR-V of its own, which is how a stage that
/// has never compiled is, its stand-in being no more than a stage of the right kind.
pub(super) fn build(
    device: &RenderDevice,
    cache: &PipelineCache,
    program: &PipelineProgram,
    label: &str,
    made: MeshPipeline,
) -> Option<RenderPipeline> {
    if !supported(device) {
        say_once(format!(
            "Shader program {label} draws with mesh shaders, which this device does not run, so it \
             draws nothing."
        ));
        return None;
    }

    let stage = |role: Role| {
        program.stages[role as usize]
            .as_ref()
            .and_then(|stage| Some((stage.spirv.clone()?, stage.entry.clone())))
    };

    let task = stage(Role::DrawTask);
    let (Some(mesh), Some(fragment)) = (stage(Role::DrawMesh), stage(Role::DrawFragment)) else {
        return None;
    };
    if program.stages[Role::DrawTask as usize].is_some() && task.is_none() {
        return None;
    }

    // No error scope around it, as there is around a SPIR-V compute pipeline. With one, a scope
    // was popped out of the order it was pushed in, on the thread building the mesh pipeline and on
    // Bevy's own threads loading shaders, a few runs in ten, which wgpu answers with a panic that
    // ends the app, and with none, in every run since. An error building it is then a render error
    // as any other, which closes the app unless `Shaders.KeepRenderingAfterErrors` says not to.
    let task_module = task
        .as_ref()
        .map(|(spirv, entry)| spirv_compute::module(device, label, spirv, entry));
    let mesh_module = spirv_compute::module(device, label, &mesh.0, &mesh.1);
    let fragment_module = spirv_compute::module(device, label, &fragment.0, &fragment.1);
    let layout = spirv_compute::pipeline_layout(device, cache, label, made.groups);

    Some(device.create_mesh_pipeline(&wgpu::MeshPipelineDescriptor {
        label: Some(label),
        layout: Some(&layout),
        task: task_module.as_ref().zip(task.as_ref()).map(|(module, (_, entry))| wgpu::TaskState {
            module,
            entry_point: Some(entry.as_ref()),
            compilation_options: Default::default(),
        }),
        mesh: wgpu::MeshState {
            module: &mesh_module,
            entry_point: Some(mesh.1.as_ref()),
            compilation_options: Default::default(),
        },
        primitive: Default::default(),
        depth_stencil: made.depth,
        multisample: MultisampleState {
            count: made.samples,
            ..Default::default()
        },
        fragment: Some(wgpu::FragmentState {
            module: &fragment_module,
            entry_point: Some(fragment.1.as_ref()),
            compilation_options: Default::default(),
            targets: &made.targets,
        }),
        multiview: None,
        cache: None,
    }))
}

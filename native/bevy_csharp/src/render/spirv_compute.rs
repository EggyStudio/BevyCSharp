//! Compute pipelines built from SPIR-V by the bridge itself.
//!
//! Bevy 0.20 hands SPIR-V to the driver through wgpu 30's passthrough without naming its entry
//! point, which wgpu 30 asks a passthrough module for, so a compute pipeline Bevy's cache builds
//! from one finds no entry point and is refused. The bridge builds those itself, the module named
//! with the one entry point slangc wrote, and leaves every other pipeline to Bevy's cache. A device
//! without passthrough reads the SPIR-V through naga as Bevy would.

#![cfg(feature = "render")]

use std::borrow::Cow;

use bevy::render::render_resource::{
    BindGroupLayoutDescriptor, CachedComputePipelineId, ComputePipeline, PipelineCache,
};
use bevy::render::renderer::RenderDevice;

/// A compute pipeline, queued in Bevy's cache or built by the bridge.
#[derive(Clone, Debug)]
pub enum ComputePipelineRef {
    Cached(CachedComputePipelineId),
    Own(ComputePipeline),
}

impl ComputePipelineRef {
    /// The pipeline, once it is built.
    pub fn get<'a>(&'a self, cache: &'a PipelineCache) -> Option<&'a ComputePipeline> {
        match self {
            Self::Cached(id) => cache.get_compute_pipeline(*id),
            Self::Own(pipeline) => Some(pipeline),
        }
    }
}

/// The words of SPIR-V bytes, which slangc writes in the machine's order.
pub fn words(bytes: &[u8]) -> Vec<u32> {
    bytes
        .chunks_exact(4)
        .map(|word| u32::from_le_bytes([word[0], word[1], word[2], word[3]]))
        .collect()
}

/// Builds a compute pipeline from SPIR-V with its groups' layouts.
///
/// With no error scope around it, so an error building it is a render error as any other, which
/// closes the app unless `Shaders.KeepRenderingAfterErrors` says not to. A scope would catch the
/// error, and around a mesh pipeline built the same way (`views::mesh_draws`) a scope was popped
/// out of the order it was pushed in a few runs in ten, on the thread building it or on one of
/// Bevy's loading shaders, which wgpu answers with a panic that ends the app. wgpu keeps a stack of
/// scopes for each thread, and how another scope came above this one's was not found, so neither
/// builder has one.
pub fn build(
    device: &RenderDevice,
    cache: &PipelineCache,
    label: &str,
    groups: &[BindGroupLayoutDescriptor],
    spirv: &[u32],
    entry: &str,
) -> ComputePipeline {
    let module = module(device, label, spirv, entry);
    let layout = pipeline_layout(device, cache, label, groups);

    device.create_compute_pipeline(&wgpu::ComputePipelineDescriptor {
        label: Some(label),
        layout: Some(&layout),
        module: &module,
        entry_point: Some(entry),
        compilation_options: Default::default(),
        cache: None,
    })
}

/// The layout of a pipeline whose groups are `groups`, in order.
pub fn pipeline_layout(
    device: &RenderDevice,
    cache: &PipelineCache,
    label: &str,
    groups: &[BindGroupLayoutDescriptor],
) -> wgpu::PipelineLayout {
    let layouts: Vec<_> = groups.iter().map(|group| cache.get_bind_group_layout(group)).collect();
    let bound: Vec<Option<&wgpu::BindGroupLayout>> = layouts.iter().map(|layout| Some(&**layout)).collect();

    device.wgpu_device().create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
        label: Some(label),
        bind_group_layouts: &bound,
        immediate_size: 0,
    })
}

/// A shader module of SPIR-V slangc wrote with one entry point, handed to the driver as it is
/// where the device takes it so, and otherwise read through naga as Bevy would.
pub fn module(device: &RenderDevice, label: &str, spirv: &[u32], entry: &str) -> wgpu::ShaderModule {
    let wgpu = device.wgpu_device();

    if device.features().contains(wgpu::Features::PASSTHROUGH_SHADERS) {
        // SAFETY: the words are what slangc wrote, handed to the driver as Bevy hands SPIR-V to it,
        // with the entry point named as wgpu asks.
        unsafe {
            wgpu.create_shader_module_passthrough(wgpu::ShaderModuleDescriptorPassthrough {
                label: Some(label),
                entry_points: Cow::Owned(vec![wgpu::PassthroughShaderEntryPoint {
                    name: Cow::Borrowed(entry),
                    // Read by Metal alone, which takes no SPIR-V.
                    workgroup_size: (1, 1, 1),
                }]),
                spirv: Some(Cow::Borrowed(spirv)),
                ..Default::default()
            })
        }
    } else {
        // SAFETY: read through naga with the checks Bevy leaves off its own shaders.
        unsafe {
            wgpu.create_shader_module_trusted(
                wgpu::ShaderModuleDescriptor {
                    label: Some(label),
                    source: wgpu::ShaderSource::SpirV(Cow::Borrowed(spirv)),
                },
                wgpu::ShaderRuntimeChecks::unchecked(),
            )
        }
    }
}

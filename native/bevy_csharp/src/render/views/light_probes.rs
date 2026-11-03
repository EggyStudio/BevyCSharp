//! Bevy's light probes as a shader on a camera reads them, its light probes' uniform and the first
//! of its reflection probes' diffuse cubes and its irradiance volumes' images, bound among the
//! inputs every pass and dispatch on a camera reads, so a pass taking back the light Bevy's
//! lighting gave a surface reads them as Bevy does (`bcs_pass_probes.slang`).

use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::With;
use bevy::ecs::system::{Commands, Query, Res};
use bevy::light::{EnvironmentMapLight, IrradianceVolume};
use bevy::pbr::{LightProbesBuffer, LightProbesUniform, MAX_VIEW_LIGHT_PROBES, RenderViewLightProbes, ViewLightProbesUniformOffset};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroupEntry, BindGroupLayoutEntry, BindingResource, BindingType, Buffer, BufferBinding, BufferBindingType,
    BufferDescriptor, BufferUsages, Extent3d, ShaderType, TextureDescriptor, TextureDimension, TextureFormat,
    TextureSampleType, TextureUsages, TextureView, TextureViewDescriptor, TextureViewDimension,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::settings::WgpuFeatures;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::ExtractedView;

/// The binding of Bevy's light probes' uniform, its reflection probes' diffuse cubes following it
/// and its irradiance volumes' images following them.
const UNIFORM: u32 = 18;

/// How many of a view's reflection probes' diffuse cubes and of its irradiance volumes' images, the
/// first by Bevy's binding index, its inputs hold, each a texture of its own rather than an array,
/// since a group binding an array of images may hold no uniform buffer, and so a camera seeing
/// three reflection probes beside its own map. A probe whose image lies past them keeps its share
/// of the blend, and its light is left as Bevy gave it.
pub(super) const LIGHT_PROBE_IMAGES: usize = 4;

/// Whether the device binds arrays of textures a shader indexes as it goes, which is when Bevy's
/// lighting reads its reflection probes and more than one irradiance volume, as bevy_pbr 0.20's
/// `binding_arrays_are_usable` decides it but for the Adreno models up to 610 it rules out, which
/// run on Android alone.
pub(super) fn light_probe_arrays(device: &RenderDevice) -> bool {
    // Bevy's 16, the textures its standard material binds at least, and the probes after them.
    let most = (16 + MAX_VIEW_LIGHT_PROBES) as u32;
    device.limits().max_binding_array_elements_per_shader_stage >= most
        && device.limits().max_binding_array_sampler_elements_per_shader_stage >= most
        && device.features().contains(
            WgpuFeatures::TEXTURE_BINDING_ARRAY | WgpuFeatures::SAMPLED_TEXTURE_AND_STORAGE_BUFFER_ARRAY_NON_UNIFORM_INDEXING,
        )
}

/// A view's light probes as Bevy's lighting reads them, its reflection probes' diffuse cubes and
/// its irradiance volumes' images in the order Bevy's light probes' uniform indexes them, and where
/// the view's part of that uniform lies, which a pass taking back the light Bevy gave reads.
#[derive(Component)]
pub struct ViewLightProbeTextures {
    diffuse: Vec<TextureView>,
    volumes: Vec<TextureView>,
    offset: Option<u32>,
}

/// Gathers each view's light probes' images out of what Bevy keeps for its own lighting, once Bevy
/// has written its light probes' uniform and the view's place in it.
pub(super) fn prepare_view_light_probes(
    mut commands: Commands,
    images: Res<RenderAssets<GpuImage>>,
    fallback: Res<FallbackImage>,
    views: Query<
        (
            Entity,
            Option<&RenderViewLightProbes<EnvironmentMapLight>>,
            Option<&RenderViewLightProbes<IrradianceVolume>>,
            Option<&ViewLightProbesUniformOffset>,
        ),
        With<ExtractedView>,
    >,
) {
    // As Bevy binds them, a stand-in where an image has not arrived, the first few by binding
    // index.
    let count = LIGHT_PROBE_IMAGES;
    let view = |id, stand_in: &TextureView| images.get(id).map_or_else(|| stand_in.clone(), |image| image.texture_view.clone());

    for (entity, maps, volumes, offset) in &views {
        let mut diffuse: Vec<TextureView> = maps.map_or_else(Vec::new, |maps| {
            maps.binding_index_to_textures.iter().take(count).map(|ids| view(ids.diffuse, &fallback.cube.texture_view)).collect()
        });
        diffuse.resize(count, fallback.cube.texture_view.clone());

        let mut voxels: Vec<TextureView> = volumes.map_or_else(Vec::new, |volumes| {
            volumes.binding_index_to_textures.iter().take(count).map(|&id| view(id, &fallback.d3.texture_view)).collect()
        });
        voxels.resize(count, fallback.d3.texture_view.clone());

        commands.entity(entity).insert(ViewLightProbeTextures {
            diffuse,
            volumes: voxels,
            offset: offset.map(|offset| **offset),
        });
    }
}

/// The layout's entries for Bevy's light probes, its uniform at the view's place in it, then its
/// reflection probes' diffuse cubes, and its irradiance volumes' images after them.
pub(super) fn layout_entries(entry: &dyn Fn(u32, BindingType) -> BindGroupLayoutEntry) -> Vec<BindGroupLayoutEntry> {
    let image = |view_dimension| BindingType::Texture {
        sample_type: TextureSampleType::Float { filterable: true },
        view_dimension,
        multisampled: false,
    };

    let mut entries = vec![entry(
        UNIFORM,
        BindingType::Buffer {
            ty: BufferBindingType::Uniform,
            has_dynamic_offset: false,
            min_binding_size: Some(LightProbesUniform::min_size()),
        },
    )];
    for index in 0..LIGHT_PROBE_IMAGES as u32 {
        entries.push(entry(UNIFORM + 1 + index, image(TextureViewDimension::Cube)));
        entries.push(entry(UNIFORM + 1 + LIGHT_PROBE_IMAGES as u32 + index, image(TextureViewDimension::D3)));
    }
    entries
}

/// What stands in for Bevy's light probes where a camera has none, a uniform of no reflection
/// probe and no irradiance volume, and a volume black everywhere.
pub(super) fn stand_ins(device: &RenderDevice) -> (Buffer, TextureView) {
    let uniform = device.create_buffer(&BufferDescriptor {
        label: Some("bcs_view_empty_light_probes"),
        size: LightProbesUniform::min_size().get(),
        usage: BufferUsages::UNIFORM,
        mapped_at_creation: false,
    });

    let volume = device
        .create_texture(&TextureDescriptor {
            label: Some("bcs_view_empty_volume"),
            size: Extent3d {
                width: 1,
                height: 1,
                depth_or_array_layers: 1,
            },
            mip_level_count: 1,
            sample_count: 1,
            dimension: TextureDimension::D3,
            format: TextureFormat::Rgba16Float,
            usage: TextureUsages::TEXTURE_BINDING,
            view_formats: &[],
        })
        .create_view(&TextureViewDescriptor::default());

    (uniform, volume)
}

/// The group's entries for a view's light probes, Bevy's uniform at the view's place in it and each
/// image the layout holds, a stand-in for each the view has none for.
pub(super) fn entries<'a>(
    probes: Option<&'a ViewLightProbeTextures>,
    buffer: Option<&'a LightProbesBuffer>,
    stand_ins: (&'a Buffer, &'a TextureView, &'a TextureView),
) -> Vec<BindGroupEntry<'a>> {
    let (empty_uniform, empty_cube, empty_volume) = stand_ins;
    let uniform = match (buffer.and_then(|buffer| buffer.buffer()), probes.and_then(|probes| probes.offset)) {
        (Some(buffer), Some(offset)) => BufferBinding {
            buffer,
            offset: u64::from(offset),
            size: Some(LightProbesUniform::min_size()),
        },
        _ => BufferBinding {
            buffer: empty_uniform,
            offset: 0,
            size: Some(LightProbesUniform::min_size()),
        },
    };

    let mut entries = vec![BindGroupEntry {
        binding: UNIFORM,
        resource: BindingResource::Buffer(uniform),
    }];
    for index in 0..LIGHT_PROBE_IMAGES {
        let (diffuse, volume) =
            probes.map_or((empty_cube, empty_volume), |probes| (&probes.diffuse[index], &probes.volumes[index]));
        entries.push(BindGroupEntry {
            binding: UNIFORM + 1 + index as u32,
            resource: BindingResource::TextureView(diffuse),
        });
        entries.push(BindGroupEntry {
            binding: UNIFORM + 1 + (LIGHT_PROBE_IMAGES + index) as u32,
            resource: BindingResource::TextureView(volume),
        });
    }
    entries
}

//! Mipmaps made on the GPU from an image's top level each frame, as Bevy's `MipGenerationJobs` makes
//! them.
//!
//! Bevy downsamples an image's top level into each level below it with a compute pass of its own,
//! for the images placed in its jobs under a phase, where a system of the game's runs that phase.
//! Both are Rust a game here cannot write, so the bridge keeps the set of images whose mipmaps are
//! made, puts each in the jobs as the frame is extracted, and runs its phase once a frame before the
//! cameras draw. Every camera then samples levels made from what the top level holds that frame,
//! whether the game wrote it or a camera drew it the frame before.

use crate::interop::status;

#[cfg(feature = "render")]
use bevy::asset::{AssetId, Assets};
#[cfg(feature = "render")]
use bevy::core_pipeline::mip_generation::{
    DownsamplePipeline, MipGenerationJobs, MipGenerationPhaseId, MipGenerationPipelines, generate_mips_for_phase,
};
#[cfg(feature = "render")]
use bevy::ecs::resource::Resource;
#[cfg(feature = "render")]
use bevy::ecs::system::{Res, ResMut};
#[cfg(feature = "render")]
use bevy::image::Image;
#[cfg(feature = "render")]
use bevy::platform::collections::HashSet;
#[cfg(feature = "render")]
use bevy::render::Extract;
#[cfg(feature = "render")]
use bevy::render::render_asset::RenderAssets;
#[cfg(feature = "render")]
use bevy::render::render_resource::PipelineCache;
#[cfg(feature = "render")]
use bevy::render::renderer::RenderContext;
#[cfg(feature = "render")]
use bevy::render::texture::GpuImage;

/// The phase the bridge's images are downsampled in, a number of its own, "bcmp" in ASCII, so it
/// meets no phase a plugin of Bevy's or of a crate runs.
#[cfg(feature = "render")]
const PHASE: MipGenerationPhaseId = MipGenerationPhaseId(0x6263_6d70);

/// The images whose mipmaps are made each frame.
#[cfg(feature = "render")]
#[derive(Resource, Default)]
struct MipGenerated(HashSet<AssetId<Image>>);

/// How many levels an image of `width` by `height` has from its full size down to one pixel.
#[cfg(feature = "render")]
fn full_mip_count(width: u32, height: u32) -> u32 {
    32 - width.max(height).max(1).leading_zeros()
}

/// Has Bevy make the mipmaps of the image behind `image` from its top level on the GPU each frame
/// while `on` is non-zero, and stop where it is zero, the levels it made last kept.
///
/// Turned on, an image of one level is given every level its size has, those below the top zero
/// until they are made, and is made writable by Bevy's downsampler, as Bevy's own example makes
/// its image. Answers [`status::INVALID_STATE`] for a format the downsampler cannot write, every
/// sRGB format among them, [`status::NO_COMPONENT`] for a key that names no image, and
/// [`status::NOT_PRESENT`] for one still loading.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_mip_generation(image: i32, on: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, on);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::image::TextureFormatPixelInfo;
            use bevy::render::render_resource::TextureUsages;

            crate::state::with_world(|world| {
                let handle = match super::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) | Err(_) => return status::NO_COMPONENT,
                };
                let id = handle.id();

                if on == 0 {
                    if let Some(mut generated) = world.get_resource_mut::<MipGenerated>() {
                        generated.0.remove(&id);
                    }
                    return status::OK;
                }

                let Some(mut images) = world.get_resource_mut::<Assets<Image>>() else {
                    return status::UNSUPPORTED;
                };
                let Some(found) = images.get(&handle) else {
                    return status::NOT_PRESENT;
                };

                let format = found.texture_descriptor.format;
                if !DownsamplePipeline::supports_texture_format(format) {
                    return status::INVALID_STATE;
                }

                // Written through what into_inner hands back, since Bevy marks an asset changed
                // only where it is written, and an image on the GPU already would keep one level.
                let descriptor = &found.texture_descriptor;
                let (width, height) = (descriptor.size.width, descriptor.size.height);
                let levels = full_mip_count(width, height);
                let wants_storage = !descriptor.usage.contains(TextureUsages::STORAGE_BINDING);

                if descriptor.mip_level_count < levels || wants_storage {
                    let Some(image) = images.get_mut(&handle) else {
                        return status::NOT_PRESENT;
                    };
                    let image = image.into_inner();
                    let layers = image.texture_descriptor.size.depth_or_array_layers.max(1) as usize;

                    image.texture_descriptor.mip_level_count = levels;
                    image.texture_descriptor.usage |= TextureUsages::STORAGE_BINDING | TextureUsages::TEXTURE_BINDING;

                    // Bevy hands the GPU every level of every layer from the image's bytes, so the
                    // levels below the top are there as zeros, the top's bytes kept where they are.
                    if let (Some(data), Ok(pixel)) = (image.data.as_mut(), format.pixel_size()) {
                        let size: usize = (0..levels)
                            .map(|level| ((width >> level).max(1) * (height >> level).max(1)) as usize * pixel)
                            .sum();
                        data.resize(size * layers, 0);
                    }
                }

                world.get_resource_or_insert_with(MipGenerated::default).0.insert(id);
                status::OK
            })
        }
    })
}

/// Forgets the images that were dropped, so the set holds only images that are still there.
#[cfg(feature = "render")]
fn forget_dropped(mut generated: ResMut<MipGenerated>, images: Res<Assets<Image>>) {
    generated.0.retain(|id| images.contains(*id));
}

/// Places every image whose mipmaps are made in Bevy's jobs for the bridge's phase.
#[cfg(feature = "render")]
fn extract_jobs(generated: Extract<Option<Res<MipGenerated>>>, jobs: Option<ResMut<MipGenerationJobs>>) {
    let (Some(generated), Some(mut jobs)) = (generated.as_ref(), jobs) else {
        return;
    };

    for id in &generated.0 {
        jobs.add(PHASE, *id);
    }
}

/// Makes the mipmaps of the bridge's phase, once a frame.
#[cfg(feature = "render")]
fn generate(
    jobs: Res<MipGenerationJobs>,
    cache: Res<PipelineCache>,
    pipelines: Option<Res<MipGenerationPipelines>>,
    images: Res<RenderAssets<GpuImage>>,
    mut ctx: RenderContext,
) {
    if let Some(pipelines) = pipelines {
        generate_mips_for_phase(PHASE, &jobs, &cache, &pipelines, &images, &mut ctx);
    }
}

/// Adds what makes mipmaps on the GPU. Called from [`super::material::install`].
#[cfg(feature = "render")]
pub(super) fn install(app: &mut bevy::app::App) {
    use bevy::app::Last;
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::render::renderer::{RenderGraph, RenderGraphSystems};
    use bevy::render::{ExtractSchedule, RenderApp};

    app.init_resource::<MipGenerated>().add_systems(Last, forget_dropped);

    if let Some(render_app) = app.get_sub_app_mut(RenderApp) {
        render_app.add_systems(ExtractSchedule, extract_jobs).add_systems(
            RenderGraph,
            generate
                .in_set(RenderGraphSystems::Render)
                .before(bevy::core_pipeline::schedule::camera_driver),
        );
    }
}

#[cfg(all(test, feature = "render"))]
mod tests {
    /// An image has a level for each halving of its longer side down to one pixel, as Bevy's
    /// example counts them.
    #[test]
    fn an_image_has_a_level_for_each_halving_of_its_longer_side() {
        assert_eq!(super::full_mip_count(640, 480), 10);
        assert_eq!(super::full_mip_count(1920, 1080), 11);
        assert_eq!(super::full_mip_count(1, 1), 1);
        assert_eq!(super::full_mip_count(256, 1), 9);
    }
}

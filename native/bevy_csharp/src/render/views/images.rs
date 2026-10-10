//! Images a camera owns, kept from frame to frame, and the copies of its picture into them.

use std::collections::HashMap;

use bevy::camera::Camera;
use bevy::core_pipeline::prepass::ViewPrepassTextures;
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::pbr::ScreenSpaceAmbientOcclusionResources;
use bevy::render::extract_component::ExtractComponent;
use bevy::render::render_resource::{
    Extent3d, PipelineCache, Texture, TextureDescriptor, TextureDimension, TextureFormat,
    TextureUsages, TextureView, TextureViewDescriptor,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::view::ViewTarget;

use crate::render::values::ViewTexture;
use super::FramePoint;

/// One image a camera was asked to own.
#[derive(Clone, PartialEq, Debug)]
pub struct ViewImageSpec {
    pub name: String,
    pub format: TextureFormat,
    /// A fraction of the picture's size, one for the same size and a half for half of it.
    pub scale: f32,
    /// Two images that trade places every frame, so last frame's is readable as `name_previous`.
    pub history: bool,
    pub mips: u32,
    /// Filled from the picture at this point of every frame, which with history is how last
    /// frame's lit picture is kept.
    pub copy: Option<FramePoint>,
    /// Cleared to zero at the start of every frame, for an image draws accumulate into, a
    /// visibility buffer's "nothing here" among them.
    pub clear: bool,
}

/// The images a camera owns.
#[derive(Component, Clone, ExtractComponent)]
#[extract_app(bevy::render::RenderApp)]
#[extract_component_filter(With<Camera>)]
pub struct BcsViewImages(pub Vec<ViewImageSpec>);

/// One of a view's images, as the render world keeps it.
pub(super) struct ViewImageSlot {
    pub(super) spec: ViewImageSpec,
    pub(super) size: (u32, u32),
    /// One texture, or two for history.
    pub(super) textures: Vec<Texture>,
    /// Which of the two is this frame's.
    pub(super) current: usize,
}

/// A view's images, kept from frame to frame.
#[derive(Component, Default)]
pub struct ViewImageTextures {
    pub(super) slots: Vec<ViewImageSlot>,
    /// Every name a shader on this view can read an image by, this frame.
    pub names: HashMap<String, ViewTexture>,
}

/// The size of an image `scale` of a picture `width` by `height`, which is never nothing.
pub fn scaled(width: u32, height: u32, scale: f32) -> (u32, u32) {
    let at = |length: u32| ((length as f32 * scale).ceil() as u32).max(1);
    (at(width), at(height))
}

/// How many mip levels an image of `size` can have, at most `asked`.
fn mip_count(size: (u32, u32), asked: u32) -> u32 {
    let full = 32 - size.0.max(size.1).leading_zeros();
    asked.clamp(1, full)
}

/// Makes each view's images match what its camera asked for, and trades history images round.
pub(super) fn prepare_view_images(
    mut commands: Commands,
    render_device: Res<RenderDevice>,
    mut views: Query<(
        Entity,
        &ViewTarget,
        &BcsViewImages,
        Option<&mut ViewImageTextures>,
    )>,
) {
    for (entity, target, asked, kept) in &mut views {
        let picture = target.main_texture();
        let mut slots: Vec<ViewImageSlot> = kept
            .map(|mut kept| std::mem::take(&mut kept.slots))
            .unwrap_or_default();

        let mut next = Vec::with_capacity(asked.0.len());

        for spec in &asked.0 {
            let size = scaled(picture.width(), picture.height(), spec.scale);

            let reusable = slots
                .iter()
                .position(|slot| slot.spec == *spec && slot.size == size);

            let slot = match reusable {
                Some(index) => {
                    let mut slot = slots.swap_remove(index);
                    if slot.spec.history {
                        slot.current = 1 - slot.current;
                    }
                    slot
                }
                None => ViewImageSlot {
                    spec: spec.clone(),
                    size,
                    textures: (0..if spec.history { 2 } else { 1 })
                        .map(|_| {
                            render_device.create_texture(&TextureDescriptor {
                                label: Some("bcs_view_image"),
                                size: Extent3d {
                                    width: size.0,
                                    height: size.1,
                                    depth_or_array_layers: 1,
                                },
                                mip_level_count: mip_count(size, spec.mips),
                                sample_count: 1,
                                dimension: TextureDimension::D2,
                                format: spec.format,
                                // Drawn into as well, by a draw on the camera that targets it, by
                                // the copy of the picture and by the clear at the start of a
                                // frame. Every format a camera image takes can be.
                                usage: TextureUsages::TEXTURE_BINDING
                                    | TextureUsages::STORAGE_BINDING
                                    | TextureUsages::COPY_SRC
                                    | TextureUsages::COPY_DST
                                    | TextureUsages::RENDER_ATTACHMENT,
                                view_formats: &[],
                            })
                        })
                        .collect(),
                    current: 0,
                },
            };

            next.push(slot);
        }

        let mut names = HashMap::new();

        for slot in &next {
            let format = slot.spec.format;
            let levels = slot.textures[0].mip_level_count();
            let current = &slot.textures[slot.current];

            names.insert(
                slot.spec.name.clone(),
                ViewTexture {
                    view: current.create_view(&TextureViewDescriptor::default()),
                    level: level_view(current, 0),
                    format,
                },
            );

            for mip in (0..levels).filter(|_| levels > 1) {
                let single = level_view(current, mip);
                names.insert(
                    format!("{}_mip{mip}", slot.spec.name),
                    ViewTexture {
                        view: single.clone(),
                        level: single,
                        format,
                    },
                );
            }

            if slot.spec.history {
                let previous = &slot.textures[1 - slot.current];
                names.insert(
                    format!("{}_previous", slot.spec.name),
                    ViewTexture {
                        view: previous.create_view(&TextureViewDescriptor::default()),
                        level: level_view(previous, 0),
                        format,
                    },
                );
            }
        }

        commands.entity(entity).insert(ViewImageTextures { slots: next, names });
    }
}

/// A view of one mip level of `texture`, as a storage binding takes.
pub(super) fn level_view(texture: &Texture, mip: u32) -> TextureView {
    texture.create_view(&TextureViewDescriptor {
        base_mip_level: mip,
        mip_level_count: Some(1),
        ..Default::default()
    })
}

/// Every name a shader on a view can read an image by: the camera's own images, and the engine's.
///
/// The engine's are Bevy's ambient occlusion where the camera has it on, which a shader may
/// replace, and the G-buffer where the camera draws deferred, which a shader reads a surface's
/// material from. Where the camera keeps the previous frame's prepass, last frame's depth and
/// G-buffer are there too, which tell a temporal technique that a pixel was hidden before. They go
/// under names of their own, so a camera's image can never shadow one.
pub fn view_names<'a>(
    owned: Option<&'a ViewImageTextures>,
    (occlusion, pyramid): (Option<&ScreenSpaceAmbientOcclusionResources>, Option<&bevy::core_pipeline::mip_generation::experimental::depth::ViewDepthPyramid>),
    prepass: Option<&ViewPrepassTextures>,
) -> Option<std::borrow::Cow<'a, HashMap<String, ViewTexture>>> {
    let gbuffer = prepass.and_then(|prepass| prepass.deferred.as_ref());
    let previous_depth = prepass
        .and_then(|prepass| prepass.depth.as_ref())
        .and_then(|depth| depth.previous_frame_texture.as_ref());

    if occlusion.is_none() && pyramid.is_none() && gbuffer.is_none() && previous_depth.is_none() {
        return owned.map(|owned| std::borrow::Cow::Borrowed(&owned.names));
    }

    let mut names = owned.map(|owned| owned.names.clone()).unwrap_or_default();

    let mut engine = |name: &str, texture: &bevy::render::texture::CachedTexture| {
        names.insert(
            name.into(),
            ViewTexture {
                view: texture.default_view.clone(),
                level: texture.default_view.clone(),
                format: texture.texture.format(),
            },
        );
    };

    if let Some(occlusion) = occlusion {
        engine("ambient_occlusion", &occlusion.screen_space_ambient_occlusion_texture);
    }

    if let Some(gbuffer) = gbuffer {
        engine("gbuffer", &gbuffer.texture);

        if let Some(previous) = &gbuffer.previous_frame_texture {
            engine("gbuffer_previous", previous);
        }
    }

    if let Some(previous) = previous_depth {
        engine("depth_previous", previous);
    }

    // Bevy's hierarchical depth, built for its occlusion culling, every level at once, each texel
    // the farthest depth of the texels under it, which a culling test against it needs.
    if let Some(pyramid) = pyramid {
        names.insert(
            "depth_pyramid".into(),
            ViewTexture {
                view: pyramid.all_mips.clone(),
                level: pyramid.mips[0].clone(),
                format: TextureFormat::R32Float,
            },
        );
    }

    Some(std::borrow::Cow::Owned(names))
}

/// The draws that copy a picture into a camera's image, by the image's format.
#[derive(Resource, Default)]
pub(super) struct PictureCopyPipelines(HashMap<TextureFormat, bevy::render::render_resource::CachedRenderPipelineId>);

/// Asks for a copy pipeline for every format a camera copies its picture into.
///
/// Bevy's own blit, which draws one texture over the whole of another, so the copy can be smaller
/// than the picture and in another format. A screen-space technique reads a half-sized, half-float
/// history of the lit picture, not the picture as it is.
pub(super) fn prepare_picture_copies(
    mut copies: ResMut<PictureCopyPipelines>,
    mut specialized: ResMut<
        bevy::render::render_resource::SpecializedRenderPipelines<bevy::core_pipeline::blit::BlitPipeline>,
    >,
    blit: Option<Res<bevy::core_pipeline::blit::BlitPipeline>>,
    cache: Res<PipelineCache>,
    views: Query<&BcsViewImages>,
) {
    let Some(blit) = blit else { return };

    for spec in views.iter().flat_map(|asked| &asked.0) {
        if spec.copy.is_none() || copies.0.contains_key(&spec.format) {
            continue;
        }

        let pipeline = specialized.specialize(
            &cache,
            &blit,
            bevy::core_pipeline::blit::BlitPipelineKey {
                target_format: spec.format,
                blend_state: None,
                samples: 1,
                source_space: None,
            },
        );

        copies.0.insert(spec.format, pipeline);
    }
}

/// Copies the picture into each of a view's images that asked for it at this point.
///
/// Before the point's dispatches, so they read the picture as the frame reached them. Into this
/// frame's image, so with history the image's `_previous` is the picture of the frame before, which
/// a dispatch at any point reads.
pub(super) fn copy_pictures<const POINT: u8>(
    view: ViewQuery<(&ViewTarget, &ViewImageTextures)>,
    copies: Res<PictureCopyPipelines>,
    blit: Option<Res<bevy::core_pipeline::blit::BlitPipeline>>,
    cache: Res<PipelineCache>,
    mut ctx: RenderContext,
) {
    use bevy::render::render_resource::{
        LoadOp, Operations, RenderPassColorAttachment, RenderPassDescriptor, StoreOp,
    };

    let (target, owned) = view.into_inner();
    let Some(blit) = blit else { return };
    let mut group = None;

    for slot in &owned.slots {
        if slot.spec.copy.map(|point| point as u8) != Some(POINT) {
            continue;
        }

        let Some(pipeline) = copies
            .0
            .get(&slot.spec.format)
            .and_then(|id| cache.get_render_pipeline(*id))
        else {
            continue;
        };

        let group = group.get_or_insert_with(|| {
            blit.create_bind_group(ctx.render_device(), target.main_texture_view(), &cache)
        });

        let into = level_view(&slot.textures[slot.current], 0);
        let mut pass = ctx.command_encoder().begin_render_pass(&RenderPassDescriptor {
            label: Some("bcs_picture_copy"),
            color_attachments: &[Some(RenderPassColorAttachment {
                view: &into,
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

        pass.set_pipeline(pipeline);
        pass.set_bind_group(0, &*group, &[]);
        pass.draw(0..3, 0..1);
    }
}

/// Drops the images of a view whose camera no longer asks for any.
pub(super) fn forget_view_images(
    mut commands: Commands,
    views: Query<Entity, (With<ViewImageTextures>, Without<BcsViewImages>)>,
) {
    for entity in &views {
        commands.entity(entity).remove::<ViewImageTextures>();
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_scaled_image_is_rounded_up_and_never_empty() {
        assert_eq!(scaled(1280, 720, 0.5), (640, 360));
        assert_eq!(scaled(1281, 721, 0.5), (641, 361));
        assert_eq!(scaled(3, 3, 0.01), (1, 1));
    }

    #[test]
    fn mips_stop_at_one_pixel() {
        assert_eq!(mip_count((1024, 512), 64), 11);
        assert_eq!(mip_count((1024, 512), 4), 4);
        assert_eq!(mip_count((1, 1), 8), 1);
        assert_eq!(mip_count((16, 16), 0), 1);
    }
}

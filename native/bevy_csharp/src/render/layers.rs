//! Cameras drawing into one layer of an image with several: a face of a cube, or a slice of an
//! array.
//!
//! Bevy's image target names an image and no layer, and a camera draws into the whole of what it is
//! given, so a camera cannot draw into one face of a cube directly. A camera pointed at a layer
//! draws into an ordinary image of the layer's size and format instead, a companion kept on the
//! camera, and the render world copies that into the layer once the cameras have drawn, the way a
//! reflection probe's faces reach its cube. Six cameras pointed at the six layers of a cube target
//! capture a cube map of the game's own, for a probe, a sky or a light's view of the world.

#![cfg(feature = "render")]

use bevy::app::App;
use bevy::asset::{AssetId, Assets, Handle};
use bevy::camera::{Camera, RenderTarget};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Query, Res, ResMut};
use bevy::ecs::world::World;
use bevy::image::Image;
use bevy::render::render_asset::RenderAssets;
use bevy::render::renderer::{RenderContext, RenderGraph, RenderGraphSystems};
use bevy::render::texture::GpuImage;
use bevy::render::{Extract, ExtractSchedule, RenderApp};

use crate::interop::status;

/// On a camera drawing into one layer of an image: the image, the layer, and the companion the
/// camera draws into.
#[derive(Component, Clone)]
pub struct LayerTarget {
    image: Handle<Image>,
    layer: u32,
    companion: Handle<Image>,
}

/// Points `camera` at layer `layer` of the image `image` names, drawing through a companion image
/// the render world copies into it.
///
/// Returns [`status::NULL_ARG`] for a layer the image does not have.
pub fn set(world: &mut World, camera: Entity, image: Handle<Image>, layer: u32) -> i32 {
    let Some(images) = world.get_resource::<Assets<Image>>() else {
        return status::UNSUPPORTED;
    };

    let Some(layered) = images.get(&image) else {
        return status::NOT_PRESENT;
    };

    let size = layered.texture_descriptor.size;

    if layer >= size.depth_or_array_layers {
        return status::NULL_ARG;
    }

    let float = layered.texture_descriptor.format == bevy::render::render_resource::TextureFormat::Rgba16Float;

    // Kept where the camera already has one for this image, so moving between layers of the same
    // image does not make a new companion each time.
    let companion = match world.get::<LayerTarget>(camera) {
        Some(existing) if existing.image == image => existing.companion.clone(),
        _ => {
            let made = super::assets::target_image_in(size.width, size.height, float);
            world.resource_mut::<Assets<Image>>().add(made)
        }
    };

    world.entity_mut(camera).insert((
        RenderTarget::Image(companion.clone().into()),
        LayerTarget {
            image,
            layer,
            companion,
        },
    ));

    status::OK
}

/// Forgets that a camera draws into a layer, for a camera pointed somewhere else.
pub fn clear(world: &mut World, camera: Entity) {
    world.entity_mut(camera).remove::<LayerTarget>();
}

/// The copies the render world makes this frame: a companion into a layer.
#[derive(Resource, Default)]
struct LayerCopies(Vec<(AssetId<Image>, AssetId<Image>, u32)>);

fn extract_layer_copies(
    mut copies: ResMut<LayerCopies>,
    cameras: Extract<Query<(&Camera, &LayerTarget)>>,
) {
    copies.0.clear();

    for (camera, target) in &cameras {
        // A camera that is off drew nothing new, and copying its old picture again costs the copy.
        if camera.is_active {
            copies.0.push((target.companion.id(), target.image.id(), target.layer));
        }
    }
}

/// Copies each camera's companion into its layer, after the cameras have drawn.
fn copy_layers(copies: Res<LayerCopies>, images: Res<RenderAssets<GpuImage>>, mut ctx: RenderContext) {
    use bevy::render::render_resource::{Extent3d, Origin3d, TexelCopyTextureInfo, TextureAspect};

    for (companion, layered, layer) in &copies.0 {
        let (Some(companion), Some(layered)) = (images.get(*companion), images.get(*layered)) else {
            continue;
        };

        let size = companion.texture_descriptor.size;

        ctx.command_encoder().copy_texture_to_texture(
            companion.texture.as_image_copy(),
            TexelCopyTextureInfo {
                texture: &layered.texture,
                mip_level: 0,
                origin: Origin3d { x: 0, y: 0, z: *layer },
                aspect: TextureAspect::All,
            },
            Extent3d {
                width: size.width,
                height: size.height,
                depth_or_array_layers: 1,
            },
        );
    }
}

pub fn install(app: &mut App) {
    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<LayerCopies>()
        .add_systems(ExtractSchedule, extract_layer_copies)
        .add_systems(
            RenderGraph,
            copy_layers
                .in_set(RenderGraphSystems::Render)
                .after(bevy::core_pipeline::schedule::camera_driver),
        );
}

//! Images a compute shader writes and anything samples: made as zeros, from texels or
//! block-compressed, and written a region at a time on the GPU.

use bevy::asset::{Assets, Handle, RenderAssetUsages};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Res, ResMut};
use bevy::ecs::world::World;
use bevy::image::Image;
use bevy::render::MainWorld;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat, TextureUsages};
use bevy::render::renderer::RenderDevice;
use bevy::render::texture::GpuImage;

use crate::interop::status;

/// Texels to write into a region of an image, on the GPU, before this frame's dispatches.
pub struct ImageWrite {
    image: Handle<Image>,
    origin: [u32; 3],
    size: [u32; 3],
    mip: u32,
    bytes: Vec<u8>,
}

/// The region writes asked for since the last frame was extracted.
#[derive(Resource, Default)]
pub struct ImageWrites(Vec<ImageWrite>);

/// The same in the render world, with the writes whose image was not on the GPU yet kept for the
/// next frame.
#[derive(Resource, Default)]
pub(super) struct PendingImageWrites(Vec<ImageWrite>);

pub(super) fn extract_image_writes(mut main_world: ResMut<MainWorld>, mut pending: ResMut<PendingImageWrites>) {
    if let Some(mut writes) = main_world.get_resource_mut::<ImageWrites>() {
        pending.0.append(&mut writes.0);
    }
}

/// Writes each queued region into its image's texture, in the order asked, through the queue, so
/// the texels are there before any of this frame's work reads them.
pub(super) fn write_image_regions(
    mut pending: ResMut<PendingImageWrites>,
    images: Res<RenderAssets<GpuImage>>,
    queue: Res<bevy::render::renderer::RenderQueue>,
) {
    use bevy::render::render_resource::{Origin3d, TexelCopyBufferLayout, TexelCopyTextureInfo, TextureAspect};

    pending.0.retain(|write| {
        let Some(gpu) = images.get(&write.image) else {
            return true;
        };

        let format = gpu.texture.format();
        let texel = format.block_copy_size(None).unwrap_or(4);
        let (block_width, block_height) = format.block_dimensions();
        let [width, height, depth] = write.size;

        queue.write_texture(
            TexelCopyTextureInfo {
                texture: &gpu.texture,
                mip_level: write.mip,
                origin: Origin3d { x: write.origin[0], y: write.origin[1], z: write.origin[2] },
                aspect: TextureAspect::All,
            },
            &write.bytes,
            TexelCopyBufferLayout {
                offset: 0,
                bytes_per_row: Some(width.div_ceil(block_width) * texel),
                rows_per_image: Some(height.div_ceil(block_height)),
            },
            Extent3d { width, height, depth_or_array_layers: depth },
        );

        false
    });
}

/// Asks for `bytes` to be written into the region of `key`'s image at `origin`, `size` texels,
/// of mip level `mip`, and checks it fits: the region inside the level, and exactly its texels.
///
/// Only the GPU's copy changes. The image's copy in memory keeps what it had, which matters only
/// if the image is changed from this side afterward, since that uploads the memory copy again.
pub fn write_image(world: &mut World, key: i32, origin: [u32; 3], size: [u32; 3], mip: u32, bytes: &[u8]) -> i32 {
    let Some(image) = crate::assets::clone_handle(world, key).and_then(|handle| handle.try_typed::<Image>().ok())
    else {
        return status::NO_COMPONENT;
    };

    let Some(found) = world.get_resource::<Assets<Image>>().and_then(|images| images.get(&image)) else {
        return status::NO_COMPONENT;
    };

    let descriptor = &found.texture_descriptor;

    if mip >= descriptor.mip_level_count || size.contains(&0) {
        return status::NULL_ARG;
    }

    let level = |extent: u32| (extent >> mip).max(1);
    let layers = if descriptor.dimension == TextureDimension::D3 {
        level(descriptor.size.depth_or_array_layers)
    } else {
        descriptor.size.depth_or_array_layers
    };

    let bounds = [level(descriptor.size.width), level(descriptor.size.height), layers];

    if (0..3).any(|axis| origin[axis].checked_add(size[axis]).is_none_or(|end| end > bounds[axis])) {
        return status::NULL_ARG;
    }

    // Counted in blocks for a compressed format, a texel being a block of one there. A region of
    // one has to start on a block and be whole blocks, except where it reaches the level's edge.
    let (block_width, block_height) = descriptor.format.block_dimensions();
    let texel = descriptor.format.block_copy_size(None).unwrap_or(4) as usize;

    let aligned = |start: u32, extent: u32, bound: u32, block: u32| {
        start % block == 0 && (extent % block == 0 || start + extent == bound)
    };

    if !aligned(origin[0], size[0], bounds[0], block_width) || !aligned(origin[1], size[1], bounds[1], block_height) {
        return status::NULL_ARG;
    }

    let blocks = size[0].div_ceil(block_width) as usize * size[1].div_ceil(block_height) as usize * size[2] as usize;

    if bytes.len() != blocks * texel {
        return status::BUFFER_TOO_SMALL;
    }

    world.get_resource_or_init::<ImageWrites>().0.push(ImageWrite {
        image,
        origin,
        size,
        mip,
        bytes: bytes.to_vec(),
    });

    status::OK
}
/// The formats an image a shader writes can be made in, by the number the entry points take.
///
/// In the order the managed side's enum lists them, which is the order of how often they are
/// wanted rather than of anything the GPU cares about. The number beside each is the bytes of a
/// texel, or for a block-compressed format the bytes of a four by four block.
pub const IMAGE_FORMATS: [(TextureFormat, usize); 16] = [
    (TextureFormat::Rgba8Unorm, 4),
    (TextureFormat::Rgba16Float, 8),
    (TextureFormat::Rgba32Float, 16),
    (TextureFormat::R32Float, 4),
    (TextureFormat::R32Uint, 4),
    (TextureFormat::R32Sint, 4),
    (TextureFormat::Rg32Float, 8),
    (TextureFormat::Rgba32Uint, 16),
    (TextureFormat::Rgba8Uint, 4),
    (TextureFormat::R16Float, 2),
    // Block-compressed, which only a sampler reads, filled from memory a block at a time. What a
    // streamed texture's cache is kept in, at a quarter or an eighth of the size of the texels.
    (TextureFormat::Bc1RgbaUnorm, 8),
    (TextureFormat::Bc4RUnorm, 8),
    (TextureFormat::Bc5RgUnorm, 16),
    (TextureFormat::Bc7RgbaUnorm, 16),
    (TextureFormat::Bc7RgbaUnormSrgb, 16),
    (TextureFormat::Bc6hRgbUfloat, 16),
];

/// Makes a block-compressed image, which starts as zeros on the GPU or as `blocks` when given,
/// and which only a sampler reads.
///
/// Its sides have to be whole blocks, and the adapter has to decode the format, which every
/// desktop GPU does for these and which is checked rather than left to fail as a GPU error.
fn create_compressed(
    world: &mut World,
    width: u32,
    height: u32,
    format: TextureFormat,
    block_bytes: usize,
    blocks: Option<&[u8]>,
) -> i32 {
    let supported = world
        .get_resource::<RenderDevice>()
        .is_some_and(|device| device.features().contains(bevy::render::settings::WgpuFeatures::TEXTURE_COMPRESSION_BC));

    if !supported {
        return status::UNSUPPORTED;
    }

    if width % 4 != 0 || height % 4 != 0 {
        return status::NULL_ARG;
    }

    let wanted = (width / 4) as usize * (height / 4) as usize * block_bytes;

    if blocks.is_some_and(|blocks| blocks.len() != wanted) {
        return status::BUFFER_TOO_SMALL;
    }

    let mut image = Image::default();
    image.texture_descriptor.size = Extent3d { width, height, depth_or_array_layers: 1 };
    image.texture_descriptor.dimension = TextureDimension::D2;
    image.texture_descriptor.format = format;
    image.texture_descriptor.mip_level_count = 1;
    image.texture_descriptor.usage = TextureUsages::TEXTURE_BINDING | TextureUsages::COPY_SRC | TextureUsages::COPY_DST;
    image.data = blocks.map(<[u8]>::to_vec);

    let Some(mut assets) = world.get_resource_mut::<Assets<Image>>() else {
        return status::UNSUPPORTED;
    };

    let handle = assets.add(image);
    crate::assets::insert_handle(world, handle.untyped())
}

/// Makes an image a compute shader can write and anything can sample, and answers its asset key.
///
/// `depth` above one makes a 3D image that many deep, for a shader writing a `RWTexture3D`. It
/// starts as zeros.
pub fn create_image(world: &mut World, width: u32, height: u32, depth: u32, format: i32) -> i32 {
    create_image_from(world, width, height, depth, format, None)
}

/// Makes an image as [`create_image`] does, with `mips` mip levels, as many as its size allows.
///
/// Every level starts as zeros, as wgpu gives a texture made without contents, since contents for
/// one level would leave the rest to be supplied and a pyramid is built on the GPU.
pub fn create_image_with_mips(
    world: &mut World,
    width: u32,
    height: u32,
    depth: u32,
    format: i32,
    mips: u32,
) -> i32 {
    if mips <= 1 {
        return create_image(world, width, height, depth, format);
    }

    let key = create_image(world, width, height, depth, format);

    if key <= 0 {
        return key;
    }

    let Some(handle) = crate::assets::clone_handle(world, key).and_then(|handle| handle.try_typed::<Image>().ok()) else {
        return status::NO_COMPONENT;
    };

    let Some(mut images) = world.get_resource_mut::<Assets<Image>>() else {
        return status::UNSUPPORTED;
    };

    let Some(mut image) = images.get_mut(&handle) else {
        return status::NO_COMPONENT;
    };

    let largest = width.max(height).max(if depth > 1 { depth } else { 1 });
    let possible = 32 - largest.leading_zeros();

    image.texture_descriptor.mip_level_count = mips.min(possible);
    image.data = None;

    key
}

/// Makes an image as [`create_image`] does, starting with `texels` rather than zeros.
///
/// `texels` is every texel in the format's own layout, row after row and slice after slice, so a
/// heightmap of floats is `width * height` floats as they are. Anything but exactly that many bytes
/// is refused, since a picture read with the wrong stride is noise rather than an error.
pub fn create_image_from(
    world: &mut World,
    width: u32,
    height: u32,
    depth: u32,
    format: i32,
    texels: Option<&[u8]>,
) -> i32 {
    let Some(&(format, texel_bytes)) = usize::try_from(format)
        .ok()
        .and_then(|index| IMAGE_FORMATS.get(index))
    else {
        return status::NULL_ARG;
    };

    if width == 0 || height == 0 || depth == 0 {
        return status::NULL_ARG;
    }

    if format.is_compressed() {
        if depth > 1 {
            return status::NULL_ARG;
        }

        return create_compressed(world, width, height, format, texel_bytes, texels);
    }

    let size = Extent3d {
        width,
        height,
        depth_or_array_layers: depth,
    };

    let dimension = if depth > 1 {
        TextureDimension::D3
    } else {
        TextureDimension::D2
    };

    let mut image = match texels {
        Some(texels) => {
            let wanted = width as usize * height as usize * depth as usize * texel_bytes;

            if texels.len() != wanted {
                return status::BUFFER_TOO_SMALL;
            }

            Image::new(size, dimension, texels.to_vec(), format, RenderAssetUsages::default())
        }
        None => Image::new_fill(
            size,
            dimension,
            &vec![0u8; texel_bytes],
            format,
            RenderAssetUsages::default(),
        ),
    };

    image.texture_descriptor.usage = TextureUsages::TEXTURE_BINDING
        | TextureUsages::STORAGE_BINDING
        | TextureUsages::COPY_SRC
        | TextureUsages::COPY_DST;

    let Some(mut assets) = world.get_resource_mut::<Assets<Image>>() else {
        return status::UNSUPPORTED;
    };

    let handle = assets.add(image);
    crate::assets::insert_handle(world, handle.untyped())
}

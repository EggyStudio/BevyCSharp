//! What the managed side reads of an image as a whole.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Writes an image's width, height and depth in texels to `size`, the depth being its slices for a
/// 3D image and its layers for a cube or an array, and one for a plain picture.
///
/// For a game sizing something by an image it did not make, as Bevy's irradiance_volumes example
/// spawns a cube for every voxel of a volume read from a file. Returns [`status::NOT_PRESENT`]
/// while the image is loading. The size is the one Bevy keeps for the texture, so an image whose
/// texels were handed to the GPU without a copy kept still has one, unlike its pixels.
///
/// # Safety
/// `size` must be writable for three integers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_image_size(image: i32, size: *mut u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, size);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if size.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };
                let Some(found) = world.resource::<bevy::asset::Assets<bevy::image::Image>>().get(&handle) else {
                    return status::NOT_PRESENT;
                };

                let extent = found.texture_descriptor.size;
                unsafe {
                    size.write(extent.width);
                    size.add(1).write(extent.height);
                    size.add(2).write(extent.depth_or_array_layers);
                }
                status::OK
            })
        }
    })
}

/// Builds an empty image sized for a camera to draw into.
///
/// The usages separate a texture that can be drawn into and copied out of from one that can only be
/// sampled. Without `RENDER_ATTACHMENT` a camera cannot target it, and without `COPY_SRC` a capture
/// finds nothing to read back.
#[cfg(feature = "render")]
pub(crate) fn target_image(width: u32, height: u32) -> bevy::image::Image {
    target_image_in(width, height, false)
}

/// The same, in half floats where `float` is set, which keeps what a camera draws brighter than
/// white as it is rather than clamping it to one, for a reflection or a picture a shader reads on.
#[cfg(feature = "render")]
pub(crate) fn target_image_in(width: u32, height: u32, float: bool) -> bevy::image::Image {
    target_image_layers(width, height, float, 1)
}

/// The same with `layers` layers, which cameras draw into one at a time (see [`super::layers`]).
///
/// Six square layers are viewed as a cube, as a material's or a probe's cube slot reads them, and
/// any other count as an array.
#[cfg(feature = "render")]
pub(crate) fn target_image_layers(width: u32, height: u32, float: bool, layers: u32) -> bevy::image::Image {
    use bevy::asset::RenderAssetUsages;
    use bevy::image::Image;
    use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat, TextureUsages};

    let size = Extent3d {
        width: width.max(1),
        height: height.max(1),
        depth_or_array_layers: layers.max(1),
    };

    // Opaque black rather than transparent, because a picture of a scene with nothing in front of
    // the camera should look like an empty scene rather than like a failure. The format is named
    // outright, because Bevy deprecated its default in favor of asking the view, and a target
    // created before there is a view to ask has to choose one.
    // Half-float one, for the opaque black.
    const HALF_ONE: [u8; 2] = 0x3C00u16.to_le_bytes();

    let (black, format): (&[u8], TextureFormat) = if float {
        (&[0, 0, 0, 0, 0, 0, HALF_ONE[0], HALF_ONE[1]], TextureFormat::Rgba16Float)
    } else {
        (&[0, 0, 0, 255], TextureFormat::Rgba8UnormSrgb)
    };

    let mut image = Image::new_fill(size, TextureDimension::D2, black, format, RenderAssetUsages::default());

    image.texture_descriptor.usage =
        TextureUsages::COPY_SRC | TextureUsages::RENDER_ATTACHMENT | TextureUsages::TEXTURE_BINDING;

    if layers > 1 {
        use bevy::render::render_resource::{TextureViewDescriptor, TextureViewDimension};

        // Written by copies from the cameras' companions rather than drawn into.
        image.texture_descriptor.usage |= TextureUsages::COPY_DST;
        image.texture_view_descriptor = Some(TextureViewDescriptor {
            dimension: Some(if layers == 6 && width == height {
                TextureViewDimension::Cube
            } else {
                TextureViewDimension::D2Array
            }),
            ..Default::default()
        });
    }

    image
}

/// Creates an image a camera can draw into, and returns an asset handle for it.
///
/// The other half of [`crate::render::scene::bcs_render_set_camera_target`], and what a portal, a
/// security monitor or a second viewport is built from. A camera draws into this image, and a
/// material sampling the same handle shows what that camera sees.
///
/// The image is empty until something draws into it. Nothing loads, so the handle is usable on the
/// frame it is returned. `format` is `0` for eight-bit sRGB and `1` for half floats, which keep
/// light brighter than white as it was drawn. `layers` above one makes an image cameras draw into a
/// layer at a time, viewed as a cube where there are six square layers and as an array otherwise.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_create_target(width: u32, height: u32, format: i32, layers: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height, format, layers);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::image::Image;

            if !(0..=1).contains(&format) || layers == 0 {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let image = target_image_layers(width, height, format == 1, layers);

                let Some(mut images) = world.get_resource_mut::<Assets<Image>>() else {
                    return status::UNSUPPORTED;
                };

                let handle = images.add(image).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

// -- Reshaping images

/// Images asked to become array or 3D textures, waiting for their pixels to arrive.
///
/// The same wait a cubemap has, because an image loads as one tall picture, and how it divides into
/// layers or slices can only be applied once it has been decoded.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct PendingReshapes(Vec<PendingReshape>);

#[cfg(feature = "render")]
impl PendingReshapes {
    /// Whether an image is waiting to be made a 3D texture, which an irradiance volume given it
    /// in the same frame as `MakeVolume` counts on.
    pub fn waits_as_volume(&self, image: &bevy::asset::Handle<bevy::image::Image>) -> bool {
        self.0.iter().any(|waiting| waiting.volume && waiting.image.id() == image.id())
    }
}

/// One image waiting to be reshaped.
#[cfg(feature = "render")]
pub struct PendingReshape {
    image: bevy::asset::Handle<bevy::image::Image>,
    /// How many layers or slices it is stacked into.
    count: u32,
    /// A 3D texture rather than an array of 2D ones.
    volume: bool,
}

/// Reshapes each loaded image on the list, and forgets it.
///
/// Both shapes read the picture as `count` equal parts stacked from top to bottom, which is the
/// order their bytes are already in, so nothing is copied.
#[cfg(feature = "render")]
pub fn reshape_images(
    mut pending: bevy::ecs::system::ResMut<PendingReshapes>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
) {
    use bevy::render::render_resource::{
        Extent3d, TextureDimension, TextureViewDescriptor, TextureViewDimension,
    };

    pending.0.retain(|waiting| {
        let Some(mut image) = images.get_mut(&waiting.image) else {
            return true;
        };

        let size = image.texture_descriptor.size;

        // Asked twice, which is not worth refusing.
        let already = if waiting.volume {
            image.texture_descriptor.dimension == TextureDimension::D3
        } else {
            size.depth_or_array_layers == waiting.count && waiting.count > 1
        };

        if !already {
            if size.depth_or_array_layers != 1 || !size.height.is_multiple_of(waiting.count) {
                bevy::log::warn!(
                    "An image {} pixels tall cannot be cut into {} equal {}, so whatever samples \
                     it as one will not.",
                    size.height,
                    waiting.count,
                    if waiting.volume { "slices" } else { "layers" }
                );
                return false;
            }

            let reshaped = Extent3d {
                width: size.width,
                height: size.height / waiting.count,
                depth_or_array_layers: waiting.count,
            };

            if image.reinterpret_size(reshaped).is_err() {
                return false;
            }

            if waiting.volume {
                image.texture_descriptor.dimension = TextureDimension::D3;
            }
        }

        // Named outright, because an array of one layer, or of six, would otherwise be taken for a
        // plain picture or a cube.
        image.texture_view_descriptor = Some(TextureViewDescriptor {
            dimension: Some(if waiting.volume {
                TextureViewDimension::D3
            } else {
                TextureViewDimension::D2Array
            }),
            ..Default::default()
        });

        false
    });
}

/// Asks for an image to be treated as a cubemap once it has loaded, and answers at once.
///
/// Six square faces stacked vertically, which is the layout the skybox takes. Suits a shader
/// material's cube slots, since a flat picture there is replaced by the fallback.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_make_cubemap(image: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = image;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };

                let id = handle.id();
                world
                    .get_resource_or_init::<crate::render::post::PendingCubemaps>()
                    .push(handle);

                // An image already here, as one made from pixels is, becomes a cube now rather than
                // at the next frame's reshaping, so a skybox or a probe given it in the same system
                // never sees it flat. One still loading waits for the reshaping as before.
                if world.resource::<bevy::asset::Assets<bevy::image::Image>>().contains(id) {
                    use bevy::ecs::system::RunSystemOnce;

                    // Made here where the app has not, as a windowless one with images has not.
                    world.init_resource::<crate::render::post::PendingEnvironments>();
                    world.init_resource::<crate::render::post::PendingFaces>();

                    if let Err(error) = world.run_system_once(crate::render::post::reinterpret_cubemaps) {
                        bevy::log::warn!("An image could not be made a cubemap at once, so it waits for the next frame. {error}");
                    }
                }

                status::OK
            })
        }
    })
}

/// Makes a cubemap out of six images, one a face, and returns its asset key at once.
///
/// `faces` holds six image keys in the column's order, +X, -X, +Y, -Y, +Z, -Z, as six files a
/// cubemap is often shipped as. The image handed back holds a placeholder until all six have
/// loaded, then their pixels as a column, then becomes a cube, so it can be given to a skybox or
/// a material at once. Returns a negative status where a key names no image.
///
/// # Safety
/// `faces` must point at six readable keys.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_cubemap_from_faces(faces: *const i32) -> i32 {
    crate::interop::guard(|| {
        if faces.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let keys = unsafe { core::slice::from_raw_parts(faces, 6) };

            with_world(|world| {
                let mut handles = Vec::with_capacity(6);
                for key in keys {
                    match crate::render::image_handle(world, *key) {
                        Ok(Some(handle)) => handles.push(handle),
                        Ok(None) => return status::NULL_ARG,
                        Err(refusal) => return refusal,
                    }
                }

                let Ok(faces) = <[_; 6]>::try_from(handles) else {
                    return status::NULL_ARG;
                };

                // A pixel to stand in until the faces arrive, so the handle names an image now.
                let placeholder = bevy::image::Image::new_fill(
                    bevy::render::render_resource::Extent3d {
                        width: 1,
                        height: 1,
                        depth_or_array_layers: 1,
                    },
                    bevy::render::render_resource::TextureDimension::D2,
                    &[0, 0, 0, 255],
                    bevy::render::render_resource::TextureFormat::Rgba8UnormSrgb,
                    bevy::asset::RenderAssetUsages::default(),
                );

                let Some(mut images) = world.get_resource_mut::<bevy::asset::Assets<bevy::image::Image>>() else {
                    return status::NOT_PRESENT;
                };
                let target = images.add(placeholder);

                world
                    .get_resource_or_init::<crate::render::post::PendingFaces>()
                    .0
                    .push(crate::render::post::PendingFace { target: target.clone(), faces });

                crate::assets::insert_handle(world, target.untyped())
            })
        }
    })
}

/// Asks for an image to be cut into `count` layers or slices once it has loaded.
///
/// `volume` non-zero makes a 3D texture of `count` slices, and zero an array of `count` 2D layers.
/// Either way the parts are stacked from top to bottom in the picture.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_reshape_image(image: i32, count: i32, volume: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, count, volume);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if count < 1 {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };

                let id = handle.id();
                world
                    .get_resource_or_init::<PendingReshapes>()
                    .0
                    .push(PendingReshape {
                        image: handle,
                        count: count as u32,
                        volume: volume != 0,
                    });

                // Reshaped now where the pixels are already here, as a cubemap is.
                if world.resource::<bevy::asset::Assets<bevy::image::Image>>().contains(id) {
                    use bevy::ecs::system::RunSystemOnce;
                    if let Err(error) = world.run_system_once(reshape_images) {
                        bevy::log::warn!("An image could not be reshaped at once, so it waits for the next frame. {error}");
                    }
                }

                status::OK
            })
        }
    })
}


/// Writes an image's width, height and bytes a texel to `size`, and copies the copy of its texels
/// the app keeps, row after row, to `out`, answering their length in bytes.
///
/// Returns [`status::NOT_PRESENT`] while the image is loading, and for one whose texels were
/// handed to the GPU without a copy kept, and [`status::INVALID_STATE`] for a compressed format,
/// whose texels are blocks rather than a row of values each.
///
/// # Safety
/// `size` must be writable for three integers, and `out` for `capacity` bytes or null when
/// `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_image_pixels(image: i32, size: *mut u32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, size, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if size.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };
                let Some(found) = world.resource::<bevy::asset::Assets<bevy::image::Image>>().get(&handle) else {
                    return status::NOT_PRESENT;
                };
                let format = found.texture_descriptor.format;
                if format.block_dimensions() != (1, 1) {
                    return status::INVALID_STATE;
                }
                let Some(data) = found.data.as_ref() else {
                    return status::NOT_PRESENT;
                };

                unsafe {
                    size.write(found.width());
                    size.add(1).write(found.height());
                    size.add(2).write(format.block_copy_size(None).unwrap_or(0));
                }

                let length = data.len().min(i32::MAX as usize) as i32;
                if !out.is_null() && capacity >= length {
                    unsafe { core::ptr::copy_nonoverlapping(data.as_ptr(), out, length as usize) };
                }
                length
            })
        }
    })
}

/// Writes texels over the copy an image keeps, as many bytes as it holds, so the GPU is given them
/// again and everything drawn with the image changes.
///
/// Returns what [`bcs_render_image_pixels`] refuses, and [`status::NULL_ARG`] where the bytes are
/// not as many as the image holds.
///
/// # Safety
/// `data` must be readable for `length` bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_image_set_pixels(image: i32, data: *const u8, length: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, data, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if data.is_null() || length < 0 {
                return status::NULL_ARG;
            }
            let bytes = unsafe { core::slice::from_raw_parts(data, length as usize) };

            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };
                let mut images = world.resource_mut::<bevy::asset::Assets<bevy::image::Image>>();
                let Some(mut found) = images.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                let Some(kept) = found.data.as_mut() else {
                    return status::NOT_PRESENT;
                };
                if kept.len() != bytes.len() {
                    return status::NULL_ARG;
                }

                kept.copy_from_slice(bytes);
                status::OK
            })
        }
    })
}

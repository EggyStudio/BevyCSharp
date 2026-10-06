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

//! Pictures of what is drawn, written to a file as a screenshot or read back into memory as a
//! capture a program inspects.

use crate::interop::status;

#[cfg(feature = "render")]
use crate::state::with_world;

/// Writes what is being drawn to a PNG file.
///
/// A negative `target` captures whatever this run is drawing into, which is the window, or the
/// image an offscreen run draws into instead. Which of those it is is not the caller's to decide,
/// because a run has one thing it is drawing and a capture is a picture of that.
///
/// An asset key instead captures that image, which reads back the view of a camera pointed at a
/// texture, such as a minimap, a portal, or the second viewport an editor draws.
///
/// The capture happens on the frame after this call, because the picture has to come back off the
/// GPU, and the file appears once it has. A caller that needs to know it arrived watches for the
/// file rather than for this returning.
///
/// This is how a change to the picture is checked without a person looking at it, which is
/// otherwise the only way. The tests can assert that settings are accepted, and nothing else can
/// tell whether the scene is drawn at all.
///
/// # Safety
/// `path` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_screenshot(path: *const core::ffi::c_char, target: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (path, target);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::render::view::screenshot::{save_to_disk, Screenshot};

            let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let capture = match crate::render::image_handle(world, target) {
                    Err(refusal) => return refusal,
                    Ok(Some(image)) => Screenshot::image(image),
                    Ok(None) => match world.get_resource::<crate::app::OffscreenTarget>() {
                        Some(target) => Screenshot::image(target.image.clone()),
                        None => Screenshot::primary_window(),
                    },
                };

                world.spawn(capture).observe(save_to_disk(path));
                status::OK
            })
        }
    })
}

/// A picture that has come back off the GPU, waiting to be read.
///
/// Held in the format it arrived in, rows top to bottom with no padding, and turned into eight-bit
/// color only when it is read that way ([`bcs_render_capture_read`]), so a half-float target can
/// also be read as it is ([`bcs_render_capture_read_raw`]), light brighter than white included.
#[cfg(feature = "render")]
struct CapturedPixels {
    /// Width in pixels.
    width: u32,
    /// Height in pixels.
    height: u32,
    /// What each pixel is.
    format: bevy::render::render_resource::TextureFormat,
    /// The pixels as they came back.
    bytes: Vec<u8>,
}

#[cfg(feature = "render")]
impl CapturedPixels {
    /// The picture as four bytes a pixel, sRGB, or `None` for a format with no colors to read.
    ///
    /// Floats are encoded here, clamped at white, and anything else through Bevy's conversion.
    /// Linear eight-bit pixels are the same bytes as sRGB ones, and Bevy's conversion knows only the
    /// second, so they are relabeled rather than refused.
    fn to_rgba8(&self) -> Option<Vec<u8>> {
        use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat};

        let format = match self.format {
            TextureFormat::Rgba8Unorm => TextureFormat::Rgba8UnormSrgb,
            other => other,
        };

        if let Some(rgba) = floats_to_rgba8(format, &self.bytes) {
            return Some(rgba);
        }

        let picture = bevy::image::Image::new(
            Extent3d {
                width: self.width,
                height: self.height,
                depth_or_array_layers: 1,
            },
            TextureDimension::D2,
            self.bytes.clone(),
            format,
            bevy::asset::RenderAssetUsages::default(),
        );

        Some(picture.try_into_dynamic().ok()?.to_rgba8().into_raw())
    }

    /// The picture as it is, with the number of its format among the formats a shader image is
    /// made in, or `None` for a format that is not one of them.
    ///
    /// An eight-bit picture in blue-green-red order, as a window's surface holds one on most
    /// platforms, is turned into red-green-blue order, so every eight-bit capture reads the
    /// same way. Its bytes are the encoded colors either way.
    fn as_it_is(&self) -> Option<(i32, std::borrow::Cow<'_, [u8]>)> {
        use bevy::render::render_resource::TextureFormat;

        match self.format {
            TextureFormat::Rgba8UnormSrgb => Some((0, std::borrow::Cow::Borrowed(&self.bytes[..]))),
            TextureFormat::Bgra8Unorm | TextureFormat::Bgra8UnormSrgb => {
                let swapped = self.bytes.chunks_exact(4).flat_map(|pixel| [pixel[2], pixel[1], pixel[0], pixel[3]]).collect();
                Some((0, std::borrow::Cow::Owned(swapped)))
            }
            format => crate::render::compute::IMAGE_FORMATS
                .iter()
                .position(|(known, _)| *known == format)
                .map(|number| (number as i32, std::borrow::Cow::Borrowed(&self.bytes[..]))),
        }
    }
}

/// The captures asked for and not yet read.
///
/// A capture is answered frames after it is asked for, so it cannot be a return value. Each one is
/// given a number when it is asked for, and the managed side holds on to the number until the
/// picture arrives.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
struct Captures {
    /// The last number handed out. Counts up, so a released number is never reused and a stale
    /// read is refused rather than answered with somebody else's picture.
    last: i32,
    /// What has arrived, by number.
    ready: bevy::platform::collections::HashMap<i32, CapturedPixels>,
}

/// Asks for a picture to be read back into memory rather than written to a file.
///
/// A negative `target` captures whatever this run is drawing into, and an asset key captures that
/// image, exactly as [`bcs_render_screenshot`] does. What differs is where the picture goes. A file
/// is for a person to look at, and this is for a program to inspect, so a test can assert on a
/// pixel.
///
/// Returns a positive number naming the capture. The picture arrives a frame or two later, because
/// it has to come back off the GPU, and [`bcs_render_capture_read`] says when.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_capture(target: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = target;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::system::ResMut;
            use bevy::render::view::screenshot::{Screenshot, ScreenshotCaptured};

            with_world(|world| {
                let capture = match crate::render::image_handle(world, target) {
                    Err(refusal) => return refusal,
                    // Bevy's screenshot of an image copies what a camera draws into it, so an
                    // image no camera draws into (what a compute shader wrote, a watch) is read
                    // back from the GPU as it is instead.
                    Ok(Some(image)) if !drawn_by_a_camera(world, &image) => {
                        return read_image_back(world, image);
                    }
                    Ok(Some(image)) => Screenshot::image(image),
                    Ok(None) => match world.get_resource::<crate::app::OffscreenTarget>() {
                        Some(target) => Screenshot::image(target.image.clone()),
                        None => Screenshot::primary_window(),
                    },
                };

                let mut captures = world.get_resource_or_init::<Captures>();
                captures.last += 1;

                let id = captures.last;

                world.spawn(capture).observe(
                    move |captured: bevy::ecs::observer::On<ScreenshotCaptured>,
                          mut captures: ResMut<Captures>| {
                        let picture = &captured.image;
                        let Some(bytes) = picture.data.clone() else {
                            bevy::log::warn!("Capture {id} came back with no pixels, so it never arrives.");
                            return;
                        };

                        let size = picture.texture_descriptor.size;
                        captures.ready.insert(
                            id,
                            CapturedPixels {
                                width: size.width,
                                height: size.height,
                                format: picture.texture_descriptor.format,
                                bytes,
                            },
                        );
                    },
                );

                id
            })
        }
    })
}

/// A picture of linear floats as eight-bit sRGB, clamped at white, or `None` for a format that is
/// not four channels of floats.
///
/// Bevy's own conversion reads only eight-bit formats, and a capture is a picture for a program to
/// inspect the way a person would see it, so light brighter than white reads as white here while
/// a shader sampling the image still reads it as it is.
#[cfg(feature = "render")]
fn floats_to_rgba8(format: bevy::render::render_resource::TextureFormat, bytes: &[u8]) -> Option<Vec<u8>> {
    use bevy::render::render_resource::TextureFormat;

    let linear: Vec<f32> = match format {
        TextureFormat::Rgba16Float => bytes
            .chunks_exact(2)
            .map(|half| half_to_f32(u16::from_le_bytes([half[0], half[1]])))
            .collect(),
        TextureFormat::Rgba32Float => bytes
            .chunks_exact(4)
            .map(|word| f32::from_le_bytes([word[0], word[1], word[2], word[3]]))
            .collect(),
        _ => return None,
    };

    Some(
        linear
            .chunks_exact(4)
            .flat_map(|pixel| {
                let encode = |value: f32| {
                    let value = value.clamp(0.0, 1.0);
                    let srgb = if value <= 0.003_130_8 {
                        value * 12.92
                    } else {
                        1.055 * value.powf(1.0 / 2.4) - 0.055
                    };
                    (srgb * 255.0 + 0.5) as u8
                };

                [encode(pixel[0]), encode(pixel[1]), encode(pixel[2]), (pixel[3].clamp(0.0, 1.0) * 255.0 + 0.5) as u8]
            })
            .collect(),
    )
}

/// A half float's value.
#[cfg(feature = "render")]
fn half_to_f32(half: u16) -> f32 {
    let sign = if half & 0x8000 != 0 { -1.0 } else { 1.0 };
    let exponent = ((half >> 10) & 0x1F) as i32;
    let fraction = (half & 0x3FF) as f32;

    sign * match exponent {
        0 => fraction * 2f32.powi(-24),
        31 if fraction == 0.0 => f32::INFINITY,
        31 => f32::NAN,
        _ => (1.0 + fraction / 1024.0) * 2f32.powi(exponent - 15),
    }
}

/// Whether a camera draws into an image, which Bevy's screenshot of an image captures.
#[cfg(feature = "render")]
fn drawn_by_a_camera(
    world: &mut bevy::ecs::world::World,
    image: &bevy::asset::Handle<bevy::image::Image>,
) -> bool {
    use bevy::camera::RenderTarget;

    let offscreen = world
        .get_resource::<crate::app::OffscreenTarget>()
        .is_some_and(|target| target.image.id() == image.id());

    offscreen
        || world
            .query::<&RenderTarget>()
            .iter(world)
            .any(|target| matches!(target, RenderTarget::Image(target) if target.handle.id() == image.id()))
}

/// Reads an image back from the GPU as it is, and answers the capture's number.
///
/// The bytes arrive with each row padded to what a copy out of a texture needs, so they are
/// unpadded and kept in the image's own format, as a screenshot's are.
#[cfg(feature = "render")]
fn read_image_back(
    world: &mut bevy::ecs::world::World,
    image: bevy::asset::Handle<bevy::image::Image>,
) -> i32 {
    use bevy::asset::Assets;
    use bevy::ecs::system::{Commands, Res, ResMut};
    use bevy::image::{Image, TextureFormatPixelInfo};
    use bevy::render::gpu_readback::{Readback, ReadbackComplete};

    let mut captures = world.get_resource_or_init::<Captures>();
    captures.last += 1;
    let id = captures.last;

    let wanted = image.clone();

    world.spawn(Readback::texture(image)).observe(
        move |done: bevy::ecs::observer::On<ReadbackComplete>,
              images: Res<Assets<Image>>,
              mut captures: ResMut<Captures>,
              mut commands: Commands| {
            // A readback reads again every frame it is there, and only one answer was asked for.
            commands.entity(done.entity).despawn();

            let Some(source) = images.get(&wanted) else {
                return;
            };

            let size = source.texture_descriptor.size;
            let format = source.texture_descriptor.format;

            let Ok(texel) = format.pixel_size() else {
                bevy::log::warn!("Capture {id} is of a {format:?} image, which cannot be read back.");
                return;
            };

            let row = size.width as usize * texel;
            let padded = if size.height > 1 { row.next_multiple_of(256) } else { row };
            let mut bytes = Vec::with_capacity(row * size.height as usize);

            for line in done.data.chunks(padded).take(size.height as usize) {
                bytes.extend_from_slice(&line[..row.min(line.len())]);
            }

            captures.ready.insert(
                id,
                CapturedPixels {
                    width: size.width,
                    height: size.height,
                    format,
                    bytes,
                },
            );
        },
    );

    id
}

/// Reads a capture, and forgets it.
///
/// Follows the same shape as [`crate::app::bcs_render_adapter`]. A null `buffer` answers with the
/// number of bytes the picture needs, and a second call with a buffer that size copies it out. The
/// picture is dropped once it has been copied, because it is a megabyte or two and nothing here
/// knows when the caller would otherwise be done with it.
///
/// Answers [`status::INVALID_STATE`] while the picture is still coming back off the GPU, which is
/// the normal answer for the first frame or two, and [`status::NOT_PRESENT`] for a number that
/// names nothing.
///
/// # Safety
/// `width` and `height` must be writable or null, and `buffer` must be null or point to at least
/// `capacity` writable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_capture_read(
    id: i32,
    width: *mut u32,
    height: *mut u32,
    buffer: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (id, width, height, buffer, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Some(mut captures) = world.get_resource_mut::<Captures>() else {
                    return status::NOT_PRESENT;
                };

                let Some(picture) = captures.ready.get(&id) else {
                    // Either still on its way back or never asked for. The two are told apart by
                    // whether the number was ever handed out, which counting up makes cheap.
                    return if id > 0 && id <= captures.last {
                        status::INVALID_STATE
                    } else {
                        status::NOT_PRESENT
                    };
                };

                if !width.is_null() {
                    unsafe { width.write(picture.width) };
                }
                if !height.is_null() {
                    unsafe { height.write(picture.height) };
                }

                let needed = (picture.width * picture.height * 4) as i32;

                if buffer.is_null() {
                    return needed;
                }

                if capacity < needed {
                    return status::BUFFER_TOO_SMALL;
                }

                // Converted only when read this way, so a picture read as it is never pays for it. One with no
                // colors to read is kept, since it can still be read that way.
                let Some(rgba) = picture.to_rgba8() else {
                    bevy::log::warn!(
                        "Capture {id} is a {:?} picture, which cannot be read as colors.",
                        picture.format
                    );
                    return status::NO_COMPONENT;
                };

                unsafe { core::ptr::copy_nonoverlapping(rgba.as_ptr(), buffer, rgba.len()) };

                captures.ready.remove(&id);
                needed
            })
        }
    })
}

/// Reads a capture in the format it arrived in, and forgets it.
///
/// The same protocol as [`bcs_render_capture_read`], with `format` set to the number of the
/// picture's format among those a shader image is made in (`IMAGE_FORMATS`), so a half-float
/// target comes back as half floats rather than clamped to eight bits. An eight-bit picture comes
/// back as `0`, red, green, blue then alpha, as it does from the eight-bit read.
///
/// Answers [`status::NO_COMPONENT`] for a picture in a format that is not one of those, which is
/// kept, so the eight-bit read can still have it.
///
/// # Safety
/// `width`, `height` and `format` must be writable or null, and `buffer` must be null or point to
/// at least `capacity` writable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_capture_read_raw(
    id: i32,
    width: *mut u32,
    height: *mut u32,
    format: *mut i32,
    buffer: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (id, width, height, format, buffer, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Some(mut captures) = world.get_resource_mut::<Captures>() else {
                    return status::NOT_PRESENT;
                };

                let Some(picture) = captures.ready.get(&id) else {
                    return if id > 0 && id <= captures.last {
                        status::INVALID_STATE
                    } else {
                        status::NOT_PRESENT
                    };
                };

                let Some((number, bytes)) = picture.as_it_is() else {
                    return status::NO_COMPONENT;
                };

                if !width.is_null() {
                    unsafe { width.write(picture.width) };
                }
                if !height.is_null() {
                    unsafe { height.write(picture.height) };
                }
                if !format.is_null() {
                    unsafe { format.write(number) };
                }

                let needed = bytes.len() as i32;

                if buffer.is_null() {
                    return needed;
                }

                if capacity < needed {
                    return status::BUFFER_TOO_SMALL;
                }

                unsafe { core::ptr::copy_nonoverlapping(bytes.as_ptr(), buffer, bytes.len()) };

                captures.ready.remove(&id);
                needed
            })
        }
    })
}

/// Forgets a capture that will not be read.
///
/// For a caller that stopped waiting. A capture that never arrives holds nothing, and one that has
/// arrived holds its pixels until something drops them.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_capture_release(id: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = id;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| match world.get_resource_mut::<Captures>() {
                Some(mut captures) => {
                    captures.ready.remove(&id);
                    status::OK
                }
                None => status::OK,
            })
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use super::*;

    #[test]
    fn a_half_float_reads_as_its_value() {
        assert_eq!(half_to_f32(0x3C00), 1.0);
        assert_eq!(half_to_f32(0x4000), 2.0);
        assert_eq!(half_to_f32(0x3800), 0.5);
        assert_eq!(half_to_f32(0xC000), -2.0);
        assert_eq!(half_to_f32(0x0000), 0.0);
    }

    /// Brighter than white reads as white, linear middle gray as sRGB's, and alpha as it is.
    #[test]
    fn a_float_picture_becomes_eight_bit_srgb() {
        use bevy::render::render_resource::TextureFormat;

        let halves: [u16; 4] = [0x4000, 0x3800, 0x0000, 0x3C00];
        let bytes: Vec<u8> = halves.iter().flat_map(|half| half.to_le_bytes()).collect();

        let rgba = floats_to_rgba8(TextureFormat::Rgba16Float, &bytes).unwrap();
        assert_eq!(rgba, vec![255, 188, 0, 255]);

        assert!(floats_to_rgba8(TextureFormat::Rgba8Unorm, &[0; 4]).is_none());
    }
}

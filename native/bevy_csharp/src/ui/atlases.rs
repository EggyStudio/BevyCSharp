//! Bevy's font atlases, the textures each size of each font is drawn into as its glyphs are first
//! needed, counted and emptied as Bevy's text stress tests count and empty them.

use super::*;

/// Writes how many font atlases there are, then the bytes their images hold, to `out`, both zero
/// where no text has been drawn.
///
/// # Safety
/// `out` must be writable for two `u64`s.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_font_atlases(out: *mut u64) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::image::Image;
            use bevy::text::FontAtlasSet;

            with_world(|world| {
                let (count, bytes) = match (world.get_resource::<FontAtlasSet>(), world.get_resource::<Assets<Image>>()) {
                    (Some(set), Some(images)) => {
                        (set.values().map(|atlases| atlases.len() as u64).sum(), set.total_bytes(images))
                    }
                    _ => (0, 0),
                };

                unsafe {
                    out.write(count);
                    out.add(1).write(bytes);
                }
                status::OK
            })
        }
    })
}

/// Empties the font atlases, which Bevy fills again with the glyphs the frames after draw, as
/// Bevy's many_text does every frame to measure the drawing of glyphs into them.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_clear_font_atlases() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                if let Some(mut set) = world.get_resource_mut::<bevy::text::FontAtlasSet>() {
                    set.clear();
                }
                status::OK
            })
        }
    })
}

//! Bevy's font atlases, the textures each size of each font is drawn into as its glyphs are first
//! needed, counted and emptied as Bevy's text stress tests count and empty them, and their pictures
//! answered as Bevy's font_atlas_debug shows them.

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

/// Writes each font atlas's picture, as an asset key, and the size of the font its glyphs are drawn
/// at, up to `capacity` of them, the smaller sizes first and the atlases of a size in the order Bevy
/// opened them, and answers how many there are, whether or not they fitted, as Bevy's
/// font_atlas_debug shows them filling.
///
/// A picture read again answers the key it had, so a game reading them every frame takes no slot
/// a frame.
///
/// # Safety
/// `keys` and `sizes` must be writable for `capacity` values each, or null where `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_font_atlas_pictures(keys: *mut i32, sizes: *mut f32, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if capacity < 0 || (capacity > 0 && (keys.is_null() || sizes.is_null())) {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = (keys, sizes);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                // Bevy keeps the sets by a key it hashes, so they are put in an order of their own,
                // the same from one call to the next.
                let mut atlases: Vec<(f32, u32, u32, usize, bevy::asset::UntypedHandle)> = world
                    .get_resource::<bevy::text::FontAtlasSet>()
                    .map(|set| {
                        set.iter()
                            .flat_map(|(key, atlases)| {
                                atlases.iter().enumerate().map(move |(order, atlas)| {
                                    let size = f32::from_bits(key.font_size_bits);
                                    (size, key.id, key.index, order, atlas.texture.clone().untyped())
                                })
                            })
                            .collect()
                    })
                    .unwrap_or_default();
                atlases.sort_by(|a, b| {
                    a.0.total_cmp(&b.0).then(a.1.cmp(&b.1)).then(a.2.cmp(&b.2)).then(a.3.cmp(&b.3))
                });

                let total = atlases.len();
                for (slot, (size, _, _, _, picture)) in atlases.into_iter().take(capacity as usize).enumerate() {
                    let key = crate::assets::key_for(world, picture);
                    unsafe {
                        keys.add(slot).write(key);
                        sizes.add(slot).write(size);
                    }
                }

                i32::try_from(total).unwrap_or(i32::MAX)
            })
        }
    })
}

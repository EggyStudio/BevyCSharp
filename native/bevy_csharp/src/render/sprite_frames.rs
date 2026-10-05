//! Sprites moved through the frames of their sheets, many in one call.
//!
//! Without this a sprite whose frame turns is set again, a call a sprite, and an animation of
//! thousands of sprites turns thousands a frame, each one replacing the whole sprite. Here only the
//! index of each one's atlas is written, in place, as a Rust system moves it, for a sprite and a
//! sprite mesh alike.

use crate::interop::status;

/// Sets the atlas frame of each entity's sprite or sprite mesh, `count` of each, and answers how
/// many it set.
///
/// An entity that is gone, that has neither, or whose sprite shows no sheet is passed over rather
/// than refused, since an animation over many sprites outlives the odd one despawned under it.
///
/// # Safety
/// `entities` and `frames` must each point to `count` readable values, or may be null where
/// `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_sprite_frames(entities: *const u64, frames: *const u32, count: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entities, frames, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if count < 0 || (count > 0 && (entities.is_null() || frames.is_null())) {
                return status::NULL_ARG;
            }
            if count == 0 {
                return 0;
            }

            // SAFETY: the caller promises `count` readable values at each.
            let entities = unsafe { core::slice::from_raw_parts(entities, count as usize) };
            let frames = unsafe { core::slice::from_raw_parts(frames, count as usize) };

            crate::state::with_world(|world| {
                let mut set = 0;
                for (&bits, &frame) in entities.iter().zip(frames) {
                    let Ok(mut entity) = world.get_entity_mut(crate::ecs::entity_from(bits)) else {
                        continue;
                    };

                    // Read before it is reached mutably, so a sprite with no sheet is not marked
                    // changed and drawn again for nothing.
                    if entity.get::<bevy::sprite::Sprite>().is_some_and(|sprite| sprite.texture_atlas.is_some()) {
                        if let Some(mut sprite) = entity.get_mut::<bevy::sprite::Sprite>()
                            && let Some(atlas) = sprite.texture_atlas.as_mut()
                        {
                            atlas.index = frame as usize;
                            set += 1;
                        }
                    } else if entity.get::<bevy::sprite::SpriteMesh>().is_some_and(|mesh| mesh.texture_atlas.is_some())
                        && let Some(mut mesh) = entity.get_mut::<bevy::sprite::SpriteMesh>()
                        && let Some(atlas) = mesh.texture_atlas.as_mut()
                    {
                        atlas.index = frame as usize;
                        set += 1;
                    }
                }
                set
            })
        }
    })
}

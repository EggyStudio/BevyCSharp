//! Sprites, and how a sliced picture meets the size it is drawn at, for a sprite and an interface
//! node alike.

use crate::interop::{status, BcsSpriteConfig};

#[cfg(feature = "render")]
use crate::state::with_world;

/// Attaches a sprite to an entity, or replaces the one it has.
///
/// Position it by writing its `Transform`, like anything else in the world.
///
/// # Safety
/// `config` must point to a readable [`BcsSpriteConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_sprite(entity: u64, config: *const BcsSpriteConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::image::{Image, TextureAtlas, TextureAtlasLayout};
            use bevy::math::{Rect, Vec2};
            use bevy::sprite::{
                Anchor, BorderRect, Sprite, SpriteImageMode, SpriteScalingMode,
                TextureSlicer,
            };

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(image) = crate::assets::clone_handle(world, config.image) else {
                    return status::NO_COMPONENT;
                };

                // A negative key is "no atlas", which is the whole image. A key that names
                // nothing is a mistake rather than a default, so it is refused.
                let atlas = if config.atlas < 0 {
                    None
                } else {
                    let Some(layout) = crate::assets::clone_handle(world, config.atlas) else {
                        return status::NO_COMPONENT;
                    };

                    Some(TextureAtlas {
                        layout: layout.typed::<TextureAtlasLayout>(),
                        index: config.atlas_index as usize,
                    })
                };

                let slicer = TextureSlicer {
                    border: BorderRect {
                        min_inset: Vec2::new(config.slice_border[0], config.slice_border[1]),
                        max_inset: Vec2::new(config.slice_border[2], config.slice_border[3]),
                    },
                    center_scale_mode: slice_scale(config.slice_tiling & 2, config.tile_stretch),
                    sides_scale_mode: slice_scale(config.slice_tiling & 1, config.tile_stretch),
                    max_corner_scale: if config.corner_scale > 0.0 {
                        config.corner_scale
                    } else {
                        1.0
                    },
                };

                let image_mode = match config.mode {
                    1 => SpriteImageMode::Sliced(slicer),
                    2 => SpriteImageMode::Tiled {
                        tile_x: config.tile_x != 0,
                        tile_y: config.tile_y != 0,
                        stretch_value: if config.tile_stretch > 0.0 {
                            config.tile_stretch
                        } else {
                            1.0
                        },
                    },
                    3 => SpriteImageMode::Scale(match config.scaling {
                        1 => SpriteScalingMode::FitStart,
                        2 => SpriteScalingMode::FitEnd,
                        3 => SpriteScalingMode::FillCenter,
                        4 => SpriteScalingMode::FillStart,
                        5 => SpriteScalingMode::FillEnd,
                        _ => SpriteScalingMode::FitCenter,
                    }),
                    _ => SpriteImageMode::Auto,
                };

                let sprite = Sprite {
                    texture_atlas: atlas,
                    image_mode,
                    image: image.typed::<Image>(),
                    color: Color::linear_rgba(
                        config.color[0],
                        config.color[1],
                        config.color[2],
                        config.color[3],
                    ),
                    flip_x: config.flip_x != 0,
                    flip_y: config.flip_y != 0,
                    custom_size: (config.has_size != 0)
                        .then(|| Vec2::new(config.size[0], config.size[1])),
                    rect: (config.has_rect != 0).then(|| {
                        Rect::new(config.rect[0], config.rect[1], config.rect[2], config.rect[3])
                    }),
                    ..Default::default()
                };

                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity))
                else {
                    return status::NO_ENTITY;
                };

                // Bevy's own insert, so the components a sprite requires arrive with it.
                entity_mut.insert(sprite);

                // The anchor is its own component rather than a field on the sprite, so an
                // entity that was never given one keeps Bevy's centered default.
                if config.has_anchor != 0 {
                    entity_mut.insert(Anchor(Vec2::new(config.anchor[0], config.anchor[1])));
                }

                status::OK
            })
        }
    })
}

/// How one part of a sliced picture meets the size it is drawn at.
///
/// A nine-slice stretches by default, and it is wrong for anything with a pattern in it, because a
/// border of dots drawn twice as wide becomes a border of ovals. Tiling repeats the slice instead,
/// which keeps a drawn edge looking drawn at every size.
#[cfg(feature = "render")]
pub fn slice_scale(tiling: i32, stretch: f32) -> bevy::sprite::SliceScaleMode {
    use bevy::sprite::SliceScaleMode;

    if tiling == 0 {
        return SliceScaleMode::Stretch;
    }

    SliceScaleMode::Tile {
        // The same number a tiled picture measures its repeat by, since both answer how much of
        // the source is laid down before it starts again.
        stretch_value: if stretch > 0.0 { stretch } else { 1.0 },
    }
}

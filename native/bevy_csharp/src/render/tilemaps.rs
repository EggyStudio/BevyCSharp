//! Bevy's tilemap chunks, a grid of tiles drawn as one mesh from the layers of one image.
//!
//! A chunk is Bevy's `TilemapChunk` with its tiles in a `TilemapChunkTileData` beside it. The chunk
//! reaches C# through reflection, its size and its tileset fields like any other, but its tiles are
//! a list of optional structs, which reflection lists no field of, so they cross here as an array of
//! [`BcsTile`]. Bevy makes the chunk's mesh and material in a hook as the chunk is inserted and
//! reads the tiles there, so the two are inserted together, and a chunk is changed afterwards by
//! writing over its tiles, which Bevy's own system turns into the chunk's tile image again.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// One tile as C# describes it.
///
/// `color` is linear RGBA, the tint the tile's layer of the tileset is multiplied by. `orientation`
/// is Bevy's `TileOrientation` by its value, three bits for a mirror across each axis and the
/// diagonal. `flags` has `1` set for a tile that is there at all, which a cell with nothing in it
/// leaves clear, and `2` for one that is drawn.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsTile {
    pub color: [f32; 4],
    pub tileset_index: u16,
    pub orientation: u8,
    pub flags: u8,
}

/// A chunk as C# describes it, its size in tiles, the size each tile is drawn at, the tileset's
/// image key, and how its alpha is read, `0` opaque, `1` masked at `alpha_cutoff` and `2` blended.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsTilemapChunk {
    pub chunk_size: [u32; 2],
    pub tile_display_size: [u32; 2],
    pub tileset: i32,
    pub alpha_mode: i32,
    pub alpha_cutoff: f32,
}

#[cfg(feature = "render")]
const PRESENT: u8 = 1;
#[cfg(feature = "render")]
const VISIBLE: u8 = 2;

/// A tile as Bevy keeps it, or nothing for an empty cell or an orientation that is none of Bevy's.
#[cfg(feature = "render")]
fn tile_from(tile: &BcsTile) -> Result<Option<bevy::sprite_render::TileData>, i32> {
    use bevy::color::{Color, LinearRgba};
    use bevy::sprite_render::{TileData, TileOrientation};

    if tile.flags & PRESENT == 0 {
        return Ok(None);
    }

    let orientation = match tile.orientation {
        0b000 => TileOrientation::Default,
        0b011 => TileOrientation::Rotate90,
        0b110 => TileOrientation::Rotate180,
        0b101 => TileOrientation::Rotate270,
        0b100 => TileOrientation::MirrorH,
        0b001 => TileOrientation::MirrorHRotate90,
        0b010 => TileOrientation::MirrorHRotate180,
        0b111 => TileOrientation::MirrorHRotate270,
        _ => return Err(status::INVALID_STATE),
    };

    let [r, g, b, a] = tile.color;
    Ok(Some(TileData {
        tileset_index: tile.tileset_index,
        color: Color::LinearRgba(LinearRgba::new(r, g, b, a)),
        visible: tile.flags & VISIBLE != 0,
        orientation,
    }))
}

/// A tile as C# reads it.
#[cfg(feature = "render")]
fn tile_to(tile: Option<&bevy::sprite_render::TileData>) -> BcsTile {
    let Some(tile) = tile else {
        return BcsTile::default();
    };

    let linear = tile.color.to_linear();
    BcsTile {
        color: [linear.red, linear.green, linear.blue, linear.alpha],
        tileset_index: tile.tileset_index,
        orientation: tile.orientation as u8,
        flags: PRESENT | if tile.visible { VISIBLE } else { 0 },
    }
}

/// The tiles `count` of them at `tiles` make, or the status refusing them.
///
/// # Safety
/// `tiles` must be readable for `count` tiles, or null when `count` is zero.
#[cfg(feature = "render")]
unsafe fn tiles_from(
    tiles: *const BcsTile,
    count: i32,
) -> Result<Vec<Option<bevy::sprite_render::TileData>>, i32> {
    if count < 0 || (tiles.is_null() && count > 0) {
        return Err(status::NULL_ARG);
    }

    if count == 0 {
        return Ok(Vec::new());
    }

    let tiles = unsafe { core::slice::from_raw_parts(tiles, count as usize) };
    tiles.iter().map(tile_from).collect()
}

/// Makes `entity` a tilemap chunk with its tiles, Bevy's `TilemapChunk` and `TilemapChunkTileData`.
///
/// The tiles are row after row from the chunk's bottom row up, `chunk_size` across and as many as
/// the chunk holds, which Bevy needs to make the chunk at all and refuses otherwise. Inserted again,
/// the chunk is made again with its mesh and material, as Bevy makes an immutable component's.
///
/// # Safety
/// `chunk` must point at one readable [`BcsTilemapChunk`], and `tiles` at `count` tiles.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_tilemap_insert(
    entity: u64,
    chunk: *const BcsTilemapChunk,
    tiles: *const BcsTile,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, chunk, tiles, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::math::UVec2;
            use bevy::sprite_render::{AlphaMode2d, TilemapChunk, TilemapChunkTileData};

            if chunk.is_null() {
                return status::NULL_ARG;
            }

            let chunk = unsafe { *chunk };
            let entity = bevy::ecs::entity::Entity::from_bits(entity);
            let tiles = match unsafe { tiles_from(tiles, count) } {
                Ok(tiles) => tiles,
                Err(refusal) => return refusal,
            };

            let [width, height] = chunk.chunk_size;
            if tiles.len() as u64 != width as u64 * height as u64 {
                return status::BUFFER_TOO_SMALL;
            }

            let alpha_mode = match chunk.alpha_mode {
                0 => AlphaMode2d::Opaque,
                1 => AlphaMode2d::Mask(chunk.alpha_cutoff),
                2 => AlphaMode2d::Blend,
                _ => return status::INVALID_STATE,
            };

            with_world(|world| {
                // A run that draws nothing, headless, has no tilemap plugin, and Bevy's hook would
                // ask it for the chunk's mesh cache and panic.
                if !world.contains_resource::<bevy::sprite_render::TilemapChunkMeshCache>() {
                    return status::UNSUPPORTED;
                }

                let tileset = match super::image_handle(world, chunk.tileset) {
                    Ok(Some(tileset)) => tileset,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };

                let Ok(mut found) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                // Together, since the chunk's hook reads the tiles as the chunk goes in.
                found.insert((
                    TilemapChunkTileData(tiles),
                    TilemapChunk {
                        chunk_size: UVec2::from_array(chunk.chunk_size),
                        tile_display_size: UVec2::from_array(chunk.tile_display_size),
                        tileset,
                        alpha_mode,
                    },
                ));
                status::OK
            })
        }
    })
}

/// Writes `count` tiles over a chunk's tiles from `start` on, which Bevy draws from the next frame.
///
/// Refused past the chunk's last tile, and on an entity that is no chunk.
///
/// # Safety
/// `tiles` must be readable for `count` tiles.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_tilemap_write(entity: u64, start: i32, tiles: *const BcsTile, count: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, start, tiles, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::sprite_render::TilemapChunkTileData;

            let entity = bevy::ecs::entity::Entity::from_bits(entity);
            let tiles = match unsafe { tiles_from(tiles, count) } {
                Ok(tiles) => tiles,
                Err(refusal) => return refusal,
            };

            with_world(|world| {
                let Ok(mut found) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                let Some(mut data) = found.get_mut::<TilemapChunkTileData>() else {
                    return status::NO_COMPONENT;
                };

                let Some(range) = range(start, tiles.len(), data.0.len()) else {
                    return status::BUFFER_TOO_SMALL;
                };

                // Written through the guard, which marks the tiles changed for Bevy's system.
                data.0[range].copy_from_slice(&tiles);
                status::OK
            })
        }
    })
}

/// Reads `count` of a chunk's tiles from `start` on into `out`, an empty cell as a tile without
/// its present bit.
///
/// # Safety
/// `out` must be writable for `count` tiles.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_tilemap_read(entity: u64, start: i32, out: *mut BcsTile, count: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, start, out, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::sprite_render::TilemapChunkTileData;

            if count < 0 || (out.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            let entity = bevy::ecs::entity::Entity::from_bits(entity);
            with_world(|world| {
                let Ok(found) = world.get_entity(entity) else {
                    return status::NO_ENTITY;
                };

                let Some(data) = found.get::<TilemapChunkTileData>() else {
                    return status::NO_COMPONENT;
                };

                let Some(range) = range(start, count as usize, data.0.len()) else {
                    return status::BUFFER_TOO_SMALL;
                };

                for (offset, tile) in data.0[range].iter().enumerate() {
                    unsafe { out.add(offset).write(tile_to(tile.as_ref())) };
                }

                status::OK
            })
        }
    })
}

/// The tiles from `start` for `count`, or nothing where they run past `length`.
#[cfg(feature = "render")]
fn range(start: i32, count: usize, length: usize) -> Option<core::ops::Range<usize>> {
    let start = usize::try_from(start).ok()?;
    let end = start.checked_add(count)?;
    (end <= length).then_some(start..end)
}

//! An atlas packed from images as the app runs, as Bevy's `TextureAtlasBuilder` packs one, which
//! Bevy's texture_atlas builds from a folder of sprites.

use bevy::asset::Assets;
use bevy::image::{Image, TextureAtlasBuilder, TextureAtlasLayout};
use bevy::math::UVec2;

use crate::interop::status;
use crate::state::with_world;

/// Packs the `count` images `images` names into one, `padding` pixels apart, as Bevy's
/// `TextureAtlasBuilder` does, and writes the packed image's key to `out_image`, its layout's to
/// `out_layout`, and each image's place in the layout to `out_indices`, in the order given.
///
/// The packed image is a copy, so changing an image packed into it later changes nothing in it.
/// Answers [`status::NOT_PRESENT`] while an image is still loading, [`status::NO_COMPONENT`] for a
/// key that names no image, and [`status::INVALID_STATE`] where they do not fit the largest atlas
/// Bevy packs.
///
/// # Safety
/// `images` and `out_indices` must hold `count` items each, and `out_image` and `out_layout` must
/// be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_atlas_build(
    images: *const i32,
    count: i32,
    padding_x: u32,
    padding_y: u32,
    out_image: *mut i32,
    out_layout: *mut i32,
    out_indices: *mut i32,
) -> i32 {
    crate::interop::guard(|| {
        if images.is_null() || out_image.is_null() || out_layout.is_null() || out_indices.is_null() || count <= 0 {
            return status::NULL_ARG;
        }
        let keys = unsafe { core::slice::from_raw_parts(images, count as usize) };

        with_world(|world| {
            let mut ids = Vec::with_capacity(keys.len());
            for key in keys {
                let Some(handle) = super::clone_handle(world, *key).and_then(|handle| handle.try_typed::<Image>().ok())
                else {
                    return status::NO_COMPONENT;
                };
                ids.push(handle.id());
            }

            let (layout, sources, packed) = {
                let Some(textures) = world.get_resource::<Assets<Image>>() else {
                    return status::UNSUPPORTED;
                };
                let mut builder = TextureAtlasBuilder::default();
                builder.padding(UVec2::new(padding_x, padding_y));
                for id in &ids {
                    let Some(texture) = textures.get(*id) else {
                        return status::NOT_PRESENT;
                    };
                    builder.add_texture(Some(*id), texture);
                }
                match builder.build() {
                    Ok(built) => built,
                    Err(_) => return status::INVALID_STATE,
                }
            };

            for (place, id) in ids.iter().enumerate() {
                let index = sources.texture_index(*id).map_or(-1, |index| index as i32);
                unsafe { out_indices.add(place).write(index) };
            }

            let packed = world.resource_mut::<Assets<Image>>().add(packed);
            let layout = world.resource_mut::<Assets<TextureAtlasLayout>>().add(layout);
            let (packed, layout) = (super::key_for(world, packed.untyped()), super::key_for(world, layout.untyped()));
            unsafe {
                out_image.write(packed);
                out_layout.write(layout);
            }
            status::OK
        })
    })
}

#[cfg(test)]
mod tests {
    use bevy::asset::{Assets, RenderAssetUsages};
    use bevy::ecs::world::World;
    use bevy::image::{Image, TextureAtlasLayout};
    use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat};

    use crate::interop::status;
    use crate::state::loan_world;

    fn image(width: u32, height: u32) -> Image {
        Image::new_fill(
            Extent3d { width, height, depth_or_array_layers: 1 },
            TextureDimension::D2,
            &[255, 0, 0, 255],
            TextureFormat::Rgba8UnormSrgb,
            RenderAssetUsages::default(),
        )
    }

    /// Two images packed with padding between them, each found again by its place in the layout,
    /// and the packed image wide or tall enough for both and the gap.
    #[test]
    fn two_images_are_packed_with_their_padding_and_each_is_placed() {
        let mut world = World::new();
        world.init_resource::<Assets<Image>>();
        world.init_resource::<Assets<TextureAtlasLayout>>();
        let (wide, tall) = {
            let mut images = world.resource_mut::<Assets<Image>>();
            (images.add(image(16, 8)), images.add(image(8, 16)))
        };
        let keys = [
            crate::assets::key_for(&mut world, wide.untyped()),
            crate::assets::key_for(&mut world, tall.untyped()),
        ];

        let (mut packed, mut layout, mut indices) = (0, 0, [0i32; 2]);
        let answer = loan_world(&mut world, || unsafe {
            super::bcs_atlas_build(keys.as_ptr(), 2, 4, 4, &mut packed, &mut layout, indices.as_mut_ptr())
        });
        assert_eq!(answer, status::OK);

        let mut placed = indices.to_vec();
        placed.sort();
        assert_eq!(placed, [0, 1]);

        let layout = crate::assets::clone_handle(&world, layout).unwrap().typed::<TextureAtlasLayout>();
        let layout = world.resource::<Assets<TextureAtlasLayout>>().get(&layout).unwrap();
        assert_eq!(layout.textures.len(), 2);
        let (first, second) = (layout.textures[indices[0] as usize], layout.textures[indices[1] as usize]);
        assert_eq!(first.size(), bevy::math::UVec2::new(16, 8));
        assert_eq!(second.size(), bevy::math::UVec2::new(8, 16));

        // Apart by the padding at least, one way or the other.
        let gap_x = second.min.x.saturating_sub(first.max.x).max(first.min.x.saturating_sub(second.max.x));
        let gap_y = second.min.y.saturating_sub(first.max.y).max(first.min.y.saturating_sub(second.max.y));
        assert!(gap_x >= 4 || gap_y >= 4, "{first:?} and {second:?} are not four pixels apart");

        let packed = crate::assets::clone_handle(&world, packed).unwrap().typed::<Image>();
        let packed = world.resource::<Assets<Image>>().get(&packed).unwrap();
        assert!(packed.width() >= first.max.x.max(second.max.x) && packed.height() >= first.max.y.max(second.max.y));
    }
}

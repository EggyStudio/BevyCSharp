use bevy::asset::RenderAssetUsages;
use bevy::image::Image;
use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat};

use super::{Mean, alpha_where, bc1, bc4, bc7, mean_of};

/// One block of each kind, random but for a BC7 block's mode, with the sixteen texels Pillow's
/// decoder reads from it, red, green, blue and alpha.
const BLOCKS: [(&str, &str, &str); 12] = [
    ("bc1", "a54dca182530bb1d", "181852ff181852ff398136ff4ab629ff4ab629ff4ab629ff284c44ff4ab629ff284c44ff398136ff284c44ff398136ff181852ff284c44ff181852ff4ab629ff"),
    ("bc3", "d91e3f721fcb1971174494d6493c9d5c", "d6d3a538739db5384282bdd9d6d3a51e4282bd38a4b8ad53a4b8ad384282bdd9d6d3a5a3a4b8ad1ed6d3a538739db5884282bd1ea4b8adbed6d3a588d6d3a5a3"),
    ("bc4", "7c2999fdafe59325", "292929ff646464ff404040ff404040ff343434ff343434ff646464ff4c4c4cff4c4c4cff585858ff343434ff292929ff292929ff646464ff292929ff292929ff"),
    ("bc5", "27a0aeb3fee9232f8af2211f9ee491c5", "00f200ff87c800ff00c800ffa0ff00ff57f200ff87c800ffffff00ffffc800ffa0c800ff87c800ffffff00ffa08a00ff3ff200ff00b300ff57f200ffa00000ff"),
    ("bc7 mode 0", "1bfb962a02f2fd727628d2661118a88c", "a11682ffac7fa2ff51ea41ff4ff13effc1128dff730031ff4cf83cff4aff39ffb71389ff7c1544ff4aff39ff4cf83cffc1128dff99567dff4bfc3aff4cf83cff"),
    ("bc7 mode 1", "b2aa366e09162edd5f30db8c4d9a4647", "99379dff77472aff8a7a1aff735aecff8f8716ff903fb0ff903fb0ff866e1eff866e1eff903fb0ff99379dff816122ff8549c5ff733b2eff8a7a1aff99379dff"),
    ("bc7 mode 2", "24f0c6ae05765ae2c99964615ddf2df5", "ce555dffce555dffce555dffde3931ffde3931ffce555dffde3931ffce555dffab606dffab606dff4bb391ff287ea0ff8e5592ff8e5592ff084aadff4bb391ff"),
    ("bc7 mode 3", "b87dbb267686613b898c94a8eae9bb3b", "be3244ffbd2a5cff7a6250ffd94fa3ffbd2a5cffab597bffab597bffbb198dffbb198dffab597bffd94fa3ffbc2175ffd94fa3ffab597bffbb198dffbe3244ff"),
    ("bc7 mode 4", "d005ae2ec55c4343bc9c2c4859470c01", "43cf6f846acf35c529d4945a5ed448b05ed248b043cf6f8477d423da43cf6f8484d410ef29d7945a36d2816f77d223da29d4945a43d76f8429cf945a29d2945a"),
    ("bc7 mode 5", "e0aa1c9c11bdfb90f5889051c039fef3", "5e9f3ebc685a3ed672183eef5e9f64bc54e14aa35e9f58bc54e164a35e9f3ebc54e158a3685a64d654e164a3721864ef54e164a3685a3ed6685a64d654e164a3"),
    ("bc7 mode 6", "40e041f3f48e1a9ec2e1aba7db721bad", "7a3cb61d25895e357a3cb61d15974d3a2d826633347c6d314b66852a347c6d312d8266331e8f57377144ac204b66852a2d8266337a3cb61d1e8f5737347c6d31"),
    ("bc7 mode 7", "8083f8de9114acc7b6d0aef39318e0df", "1424f7a6b240d7d71424f7a6cc4489d41424f7a6ff4dc7eff70cb6d7f70cb6d71424f7a61424f7a675b62ccf75b62ccfff4dc7ef75b62ccfa07e59d2cc4489d4"),
];

fn bytes(hex: &str) -> Vec<u8> {
    (0..hex.len() / 2).map(|i| u8::from_str_radix(&hex[2 * i..2 * i + 2], 16).unwrap()).collect()
}

#[test]
fn each_kind_of_block_reads_as_pillow_reads_it() {
    for (kind, block, texels) in BLOCKS {
        let (block, texels) = (bytes(block), bytes(texels));
        let ours: [[u8; 4]; 16] = match kind {
            "bc1" => bc1(&block, false),
            "bc3" => {
                let (alpha, color) = (bc4(&block[..8]), bc1(&block[8..], true));
                std::array::from_fn(|i| [color[i][0], color[i][1], color[i][2], alpha[i]])
            }
            // Pillow reads one channel as gray and two with no blue.
            "bc4" => bc4(&block).map(|red| [red, red, red, 255]),
            "bc5" => {
                let (red, green) = (bc4(&block[..8]), bc4(&block[8..]));
                std::array::from_fn(|i| [red[i], green[i], 0, 255])
            }
            _ => bc7(&block),
        };
        // BC7 to the level, the others to one, where Pillow rounds the steps between the ends the
        // other way.
        let allowed = if kind.starts_with("bc7") { 0 } else { 1 };
        for (i, texel) in ours.iter().enumerate() {
            for channel in 0..4 {
                let theirs = texels[i * 4 + channel];
                assert!(texel[channel].abs_diff(theirs) <= allowed, "{kind}, texel {i}, channel {channel}: {} against {theirs}", texel[channel]);
            }
        }
    }
}

fn image(width: u32, height: u32, data: Vec<u8>, format: TextureFormat) -> Image {
    let mut image = Image::new_fill(
        Extent3d { width, height, depth_or_array_layers: 1 },
        TextureDimension::D2,
        &[0, 0, 0, 0],
        TextureFormat::Rgba8Unorm,
        RenderAssetUsages::default(),
    );
    image.texture_descriptor.format = format;
    image.data = Some(data);
    image
}

#[test]
fn an_srgb_color_is_averaged_as_linear_light() {
    // Half the texels at 188, about half of white's light, and half black.
    let data = [188, 188, 188, 255, 0, 0, 0, 255].repeat(2);
    let mean = mean_of(&image(2, 2, data, TextureFormat::Rgba8UnormSrgb)).unwrap();
    let half = ((188.0 / 255.0 + 0.055f32) / 1.055).powf(2.4) / 2.0;
    for channel in mean.all {
        assert!((channel - half).abs() < 1e-6, "{channel} against {half}");
    }
}

#[test]
fn a_map_is_averaged_as_its_levels_and_a_texel_by_its_alpha() {
    // A solid red texel beside a clear black one.
    let data = [255, 0, 0, 255, 0, 0, 0, 0].repeat(2);
    let mean = mean_of(&image(2, 2, data, TextureFormat::Rgba8Unorm)).unwrap();
    assert_eq!(mean, Mean { all: [0.5, 0.0, 0.0], solid: [1.0, 0.0, 0.0] });
}

#[test]
fn a_large_image_is_averaged_over_its_level_of_64_texels_a_side() {
    // A white top level of 128 by 128 over a black level of 64 by 64.
    let mut data = vec![255; 128 * 128 * 4];
    data.extend(vec![0; 64 * 64 * 4]);
    let mut large = image(128, 128, data, TextureFormat::Rgba8Unorm);
    large.texture_descriptor.mip_level_count = 2;
    assert_eq!(mean_of(&large).unwrap().all, [0.0; 3]);

    // With no levels under it, 64 texels along each row and column of the white one.
    large.texture_descriptor.mip_level_count = 1;
    assert_eq!(mean_of(&large).unwrap().all, [1.0; 3]);
}

#[test]
fn a_bc7_texture_is_averaged_from_its_blocks() {
    // Two blocks side by side, each of one color in mode 6, its ends alike and every index 0.
    let solid = |r: u128, g: u128, b: u128| {
        let mut bits: u128 = 1 << 6;
        let mut at = 7;
        for value in [r, r, g, g, b, b, 127, 127] {
            bits |= value << at;
            at += 7;
        }
        // Both ends' p-bits clear, so each channel is its seven bits and a 0, 127 reading 254.
        bits.to_le_bytes()
    };
    let mut data = solid(127, 0, 0).to_vec();
    data.extend(solid(0, 0, 127));
    let mean = mean_of(&image(8, 4, data, TextureFormat::Bc7RgbaUnorm)).unwrap();
    for (channel, expected) in mean.all.into_iter().zip([127.0 / 255.0, 0.0, 127.0 / 255.0]) {
        assert!((channel - expected).abs() < 1e-6, "{channel} against {expected}");
    }
    assert!(mean_of(&image(8, 4, vec![0; 32], TextureFormat::Rgba16Float)).is_none());
}

#[test]
fn a_transmission_is_averaged_over_the_texels_its_base_color_shows() {
    // A base color whose left texel shows and whose right one is mostly clear, over a transmission
    // texture twice as wide whose alpha is 204 under the left texel and 0 under the right.
    let shown = image(2, 1, vec![255, 255, 255, 255, 255, 255, 255, 100], TextureFormat::Rgba8Unorm);
    let through = image(4, 1, [255, 255, 255, 204, 255, 255, 255, 204, 255, 255, 255, 0, 255, 255, 255, 0].to_vec(), TextureFormat::Rgba8Unorm);
    let leaf = 204.0 / 255.0;

    // Cut out at a half, the left texel alone.
    let cut = alpha_where(&through, &shown, Some(0.5)).unwrap();
    assert!((cut - leaf).abs() < 1e-6, "{cut} against {leaf}");

    // Blended, each by how much it shows.
    let blended = alpha_where(&through, &shown, None).unwrap();
    let expected = leaf / (1.0 + 100.0 / 255.0);
    assert!((blended - expected).abs() < 1e-6, "{blended} against {expected}");

    // A base color that shows nothing gives no share.
    let clear = image(2, 1, vec![0; 8], TextureFormat::Rgba8Unorm);
    assert_eq!(alpha_where(&through, &clear, Some(0.5)), None);
}

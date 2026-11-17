//! The mean of a texture, which global illumination's field paints a surface with where the picture
//! paints it the texture, since the field holds one color a mesh and reads no texel.
//!
//! A material's base color, its metal and roughness and its emission are each a factor times a
//! texel, and a scene made to be drawn, as the scene packs are, holds its colors in its textures
//! and leaves its factors white and its metal at 1 for its map to say. Painted by its factors
//! alone, each such surface stands white and full metal in the field, so a ray's hit takes no
//! diffuse light off it and every wall sends back the same gray. Each texture a material gathered
//! for the field draws is averaged the first time it is met once it has loaded, and kept by its
//! asset until Bevy says it changed or went. A color's mean is its linear light where its format is
//! sRGB, and a map's, as a metallic and roughness map is, its levels as they are.
//!
//! A material's diffuse transmission is a factor times its texture's alpha, and the share the field
//! carries is that alpha's mean over the texels its base color shows, since a leaf's card lets its
//! light through only where the leaf is (`alpha_where`).
//!
//! The mean is taken over the level of the image's mipmaps of 64 texels a side or fewer where it
//! has one, and over 64 texels along each row and column of the level it stops at otherwise, so a
//! large texture costs no more than a small one. The formats read are those of eight bits a channel
//! and the blocks BC1, BC3, BC4, BC5 and BC7, which the scene packs' KTX2 files hold. An image of
//! another format, or one whose data the main world does not keep, leaves its material at its
//! factor.

use std::collections::HashMap;

use bevy::asset::{AssetId, Assets};
use bevy::image::Image;
use bevy::render::render_resource::{TextureDataOrder, TextureFormat};

#[cfg(test)]
mod tests;

/// Each image's mean by its asset, measured once, none where its format or data is not read, and
/// each image's alpha over the texels another shows (`alpha_where`), by the two and the cutoff.
#[derive(Default)]
pub struct TextureMeans {
    means: HashMap<AssetId<Image>, Option<Mean>>,
    shares: HashMap<(AssetId<Image>, AssetId<Image>, Option<u32>), Option<f32>>,
}

/// A texture's mean, from 0 to 1 a channel, of every texel alike and of each texel by its alpha.
#[derive(Clone, Copy, Debug, PartialEq)]
pub struct Mean {
    pub all: [f32; 3],
    pub solid: [f32; 3],
}

impl TextureMeans {
    /// The mean of an image, measured the first time it is asked for once the image has loaded, and
    /// none while it loads.
    pub fn of(&mut self, images: Option<&Assets<Image>>, id: AssetId<Image>) -> Option<Mean> {
        if let Some(&kept) = self.means.get(&id) {
            return kept;
        }
        let image = images?.get(id)?;
        let mean = mean_of(image);
        self.means.insert(id, mean);
        mean
    }

    /// The mean of an image's alpha over the texels another shows (`alpha_where`), measured the first
    /// time it is asked for once both have loaded, and none while either loads.
    pub fn alpha_where(&mut self, images: Option<&Assets<Image>>, id: AssetId<Image>, shown: AssetId<Image>, cutoff: Option<f32>) -> Option<f32> {
        let key = (id, shown, cutoff.map(f32::to_bits));
        if let Some(&kept) = self.shares.get(&key) {
            return kept;
        }
        let images = images?;
        let share = alpha_where(images.get(id)?, images.get(shown)?, cutoff);
        self.shares.insert(key, share);
        share
    }

    /// Forgets what was measured of an image, so it is measured again as it next loads.
    pub fn forget(&mut self, id: AssetId<Image>) {
        self.means.remove(&id);
        self.shares.retain(|&(image, shown, _), _| image != id && shown != id);
    }
}

/// How an image's texels are kept, eight bits a channel in the order given or blocks of four by
/// four, and whether a color's levels are sRGB.
#[derive(Clone, Copy, PartialEq)]
enum Kind {
    Texels([usize; 4], usize),
    Bc1,
    Bc3,
    Bc4,
    Bc5,
    Bc7,
}

// The channels of each texel's bytes that red, green, blue and alpha are, a channel past the bytes
// reading 0, and alpha's 1.
const NONE: usize = 4;

fn kind_of(format: TextureFormat) -> Option<(Kind, bool)> {
    use TextureFormat as F;
    Some(match format {
        F::Rgba8Unorm => (Kind::Texels([0, 1, 2, 3], 4), false),
        F::Rgba8UnormSrgb => (Kind::Texels([0, 1, 2, 3], 4), true),
        F::Bgra8Unorm => (Kind::Texels([2, 1, 0, 3], 4), false),
        F::Bgra8UnormSrgb => (Kind::Texels([2, 1, 0, 3], 4), true),
        F::Rg8Unorm => (Kind::Texels([0, 1, NONE, NONE], 2), false),
        F::R8Unorm => (Kind::Texels([0, NONE, NONE, NONE], 1), false),
        F::Bc1RgbaUnorm => (Kind::Bc1, false),
        F::Bc1RgbaUnormSrgb => (Kind::Bc1, true),
        F::Bc3RgbaUnorm => (Kind::Bc3, false),
        F::Bc3RgbaUnormSrgb => (Kind::Bc3, true),
        F::Bc4RUnorm => (Kind::Bc4, false),
        F::Bc5RgUnorm => (Kind::Bc5, false),
        F::Bc7RgbaUnorm => (Kind::Bc7, false),
        F::Bc7RgbaUnormSrgb => (Kind::Bc7, true),
        _ => return None,
    })
}

impl Kind {
    /// The bytes of a level of a size.
    fn bytes(self, width: usize, height: usize) -> usize {
        match self {
            Kind::Texels(_, size) => width * height * size,
            Kind::Bc1 | Kind::Bc4 => width.div_ceil(4) * height.div_ceil(4) * 8,
            Kind::Bc3 | Kind::Bc5 | Kind::Bc7 => width.div_ceil(4) * height.div_ceil(4) * 16,
        }
    }

    /// The texels of the block at a place, row by row.
    fn block(self, block: &[u8]) -> [[u8; 4]; 16] {
        match self {
            Kind::Texels(..) => [[0; 4]; 16],
            Kind::Bc1 => bc1(block, false),
            Kind::Bc3 => {
                let (alpha, color) = (bc4(&block[..8]), bc1(&block[8..], true));
                std::array::from_fn(|i| [color[i][0], color[i][1], color[i][2], alpha[i]])
            }
            Kind::Bc4 => bc4(block).map(|red| [red, 0, 0, 255]),
            Kind::Bc5 => {
                let (red, green) = (bc4(&block[..8]), bc4(&block[8..]));
                std::array::from_fn(|i| [red[i], green[i], 0, 255])
            }
            Kind::Bc7 => bc7(block),
        }
    }
}

/// A level from 0 to 255 as linear light, as sRGB decodes it.
fn linear(level: u8) -> f32 {
    let v = f32::from(level) / 255.0;
    if v <= 0.04045 { v / 12.92 } else { ((v + 0.055) / 1.055).powf(2.4) }
}

/// The level of an image's mipmaps a mean is taken over, the one of 64 texels a side or fewer where
/// it has one and the largest otherwise, read a texel at a time.
struct Level<'a> {
    kind: Kind,
    srgb: bool,
    width: usize,
    height: usize,
    texels: &'a [u8],
    // The block read last, by its place, since the texels read lie row by row.
    kept: (usize, [[u8; 4]; 16]),
}

impl<'a> Level<'a> {
    /// The level of an image, none where its format is not read or its data is not kept.
    fn of(image: &'a Image) -> Option<Self> {
        let data = image.data.as_ref()?;
        let (kind, srgb) = kind_of(image.texture_descriptor.format)?;
        let size = image.texture_descriptor.size;
        let (width, height) = (size.width as usize, size.height as usize);
        let levels = image.texture_descriptor.mip_level_count.max(1) as usize;
        // Layer by layer each level of the first layer follows the one above it, and level by level
        // each level of every layer does.
        let layers = match image.data_order {
            TextureDataOrder::LayerMajor => 1,
            _ => size.depth_or_array_layers.max(1) as usize,
        };
        let (mut level, mut start) = (0, 0);
        while level + 1 < levels && (width >> level).max(height >> level) > 64 {
            start += kind.bytes((width >> level).max(1), (height >> level).max(1)) * layers;
            level += 1;
        }
        let (width, height) = ((width >> level).max(1), (height >> level).max(1));
        let texels = data.get(start..start + kind.bytes(width, height))?;
        Some(Self { kind, srgb, width, height, texels, kept: (usize::MAX, [[0; 4]; 16]) })
    }

    /// The places of at most 64 texels along each row and column, each in the middle of its share,
    /// row by row.
    fn places(&self) -> impl Iterator<Item = (usize, usize)> + use<> {
        let (width, height) = (self.width, self.height);
        let (across, down) = (width.min(64), height.min(64));
        (0..down).flat_map(move |row| (0..across).map(move |column| ((2 * column + 1) * width / (2 * across), (2 * row + 1) * height / (2 * down))))
    }

    /// The texel at a place, red, green, blue and alpha from 0 to 255, a channel the format lacks
    /// reading 0 and its alpha 255.
    fn texel(&mut self, x: usize, y: usize) -> [u8; 4] {
        match self.kind {
            Kind::Texels(order, size) => {
                let at = (y * self.width + x) * size;
                order.map(|channel| if channel == NONE { 0 } else { self.texels[at + channel] })
            }
            kind => {
                let block = (y / 4) * self.width.div_ceil(4) + x / 4;
                if self.kept.0 != block {
                    let bytes = kind.bytes(4, 4);
                    self.kept = (block, kind.block(&self.texels[block * bytes..(block + 1) * bytes]));
                }
                self.kept.1[(y % 4) * 4 + x % 4]
            }
        }
    }

    /// The texel at a place as light or as levels from 0 to 1, red, green and blue, and its alpha.
    fn read(&mut self, x: usize, y: usize) -> ([f32; 3], f32) {
        let texel = self.texel(x, y);
        let alpha = match self.kind {
            Kind::Texels(order, _) if order[3] == NONE => 1.0,
            _ => f32::from(texel[3]) / 255.0,
        };
        let srgb = self.srgb;
        (std::array::from_fn(|channel| if srgb { linear(texel[channel]) } else { f32::from(texel[channel]) / 255.0 }), alpha)
    }
}

/// An image's mean, none where its format is not read or its data is not kept.
pub fn mean_of(image: &Image) -> Option<Mean> {
    let mut level = Level::of(image)?;
    let (mut all, mut solid, mut covered, mut count) = ([0f32; 3], [0f32; 3], 0f32, 0f32);
    for (x, y) in level.places() {
        let (color, alpha) = level.read(x, y);
        for channel in 0..3 {
            all[channel] += color[channel];
            solid[channel] += color[channel] * alpha;
        }
        covered += alpha;
        count += 1.0;
    }
    let all = all.map(|sum| sum / count);
    let solid = if covered > 0.0 { solid.map(|sum| sum / covered) } else { all };
    Some(Mean { all, solid })
}

/// The mean of an image's alpha over the texels another shows, as a diffuse transmission texture's
/// over the leaf its base color cuts out of its card, each of the other's texels read beside the
/// image's texel at the same place, where its alpha is `cutoff` or more, or by its alpha where there
/// is no cutoff, as a blended surface shows it. None where either is not read or the other shows
/// nothing.
pub fn alpha_where(image: &Image, shown: &Image, cutoff: Option<f32>) -> Option<f32> {
    let (mut level, mut over) = (Level::of(image)?, Level::of(shown)?);
    let (mut sum, mut weight) = (0f32, 0f32);
    for (x, y) in over.places() {
        let (_, shows) = over.read(x, y);
        let by = match cutoff {
            Some(cutoff) => f32::from(u8::from(shows >= cutoff)),
            None => shows,
        };
        if by > 0.0 {
            let (u, v) = ((x * 2 + 1) as f32 / (2 * over.width) as f32, (y * 2 + 1) as f32 / (2 * over.height) as f32);
            let at = (((u * level.width as f32) as usize).min(level.width - 1), ((v * level.height as f32) as usize).min(level.height - 1));
            sum += level.read(at.0, at.1).1 * by;
            weight += by;
        }
    }
    (weight > 0.0).then(|| sum / weight)
}

/// Sixteen texels of a BC1 block, eight bytes, red, green, blue and alpha from 0 to 255 row by row.
/// `four` reads it as BC3's color always does, four colors whatever order its ends stand in.
fn bc1(block: &[u8], four: bool) -> [[u8; 4]; 16] {
    let ends = [u16::from_le_bytes([block[0], block[1]]), u16::from_le_bytes([block[2], block[3]])];
    let [a, b] = ends.map(|end| {
        let (r, g, b) = (u32::from(end >> 11), u32::from((end >> 5) & 63), u32::from(end & 31));
        [(r << 3) | (r >> 2), (g << 2) | (g >> 4), (b << 3) | (b >> 2)]
    });
    let mix = |wa: u32, wb: u32| {
        let total = wa + wb;
        [0, 1, 2].map(|c| ((wa * a[c] + wb * b[c] + total / 2) / total) as u8)
    };
    let solid = |c: [u32; 3]| [c[0] as u8, c[1] as u8, c[2] as u8, 255];
    let palette = if four || ends[0] > ends[1] {
        let (third, two_thirds) = (mix(2, 1), mix(1, 2));
        [solid(a), solid(b), [third[0], third[1], third[2], 255], [two_thirds[0], two_thirds[1], two_thirds[2], 255]]
    } else {
        let half = mix(1, 1);
        [solid(a), solid(b), [half[0], half[1], half[2], 255], [0, 0, 0, 0]]
    };
    let indices = u32::from_le_bytes([block[4], block[5], block[6], block[7]]);
    std::array::from_fn(|i| palette[((indices >> (2 * i)) & 3) as usize])
}

/// Sixteen levels of one channel of a BC4 block, eight bytes, as BC3's alpha and BC5's two channels
/// are kept.
fn bc4(block: &[u8]) -> [u8; 16] {
    let (a, b) = (u32::from(block[0]), u32::from(block[1]));
    let mut palette = [a, b, 0, 0, 0, 0, 0, 255];
    if a > b {
        for i in 1..7 {
            palette[i + 1] = ((7 - i as u32) * a + i as u32 * b + 3) / 7;
        }
    } else {
        for i in 1..5 {
            palette[i + 1] = ((5 - i as u32) * a + i as u32 * b + 2) / 5;
        }
    }
    let mut bits = [0u8; 8];
    bits[..6].copy_from_slice(&block[2..8]);
    let indices = u64::from_le_bytes(bits);
    std::array::from_fn(|i| palette[((indices >> (3 * i)) & 7) as usize] as u8)
}

// BC7's weights for an index of two, three and four bits, out of 64.
const WEIGHTS2: [u32; 4] = [0, 21, 43, 64];
const WEIGHTS3: [u32; 8] = [0, 9, 18, 27, 37, 46, 55, 64];
const WEIGHTS4: [u32; 16] = [0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64];

/// Each BC7 mode's subsets, then the bits of its partition, its rotation and its index selection,
/// of each color and alpha end, of a p-bit for each end and for each subset, and of its two
/// indices, as the format sets them out.
const MODES: [[u32; 10]; 8] = [
    [3,  4, 0, 0, 4, 0, 1, 0, 3, 0],
    [2, 6, 0, 0, 6, 0, 0, 1, 3, 0],
    [3,  6, 0, 0, 5, 0, 0, 0, 2, 0],
    [2, 6, 0, 0, 7, 0, 1, 0, 2, 0],
    [1, 0, 2, 1, 5, 6, 0, 0, 2, 3],
    [1, 0, 2, 0, 7, 8, 0, 0, 2, 2],
    [1, 0, 0, 0, 7, 7, 1, 0, 4, 0],
    [2, 6, 0, 0, 5, 5, 1, 0, 2, 0],
];

/// The subset of each texel in each of the 64 shapes of two subsets, a bit a texel from the first.
const TWO: [u16; 64] = [
    0xcccc, 0x8888, 0xeeee, 0xecc8, 0xc880, 0xfeec, 0xfec8, 0xec80,
    0xc800, 0xffec, 0xfe80, 0xe800, 0xffe8, 0xff00, 0xfff0, 0xf000,
    0xf710, 0x008e, 0x7100, 0x08ce, 0x008c, 0x7310, 0x3100, 0x8cce,
    0x088c, 0x3110, 0x6666, 0x366c, 0x17e8, 0x0ff0, 0x718e, 0x399c,
    0xaaaa, 0xf0f0, 0x5a5a, 0x33cc, 0x3c3c, 0x55aa, 0x9696, 0xa55a,
    0x73ce, 0x13c8, 0x324c, 0x3bdc, 0x6996, 0xc33c, 0x9966, 0x0660,
    0x0272, 0x04e4, 0x4e40, 0x2720, 0xc936, 0x936c, 0x39c6, 0x639c,
    0x9336, 0x9cc6, 0x817e, 0xe718, 0xccf0, 0x0fcc, 0x7744, 0xee22,
];
/// The subset of each texel in each of the 64 shapes of three subsets, two bits a texel from the
/// first.
const THREE: [u32; 64] = [
    0xaa685050, 0x6a5a5040, 0x5a5a4200, 0x5450a0a8, 0xa5a50000, 0xa0a05050,
    0x5555a0a0, 0x5a5a5050, 0xaa550000, 0xaa555500, 0xaaaa5500, 0x90909090,
    0x94949494, 0xa4a4a4a4, 0xa9a59450, 0x2a0a4250, 0xa5945040, 0x0a425054,
    0xa5a5a500, 0x55a0a0a0, 0xa8a85454, 0x6a6a4040, 0xa4a45000, 0x1a1a0500,
    0x0050a4a4, 0xaaa59090, 0x14696914, 0x69691400, 0xa08585a0, 0xaa821414,
    0x50a4a450, 0x6a5a0200, 0xa9a58000, 0x5090a0a8, 0xa8a09050, 0x24242424,
    0x00aa5500, 0x24924924, 0x24499224, 0x50a50a50, 0x500aa550, 0xaaaa4444,
    0x66660000, 0xa5a0a5a0, 0x50a050a0, 0x69286928, 0x44aaaa44, 0x66666600,
    0xaa444444, 0x54a854a8, 0x95809580, 0x96969600, 0xa85454a8, 0x80959580,
    0xaa141414, 0x96960000, 0xaaaa1414, 0xa05050a0, 0xa0a5a5a0, 0x96000000,
    0x40804080, 0xa9a8a9a8, 0xaaaaaa44, 0x2a4a5254,
];
/// The texel of each shape of two subsets that holds the second subset's index a bit short, the
/// first's being texel 0.
const ANCHOR_TWO: [u8; 64] = [
    15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
    15, 2, 8, 2, 2, 8, 8, 15, 2, 8, 2, 2, 8, 8, 2, 2,
    15, 15, 6, 8, 2, 8, 15, 15, 2, 8, 2, 2, 2, 15, 15, 6,
    6, 2, 6, 8, 15, 15, 2, 2, 15, 15, 15, 15, 15, 2, 2, 15,
];
/// The texels of each shape of three subsets that hold the second and third subsets' indices a bit
/// short.
const ANCHOR_THREE: [[u8; 2]; 64] = [
    [3, 15], [3, 8], [15, 8], [15, 3], [8, 15], [3, 15], [15, 3], [15, 8],
    [8, 15], [8, 15], [6, 15], [6, 15], [6, 15], [5, 15], [3, 15], [3, 8],
    [3, 15], [3, 8], [8, 15], [15, 3], [3, 15], [3, 8], [6, 15], [10, 8],
    [5, 3], [8, 15], [8, 6], [6, 10], [8, 15], [5, 15], [15, 10], [15, 8],
    [8, 15], [15, 3], [3, 15], [5, 10], [6, 10], [10, 8], [8, 9], [15, 10],
    [15, 6], [3, 15], [15, 8], [5, 15], [15, 3], [15, 6], [15, 6], [15, 8],
    [3, 15], [15, 3], [5, 15], [5, 15], [5, 15], [8, 15], [5, 15], [10, 15],
    [5, 15], [10, 15], [8, 15], [13, 15], [15, 3], [12, 15], [3, 15], [3, 8],
];

/// The bits of a block from the lowest, as BC7 lays its fields out.
struct Bits(u128, u32);

impl Bits {
    fn take(&mut self, count: u32) -> u32 {
        let value = ((self.0 >> self.1) & ((1u128 << count) - 1)) as u32;
        self.1 += count;
        value
    }
}

/// Sixteen texels of a BC7 block, sixteen bytes, red, green, blue and alpha from 0 to 255 row by
/// row, a block of no mode reading clear black as the format says.
fn bc7(block: &[u8]) -> [[u8; 4]; 16] {
    let bits = u128::from_le_bytes(block[..16].try_into().unwrap_or([0; 16]));
    let mode = bits.trailing_zeros();
    if mode >= 8 {
        return [[0; 4]; 16];
    }
    let [subsets, partition_bits, rotation_bits, selection_bits, color_bits, alpha_bits, end_p, subset_p, index_bits, second_bits] =
        MODES[mode as usize];
    let mut read = Bits(bits, mode + 1);
    let partition = read.take(partition_bits) as usize;
    let rotation = read.take(rotation_bits);
    let selection = read.take(selection_bits);

    let subsets = subsets as usize;
    let mut ends = [[[0u32; 4]; 2]; 3];
    for channel in 0..3 {
        for subset in ends.iter_mut().take(subsets) {
            for end in subset.iter_mut() {
                end[channel] = read.take(color_bits);
            }
        }
    }
    for subset in ends.iter_mut().take(subsets) {
        for end in subset.iter_mut() {
            end[3] = read.take(alpha_bits);
        }
    }

    // A p-bit is each end's lowest bit, one an end or one a subset, alpha's too where it has one.
    let (mut color_bits, mut alpha_bits) = (color_bits, alpha_bits);
    if end_p + subset_p > 0 {
        for subset in ends.iter_mut().take(subsets) {
            let shared = if subset_p > 0 { read.take(1) } else { 0 };
            for end in subset.iter_mut() {
                let p = if end_p > 0 { read.take(1) } else { shared };
                for (channel, value) in end.iter_mut().enumerate() {
                    if channel < 3 || alpha_bits > 0 {
                        *value = (*value << 1) | p;
                    }
                }
            }
        }
        color_bits += 1;
        if alpha_bits > 0 {
            alpha_bits += 1;
        }
    }
    let widen = |value: u32, bits: u32| (value << (8 - bits)) | (value >> (2 * bits - 8));
    for subset in ends.iter_mut().take(subsets) {
        for end in subset.iter_mut() {
            for channel in 0..3 {
                end[channel] = widen(end[channel], color_bits);
            }
            end[3] = if alpha_bits > 0 { widen(end[3], alpha_bits) } else { 255 };
        }
    }

    let subset_of = |texel: usize| match subsets {
        1 => 0,
        2 => usize::from((TWO[partition] >> texel) & 1),
        _ => ((THREE[partition] >> (2 * texel)) & 3) as usize,
    };
    let anchor = |texel: usize| {
        texel == 0
            || (subsets == 2 && texel == usize::from(ANCHOR_TWO[partition]))
            || (subsets == 3 && ANCHOR_THREE[partition].contains(&(texel as u8)))
    };
    let first: [u32; 16] = std::array::from_fn(|texel| read.take(index_bits - u32::from(anchor(texel))));
    let second: [u32; 16] = std::array::from_fn(|texel| if second_bits > 0 { read.take(second_bits - u32::from(texel == 0)) } else { 0 });

    let weight = |index: u32, bits: u32| match bits {
        2 => WEIGHTS2[index as usize],
        3 => WEIGHTS3[index as usize],
        _ => WEIGHTS4[index as usize],
    };
    std::array::from_fn(|texel| {
        let [low, high] = ends[subset_of(texel)];
        let ((color, color_width), (alpha, alpha_width)) = match (second_bits, selection) {
            (0, _) => ((first[texel], index_bits), (first[texel], index_bits)),
            (_, 0) => ((first[texel], index_bits), (second[texel], second_bits)),
            _ => ((second[texel], second_bits), (first[texel], index_bits)),
        };
        let mix = |channel: usize, w: u32| (((64 - w) * low[channel] + w * high[channel] + 32) >> 6) as u8;
        let w = weight(color, color_width);
        let mut texel = [mix(0, w), mix(1, w), mix(2, w), mix(3, weight(alpha, alpha_width))];
        if rotation > 0 {
            texel.swap(3, rotation as usize - 1);
        }
        texel
    })
}

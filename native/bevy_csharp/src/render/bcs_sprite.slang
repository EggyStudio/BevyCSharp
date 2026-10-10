// What a BevyCSharp 2D material drawing a sprite reads of the sprite: its image, its color, and how
// the image is cut, flipped, tiled and sliced over the quad, with Bevy's functions for each.
//
// A fragment shader drawing a sprite reaches it with `import bcs_sprite;` beside `import bcs2d;`,
// and everything in it is under the `bcs_sprite` namespace. Bevy draws a sprite as a quad mesh,
// and the bridge binds what the sprite says in the material's group at bindings 100 to 102, past
// the program's own, so a shader names its own values as it would on any 2D mesh. A shader that
// imports this draws only on a sprite, since only a sprite has these to bind.
//
// The functions are Bevy's own from `bevy_sprite_render::sprite_mesh::functions`, without the
// instance index Bevy's take, which only its bindless materials read, and the bridge binds none.

module bcs_sprite;

public namespace bcs_sprite
{

/// How the sprite is drawn, in the layout of Bevy's `SpriteMeshMaterialUniform`, which Bevy works
/// out from the sprite and its image.
public struct Sprite
{
    /// The sprite's color, which tints its image.
    public float4 color;
    /// Which way the image is flipped, whether it tiles, and how the sprite blends, as the
    /// constants below.
    public uint flags;
    /// Below this alpha a masked sprite is not drawn.
    public float alpha_cutoff;
    /// The quad's size, which scales Bevy's quad of one unit a side.
    public float2 vertex_scale;
    /// Where the quad's center is moved to, from the sprite's anchor.
    public float2 vertex_offset;
    /// From the quad's UVs to the image's, for an atlas, a rect or a scaling mode, as three columns.
    public float4 uv_transform[3];
    /// Where a tiled image repeats, as a share of the quad.
    public float2 tile_stretch_value;
    /// What slicing needs, all zero for a sprite that is not sliced.
    public float2 scale;
    public float2 min_inset;
    public float2 max_inset;
    public float2 side_stretch_value;
    public float2 center_stretch_value;
};

/// The image is flipped across its vertical axis.
public static const uint FLIP_X = 1u;
/// The image is flipped across its horizontal axis.
public static const uint FLIP_Y = 2u;
/// The image repeats across the quad.
public static const uint TILE_X = 4u;
/// The image repeats down the quad.
public static const uint TILE_Y = 8u;
/// The bits that say how the sprite blends.
public static const uint ALPHA_MODE_BITS = 3u << 30;
public static const uint ALPHA_MODE_OPAQUE = 0u;
public static const uint ALPHA_MODE_MASK = 1u << 30;
public static const uint ALPHA_MODE_BLEND = 2u << 30;

[[vk::binding(100, 102)]] public ConstantBuffer<Sprite> sprite;
/// The sprite's image, the whole of it, an atlas included.
[[vk::binding(101, 102)]] public Texture2D image;
[[vk::binding(102, 102)]] public SamplerState image_sampler;

// -- Bevy's functions

/// The sprite's image at `uv` with its color applied and a masked sprite's alpha cut off, which is
/// how Bevy draws a sprite.
public float4 sample_final_color(float2 uv)
{
    return get_final_color(sample_sprite_texture(uv));
}

/// The sprite's image at `uv`, a point on the quad from zero to one, read where the sprite's
/// flipping, tiling, slicing and atlas put it, without its color.
public float4 sample_sprite_texture(float2 uv)
{
    return image.Sample(image_sampler, final_uv(uv));
}

/// A color with the sprite's color applied and its alpha mode, which leaves an opaque sprite's alpha
/// at one and does not draw a masked sprite's fragment below the cutoff.
public float4 get_final_color(float4 sprite_color)
{
    return alpha_discard(apply_tint(sprite_color));
}

/// Where on the image a point on the quad reads, from zero to one.
public float2 final_uv(float2 uv)
{
    var out = uv;
    out = apply_flip(out);
    out = apply_tiling(out);
    out = apply_slicing(out);
    out = apply_uv_transform(out);
    return out;
}

/// Flips `uv` as the sprite's `flip_x` and `flip_y` say.
public float2 apply_flip(float2 uv)
{
    var out = uv;
    if ((sprite.flags & FLIP_X) != 0u)
        out.x = 1.0 - out.x;
    if ((sprite.flags & FLIP_Y) != 0u)
        out.y = 1.0 - out.y;
    return out;
}

/// Repeats `uv` where the sprite's image mode tiles it.
public float2 apply_tiling(float2 uv)
{
    let stretch = sprite.tile_stretch_value;
    var out = uv;
    if ((sprite.flags & TILE_X) != 0u)
        out.x = (out.x - stretch.x * floor(out.x / stretch.x)) / stretch.x;
    if ((sprite.flags & TILE_Y) != 0u)
        out.y = (out.y - stretch.y * floor(out.y / stretch.y)) / stretch.y;
    return out;
}

/// Moves `uv` onto the part of the image an atlas, a rect or a scaling mode picks.
public float2 apply_uv_transform(float2 uv)
{
    let columns = sprite.uv_transform;
    return columns[0].xy * uv.x + columns[1].xy * uv.y + columns[2].xy;
}

/// Moves `uv` onto the image's corners, edges and center where the sprite is sliced, so the
/// corners keep their size as the quad grows.
public float2 apply_slicing(float2 uv)
{
    let scale = sprite.scale;
    let min_inset = sprite.min_inset;
    let max_inset = sprite.max_inset;
    let side = sprite.side_stretch_value;
    let center = sprite.center_stretch_value;

    // Zero where the sprite is not sliced, as Bevy checks it.
    if (scale.x == 0.0)
        return uv;

    let min_scaled = min_inset / scale;
    let max_scaled = max_inset / scale;

    let left = uv.x < min_scaled.x;
    let right = uv.x > 1.0 - max_scaled.x;
    let top = uv.y < min_scaled.y;
    let bottom = uv.y > 1.0 - max_scaled.y;

    if (top && left)
        return uv * scale;
    if (top && right)
        return float2(1.0 - (1.0 - uv.x) * scale.x, uv.y * scale.y);
    if (bottom && left)
        return float2(uv.x * scale.x, 1.0 - (1.0 - uv.y) * scale.y);
    if (bottom && right)
        return float2(1.0) - (float2(1.0) - uv) * scale;

    let across = tile_or_stretch(uv.x, min_scaled.x, 1.0 - max_scaled.x, min_inset.x, 1.0 - max_inset.x, side.x);
    let down = tile_or_stretch(uv.y, min_scaled.y, 1.0 - max_scaled.y, min_inset.y, 1.0 - max_inset.y, side.y);

    if (top)
        return float2(across, uv.y * scale.y);
    if (bottom)
        return float2(across, 1.0 - (1.0 - uv.y) * scale.y);
    if (left)
        return float2(uv.x * scale.x, down);
    if (right)
        return float2(1.0 - (1.0 - uv.x) * scale.x, down);

    return float2(
        tile_or_stretch(uv.x, min_scaled.x, 1.0 - max_scaled.x, min_inset.x, 1.0 - max_inset.x, center.x),
        tile_or_stretch(uv.y, min_scaled.y, 1.0 - max_scaled.y, min_inset.y, 1.0 - max_inset.y, center.y));
}

/// A color multiplied by the sprite's.
public float4 apply_tint(float4 sprite_color)
{
    return sprite_color * sprite.color;
}

/// A color with the sprite's alpha mode applied, which sets an opaque sprite's alpha to one, sets a
/// masked sprite's to one at or above the cutoff and draws nothing of it below.
public float4 alpha_discard(float4 color)
{
    var out = color;
    let mode = sprite.flags & ALPHA_MODE_BITS;

    if (mode == ALPHA_MODE_OPAQUE)
    {
        out.a = 1.0;
    }
    else if (mode == ALPHA_MODE_MASK)
    {
        if (out.a < sprite.alpha_cutoff)
            discard;
        out.a = 1.0;
    }

    return out;
}

/// Maps `p` from `a` to `b` onto `c` to `d`, repeating it where `stretch` is not zero.
float tile_or_stretch(float p, float a, float b, float c, float d, float stretch)
{
    if (stretch == 0.0)
        return (p - a) / (b - a) * (d - c) + c;

    let value = (p - a) / (b - a);
    let tile = (value - stretch * floor(value / stretch)) / stretch;
    return tile * (d - c) + c;
}

}

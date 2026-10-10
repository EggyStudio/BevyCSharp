// Bevy's sprite_material shader in Slang, which dissolves a sprite through noise, the pixels the
// noise puts just past the dissolved ones drawn as a burning edge.
import bcs2d;
import bcs_sprite;

/// The color of the edge as it burns.
uniform float4 burn_edge_color;
/// How much of the sprite is gone, from zero to one.
uniform float amount;
/// How wide the burning edge is, in the noise's units.
uniform float burn_edge;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput input) : SV_Target
{
    // The noise at twice the UVs, which makes it denser, from burn_edge to one less burn_edge, so
    // no edge shows on a sprite that is whole or gone.
    var noise = fbm_simplex_2d(input.uv * 2.0);
    noise = lerp(burn_edge, 1.0 - burn_edge, noise);

    // Below zero the pixel is gone, up to burn_edge it is burning, and past that it is whole.
    let difference = noise - amount;
    let sprite_color = bcs_sprite::sample_sprite_texture(input.uv);

    var color = float4(0.0);
    if (difference >= 0.0 && difference < burn_edge)
    {
        color = burn_edge_color;
        color.a = sprite_color.a;
    }
    else if (difference >= burn_edge)
    {
        color = sprite_color;
    }

    // The sprite's tint and alpha mode, as Bevy draws a sprite.
    return bcs_sprite::get_final_color(color);
}

/// Simplex noise, from zero to one, after the one in the noisy_bevy crate. Sampling a texture of it
/// would be quicker, and Bevy computes it to keep a texture out of its repository.
float simplex_noise_2d(float2 v)
{
    let C = float4(
        0.211324865405187,   // (3 - sqrt(3)) / 6
        0.366025403784439,   // (sqrt(3) - 1) / 2
        -0.577350269189626,  // 2C.x - 1
        0.024390243902439);  // 1 / 41

    // The first corner.
    var i = floor(v + dot(v, C.yy));
    let x0 = v - i + dot(i, C.xx);

    // The other two.
    let i1 = x0.x > x0.y ? float2(1.0, 0.0) : float2(0.0, 1.0);
    let x12 = x0.xyxy + C.xxzz - float4(i1, 0.0, 0.0);

    // Permutations.
    i = i % float2(289.0);

    let p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
    var m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), float3(0.0));
    m *= m;
    m *= m;

    // Gradients, 41 points along a line mapped onto a diamond, since a ring of 17 by 17, 289, is
    // close to a multiple of 41 (41 * 7 = 287).
    let x = 2.0 * frac(p * C.www) - 1.0;
    let h = abs(x) - 0.5;
    let ox = floor(x + 0.5);
    let a0 = x - ox;

    // Normalizes the gradients by scaling m, close to multiplying it by the inverse square root of
    // a0 * a0 + h * h.
    m = m * (1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h));

    let g = float3(a0.x * x0.x + h.x * x0.y, a0.yz * x12.xz + h.yz * x12.yw);
    return (130.0 * dot(m, g) + 1.0) / 2.0;
}

/// Noise summed over octaves, each at twice the frequency and half the amplitude of the last, which
/// gives it finer detail, from zero to one.
float fbm_simplex_2d(float2 position)
{
    static const int octaves = 3;
    static const float lacunarity = 2.0;
    static const float gain = 0.5;

    // The most the sum reaches, the series of amplitudes summed.
    let most = (1.0 - pow(gain, float(octaves))) / (1.0 - gain);

    var sum = 0.0;
    var amplitude = 1.0;
    var frequency = 1.0;
    for (int octave = 0; octave < octaves; octave++)
    {
        sum += simplex_noise_2d(position * frequency) * amplitude;
        amplitude *= gain;
        frequency *= lacunarity;
    }

    return sum / most;
}

float3 permute(float3 x)
{
    return ((x * 34.0 + 1.0) * x) % float3(289.0);
}

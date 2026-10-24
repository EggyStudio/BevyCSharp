// A camera's look, the pass that turns its linear picture into the one the screen shows in the place
// of Bevy's tonemapper (Render.SetLook). It multiplies the light by an exposure, brings light past
// white under a curve, saturates and tints it in linear light, and sets its contrast about the
// middle of what is shown once encoded, in the order 3DEngine's composite runs them, so a look set
// here and there with the same numbers shows the same picture.
//
// The pass runs after Bevy's bloom, effects and auto exposure have done their work on the linear
// picture, with Bevy's tonemapper doing nothing, and before Bevy's antialiasing, which smooths the
// picture as the screen shows it. It writes that picture back linear, since the window's picture
// encodes what it is given, so the encoding there gives exactly what was shown here.
import bcs_pass;

// x: 0 for the engine's curve, 1 for the curve with four numbers. y: the exposure the light is
// multiplied by first. z: the knee past which the engine's curve bends each channel toward white on
// its own. w: how far it does, from the hue held at 0 to each channel bent at 1.
uniform float4 curve;

// The curve with four numbers, its exposure, its white, and the powers it raises the dark end and
// the bright end by.
uniform float4 numbers;

// x: the contrast about the middle of what is shown. y: the saturation in linear light. z: 1 where
// the picture is dithered. w: unused.
uniform float4 grade;

// What every channel is multiplied by after the saturation, linear.
uniform float4 tint;

// sRGB's transfer functions exactly rather than as a power of 2.2, so a level decoded and encoded
// again comes back as itself.
float3 srgbToLinear(float3 c)
{
    return select(c <= 0.04045, c / 12.92, pow((max(c, 0.0) + 0.055) / 1.055, 2.4));
}

float3 linearToSrgb(float3 c)
{
    c = saturate(c);
    return select(c <= 0.0031308, c * 12.92, 1.055 * pow(c, 1.0 / 2.4) - 0.055);
}

// The brightest channel left alone up to 0.9 and bent smoothly toward 1 past it, the other two
// scaled with it, so a bright orange stays orange where a clamp per channel turns it white.
float3 heldCurve(float3 color)
{
    static const float knee = 0.9;
    float peak = max(color.r, max(color.g, color.b));
    if (peak <= knee)
        return max(color, 0.0);
    float bent = knee + (1.0 - knee) * (1.0 - exp(-(peak - knee) / (1.0 - knee)));
    return max(color, 0.0) * (bent / peak);
}

// The engine's curve with its highlights' hue let go by a strength, each channel past the knee bent
// toward 1 on its own along the same shape, so a light bright in all three goes to white as film
// and a path-traced reference's clip take it. A light with every channel under the knee is as it
// was, and a strength of 0 is the held curve alone.
float3 kneeCurve(float3 color, float knee, float strength)
{
    float3 held = heldCurve(color);
    if (strength <= 0.0 || knee >= 1.0)
        return held;
    float3 c = max(color, 0.0);
    float3 bent = select(c > knee, knee + (1.0 - knee) * (1.0 - exp(-(c - knee) / (1.0 - knee))), c);
    return lerp(held, bent, strength);
}

// The curve with four numbers. The light is multiplied by its exposure, brought under 1 by a white
// curve, raised by a power running from the lower to the upper as it brightens, and shown at a gamma
// of 2.2, which is decoded here so the encoding after gives that value back exactly.
float3 numberedCurve(float3 c, float4 n)
{
    c = max(c, 0.0) * n.x;
    c = c / pow(pow(c, n.y) + 1.0, 1.0 / n.y);
    c = pow(c, lerp(n.zzz, n.www, sqrt(c)));
    return srgbToLinear(pow(c, 1.0 / 2.2));
}

// Bevy's dither of the picture as shown, half a level either way, so a gradient's levels do not
// band.
float3 dither(float2 pixel)
{
    float3 noise = dot(float2(171.0, 231.0), pixel).xxx;
    noise = frac(noise / float3(103.0, 71.0, 97.0));
    return (noise - 0.5) / 255.0;
}

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    float3 light = max(passPicture.Load(int3(int2(input.position.xy), 0)).rgb * curve.y, 0.0);
    float3 bent = curve.x > 0.5 ? numberedCurve(light, numbers) : kneeCurve(light, curve.z, curve.w);

    float luminance = dot(bent, float3(0.2126, 0.7152, 0.0722));
    float3 graded = max(lerp(luminance.xxx, bent, grade.y) * tint.rgb, 0.0);

    float3 shown = saturate((linearToSrgb(graded) - 0.5) * grade.x + 0.5);
    if (grade.z > 0.5)
        shown = saturate(shown + dither(floor(input.position.xy)));
    return float4(srgbToLinear(shown), 1.0);
}

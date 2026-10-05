// Bevy's animate_shader in Slang, four colors mixed in Oklab, a perceptual color space, by time and
// by how far a point is from the middle of its face.
import bcs;

float3 oklab_to_linear_srgb(float3 c)
{
    let L = c.x;
    let a = c.y;
    let b = c.z;

    let l_ = L + 0.3963377774 * a + 0.2158037573 * b;
    let m_ = L - 0.1055613458 * a - 0.0638541728 * b;
    let s_ = L - 0.0894841775 * a - 1.2914855480 * b;

    let l = l_ * l_ * l_;
    let m = m_ * m_ * m_;
    let s = s_ * s_ * s_;

    return float3(
        4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
        -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
        -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let speed = 2.0;

    // Time since startup, in seconds, from Bevy's globals.
    let t_1 = sin(bcs::globals.time * speed) * 0.5 + 0.5;
    let t_2 = cos(bcs::globals.time * speed);

    let distance_to_center = distance(mesh.uv, float2(0.5)) * 1.4;

    let red = float3(0.627955, 0.224863, 0.125846);
    let green = float3(0.86644, -0.233887, 0.179498);
    let blue = float3(0.701674, 0.274566, -0.169156);
    let white = float3(1.0, 0.0, 0.0);
    let mixed = lerp(lerp(red, blue, t_1), lerp(green, white, t_2), distance_to_center);

    return float4(oklab_to_linear_srgb(mixed), 1.0);
}

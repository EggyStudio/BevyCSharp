// The fog's froxels added up from the eye, slice after slice along each column of the view, into
// the light the fog in front of each depth scatters toward the eye and the share of the light
// behind it that gets through (Render.SetSkyFog with SkyFogSettings.Froxels), which sky_fog.slang
// lays over the picture.
//
// Each slice's light is added as the fog's own light fades through the slice, worked out in closed
// form for the slice's depth rather than as though the slice were a point, so a thick slice near
// the eye does not brighten the fog past what its light could give, which Hillaire's account of
// Frostbite's volumetric fog calls energy conserving.
import bcs_pass;

// fog_froxels.slang's froxels, the light scattered a meter in the rgb and the light a meter takes
// away in the alpha.
Texture3D<float4> fog_scatter;

// The light scattered toward the eye by the fog in front of each slice's far end in the rgb, and
// the share of the light from beyond it that gets through in the alpha.
[format("rgba16f")] WTexture3D<float4> fog_volume;

// x, y, z: the froxels across, down and deep. w: how far from the eye the last slice ends.
uniform float4 grid;

static const float Near = 0.5;

float sliceDepth(float slice)
{
    if (slice <= 0.0)
        return 0.0;
    return Near * pow(grid.w / Near, slice / grid.z);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint3 size = uint3(grid.xyz);
    if (any(id.xy >= size.xy))
        return;

    float2 uv = (float2(id.xy) + 0.5) / grid.xy;
    float3 way = normalize(passRayDirection(uv));
    float3 forward = -normalize(passView.world_from_view[2].xyz);
    float slant = max(dot(way, forward), 1e-3);

    float3 scattered = 0.0;
    float through = 1.0;
    for (uint slice = 0u; slice < size.z; slice++)
    {
        float4 froxel = fog_scatter[uint3(id.xy, slice)];
        float length = (sliceDepth(float(slice) + 1.0) - sliceDepth(float(slice))) / slant;
        float thick = max(froxel.a, 1e-7);
        float passed = exp(-thick * length);

        // The slice's own light, which is its light a meter over how much a meter takes away,
        // faded through as much of the slice as lies in front of each point of it.
        scattered += through * (froxel.rgb - froxel.rgb * passed) / thick;
        through *= passed;
        fog_volume.Store(uint3(id.xy, slice), float4(scattered, through));
    }
}

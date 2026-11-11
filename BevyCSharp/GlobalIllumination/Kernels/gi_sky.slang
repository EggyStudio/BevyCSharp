// The light the camera's environment map sends along each way, times the camera's exposure, folded
// onto an octahedron as the probes' directions are, which global illumination's probes read where a
// ray of their last cascade meets nothing. A dispatch on the camera writes it, since the
// environment map is the camera's, and the probes, which are traced before the cameras draw, read
// the frame before's. Alpha 1 where the camera has an environment map, 0 where it has none, so the
// probes take the ambient light alone there.
import bcs_pass;
import bcs_gi;

[format("rgba16f")] WTexture2D<float4> sky_map;

// The lights the probes are lit by, and the share of the sun's light the camera's air lets through
// toward it, which this writes for the probes' rays (gi_trace.slang). The lights give the sun as it
// is above the air, and Bevy dims and reddens it through the camera's air for the picture, so the
// probes lit their faces by a low sun a third and more brighter than the picture's and whiter.
StructuredBuffer<GiLights> gi_lights;
RWStructuredBuffer<float4> gi_air;

// How much the environment is blurred, as a surface of this perceptual roughness blurs it, since a
// texel of the probes' first cascade, of eight a side, spans a sixty-fourth of the sphere, which a
// few rays stand for.
static const float Blur = 0.25;

// The directional lights whose air is read after the sky's, as many as the light pass reads.
static const uint AirSuns = 10u;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    // Read at the camera as Bevy reads it, and before any thread returns, as the clouds read it.
    GiLights lights = gi_lights[0];
    float3 through = lights.sunDirection.w > 0.5 ? passAtmosphereTransmittance(passView.world_position, lights.sunDirection.xyz) : float3(1.0);

    // The environment map's light from above and the air toward each directional light, as the
    // light pass reads them at the camera for the light past the probes, which a leaf drawn forward
    // reads here (forward_probes.rs), the pass reading them itself.
    float3 above = passEnvironmentDiffuse(float3(0.0, 1.0, 0.0)) * passView.exposure;
    float3 suns[AirSuns];
    for (uint i = 0u; i < AirSuns; i++)
        suns[i] = i < passLights.n_directional_lights ? passAtmosphereTransmittance(passView.world_position, passLights.directional_lights[i].direction_to_light) : float3(1.0);

    if (all(id.xy == uint2(0u)))
    {
        gi_air[0] = float4(through, 1.0);
        gi_air[1] = float4(above, 1.0);
        for (uint i = 0u; i < AirSuns; i++)
            gi_air[2u + i] = float4(suns[i], 1.0);
    }

    uint width, height;
    sky_map.GetDimensions(width, height);
    if (id.x >= width || id.y >= height)
        return;
    float3 way = fromOctahedron((float2(id.xy) + 0.5) / float2(width, height));
    float3 light = passEnvironmentSpecular(way, Blur) * passView.exposure;
    sky_map.Store(id.xy, float4(light, passHasEnvironment() ? 1.0 : 0.0));
}

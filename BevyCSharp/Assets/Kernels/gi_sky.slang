// The light the camera's environment map sends along each way, times the camera's exposure, folded
// onto an octahedron as the probes' directions are, which global illumination's probes read where a
// ray of their last cascade meets nothing, as 3DEngine's read its environment map. A dispatch on the
// camera writes it, since the environment map is the camera's, and the probes, which are traced
// before the cameras draw, read the frame before's. Alpha 1 where the camera has an environment map,
// 0 where it has none, so the probes take the ambient light alone there.
import bcs_pass;
import bcs_gi;

[format("rgba16f")] WTexture2D<float4> sky_map;

// How much the environment is blurred, as a surface of this perceptual roughness blurs it, since a
// texel of the probes' first cascade, of eight a side, spans a sixty-fourth of the sphere, which a
// few rays stand for.
static const float Blur = 0.25;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width, height;
    sky_map.GetDimensions(width, height);
    if (id.x >= width || id.y >= height)
        return;
    float3 way = fromOctahedron((float2(id.xy) + 0.5) / float2(width, height));
    float3 light = passEnvironmentSpecular(way, Blur) * passView.exposure;
    sky_map.Store(id.xy, float4(light, passHasEnvironment() ? 1.0 : 0.0));
}

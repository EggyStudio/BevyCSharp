// Bevy's shader_defs shader in Slang, red where the program was compiled with IS_RED defined, and
// the material's color otherwise.
import bcs;

struct CustomMaterial
{
    float4 color;
};

uniform CustomMaterial material;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
#ifdef IS_RED
    return float4(1.0, 0.0, 0.0, 1.0);
#else
    return material.color;
#endif
}

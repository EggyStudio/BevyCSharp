// One level of a 2D material's image, chosen by number, with no other level blended in.
import bcs2d;

uniform uint level;
Texture2D picture;
SamplerState picture_sampler;

[shader("fragment")]
float4 fragment(Mesh2dVertexOutput mesh) : SV_Target
{
    return picture.SampleLevel(picture_sampler, mesh.uv, float(level));
}

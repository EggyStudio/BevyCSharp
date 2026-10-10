// A 2D material red where its pipeline constant RED is one and green where it is zero, as it is
// where nothing sets it.
import bcs2d;

[SpecializationConstant]
const float RED = 0.0;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return float4(RED, 1.0 - RED, 0.0, 1.0);
}

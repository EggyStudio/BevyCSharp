// A pass whose red is a pipeline constant, which a pass runs with as the shader declares it.
import bcs_pass;

[SpecializationConstant]
const float STRENGTH = 1.0;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    return float4(STRENGTH, 0.0, 0.0, 1.0);
}

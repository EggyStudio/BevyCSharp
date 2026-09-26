// A pass that paints the whole picture red, whatever was there.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    return float4(1.0, 0.0, 0.0, 1.0);
}

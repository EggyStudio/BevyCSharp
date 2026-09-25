// Paints the color its defines spell out, which is what makes one file many programs.
//
// A program with different defines is a different program, compiled on its own, so a scene with
// one of these per cube has as many programs as it has cubes.

import bcs;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return float4(float(RED), float(GREEN), float(BLUE), 1.0);
}

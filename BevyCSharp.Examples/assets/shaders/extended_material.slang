// Bevy's extended_material shader in Slang: its standard material's red, lit by Bevy, with the
// blue made the same as the red before lighting and the lit color cut into steps after it.
import bcs;

uniform float4 base_color;
uniform uint quantize_steps;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    var surface = bcs::surface(mesh);
    surface.base_color = base_color;

    // The surface can be changed before it is lit.
    surface.base_color.b = surface.base_color.r;

    // And the lit color before fog and the shader's own tonemapping, here cut into as many steps as
    // the material says.
    var color = bcs::light(surface, mesh);
    color = float4(uint4(color * float(quantize_steps))) / float(quantize_steps);
    color = bcs::finish(color, mesh);

    // And the finished color.
    return color * 2.0;
}

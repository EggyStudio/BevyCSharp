// Copies the first two slots of a material buffer into a buffer of plain numbers, three rows a
// slot, for the test to read back through the struct the prelude declares.

import bcs_compute;
import bcs_scene;

StructuredBuffer<bcs_scene::Material> materials;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    for (uint slot = 0; slot < 2; slot++)
    {
        let material = materials[slot];
        into[slot * 3 + 0] = material.base_color;
        into[slot * 3 + 1] = material.emissive;
        into[slot * 3 + 2] = float4(material.perceptual_roughness, material.metallic, material.reflectance, material.unlit);
    }
}

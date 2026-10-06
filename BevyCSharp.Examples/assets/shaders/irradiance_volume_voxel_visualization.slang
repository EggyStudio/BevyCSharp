// Bevy's irradiance_volume_voxel_visualization shader in Slang. Each voxel's cube is drawn in the
// light the irradiance volume gives it, unlit by anything else.
//
// Bevy's works out the middle of the voxel the fragment is in and then reads the volume at the
// fragment's own place, so the middle is left out here and the picture is the same.
import bcs;

// What the volume's light is multiplied by, which is divided out again, since at the volume's own
// intensity the cubes would be far too bright.
uniform float intensity;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return float4(bcs::irradiance(mesh, mesh.world_normal) / intensity, 1.0);
}

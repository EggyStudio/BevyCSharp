// Bevy's show_prepass shader in Slang, as a pass over the picture, showing the camera's depth, its
// normals or its motion vectors, whichever the settings name, or the picture as it is.
import bcs_pass;

struct ShowPrepassSettings
{
    uint show_depth;
    uint show_normals;
    uint show_motion_vectors;
    uint padding_1;
    uint padding_2;
};

uniform ShowPrepassSettings settings;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    if (settings.show_depth == 1u)
    {
        let depth = bcs_pass::depth_at(input.position);
        return float4(depth, depth, depth, 1.0);
    }
    else if (settings.show_normals == 1u)
    {
        return float4(bcs_pass::normal_at(input.position), 1.0);
    }
    else if (settings.show_motion_vectors == 1u)
    {
        return float4(bcs_pass::motion_at(input.position) / bcs_pass::globals.delta_time, 0.0, 1.0);
    }

    return bcs_pass::sample(input.uv);
}

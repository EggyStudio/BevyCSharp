// A pass that paints what the camera's environment map sends along each pixel's ray, at the
// camera's exposure, as a skybox drawn from the same cubemap shows there.

import bcs_pass;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let far = bcs_pass::world_position(input.uv, 1e-6);
    let direction = normalize(far - bcs_pass::view.world_position);
    let light = bcs_pass::environment_specular(direction, 0.0) * bcs_pass::view.exposure;
    return float4(light, 1.0);
}

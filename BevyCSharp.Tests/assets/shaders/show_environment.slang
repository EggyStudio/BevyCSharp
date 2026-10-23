// A pass that paints what the camera's environment map sends along each pixel's ray, at the
// camera's exposure, as a skybox drawn from the same cubemap shows there.

import bcs_pass;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    let far = passWorldPosition(input.uv, 1e-6);
    let direction = normalize(far - passView.world_position);
    let light = passEnvironmentSpecular(direction, 0.0) * passView.exposure;
    return float4(light, 1.0);
}

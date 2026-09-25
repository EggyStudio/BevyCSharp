// A pass that paints green wherever the camera's depth says something was drawn, and red where it
// says nothing was, which is depth zero, the far plane.

#import bevy_core_pipeline::fullscreen_vertex_shader::FullscreenVertexOutput

@group(0) @binding(14) var depth: texture_depth_2d;

@fragment
fn fragment(in: FullscreenVertexOutput) -> @location(0) vec4<f32> {
    let here = textureLoad(depth, vec2<i32>(in.position.xy), 0);
    return select(vec4<f32>(1.0, 0.0, 0.0, 1.0), vec4<f32>(0.0, 1.0, 0.0, 1.0), here > 0.0);
}

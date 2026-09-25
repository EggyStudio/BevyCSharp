// Paints the color its defines spell out, which is what makes one file many programs.
//
// A program with different defines is a different program, compiled on its own, so a scene with
// one of these per cube has as many programs as it has cubes.

#import bevy_pbr::forward_io::VertexOutput

@fragment
fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
    return vec4<f32>(f32(#{RED}), f32(#{GREEN}), f32(#{BLUE}), 1.0);
}

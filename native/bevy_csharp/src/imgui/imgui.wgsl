// What an interface is made of: clipped, textured triangles in screen space.
//
// The whole of the backend's shading. ImGui hands over triangles whose colours are what somebody
// typed into a palette, so they are sRGB; the target this draws into is linear, and the conversion
// belongs here rather than anywhere a colour is chosen.

struct Projection {
    matrix: mat4x4<f32>,
}

@group(0) @binding(0) var<uniform> projection: Projection;

@group(1) @binding(0) var picture: texture_2d<f32>;
@group(1) @binding(1) var picture_sampler: sampler;

struct VertexOut {
    @builtin(position) clip: vec4<f32>,
    @location(0) uv: vec2<f32>,
    @location(1) color: vec4<f32>,
}

fn to_linear(color: vec4<f32>) -> vec4<f32> {
    let low = color.rgb / 12.92;
    let high = pow((color.rgb + vec3(0.055)) / 1.055, vec3(2.4));
    let cutoff = color.rgb <= vec3(0.04045);

    return vec4(select(high, low, cutoff), color.a);
}

@vertex
fn vertex(
    @location(0) position: vec2<f32>,
    @location(1) uv: vec2<f32>,
    @location(2) color: vec4<f32>,
) -> VertexOut {
    var out: VertexOut;

    out.clip = projection.matrix * vec4(position, 0.0, 1.0);
    out.uv = uv;
    out.color = to_linear(color);

    return out;
}

@fragment
fn fragment(in: VertexOut) -> @location(0) vec4<f32> {
    return in.color * textureSample(picture, picture_sampler, in.uv);
}

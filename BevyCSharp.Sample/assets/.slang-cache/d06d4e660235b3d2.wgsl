// bevy_csharp slang cache 3
// source 8ee4fd7553216491
// end
@binding(1) @group(0) var lit_previous_0 : texture_2d<f32>;

@binding(1) @group(101) var picture_sampler_0 : sampler;

struct GlobalParams_std140_0
{
    @align(16) scale_0 : f32,
};

@binding(0) @group(0) var<uniform> globalParams_0 : GlobalParams_std140_0;
struct pixelOutput_0
{
    @location(0) output_0 : vec4<f32>,
};

struct pixelInput_0
{
    @location(0) uv_0 : vec2<f32>,
};

@fragment
fn fragment( _S1 : pixelInput_0, @builtin(position) position_0 : vec4<f32>) -> pixelOutput_0
{
    var _S2 : pixelOutput_0 = pixelOutput_0( vec4<f32>((textureSampleLevel((lit_previous_0), (picture_sampler_0), (_S1.uv_0), (0.0f))).xyz * vec3<f32>(globalParams_0.scale_0), 1.0f) );
    return _S2;
}


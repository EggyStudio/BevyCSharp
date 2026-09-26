// bevy_csharp slang cache 3
// source e8c930b863381854
// end
@binding(0) @group(0) var gi_0 : texture_2d<f32>;

@binding(1) @group(101) var picture_sampler_0 : sampler;

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
    var _S2 : pixelOutput_0 = pixelOutput_0( vec4<f32>((textureSampleLevel((gi_0), (picture_sampler_0), (_S1.uv_0), (0.0f))).xyz, 1.0f) );
    return _S2;
}


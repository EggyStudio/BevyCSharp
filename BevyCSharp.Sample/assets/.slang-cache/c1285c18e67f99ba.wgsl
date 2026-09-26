// bevy_csharp slang cache 3
// source 7ae9a50339ce6392
// end
@binding(3) @group(0) var gi_0 : texture_storage_2d<rgba16float, write>;

@binding(0) @group(101) var picture_0 : texture_2d<f32>;

@binding(6) @group(101) var motion_0 : texture_2d<f32>;

@binding(1) @group(0) var gathered_0 : texture_2d<f32>;

@binding(2) @group(0) var gi_previous_0 : texture_2d<f32>;

@binding(1) @group(101) var picture_sampler_0 : sampler;

struct GlobalParams_std140_0
{
    @align(16) blend_0 : f32,
};

@binding(0) @group(0) var<uniform> globalParams_0 : GlobalParams_std140_0;
fn bcs_pass_picture_size_0() -> vec2<u32>
{
    var width_0 : u32;
    var height_0 : u32;
    {var dim = textureDimensions((picture_0));((width_0)) = dim.x;((height_0)) = dim.y;};
    return vec2<u32>(width_0, height_0);
}

fn bcs_pass_load_motion_0( pixel_0 : vec2<i32>) -> vec2<f32>
{
    var _S1 : vec3<i32> = vec3<i32>(pixel_0, i32(0));
    return (textureLoad((motion_0), ((_S1)).xy, ((_S1)).z).xy);
}

fn bcs_pass_previous_uv_0( uv_0 : vec2<f32>,  moved_0 : vec2<f32>) -> vec2<f32>
{
    return uv_0 - moved_0;
}

@compute
@workgroup_size(8, 8, 1)
fn main(@builtin(global_invocation_id) id_0 : vec3<u32>)
{
    var width_1 : u32;
    var height_1 : u32;
    {var dim = textureDimensions((gi_0));((width_1)) = dim.x;((height_1)) = dim.y;};
    var _S2 : bool;
    if((id_0.x) >= width_1)
    {
        _S2 = true;
    }
    else
    {
        _S2 = (id_0.y) >= height_1;
    }
    if(_S2)
    {
        return;
    }
    var _S3 : vec2<u32> = id_0.xy;
    var _S4 : vec2<f32> = (vec2<f32>(_S3) + vec2<f32>(0.5f)) / vec2<f32>(f32(width_1), f32(height_1));
    var _S5 : vec2<f32> = bcs_pass_previous_uv_0(_S4, bcs_pass_load_motion_0(vec2<i32>(_S4 * vec2<f32>(bcs_pass_picture_size_0()))));
    var _S6 : vec3<i32> = vec3<i32>(vec2<i32>(_S3), i32(0));
    var _S7 : vec4<f32> = (textureLoad((gathered_0), ((_S6)).xy, ((_S6)).z));
    if((any((_S5 < vec2<f32>(0.0f)))))
    {
        _S2 = true;
    }
    else
    {
        _S2 = (any((_S5 > vec2<f32>(1.0f))));
    }
    if(_S2)
    {
        textureStore((gi_0), (_S3), (_S7));
        return;
    }
    textureStore((gi_0), (_S3), (mix((textureSampleLevel((gi_previous_0), (picture_sampler_0), (_S5), (0.0f))), _S7, vec4<f32>(globalParams_0.blend_0))));
    return;
}


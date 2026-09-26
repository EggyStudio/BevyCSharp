// bevy_csharp slang cache 3
// source 7ae9a50339ce6392
// end
@binding(3) @group(0) var gi_0 : texture_storage_2d<rgba16float, write>;

@binding(0) @group(101) var picture_0 : texture_2d<f32>;

struct bcs_pass_View_std140_0
{
    @align(16) clip_from_world_0 : array<vec4<f32>, i32(4)>,
    @align(16) unjittered_clip_from_world_0 : array<vec4<f32>, i32(4)>,
    @align(16) world_from_clip_0 : array<vec4<f32>, i32(4)>,
    @align(16) world_from_view_0 : array<vec4<f32>, i32(4)>,
    @align(16) view_from_world_0 : array<vec4<f32>, i32(4)>,
    @align(16) clip_from_view_0 : array<vec4<f32>, i32(4)>,
    @align(16) view_from_clip_0 : array<vec4<f32>, i32(4)>,
    @align(16) world_position_0 : vec3<f32>,
    @align(4) exposure_0 : f32,
    @align(16) viewport_0 : vec4<f32>,
    @align(16) main_pass_viewport_0 : vec4<f32>,
};

@binding(3) @group(101) var<uniform> view_0 : bcs_pass_View_std140_0;
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
    var _S1 : bool;
    if(width_0 <= u32(1))
    {
        _S1 = height_0 <= u32(1);
    }
    else
    {
        _S1 = false;
    }
    if(_S1)
    {
        return vec2<u32>(view_0.viewport_0.zw);
    }
    return vec2<u32>(width_0, height_0);
}

fn bcs_pass_load_motion_0( pixel_0 : vec2<i32>) -> vec2<f32>
{
    var _S2 : vec3<i32> = vec3<i32>(pixel_0, i32(0));
    return (textureLoad((motion_0), ((_S2)).xy, ((_S2)).z).xy);
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
    var _S3 : bool;
    if((id_0.x) >= width_1)
    {
        _S3 = true;
    }
    else
    {
        _S3 = (id_0.y) >= height_1;
    }
    if(_S3)
    {
        return;
    }
    var _S4 : vec2<u32> = id_0.xy;
    var _S5 : vec2<f32> = (vec2<f32>(_S4) + vec2<f32>(0.5f)) / vec2<f32>(f32(width_1), f32(height_1));
    var _S6 : vec2<f32> = bcs_pass_previous_uv_0(_S5, bcs_pass_load_motion_0(vec2<i32>(_S5 * vec2<f32>(bcs_pass_picture_size_0()))));
    var _S7 : vec3<i32> = vec3<i32>(vec2<i32>(_S4), i32(0));
    var _S8 : vec4<f32> = (textureLoad((gathered_0), ((_S7)).xy, ((_S7)).z));
    if((any((_S6 < vec2<f32>(0.0f)))))
    {
        _S3 = true;
    }
    else
    {
        _S3 = (any((_S6 > vec2<f32>(1.0f))));
    }
    if(_S3)
    {
        textureStore((gi_0), (_S4), (_S8));
        return;
    }
    textureStore((gi_0), (_S4), (mix((textureSampleLevel((gi_previous_0), (picture_sampler_0), (_S6), (0.0f))), _S8, vec4<f32>(globalParams_0.blend_0))));
    return;
}


// bevy_csharp slang cache 3
// source 10b973b03ef52009
// end
@binding(0) @group(0) var ambient_occlusion_0 : texture_2d<f32>;

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

struct pixelOutput_0
{
    @location(0) output_0 : vec4<f32>,
};

struct pixelInput_0
{
    @location(0) uv_0 : vec2<f32>,
};

@fragment
fn fragment( _S2 : pixelInput_0, @builtin(position) position_0 : vec4<f32>) -> pixelOutput_0
{
    var width_1 : u32;
    var height_1 : u32;
    {var dim = textureDimensions((ambient_occlusion_0));((width_1)) = dim.x;((height_1)) = dim.y;};
    var _S3 : vec3<i32> = vec3<i32>(vec2<i32>(position_0.xy * vec2<f32>(f32(width_1), f32(height_1)) / vec2<f32>(bcs_pass_picture_size_0())), i32(0));
    var _S4 : f32 = (textureLoad((ambient_occlusion_0), ((_S3)).xy, ((_S3)).z).x);
    var _S5 : pixelOutput_0 = pixelOutput_0( vec4<f32>(_S4, _S4, _S4, 1.0f) );
    return _S5;
}


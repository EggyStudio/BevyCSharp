// bevy_csharp slang cache 3
// source 2db1b97f71a2268d
// end
@binding(1) @group(0) var gbuffer_0 : texture_2d<u32>;

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
@binding(2) @group(0) var gi_0 : texture_2d<f32>;

@binding(1) @group(101) var picture_sampler_0 : sampler;

struct GlobalParams_std140_0
{
    @align(16) strength_0 : f32,
};

@binding(0) @group(0) var<uniform> globalParams_0 : GlobalParams_std140_0;
fn bcs_pass_unpack_unorm4x8_0( v_0 : u32) -> vec4<f32>
{
    return vec4<f32>(f32((v_0 & (u32(255)))), f32((((v_0 >> (u32(8)))) & (u32(255)))), f32((((v_0 >> (u32(16)))) & (u32(255)))), f32((((v_0 >> (u32(24)))) & (u32(255))))) / vec4<f32>(255.0f);
}

fn bcs_pass_unpack_rgb9e5_0( v_1 : u32) -> vec3<f32>
{
    return vec3<f32>(f32((v_1 & (u32(511)))), f32((((v_1 >> (u32(9)))) & (u32(511)))), f32((((v_1 >> (u32(18)))) & (u32(511))))) * vec3<f32>(exp2(f32(i32((((v_1 >> (u32(27)))) & (u32(31)))) - i32(15) - i32(9))));
}

fn bcs_pass_octahedral_decode_0( encoded_0 : vec2<f32>) -> vec3<f32>
{
    var _S1 : vec2<f32> = encoded_0 * vec2<f32>(2.0f) - vec2<f32>(1.0f);
    var _S2 : vec3<f32> = vec3<f32>(_S1, 1.0f - abs(_S1.x) - abs(_S1.y));
    var n_0 : vec3<f32> = _S2;
    var _S3 : f32 = saturate(- _S2.z);
    var _S4 : vec2<f32> = _S2.xy;
    var _S5 : vec2<f32> = _S4 + select(vec2<f32>(_S3), vec2<f32>(- _S3), _S4 >= vec2<f32>(0.0f));
    n_0.x = _S5.x;
    n_0.y = _S5.y;
    return normalize(n_0);
}

struct bcs_pass_Surface_0
{
     base_color_0 : vec3<f32>,
     perceptual_roughness_0 : f32,
     metallic_0 : f32,
     reflectance_0 : f32,
     diffuse_occlusion_0 : f32,
     emissive_0 : vec3<f32>,
     normal_0 : vec3<f32>,
     unlit_0 : bool,
     shadow_receiver_0 : bool,
};

fn bcs_pass_surface_of_0( texel_0 : vec4<u32>) -> bcs_pass_Surface_0
{
    var _S6 : u32 = texel_0.w;
    var _S7 : u32 = (((_S6 >> (u32(24)))) & (u32(255)));
    var surface_0 : bcs_pass_Surface_0;
    var _S8 : bool = ((_S7 & (u32(1)))) != u32(0);
    surface_0.unlit_0 = _S8;
    surface_0.shadow_receiver_0 = ((_S7 & (u32(4)))) != u32(0);
    var _S9 : vec4<f32> = bcs_pass_unpack_unorm4x8_0(texel_0.x);
    surface_0.perceptual_roughness_0 = _S9.w;
    surface_0.emissive_0 = bcs_pass_unpack_rgb9e5_0(texel_0.y);
    var _S10 : vec3<f32>;
    if(_S8)
    {
        _S10 = vec3<f32>(0.0f);
    }
    else
    {
        _S10 = pow(_S9.xyz, vec3<f32>(2.20000004768371582f));
    }
    surface_0.base_color_0 = _S10;
    var _S11 : vec4<f32> = bcs_pass_unpack_unorm4x8_0(texel_0.z);
    surface_0.reflectance_0 = _S11.x;
    surface_0.metallic_0 = _S11.y;
    surface_0.diffuse_occlusion_0 = _S11.z;
    surface_0.normal_0 = bcs_pass_octahedral_decode_0(vec2<f32>(f32((_S6 & (u32(4095)))), f32((((_S6 >> (u32(12)))) & (u32(4095))))) / vec2<f32>(4095.0f));
    return surface_0;
}

fn bcs_pass_picture_size_0() -> vec2<u32>
{
    var width_0 : u32;
    var height_0 : u32;
    {var dim = textureDimensions((picture_0));((width_0)) = dim.x;((height_0)) = dim.y;};
    var _S12 : bool;
    if(width_0 <= u32(1))
    {
        _S12 = height_0 <= u32(1);
    }
    else
    {
        _S12 = false;
    }
    if(_S12)
    {
        return vec2<u32>(view_0.viewport_0.zw);
    }
    return vec2<u32>(width_0, height_0);
}

struct pixelOutput_0
{
    @location(0) output_0 : vec4<f32>,
};

@fragment
fn fragment(@builtin(position) position_0 : vec4<f32>) -> pixelOutput_0
{
    var _S13 : vec2<f32> = position_0.xy;
    var _S14 : vec3<i32> = vec3<i32>(vec2<i32>(_S13), i32(0));
    var _S15 : bcs_pass_Surface_0 = bcs_pass_surface_of_0((textureLoad((gbuffer_0), ((_S14)).xy, ((_S14)).z)));
    var _S16 : pixelOutput_0 = pixelOutput_0( vec4<f32>(_S15.base_color_0 * vec3<f32>((1.0f - _S15.metallic_0)) * (textureSampleLevel((gi_0), (picture_sampler_0), (_S13 / vec2<f32>(bcs_pass_picture_size_0())), (0.0f))).xyz * vec3<f32>(globalParams_0.strength_0), 0.0f) );
    return _S16;
}


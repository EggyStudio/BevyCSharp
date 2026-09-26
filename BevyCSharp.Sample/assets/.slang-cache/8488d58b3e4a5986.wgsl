// bevy_csharp slang cache 3
// source 5988a792ee42ad5a
// end
@binding(3) @group(0) var gathered_0 : texture_storage_2d<rgba16float, write>;

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
@binding(4) @group(101) var depth_0 : texture_depth_2d;

@binding(1) @group(0) var gbuffer_0 : texture_2d<u32>;

@binding(16) @group(101) var blue_noise_layers_0 : texture_2d_array<f32>;

struct bcs_pass_Globals_0
{
     time_0 : f32,
     delta_time_0 : f32,
     frame_count_0 : u32,
};

@binding(2) @group(101) var<uniform> globals_0 : bcs_pass_Globals_0;
struct bcs_pass_SceneInfo_std140_0
{
    @align(16) point_lights_0 : u32,
    @align(4) environment_mips_0 : u32,
    @align(8) environment_intensity_0 : f32,
    @align(4) unused_0 : u32,
    @align(16) environment_rotation_0 : vec4<f32>,
};

@binding(13) @group(101) var<uniform> scene_info_0 : bcs_pass_SceneInfo_std140_0;
@binding(14) @group(101) var environment_diffuse_map_0 : texture_cube<f32>;

@binding(1) @group(101) var picture_sampler_0 : sampler;

struct bcs_pass_PreviousView_std140_0
{
    @align(16) view_from_world_1 : array<vec4<f32>, i32(4)>,
    @align(16) clip_from_world_1 : array<vec4<f32>, i32(4)>,
    @align(16) clip_from_view_1 : array<vec4<f32>, i32(4)>,
    @align(16) world_from_clip_1 : array<vec4<f32>, i32(4)>,
    @align(16) view_from_clip_1 : array<vec4<f32>, i32(4)>,
};

@binding(7) @group(101) var<uniform> previous_view_0 : bcs_pass_PreviousView_std140_0;
@binding(2) @group(0) var lit_previous_0 : texture_2d<f32>;

struct GlobalParams_std140_0
{
    @align(16) steps_0 : u32,
    @align(4) reach_0 : f32,
    @align(8) thickness_0 : f32,
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

fn bcs_pass_load_depth_0( pixel_0 : vec2<i32>) -> f32
{
    var _S2 : vec3<i32> = vec3<i32>(pixel_0, i32(0));
    return (textureLoad((depth_0), ((_S2)).xy, ((_S2)).z));
}

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
    var _S3 : vec2<f32> = encoded_0 * vec2<f32>(2.0f) - vec2<f32>(1.0f);
    var _S4 : vec3<f32> = vec3<f32>(_S3, 1.0f - abs(_S3.x) - abs(_S3.y));
    var n_0 : vec3<f32> = _S4;
    var _S5 : f32 = saturate(- _S4.z);
    var _S6 : vec2<f32> = _S4.xy;
    var _S7 : vec2<f32> = _S6 + select(vec2<f32>(_S5), vec2<f32>(- _S5), _S6 >= vec2<f32>(0.0f));
    n_0.x = _S7.x;
    n_0.y = _S7.y;
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
    var _S8 : u32 = texel_0.w;
    var _S9 : u32 = (((_S8 >> (u32(24)))) & (u32(255)));
    var surface_0 : bcs_pass_Surface_0;
    var _S10 : bool = ((_S9 & (u32(1)))) != u32(0);
    surface_0.unlit_0 = _S10;
    surface_0.shadow_receiver_0 = ((_S9 & (u32(4)))) != u32(0);
    var _S11 : vec4<f32> = bcs_pass_unpack_unorm4x8_0(texel_0.x);
    surface_0.perceptual_roughness_0 = _S11.w;
    surface_0.emissive_0 = bcs_pass_unpack_rgb9e5_0(texel_0.y);
    var _S12 : vec3<f32>;
    if(_S10)
    {
        _S12 = vec3<f32>(0.0f);
    }
    else
    {
        _S12 = pow(_S11.xyz, vec3<f32>(2.20000004768371582f));
    }
    surface_0.base_color_0 = _S12;
    var _S13 : vec4<f32> = bcs_pass_unpack_unorm4x8_0(texel_0.z);
    surface_0.reflectance_0 = _S13.x;
    surface_0.metallic_0 = _S13.y;
    surface_0.diffuse_occlusion_0 = _S13.z;
    surface_0.normal_0 = bcs_pass_octahedral_decode_0(vec2<f32>(f32((_S8 & (u32(4095)))), f32((((_S8 >> (u32(12)))) & (u32(4095))))) / vec2<f32>(4095.0f));
    return surface_0;
}

fn bcs_pass_pixel_size_0() -> vec2<f32>
{
    return vec2<f32>(1.0f) / vec2<f32>(bcs_pass_picture_size_0());
}

fn bcs_pass_uv_of_0( pixel_1 : vec2<i32>) -> vec2<f32>
{
    return (vec2<f32>(pixel_1) + vec2<f32>(0.5f)) * bcs_pass_pixel_size_0();
}

fn bcs_pass_transform_0( _S14 : vec4<f32>) -> vec4<f32>
{
    return view_0.world_from_clip_0[i32(0)] * vec4<f32>(_S14.x) + view_0.world_from_clip_0[i32(1)] * vec4<f32>(_S14.y) + view_0.world_from_clip_0[i32(2)] * vec4<f32>(_S14.z) + view_0.world_from_clip_0[i32(3)] * vec4<f32>(_S14.w);
}

fn bcs_pass_world_position_0( uv_0 : vec2<f32>,  depth_value_0 : f32) -> vec3<f32>
{
    var _S15 : vec4<f32> = bcs_pass_transform_0(vec4<f32>(uv_0.x * 2.0f - 1.0f, 1.0f - uv_0.y * 2.0f, depth_value_0, 1.0f));
    return _S15.xyz / vec3<f32>(_S15.w);
}

fn bcs_pass_blue_noise_0( pixel_2 : vec2<i32>) -> vec4<f32>
{
    var width_1 : u32;
    var height_1 : u32;
    var layers_0 : u32;
    {var dim = textureDimensions((blue_noise_layers_0));((width_1)) = dim.x;((height_1)) = dim.y;((layers_0)) = textureNumLayers((blue_noise_layers_0));};
    var _S16 : vec2<u32> = vec2<u32>(pixel_2) % vec2<u32>(width_1, height_1);
    var _S17 : vec2<i32> = vec2<i32>(_S16);
    var _S18 : u32 = globals_0.frame_count_0 % layers_0;
    var _S19 : vec4<i32> = vec4<i32>(_S17, i32(_S18), i32(0));
    return (textureLoad((blue_noise_layers_0), ((_S19)).xy, i32(((_S19)).z), ((_S19)).w));
}

fn cosine_direction_0( normal_1 : vec3<f32>,  random_0 : vec2<f32>) -> vec3<f32>
{
    var _S20 : f32 = 6.28318548202514648f * random_0.x;
    var _S21 : f32 = random_0.y;
    var _S22 : f32 = sqrt(_S21);
    var _S23 : f32 = _S22 * cos(_S20);
    var _S24 : f32 = _S22 * sin(_S20);
    var _S25 : f32 = sqrt(max(0.0f, 1.0f - _S21));
    var _S26 : vec3<f32>;
    if((abs(normal_1.y)) < 0.99900001287460327f)
    {
        _S26 = vec3<f32>(0.0f, 1.0f, 0.0f);
    }
    else
    {
        _S26 = vec3<f32>(1.0f, 0.0f, 0.0f);
    }
    var _S27 : vec3<f32> = normalize(cross(_S26, normal_1));
    return normalize(_S27 * vec3<f32>(_S23) + cross(normal_1, _S27) * vec3<f32>(_S24) + normal_1 * vec3<f32>(_S25));
}

fn bcs_pass_has_environment_0() -> bool
{
    return (scene_info_0.environment_mips_0) > u32(0);
}

fn bcs_pass_environment_direction_0( direction_0 : vec3<f32>) -> vec3<f32>
{
    var _S28 : vec3<f32> = scene_info_0.environment_rotation_0.xyz;
    var _S29 : vec3<f32> = vec3<f32>(2.0f) * cross(_S28, direction_0);
    var _S30 : vec3<f32> = direction_0 + vec3<f32>(scene_info_0.environment_rotation_0.w) * _S29 + cross(_S28, _S29);
    var turned_0 : vec3<f32> = _S30;
    turned_0[i32(2)] = - _S30.z;
    return turned_0;
}

fn bcs_pass_environment_diffuse_0( normal_2 : vec3<f32>) -> vec3<f32>
{
    if(!bcs_pass_has_environment_0())
    {
        return vec3<f32>(0.0f);
    }
    return (textureSampleLevel((environment_diffuse_map_0), (picture_sampler_0), (bcs_pass_environment_direction_0(normal_2)), (0.0f))).xyz * vec3<f32>(scene_info_0.environment_intensity_0);
}

fn bcs_pass_transform_1( _S31 : vec4<f32>) -> vec4<f32>
{
    return view_0.view_from_world_0[i32(0)] * vec4<f32>(_S31.x) + view_0.view_from_world_0[i32(1)] * vec4<f32>(_S31.y) + view_0.view_from_world_0[i32(2)] * vec4<f32>(_S31.z) + view_0.view_from_world_0[i32(3)] * vec4<f32>(_S31.w);
}

fn bcs_pass_view_depth_0( world_0 : vec3<f32>) -> f32
{
    return - bcs_pass_transform_1(vec4<f32>(world_0, 1.0f)).z;
}

fn bcs_pass_distance_from_depth_0( depth_value_1 : f32) -> f32
{
    return view_0.clip_from_view_0[i32(3)][i32(2)] / max(depth_value_1, 1.00000001168609742e-07f);
}

fn bcs_pass_transform_2( _S32 : vec4<f32>) -> vec4<f32>
{
    return previous_view_0.clip_from_world_1[i32(0)] * vec4<f32>(_S32.x) + previous_view_0.clip_from_world_1[i32(1)] * vec4<f32>(_S32.y) + previous_view_0.clip_from_world_1[i32(2)] * vec4<f32>(_S32.z) + previous_view_0.clip_from_world_1[i32(3)] * vec4<f32>(_S32.w);
}

fn bcs_pass_previous_uv_of_0( world_1 : vec3<f32>) -> vec2<f32>
{
    var _S33 : vec4<f32> = bcs_pass_transform_2(vec4<f32>(world_1, 1.0f));
    var _S34 : vec2<f32> = _S33.xy / vec2<f32>(_S33.w);
    return vec2<f32>(_S34.x * 0.5f + 0.5f, 0.5f - _S34.y * 0.5f);
}

fn bcs_pass_transform_3( _S35 : vec4<f32>) -> vec4<f32>
{
    return view_0.clip_from_world_0[i32(0)] * vec4<f32>(_S35.x) + view_0.clip_from_world_0[i32(1)] * vec4<f32>(_S35.y) + view_0.clip_from_world_0[i32(2)] * vec4<f32>(_S35.z) + view_0.clip_from_world_0[i32(3)] * vec4<f32>(_S35.w);
}

@compute
@workgroup_size(8, 8, 1)
fn main(@builtin(global_invocation_id) id_0 : vec3<u32>)
{
    var light_0 : vec3<f32>;
    var width_2 : u32;
    var height_2 : u32;
    {var dim = textureDimensions((gathered_0));((width_2)) = dim.x;((height_2)) = dim.y;};
    var _S36 : bool;
    if((id_0.x) >= width_2)
    {
        _S36 = true;
    }
    else
    {
        _S36 = (id_0.y) >= height_2;
    }
    if(_S36)
    {
        return;
    }
    var _S37 : vec2<u32> = bcs_pass_picture_size_0();
    var _S38 : vec2<u32> = id_0.xy;
    var _S39 : vec2<i32> = vec2<i32>(_S38 * vec2<u32>(u32(2)));
    var _S40 : f32 = bcs_pass_load_depth_0(_S39);
    if(_S40 <= 0.0f)
    {
        textureStore((gathered_0), (_S38), (vec4<f32>(0.0f)));
        return;
    }
    var _S41 : vec3<i32> = vec3<i32>(_S39, i32(0));
    var _S42 : bcs_pass_Surface_0 = bcs_pass_surface_of_0((textureLoad((gbuffer_0), ((_S41)).xy, ((_S41)).z)));
    var _S43 : vec3<f32> = bcs_pass_world_position_0(bcs_pass_uv_of_0(_S39), _S40);
    var _S44 : vec4<f32> = bcs_pass_blue_noise_0(vec2<i32>(_S38));
    var _S45 : vec3<f32> = cosine_direction_0(_S42.normal_0, _S44.xy);
    var _S46 : vec3<f32> = bcs_pass_environment_diffuse_0(_S45) * vec3<f32>(view_0.exposure_0);
    var _S47 : vec3<f32> = _S43 + _S42.normal_0 * vec3<f32>(0.01999999955296516f);
    var index_0 : u32 = u32(0);
    for(;;)
    {
        if(index_0 < (globalParams_0.steps_0))
        {
        }
        else
        {
            light_0 = _S46;
            break;
        }
        var _S48 : vec3<f32> = _S47 + _S45 * vec3<f32>((globalParams_0.reach_0 * (f32(index_0) + _S44.z) / f32(globalParams_0.steps_0)));
        var _S49 : vec4<f32> = bcs_pass_transform_3(vec4<f32>(_S48, 1.0f));
        var _S50 : f32 = _S49.w;
        if(_S50 <= 0.0f)
        {
            light_0 = _S46;
            break;
        }
        var _S51 : vec2<f32> = _S49.xy / vec2<f32>(_S50);
        var _S52 : vec2<f32> = vec2<f32>(_S51.x * 0.5f + 0.5f, 0.5f - _S51.y * 0.5f);
        if((any((_S52 < vec2<f32>(0.0f)))))
        {
            _S36 = true;
        }
        else
        {
            _S36 = (any((_S52 > vec2<f32>(1.0f))));
        }
        if(_S36)
        {
            light_0 = _S46;
            break;
        }
        var _S53 : f32 = bcs_pass_load_depth_0(vec2<i32>(_S52 * vec2<f32>(_S37)));
        if(_S53 <= 0.0f)
        {
            index_0 = index_0 + u32(1);
            continue;
        }
        var _S54 : f32 = bcs_pass_view_depth_0(_S48) - bcs_pass_distance_from_depth_0(_S53);
        var _S55 : bool;
        if(_S54 > 0.0f)
        {
            _S55 = _S54 < (globalParams_0.thickness_0);
        }
        else
        {
            _S55 = false;
        }
        if(_S55)
        {
            light_0 = (textureSampleLevel((lit_previous_0), (picture_sampler_0), (bcs_pass_previous_uv_of_0(bcs_pass_world_position_0(_S52, _S53))), (0.0f))).xyz;
            break;
        }
        index_0 = index_0 + u32(1);
    }
    textureStore((gathered_0), (_S38), (vec4<f32>(light_0, 1.0f)));
    return;
}


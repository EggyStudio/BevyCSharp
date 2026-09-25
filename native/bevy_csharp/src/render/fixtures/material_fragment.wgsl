@binding(1) @group(0) var albedo_0 : texture_2d<f32>;

@binding(2) @group(0) var albedo_sampler_0 : sampler;

struct bcs_Globals_std140_0
{
    @align(16) time_0 : f32,
    @align(4) delta_time_0 : f32,
    @align(8) frame_count_0 : u32,
};

@binding(11) @group(100) var<uniform> globals_0 : bcs_Globals_std140_0;
@binding(3) @group(0) var layers_0 : array<texture_2d<f32>, i32(64)>;

@binding(4) @group(0) var skies_0 : array<texture_cube<f32>, i32(16)>;

@binding(5) @group(0) var fog_0 : texture_3d<f32>;

@binding(6) @group(0) var<storage, read> points_0 : array<vec4<f32>>;

@binding(7) @group(0) var<storage, read> raw_0 : array<u32>;

struct Light_std140_0
{
    @align(16) color_0 : vec3<f32>,
    @align(4) power_0 : f32,
};

@binding(8) @group(0) var<uniform> sun_0 : Light_std140_0;
struct _Array_std140_float1000_0
{
    @align(16) data_0 : array<vec4<f32>, i32(1000)>,
};

struct _Array_std140_Light4_0
{
    @align(16) data_1 : array<Light_std140_0, i32(4)>,
};

struct _MatrixStorage_float4x4_ColMajorstd140_0
{
    @align(16) data_2 : array<vec4<f32>, i32(4)>,
};

struct GlobalParams_std140_0
{
    @align(16) roughness_0 : f32,
    @align(16) tint_0 : vec4<f32>,
    @align(16) weights_0 : _Array_std140_float1000_0,
    @align(16) lights_0 : _Array_std140_Light4_0,
    @align(16) twist_0 : _MatrixStorage_float4x4_ColMajorstd140_0,
    @align(16) mode_0 : i32,
};

@binding(0) @group(0) var<uniform> globalParams_0 : GlobalParams_std140_0;
struct pixelOutput_0
{
    @location(0) output_0 : vec4<f32>,
};

struct pixelInput_0
{
    @location(0) world_position_0 : vec4<f32>,
    @location(1) world_normal_0 : vec3<f32>,
    @location(2) uv_0 : vec2<f32>,
};

@fragment
fn fragment( _S1 : pixelInput_0, @builtin(position) position_0 : vec4<f32>) -> pixelOutput_0
{
    var c_0 : vec4<f32> = (textureSample((albedo_0), (albedo_sampler_0), (_S1.uv_0))) * globalParams_0.tint_0 * vec4<f32>(globalParams_0.roughness_0) * vec4<f32>(globalParams_0.weights_0.data_0[i32(999)].x) * vec4<f32>(globals_0.time_0) + ((textureSample((layers_0[i32(63)]), (albedo_sampler_0), (_S1.uv_0))) + (textureSample((skies_0[i32(15)]), (albedo_sampler_0), (_S1.world_normal_0))) + (textureSample((fog_0), (albedo_sampler_0), (_S1.world_position_0.xyz))));
    var _S2 : vec4<f32> = points_0[i32(3)] + vec4<f32>(globalParams_0.lights_0.data_1[i32(3)].color_0, globalParams_0.lights_0.data_1[i32(3)].power_0) + (((c_0) * (mat4x4<f32>(globalParams_0.twist_0.data_2[i32(0)][i32(0)], globalParams_0.twist_0.data_2[i32(1)][i32(0)], globalParams_0.twist_0.data_2[i32(2)][i32(0)], globalParams_0.twist_0.data_2[i32(3)][i32(0)], globalParams_0.twist_0.data_2[i32(0)][i32(1)], globalParams_0.twist_0.data_2[i32(1)][i32(1)], globalParams_0.twist_0.data_2[i32(2)][i32(1)], globalParams_0.twist_0.data_2[i32(3)][i32(1)], globalParams_0.twist_0.data_2[i32(0)][i32(2)], globalParams_0.twist_0.data_2[i32(1)][i32(2)], globalParams_0.twist_0.data_2[i32(2)][i32(2)], globalParams_0.twist_0.data_2[i32(3)][i32(2)], globalParams_0.twist_0.data_2[i32(0)][i32(3)], globalParams_0.twist_0.data_2[i32(1)][i32(3)], globalParams_0.twist_0.data_2[i32(2)][i32(3)], globalParams_0.twist_0.data_2[i32(3)][i32(3)])))) + vec4<f32>(f32(globalParams_0.mode_0));
    var _S3 : u32 = raw_0[(u32(0))/4];
    var _S4 : f32 = bitcast<f32>(_S3);
    var _S5 : u32 = raw_0[(u32(4))/4];
    var _S6 : f32 = bitcast<f32>(_S5);
    var _S7 : u32 = raw_0[(u32(8))/4];
    var _S8 : f32 = bitcast<f32>(_S7);
    var _S9 : u32 = raw_0[(u32(12))/4];
    var _S10 : pixelOutput_0 = pixelOutput_0( c_0 + (_S2 + vec4<f32>(_S4, _S6, _S8, bitcast<f32>(_S9)) + vec4<f32>(sun_0.color_0, sun_0.power_0)) );
    return _S10;
}


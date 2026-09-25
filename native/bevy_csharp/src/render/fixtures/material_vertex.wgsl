struct bcs_Mesh_std430_0
{
    @align(16) world_from_local_0 : array<vec4<f32>, i32(3)>,
    @align(16) previous_world_from_local_0 : array<vec4<f32>, i32(3)>,
    @align(16) local_from_world_transpose_a_0 : array<vec4<f32>, i32(2)>,
    @align(16) local_from_world_transpose_b_0 : f32,
    @align(4) flags_0 : u32,
    @align(8) lightmap_uv_rect_0 : vec2<u32>,
    @align(16) first_vertex_index_0 : u32,
    @align(4) current_skin_index_0 : u32,
    @align(8) material_and_lightmap_bind_group_slot_0 : u32,
    @align(4) tag_0 : u32,
    @align(16) morph_descriptor_index_0 : u32,
};

@binding(0) @group(102) var<storage, read> meshes_0 : array<bcs_Mesh_std430_0>;

struct bcs_View_std140_0
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

@binding(0) @group(100) var<uniform> view_0 : bcs_View_std140_0;
struct _Array_std140_float1000_0
{
    @align(16) data_0 : array<vec4<f32>, i32(1000)>,
};

struct Light_std140_0
{
    @align(16) color_0 : vec3<f32>,
    @align(4) power_0 : f32,
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
fn bcs_local_to_world_0( instance_0 : u32,  position_0 : vec3<f32>) -> vec3<f32>
{
    var _S1 : vec4<f32> = vec4<f32>(position_0, 1.0f);
    return vec3<f32>(dot(meshes_0[instance_0].world_from_local_0[i32(0)], _S1), dot(meshes_0[instance_0].world_from_local_0[i32(1)], _S1), dot(meshes_0[instance_0].world_from_local_0[i32(2)], _S1));
}

fn bcs_transform_0( _S2 : vec4<f32>) -> vec4<f32>
{
    return view_0.clip_from_world_0[i32(0)] * vec4<f32>(_S2.x) + view_0.clip_from_world_0[i32(1)] * vec4<f32>(_S2.y) + view_0.clip_from_world_0[i32(2)] * vec4<f32>(_S2.z) + view_0.clip_from_world_0[i32(3)] * vec4<f32>(_S2.w);
}

fn bcs_world_to_clip_0( position_1 : vec3<f32>) -> vec4<f32>
{
    return bcs_transform_0(vec4<f32>(position_1, 1.0f));
}

fn bcs_normal_local_to_world_0( instance_1 : u32,  normal_0 : vec3<f32>) -> vec3<f32>
{
    var _S3 : vec3<f32> = meshes_0[instance_1].local_from_world_transpose_a_0[i32(0)].xyz * vec3<f32>(normal_0.x) + vec3<f32>(meshes_0[instance_1].local_from_world_transpose_a_0[i32(0)].w, meshes_0[instance_1].local_from_world_transpose_a_0[i32(1)].xy) * vec3<f32>(normal_0.y) + vec3<f32>(meshes_0[instance_1].local_from_world_transpose_a_0[i32(1)].zw, meshes_0[instance_1].local_from_world_transpose_b_0) * vec3<f32>(normal_0.z);
    var _S4 : vec3<f32>;
    if((any((normal_0 != vec3<f32>(0.0f)))))
    {
        _S4 = normalize(_S3);
    }
    else
    {
        _S4 = _S3;
    }
    return _S4;
}

struct bcs_VertexOutput_0
{
    @builtin(position) position_2 : vec4<f32>,
    @location(0) world_position_1 : vec4<f32>,
    @location(1) world_normal_0 : vec3<f32>,
    @location(2) uv_0 : vec2<f32>,
};

struct bcs_Vertex_0
{
     position_3 : vec3<f32>,
     normal_1 : vec3<f32>,
     uv_1 : vec2<f32>,
     instance_index_0 : u32,
};

fn bcs_standard_vertex_0( vertex_0 : bcs_Vertex_0) -> bcs_VertexOutput_0
{
    var _S5 : vec3<f32> = bcs_local_to_world_0(vertex_0.instance_index_0, vertex_0.position_3);
    var output_0 : bcs_VertexOutput_0;
    output_0.world_position_1 = vec4<f32>(_S5, 1.0f);
    output_0.position_2 = bcs_world_to_clip_0(_S5);
    output_0.world_normal_0 = bcs_normal_local_to_world_0(vertex_0.instance_index_0, vertex_0.normal_1);
    output_0.uv_0 = vertex_0.uv_1;
    return output_0;
}

struct vertexInput_0
{
    @location(0) position_4 : vec3<f32>,
    @location(1) normal_2 : vec3<f32>,
    @location(2) uv_2 : vec2<f32>,
};

@vertex
fn vertex( _S6 : vertexInput_0, @builtin(instance_index) instance_index_1 : u32) -> bcs_VertexOutput_0
{
    var _S7 : bcs_Vertex_0;
    _S7.position_3 = _S6.position_4;
    _S7.normal_1 = _S6.normal_2;
    _S7.uv_1 = _S6.uv_2;
    _S7.instance_index_0 = instance_index_1;
    _S7.position_3[i32(1)] = _S7.position_3[i32(1)] + globalParams_0.weights_0.data_0[i32(2)].x * globalParams_0.roughness_0;
    return bcs_standard_vertex_0(_S7);
}


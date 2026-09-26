// The layout of what the engine writes about the scene into buffers a shader reads, which any
// shader can reach with `import bcs_scene;`, whatever it runs as.
//
// Nothing here is bound. A buffer the engine keeps (see `Shaders.CreateInstanceBuffer`) is handed
// to a shader by name like any other, and this says how to read it.

module bcs_scene;

public namespace bcs_scene
{

/// One slot of an instance buffer: an entity's transform this frame and on the previous one.
///
/// Matrices are four columns, which `transform` multiplies out. A slot with no entity in it holds
/// zeros, which transform everything to the origin.
public struct Instance
{
    public float4 world_from_local[4];
    public float4 previous_world_from_local[4];
};

/// Multiplies a matrix held as four columns by a vector.
public float4 transform(float4 columns[4], float4 v)
{
    return columns[0] * v.x + columns[1] * v.y + columns[2] * v.z + columns[3] * v.w;
}

/// Where a point on an instance is in the world this frame.
public float3 to_world(Instance instance, float3 local)
{
    return transform(instance.world_from_local, float4(local, 1.0)).xyz;
}

/// Where the same point was on the previous frame, which is what motion and reprojection need.
public float3 to_previous_world(Instance instance, float3 local)
{
    return transform(instance.previous_world_from_local, float4(local, 1.0)).xyz;
}

/// Where an instance's origin is this frame.
public float3 position(Instance instance)
{
    return instance.world_from_local[3].xyz;
}

/// Where an instance's origin was on the previous frame.
public float3 previous_position(Instance instance)
{
    return instance.previous_world_from_local[3].xyz;
}

}

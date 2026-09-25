// What a Slang shader pass sees of the camera it runs on: the picture drawn so far, time, the view,
// and the depth and normals the camera's prepass drew.
//
// A shader reaches it with `import bcs_pass;`, and everything in it is under the `bcs_pass`
// namespace. What the pass itself carries is whatever the shader declares: every global without a
// binding of its own belongs to the pass and is set from C# by its name. The inputs below are in
// space 101, which the bridge moves to group one after the compile, so the pass's own globals keep
// group zero to themselves whatever they are.
//
// A pass's entry point is a fragment shader taking `bcs_pass::Input`, which is what Bevy's
// full-screen triangle writes, and returning the pixel.

module bcs_pass;

public namespace bcs_pass
{

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

/// The part of Bevy's view uniform a pass usually reads, the same prefix `bcs::View` is.
public struct View
{
    public float4 clip_from_world[4];
    public float4 unjittered_clip_from_world[4];
    public float4 world_from_clip[4];
    public float4 world_from_view[4];
    public float4 view_from_world[4];
    public float4 clip_from_view[4];
    public float4 view_from_clip[4];
    public float3 world_position;
    public float exposure;
    public float4 viewport;
    public float4 main_pass_viewport;
};

/// The picture so far, and the sampler it is read with, which is linear and clamped at the edges.
[[vk::binding(0, 101)]] public Texture2D picture;
[[vk::binding(1, 101)]] public SamplerState picture_sampler;

/// Time.
///
/// Laid out as C lays it out (`ScalarDataLayout`), which is twelve bytes and what Bevy binds. The
/// usual constant buffer rules round the struct up to sixteen, and a pipeline refuses a shader
/// that reads more of a buffer than its layout promises.
[[vk::binding(2, 101)]] public ConstantBuffer<Globals, ScalarDataLayout> globals;
[[vk::binding(3, 101)]] public ConstantBuffer<View> view;

/// The camera's depth, from its prepass, or zero, which is the far plane, where it draws none.
///
/// Bevy's depth runs backwards, from one at the near plane to zero infinitely far away, so a
/// larger number is nearer. `Shaders.SetPrepass` asks a camera for it.
[[vk::binding(4, 101)]] public DepthTexture2D depth;

/// The camera's normals, from its prepass, each packed from minus one to one into zero to one, or
/// white where it draws none.
[[vk::binding(5, 101)]] public Texture2D normals;

/// What Bevy's full-screen triangle hands the fragment shader.
public struct Input
{
    public float4 position : SV_Position;
    /// Where the pixel is on the picture, from zero at the top left to one at the bottom right.
    public float2 uv : TEXCOORD0;
};

/// The picture so far at a point, from zero to one across it.
public float4 sample(float2 uv)
{
    return picture.Sample(picture_sampler, uv);
}

/// How far apart two neighbouring pixels are in `uv`, which is what a blur steps by.
public float2 pixel_size()
{
    uint width;
    uint height;
    picture.GetDimensions(width, height);
    return float2(1.0 / float(width), 1.0 / float(height));
}

/// The depth at a pixel, given the pixel's `SV_Position`.
public float depth_at(float4 position)
{
    return depth.Load(int3(int2(position.xy), 0));
}

/// How far in front of the camera the surface at a pixel is, in world units.
///
/// Worked out from the depth the way Bevy's own `depth_ndc_to_view_z` does for a perspective
/// camera. Infinitely far, where nothing was drawn.
public float distance_at(float4 position)
{
    let near = view.clip_from_view[3][2];
    return near / max(depth_at(position), 1e-7);
}

/// The world-space normal of the surface at a pixel.
public float3 normal_at(float4 position)
{
    return normals.Load(int3(int2(position.xy), 0)).xyz * 2.0 - 1.0;
}

}

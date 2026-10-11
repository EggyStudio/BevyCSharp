// What a BevyCSharp interface material shader sees of Bevy: the view, time, and the struct Bevy's
// own interface material vertex shader hands a fragment shader, the node's place on it, its size,
// its borders and its corners.
//
// A shader drawing an interface node reaches it with `import bcs_ui;`, and everything in it is under
// the `bcs_ui` namespace. What the material carries is whatever the shader declares, as for a 3D
// material, and is set from C# by name. Bevy's interface material pipeline binds the view in group
// zero and the material in group one, and the bindings below are in space 100, which the bridge
// moves to group zero after the compile, out of the way of the material's.
//
// Locations are `TEXCOORDn` semantics, as in `bcs`, the index being the location, and the structs
// below line up with the ones Bevy's own interface material WGSL declares.

module bcs_ui;

public namespace bcs_ui
{

// -- The view, in Bevy's group zero

/// The part of Bevy's view uniform a shader usually reads, in the order Bevy declares it.
///
/// Matrices are arrays of four columns, which `transform` multiplies out. An interface camera's
/// world is its window in pixels, so `clip_from_world` takes a point on the window to clip space.
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
    /// Where the view is on its target: x, y, width and height, in pixels.
    public float4 viewport;
    public float4 main_pass_viewport;
};

[[vk::binding(0, 100)]] public ConstantBuffer<View> view;

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

/// Time, laid out as C lays it out (`ScalarDataLayout`) in the twelve bytes Bevy binds.
[[vk::binding(1, 100)]] public ConstantBuffer<Globals, ScalarDataLayout> globals;

/// Multiplies a column-major matrix, held as four columns, by a vector.
public float4 transform(float4 columns[4], float4 v)
{
    return columns[0] * v.x + columns[1] * v.y + columns[2] * v.z + columns[3] * v.w;
}

// -- What passes between stages

/// A corner of the node's quad, as Bevy's interface material pipeline hands it to a vertex shader.
public struct Vertex
{
    /// Where the corner is on the window, in pixels.
    public float3 position : TEXCOORD0;
    public float2 uv : TEXCOORD1;
    /// The node's width and height, in pixels.
    public float2 size : TEXCOORD2;
    /// The node's borders, left, top, right and bottom, in pixels.
    public float4 border_widths : TEXCOORD3;
    /// How far each corner's rounding reaches across, top left, top right, bottom right and
    /// bottom left, in pixels.
    public float4 border_radius_x : TEXCOORD4;
    /// How far the same reaches down.
    public float4 border_radius_y : TEXCOORD5;
};

/// What Bevy's own interface material vertex shader hands the fragment shader.
///
/// A fragment shader taking this works with Bevy's vertex shader as well as with one written here,
/// which is why an interface material may name a fragment shader alone.
public struct VertexOutput
{
    /// Where on the node the fragment is, from the top left at zero to the bottom right at one.
    public float2 uv : TEXCOORD0;
    /// The node's borders, left, top, right and bottom, in pixels.
    public float4 border_widths : TEXCOORD1;
    /// How far each corner's rounding reaches across, top left, top right, bottom right and
    /// bottom left, in pixels.
    public float4 border_radius_x : TEXCOORD2;
    /// How far the same reaches down.
    public float4 border_radius_y : TEXCOORD3;
    /// The node's width and height, in pixels, the same at every fragment.
    public nointerpolation float2 size : TEXCOORD4;
    public float4 position : SV_Position;
};

/// Does what Bevy's own interface material vertex shader does, for a shader that starts from it.
public VertexOutput standard_vertex(Vertex vertex)
{
    VertexOutput output;
    output.uv = vertex.uv;
    output.position = transform(view.clip_from_world, float4(vertex.position, 1.0));
    output.size = vertex.size;
    output.border_widths = vertex.border_widths;
    output.border_radius_x = vertex.border_radius_x;
    output.border_radius_y = vertex.border_radius_y;
    return output;
}

}

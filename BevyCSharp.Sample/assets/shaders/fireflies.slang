// Draws each firefly as a glowing square turned to face the camera, placed from the buffer the
// compute shader moves.
//
// The mesh is one square per firefly with its corners at the origin, so where each one is comes
// entirely from the buffer. The glow is far brighter than white, which is what the camera's bloom
// spreads into a halo. The material blends by adding, so it is never drawn in the prepass, and the
// vertex and fragment shaders can hand each other a struct of their own.

import bcs;

struct Firefly
{
    float3 position;
    float phase;
    float3 home;
    float speed;
};

StructuredBuffer<Firefly> flies;

/// The glow's color, with how bright it is in `w`.
uniform float4 glow;

/// How large a firefly is, in world units.
uniform float size;

/// How fast the glow flickers, in cycles a second.
uniform float flicker_rate;

struct Corner
{
    float3 offset : TEXCOORD0;
    float2 uv : TEXCOORD2;
    uint instance_index : SV_InstanceID;
    uint vertex_id : SV_VertexID;
};

struct Glow
{
    float4 position : SV_Position;
    float2 uv : TEXCOORD0;
    nointerpolation float phase : TEXCOORD1;
};

[shader("vertex")]
Glow vertex(Corner corner)
{
    let fly = flies[bcs::vertex_number(corner.instance_index, corner.vertex_id) / 4];

    // The camera's own right and up, so the square always faces it.
    let right = bcs::view.world_from_view[0].xyz;
    let up = bcs::view.world_from_view[1].xyz;
    let world = fly.position + (right * corner.offset.x + up * corner.offset.y) * size;

    Glow output;
    output.position = bcs::world_to_clip(world);
    output.uv = corner.uv;
    output.phase = fly.phase;
    return output;
}

[shader("fragment")]
float4 fragment(Glow input) : SV_Target
{
    let flicker = 0.6 + 0.4 * sin(bcs::globals.time * flicker_rate + input.phase * 13.0);
    let strength = pow(saturate(1.0 - distance(input.uv, float2(0.5)) * 2.0), 2.0) * flicker;

    return float4(glow.rgb * glow.w * strength, strength);
}

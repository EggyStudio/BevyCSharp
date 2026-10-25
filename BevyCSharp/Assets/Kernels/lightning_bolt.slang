// The bolts of the lightning striking about a camera (Render.SetThunder), a segment an instance,
// added to the picture after temporal antialiasing, which would smear a flash this short, behind
// whatever stands in front of them.
//
// Each segment is a strip facing the eye, the channel's core in its middle and a glow about it ten
// times as wide. The core is drawn a pixel and a half wide at least and dimmer by as much as the
// channel is narrower than that, so a far bolt is a bright thread rather than nothing. Where the bolt
// goes up into the clouds' base it fades, as the clouds hide its top.
import bcs_pass;

// Two for each segment. Where it starts (xyz) and how wide its channel is, in meters (w). Where it
// ends (xyz) and how bright it is against the main channel (w).
StructuredBuffer<float4> segments;

// rgb: the core's light, in candelas a square meter, before the exposure. w: the height of the
// clouds' base, where the bolt fades out.
uniform float4 bolt;

struct Corner
{
    float4 position : SV_Position;
    // Across the strip, from -1 to 1 in its glow's half widths.
    float across : TEXCOORD0;
    // The core's half width against the glow's.
    nointerpolation float core : TEXCOORD1;
    // The core's light, after the exposure.
    nointerpolation float3 light : TEXCOORD2;
};

static const float2 Corners[6] = {
    float2(-1.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
    float2(-1.0, 0.0), float2(1.0, 1.0), float2(-1.0, 1.0),
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
{
    float4 from = segments[instance_id * 2u];
    float4 to = segments[instance_id * 2u + 1u];
    float2 corner = Corners[vertex_id % 6u];
    float3 eye = passView.world_position;

    // The segment's middle sets how wide a pixel is, so the strip is one width along its length.
    float3 middle = (from.xyz + to.xyz) * 0.5;
    float distance = max(length(middle - eye), 1.0);
    float pixel = distance * 2.0 / (abs(passView.clip_from_view[1].y) * max(passView.viewport.w, 1.0));
    float coreHalf = max(from.w, pixel * 1.5) * 0.5;
    float glowHalf = max(coreHalf * 10.0, pixel * 4.0);

    float3 along = to.xyz - from.xyz;
    float3 side = cross(along, eye - middle);
    float sideSpan = sqrt(dot(side, side));
    side = sideSpan > 1e-4 ? side / sideSpan : passView.world_from_view[0].xyz;
    float3 at = lerp(from.xyz, to.xyz, corner.y) + side * corner.x * glowHalf;
    float fade = 1.0 - smoothstep(bolt.w - 250.0, bolt.w, at.y);

    Corner output;
    output.position = passTransform(passView.unjittered_clip_from_world, float4(at, 1.0));
    output.across = corner.x;
    output.core = coreHalf / glowHalf;
    output.light = bolt.rgb * to.w * fade * (from.w / (coreHalf * 2.0)) * passView.exposure;
    return output;
}

[shader("fragment")]
float4 fragment(Corner input) : SV_Target
{
    float x = abs(input.across);
    float core = 1.0 - smoothstep(input.core * 0.5, input.core, x);
    float glow = 0.03 * exp(-x * 5.0) * (1.0 - smoothstep(0.7, 1.0, x));
    return float4(input.light * (core + glow), 1.0);
}

// The drops and the flakes precipitation.slang moved and counted, one instance each, drawn over a
// camera's picture after temporal antialiasing, which would average them away, and placed by the
// projection without its jitter, as that picture is, behind whatever stands in front of them
// (Render.SetPrecipitation).
//
// Each is the streak it makes over a camera's shutter, as long as it moves in that time and as wide
// as it is, at most 25 centimeters, with rounded ends, a drop's thin and hard edged and a flake's
// soft. A flake is drawn over a shorter shutter than a drop, as the eye sees the two, a drifting disc
// and a falling line, so a flake in still air is a disc and one in a gale a short smear. Either is
// drawn a pixel and a half wide at least, fading as how much narrower it is than that to the power
// 0.6, so a far drop thins away rather than flickering between pixels, and keeps its opacity along
// its length, as rain seen through a camera does. Past two fifths of the far box's reach the drops
// fade out with distance, where rain is a haze the sky's fog holds rather than lines.
import bcs_pass;

// Three for each particle drawn, as precipitation.slang writes them.
StructuredBuffer<float4> drawn;

// x: how long the shutter is open for a drop, in seconds. y: for a flake. z: the far box's reach,
// in meters. w: unused.
uniform float4 look;

struct Corner
{
    float4 position : SV_Position;
    // Where in the streak, in widths from its middle, across (x) and along (y).
    float2 across : TEXCOORD0;
    // The light toward the eye and how much of what is behind it hides.
    float4 light : TEXCOORD1;
    // How far along from its middle each rounded end's center lies, in widths, and whether it is a
    // flake.
    nointerpolation float2 ends : TEXCOORD2;
};

static const float2 Corners[6] = {
    float2(-1.0, -1.0), float2(1.0, -1.0), float2(1.0, 1.0),
    float2(-1.0, -1.0), float2(1.0, 1.0), float2(-1.0, 1.0),
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
{
    float4 where = drawn[instance_id * 3u];
    float4 moving = drawn[instance_id * 3u + 1u];
    float4 light = drawn[instance_id * 3u + 2u];
    float2 corner = Corners[vertex_id % 6u];
    float3 eye = passView.world_position;
    float3 toEye = where.xyz - eye;
    float distance = max(length(toEye), 1e-3);
    toEye /= -distance;

    // How wide a pixel is at the particle's distance, from the projection's scale and the picture's
    // height.
    float pixel = distance * 2.0 / (abs(passView.clip_from_view[1].y) * max(passView.viewport.w, 1.0));
    float width = max(where.w, pixel * 1.5);
    float opacity = light.a * pow(where.w / width, 0.6) * (1.0 - smoothstep(0.4 * look.z, look.z, distance));

    // Along the particle's way across the picture, the streak's side square to it and to the eye, or
    // the picture's own axes where it moves straight toward or away from the eye.
    float3 along = moving.xyz * (moving.w > 0.5 ? look.y : look.x);
    float span = sqrt(dot(along, along));
    float3 way = span > 1e-5 ? along / span : passView.world_from_view[1].xyz;
    span = min(span, 0.25);
    float3 side = cross(way, toEye);
    float sideSpan = sqrt(dot(side, side));
    if (sideSpan > 1e-3)
    {
        side /= sideSpan;
    }
    else
    {
        side = passView.world_from_view[0].xyz;
        way = passView.world_from_view[1].xyz;
    }
    float reach = max(span, width) * 0.5;
    float3 at = where.xyz + way * corner.y * reach + side * corner.x * width * 0.5;

    Corner output;
    output.position = passTransform(passView.unjittered_clip_from_world, float4(at, 1.0));
    output.across = float2(corner.x, corner.y * reach / (width * 0.5));
    output.light = float4(light.rgb, opacity);
    output.ends = float2(max(reach / (width * 0.5) - 1.0, 0.0), moving.w);
    return output;
}

[shader("fragment")]
float4 fragment(Corner input) : SV_Target
{
    // How far from the streak's spine, the segment between its ends' centers, in half widths.
    float2 q = float2(input.across.x, max(abs(input.across.y) - input.ends.x, 0.0));
    float d = dot(q, q);
    float shape = input.ends.y > 0.5 ? pow(saturate(1.0 - d), 1.5) : saturate(1.0 - d * d);
    float alpha = input.light.a * shape;
    if (alpha < 1.0 / 1024.0)
        discard;
    return float4(input.light.rgb, saturate(alpha));
}

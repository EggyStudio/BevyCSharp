// The splashes the rain leaves where it lands close to a camera (precipitation.slang keeps them),
// one instance a slot, drawn over the picture after temporal antialiasing behind whatever stands in
// front of them (Render.SetPrecipitation).
//
// A splash stands on what the drop landed on, facing the eye, for a quarter of a second or so. A ring
// spreads at its foot and a crown of droplets is thrown up and out from it, rising and falling back
// as the splash ages, and all of it fades. A slot holding no splash, or one past its time, is drawn
// as nothing, its corners all at one point.
import bcs_pass;

// Two for each splash, as precipitation.slang writes them.
StructuredBuffer<float4> splashes;

// x: the clock, in seconds, wrapping each hour, as the splashes were written with. y: how long a
// splash lasts, in seconds. zw: unused.
uniform float4 clock;

struct Corner
{
    float4 position : SV_Position;
    // Across the splash from -1 to 1 (x), and up it from 0 at its foot to 1 at its top (y).
    float2 across : TEXCOORD0;
    // The light toward the eye and how far through its time the splash is.
    float4 light : TEXCOORD1;
};

static const float2 Corners[6] = {
    float2(-1.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
    float2(-1.0, 0.0), float2(1.0, 1.0), float2(-1.0, 1.0),
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
{
    float4 where = splashes[instance_id * 2u];
    float4 light = splashes[instance_id * 2u + 1u];
    float age = (clock.x - where.w) / max(clock.y, 1e-3);

    Corner output;
    output.across = 0.0;
    output.light = 0.0;
    if (light.a <= 0.0 || age < 0.0 || age >= 1.0)
    {
        output.position = float4(0.0, 0.0, -1.0, 1.0);
        return output;
    }

    // Standing up from where it landed, turned to face the eye about the upright, so it is seen from
    // the side as a splash is.
    float2 corner = Corners[vertex_id % 6u];
    float3 toEye = passView.world_position - where.xyz;
    float3 side = float3(-toEye.z, 0.0, toEye.x);
    float sideSpan = sqrt(dot(side, side));
    side = sideSpan > 1e-3 ? side / sideSpan : passView.world_from_view[0].xyz;
    float wide = light.a;
    float3 at = where.xyz + side * corner.x * wide * 0.5 + float3(0.0, corner.y * wide * 0.6, 0.0);

    output.position = passTransform(passView.unjittered_clip_from_world, float4(at, 1.0));
    output.across = corner;
    output.light = float4(light.rgb, age);
    return output;
}

[shader("fragment")]
float4 fragment(Corner input) : SV_Target
{
    float age = input.light.a;
    float2 q = input.across;

    // A ring spreading at the foot, seen edge on as a short bright line widening out.
    float ring = (1.0 - smoothstep(0.0, 0.06, q.y)) * (1.0 - smoothstep(age * 0.9, age * 0.9 + 0.15, abs(q.x)));

    // A crown of droplets thrown out along arcs, high in the middle of its time, falling back after.
    float drops = 0.0;
    for (int k = 0; k < 6; k++)
    {
        float spread = (float(k) / 5.0) * 2.0 - 1.0;
        float2 droplet = float2(spread * (0.25 + 0.6 * age), (1.0 - spread * spread * 0.5) * 4.0 * age * (1.0 - age) * 0.9);
        float2 apart = (q - droplet) * float2(1.0, 1.7);
        float size = 0.07 * (1.0 - 0.5 * age);
        drops += 1.0 - smoothstep(size * 0.5, size, length(apart));
    }

    float alpha = saturate(ring * 0.6 + drops) * (1.0 - age) * 0.8;
    if (alpha < 1.0 / 1024.0)
        discard;
    return float4(input.light.rgb, alpha);
}

// Bevy's custom_clustered_decal shader in Slang. Its standard material's silver has the decals over
// it laid on, and each decal laid on again tinted by its tag, red for 1 and blue for 2, before the
// surface is lit by Bevy.
import bcs;

uniform float4 base_color;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    var surface = bcs::surface(mesh);
    surface.base_color = base_color;

    // The decals laid on as the standard material lays them.
    surface = bcs::decals(surface, mesh);

    // And each laid on again, its color tinted by its tag.
    let count = bcs::decal_count(mesh);
    for (uint index = 0; index < count; index++)
    {
        var color = bcs::decal_sample(mesh, index, bcs::DecalMap.Color);

        switch (bcs::decal_tag(mesh, index))
        {
        case 1:
            color = float4(lerp(surface.base_color.rgb, float3(1.0, 0.0, 0.0), 0.5), color.a);
            break;
        case 2:
            color = float4(lerp(surface.base_color.rgb, float3(0.0, 0.0, 1.0), 0.5), color.a);
            break;
        default:
            break;
        }

        surface.base_color = float4(
            lerp(surface.base_color.rgb, color.rgb, color.a),
            surface.base_color.a + color.a);
    }

    return bcs::lit(surface, mesh);
}

// Bevy's custom_ui_material shader in Slang, which draws a progress bar, the image times the
// material's color as far across as the slider reaches, inside a border of the material's border
// color rounded at the node's corners.
import bcs_ui;

uniform float4 color;
/// How much of the image is shown, from zero to one, in the first of its four.
uniform float4 slider;
Texture2D material_color_texture;
SamplerState material_color_sampler;
uniform float4 border_color;

[shader("fragment")]
float4 fragment(UiVertexOutput node) : SV_Target
{
    let output_color = material_color_texture.Sample(material_color_sampler, node.uv) * color;

    // Half the node's size, and where the fragment is from its middle.
    let half_size = 0.5 * node.size;
    let p = node.uv * node.size - half_size;

    // The width of the border nearest the fragment on each axis.
    let b = float2(p.x > 0.0 ? node.border_widths.z : node.border_widths.x, p.y > 0.0 ? node.border_widths.w : node.border_widths.y);

    // How far the fragment is from the edge on each axis.
    let d = half_size - abs(p);

    // Within the border on either axis, the border's color, but outside the rounding of the
    // nearest corner nothing.
    if (d.x < b.x || d.y < b.y)
    {
        let rs = p.y > 0.0 ? node.border_radius_x.wz : node.border_radius_x.xy;
        let radius = p.x > 0.0 ? rs.y : rs.x;
        let q = radius - d;
        if (radius < min(max(q.x, q.y), 0.0) + length(max(q, float2(0.0))))
            return float4(0.0);
        return border_color;
    }

    // The image left of the slider, and nothing right of it.
    return node.uv.x < slider.x ? output_color : float4(0.0);
}

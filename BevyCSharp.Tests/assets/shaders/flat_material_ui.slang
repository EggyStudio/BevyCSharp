// An interface material of one color, with a border of another where the node has one, its node
// moved across by `offset` where its own interface vertex shader draws it.
import bcs_ui;

uniform float4 color;
uniform float4 border_color;
uniform float2 offset;

[shader("vertex")]
UiVertexOutput vertex(UiVertex v)
{
    v.position.xy += offset;
    return uiStandardVertex(v);
}

[shader("fragment")]
float4 fragment(UiVertexOutput node) : SV_Target
{
    let p = node.uv * node.size;
    let b = node.border_widths;
    let inside = p.x >= b.x && p.y >= b.y && p.x <= node.size.x - b.z && p.y <= node.size.y - b.w;
    return inside ? color : border_color;
}

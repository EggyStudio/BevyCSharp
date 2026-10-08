// The ramp each tonemapper is drawn through, as SHARED.md defines it for this engine and 3DEngine
// alike, on a picture 1024 pixels wide and 8 high.
//
// Column x holds the linear value 2 to the power of (20 x / 1023 - 12), from a four thousandth to
// 256, so the picture runs from black through every exposure a tonemapper bends to far past white.
// Each row holds it in another color: gray, red, green and blue alone, cyan, magenta and yellow,
// and the value with half of it and a quarter of it, a warm color whose hue a tonemapper may turn.
// The value is written as it is, with no light, exposure or grading on it, so what the camera's
// tonemapper is given is this and nothing else.

import bcs;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let column = floor(mesh.position.x);
    let row = uint(mesh.position.y);
    let v = exp2(20.0 * column / 1023.0 - 12.0);

    float3 rows[8] = {
        float3(v, v, v),
        float3(v, 0.0, 0.0),
        float3(0.0, v, 0.0),
        float3(0.0, 0.0, v),
        float3(0.0, v, v),
        float3(v, 0.0, v),
        float3(v, v, 0.0),
        float3(v, v * 0.5, v * 0.25),
    };

    return float4(rows[min(row, 7u)], 1.0);
}

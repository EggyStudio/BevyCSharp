// The camera's picture before tonemapping copied into an image of floats, its light in the units a
// reference of gi_reference.slang holds, which gi.compare reads back and measures against it. The
// picture's size is written beside it, which an image of one texel is bound to ask for before the
// images the size of the picture are made, since an offscreen camera has no window to ask.
import bcs_pass;

[format("rgba32f")] WTexture2D<float4> frame;
RWStructuredBuffer<uint> size;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint2 picture = passPictureSize();
    if (all(id.xy == uint2(0u)))
    {
        size[0] = picture.x;
        size[1] = picture.y;
    }

    uint width, height;
    frame.GetDimensions(width, height);
    if (id.x >= min(width, picture.x) || id.y >= min(height, picture.y))
        return;
    frame.Store(id.xy, float4(passPicture.Load(int3(id.xy, 0)).rgb, 1.0));
}

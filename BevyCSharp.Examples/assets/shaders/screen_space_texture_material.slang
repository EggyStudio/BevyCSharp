// Bevy's screen_space_texture_material shader in Slang: a black, polished metal surface that gives
// off light read from an image at the pixel it is drawn on, rather than at its UVs, so what a second
// camera drew shows through it as through a window.
import bcs;

Texture2D emissive_texture;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    var surface = bcs::surface(mesh);
    surface.base_color = float4(0.0, 0.0, 0.0, 1.0);
    surface.metallic = 1.0;
    surface.roughness = 0.0;

    // An alpha of zero keeps the view's exposure from scaling it, as Bevy's does.
    surface.emissive = float4(emissive_texture.Load(int3(int2(floor(mesh.position.xy)), 0)).rgb, 0.0);
    return bcs::lit(surface, mesh);
}

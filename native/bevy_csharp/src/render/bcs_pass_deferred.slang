// The deferred buffers as a pass reads them and a draw writes them, and the depth a draw's shadow
// stage returns, part of the `bcs_pass` module, which includes this file, and under its namespace,
// so a shader reaches them as `bcs_pass::surface_of` and the rest whichever file holds them.

implementing bcs_pass;

public namespace bcs_pass
{

/// What Bevy's G-buffer says about the surface at a pixel.
///
/// Only there on a camera drawing deferred (`Render.SetDeferredRendering`), where a shader reads it
/// by declaring `Texture2D<uint4> gbuffer;` and loading a texel, since the texture holds packed
/// bits rather than anything a sampler can blend. `surface_of` unpacks one the way Bevy's lighting
/// does, so a technique needing albedo and roughness per pixel need not draw the scene again to get
/// them.
public struct Surface
{
    /// The surface's color, linear. Black for an unlit material, whose color is its `emissive`.
    public float3 base_color;
    public float perceptual_roughness;
    public float metallic;
    /// Specular reflectance at normal incidence, Bevy's `reflectance`.
    public float reflectance;
    /// The material's own occlusion, from its occlusion texture, not screen-space occlusion.
    public float diffuse_occlusion;
    /// The surface's emissive light, times the camera's exposure where a lightmap added to it.
    public float3 emissive;
    /// The world-space normal.
    public float3 normal;
    /// Whether the material is unlit.
    public bool unlit;
    /// Whether the mesh receives shadows.
    public bool shadow_receiver;
    /// Whether the camera's fog covers it, as it covers a standard material by default.
    public bool fog;
};

float4 unpack_unorm4x8(uint v)
{
    return float4(float(v & 0xFF), float((v >> 8) & 0xFF), float((v >> 16) & 0xFF), float((v >> 24) & 0xFF)) / 255.0;
}

float3 unpack_rgb9e5(uint v)
{
    let exponent = int((v >> 27) & 0x1F) - 15 - 9;
    let scale = exp2(float(exponent));
    return float3(float(v & 0x1FF), float((v >> 9) & 0x1FF), float((v >> 18) & 0x1FF)) * scale;
}

float3 octahedral_decode(float2 encoded)
{
    let f = encoded * 2.0 - 1.0;
    var n = float3(f, 1.0 - abs(f.x) - abs(f.y));
    let t = saturate(-n.z);
    n.xy += select(n.xy >= 0.0, float2(-t), float2(t));
    return normalize(n);
}

/// Unpacks a texel of Bevy's G-buffer, laid out as Bevy's `deferred_gbuffer_from_pbr_input` writes
/// it outside WebGL: base color and roughness, emissive, material properties, normal and flags.
public Surface surface_of(uint4 texel)
{
    Surface surface;

    let flags = (texel.w >> 24) & 0xFF;
    surface.unlit = (flags & 1) != 0;
    surface.fog = (flags & 2) != 0;
    surface.shadow_receiver = (flags & 4) != 0;

    let base_rough = unpack_unorm4x8(texel.x);
    surface.perceptual_roughness = base_rough.a;

    surface.emissive = unpack_rgb9e5(texel.y);

    // Stored as a gamma of 2.2 for precision in eight bits, which Bevy undoes. An unlit material
    // keeps its color in the emissive slot instead and leaves this black.
    surface.base_color = surface.unlit ? float3(0.0) : pow(base_rough.rgb, float3(2.2));

    let props = unpack_unorm4x8(texel.z);
    surface.reflectance = props.r;
    surface.metallic = props.g;
    surface.diffuse_occlusion = props.b;

    let normal = float2(float(texel.w & 0xFFF), float((texel.w >> 12) & 0xFFF)) / 4095.0;
    surface.normal = octahedral_decode(normal);

    return surface;
}

// -- Drawing into the deferred buffers
//
// A draw at `FramePoint.InPrepass` with `Targets = ["gbuffer", "lighting_pass"]` writes a surface
// of its own into a camera's deferred buffers, which Bevy's deferred lighting then lights as it
// lights a standard material, shadows, ambient occlusion and screen-space reflections with it. A
// surface found by marching a ray from each pixel is drawn this way, as Bevy's deferred_raymarch
// example draws one.

/// The deferred lighting pass Bevy lights a standard material with, which `lighting_pass` holds
/// for each pixel it lights.
public static const uint STANDARD_LIGHTING = 1u;

/// A surface as Bevy's default standard material describes one facing `normal`, white, rough by
/// half, not metal, reflecting half the light head on, receiving shadows and covered by fog.
public Surface surface(float3 normal)
{
    Surface output;
    output.base_color = float3(1.0);
    output.perceptual_roughness = 0.5;
    output.metallic = 0.0;
    output.reflectance = 0.5;
    output.diffuse_occlusion = 1.0;
    output.emissive = float3(0.0);
    output.normal = normal;
    output.unlit = false;
    output.shadow_receiver = true;
    output.fog = true;
    return output;
}

uint pack_unorm4x8(float4 values)
{
    let v = uint4(saturate(values) * 255.0 + 0.5);
    return (v.w << 24) | (v.z << 16) | (v.y << 8) | v.x;
}

/// Three numbers sharing one exponent, as Bevy's `vec3_to_rgb9e5_` packs them, which keeps what
/// the decoder above reads exact.
uint pack_rgb9e5(float3 color)
{
    let rgb = clamp(color, float3(0.0), float3(65408.0));
    let largest = max(rgb.r, max(rgb.g, rgb.b));
    let floor_log2 = int((asuint(largest) & 0x7F800000u) >> 23) - 127;

    var exponent = max(-16, floor_log2) + 1 + 15;
    var scale = exp2(float(exponent - 15 - 9));

    if (int(floor(largest / scale + 0.5)) == 512)
    {
        scale *= 2.0;
        exponent += 1;
    }

    let n = uint3(floor(rgb / scale + 0.5));
    return (uint(exponent) << 27) | (n.b << 18) | (n.g << 9) | n.r;
}

float2 octahedral_encode(float3 v)
{
    let n = v / (abs(v.x) + abs(v.y) + abs(v.z));
    let wrapped = (1.0 - abs(n.yx)) * select(n.xy >= 0.0, float2(1.0), float2(-1.0));
    let folded = n.z >= 0.0 ? n.xy : wrapped;
    return folded * 0.5 + 0.5;
}

/// Packs a surface into a texel of Bevy's G-buffer, as Bevy's `deferred_gbuffer_from_pbr_input`
/// packs a standard material's outside WebGL, which `surface_of` unpacks again.
///
/// An unlit surface's color is its `emissive`, as `surface_of` gives it back, and its `base_color`
/// is not kept. A standard material's clearcoat and a lightmap's light are not written, so a
/// surface drawn this way has neither.
///
/// Written here rather than reached through Bevy's own function as a material's deferred stage
/// reaches it, since Bevy's reads the view's uniform in group zero for the lightmap's light, and
/// group zero is where a draw on a camera keeps its own values.
public uint4 gbuffer_of(Surface surface)
{
    var flags = 0u;
    if (surface.unlit)
        flags |= 1u;
    if (surface.fog)
        flags |= 2u;
    if (surface.shadow_receiver)
        flags |= 4u;

    // Kept as a gamma of 2.2 for precision in eight bits, which `surface_of` and Bevy's lighting
    // undo.
    let base_color = surface.unlit ? float3(0.0) : pow(max(surface.base_color, float3(0.0)), float3(1.0 / 2.2));
    let normal = octahedral_encode(normalize(surface.normal));
    let x = uint(saturate(normal.x) * 4095.0 + 0.5);
    let y = uint(saturate(normal.y) * 4095.0 + 0.5);

    return uint4(
        pack_unorm4x8(float4(base_color, surface.perceptual_roughness)),
        pack_rgb9e5(surface.emissive),
        pack_unorm4x8(float4(surface.reflectance, surface.metallic, surface.diffuse_occlusion, 0.0)),
        (x & 0xFFF) | ((y & 0xFFF) << 12) | ((flags & 0xFF) << 24));
}

/// What a draw writes into the deferred buffers, the packed surface, the lighting pass that
/// lights it, and its depth, which tests it against Bevy's geometry and hides it behind what is
/// nearer.
public struct Deferred
{
    public uint4 gbuffer : SV_Target0;
    public uint lighting_pass : SV_Target1;
    public float depth : SV_Depth;
};

/// A surface at a point in the world as the deferred buffers hold it, lit by Bevy's lighting for
/// a standard material.
public Deferred deferred(Surface surface, float3 world)
{
    Deferred output;
    output.gbuffer = gbuffer_of(surface);
    output.lighting_pass = STANDARD_LIGHTING;
    output.depth = depth_of(world);
    return output;
}

/// What a draw's shadow stage returns, the depth of the surface it found as the light's view keeps
/// it.
///
/// A struct, because slangc writes a depth given as a function's own return (`float shadow(...)
/// : SV_Depth`) as a color when it writes WGSL, which the shadow map has no target for, so the
/// triangle's own depth would be kept instead. A member marked `SV_Depth` is written as the depth.
public struct ShadowDepth
{
    public float depth : SV_Depth;
};

/// The depth of a point in the world, for a shadow stage to return.
public ShadowDepth shadow_depth(float3 world)
{
    ShadowDepth output;
    output.depth = depth_of(world);
    return output;
}

}

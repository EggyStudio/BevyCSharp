using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a camera should see, handed to the bridge when one is spawned.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeCameraConfig
{
    /// <summary>0 perspective, 1 orthographic.</summary>
    public int Projection;

    /// <summary>Vertical field of view in degrees. Perspective only.</summary>
    public float FovDegrees;

    /// <summary>World units visible vertically. Orthographic only.</summary>
    public float OrthoHeight;

    /// <summary>Nearest visible distance.</summary>
    public float Near;

    /// <summary>Furthest visible distance.</summary>
    public float Far;

    /// <summary>0 world clear colour, 1 the one below, 2 no clear.</summary>
    public int ClearMode;

    /// <summary>Clear colour red.</summary>
    public float ClearR;

    /// <summary>Clear colour green.</summary>
    public float ClearG;

    /// <summary>Clear colour blue.</summary>
    public float ClearB;

    /// <summary>Clear colour alpha.</summary>
    public float ClearA;

    /// <summary>Draw order; higher draws over lower.</summary>
    public int Order;

    /// <summary>Non-zero to draw into part of the window.</summary>
    public int HasViewport;

    /// <summary>Viewport left, in physical pixels.</summary>
    public uint ViewportX;

    /// <summary>Viewport top, in physical pixels.</summary>
    public uint ViewportY;

    /// <summary>Viewport width, in physical pixels.</summary>
    public uint ViewportWidth;

    /// <summary>Viewport height, in physical pixels.</summary>
    public uint ViewportHeight;

    /// <summary>A bit per render layer; 0 for the default layer.</summary>
    public uint Layers;
}

/// <summary>What kind of light to spawn and how it behaves.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeLightConfig
{
    /// <summary>0 directional, 1 point, 2 spot.</summary>
    public int Kind;

    /// <summary>Lux for a directional light, lumens for the other two.</summary>
    public float Intensity;

    /// <summary>Linear red.</summary>
    public float ColorR;

    /// <summary>Linear green.</summary>
    public float ColorG;

    /// <summary>Linear blue.</summary>
    public float ColorB;

    /// <summary>How far the light reaches. Point and spot only.</summary>
    public float Range;

    /// <summary>Radius of the emitting sphere. Point and spot only.</summary>
    public float Radius;

    /// <summary>Non-zero to cast shadows.</summary>
    public int Shadows;

    /// <summary>Radians of full brightness about the axis. Spot only.</summary>
    public float InnerAngle;

    /// <summary>Radians at which a spot light has fallen to nothing.</summary>
    public float OuterAngle;

    /// <summary>Depth bias applied before the shadow test.</summary>
    public float ShadowDepthBias;

    /// <summary>Bias along the surface normal.</summary>
    public float ShadowNormalBias;
}

/// <summary>Everything a physically based material is made of.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMaterialConfig
{
    /// <summary>Base colour red, linear.</summary>
    public float BaseR;

    /// <summary>Base colour green, linear.</summary>
    public float BaseG;

    /// <summary>Base colour blue, linear.</summary>
    public float BaseB;

    /// <summary>Opacity.</summary>
    public float BaseA;

    /// <summary>Zero for a dielectric, one for a metal.</summary>
    public float Metallic;

    /// <summary>Near zero for a mirror, one for matte.</summary>
    public float Roughness;

    /// <summary>Emissive red, linear.</summary>
    public float EmissiveR;

    /// <summary>Emissive green, linear.</summary>
    public float EmissiveG;

    /// <summary>Emissive blue, linear.</summary>
    public float EmissiveB;

    /// <summary>Emissive alpha, unused by the renderer but part of the colour.</summary>
    public float EmissiveA;

    /// <summary>0 opaque, 1 masked, 2 blended, 3 additive.</summary>
    public int AlphaMode;

    /// <summary>Cut-off for a masked material.</summary>
    public float AlphaCutoff;

    /// <summary>Non-zero to draw back faces too.</summary>
    public int DoubleSided;

    /// <summary>Non-zero to skip lighting.</summary>
    public int Unlit;

    /// <summary>Asset key of the base colour map, or -1.</summary>
    public int BaseColorTexture;

    /// <summary>Asset key of the normal map, or -1.</summary>
    public int NormalMap;

    /// <summary>Asset key of the metallic-roughness map, or -1.</summary>
    public int MetallicRoughnessTexture;

    /// <summary>Asset key of the emissive map, or -1.</summary>
    public int EmissiveTexture;

    /// <summary>Asset key of the ambient occlusion map, or -1.</summary>
    public int OcclusionTexture;

    /// <summary>Texture repeats across the surface, in U.</summary>
    public float UvScaleX;

    /// <summary>Texture repeats across the surface, in V.</summary>
    public float UvScaleY;

    /// <summary>Radians the texture is turned by.</summary>
    public float UvRotation;

    /// <summary>Texture shift, in U.</summary>
    public float UvOffsetX;

    /// <summary>Texture shift, in V.</summary>
    public float UvOffsetY;
}

/// <summary>How an image should be sampled, and how its bytes should be read.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeImageConfig
{
    /// <summary>0 clamp, 1 repeat, 2 mirror, for U.</summary>
    public int AddressU;

    /// <summary>0 clamp, 1 repeat, 2 mirror, for V.</summary>
    public int AddressV;

    /// <summary>0 nearest, 1 linear, when drawn larger than the texture.</summary>
    public int MagFilter;

    /// <summary>0 nearest, 1 linear, when drawn smaller.</summary>
    public int MinFilter;

    /// <summary>0 nearest, 1 linear, between mip levels.</summary>
    public int MipmapFilter;

    /// <summary>Maximum anisotropic samples; 1 disables it.</summary>
    public uint Anisotropy;

    /// <summary>Non-zero to read the file as sRGB.</summary>
    public int Srgb;
}

/// <summary>How a sprite is drawn.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeSpriteConfig
{
    /// <summary>Asset key of the image.</summary>
    public int Image;

    /// <summary>Tint red.</summary>
    public float ColorR;

    /// <summary>Tint green.</summary>
    public float ColorG;

    /// <summary>Tint blue.</summary>
    public float ColorB;

    /// <summary>Tint alpha.</summary>
    public float ColorA;

    /// <summary>Non-zero to use <see cref="SizeX"/> and <see cref="SizeY"/>.</summary>
    public int HasSize;

    /// <summary>Width in world units.</summary>
    public float SizeX;

    /// <summary>Height in world units.</summary>
    public float SizeY;

    /// <summary>Non-zero to draw only part of the image.</summary>
    public int HasRect;

    /// <summary>Left of that part, in pixels.</summary>
    public float RectLeft;

    /// <summary>Top of that part, in pixels.</summary>
    public float RectTop;

    /// <summary>Right of that part, in pixels.</summary>
    public float RectRight;

    /// <summary>Bottom of that part, in pixels.</summary>
    public float RectBottom;

    /// <summary>Non-zero to mirror horizontally.</summary>
    public int FlipX;

    /// <summary>Non-zero to mirror vertically.</summary>
    public int FlipY;
}

/// <summary>One debug shape to draw this frame.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeGizmoConfig
{
    /// <summary>0 line, 1 sphere, 2 axes.</summary>
    public int Kind;

    /// <summary>Start, centre or position, X.</summary>
    public float StartX;

    /// <summary>Start, centre or position, Y.</summary>
    public float StartY;

    /// <summary>Start, centre or position, Z.</summary>
    public float StartZ;

    /// <summary>Line end, X.</summary>
    public float EndX;

    /// <summary>Line end, Y.</summary>
    public float EndY;

    /// <summary>Line end, Z.</summary>
    public float EndZ;

    /// <summary>Orientation, X.</summary>
    public float RotationX;

    /// <summary>Orientation, Y.</summary>
    public float RotationY;

    /// <summary>Orientation, Z.</summary>
    public float RotationZ;

    /// <summary>Orientation, W.</summary>
    public float RotationW;

    /// <summary>Sphere radius or axis length.</summary>
    public float Radius;

    /// <summary>Colour red.</summary>
    public float ColorR;

    /// <summary>Colour green.</summary>
    public float ColorG;

    /// <summary>Colour blue.</summary>
    public float ColorB;

    /// <summary>Colour alpha.</summary>
    public float ColorA;
}

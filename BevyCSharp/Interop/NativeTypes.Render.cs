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

    /// <summary>0 world clear color, 1 the one below, 2 no clear.</summary>
    public int ClearMode;

    /// <summary>Clear color red.</summary>
    public float ClearR;

    /// <summary>Clear color green.</summary>
    public float ClearG;

    /// <summary>Clear color blue.</summary>
    public float ClearB;

    /// <summary>Clear color alpha.</summary>
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

/// <summary>What a mesh holds, without its vertices, as the bridge reports it.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMeshInfo
{
    /// <summary>How many vertices.</summary>
    public uint Vertices;

    /// <summary>How many indices, or zero for none.</summary>
    public uint Indices;

    /// <summary>16 or 32, or zero for no indices.</summary>
    public uint IndexBits;

    /// <summary>0 triangles, 1 a strip, 2 lines, 3 a line strip, 4 points.</summary>
    public uint Topology;

    /// <summary>One bit per attribute, as <see cref="MeshAttributes"/> numbers them.</summary>
    public uint Attributes;

    /// <summary>The bounds' smallest corner.</summary>
    public float MinX, MinY, MinZ;

    /// <summary>The bounds' largest corner.</summary>
    public float MaxX, MaxY, MaxZ;
}

/// <summary>Everything Bevy's <c>ColorMaterial</c> for a 2D mesh is made of.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeColorMaterial
{
    /// <summary>The color, linear RGBA.</summary>
    public fixed float Color[4];

    /// <summary>An image's key, or zero for none.</summary>
    public int Texture;

    /// <summary>Opaque at zero, masked at one, blended at two.</summary>
    public int AlphaMode;

    /// <summary>The alpha below which a masked pixel is not drawn.</summary>
    public float AlphaCutoff;

    /// <summary>The texture coordinates' transform, its two matrix columns and its translation.</summary>
    public fixed float Uv[6];
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

    /// <summary>How many layers a loaded file is cut into, top to bottom; 0 or 1 for one picture.</summary>
    public uint Layers;
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

    /// <summary>Asset key of the atlas layout, or negative for the whole image.</summary>
    public int Atlas;

    /// <summary>Which frame of that layout to draw.</summary>
    public uint AtlasIndex;

    /// <summary>Non-zero to move the sprite's origin off center.</summary>
    public int HasAnchor;

    /// <summary>Where the transform sits on the sprite, horizontally.</summary>
    public float AnchorX;

    /// <summary>Where the transform sits on the sprite, vertically.</summary>
    public float AnchorY;

    /// <summary>0 the image's own size, 1 sliced, 2 tiled, 3 scaled to fit.</summary>
    public int Mode;

    /// <summary>How a scaled picture is fitted, when the mode is scaled.</summary>
    public int Scaling;

    /// <summary>Left inset of the nine-slice border, in pixels.</summary>
    public float SliceLeft;

    /// <summary>Top inset of the nine-slice border, in pixels.</summary>
    public float SliceTop;

    /// <summary>Right inset of the nine-slice border, in pixels.</summary>
    public float SliceRight;

    /// <summary>Bottom inset of the nine-slice border, in pixels.</summary>
    public float SliceBottom;

    /// <summary>How far a sliced corner may be scaled up; 0 for Bevy's default.</summary>
    public float CornerScale;

    /// <summary>Non-zero to repeat horizontally when tiled.</summary>
    public int TileX;

    /// <summary>Non-zero to repeat vertically when tiled.</summary>
    public int TileY;

    /// <summary>How far the picture stretches before a tile repeats; 0 for Bevy's default.</summary>
    public float TileStretch;

    /// <summary>Which parts of a sliced picture tile: 1 the sides, 2 the middle, 3 both.</summary>
    public int SliceTiling;
}

/// <summary>How a text field behaves. Mirrors <c>BcsEditableTextConfig</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeEditableTextConfig
{
    /// <summary>The most characters it holds, or zero for no limit.</summary>
    public int MaxCharacters;

    /// <summary>How many glyphs wide, or zero for its node's width.</summary>
    public float VisibleWidth;

    /// <summary>How many lines tall, or zero for one.</summary>
    public float VisibleLines;

    /// <summary>Non-zero to let Enter start a new line.</summary>
    public int AllowNewlines;

    /// <summary>A <see cref="TextReadWriteMode"/>.</summary>
    public int Mode;
}

/// <summary>Where and how a run of gizmo text is drawn. Mirrors <c>BcsGizmoText</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeGizmoText
{
    /// <summary>Where its anchor sits.</summary>
    public fixed float Position[3];

    /// <summary>Which way it faces, as a quaternion.</summary>
    public fixed float Rotation[4];

    /// <summary>The height of a capital letter.</summary>
    public float Size;

    /// <summary>The point of its bounds at the position, from minus a half to a half.</summary>
    public fixed float Anchor[2];

    /// <summary>Linear RGBA.</summary>
    public fixed float Color[4];

    /// <summary>Non-zero to draw over everything.</summary>
    public int InFront;
}

/// <summary>One debug shape to draw this frame.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeGizmoConfig
{
    /// <summary>0 line, 1 sphere, 2 axes.</summary>
    public int Kind;

    /// <summary>Start, center or position, X.</summary>
    public float StartX;

    /// <summary>Start, center or position, Y.</summary>
    public float StartY;

    /// <summary>Start, center or position, Z.</summary>
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

    /// <summary>Color red.</summary>
    public float ColorR;

    /// <summary>Color green.</summary>
    public float ColorG;

    /// <summary>Color blue.</summary>
    public float ColorB;

    /// <summary>Color alpha.</summary>
    public float ColorA;

    /// <summary>What a fading line reaches at its far end, red.</summary>
    public float EndColorR;

    /// <summary>Far end green.</summary>
    public float EndColorG;

    /// <summary>Far end blue.</summary>
    public float EndColorB;

    /// <summary>Far end alpha.</summary>
    public float EndColorA;

    /// <summary>Whether the scene can hide it: 0 is depth tested, anything else draws in front.</summary>
    public int InFront;
}

/// <summary>What a camera does to the picture after the scene has been drawn.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativePostConfig
{
    /// <summary>0 none, 1 Reinhard, 2 Reinhard luminance, 3 ACES, 4 AgX, 5 boring, 6 Tony, 7 filmic.</summary>
    public int Tonemapping;

    /// <summary>Non-zero to dither before quantizing.</summary>
    public int Dither;

    /// <summary>Non-zero to draw into a high dynamic range target.</summary>
    public int Hdr;

    /// <summary>Samples per pixel: 1 off, or 2, 4, 8.</summary>
    public int Msaa;

    /// <summary>0 none, 1 FXAA, 2 SMAA.</summary>
    public int AntiAlias;

    /// <summary>0 low, 1 medium, 2 high, 3 ultra.</summary>
    public int AntiAliasQuality;

    /// <summary>Sharpening strength, 0 for none.</summary>
    public float Sharpen;

    /// <summary>Non-zero to scatter light from the brightest parts.</summary>
    public int Bloom;

    /// <summary>How much is scattered.</summary>
    public float BloomIntensity;

    /// <summary>Brightness a pixel reaches before it blooms.</summary>
    public float BloomThreshold;

    /// <summary>How gradually that threshold takes effect.</summary>
    public float BloomThresholdSoftness;

    /// <summary>0 energy conserving, 1 additive.</summary>
    public int BloomMode;
}

/// <summary>A sky computed from light scattering through the air.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeAtmosphereConfig
{
    /// <summary>Non-zero to draw the sky from this camera.</summary>
    public int Enabled;

    /// <summary>Density multiplier for the air; 0 for earth's own.</summary>
    public float Density;

    /// <summary>Planet scale against the scene; 0 for one.</summary>
    public float Scale;

    /// <summary>How far the haze in front of the scene is computed, in meters; 0 for Bevy's.</summary>
    public float HazeDistance;

    /// <summary>How finely the sky is computed: 0 Bevy's own, 1 cheaper, 2 finer.</summary>
    public int Quality;

    /// <summary>How much light the ground bounces back, or 0 for the medium's own.</summary>
    public float GroundAlbedo;
}

/// <summary>One tonal range's part of a color grade. Mirrors <c>BcsGradingSection</c>.</summary>
public struct NativeGradingSection
{
    /// <summary>Below one drains color toward gray, above one spreads it out.</summary>
    public float Saturation;

    /// <summary>Below one pulls toward neutral gray, above one pushes away.</summary>
    public float Contrast;

    /// <summary>The exponent, which mostly moves the top of the range.</summary>
    public float Gamma;

    /// <summary>The multiplier, which mostly moves the middle of the range.</summary>
    public float Gain;

    /// <summary>The offset, which mostly moves the bottom of the range.</summary>
    public float Lift;
}

/// <summary>How a camera grades the picture. Mirrors <c>BcsGradingConfig</c>.</summary>
public struct NativeGradingConfig
{
    /// <summary>Stops of exposure applied before anything else.</summary>
    public float Exposure;

    /// <summary>White balance, toward blue below zero and toward orange above it.</summary>
    public float Temperature;

    /// <summary>White balance the other way, toward green and toward magenta.</summary>
    public float Tint;

    /// <summary>Hue rotation in degrees.</summary>
    public float Hue;

    /// <summary>Saturation applied to everything, after the three sections.</summary>
    public float PostSaturation;

    /// <summary>Where the midtones begin, as a luminance.</summary>
    public float MidtonesFrom;

    /// <summary>Where they end.</summary>
    public float MidtonesTo;

    /// <summary>The darkest range.</summary>
    public NativeGradingSection Shadows;

    /// <summary>The middle range.</summary>
    public NativeGradingSection Midtones;

    /// <summary>The brightest range.</summary>
    public NativeGradingSection Highlights;
}

/// <summary>A mesh described vertex by vertex. Mirrors <c>BcsMeshData</c>.</summary>
public unsafe struct NativeMeshData
{
    /// <summary>Three floats a vertex.</summary>
    public float* Positions;

    /// <summary>How many vertices there are.</summary>
    public int VertexCount;

    /// <summary>Three floats a vertex, or null.</summary>
    public float* Normals;

    /// <summary>Two floats a vertex, or null.</summary>
    public float* Uvs;

    /// <summary>Four floats a vertex, or null.</summary>
    public float* Colors;

    /// <summary>Indices into the vertices, or null.</summary>
    public uint* Indices;

    /// <summary>How many indices there are.</summary>
    public int IndexCount;

    /// <summary>0 triangles, 1 lines, 2 points, 3 a line strip, 4 a triangle strip.</summary>
    public int Topology;
}

/// <summary>What <see cref="Animation.StateOf"/> reads, in the bridge's layout.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeAnimationState
{
    /// <summary>The clip's number, or -1.</summary>
    public int Clip;

    /// <summary>Seconds into it.</summary>
    public float Seconds;

    /// <summary>How fast it plays.</summary>
    public float Speed;

    /// <summary>Non-zero while held.</summary>
    public int Paused;

    /// <summary>Non-zero once a clip that plays once has ended.</summary>
    public int Finished;

    /// <summary>How many times it has come round.</summary>
    public uint Completions;
}

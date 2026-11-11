using Bevy.Interop;

namespace Bevy;

/// <summary>
/// A 2D mesh's material, Bevy's <c>ColorMaterial</c>, a color, an image the color multiplies, how
/// alpha is read, and where on the mesh the image falls.
/// </summary>
/// <remarks>
/// <para>
/// What Bevy's 2D meshes are drawn with by default. A mesh with a material of one color and no
/// image draws as that color. With an image, the color tints it, white leaving it as it is.
/// </para>
/// <para>
/// The image's coordinates are moved by <see cref="UvScale"/>, then <see cref="UvRotation"/>, then
/// <see cref="UvOffset"/>, so a scale of four repeats an image four times across a mesh, provided
/// the image samples repeating rather than clamped.
/// </para>
/// </remarks>
public sealed class ColorMaterialSettings
{
    /// <summary>The color, linear RGBA, which multiplies the image where there is one.</summary>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>The image, or <see cref="AssetHandle.None"/> for the color alone.</summary>
    public AssetHandle Texture { get; set; } = AssetHandle.None;

    /// <summary>How alpha is read. Opaque by default, as Bevy's is for an opaque color.</summary>
    public AlphaMode2d AlphaMode { get; set; } = AlphaMode2d.Opaque;

    /// <summary>The alpha a masked pixel needs to be drawn.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>How many times the image fits across the mesh on each axis.</summary>
    public Vec2 UvScale { get; set; } = new(1f, 1f);

    /// <summary>How far the image's coordinates are turned, in radians.</summary>
    public float UvRotation { get; set; }

    /// <summary>How far the image's coordinates are moved, in image widths and heights.</summary>
    public Vec2 UvOffset { get; set; } = Vec2.Zero;

    /// <summary>The settings as the bridge reads them.</summary>
    internal unsafe NativeColorMaterial Native()
    {
        var native = new NativeColorMaterial
        {
            Texture = Texture.Key,
            AlphaMode = (int)AlphaMode,
            AlphaCutoff = AlphaCutoff,
        };
        (native.Color[0], native.Color[1], native.Color[2], native.Color[3]) = Color;

        // An affine transform's two matrix columns, the rotation times the scale, and its
        // translation, as glam's Affine2 lays them out.
        var (cos, sin) = (MathF.Cos(UvRotation), MathF.Sin(UvRotation));
        (native.Uv[0], native.Uv[1]) = (cos * UvScale.X, sin * UvScale.X);
        (native.Uv[2], native.Uv[3]) = (-sin * UvScale.Y, cos * UvScale.Y);
        (native.Uv[4], native.Uv[5]) = (UvOffset.X, UvOffset.Y);
        return native;
    }
}

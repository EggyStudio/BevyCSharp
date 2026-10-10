using Bevy.Interop;

namespace Bevy;

/// <summary>How a 2D mesh's material reads its alpha.</summary>
public enum AlphaMode2d
{
    /// <summary>Ignores alpha, drawing every pixel solid.</summary>
    Opaque = 0,

    /// <summary>Draws a pixel or leaves it out at <see cref="ColorMaterialSettings.AlphaCutoff"/>.</summary>
    Mask = 1,

    /// <summary>Blends with what is behind, drawn after the solid meshes and sorted back to front.</summary>
    Blend = 2,
}

/// <summary>
/// A 2D mesh's material, Bevy's <c>ColorMaterial</c>: a color, an image the color multiplies, how
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

public static unsafe partial class Render2d
{
    /// <summary>Makes a 2D mesh's material and returns it.</summary>
    /// <remarks>
    /// Shared like any asset, so many meshes can draw with one material and a change to it, through
    /// <see cref="WriteMaterial"/>, shows on them all.
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateMaterial(ColorMaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = settings.Native();
        var key = Interop.Native.bcs_render_color_material_create(&native);
        if (key == NativeStatus.Unsupported) throw Render.NoRenderer("Building a 2D material");
        Interop.Native.Check(key, "building a 2D material");
        return new AssetHandle(key);
    }

    /// <summary>Writes settings over a 2D mesh's material in place, so every mesh drawn with it changes.</summary>
    /// <exception cref="BevyNativeException">The handle is not a 2D material, or this build has no renderer.</exception>
    public static void WriteMaterial(AssetHandle material, ColorMaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = settings.Native();
        var status = Interop.Native.bcs_render_color_material_write(material.Key, &native);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Writing a 2D material");
        Interop.Native.Check(status, $"writing the 2D material {material}");
    }

    /// <summary>Gives an entity a mesh for a 2D camera to draw.</summary>
    /// <remarks>
    /// Any mesh <see cref="Render.CreateMesh(string, float, float, float)"/> makes, a flat one such as
    /// <see cref="MeshShape.Circle"/> or <see cref="MeshShape.RegularPolygon"/> lying in the plane
    /// a 2D camera looks at. It needs a material from <see cref="CreateMaterial"/> to be drawn.
    /// </remarks>
    public static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh) =>
        Render.Attach(world, entity, "Mesh2d", mesh, "a mesh");

    /// <summary>
    /// Gives an entity a 2D material to draw its mesh with, a color material from
    /// <see cref="CreateMaterial"/> or a shader material from <see cref="Shaders.CreateMaterial2d"/>.
    /// </summary>
    /// <remarks>
    /// A shader material given to an entity that is already a sprite, from
    /// <see cref="SetSprite(EcsWorld, Entity, AssetHandle)"/>, draws the sprite, as Bevy's
    /// <c>SpriteMaterial</c> does, so the sprite comes first. Any other material is for an entity
    /// with a 2D mesh.
    /// </remarks>
    public static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material) =>
        Render.Attach(world, entity, "MeshMaterial2d", material, "a 2D material");
}

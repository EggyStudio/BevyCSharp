using Bevy.Interop;

namespace Bevy;

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

using Bevy.Interop;

namespace Bevy;

/// <summary>The mesh primitives the engine can build without an asset file.</summary>
public static class MeshShape
{
    /// <summary>A box, sized by width, height and depth.</summary>
    public const string Cuboid = "Cuboid";

    /// <summary>A sphere, sized by radius.</summary>
    public const string Sphere = "Sphere";

    /// <summary>A flat plane on the XZ axes, sized by width and depth.</summary>
    public const string Plane = "Plane";

    /// <summary>A capsule, sized by radius and length.</summary>
    public const string Capsule = "Capsule";
}

/// <summary>Which of Bevy's lights to spawn.</summary>
public enum LightKind
{
    /// <summary>Parallel rays, as from the sun. Intensity is illuminance in lux.</summary>
    Directional = 0,

    /// <summary>Rays from a point. Intensity is luminous power in lumens.</summary>
    Point = 1,
}

/// <summary>
/// Builds renderable assets and attaches them to entities.
/// </summary>
/// <remarks>
/// <para>
/// Everything here needs a native build with the renderer compiled in. On a headless build the
/// calls report that rather than failing obscurely, so the same behavior code runs either way and
/// simply draws nothing.
/// </para>
/// <para>
/// Meshes and materials are Rust values that have to be constructed rather than described by a
/// layout, and the components carrying them hold a typed handle that raw bytes cannot represent.
/// That is why these are named operations rather than a component written through
/// <see cref="EcsWorld.Add{T}"/>, the way <see cref="Transform"/> is.
/// </para>
/// </remarks>
public static class Render
{
    /// <summary>
    /// Builds a mesh primitive and returns a handle to it.
    /// </summary>
    /// <param name="shape">One of the constants on <see cref="MeshShape"/>.</param>
    /// <param name="a">Width for a cuboid or plane, radius for a sphere or capsule.</param>
    /// <param name="b">Height for a cuboid, depth for a plane, length for a capsule.</param>
    /// <param name="c">Depth, for a cuboid.</param>
    public static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(shape);

        var key = Native.bcs_mesh_create(shape, a, b, c);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a mesh");
        if (key == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"'{shape}' is not a mesh primitive the engine can build. Use one of the "
                + "constants on MeshShape.");

        Native.Check(key, $"building a {shape} mesh");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Builds a physically based material and returns a handle to it.
    /// </summary>
    /// <param name="red">Linear sRGB red, from zero to one.</param>
    /// <param name="green">Linear sRGB green, from zero to one.</param>
    /// <param name="blue">Linear sRGB blue, from zero to one.</param>
    /// <param name="alpha">Opacity, from zero to one.</param>
    /// <param name="metallic">Zero for a dielectric, one for a metal.</param>
    /// <param name="roughness">Near zero for a mirror, one for a matte surface.</param>
    public static AssetHandle CreateMaterial(
        float red,
        float green,
        float blue,
        float alpha = 1f,
        float metallic = 0f,
        float roughness = 0.5f)
    {
        var key = Native.bcs_material_create(red, green, blue, alpha, metallic, roughness);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a material");

        Native.Check(key, "building a material");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Gives an entity a mesh to draw.
    /// </summary>
    /// <remarks>
    /// Inserting this also pulls in the components Bevy requires alongside it, such as
    /// <see cref="Transform"/> and visibility, so an entity needs nothing else to be drawable
    /// beyond a material.
    /// </remarks>
    public static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh) =>
        Attach(world, entity, "Mesh3d", mesh, "a mesh");

    /// <summary>Gives an entity a material to draw its mesh with.</summary>
    public static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material) =>
        Attach(world, entity, "MeshMaterial3d", material, "a material");

    /// <summary>
    /// Spawns a 3D camera and returns it.
    /// </summary>
    /// <remarks>
    /// Bevy draws nothing without a camera. The new one sits at the origin looking down negative
    /// Z; position it by writing its <see cref="Transform"/> like any other entity.
    /// </remarks>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d() => new(Native.bcs_render_spawn_camera_3d());

    /// <summary>
    /// Spawns a light and returns it.
    /// </summary>
    /// <param name="kind">Which light to spawn.</param>
    /// <param name="intensity">
    /// Illuminance in lux for a directional light, luminous power in lumens for a point light.
    /// </param>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightKind kind, float intensity) =>
        new(Native.bcs_render_spawn_light((int)kind, intensity));

    /// <summary>Attaches a handle through one of the components that carry one.</summary>
    private static void Attach(
        EcsWorld world,
        Entity entity,
        string component,
        AssetHandle handle,
        string described)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_ecs_insert_asset(entity.Bits, component, handle.Key);
        if (status == NativeStatus.Unsupported) throw NoRenderer($"Attaching {described}");
        if (status == NativeStatus.NoEntity)
            throw new BevyNativeException(
                NativeStatus.NoEntity,
                $"Cannot attach {described} to {entity}: either the entity is no longer alive, "
                + "or the handle has been released.");
        if (status == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"That handle does not point at {described}. Check that the handle came from the "
                + "matching Create call.");

        Native.Check(status, $"attaching {described}");
    }

    /// <summary>The error for asking a headless build to do something graphical.</summary>
    private static BevyNativeException NoRenderer(string attempted) =>
        new(NativeStatus.Unsupported,
            $"{attempted} needs a native build with the renderer compiled in. Rebuild the bridge "
            + "with build/build-native.sh --render, or guard the call with App.HasRenderer.");
}

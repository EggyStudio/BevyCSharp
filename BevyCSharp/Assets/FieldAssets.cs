namespace Bevy;

/// <summary>
/// The handles the asset fields of the components a scene, a save or an instance's overrides
/// spawns are read with, one for each asset a path names as a kind, shared by every field naming
/// it and released once no component's field holds it.
/// </summary>
/// <remarks>
/// <para>
/// A field read from a file is given the handle its asset was last read with, while that handle is
/// held, rather than a handle of its own, so a level loaded again over itself takes no more handles
/// than its first load did, where each load would take one for every field and nothing would
/// release them, and the engine's table of handles would grow with every load.
/// </para>
/// <para>
/// What no component's field holds, since the entities holding it were despawned or their fields
/// were given another asset, is released as each scene a game loads has been read, so an asset the
/// new scene names as well is shared with it rather than read again, and an asset of a level left
/// behind goes once nothing else holds it, as a handle given to <see cref="AssetServer.Release"/>
/// does. A handle read off a field and kept somewhere else names nothing once its last field has
/// gone, so a game keeping an asset past the entities naming it loads one of its own with
/// <see cref="AssetServer.Load"/>.
/// </para>
/// <para>
/// The fields read are the C# components' alone. Reading a handle out of one of Bevy's components
/// takes a handle the program then holds, so a sweep reading them would hold one for every mesh
/// drawn, and Bevy's component holds its asset itself, whatever this side releases. A data asset's
/// fields and the resources a scene was saved with keep handles of their own, read once as each is,
/// since no entity holds them and a sweep of the entities would take their assets away.
/// </para>
/// </remarks>
internal static class FieldAssets
{
    private static readonly Dictionary<(string Kind, string Path), AssetHandle> Shared = [];

    /// <summary>The world an item's fields are given, which they ignore, since they read a box.</summary>
    private static readonly EcsWorld Loose = new();

    /// <summary>The handle a field naming an asset is given, shared with every other field naming it.</summary>
    internal static AssetHandle Load(string kind, string path)
    {
        if (Shared.TryGetValue((kind, path), out var held) && AssetServer.IsAlive(held)) return held;

        var loaded = AssetServer.Load(kind, path);
        Shared[(kind, path)] = loaded;
        return loaded;
    }

    /// <summary>
    /// Releases every shared handle no field of a C# component in the world holds, called once a
    /// scene a game loaded has been read.
    /// </summary>
    /// <returns>How many were released.</returns>
    internal static int Release(EcsWorld world)
    {
        if (Shared.Count == 0) return 0;

        var held = new HashSet<AssetHandle>();
        foreach (var entity in world.All())
        {
            foreach (var id in world.ComponentsOf(entity))
            {
                if (ComponentSchemas.For(id) is not { Origin: SchemaOrigin.Declared } schema) continue;
                foreach (var field in schema.Fields)
                {
                    if (!field.Derived && CanHold(field)) Collect(field, field.Read(world, entity), held);
                }
            }
        }

        var released = 0;
        foreach (var (key, handle) in Shared.ToArray())
        {
            if (held.Contains(handle)) continue;

            Shared.Remove(key);
            if (AssetServer.Release(handle)) released++;
        }

        return released;
    }

    /// <summary>Forgets the shared handles, for an app starting, whose table of handles is its own.</summary>
    internal static void Forget() => Shared.Clear();

    /// <summary>Whether a field can hold an asset, itself or in a list's or a map's items.</summary>
    private static bool CanHold(ComponentField field) => field.Kind switch
    {
        FieldKind.Asset => true,
        FieldKind.List or FieldKind.Map => field.ElementKind is FieldKind.Asset or FieldKind.Struct,
        _ => false,
    };

    /// <summary>Adds the handles a field's value holds, itself, a list's items or a map's values.</summary>
    private static void Collect(ComponentField field, object? value, HashSet<AssetHandle> held)
    {
        switch (value)
        {
            case AssetHandle { IsValid: true } handle:
                held.Add(handle);
                break;

            case ListValue items:
                foreach (var item in items) Item(field, item, held);
                break;

            case MapValue entries:
                foreach (var entry in entries) Item(field, entry.Value, held);
                break;
        }
    }

    /// <summary>Adds the handles one item of a list or a map holds, read through its fields where it has them.</summary>
    private static void Item(ComponentField field, object? item, HashSet<AssetHandle> held)
    {
        if (item is null) return;
        if (field.ElementKind != FieldKind.Struct || field.Items is not { } fields)
        {
            Collect(field, item, held);
            return;
        }

        foreach (var inner in fields.Bind(item).Schema.Fields)
        {
            if (!inner.Derived && CanHold(inner)) Collect(inner, inner.Read(Loose, Entity.None), held);
        }
    }
}

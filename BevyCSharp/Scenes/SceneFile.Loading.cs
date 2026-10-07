using System.Text.Json;

namespace Bevy;

public static partial class SceneFile
{
    /// <summary>
    /// Spawns everything a scene file holds.
    /// </summary>
    /// <param name="world">The world to spawn into.</param>
    /// <param name="path">Where, as <see cref="Save"/> takes it.</param>
    /// <param name="parent">An entity to put the scene's top level under, or none.</param>
    /// <exception cref="InvalidDataException">The file is not a scene in this format.</exception>
    public static SceneLoad Load(EcsWorld world, string path, Entity parent = default)
    {
        ArgumentNullException.ThrowIfNull(world);

        using var document = AssetFiles.ReadJson(Resolve(path), "a scene");
        if (!document.RootElement.TryGetProperty("format", out var format) || format.GetString() != Format)
            throw new InvalidDataException($"{path} is not a scene in the {Format} format.");
        return Read(world, document.RootElement, parent);
    }

    /// <summary>Spawns everything a scene document holds.</summary>
    /// <exception cref="InvalidDataException">The document is not a scene in this format.</exception>
    public static SceneLoad Read(EcsWorld world, JsonElement scene, Entity parent = default)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!scene.TryGetProperty("format", out var format) || format.GetString() != Format)
            throw new InvalidDataException($"Not a scene in the {Format} format.");

        var entries = scene.TryGetProperty("entities", out var listed) && listed.ValueKind == JsonValueKind.Array ? listed.EnumerateArray().ToArray() : [];

        // The meshes and materials made in memory, each made once and shared by what refers to it,
        // as it was when the scene was written.
        var made = App.HasRenderer && scene.TryGetProperty("resources", out var resources)
            ? Resources.Make(resources)
            : [];
        if (!KeepsAssets)
        {
            foreach (var handle in made.Values) AssetServer.ReleaseWhenUnused(handle);
        }
        var references = new SceneReferences();
        var spawned = new List<Entity>(entries.Length);
        var byId = new Dictionary<int, Entity>();

        // Every entity first, so a component referring to one listed later finds it.
        foreach (var entry in entries)
        {
            // An instance is spawned from its scene, with its overrides kept to apply once Bevy
            // has spawned the scene's entities under it.
            var entity = entry.TryGetProperty("instance", out var instance)
                && SceneReferences.ReadFile(instance) is { Length: > 0 } placed
                    ? SpawnInstance(world, placed, entry)
                    : world.Spawn();
            spawned.Add(entity);

            if (entry.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id))
            {
                byId[id] = entity;
                references.Spawned(id, entity);
                world.Add(entity, new SceneId { Value = id });
            }

            if (entry.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } named)
                world.SetName(entity, named);
        }

        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        var refused = new List<string>();

        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var entity = spawned[i];

            // Parented before its components are written, so a transform is read as relative to the
            // parent it belongs under, as it was written.
            var above = entry.TryGetProperty("parent", out var parentId)
                && parentId.TryGetInt32(out var pid)
                && byId.TryGetValue(pid, out var found)
                    ? found
                    : parent;
            if (!above.IsNone) world.SetParent(entity, above);

            if (App.HasRenderer)
            {
                if (Drawn(entry, "mesh", AssetKind.Mesh, made) is { IsValid: true } mesh)
                    Render.SetMesh(world, entity, mesh);

                if (Drawn(entry, "material", AssetKind.StandardMaterial, made) is { IsValid: true } material)
                    Render.SetMaterial(world, entity, material);
            }

            if (!entry.TryGetProperty("components", out var components)) continue;

            ReadComponents(world, entity, components, references, unknown, refused);
        }

        // The children added under an instance's nodes are spawned and under the root, and go
        // under their nodes now or when the instance has spawned its scene.
        foreach (var entity in spawned)
            if (SceneInstances.IsInstance(world, entity)) SceneInstances.ResolveChildren(world, entity, byId);

        return new SceneLoad(spawned, [.. unknown], refused);
    }

    /// <summary>Bevy's components read after the rest of an entity's, for what Bevy checks as they arrive.</summary>
    /// <remarks>
    /// A camera's settings warn the moment they are added unless the entity already says what kind
    /// of camera it is, and the kind (<c>Camera3d</c>) brings the settings with it, so read second
    /// they are written over the ones it brought and nothing is warned of.
    /// </remarks>
    private static readonly HashSet<string> InsertedLast = new(StringComparer.Ordinal) { "bevy_camera::camera::Camera" };

    /// <summary>
    /// Puts the components a component object names on an entity, keeping on it what cannot be read.
    /// </summary>
    /// <remarks>
    /// A component the entity carries already has the fields named written over it and the rest
    /// left, rather than being replaced by a default first, so a save laid over a scene's entity
    /// changes what it holds and nothing else.
    /// </remarks>
    internal static void ReadComponents(
        EcsWorld world,
        Entity entity,
        JsonElement components,
        SceneReferences references,
        ISet<string> unknown,
        List<string> refused)
    {
        var kept = new SceneKept.Kept();
        foreach (var component in components.EnumerateObject().OrderBy(component => InsertedLast.Contains(component.Name) ? 1 : 0))
        {
            var schema = ComponentSchemas.For(component.Name);

            if (schema is { Origin: SchemaOrigin.Reflected })
            {
                try
                {
                    world.InsertReflected(entity, component.Name, component.Value.GetRawText());
                }
                catch (Interop.BevyNativeException error)
                {
                    refused.Add($"{component.Name}: {error.Message}");
                    kept.Components.Add(new(component.Name, component.Value.GetRawText()));
                }

                continue;
            }

            if (schema is null || !schema.CanAdd)
            {
                unknown.Add(component.Name);
                kept.Components.Add(new(component.Name, component.Value.GetRawText()));
                continue;
            }

            // Brought up to the type's current shape first, so the fields read and the values
            // kept are both of that shape.
            var value = SceneValue.Migrated(component.Value, schema);
            if (!Present(world, entity, schema.QualifiedName)) schema.Add(world, entity);
            SceneValue.ReadComponent(value, schema, world, entity, references);

            if (SceneValue.Unread(value, schema) is { Count: > 0 } unread)
                kept.Fields[schema.QualifiedName] = unread;
        }

        if (kept.IsEmpty) return;

        // Laid over what an earlier load kept, rather than a second component replacing it.
        if (world.Has<SceneKept>(entity) && world.GetRef<SceneKept>(entity).Value is { } earlier)
        {
            earlier.Components.AddRange(kept.Components);
            foreach (var (name, fields) in kept.Fields) earlier.Fields[name] = fields;
        }
        else
        {
            world.Add(entity, SceneKept.Of(kept));
        }
    }

    /// <summary>
    /// What an entity is drawn with: one of the scene's resources, or a file found by its id or
    /// its path.
    /// </summary>
    private static AssetHandle Drawn(JsonElement entry, string name, string kind, Dictionary<int, AssetHandle> made)
    {
        if (!entry.TryGetProperty(name, out var value)) return AssetHandle.None;

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("resource", out var id))
            return made.TryGetValue(id.GetInt32(), out var handle) ? handle : AssetHandle.None;

        if (SceneReferences.ReadFile(value) is not { Length: > 0 } path) return AssetHandle.None;

        // A material or mesh file is read on this side into one asset shared by every scene using it.
        if (MaterialFiles.IsMaterialFile(path)) return MaterialFiles.Load(path);
        if (MeshFiles.IsMeshFile(path)) return MeshFiles.Load(path);

        // Any other file takes a handle at each load, for the entity it is put on alone.
        var loaded = AssetServer.Load(kind, path);
        if (!KeepsAssets) AssetServer.ReleaseWhenUnused(loaded);
        return loaded;
    }

    /// <summary>Spawns an instance a scene file names, with the overrides the file holds for it.</summary>
    private static Entity SpawnInstance(EcsWorld world, string scene, JsonElement entry)
    {
        var read = new List<InstanceOverride>();

        if (entry.TryGetProperty("overrides", out var overrides) && overrides.ValueKind == JsonValueKind.Array)
        {
            foreach (var change in overrides.EnumerateArray())
            {
                if (!change.TryGetProperty("at", out var atElement) || atElement.GetString() is not { } at) continue;

                if (change.TryGetProperty("delete", out var delete) && delete.ValueKind == JsonValueKind.True)
                    read.Add(new InstanceOverride(at, OverrideKind.Delete));

                if (change.TryGetProperty("rename", out var renamed) && renamed.GetString() is { Length: > 0 } called)
                    read.Add(new InstanceOverride(at, OverrideKind.Rename, Fields: called));

                if (change.TryGetProperty("child", out var child) && child.TryGetInt32(out var childId))
                    read.Add(new InstanceOverride(at, OverrideKind.Child, Fields: "#" + childId));

                if (change.TryGetProperty("remove", out var removed) && removed.GetString() is { Length: > 0 } component)
                    read.Add(new InstanceOverride(at, OverrideKind.Remove, component));

                foreach (var (name, kind) in new[] { ("set", OverrideKind.Set), ("add", OverrideKind.Add) })
                {
                    if (!change.TryGetProperty(name, out var components) || components.ValueKind != JsonValueKind.Object)
                        continue;

                    foreach (var written in components.EnumerateObject())
                        read.Add(new InstanceOverride(at, kind, written.Name, written.Value.GetRawText()));
                }
            }
        }

        // Given the overrides as it is made, so a subscene, which is read and applied at once, has
        // them to apply.
        return SceneInstances.Place(world, scene, read);
    }
}

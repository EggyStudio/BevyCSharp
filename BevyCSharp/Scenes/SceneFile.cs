using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Writes entities to a scene file and spawns them from one again.
/// </summary>
/// <remarks>
/// <para>
/// A scene file holds every entity it was given, named or not, with its place in the hierarchy,
/// its components and what it is drawn with. Loading spawns all of it, so a scene is a file rather
/// than a set of edits over entities code made:
/// </para>
/// <code>
/// { "format": "bevycsharp.scene.2",
///   "entities": [
///     { "id": 1, "name": "Player",
///       "components": { "Bevy.Transform": { "Translation": [0, 1, 0], … },
///                       "Game.Health": { "Current": 80, "Target": { "entity": 2 } } } },
///     { "id": 2, "parent": 1, "name": "Camera", "components": { … } } ] }
/// </code>
/// <para>
/// Each entity has an id local to the file, kept across saves through <see cref="SceneId"/>. The
/// hierarchy is a parent id, and siblings keep the order the file lists them in. A field holding
/// an entity is written as that entity's id and read back as whichever entity the id spawned as, so
/// a reference inside the scene survives the reload, and one to an entity outside it is written as
/// nothing. Components are written by <see cref="SceneValue"/>, keyed by the schema's full name, so a
/// C# component, a mirrored one of Bevy's and a data reference all go through one writer.
/// </para>
/// <para>
/// Bevy's reflected components (a camera's projection, a light's settings) are written as the JSON
/// Bevy's own serializer makes for them, keyed by their Rust path, and read back through it, less
/// the ones the engine works out for itself (<see cref="Computed"/>). A mesh or material built in
/// memory has no path, so it is written once as a resource of the scene saying how to make it.
/// </para>
/// <para>
/// A glTF scene placed with <see cref="SceneInstances.Spawn"/> is written as an instance, the
/// reference to its scene and the overrides made over it, and the entities Bevy spawned from the
/// scene are left out. Loading spawns the scene again and applies the overrides once Bevy reports
/// it ready.
/// </para>
/// <para>
/// A component this build has no type for, one Bevy would not take, and a value no field reads are
/// reported in <see cref="SceneLoad"/> and kept on the entity as the JSON the file had, so the next
/// save writes them back where they were. A type or a field renamed with
/// <see cref="FormerNameAttribute"/> is read from its old name and written under its new one.
/// </para>
/// </remarks>
public static partial class SceneFile
{
    static SceneFile() => SceneKept.Initialize();

    /// <summary>The format the file says it is in.</summary>
    public const string Format = "bevycsharp.scene.2";

    /// <summary>The prefix naming a path under the asset root.</summary>
    public const string AssetsPrefix = "assets://";

    /// <summary>
    /// Entities a save leaves out whatever it is asked to write, such as the ones a tool spawns for
    /// itself.
    /// </summary>
    /// <remarks>
    /// Set by the editor to its own cameras and previews, so a scene saved from inside it holds the
    /// scene and not the editor. Nothing by default.
    /// </remarks>
    public static Func<EcsWorld, Entity, bool>? Excluded { get; set; }

    /// <summary>
    /// Whether the meshes and materials a load makes and loads for its entities are kept after none
    /// of them draws with them any more.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Off by default, so what a load made goes with the last entity using it
    /// (<see cref="AssetServer.ReleaseWhenUnused"/>), as Bevy lets go of a scene's assets.
    /// Otherwise a level loaded again, or a save loaded over it, would make its look again each
    /// time while every earlier load's stayed held, which a game left running climbs by.
    /// </para>
    /// <para>
    /// Set by the editor, whose undo puts a deleted entity back with the handles it was drawn with,
    /// which would name nothing once the entity had been gone a frame or two.
    /// </para>
    /// </remarks>
    public static bool KeepsAssets { get; set; }

    /// <summary>
    /// Bevy's components a scene does not write, by short name, because the engine works them out
    /// for itself or the file says them another way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A global transform from a local one, a view's visibility from an inherited one, a camera's
    /// frustum and what it sees, a light's shadow cascades. Each is reflected and so could be
    /// written, and each is pages of numbers the next frame overwrites, which loading would insert
    /// for the engine to fight. The name, the hierarchy and the mesh and material are written as
    /// the file's own fields instead.
    /// </para>
    /// <para>
    /// A set rather than a constant, so a plugin whose components are worked out the same way can
    /// say so. The editor leaves the same components out of its inspector.
    /// </para>
    /// </remarks>
    public static readonly HashSet<string> Computed =
    [
        "GlobalTransform",
        "PreviousGlobalTransform",
        "TransformTreeChanged",
        "InheritedVisibility",
        "ViewVisibility",
        "VisibilityClass",
        "Aabb",
        "Name",
        "SyncToRenderWorld",
        "MainEntity",
        "RenderEntity",
        "Children",
        "ChildOf",
        "Mesh3d",
        "MeshMaterial3d",
        "PickingInteraction",
        "Pickable",
        "Frustum",
        "VisibleEntities",
        "Clusters",
        "PreviousViewData",
        "CameraRenderGraph",
        "Cascades",
        "CascadesFrusta",
        "CascadesVisibleEntities",
        "CubemapFrusta",
        "CubemapVisibleEntities",
        "VisibleMeshEntities",
        "WorldAssetRoot",
        "WorldInstance",
    ];

    /// <summary>Whether a component, by its full name, is one the engine works out for itself.</summary>
    public static bool IsComputed(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var open = name.IndexOf('<');
        var head = open < 0 ? name : name[..open];
        var cut = Math.Max(head.LastIndexOf(':'), head.LastIndexOf('.'));
        return Computed.Contains(cut < 0 ? head : head[(cut + 1)..]);
    }

    /// <summary>
    /// Writes entities to a scene file, and their children with them.
    /// </summary>
    /// <param name="world">The world they are in.</param>
    /// <param name="path">
    /// Where, as a path under the asset root (with or without <see cref="AssetsPrefix"/>) or an
    /// absolute one.
    /// </param>
    /// <param name="include">
    /// Which entities to write, or nothing for every entity carrying a component of this project's
    /// own or a mirrored one of Bevy's, which leaves out the engine's own bookkeeping.
    /// </param>
    /// <param name="giveIds">
    /// Whether a file the scene refers to that has no id is given one, by writing a sidecar beside
    /// it (<see cref="SceneReferences.GiveIds"/>). For a tool editing the project.
    /// </param>
    /// <returns>How many entities were written.</returns>
    /// <exception cref="InvalidOperationException">
    /// The scene would place an instance of itself, directly or through the scenes it places.
    /// </exception>
    public static int Save(EcsWorld world, string path, Func<Entity, bool>? include = null, bool giveIds = false)
    {
        ArgumentNullException.ThrowIfNull(world);

        // Refused before anything is written, since a scene placing itself could never be loaded.
        var target = Path.GetFullPath(Resolve(path));
        foreach (var entity in world.All())
        {
            if (SceneInstances.DataOf(world, entity) is { } placed
                && SceneInstances.OuterRootOf(world, entity) == entity
                && (include?.Invoke(entity) ?? true)
                && SceneInstances.Places(placed.Path, target))
                throw new InvalidOperationException($"{path} would contain an instance of itself, through {placed.Path}.");
        }

        var buffer = new ArrayBufferWriter<byte>();
        int written;
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            written = Write(world, json, include, giveIds);

        var full = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        // Through a temporary file renamed over the old one, so a crash while saving leaves the last
        // scene whole.
        var temporary = full + ".tmp";
        File.WriteAllText(temporary, Encoding.UTF8.GetString(buffer.WrittenSpan) + "\n");
        File.Move(temporary, full, overwrite: true);
        return written;
    }

    /// <summary>Writes entities as a scene document.</summary>
    /// <returns>How many entities were written.</returns>
    public static int Write(
        EcsWorld world, Utf8JsonWriter json, Func<Entity, bool>? include = null, bool giveIds = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(json);

        include ??= entity => Default(world, entity);
        // An instance's own entities come from its scene, so they are left out and the instance is
        // written as the reference and the changes made over it. An entity added under one of its
        // nodes is this scene's and is written, with an override putting it under the node.
        var chosen = world.All()
            .Where(entity => include(entity) && Excluded?.Invoke(world, entity) != true)
            .Where(entity => !SceneInstances.IsFromModel(world, entity))
            .ToHashSet();
        var ordered = Ordered(world, chosen);

        // Ids first, all of them, so a field referring to an entity written later in the file is
        // named as well as one referring to an entity written before it.
        var references = new SceneReferences { GiveIds = giveIds };
        var ids = Ids(world, ordered);
        foreach (var (entity, id) in ids) references.Name(entity, id);

        // Each entity added under a node of an instance, found by the node it sits under, and
        // written under the instance's root with an override naming the node.
        var added = new Dictionary<Entity, List<(string At, Entity Child)>>();
        var under = new Dictionary<Entity, Entity>();
        foreach (var entity in ordered)
        {
            var parent = world.ParentOf(entity);
            if (parent.IsNone || chosen.Contains(parent) || !SceneInstances.IsFromModel(world, parent)) continue;

            var root = SceneInstances.OuterRootOf(world, parent);
            if (!ids.ContainsKey(root) || SceneInstances.PathOf(world, root, parent) is not { } at) continue;

            if (!added.TryGetValue(root, out var list)) added[root] = list = [];
            list.Add((at, entity));
            under[entity] = root;
        }

        // Meshes and materials made in memory, written once each however many entities share them.
        var made = new Resources(references);

        json.WriteStartObject();
        json.WriteString("format", Format);
        json.WriteStartArray("entities");

        foreach (var entity in ordered)
        {
            json.WriteStartObject();
            json.WriteNumber("id", ids[entity]);

            var parent = under.TryGetValue(entity, out var instanceRoot) ? instanceRoot : world.ParentOf(entity);
            if (!parent.IsNone && ids.TryGetValue(parent, out var parentId)) json.WriteNumber("parent", parentId);
            if (world.NameOf(entity) is { } name) json.WriteString("name", name);

            if (SceneInstances.DataOf(world, entity) is { } instance)
            {
                json.WritePropertyName("instance");
                references.WriteFile(json, instance.Path);

                // The children are the world's as it is, so the ones recorded at the last load are
                // left for the ones found now.
                var children = added.GetValueOrDefault(entity) ?? [];
                WriteOverrides(
                    json,
                    [
                        .. instance.Overrides.Where(change => change.Kind != OverrideKind.Child),
                        .. children.Select(pair => new InstanceOverride(pair.At, OverrideKind.Child, Fields: "#" + ids[pair.Child])),
                    ]);
            }

            if (App.HasRenderer)
            {
                // What it is drawn with, by the file it came from, or, for one made in memory, as a
                // resource of the scene saying how to make it again.
                if ((Render.MeshPathOf(entity) is { Length: > 0 } loadedMesh
                        ? loadedMesh
                        : MeshFiles.PathOf(Render.MeshOf(world, entity))) is { Length: > 0 } meshPath)
                {
                    json.WritePropertyName("mesh");
                    references.WriteFile(json, meshPath);
                }
                else if (made.Mesh(Render.MeshOf(world, entity)) is var mesh and > 0)
                    Resource(json, "mesh", mesh);

                if ((Render.MaterialPathOf(entity) is { Length: > 0 } loaded
                        ? loaded
                        : MaterialFiles.PathOf(Render.MaterialOf(world, entity))) is { Length: > 0 } materialPath)
                {
                    json.WritePropertyName("material");
                    references.WriteFile(json, materialPath);
                }
                else if (made.Material(Render.MaterialOf(world, entity)) is var material and > 0)
                    Resource(json, "material", material);
            }

            json.WritePropertyName("components");
            WriteComponents(world, entity, json, references);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        made.Write(json);
        json.WriteEndObject();
        return ordered.Count;
    }

    /// <summary>
    /// Whether a scene can say how to make what an entity is drawn with, as <see cref="Write"/>
    /// writes it, or the entity is drawn with nothing.
    /// </summary>
    /// <remarks>
    /// A mesh or a material from a file is named by the file, and one made in memory is written as
    /// a primitive's recipe, a mesh's geometry or a standard material's settings. Anything else,
    /// such as a material drawn by a shader of the game's own, is left out of a scene, and an
    /// entity carrying one comes back from it with nothing to draw.
    /// </remarks>
    public static bool CanDescribe(EcsWorld world, Entity entity)
    {
        if (!App.HasRenderer) return true;

        var mesh = Render.MeshOf(world, entity);
        var meshKnown = !mesh.IsValid
            || Render.MeshPathOf(entity) is { Length: > 0 }
            || MeshFiles.PathOf(mesh) is { Length: > 0 }
            || Render.RecipeOf(mesh) is not null
            || Render.DataOf(mesh) is not null;

        var material = Render.MaterialOf(world, entity);
        var materialKnown = !material.IsValid
            || Render.MaterialPathOf(entity) is { Length: > 0 }
            || MaterialFiles.PathOf(material) is { Length: > 0 }
            || Render.TryReadMaterial(material, out _);

        return meshKnown && materialKnown;
    }

    /// <summary>
    /// Writes an entity's components as an object keyed by their full names, with what a load kept
    /// for it back where it was.
    /// </summary>
    /// <param name="world">The world it is in.</param>
    /// <param name="entity">The entity.</param>
    /// <param name="json">Where to write.</param>
    /// <param name="references">How to name entities and assets.</param>
    /// <param name="which">Which components to write, or nothing for every one a scene writes.</param>
    internal static void WriteComponents(
        EcsWorld world, Entity entity, Utf8JsonWriter json, SceneReferences references, Func<ComponentSchema, bool>? which = null)
    {
        var kept = world.Has<SceneKept>(entity) ? world.GetRef<SceneKept>(entity).Value : null;

        json.WriteStartObject();
        foreach (var id in world.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (which is not null && !which(schema)) continue;

            if (schema.Origin == SchemaOrigin.Reflected)
            {
                // Bevy's own, written as the JSON Bevy's serializer makes for it and read back
                // through the same, keyed by its Rust path. One that has no JSON form, such as
                // a component holding a handle, is left out.
                if (IsComputed(schema.QualifiedName)) continue;
                if (Reflected(world, entity, schema.QualifiedName) is not { } text) continue;

                json.WritePropertyName(schema.QualifiedName);
                json.WriteRawValue(text);
                continue;
            }

            json.WritePropertyName(schema.QualifiedName);
            SceneValue.WriteComponent(
                json, schema, world, entity, references, kept?.Fields.GetValueOrDefault(schema.QualifiedName));
        }

        // What the file held that this build could not read, back where it was. A component
        // this build has since been given, by code or by hand, was written above and wins.
        foreach (var (component, raw) in which is null ? kept?.Components ?? [] : [])
        {
            if (Present(world, entity, component)) continue;

            json.WritePropertyName(component);
            json.WriteRawValue(raw);
        }

        json.WriteEndObject();
    }

    /// <summary>Writes a reference to one of the scene's resources.</summary>
    private static void Resource(Utf8JsonWriter json, string name, int id)
    {
        json.WriteStartObject(name);
        json.WriteNumber("resource", id);
        json.WriteEndObject();
    }

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

        using var document = JsonDocument.Parse(AssetFiles.ReadAllText(Resolve(path)));
        return Read(world, document.RootElement, parent);
    }

    /// <summary>Spawns everything a scene document holds.</summary>
    /// <exception cref="InvalidDataException">The document is not a scene in this format.</exception>
    public static SceneLoad Read(EcsWorld world, JsonElement scene, Entity parent = default)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!scene.TryGetProperty("format", out var format) || format.GetString() != Format)
            throw new InvalidDataException($"Not a scene in the {Format} format.");

        var entries = scene.GetProperty("entities").EnumerateArray().ToArray();

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

    /// <summary>
    /// Writes an instance's overrides, each as an object naming its node and what is done there.
    /// </summary>
    /// <remarks>
    /// <c>{ "at": "Hull/Turret", "set": { "Bevy.Transform": { … } } }</c>, with <c>add</c> in
    /// place of <c>set</c> for a component put on, <c>"remove": "Game.Aim"</c> for one taken off,
    /// and <c>"delete": true</c> for a node despawned. A component is written under its current
    /// name, so one read under a name its type gave up is saved under the new one.
    /// </remarks>
    private static void WriteOverrides(Utf8JsonWriter json, IReadOnlyList<InstanceOverride> overrides)
    {
        if (overrides.Count == 0) return;

        json.WriteStartArray("overrides");
        foreach (var change in overrides)
        {
            json.WriteStartObject();
            json.WriteString("at", change.At);

            var component = change.Component is { } named
                ? ComponentSchemas.For(named)?.QualifiedName ?? named
                : null;

            switch (change.Kind)
            {
                case OverrideKind.Set or OverrideKind.Add when component is not null:
                    json.WriteStartObject(change.Kind == OverrideKind.Set ? "set" : "add");
                    json.WritePropertyName(component);
                    json.WriteRawValue(change.Fields ?? "{}");
                    json.WriteEndObject();
                    break;

                case OverrideKind.Remove when component is not null:
                    json.WriteString("remove", component);
                    break;

                case OverrideKind.Delete:
                    json.WriteBoolean("delete", true);
                    break;

                case OverrideKind.Rename when change.Fields is { Length: > 0 } name:
                    json.WriteString("rename", name);
                    break;

                case OverrideKind.Child when change.Fields is ['#', .. var number] && int.TryParse(number, out var id):
                    json.WriteNumber("child", id);
                    break;
            }

            json.WriteEndObject();
        }

        json.WriteEndArray();
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

    /// <summary>Whether an entity carries the component a file names, under the name the file used.</summary>
    private static bool Present(EcsWorld world, Entity entity, string name)
    {
        if (ComponentSchemas.For(name) is not { } schema) return false;

        try
        {
            return world.ComponentsOf(entity).Contains(schema.Id);
        }
        catch (Interop.BevyNativeException)
        {
            return false;
        }
    }

    /// <summary>One of Bevy's components as Bevy's JSON for it, or nothing when it has no JSON form.</summary>
    private static string? Reflected(EcsWorld world, Entity entity, string type)
    {
        try
        {
            return world.GetReflected(entity, type);
        }
        catch (Interop.BevyNativeException)
        {
            return null;
        }
    }

    /// <summary>
    /// The full path a scene path names: one under the asset root, with or without
    /// <see cref="AssetsPrefix"/>, one under the player's own directory after
    /// <see cref="UserData.Prefix"/>, or an absolute one as it is.
    /// </summary>
    public static string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (path.StartsWith(UserData.Prefix, StringComparison.Ordinal)) return UserData.Resolve(path);
        if (path.StartsWith(AssetsPrefix, StringComparison.Ordinal)) path = path[AssetsPrefix.Length..];
        return Path.IsPathRooted(path) ? path : Path.Combine(AssetIds.Root, path);
    }

    /// <summary>
    /// Whether an entity is one a game made, carrying a component of its own or a mirrored one of
    /// Bevy's, rather than one the engine keeps for itself.
    /// </summary>
    private static bool Default(EcsWorld world, Entity entity) =>
        world.ComponentsOf(entity).Any(id => ComponentSchemas.For(id) is { Origin: not SchemaOrigin.Reflected });

    /// <summary>
    /// The entities with every parent before its children, siblings in the order the world keeps
    /// them, so a file is read top down.
    /// </summary>
    private static List<Entity> Ordered(EcsWorld world, HashSet<Entity> chosen)
    {
        var ordered = new List<Entity>(chosen.Count);
        var placed = new HashSet<Entity>();

        void Visit(Entity entity)
        {
            if (!placed.Add(entity)) return;

            ordered.Add(entity);
            foreach (var child in world.ChildrenOf(entity))
                if (chosen.Contains(child)) Visit(child);
        }

        // A root is an entity whose parent is not written, which a child of an unwritten parent is
        // too, so it is written at the top level rather than lost.
        foreach (var entity in chosen.OrderBy(entity => entity.Bits))
        {
            var parent = world.ParentOf(entity);
            if (parent.IsNone || !chosen.Contains(parent)) Visit(entity);
        }

        return ordered;
    }

    /// <summary>
    /// Each entity's id: the one it was loaded with when no other entity being written has it, and
    /// the next unused number otherwise.
    /// </summary>
    private static Dictionary<Entity, int> Ids(EcsWorld world, List<Entity> ordered)
    {
        var ids = new Dictionary<Entity, int>();
        var used = new HashSet<int>();

        foreach (var entity in ordered)
        {
            if (world.TryGet<SceneId>(entity, out var kept) && kept.Value > 0 && used.Add(kept.Value))
                ids[entity] = kept.Value;
        }

        var next = used.Count == 0 ? 1 : used.Max() + 1;
        foreach (var entity in ordered)
        {
            if (ids.ContainsKey(entity)) continue;

            ids[entity] = next;
            used.Add(next);

            // Remembered on the entity, so the next save gives it the same id.
            world.Add(entity, new SceneId { Value = next });
            next++;
        }

        return ids;
    }
}

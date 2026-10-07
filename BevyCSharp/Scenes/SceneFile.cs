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
    /// Off by default, so what a load made goes with the last entity using it
    /// (<see cref="AssetServer.ReleaseWhenUnused"/>), as Bevy lets go of a scene's assets.
    /// Otherwise a level loaded again, or a save loaded over it, would make its look again each
    /// time while every earlier load's stayed held, which a game left running climbs by. The editor
    /// sets it, since its undo puts a deleted entity back with the handles it was drawn with, which
    /// would name nothing once the entity had been gone a frame or two.
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

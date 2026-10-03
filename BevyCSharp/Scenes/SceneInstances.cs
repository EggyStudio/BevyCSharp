using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bevy;

/// <summary>
/// Posted when Bevy has spawned a scene asset under an entity, with any overrides on it applied.
/// </summary>
/// <param name="Entity">The entity the scene was spawned under.</param>
/// <remarks>
/// Bevy announces a spawned scene to an observer, which the bridge turns into this message at the
/// top of the next frame, after the scene's entities are in the world. An instance placed with
/// <see cref="SceneInstances.Spawn"/> has had its overrides applied by the time this is read, so a
/// system reacting to it sees the instance as the scene file describes it.
/// </remarks>
public readonly record struct WorldInstanceReady(Entity Entity);

/// <summary>What an override does to the node it addresses.</summary>
public enum OverrideKind
{
    /// <summary>Writes the fields it names on a component the node has, leaving the rest.</summary>
    Set,

    /// <summary>Puts a component on the node with the fields it names.</summary>
    Add,

    /// <summary>Takes a component off the node.</summary>
    Remove,

    /// <summary>Despawns the node and everything under it.</summary>
    Delete,

    /// <summary>Gives the node another name, which <c>Fields</c> holds.</summary>
    /// <remarks>
    /// Paths keep naming the node by the model's name, so an override recorded after the rename
    /// still finds it when the scene loads and the model spawns it under its own name.
    /// </remarks>
    Rename,

    /// <summary>Puts an entity of the scene the instance is placed in under the node.</summary>
    /// <remarks>
    /// The entity is written in that scene like any of its own, and the override holds its id, so
    /// it goes under the node once the instance has spawned it.
    /// </remarks>
    Child,
}

/// <summary>
/// One change an instance makes to the scene it places.
/// </summary>
/// <param name="At">The node, as a path of names from the instance's root.</param>
/// <param name="Kind">What is done to it.</param>
/// <param name="Component">
/// The component's full name, as a scene file keys it, or nothing for <see cref="OverrideKind.Delete"/>.
/// </param>
/// <param name="Fields">
/// The fields written, as the JSON object a scene file holds for the component, or nothing where
/// the kind writes none. For <see cref="OverrideKind.Child"/>, the child: its id in the file as
/// <c>#8</c> until the file's entities are spawned, then the entity's bits.
/// </param>
public sealed record InstanceOverride(string At, OverrideKind Kind, string? Component = null, string? Fields = null);

/// <summary>
/// Marks the entity a scene asset was placed under as an instance, holding which scene it is and
/// the changes made to it.
/// </summary>
/// <remarks>
/// A handle into the managed store, freed with the entity and copied with a clone, as
/// the values a scene keeps for what it cannot read are. A tool asks whether an entity carries one
/// to learn that it is an instance, and reads what it holds through <see cref="SceneInstances"/>.
/// </remarks>
public struct SceneInstance
{
    internal int Slot;
    internal int Generation;

    internal readonly SceneInstances.Data? Value => EcsStore.Get(Slot, Generation) as SceneInstances.Data;

    internal static SceneInstance Of(SceneInstances.Data data)
    {
        var (slot, generation) = EcsStore.Allocate(data);
        return new SceneInstance { Slot = slot, Generation = generation };
    }

    /// <summary>Registers the hooks that free and copy the slot.</summary>
    /// <remarks>Called from the static constructors of everything that puts one on an entity.</remarks>
    internal static void Initialize()
    {
        ComponentHooks.OnRemove(static (in SceneInstance instance) => EcsStore.Release(instance.Slot, instance.Generation));
        ComponentHooks.OnClone(static (ref SceneInstance instance) =>
        {
            if (instance.Value is { } value) instance = Of(value.Copy());
        });
    }
}

/// <summary>
/// Places a glTF scene in the world as an instance, and keeps the changes made to it so a scene
/// file holds those changes rather than a copy of the model.
/// </summary>
/// <remarks>
/// <para>
/// An instance is a reference to a scene asset and a list of <see cref="InstanceOverride"/>s, each
/// addressing a node of it by its path of names from the instance's root, with <c>#2</c> after a
/// name its earlier siblings already have. Bevy spawns a glTF scene as an entity named after the
/// scene with the file's nodes under it, and each mesh primitive as a child of its node named after
/// the mesh and its material, so a path reads <c>Scene/Hull/Turret</c> and reaches a primitive as
/// <c>Scene/Hull/Turret/Barrel.Steel</c>. A scene file writes the reference and
/// the list and leaves the model's own entities out, so a model exported again reaches every place
/// it is used, and an edit made over it survives the export.
/// </para>
/// <para>
/// Bevy spawns the model's entities once its file has loaded, a frame or more after
/// <see cref="Spawn"/> returns. The overrides are applied when Bevy reports the instance ready, at
/// the top of the next frame and before <see cref="WorldInstanceReady"/> is read. One whose node
/// is not found is kept in the list and written back rather than dropped, and reported through
/// <see cref="Missed"/>, so a node renamed in the model can be repaired instead of losing its edits
/// without a word.
/// </para>
/// <para>
/// <see cref="Set"/>, <see cref="Add"/>, <see cref="Remove"/> and <see cref="Delete"/> change a node
/// and record the change in one call, which is how an editor or a game makes an override. Setting
/// a field records that field alone, so a later change to the model's other fields still arrives.
/// </para>
/// </remarks>
public static class SceneInstances
{
    static SceneInstances() => SceneInstance.Initialize();

    /// <summary>What an instance holds: the scene's path and its overrides.</summary>
    internal sealed class Data
    {
        public required string Path { get; set; }

        public List<InstanceOverride> Overrides { get; } = [];

        public List<string> Missed { get; } = [];

        /// <summary>
        /// The entities the scene spawned under the instance, taken when its overrides are first
        /// applied, which tells them from ones the scene placing it added.
        /// </summary>
        public HashSet<Entity> Model { get; } = [];

        /// <summary>Whether <see cref="Model"/> has been taken.</summary>
        public bool Tagged { get; set; }

        /// <summary>
        /// What the model held in each field an override sets, as JSON, by <see cref="Key"/>, so a
        /// revert has something to put back.
        /// </summary>
        public Dictionary<string, string> Originals { get; } = [];

        public Data Copy()
        {
            var copy = new Data { Path = Path };
            copy.Overrides.AddRange(Overrides);
            copy.Missed.AddRange(Missed);
            foreach (var (key, value) in Originals) copy.Originals[key] = value;
            return copy;
        }

        /// <summary>The key a field of a component on a node is remembered under.</summary>
        public static string Key(string at, string component, string field) => at + "\n" + component + "\n" + field;
    }

    /// <summary>
    /// Places a scene asset in the world under a new entity, as an instance.
    /// </summary>
    /// <param name="world">The world to spawn into.</param>
    /// <param name="path">
    /// The scene, as an asset path: a glTF file with the scene named after <c>#</c>
    /// (<c>models/ship.gltf#Scene0</c>), or the file alone for its first scene.
    /// </param>
    /// <returns>The instance's root, which has a transform of its own to place it by.</returns>
    /// <exception cref="InvalidDataException">
    /// A scene file that contains itself, directly or through the scenes it places.
    /// </exception>
    public static Entity Spawn(EcsWorld world, string path) => Place(world, path, []);

    /// <summary>Whether a path names a scene file, placed as a subscene, rather than a glTF scene.</summary>
    public static bool IsSubscene(string path) =>
        path.Split('#')[0].EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase);

    /// <summary>Places a scene with the overrides a scene file holds for it.</summary>
    /// <remarks>
    /// A glTF scene is spawned by Bevy once its file has loaded, and the overrides wait for it. A
    /// scene file is read here, under the root, and the overrides applied at once. Either is posted
    /// as <see cref="WorldInstanceReady"/>, a subscene at the top of the next frame, so code that
    /// waits for one waits for both the same way.
    /// </remarks>
    internal static Entity Place(EcsWorld world, string path, IEnumerable<InstanceOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var subscene = IsSubscene(path);
        if (!subscene && !path.Contains('#', StringComparison.Ordinal)) path += "#Scene0";

        var data = new Data { Path = path };
        data.Overrides.AddRange(overrides);

        if (!subscene)
        {
            var placed = world.SpawnScene(AssetServer.Load(AssetKind.Scene, path));
            world.Add(placed, Transform.Identity);
            world.Add(placed, SceneInstance.Of(data));
            return placed;
        }

        // A file read while it is already being read contains itself, which would never finish.
        var full = Path.GetFullPath(SceneFile.Resolve(path));
        Loading ??= [];
        if (!Loading.Add(full))
            throw new InvalidDataException($"{path} contains an instance of itself, directly or through another scene.");

        var root = world.Spawn();
        try
        {
            world.Add(root, Transform.Identity);
            world.Add(root, SceneInstance.Of(data));
            SceneFile.Load(world, path, root);
        }
        finally
        {
            Loading.Remove(full);
        }

        Report(world, root, Apply(world, root));
        lock (Pending) Pending.Add(root);
        return root;
    }

    /// <summary>The scene files being read on this thread, by full path, which a cycle finds itself in.</summary>
    [ThreadStatic]
    private static HashSet<string>? Loading;

    /// <summary>Subscenes placed since the last frame, posted as ready at the top of the next.</summary>
    private static readonly List<Entity> Pending = [];

    /// <summary>
    /// Whether a scene file places <paramref name="target"/>, itself or through the scenes it
    /// places, which a save checks so it never writes a scene that contains itself.
    /// </summary>
    /// <param name="scene">The scene file placed, as an instance's path.</param>
    /// <param name="target">The full path of the file being saved.</param>
    /// <param name="depth">How many scenes deep the search is, which stops one that runs away.</param>
    internal static bool Places(string scene, string target, int depth = 0)
    {
        if (!IsSubscene(scene) || depth > 32) return false;

        var full = Path.GetFullPath(SceneFile.Resolve(scene));
        if (string.Equals(full, target, StringComparison.Ordinal)) return true;
        if (!AssetFiles.Exists(full)) return false;

        try
        {
            using var document = JsonDocument.Parse(AssetFiles.ReadAllText(full));
            if (!document.RootElement.TryGetProperty("entities", out var entities)) return false;

            foreach (var entry in entities.EnumerateArray())
            {
                if (entry.TryGetProperty("instance", out var instance)
                    && SceneReferences.ReadFile(instance) is { Length: > 0 } inner
                    && Places(inner, target, depth + 1))
                    return true;
            }
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidOperationException)
        {
        }

        return false;
    }

    /// <summary>Whether an entity is an instance's root.</summary>
    public static bool IsInstance(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        return !entity.IsNone && world.IsAlive(entity) && world.Has<SceneInstance>(entity);
    }

    /// <summary>The scene an instance places, as the asset path it was spawned from, or nothing.</summary>
    public static string? SceneOf(EcsWorld world, Entity root) => DataOf(world, root)?.Path;

    /// <summary>The overrides an instance holds, in the order they were made.</summary>
    public static IReadOnlyList<InstanceOverride> Overrides(EcsWorld world, Entity root) =>
        DataOf(world, root)?.Overrides.ToArray() ?? [];

    /// <summary>
    /// The paths the last application of an instance's overrides could not find, which are kept in
    /// the instance and written back.
    /// </summary>
    public static IReadOnlyList<string> Missed(EcsWorld world, Entity root) =>
        DataOf(world, root)?.Missed.ToArray() ?? [];

    /// <summary>
    /// The instance root an entity belongs to: the nearest ancestor that is one, or the entity
    /// itself, or none.
    /// </summary>
    public static Entity RootOf(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        for (var at = entity; !at.IsNone; at = world.ParentOf(at))
            if (IsInstance(world, at)) return at;

        return Entity.None;
    }

    /// <summary>
    /// The outermost instance root an entity belongs to, which is the one a scene file writes, or
    /// none.
    /// </summary>
    public static Entity OuterRootOf(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        var found = Entity.None;
        for (var at = entity; !at.IsNone; at = world.ParentOf(at))
            if (IsInstance(world, at)) found = at;

        return found;
    }

    /// <summary>
    /// A node's path of names from an instance's root, or nothing when it is not under the root.
    /// </summary>
    /// <remarks>
    /// A node with no name is called by its place among its siblings (<c>#3</c>), and one sharing
    /// its name with earlier siblings gets <c>#2</c>, <c>#3</c> and on after it, which is what keeps
    /// two meshes a model calls the same apart. The root itself is the empty path.
    /// </remarks>
    public static string? PathOf(EcsWorld world, Entity root, Entity node)
    {
        ArgumentNullException.ThrowIfNull(world);

        var parts = new List<string>();
        for (var at = node; at != root; at = world.ParentOf(at))
        {
            if (at.IsNone) return null;
            parts.Add(Step(world, at));
        }

        parts.Reverse();
        return string.Join('/', parts);
    }

    /// <summary>The node a path of names leads to from an instance's root, or none.</summary>
    public static Entity Find(EcsWorld world, Entity root, string path)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(path);

        var at = root;
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            at = world.ChildrenOf(at).FirstOrDefault(child => Step(world, child) == part);
            if (at.IsNone) return Entity.None;
        }

        return at;
    }

    /// <summary>
    /// Writes a field of a component on a node of an instance, and records it as an override.
    /// </summary>
    /// <param name="world">The world the instance is in.</param>
    /// <param name="node">The node, which must be under an instance's root.</param>
    /// <param name="component">The component's full name, as its schema has it.</param>
    /// <param name="field">The field, as its schema names it, with dots into a struct.</param>
    /// <param name="value">The value, boxed as the field's writer takes it.</param>
    /// <returns>Whether the field was written and recorded.</returns>
    /// <remarks>
    /// The field is recorded alone and merged into what the override already holds for the
    /// component, so setting two fields leaves one override naming both.
    /// </remarks>
    public static bool Set(EcsWorld world, Entity node, string component, string field, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Locate(world, node) is null) return false;
        if (ComponentSchemas.For(component) is not { } schema || schema.Field(field) is not { } described) return false;

        var before = described.Read(world, node);
        return described.Write(world, node, value) && Mark(world, node, component, field, before);
    }

    /// <summary>
    /// Records what a field on a node of an instance holds as an override, for a tool that wrote it
    /// some other way, such as an inspector writing through the field itself.
    /// </summary>
    /// <param name="world">The world the instance is in.</param>
    /// <param name="node">The node, which must be under an instance's root and not the root itself.</param>
    /// <param name="component">The component's full name.</param>
    /// <param name="field">The field, as its schema names it.</param>
    /// <param name="before">
    /// What the field held before the write, remembered as the model's value the first time the
    /// field is overridden, for <see cref="Revert"/> to put back.
    /// </param>
    /// <returns>Whether the field was recorded, or found back at the model's value and let go.</returns>
    /// <remarks>
    /// <para>
    /// A field written back to the model's value, by an undo or by hand, is taken out of the
    /// override rather than recorded as a change to the same thing, so the list holds only what
    /// differs from the model.
    /// </para>
    /// <para>
    /// One of Bevy's reflected components is recorded whole, as the JSON Bevy writes for it, since
    /// that is the form a load hands back to Bevy. It is neither compared with the model's value
    /// nor reverted field by field.
    /// </para>
    /// </remarks>
    public static bool Mark(EcsWorld world, Entity node, string component, string field, object? before = null)
    {
        if (Locate(world, node) is not var (data, at)) return false;
        if (ComponentSchemas.For(component) is not { } schema || schema.Field(field) is not { } described) return false;

        if (schema.Origin == SchemaOrigin.Reflected)
        {
            if (world.GetReflected(node, schema.QualifiedName) is not { } whole) return false;
            Record(data, new InstanceOverride(at, OverrideKind.Set, schema.QualifiedName, whole));
            return true;
        }

        // The value as the file will hold it, read back through the field so what is recorded is
        // what the component holds rather than what was asked for.
        if (described.Read(world, node) is not { } held || Json(json => SceneValue.Write(json, described, held)) is not { } written)
            return false;

        var key = Data.Key(at, schema.QualifiedName, field);
        if (!data.Originals.ContainsKey(key)
            && !Holds(data, at, schema.QualifiedName, field)
            && before is not null
            && Json(json => SceneValue.Write(json, described, before)) is { } original)
            data.Originals[key] = original;

        if (data.Originals.TryGetValue(key, out var model) && JsonNode.DeepEquals(JsonNode.Parse(model), JsonNode.Parse(written)))
        {
            Unset(data, at, schema.QualifiedName, field);
            data.Originals.Remove(key);
            return true;
        }

        var fields = new JsonObject();
        Place(fields, field, JsonNode.Parse(written));
        Record(data, new InstanceOverride(at, OverrideKind.Set, schema.QualifiedName, fields.ToJsonString()));
        return true;
    }

    /// <summary>Whether a field on a node of an instance is set by one of its overrides.</summary>
    public static bool IsOverridden(EcsWorld world, Entity node, string component, string field)
    {
        if (Locate(world, node) is not var (data, at)) return false;
        return ComponentSchemas.For(component) is { } schema && Holds(data, at, schema.QualifiedName, field);
    }

    /// <summary>Whether the model's value of an overridden field is known, so it can be put back.</summary>
    public static bool CanRevert(EcsWorld world, Entity node, string component, string field) =>
        Locate(world, node) is var (data, at)
        && ComponentSchemas.For(component) is { } schema
        && data.Originals.ContainsKey(Data.Key(at, schema.QualifiedName, field));

    /// <summary>
    /// Puts the model's value back in an overridden field and takes the field out of the override.
    /// </summary>
    /// <returns>Whether the model's value was known and written.</returns>
    /// <remarks>
    /// The model's value is the one the field held before it was first overridden in this run, or
    /// before a load applied the override, so a field set in a file and never applied has none.
    /// </remarks>
    public static bool Revert(EcsWorld world, Entity node, string component, string field)
    {
        if (Locate(world, node) is not var (data, at)) return false;
        if (ComponentSchemas.For(component) is not { } schema || schema.Field(field) is not { } described) return false;

        var key = Data.Key(at, schema.QualifiedName, field);
        if (!data.Originals.TryGetValue(key, out var original)) return false;

        using var document = JsonDocument.Parse(original);
        if (SceneValue.Read(document.RootElement, described) is not { } value || !described.Write(world, node, value))
            return false;

        Unset(data, at, schema.QualifiedName, field);
        data.Originals.Remove(key);
        return true;
    }

    /// <summary>Puts a component on a node of an instance at its defaults, and records it.</summary>
    /// <returns>Whether the component was added and recorded.</returns>
    public static bool Add(EcsWorld world, Entity node, string component)
    {
        if (Locate(world, node) is null) return false;
        if (ComponentSchemas.For(component) is not { CanAdd: true } schema) return false;

        schema.Add(world, node);
        return MarkAdded(world, node, component);
    }

    /// <summary>
    /// Records a component a node of an instance carries as added, with every field it holds, for a
    /// tool that put it on some other way.
    /// </summary>
    /// <returns>Whether it was recorded.</returns>
    /// <remarks>
    /// Added after being removed, it replaces the removal with an add holding the values it has,
    /// which applied over the model's component sets each of them.
    /// </remarks>
    public static bool MarkAdded(EcsWorld world, Entity node, string component)
    {
        if (Locate(world, node) is not var (data, at)) return false;
        if (ComponentSchemas.For(component) is not { } schema) return false;

        var fields = schema.Origin == SchemaOrigin.Reflected
            ? world.GetReflected(node, schema.QualifiedName) ?? "{}"
            : Json(json => SceneValue.WriteComponent(json, schema, world, node) >= 0) ?? "{}";
        Record(data, new InstanceOverride(at, OverrideKind.Add, schema.QualifiedName, fields));
        return true;
    }

    /// <summary>Takes a component off a node of an instance, and records it.</summary>
    /// <returns>Whether the component was removed and recorded.</returns>
    public static bool Remove(EcsWorld world, Entity node, string component)
    {
        if (Locate(world, node) is null) return false;
        if (ComponentSchemas.For(component) is not { } schema) return false;

        if (schema.Origin == SchemaOrigin.Reflected) world.RemoveReflected(node, schema.QualifiedName);
        else if (!schema.Remove(world, node)) return false;

        return MarkRemoved(world, node, component);
    }

    /// <summary>
    /// Records a component as taken off a node of an instance, for a tool that took it off some
    /// other way.
    /// </summary>
    /// <returns>Whether it was recorded.</returns>
    /// <remarks>
    /// Removing a component an override added takes the add away instead, since the model never had
    /// it and there is nothing left to remove.
    /// </remarks>
    public static bool MarkRemoved(EcsWorld world, Entity node, string component)
    {
        if (Locate(world, node) is not var (data, at)) return false;
        if (ComponentSchemas.For(component) is not { } schema) return false;

        Record(data, new InstanceOverride(at, OverrideKind.Remove, schema.QualifiedName));
        return true;
    }

    /// <summary>Gives a node of an instance another name, and records it.</summary>
    /// <returns>Whether the node was renamed and recorded.</returns>
    /// <remarks>
    /// The node keeps its path, which names it as the model does, so overrides made before and after
    /// the rename all find it. Renaming it back to the model's name leaves no override.
    /// </remarks>
    public static bool Rename(EcsWorld world, Entity node, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (Locate(world, node) is not var (data, at)) return false;

        world.SetName(node, name);

        data.Overrides.RemoveAll(change => change.At == at && change.Kind == OverrideKind.Rename);
        if (name != NameOf(world, node)) data.Overrides.Add(new InstanceOverride(at, OverrideKind.Rename, Fields: name));
        return true;
    }

    /// <summary>Despawns a node of an instance and what is under it, and records it.</summary>
    /// <returns>Whether the node was deleted and recorded, which the root itself cannot be.</returns>
    public static bool Delete(EcsWorld world, Entity node)
    {
        if (Locate(world, node) is not var (data, at)) return false;

        world.Despawn(node);
        Record(data, new InstanceOverride(at, OverrideKind.Delete));
        return true;
    }

    /// <summary>
    /// Applies an instance's overrides to the scene spawned under it.
    /// </summary>
    /// <returns>The paths no node was found at, which stay in the instance.</returns>
    /// <remarks>
    /// Called when Bevy reports the instance ready, and callable again after the scene is spawned
    /// anew. Every path is found before anything is changed, so a delete earlier in the list does
    /// not take a node a later override addresses by a path through it.
    /// </remarks>
    public static IReadOnlyList<string> Apply(EcsWorld world, Entity root) => Apply(world, root, retry: false);

    /// <summary>Applies every override, or with <paramref name="retry"/> only the ones that missed.</summary>
    private static IReadOnlyList<string> Apply(EcsWorld world, Entity root, bool retry)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (DataOf(world, root) is not { } data) return [];

        if (!data.Tagged) Tag(world, root, data);

        var again = retry ? data.Missed.ToHashSet(StringComparer.Ordinal) : null;
        var chosen = data.Overrides.Where(change => again is null || again.Contains(change.At)).ToArray();
        var targets = chosen.Select(change => Find(world, root, change.At)).ToArray();

        data.Missed.Clear();
        for (var i = 0; i < chosen.Length; i++)
        {
            if (targets[i].IsNone || !world.IsAlive(targets[i]) || !Applied(world, targets[i], chosen[i]))
                data.Missed.Add(chosen[i].At);
        }

        return [.. data.Missed];
    }

    /// <summary>Says on stderr which overrides of an instance found no node.</summary>
    /// <remarks>
    /// An override reaching into an instance nested in this one finds nothing until the nested one
    /// is spawned, and is applied again then, so a path missed while one of its nested instances
    /// is still loading is not reported.
    /// </remarks>
    private static void Report(EcsWorld world, Entity root, IReadOnlyList<string> missed)
    {
        var waiting = missed.Where(at => !Waiting(world, root, at)).Distinct().ToArray();
        if (waiting.Length == 0) return;

        Console.Error.WriteLine(
            $"[scene] {SceneOf(world, root)} has no node at {string.Join(", ", waiting)}, "
            + "so the overrides there were kept and not applied");
    }

    /// <summary>Whether a path stops at an instance nested in this one that has not spawned its scene yet.</summary>
    private static bool Waiting(EcsWorld world, Entity root, string path)
    {
        var at = root;
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = world.ChildrenOf(at).FirstOrDefault(child => Step(world, child) == part);
            if (next.IsNone) return at != root && IsInstance(world, at) && world.ChildrenOf(at).Length == 0;
            at = next;
        }

        return false;
    }

    /// <summary>
    /// Takes what Bevy reported ready since the last frame, applies the overrides of each instance
    /// among them, and posts each one.
    /// </summary>
    internal static unsafe void PostReady(EcsWorld world, MessageBus bus)
    {
        const int Batch = 64;
        var bits = stackalloc ulong[Batch];

        int read;
        do
        {
            read = Interop.Native.bcs_world_instances_ready(bits, Batch);
            for (var i = 0; i < read; i++) Ready(world, bus, new Entity(bits[i]), apply: true);
        }
        while (read == Batch);

        // Subscenes were applied as they were read, and are only announced here.
        Entity[] placed;
        lock (Pending)
        {
            placed = [.. Pending];
            Pending.Clear();
        }

        foreach (var root in placed)
            if (world.IsAlive(root)) Ready(world, bus, root, apply: false);
    }

    /// <summary>
    /// Applies a ready instance's overrides, then those of every instance it is nested in that
    /// were waiting for it, and posts it.
    /// </summary>
    private static void Ready(EcsWorld world, MessageBus bus, Entity entity, bool apply)
    {
        if (apply && IsInstance(world, entity)) Report(world, entity, Apply(world, entity));

        // An override reaching into this instance from one it is nested in found nothing while it
        // was loading, and finds its node now.
        for (var outer = world.ParentOf(entity); !outer.IsNone; outer = world.ParentOf(outer))
        {
            if (DataOf(world, outer) is { Missed.Count: > 0 }) Report(world, outer, Apply(world, outer, retry: true));
        }

        bus.Send(new WorldInstanceReady(entity));
    }

    /// <summary>Puts a recorded instance on an entity, as a scene file read it.</summary>
    internal static void Attach(EcsWorld world, Entity root, string path, IEnumerable<InstanceOverride> overrides)
    {
        var data = new Data { Path = path };
        data.Overrides.AddRange(overrides);
        world.Add(root, SceneInstance.Of(data));
    }

    /// <summary>What an entity's instance holds, or nothing when it is not an instance.</summary>
    internal static Data? DataOf(EcsWorld world, Entity root)
    {
        ArgumentNullException.ThrowIfNull(world);
        return IsInstance(world, root) ? world.GetRef<SceneInstance>(root).Value : null;
    }

    /// <summary>
    /// Takes note of what the scene spawned under an instance, leaving out the children the scene
    /// placing it put there.
    /// </summary>
    private static void Tag(EcsWorld world, Entity root, Data data)
    {
        var added = data.Overrides
            .Where(change => change.Kind == OverrideKind.Child)
            .Select(change => ChildOf(change))
            .Where(child => !child.IsNone)
            .ToHashSet();

        void Walk(Entity at)
        {
            foreach (var child in world.ChildrenOf(at))
            {
                if (added.Contains(child)) continue;

                data.Model.Add(child);
                RememberName(world, child);
                Walk(child);
            }
        }

        Walk(root);
        data.Tagged = true;
    }

    /// <summary>The entity a child override puts under its node, or none while it is still a file id.</summary>
    private static Entity ChildOf(InstanceOverride change) =>
        change.Fields is { } bits && ulong.TryParse(bits, out var value) ? new Entity(value) : Entity.None;

    /// <summary>
    /// Whether an entity was spawned from the scene of an instance it is under, rather than added
    /// by the scene the instance is placed in.
    /// </summary>
    /// <remarks>
    /// Every instance above it is asked, since a node of a model nested in a subscene was spawned
    /// by the nested instance and not the outer one.
    /// </remarks>
    public static bool IsFromModel(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        for (var at = world.ParentOf(entity); !at.IsNone; at = world.ParentOf(at))
            if (DataOf(world, at) is { } data && data.Model.Contains(entity)) return true;

        return false;
    }

    /// <summary>
    /// Turns the file ids an instance's child overrides hold into the entities they spawned as, and
    /// puts them under their nodes when the instance has spawned its scene already.
    /// </summary>
    internal static void ResolveChildren(EcsWorld world, Entity root, IReadOnlyDictionary<int, Entity> byId)
    {
        if (DataOf(world, root) is not { } data) return;

        var resolved = false;
        for (var i = 0; i < data.Overrides.Count; i++)
        {
            var change = data.Overrides[i];
            if (change.Kind != OverrideKind.Child || change.Fields is not ['#', .. var number]) continue;
            if (!int.TryParse(number, out var id) || !byId.TryGetValue(id, out var child)) continue;

            data.Overrides[i] = change with { Fields = child.Bits.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            data.Model.Remove(child);
            resolved = true;
        }

        // A subscene has spawned already, and its children go under their nodes now. A glTF scene
        // has not, and its overrides, these among them, are applied when it has.
        if (resolved && data.Tagged)
        {
            foreach (var change in data.Overrides.Where(change => change.Kind == OverrideKind.Child))
            {
                var node = Find(world, root, change.At);
                var child = ChildOf(change);
                if (!node.IsNone && !child.IsNone && world.IsAlive(child)) world.SetParent(child, node);
            }
        }
    }

    /// <summary>Applies one override to the node it found, reporting whether it could.</summary>
    private static bool Applied(EcsWorld world, Entity node, InstanceOverride change)
    {
        if (change.Kind == OverrideKind.Rename)
        {
            if (change.Fields is not { Length: > 0 } name) return false;

            world.SetName(node, name);
            return true;
        }

        if (change.Kind == OverrideKind.Child)
        {
            // One still holding a file id is put in place once the file's entities are spawned.
            var child = ChildOf(change);
            if (child.IsNone) return true;
            if (!world.IsAlive(child)) return false;

            world.SetParent(child, node);
            return true;
        }

        if (change.Kind == OverrideKind.Delete)
        {
            world.Despawn(node);
            return true;
        }

        if (change.Component is null || ComponentSchemas.For(change.Component) is not { } schema) return false;

        if (change.Kind == OverrideKind.Remove)
        {
            if (schema.Origin == SchemaOrigin.Reflected) world.RemoveReflected(node, schema.QualifiedName);
            else schema.Remove(world, node);
            return true;
        }

        using var document = JsonDocument.Parse(change.Fields ?? "{}");

        if (schema.Origin == SchemaOrigin.Reflected)
        {
            // Bevy's own take a whole value, so what the node holds is read, the named fields laid
            // over it, and the result put back, which leaves the fields not named as they were.
            var held = world.GetReflected(node, schema.QualifiedName) is { } text ? JsonNode.Parse(text) as JsonObject : null;
            var merged = held ?? new JsonObject();
            Merge(merged, JsonNode.Parse(change.Fields ?? "{}")!.AsObject());

            try
            {
                world.InsertReflected(node, schema.QualifiedName, merged.ToJsonString());
                return true;
            }
            catch (Interop.BevyNativeException)
            {
                return false;
            }
        }

        if (!world.ComponentsOf(node).Contains(schema.Id))
        {
            if (!schema.Add(world, node)) return false;
        }
        else if (change.Kind == OverrideKind.Set && Locate(world, node) is var (data, at))
        {
            // What the model holds in each field about to be overwritten, remembered for a revert,
            // unless an earlier application in this run remembered it first.
            foreach (var field in schema.Fields)
            {
                var key = Data.Key(at, schema.QualifiedName, field.Name);
                if (data.Originals.ContainsKey(key) || !Holds(change, field.Name)) continue;
                if (field.Read(world, node) is { } held && Json(json => SceneValue.Write(json, field, held)) is { } original)
                    data.Originals[key] = original;
            }
        }

        SceneValue.ReadComponent(document.RootElement, schema, world, node);
        return true;
    }

    /// <summary>Whether an instance's overrides set a field of a component on a node.</summary>
    private static bool Holds(Data data, string at, string component, string field) =>
        data.Overrides.Any(change =>
            change.At == at
            && change.Kind is OverrideKind.Set or OverrideKind.Add
            && change.Component is { } named
            && (named == component || ComponentSchemas.For(named)?.QualifiedName == component)
            && Holds(change, field));

    /// <summary>Whether an override's fields name a field, by its dotted path.</summary>
    private static bool Holds(InstanceOverride change, string field)
    {
        JsonNode? at = JsonNode.Parse(change.Fields ?? "{}");
        foreach (var part in field.Split('.'))
        {
            if (at is not JsonObject inner || !inner.TryGetPropertyValue(part, out at)) return false;
        }

        return true;
    }

    /// <summary>
    /// Takes a field out of the override that sets it, and the override itself once a set names no
    /// field, since a set that changes nothing is not a change.
    /// </summary>
    private static void Unset(Data data, string at, string component, string field)
    {
        var index = data.Overrides.FindIndex(change =>
            change.At == at && change.Component == component && change.Kind is OverrideKind.Set or OverrideKind.Add);
        if (index < 0) return;

        var change = data.Overrides[index];
        var fields = JsonNode.Parse(change.Fields ?? "{}")!.AsObject();

        // Down to the field, keeping each object on the way so an emptied one can be taken too.
        var parts = field.Split('.');
        var trail = new List<JsonObject> { fields };
        foreach (var part in parts[..^1])
        {
            if (trail[^1][part] is not JsonObject next) return;
            trail.Add(next);
        }

        trail[^1].Remove(parts[^1]);
        for (var i = trail.Count - 1; i > 0 && trail[i].Count == 0; i--) trail[i - 1].Remove(parts[i - 1]);

        if (fields.Count == 0 && change.Kind == OverrideKind.Set) data.Overrides.RemoveAt(index);
        else data.Overrides[index] = change with { Fields = fields.ToJsonString() };
    }

    /// <summary>Records an override, folding it into an earlier one for the same node and component.</summary>
    /// <remarks>
    /// A set after a set merges fields, a set after an add merges into the add, and a remove or a
    /// delete replaces what was recorded for what it takes away, so the list says what the
    /// instance ends up with rather than every step taken to get there.
    /// </remarks>
    private static void Record(Data data, InstanceOverride change)
    {
        var overrides = data.Overrides;

        if (change.Kind == OverrideKind.Delete)
        {
            overrides.RemoveAll(earlier => earlier.At == change.At || earlier.At.StartsWith(change.At + "/", StringComparison.Ordinal));
            overrides.Add(change);
            return;
        }

        var index = overrides.FindIndex(earlier => earlier.At == change.At && earlier.Component == change.Component);
        if (index < 0)
        {
            overrides.Add(change);
            return;
        }

        var earlier = overrides[index];
        if (change.Kind == OverrideKind.Set && earlier.Kind is OverrideKind.Set or OverrideKind.Add)
        {
            var fields = JsonNode.Parse(earlier.Fields ?? "{}")!.AsObject();
            Merge(fields, JsonNode.Parse(change.Fields ?? "{}")!.AsObject());
            overrides[index] = earlier with { Fields = fields.ToJsonString() };
            return;
        }

        // A remove after an add of a component the model never had leaves nothing to record.
        if (change.Kind == OverrideKind.Remove && earlier.Kind == OverrideKind.Add)
        {
            overrides.RemoveAt(index);
            return;
        }

        overrides[index] = change;
    }

    /// <summary>The instance a node is in and its path there, or nothing for a node in none.</summary>
    private static (Data Data, string At)? Locate(EcsWorld world, Entity node)
    {
        ArgumentNullException.ThrowIfNull(world);

        // The root's own components are the instance's, written with it like any entity's, so the
        // root is not one of the nodes an override addresses. The outermost instance, because the
        // ones nested in it are spawned from its scene and not written, so a change inside one is
        // kept by the instance that is.
        // An entity the scene placing the instance added is that scene's own, written with it.
        var root = OuterRootOf(world, node);
        if (root == node || !IsFromModel(world, node)) return null;
        if (DataOf(world, root) is not { } data || PathOf(world, root, node) is not { } at) return null;
        return (data, at);
    }

    /// <summary>
    /// What the model called each node it spawned, kept so a path names a renamed node as the
    /// model does, by entity, for the app the entities belong to.
    /// </summary>
    private static readonly Dictionary<Entity, string?> ModelNames = [];

    private static int _namesGeneration = -1;

    /// <summary>A node's name as the model gave it, or its own name for an entity the model did not spawn.</summary>
    private static string? NameOf(EcsWorld world, Entity node)
    {
        lock (ModelNames)
        {
            if (_namesGeneration != ComponentRegistry.Generation)
            {
                // Entities belong to a world, so another app's names say nothing about this one's.
                ModelNames.Clear();
                _namesGeneration = ComponentRegistry.Generation;
            }

            return ModelNames.TryGetValue(node, out var name) ? name : world.NameOf(node);
        }
    }

    /// <summary>Remembers what the model called a node it spawned.</summary>
    private static void RememberName(EcsWorld world, Entity node)
    {
        _ = NameOf(world, node);
        lock (ModelNames) ModelNames.TryAdd(node, world.NameOf(node));
    }

    /// <summary>One step of a path: a node's name, numbered after the earlier siblings sharing it.</summary>
    private static string Step(EcsWorld world, Entity node)
    {
        var siblings = world.ChildrenOf(world.ParentOf(node));
        var name = NameOf(world, node);

        if (name is null)
        {
            var place = Array.IndexOf(siblings, node) + 1;
            return "#" + place;
        }

        var same = 0;
        foreach (var sibling in siblings)
        {
            if (NameOf(world, sibling) == name) same++;
            if (sibling == node) break;
        }

        return same > 1 ? $"{name}#{same}" : name;
    }

    /// <summary>Lays one object's values over another's, going into objects both have.</summary>
    private static void Merge(JsonObject into, JsonObject from)
    {
        foreach (var (name, value) in from.ToArray())
        {
            if (value is JsonObject inner && into[name] is JsonObject existing)
            {
                Merge(existing, inner);
                continue;
            }

            into[name] = value?.DeepClone();
        }
    }

    /// <summary>Puts a value at a dotted name inside nested objects, making the objects on the way.</summary>
    private static void Place(JsonObject into, string name, JsonNode? value)
    {
        var parts = name.Split('.');
        var at = into;
        foreach (var part in parts[..^1])
        {
            if (at[part] is not JsonObject next)
            {
                next = new JsonObject();
                at[part] = next;
            }

            at = next;
        }

        at[parts[^1]] = value;
    }

    /// <summary>What a writer writes, as text, or nothing when it reports it wrote nothing.</summary>
    private static string? Json(Func<Utf8JsonWriter, bool> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        bool wrote;
        using (var json = new Utf8JsonWriter(buffer)) wrote = write(json);
        return wrote ? Encoding.UTF8.GetString(buffer.WrittenSpan) : null;
    }
}

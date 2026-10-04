using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Marks an entity a save game keeps track of, by an id that stays the same between runs.
/// </summary>
/// <remarks>
/// Set in the editor, where adding it gives a random one, or in code with <see cref="New"/> when
/// something is spawned during play that a save should keep. An entity without one is the scene's
/// alone, and loading a save leaves it as the scene has it.
/// </remarks>
public struct SaveId : IEquatable<SaveId>
{
    /// <summary>The id, never zero for one that was given.</summary>
    public ulong Value;

    /// <summary>A random id, which two entities are as good as certain never to share.</summary>
    public static SaveId New()
    {
        Span<byte> bytes = stackalloc byte[8];
        ulong value;
        do
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            value = BitConverter.ToUInt64(bytes);
        }
        while (value == 0);

        return new SaveId { Value = value };
    }

    /// <summary>An id written as sixteen hex digits, or nothing when the text is not one.</summary>
    public static SaveId? Parse(string text) =>
        ulong.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) && value != 0
            ? new SaveId { Value = value }
            : null;

    /// <summary>The id as sixteen hex digits.</summary>
    public override readonly string ToString() => Value.ToString("x16", CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public readonly bool Equals(SaveId other) => Value == other.Value;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is SaveId other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => Value.GetHashCode();

    /// <summary>Whether two ids are the same.</summary>
    public static bool operator ==(SaveId left, SaveId right) => left.Equals(right);

    /// <summary>Whether two ids differ.</summary>
    public static bool operator !=(SaveId left, SaveId right) => !left.Equals(right);
}

/// <summary>
/// Puts a component's fields in a save game.
/// </summary>
/// <remarks>
/// A save holds what changed while playing, so it writes only what is marked, the fields of a
/// component with this attribute, on an entity with a <see cref="SaveId"/>. A level's walls and
/// lights stay the scene's, so a save is small and a level fixed in an update reaches players with
/// saves from before it.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class PersistAttribute : Attribute;

/// <summary>
/// Sent the frame after <see cref="SaveGame.Load"/> brought a save back, for a game to build what a
/// save does not hold.
/// </summary>
/// <param name="Path">The save that was loaded, as <see cref="SaveGame.Load"/> was given it.</param>
/// <param name="Entities">Everything the load spawned, the scenes' and the save's.</param>
/// <remarks>
/// <para>
/// A load ends the game in progress and spawns its scenes again with the save laid over them, and
/// enters no state, so what a game builds on entering one, such as the player's body or a camera
/// following it, is not built for what came back. A system reading this builds it once, where it
/// reads it, rather than looking every frame for what is missing.
/// </para>
/// <para>
/// The frame after rather than the frame of the load, as every message the engine sends arrives at
/// the top of a frame, so every system of that frame reads it and none reads it twice.
/// </para>
/// </remarks>
public readonly record struct SaveLoaded(string Path, IReadOnlyList<Entity> Entities);

/// <summary>
/// Saves a game as what changed over the scenes it started from, and loads it back.
/// </summary>
/// <remarks>
/// <para>
/// A save names the scenes a game was started from with <see cref="Start"/> and holds only what
/// differs from them, keyed by <see cref="SaveId"/>:
/// </para>
/// <code>
/// { "format": "bevycsharp.save.1",
///   "scenes": [ { "uid": "…", "path": "levels/one.scene.json" } ],
///   "entities": [
///     { "save": "1f…", "components": { "Game.Health": { "Current": 40 } } },
///     { "save": "9a…", "spawned": true, "name": "Arrow", "components": { … } },
///     { "save": "c3…", "deleted": true } ],
///   "values": { "quests": { … } } }
/// </code>
/// <para>
/// An entity the scenes hold has the components marked <see cref="PersistAttribute"/>, or named in
/// <see cref="Persisted"/>, written. One spawned during play with a <see cref="SaveId"/> is written
/// whole, as a scene writes an entity, and one of the scenes' that has been despawned is written as
/// deleted. Loading reads the scenes again, then lays the save over them, so a scene's content
/// comes from the scene and only the player's progress from the save.
/// </para>
/// <para>
/// A load ends the game in progress first, despawning what its scenes spawned and every entity
/// with a <see cref="SaveId"/>, so a game loads from its pause menu as from its title screen. What
/// the game spawned itself without an id, such as its camera or its interface, is left for the
/// game, which knows whether it still belongs.
/// </para>
/// <para>
/// A <see cref="Persistent{T}"/> given to <see cref="Carry"/> belongs to the save slot, and is
/// copied into each save under its name and put back when one is loaded.
/// </para>
/// <para>
/// A field referring to another entity is written as that entity's save id, so it refers to the
/// same one after a load whether that entity came from a scene or was spawned. A save is written
/// through a temporary file renamed over the old one, and by default under <c>user://saves/</c>.
/// </para>
/// </remarks>
public static class SaveGame
{
    /// <summary>The format a save file says it is in.</summary>
    public const string Format = "bevycsharp.save.1";

    /// <summary>
    /// Components a save writes by their full name, besides the ones marked
    /// <see cref="PersistAttribute"/>, for one declared elsewhere such as <c>Bevy.Transform</c>.
    /// </summary>
    public static readonly HashSet<string> Persisted = new(StringComparer.Ordinal);

    /// <summary>
    /// Persistent values that belong to the save slot rather than to the player, such as a
    /// quest's flags, by name.
    /// </summary>
    private static readonly Dictionary<string, IPersistentValue> Carried = new(StringComparer.Ordinal);

    /// <summary>
    /// Makes a persistent value part of every save, copied into the save and put back when the
    /// save is loaded.
    /// </summary>
    /// <remarks>
    /// For state that belongs to one playthrough and not to the player, so loading an earlier save
    /// takes it back with the world. Settings and anything shared across saves are left out, and
    /// stay in their own files. A value carried under a name another already has replaces it.
    /// </remarks>
    public static void Carry(IPersistentValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Carried[value.Name] = value;
    }

    /// <summary>Stops carrying a persistent value in saves.</summary>
    public static void Drop(IPersistentValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Carried.TryGetValue(value.Name, out var held) && ReferenceEquals(held, value)) Carried.Remove(value.Name);
    }

    private static readonly List<string> StartedFrom = [];
    private static readonly HashSet<SaveId> FromScenes = [];

    /// <summary>What the scenes of the game in progress spawned, which a load despawns.</summary>
    private static readonly List<Entity> Spawned = [];

    /// <summary>The scenes the game in progress was started from, which a save names.</summary>
    public static IReadOnlyList<string> Scenes => [.. StartedFrom];

    /// <summary>
    /// Starts a game from scenes, and remembers which saved entities they hold, so a save can tell
    /// what play changed.
    /// </summary>
    /// <param name="world">The world to spawn into.</param>
    /// <param name="scenes">The scene files, as <see cref="SceneFile.Load"/> takes them.</param>
    /// <returns>Everything spawned, in the order of the scenes.</returns>
    public static IReadOnlyList<Entity> Start(EcsWorld world, params string[] scenes)
    {
        ArgumentNullException.ThrowIfNull(world);

        var spawned = new List<Entity>();
        foreach (var scene in scenes) spawned.AddRange(SceneFile.Load(world, scene).Entities);

        Begin(world, scenes);
        Spawned.AddRange(spawned);
        return spawned;
    }

    /// <summary>
    /// Starts a game from scenes already loaded, remembering them and the saved entities they hold,
    /// as <see cref="Start"/> does after it loads them.
    /// </summary>
    /// <remarks>
    /// For a host that loads the scenes itself and reports what each held, as the player does when
    /// the editor plays a scene, so a save made there lays itself over the same scene a game's
    /// would.
    /// </remarks>
    /// <param name="world">The world they were loaded into.</param>
    /// <param name="scenes">The scene files, as they were loaded.</param>
    public static void Begin(EcsWorld world, params string[] scenes)
    {
        ArgumentNullException.ThrowIfNull(world);

        StartedFrom.Clear();
        StartedFrom.AddRange(scenes);

        // Everything loaded from a scene, which covers what a host loading them itself spawned,
        // since a scene marks each entity it spawns with the id it had in the file.
        Spawned.Clear();
        Spawned.AddRange(world.All().Where(entity => world.Has<SceneId>(entity)));

        // Every saved entity there is once the scenes are in, including the ones inside a subscene.
        FromScenes.Clear();
        foreach (var (_, id) in Ids(world)) FromScenes.Add(id);
    }

    /// <summary>Writes what play changed over the scenes the game started from.</summary>
    /// <param name="world">The world being played.</param>
    /// <param name="path">Where, as a <c>user://</c> path, a scene path, or an absolute one.</param>
    /// <returns>How many entities were written.</returns>
    public static int Save(EcsWorld world, string path = "user://saves/slot.save.json")
    {
        ArgumentNullException.ThrowIfNull(world);

        var ids = Ids(world);
        var references = new SaveReferences(ids);

        var buffer = new ArrayBufferWriter<byte>();
        var written = 0;
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("format", Format);

            json.WriteStartArray("scenes");
            foreach (var scene in StartedFrom) references.WriteFile(json, scene);
            json.WriteEndArray();

            json.WriteStartArray("entities");
            foreach (var (entity, id) in ids)
            {
                json.WriteStartObject();
                json.WriteString("save", id.ToString());

                if (FromScenes.Contains(id))
                {
                    json.WritePropertyName("components");
                    SceneFile.WriteComponents(world, entity, json, references, IsPersisted);
                }
                else
                {
                    // Spawned during play, so the save is the only place it is described.
                    json.WriteBoolean("spawned", true);
                    if (world.NameOf(entity) is { } name) json.WriteString("name", name);

                    var parent = world.ParentOf(entity);
                    if (!parent.IsNone && world.TryGet<SaveId>(parent, out var above))
                        json.WriteString("parent", above.ToString());

                    json.WritePropertyName("components");
                    SceneFile.WriteComponents(world, entity, json, references);
                }

                json.WriteEndObject();
                written++;
            }

            // What the scenes hold that play took away.
            var present = ids.Select(pair => pair.Id).ToHashSet();
            foreach (var gone in FromScenes.Where(id => !present.Contains(id)).OrderBy(id => id.Value))
            {
                json.WriteStartObject();
                json.WriteString("save", gone.ToString());
                json.WriteBoolean("deleted", true);
                json.WriteEndObject();
                written++;
            }

            json.WriteEndArray();

            if (Carried.Count > 0)
            {
                json.WriteStartObject("values");
                foreach (var (name, value) in Carried.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    json.WritePropertyName(name);
                    value.Write(json);
                }

                json.WriteEndObject();
            }

            json.WriteEndObject();
        }

        UserData.WriteAtomically(SceneFile.Resolve(path), buffer.WrittenSpan);
        return written;
    }

    /// <summary>
    /// Loads the scenes a save names and lays the save over them.
    /// </summary>
    /// <param name="world">The world to spawn into, with the game in progress in it or none.</param>
    /// <param name="path">Where the save is, as <see cref="Save"/> takes it.</param>
    /// <returns>What loading the scenes and the save could not read.</returns>
    /// <exception cref="InvalidDataException">The file is not a save in this format.</exception>
    /// <remarks>
    /// The game in progress is ended only once the file has been read as a save, so a missing or
    /// broken one leaves the game being played as it was. <see cref="SaveLoaded"/> is sent the frame
    /// after, for what the game builds itself.
    /// </remarks>
    public static SceneLoad Load(EcsWorld world, string path = "user://saves/slot.save.json")
    {
        ArgumentNullException.ThrowIfNull(world);

        using var document = JsonDocument.Parse(AssetFiles.ReadAllText(SceneFile.Resolve(path)));
        var root = document.RootElement;
        if (!root.TryGetProperty("format", out var format) || format.GetString() != Format)
            throw new InvalidDataException($"Not a save in the {Format} format.");

        End(world);

        // The scenes first, by id where a scene file was renamed since the save was written.
        var scenes = root.TryGetProperty("scenes", out var named)
            ? named.EnumerateArray().Select(SceneReferences.ReadFile).OfType<string>().ToArray()
            : [];
        var spawned = new List<Entity>(Start(world, scenes));

        var entries = root.TryGetProperty("entities", out var listed) ? listed.EnumerateArray().ToArray() : [];
        var byId = Ids(world).ToDictionary(pair => pair.Id, pair => pair.Entity);

        // Spawned ones made first, so a component referring to one finds it whatever the order.
        foreach (var entry in entries)
        {
            if (!Flag(entry, "spawned") || Id(entry) is not { } id) continue;

            var entity = world.Spawn();
            world.Add(entity, id);
            if (entry.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } called)
                world.SetName(entity, called);

            byId[id] = entity;
            spawned.Add(entity);
        }

        var references = new SaveReferences(byId.Select(pair => (pair.Value, pair.Key)).ToList());
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        var refused = new List<string>();

        foreach (var entry in entries)
        {
            if (Id(entry) is not { } id || !byId.TryGetValue(id, out var entity)) continue;

            if (Flag(entry, "deleted"))
            {
                world.Despawn(entity);
                continue;
            }

            if (entry.TryGetProperty("parent", out var parentText)
                && parentText.GetString() is { } parentId
                && SaveId.Parse(parentId) is { } above
                && byId.TryGetValue(above, out var parent))
                world.SetParent(entity, parent);

            if (entry.TryGetProperty("components", out var components))
                SceneFile.ReadComponents(world, entity, components, references, unknown, refused);
        }

        // The values the slot carries, put back as they were saved. One the save has and nothing
        // carries is reported, and one carried that the save does not have is left as it is.
        if (root.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Object)
        {
            foreach (var value in values.EnumerateObject())
            {
                if (!Carried.TryGetValue(value.Name, out var carried)) unknown.Add(value.Name);
                else if (!carried.Read(value.Value)) refused.Add($"{value.Name}: the saved value could not be read.");
            }
        }

        var load = new SceneLoad([.. spawned.Where(world.IsAlive)], [.. unknown], refused);
        Loaded.Enqueue(new SaveLoaded(path, load.Entities));
        return load;
    }

    /// <summary>Forgets the game in progress, for an app starting, whose entities are its own.</summary>
    internal static void Forget()
    {
        StartedFrom.Clear();
        FromScenes.Clear();
        Spawned.Clear();
        Loaded.Clear();
    }

    /// <summary>The loads made since the last frame began, for the bus.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentQueue<SaveLoaded> Loaded = new();

    /// <summary>Sends the loads made since the last frame onto the bus. Called by the app each frame.</summary>
    internal static void PostLoaded(MessageBus bus)
    {
        while (Loaded.TryDequeue(out var loaded)) bus.Send(loaded);
    }

    /// <summary>Despawns the game in progress, its scenes' entities and every one with a save id.</summary>
    /// <remarks>
    /// The ones with an id as well, since a save writes an entity spawned during play whole and a
    /// load spawns it again, so one left standing would be there twice.
    /// </remarks>
    private static void End(EcsWorld world)
    {
        var going = new HashSet<Entity>(Spawned);
        foreach (var (entity, _) in Ids(world)) going.Add(entity);

        foreach (var entity in going)
        {
            if (world.IsAlive(entity)) world.Despawn(entity);
        }

        Spawned.Clear();
    }

    /// <summary>Whether a save writes a component of an entity the scenes hold.</summary>
    private static bool IsPersisted(ComponentSchema schema) =>
        schema.Persisted || Persisted.Contains(schema.QualifiedName) || Persisted.Contains(schema.Name);

    /// <summary>Every entity with a save id, in id order so a save written twice reads the same.</summary>
    private static List<(Entity Entity, SaveId Id)> Ids(EcsWorld world) =>
        [.. world.All()
            .Where(entity => world.Has<SaveId>(entity))
            .Select(entity => (entity, world.GetRef<SaveId>(entity)))
            .OrderBy(pair => pair.Item2.Value)];

    private static SaveId? Id(JsonElement entry) =>
        entry.TryGetProperty("save", out var id) && id.GetString() is { } text ? SaveId.Parse(text) : null;

    private static bool Flag(JsonElement entry, string name) =>
        entry.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>Names an entity in a save by its save id, which stays the same between runs.</summary>
    private sealed class SaveReferences : SceneReferences
    {
        private readonly Dictionary<Entity, SaveId> _ids = [];
        private readonly Dictionary<SaveId, Entity> _entities = [];

        public SaveReferences(IEnumerable<(Entity Entity, SaveId Id)> ids)
        {
            foreach (var (entity, id) in ids)
            {
                _ids[entity] = id;
                _entities[id] = entity;
            }
        }

        public override bool WriteEntity(Utf8JsonWriter json, Entity entity)
        {
            if (!_ids.TryGetValue(entity, out var id)) return false;

            json.WriteStartObject();
            json.WriteString("save", id.ToString());
            json.WriteEndObject();
            return true;
        }

        public override object? ReadEntity(JsonElement json) =>
            json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty("save", out var id)
            && id.GetString() is { } text
            && SaveId.Parse(text) is { } parsed
            && _entities.TryGetValue(parsed, out var entity)
                ? entity
                : null;
    }
}

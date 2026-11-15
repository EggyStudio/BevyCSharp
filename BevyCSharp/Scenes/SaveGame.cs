using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace Bevy;

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
/// An entity the scenes hold has the fields play changed written, of the components marked
/// <see cref="PersistAttribute"/> or named in <see cref="Persisted"/>, with a component play took
/// off written as <c>null</c> and one play put on written whole. One spawned during play with a
/// <see cref="SaveId"/> is written whole, as a scene writes an entity, and one of the scenes' that
/// has been despawned is written as deleted. Loading reads the scenes again, then lays the save
/// over them, so a scene's content comes from the scene and only the player's progress from the
/// save, and a field of a level fixed in an update reaches a save from before it unless play
/// changed that field.
/// </para>
/// <para>
/// A load ends the game in progress first, despawning what its scenes spawned and every entity
/// with a <see cref="SaveId"/>, so a game loads from its pause menu as from its title screen. What
/// the game spawned itself without an id, such as its camera or its interface, is left for the
/// game, which knows whether it still belongs, and so is a scene the game loaded itself and did
/// not start the game from, such as its interface's.
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

    /// <summary>
    /// What each saved entity of the scenes held as they were loaded, as a save writes its
    /// components, which a save compares with to write only what play changed.
    /// </summary>
    private static readonly Dictionary<SaveId, JsonElement> AsLoaded = [];

    /// <summary>What the scenes of the game in progress spawned, which a load despawns.</summary>
    private static readonly List<Entity> Spawned = [];

    /// <summary>
    /// What each scene file loaded by a call of the game's own spawned, by the file, which
    /// <see cref="Begin"/> takes the game's entities from, the loads whose entities have all gone
    /// passed over.
    /// </summary>
    private static readonly List<(string File, Entity[] Entities)> Loads = [];

    /// <summary>
    /// The entries of the save loaded last for entities no scene holds, by their ids, as the save
    /// had them, which the next save writes back.
    /// </summary>
    /// <remarks>
    /// A scene the save names that could not be read holds the entities these were for, so a save
    /// made while it is missing keeps the player's progress in it for a load that reads it again.
    /// An entity an update took out of its scene leaves one here as well, which costs a save a few
    /// lines and finds the entity again should it come back.
    /// </remarks>
    private static readonly Dictionary<SaveId, JsonElement> Unplaced = [];

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
        return Open(world, scenes).Entities;
    }

    /// <summary>
    /// Loads the scenes and starts the game from them, with what each load could not read, every
    /// scene's problem said in one, naming its file.
    /// </summary>
    private static SceneLoad Open(EcsWorld world, string[] scenes)
    {
        var spawned = new List<Entity>();
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        var refused = new List<string>();
        var problems = new List<string>();
        foreach (var scene in scenes)
        {
            var load = SceneFile.Load(world, scene);
            spawned.AddRange(load.Entities);
            unknown.UnionWith(load.Unknown);
            refused.AddRange(load.Refused);
            if (load.Problem is { } problem) problems.Add(problem);
        }

        Begin(world, scenes);
        return new SceneLoad(spawned, [.. unknown], refused, problems.Count > 0 ? string.Join(" ", problems) : null);
    }

    /// <summary>
    /// Keeps what a load of a scene file by a call of the game's own spawned, called by
    /// <see cref="SceneFile.Load"/>, for <see cref="Begin"/> to find the game's entities among.
    /// </summary>
    internal static void Noted(EcsWorld world, string path, IReadOnlyList<Entity> spawned)
    {
        Loads.RemoveAll(each => !each.Entities.Any(world.IsAlive));
        Loads.Add((FileOf(path), [.. spawned]));
    }

    /// <summary>A scene path as the file it names, so two ways of naming one file match.</summary>
    private static string FileOf(string path) => Path.GetFullPath(SceneFile.Resolve(path));

    /// <summary>
    /// Starts a game from scenes already loaded, remembering them and the saved entities they hold,
    /// as <see cref="Start"/> does after it loads them.
    /// </summary>
    /// <remarks>
    /// For a host that loads the scenes itself with <see cref="SceneFile.Load"/> and reports what
    /// each held, as the player does when the editor plays a scene, so a save made there lays
    /// itself over the same scene a game's would. The game's entities are the ones those loads of
    /// the named files spawned, so another scene the game loaded, such as its interface's, stays
    /// the game's own and a load leaves it standing.
    /// </remarks>
    /// <param name="world">The world they were loaded into.</param>
    /// <param name="scenes">The scene files, as they were loaded.</param>
    public static void Begin(EcsWorld world, params string[] scenes)
    {
        ArgumentNullException.ThrowIfNull(world);

        StartedFrom.Clear();
        StartedFrom.AddRange(scenes);
        Unplaced.Clear();

        // What the loads of the named files spawned, the scenes they place among it.
        var named = scenes.Select(FileOf).ToHashSet(StringComparer.Ordinal);
        Spawned.Clear();
        Spawned.AddRange(Loads.Where(each => named.Contains(each.File)).SelectMany(each => each.Entities).Where(world.IsAlive).Distinct());

        // Every saved entity there is once the scenes are in, including the ones inside a subscene,
        // with what it holds, written as a save writes it so the two compare value for value.
        FromScenes.Clear();
        AsLoaded.Clear();
        var ids = Kept(Ids(world));
        var references = new SaveReferences(ids);
        foreach (var (entity, id) in ids)
        {
            FromScenes.Add(id);
            AsLoaded[id] = Persistable(world, entity, references);
        }
    }

    /// <summary>Writes what play changed over the scenes the game started from.</summary>
    /// <param name="world">The world being played.</param>
    /// <param name="path">Where, as a <c>user://</c> path, a scene path, or an absolute one.</param>
    /// <returns>How many entities were written.</returns>
    public static int Save(EcsWorld world, string path = "user://saves/slot.save.json")
    {
        ArgumentNullException.ThrowIfNull(world);

        var ids = Kept(Ids(world));
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
                // What play changed on one of the scenes' entities, and nothing for one it left as
                // the scene has it, which is not written, since a load leaves it as the scene has
                // it either way.
                string? changed = null;
                if (FromScenes.Contains(id))
                {
                    var now = Persistable(world, entity, references);
                    changed = AsLoaded.TryGetValue(id, out var loaded) ? Changes(now, loaded) : now.GetRawText();
                    if (changed is null) continue;
                }

                json.WriteStartObject();
                json.WriteString("save", id.ToString());

                if (changed is not null)
                {
                    // Parsed and written again rather than written raw, so it is indented as the
                    // rest of the save is.
                    using var components = JsonDocument.Parse(changed);
                    json.WritePropertyName("components");
                    components.RootElement.WriteTo(json);
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

            // What the save loaded last had for entities no scene holds, as it had it.
            var present = ids.Select(pair => pair.Id).ToHashSet();
            foreach (var (_, entry) in Unplaced.Where(pair => !present.Contains(pair.Key)).OrderBy(pair => pair.Key.Value))
            {
                entry.WriteTo(json);
                written++;
            }

            // What the scenes hold that play took away.
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
    /// <returns>
    /// What loading the scenes and the save could not read, or, for a file that is missing, empty,
    /// cut short or not a save in this format, nothing and why, naming the file
    /// (<see cref="SceneLoad.Problem"/>).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The game in progress is ended only once the file has been read as a save, so a missing or
    /// broken one leaves the game being played as it was, and is said on the log as well for a game
    /// that does not look at what is returned. <see cref="SaveLoaded"/> is sent the frame after a
    /// save is laid, for what the game builds itself.
    /// </para>
    /// <para>
    /// A scene the save names that cannot be read is named in the problem returned, with the save
    /// laid over the scenes that could be, and the save's entries for that scene's entities are
    /// kept as they were, so the next save writes them back and the player's progress there comes
    /// back once the scene can be read again.
    /// </para>
    /// </remarks>
    public static SceneLoad Load(EcsWorld world, string path = "user://saves/slot.save.json")
    {
        ArgumentNullException.ThrowIfNull(world);

        using var document = AssetFiles.TryReadJson(SceneFile.Resolve(path), "a save", out var problem);
        if (document is null) return SceneLoad.Unread(problem!);

        var root = document.RootElement;
        if (!root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String || format.GetString() != Format)
            return SceneLoad.Unread($"{path} is not a save in the {Format} format.");

        End(world);

        // The scenes first, by id where a scene file was renamed since the save was written.
        var scenes = root.TryGetProperty("scenes", out var named)
            ? named.EnumerateArray().Select(SceneReferences.ReadFile).OfType<string>().ToArray()
            : [];
        var opened = Open(world, scenes);
        var spawned = new List<Entity>(opened.Entities);

        var entries = root.TryGetProperty("entities", out var listed) ? listed.EnumerateArray().ToArray() : [];
        var byId = Kept(Ids(world)).ToDictionary(pair => pair.Id, pair => pair.Entity);

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
        var unknown = new SortedSet<string>(opened.Unknown, StringComparer.Ordinal);
        var refused = new List<string>(opened.Refused);

        foreach (var entry in entries)
        {
            if (Id(entry) is not { } id) continue;
            if (!byId.TryGetValue(id, out var entity))
            {
                Unplaced[id] = entry.Clone();
                continue;
            }

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

        // The assets the scenes' fields named and the save replaced, which no field holds once it is laid.
        if (!SceneFile.KeepsAssets) FieldAssets.Release(world);

        var load = new SceneLoad([.. spawned.Where(world.IsAlive)], [.. unknown], refused, opened.Problem);
        Loaded.Enqueue(new SaveLoaded(path, load.Entities));
        return load;
    }

    /// <summary>Forgets the game in progress, for an app starting, whose entities are its own.</summary>
    internal static void Forget()
    {
        StartedFrom.Clear();
        FromScenes.Clear();
        AsLoaded.Clear();
        Spawned.Clear();
        Loads.Clear();
        Unplaced.Clear();
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

    /// <summary>The components of an entity a save writes, as the object a save holds them in.</summary>
    private static JsonElement Persistable(EcsWorld world, Entity entity, SceneReferences references)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer)) SceneFile.WriteComponents(world, entity, json, references, IsPersisted);

        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// The components that differ between what an entity holds and what it held as the scenes
    /// were loaded, as a components object, or nothing when none does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A component of the game's own is compared field by field, along the paths its schema names,
    /// and written with only the fields that differ, which a load lays over the scene's component
    /// and leaves the rest of it as the scene has it. A value no field reads is compared the same
    /// way, as one more field. A field holding an object, such as a reference to an asset, is
    /// compared and written whole, since half of one names nothing.
    /// </para>
    /// <para>
    /// One of Bevy's reflected components is compared and written whole, because Bevy reads its
    /// JSON as a whole value. A component play put on is written whole, and one it took off is
    /// written as <c>null</c>, which a load takes off the scene's entity.
    /// </para>
    /// </remarks>
    private static string? Changes(JsonElement now, JsonElement loaded)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var any = false;
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();

            foreach (var component in now.EnumerateObject())
            {
                if (!loaded.TryGetProperty(component.Name, out var was))
                {
                    component.WriteTo(json);
                    any = true;
                    continue;
                }

                if (JsonElement.DeepEquals(component.Value, was)) continue;

                var schema = ComponentSchemas.For(component.Name);
                if (schema is null or { Origin: SchemaOrigin.Reflected }
                    || component.Value.ValueKind != JsonValueKind.Object
                    || was.ValueKind != JsonValueKind.Object)
                {
                    component.WriteTo(json);
                    any = true;
                    continue;
                }

                json.WritePropertyName(component.Name);
                WriteChangedFields(json, component.Value, was, Nested(schema), string.Empty);
                any = true;
            }

            foreach (var component in loaded.EnumerateObject())
            {
                if (now.TryGetProperty(component.Name, out _)) continue;

                json.WriteNull(component.Name);
                any = true;
            }

            json.WriteEndObject();
        }

        return any ? System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan) : null;
    }

    /// <summary>
    /// Writes the properties of an object that differ from what it held, going into a property only
    /// where it is a struct the schema's fields lie under.
    /// </summary>
    /// <remarks>
    /// The version a component records is written with the fields, whether or not it changed, so a
    /// later build brings the fields up from the version they were written at.
    /// </remarks>
    private static void WriteChangedFields(Utf8JsonWriter json, JsonElement now, JsonElement was, HashSet<string> nested, string prefix)
    {
        json.WriteStartObject();
        foreach (var property in now.EnumerateObject())
        {
            var path = prefix + property.Name;

            if (path == SceneValue.VersionName)
            {
                property.WriteTo(json);
                continue;
            }

            if (!was.TryGetProperty(property.Name, out var before))
            {
                property.WriteTo(json);
                continue;
            }

            if (JsonElement.DeepEquals(property.Value, before)) continue;

            if (nested.Contains(path) && property.Value.ValueKind == JsonValueKind.Object && before.ValueKind == JsonValueKind.Object)
            {
                json.WritePropertyName(property.Name);
                WriteChangedFields(json, property.Value, before, nested, path + ".");
                continue;
            }

            property.WriteTo(json);
        }

        json.WriteEndObject();
    }

    /// <summary>The dotted paths of a schema's structs, the parts of its fields' names before the last.</summary>
    private static HashSet<string> Nested(ComponentSchema schema)
    {
        var nested = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in schema.Fields)
        {
            var parts = field.Name.Split('.');
            for (var i = 1; i < parts.Length; i++) nested.Add(string.Join('.', parts[..i]));
        }

        return nested;
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

    // One entity of each save id, the first, where a scene a game placed more than once while it
    // ran gave two the same, said on the log rather than ending a load after the game in progress
    // has ended or keeping one entity's state for both. A placing in a scene file gives each of its
    // entities an id of its own (SaveId).
    private static List<(Entity Entity, SaveId Id)> Kept(List<(Entity Entity, SaveId Id)> ids)
    {
        var seen = new HashSet<SaveId>();
        var kept = new List<(Entity Entity, SaveId Id)>(ids.Count);
        var repeated = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var pair in ids)
        {
            if (seen.Add(pair.Id)) kept.Add(pair);
            else repeated.Add(pair.Id.ToString());
        }

        if (repeated.Count > 0)
        {
            Log.Warn($"The save ids {string.Join(", ", repeated.Take(4))}{(repeated.Count > 4 ? " and more" : "")} are each held by more "
                + "than one entity, and the save keeps the first of each. A scene placed more than once while the game runs "
                + "gives its entities the same ids, where each placing in a scene file gives them ids of its own.");
        }

        return kept;
    }

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
            SharesAssets = true;
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

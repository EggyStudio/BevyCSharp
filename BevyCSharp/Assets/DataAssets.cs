using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Loads, caches and saves data assets, the files <see cref="DataAssetAttribute"/> types live in.
/// </summary>
/// <remarks>
/// <para>
/// A data asset is a JSON file, <c>*.data.json</c>, holding its type's name and its fields in the
/// form <see cref="SceneValue"/> writes a component in, so an asset is opened as the right type
/// without its extension saying which:
/// </para>
/// <code>
/// { "format": "bevycsharp.data.1", "type": "Game.WeaponStats",
///   "fields": { "Damage": 12, "Title": "Sword", "Tags": ["sharp"] } }
/// </code>
/// <para>
/// <see cref="Get{T}"/> loads a file once and hands every caller the same value, which is shared
/// between everything referring to it and is not any one caller's to change. Changing it is done
/// through the editor or <see cref="Save{T}"/>, which write the file. <see cref="Reload"/> drops a
/// cached value so the next <c>Get</c> reads the file again, and posts
/// <see cref="DataAssetChanged"/>.
/// </para>
/// </remarks>
public static class DataAssets
{
    private const string Format = "bevycsharp.data.1";

    /// <summary>What a data asset's file is called after its name.</summary>
    public const string Extension = ".data.json";

    private static readonly object Gate = new();
    private static readonly Dictionary<string, Kind> Kinds = [];
    private static readonly Dictionary<Type, Kind> KindsByType = [];

    // Apart from Kinds so the names a type gave up are not offered in a menu of types to make, and
    // a type that has since taken one of those names still wins.
    private static readonly Dictionary<string, Kind> Former = [];
    private static readonly Dictionary<ulong, Loaded> Cache = [];

    /// <summary>
    /// The defaults a reference whose file could not be read answers with, by its id and the type
    /// it was read as, until the file is read again.
    /// </summary>
    private static readonly Dictionary<(ulong Id, Type Type), object> Fallbacks = [];

    /// <summary>
    /// Ids written or reloaded since the bus was last told, because a save can happen with no world
    /// to send through, and the app drains them onto the bus at the start of the next frame.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentQueue<ulong> Changed = [];

    /// <summary>
    /// A world standing in for the one a component's field reads through, which a data asset's
    /// fields ignore, since they read the asset rather than an entity.
    /// </summary>
    private static readonly EcsWorld Detached = new();

    /// <summary>One registered data asset type.</summary>
    /// <param name="Name">Its full name, as the file records it.</param>
    /// <param name="Type">The type.</param>
    /// <param name="Create">Makes a value at the type's defaults, boxed.</param>
    /// <param name="Bind">Describes the fields of one boxed value.</param>
    private sealed record Kind(string Name, Type Type, Func<object> Create, Func<object, ComponentSchema> Bind);

    /// <summary>One loaded asset: its box, the schema bound to it, and its type.</summary>
    /// <remarks>
    /// <c>Unread</c> holds the values the file had that no field reads, written back by every save
    /// so a file outlives a build that does not know them, as a scene's do.
    /// </remarks>
    private sealed record Loaded(
        object Box, ComponentSchema Schema, Kind Kind, IReadOnlyList<KeyValuePair<string, string>>? Unread = null);

    /// <summary>Loaded assets changed through their fields and not yet written.</summary>
    private static readonly HashSet<ulong> Unsaved = [];

    /// <summary>Registers a data asset type. Called by generated module initializers.</summary>
    /// <param name="name">The type's full name, which files record.</param>
    /// <param name="create">Makes a value at its defaults.</param>
    /// <param name="fields">Describes the fields of a boxed value, reading and writing through the box.</param>
    /// <param name="formerNames">The full names the type had before, which an old file may still record.</param>
    /// <param name="version">The version its files are written at, or zero for none.</param>
    /// <param name="migrate">Brings an older file's fields up to that version, or nothing.</param>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Register<T>(
        string name,
        Func<T> create,
        Func<DataBox<T>, IReadOnlyList<ComponentField>> fields,
        IReadOnlyList<string>? formerNames = null,
        int version = 0,
        Func<int, System.Text.Json.Nodes.JsonObject, System.Text.Json.Nodes.JsonObject>? migrate = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var shortName = name[(name.LastIndexOf('.') + 1)..];
        var kind = new Kind(
            name,
            typeof(T),
            () => new DataBox<T> { Value = create() },
            box => new ComponentSchema(shortName, name, static () => -1, fields((DataBox<T>)box))
            {
                Version = version,
                Migrate = migrate,
            });

        lock (Gate)
        {
            Kinds[name] = kind;
            KindsByType[typeof(T)] = kind;
            foreach (var former in formerNames ?? []) Former[former] = kind;
        }
    }

    /// <summary>The type a file's recorded name stands for, under its current name or one it had.</summary>
    private static Kind? KindNamed(string? name)
    {
        if (name is null) return null;

        lock (Gate)
            return Kinds.TryGetValue(name, out var kind) ? kind : Former.GetValueOrDefault(name);
    }

    /// <summary>The full names of every registered data asset type, for a menu that makes one.</summary>
    public static IReadOnlyList<string> Types
    {
        get { lock (Gate) return [.. Kinds.Keys.Order(StringComparer.Ordinal)]; }
    }

    /// <summary>The value of the data asset a reference names, loaded once and shared.</summary>
    /// <exception cref="FileNotFoundException">No file has the reference's id.</exception>
    /// <exception cref="InvalidDataException">The file is not a <typeparamref name="T"/>.</exception>
    public static T Get<T>(DataRef<T> reference)
    {
        if (!reference.IsSet) throw new ArgumentException("The reference names no data asset.", nameof(reference));

        var loaded = Load(reference.Id);
        if (loaded.Kind.Type != typeof(T))
            throw new InvalidDataException(
                $"{AssetIds.PathOf(reference.Id)} holds a {loaded.Kind.Name}, not a {typeof(T).FullName}.");

        return ((DataBox<T>)loaded.Box).Value;
    }

    /// <summary>Reads the data asset a reference names, reporting whether there is one to read.</summary>
    public static bool TryGet<T>(DataRef<T> reference, out T value) => TryGet(reference, out value, out _);

    /// <summary>
    /// Reads the data asset a reference names, or says why there is none to read.
    /// </summary>
    /// <param name="reference">The asset.</param>
    /// <param name="value">
    /// Its value, shared as <see cref="Get{T}"/> shares it, or the type's default.
    /// </param>
    /// <param name="problem">
    /// Why it was not read, naming its file, or its id where no file has it, or nothing where it
    /// was.
    /// </param>
    public static bool TryGet<T>(DataRef<T> reference, out T value, [NotNullWhen(false)] out string? problem)
    {
        try
        {
            value = Get(reference);
            problem = null;
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            value = default!;
            problem = error.Message;
            return false;
        }
    }

    /// <summary>
    /// The value of the data asset a reference names, or, where it cannot be read, its type's
    /// defaults, with why said once on the log and as <see cref="AssetLoadFailed"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <see cref="DataRef{T}.Value"/> answers, so a component's reference to a file a player
    /// or an artist broke reads as the type's defaults and the game goes on, as N 2.6 of NORM.md
    /// has it. <see cref="Get{T}"/> stays the form that throws, for a game that asks.
    /// </para>
    /// <para>
    /// The defaults are kept for the reference until its file is read again, by
    /// <see cref="Reload"/> or the watcher, so a system reading it every frame is answered from
    /// memory and the problem is said the once. A reference that names nothing is a mistake in the
    /// code rather than a file, and throws as <see cref="Get{T}"/> does.
    /// </para>
    /// </remarks>
    internal static T ValueOf<T>(DataRef<T> reference)
    {
        if (!reference.IsSet) throw new ArgumentException("The reference names no data asset.", nameof(reference));

        lock (Gate)
        {
            if (Fallbacks.TryGetValue((reference.Id, typeof(T)), out var kept)) return ((DataBox<T>)kept).Value;
        }

        if (TryGet(reference, out var value, out var problem)) return value;

        var box = KindOf(typeof(T)).Create();
        lock (Gate) Fallbacks[(reference.Id, typeof(T))] = box;
        AssetServer.Failed(reference.ToString(), problem, typeof(T).FullName ?? typeof(T).Name);
        return ((DataBox<T>)box).Value;
    }

    /// <summary>
    /// Writes a new data asset at its type's defaults, gives it an id, and returns a reference.
    /// </summary>
    /// <param name="path">Where, relative to the asset root, ending in <see cref="Extension"/>.</param>
    /// <exception cref="InvalidOperationException">The type is not a registered data asset.</exception>
    public static DataRef<T> Create<T>(string path)
    {
        var kind = KindOf(typeof(T));
        var box = kind.Create();
        Write(path, kind, kind.Bind(box));

        var id = AssetIds.IdOf(path, create: true);
        lock (Gate) Cache[id] = new Loaded(box, Tracked(id, kind.Bind(box)), kind);
        return new DataRef<T>(id);
    }

    /// <summary>
    /// Makes a new data asset of a type named at runtime, for a menu offering every type.
    /// </summary>
    /// <returns>The new file's id.</returns>
    public static ulong Create(string type, string path)
    {
        Kind kind;
        lock (Gate)
        {
            if (!Kinds.TryGetValue(type, out kind!))
                throw new InvalidOperationException($"'{type}' is not a data asset type.");
        }

        var box = kind.Create();
        Write(path, kind, kind.Bind(box));
        var id = AssetIds.IdOf(path, create: true);
        lock (Gate) Cache[id] = new Loaded(box, Tracked(id, kind.Bind(box)), kind);
        return id;
    }

    /// <summary>
    /// Writes a copy of a data asset to a new file with an id of its own, for the one thing that
    /// should differ from the rest sharing it.
    /// </summary>
    /// <param name="id">The asset to copy.</param>
    /// <param name="path">Where the copy goes, relative to the asset root.</param>
    /// <returns>The copy's id.</returns>
    /// <remarks>
    /// The copy holds the value as it is loaded, unsaved changes included, and the values the file
    /// had that no field reads, so nothing the original carried is lost in the copy.
    /// </remarks>
    /// <exception cref="IOException">A file is already at <paramref name="path"/>.</exception>
    public static ulong Copy(ulong id, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (File.Exists(Path.Combine(AssetIds.Root, path))) throw new IOException($"{path} is already there.");

        var loaded = Load(id);
        Write(path, loaded.Kind, loaded.Schema, loaded.Unread);
        return AssetIds.IdOf(path, create: true);
    }

    /// <summary>Writes a value over the data asset a reference names, and shares it from then on.</summary>
    public static void Save<T>(DataRef<T> reference, T value)
    {
        var loaded = Load(reference.Id);
        ((DataBox<T>)loaded.Box).Value = value;
        Save(reference.Id);
    }

    /// <summary>Writes a loaded data asset's value to its file, as it is now.</summary>
    /// <remarks>What the editor calls after changing one of its fields.</remarks>
    public static void Save(ulong id)
    {
        var loaded = Load(id);
        var path = AssetIds.PathOf(id) ?? throw new FileNotFoundException($"No data asset has the id {id:x16}.");
        Write(path, loaded.Kind, loaded.Schema, loaded.Unread);
        Changed.Enqueue(id);
    }

    /// <summary>
    /// The fields of a data asset, bound to its loaded value, for a tool to draw and edit.
    /// </summary>
    /// <remarks>
    /// A field written through it changes the shared value at once, and <see cref="Save(ulong)"/>
    /// writes the file. The world and entity a field is read with are ignored.
    /// </remarks>
    public static ComponentSchema? SchemaOf(ulong id)
    {
        try
        {
            return Load(id).Schema;
        }
        catch (Exception error) when (error is FileNotFoundException or InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>
    /// The data asset files under the asset root, as paths from it, holding one type or any.
    /// </summary>
    /// <remarks>
    /// Read from the folder each time it is asked, since files are added and renamed while a tool
    /// runs, and knowing a file's type means opening it, so a filtered list costs a read of each
    /// file. Files compiled into the game's assembly (<see cref="AssetFiles"/>) are listed as well,
    /// under the paths they had.
    /// </remarks>
    /// <param name="type">The full name of the type to keep, or nothing for every data asset.</param>
    public static IReadOnlyList<string> Files(string? type = null)
    {
        var root = AssetIds.Root;
        var found = new SortedSet<string>(StringComparer.Ordinal);

        if (Directory.Exists(root))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*" + Extension, SearchOption.AllDirectories))
                found.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }

        foreach (var file in AssetFiles.Carried(Extension)) found.Add(file);

        return [.. found.Where(file => type is null || TypeOf(file) == type)];
    }

    /// <summary>The full name of the type a data asset file holds, or nothing for a file that holds none.</summary>
    /// <remarks>
    /// A file recording a name its type has since given up answers with the current name, so a
    /// picker filtering by type still offers files written before the rename.
    /// </remarks>
    public static string? TypeOf(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(AssetFiles.ReadAllText(path));
            var name = document.RootElement.TryGetProperty("type", out var type) ? type.GetString() : null;
            return KindNamed(name)?.Name ?? name;
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Forgets a loaded data asset, so the next read loads its file again.</summary>
    public static void Reload(ulong id)
    {
        lock (Gate)
        {
            Cache.Remove(id);
            foreach (var key in Fallbacks.Keys.Where(key => key.Id == id).ToArray()) Fallbacks.Remove(key);
        }

        Changed.Enqueue(id);
    }

    /// <summary>Forgets every loaded data asset.</summary>
    public static void ReloadAll()
    {
        lock (Gate)
        {
            Cache.Clear();
            Fallbacks.Clear();
        }
    }

    /// <summary>
    /// Writes every data asset changed through its fields since the last call.
    /// </summary>
    /// <remarks>
    /// The editor calls this once a frame, so a slider dragged across a field writes the file once a
    /// frame rather than once per step, and an undo that writes a field back is saved like any edit.
    /// </remarks>
    /// <returns>How many files were written.</returns>
    public static int SaveChanged()
    {
        ulong[] changed;
        lock (Gate)
        {
            if (Unsaved.Count == 0) return 0;
            changed = [.. Unsaved];
            Unsaved.Clear();
        }

        foreach (var id in changed) Save(id);
        return changed.Length;
    }

    /// <summary>
    /// The schema with every field's write marking the asset changed, so <see cref="SaveChanged"/>
    /// knows which files to write.
    /// </summary>
    private static ComponentSchema Tracked(ulong id, ComponentSchema schema)
    {
        var fields = schema.Fields
            .Select(field => new ComponentField(
                field.Name,
                field.Kind,
                field.Type,
                field.Read,
                // A field with no writer refuses through Write, and one the generator could not
                // write is marked read only in its hints, so wrapping every field is safe.
                (world, entity, value) =>
                {
                    if (!field.Write(world, entity, value)) return false;
                    lock (Gate) Unsaved.Add(id);
                    return true;
                },
                field.Options,
                field.Hints)
            {
                Derived = field.Derived,
                ElementKind = field.ElementKind,
                Items = field.Items,
                KeyKind = field.KeyKind,
            })
            .ToArray();

        // Everything about the type carried over, since a save writes through this schema and a
        // version left behind would write a file a later load migrates a second time.
        return new ComponentSchema(schema.Name, schema.QualifiedName, static () => -1, fields)
        {
            Origin = schema.Origin,
            FormerNames = schema.FormerNames,
            Version = schema.Version,
            Migrate = schema.Migrate,
        };
    }

    /// <summary>Sends what changed since last frame onto the bus. Called by the app each frame.</summary>
    internal static void PostChanges(MessageBus bus)
    {
        ReloadTouched();
        while (Changed.TryDequeue(out var id)) bus.Send(new DataAssetChanged(id));
    }

    private static Kind KindOf(Type type)
    {
        lock (Gate)
        {
            return KindsByType.TryGetValue(type, out var kind)
                ? kind
                : throw new InvalidOperationException(
                    $"'{type.FullName}' is not a data asset. Mark it [DataAsset] so the generator describes it.");
        }
    }

    /// <summary>The loaded asset with an id, reading its file the first time it is asked for.</summary>
    private static Loaded Load(ulong id)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(id, out var cached)) return cached;
        }

        var path = AssetIds.PathOf(id) ?? throw new FileNotFoundException($"No data asset has the id {id:x16}.");

        using var document = AssetFiles.ReadJson(path, "a data asset");
        var root = document.RootElement;

        var name = root.TryGetProperty("type", out var type) ? type.GetString() : null;
        var kind = KindNamed(name);
        if (kind is null)
            throw new InvalidDataException($"{path} holds '{name}', which is not a data asset type this build has.");

        var box = kind.Create();
        var schema = kind.Bind(box);
        IReadOnlyList<KeyValuePair<string, string>>? unread = null;
        if (root.TryGetProperty("fields", out var found))
        {
            var fields = SceneValue.Migrated(found, schema);
            SceneValue.ReadComponent(fields, schema, Detached, Entity.None);
            unread = SceneValue.Unread(fields, schema);
        }

        var loaded = new Loaded(box, Tracked(id, schema), kind, unread);
        lock (Gate) Cache[id] = loaded;
        return loaded;
    }

    /// <summary>Writes a value to its file, through a temporary file so a crash leaves the last one whole.</summary>
    private static void Write(
        string path, Kind kind, ComponentSchema schema, IReadOnlyList<KeyValuePair<string, string>>? unread = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("format", Format);
            json.WriteString("type", kind.Name);
            json.WritePropertyName("fields");
            SceneValue.WriteComponent(json, schema, Detached, Entity.None, kept: unread);
            json.WriteEndObject();
        }

        var full = Path.Combine(AssetIds.Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        var temporary = full + ".tmp";
        File.WriteAllText(temporary, Encoding.UTF8.GetString(buffer.WrittenSpan) + "\n");
        File.Move(temporary, full, overwrite: true);

        // Remembered, so the watcher passes over the change this write makes rather than reading
        // back what was written a moment ago and announcing it as news.
        lock (Gate) Wrote[Path.GetFullPath(full)] = File.GetLastWriteTimeUtc(full);
    }

    /// <summary>
    /// Whether a data file changed on disk is read again on its own, as an edit in a text editor or
    /// a pull from version control is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set from <see cref="Config.WatchAssets"/> when an app starts, the same switch that has Bevy
    /// reload a texture, so a shipped game watches nothing. While it is on, a file under the asset
    /// root that changes and is loaded is dropped from the cache at the top of the next frame, and
    /// <see cref="DataAssetChanged"/> says so, as <see cref="Reload"/> does. A file not loaded is
    /// read fresh the next time it is asked for anyway.
    /// </para>
    /// <para>
    /// A change this side wrote is passed over, told apart by the time the write left on the file,
    /// and a run of changes to one file in a frame is one reload.
    /// </para>
    /// </remarks>
    public static bool Watching { get; set; }

    private static FileSystemWatcher? _watcher;
    private static string? _watched;
    private static readonly ConcurrentQueue<string> Touched = new();
    private static readonly Dictionary<string, DateTime> Wrote = new(StringComparer.Ordinal);

    /// <summary>Starts, moves or stops the watcher so it matches <see cref="Watching"/> and the root.</summary>
    private static void Watch()
    {
        var root = AssetIds.Root;
        var wanted = Watching && Directory.Exists(root) ? root : null;
        if (wanted == _watched) return;

        _watcher?.Dispose();
        _watcher = null;
        _watched = wanted;
        if (wanted is null) return;

        _watcher = new FileSystemWatcher(wanted, "*.data.json")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };

        // On the watcher's own thread, so they only note the path for the frame to deal with.
        _watcher.Changed += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.Created += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.Renamed += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>Drops each loaded asset whose file changed on disk since the last frame.</summary>
    private static void ReloadTouched()
    {
        Watch();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (Touched.TryDequeue(out var full))
        {
            if (!seen.Add(full) || !File.Exists(full)) continue;

            DateTime written;
            try
            {
                written = File.GetLastWriteTimeUtc(full);
            }
            catch (IOException)
            {
                continue;
            }

            lock (Gate)
            {
                if (Wrote.TryGetValue(full, out var ours) && ours == written) continue;
            }

            var relative = Path.GetRelativePath(AssetIds.Root, full).Replace('\\', '/');
            if (AssetIds.IdOf(relative) is not (var id and not 0)) continue;

            // One answered with its defaults is read again too, since the change may mend it.
            bool loaded;
            lock (Gate) loaded = Cache.ContainsKey(id) || Fallbacks.Keys.Any(key => key.Id == id);
            if (loaded) Reload(id);
        }
    }
}

using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bevy;

/// <summary>
/// Writes a component's fields as JSON and reads them back, one writer and one reader per
/// <see cref="FieldKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// The values of a scene file, a data asset and a save all go through here, so a vector or a color
/// is written one way everywhere. Nothing goes through <c>ToString</c> and back, which is how a
/// float written on a machine that uses a comma for the decimal point stops reading on one that
/// uses a full stop, and nothing reflects, so trimming and compiling ahead of time leave it working.
/// </para>
/// <para>
/// A vector, a quaternion and a color are arrays of numbers, a color in linear RGBA. An enum is its
/// name and a set of flags a list of names, so reordering an enum's members leaves a file readable.
/// A list is an array, and a map an object, or an array of pairs where its keys cannot be property
/// names. An entity and an asset are objects, which <see cref="SceneReferences"/> writes and reads, since
/// what either refers to is a question about the scene as a whole rather than about one value.
/// </para>
/// <para>
/// A component is an object of its fields. A field the generator took apart from a nested struct
/// (<c>Front.Held.At</c>) is written back inside objects named for the struct it came from, so the
/// file has the shape of the type rather than of the inspector's rows. A derived field, worked out
/// from the others, is not written.
/// </para>
/// </remarks>
public static class SceneValue
{
    /// <summary>Writes one value as the JSON its kind takes.</summary>
    /// <param name="json">Where it goes.</param>
    /// <param name="field">The field it was read from, which says its kind and its options.</param>
    /// <param name="value">The value, as <see cref="ComponentField.Read"/> boxed it.</param>
    /// <param name="references">How an entity or an asset is named, or nothing for the default.</param>
    /// <returns>Whether anything was written, which a value with no JSON form is not.</returns>
    public static bool Write(
        Utf8JsonWriter json, ComponentField field, object value, SceneReferences? references = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

        references ??= SceneReferences.Default;

        // A list is an array of its items, each written as the kind the list holds. An item that
        // cannot be written is null in its place, so the items after it keep their places.
        if (field.Kind == FieldKind.List)
        {
            if (value is not ListValue items) return false;

            json.WriteStartArray();
            foreach (var item in items)
            {
                if (item is null || !Item(json, field, item, references))
                    json.WriteNullValue();
            }

            json.WriteEndArray();
            return true;
        }

        if (field.Kind == FieldKind.Map)
        {
            if (value is not MapValue entries) return false;

            // An object where the keys can be its property names, so a map of names to numbers
            // reads as one, and an array of pairs where they cannot, such as vectors.
            var named = Named(field.KeyKind);
            if (named) json.WriteStartObject();
            else json.WriteStartArray();

            foreach (var (key, held) in entries)
            {
                if (named)
                {
                    json.WritePropertyName(KeyText(key));
                }
                else
                {
                    json.WriteStartArray();
                    if (!Write(json, field.KeyKind, key, references)) json.WriteNullValue();
                }

                if (held is null || !Item(json, field, held, references))
                    json.WriteNullValue();

                if (!named) json.WriteEndArray();
            }

            if (named) json.WriteEndObject();
            else json.WriteEndArray();
            return true;
        }

        return Write(json, field.Kind, value, references);
    }

    /// <summary>
    /// Writes one item of a list or one value of a map, as an object of its fields where it has
    /// fields of its own.
    /// </summary>
    private static bool Item(Utf8JsonWriter json, ComponentField field, object item, SceneReferences references)
    {
        if (field.ElementKind != FieldKind.Struct) return Write(json, field.ElementKind, item, references);
        if (field.Items is not { } fields) return false;

        WriteComponent(json, fields.Bind(item).Schema, Loose, Entity.None, references);
        return true;
    }

    /// <summary>Reads one item of a list or one value of a map, or nothing when it is not that kind.</summary>
    private static object? Item(JsonElement json, ComponentField field, SceneReferences references)
    {
        if (field.ElementKind != FieldKind.Struct) return Read(json, field.ElementKind, field.Hints.Asset, references);
        if (field.Items is not { } fields || json.ValueKind != JsonValueKind.Object) return null;

        // From the defaults, so a field the file leaves out keeps the value a new item would have.
        var bound = fields.Bind(fields.Create());
        ReadComponent(json, bound.Schema, Loose, Entity.None, references);
        return bound.Value();
    }

    /// <summary>The world an item's fields are given, which they ignore, since they read a box.</summary>
    private static readonly EcsWorld Loose = new();

    /// <summary>Whether a map's keys can be written as an object's property names.</summary>
    private static bool Named(FieldKind key) => key is FieldKind.String or FieldKind.Int or FieldKind.Enum;

    /// <summary>A key as a property name.</summary>
    private static string KeyText(object key) => key is IConvertible number and not string and not Enum
        ? number.ToInt64(CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
        : key.ToString() ?? string.Empty;

    /// <summary>Writes one value of a kind, reporting whether it had a form to write.</summary>
    private static bool Write(Utf8JsonWriter json, FieldKind kind, object value, SceneReferences references)
    {
        var plain = CultureInfo.InvariantCulture;

        switch (kind)
        {
            case FieldKind.Bool when value is bool on:
                json.WriteBooleanValue(on);
                return true;

            case FieldKind.Int when value is IConvertible whole:
                json.WriteNumberValue(whole.ToInt64(plain));
                return true;

            case FieldKind.Float when value is IConvertible number:
                return Finite(json, number.ToSingle(plain));

            case FieldKind.Double when value is IConvertible number:
                var wide = number.ToDouble(plain);
                if (!double.IsFinite(wide)) return false;
                json.WriteNumberValue(wide);
                return true;

            case FieldKind.String when value is string text:
                json.WriteStringValue(text);
                return true;

            case FieldKind.Vec2 when value is Vec2 v:
                return Numbers(json, v.X, v.Y);

            case FieldKind.Vec3 when value is Vec3 v:
                return Numbers(json, v.X, v.Y, v.Z);

            case FieldKind.Vec4 when value is Vec4 v:
                return Numbers(json, v.X, v.Y, v.Z, v.W);

            case FieldKind.Quat when value is Quat q:
                return Numbers(json, q.X, q.Y, q.Z, q.W);

            case FieldKind.Color when value is Color c:
                return Numbers(json, c.R, c.G, c.B, c.A);

            case FieldKind.Enum:
                json.WriteStringValue(value.ToString());
                return true;

            case FieldKind.Flags:
                // A flags enum says itself as its names joined by commas, and as a number for a
                // value with a bit no name covers, which is written as the number.
                var said = value.ToString() ?? string.Empty;
                json.WriteStartArray();
                var parts = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
                foreach (var name in said.Split(',', parts)) json.WriteStringValue(name);
                json.WriteEndArray();
                return true;

            case FieldKind.Entity when value is Entity entity:
                return references.WriteEntity(json, entity);

            case FieldKind.Asset when value is AssetHandle asset:
                return references.WriteAsset(json, asset);

            case FieldKind.Data when value is IDataRef { Id: not 0 } data:
                // The id and the path both, as every reference to a file is written: found by the
                // id while the file is renamed, and by the path if its sidecar is lost.
                json.WriteStartObject();
                json.WriteString("uid", data.Id.ToString("x16", plain));
                if (AssetIds.PathOf(data.Id) is { } path) json.WriteString("path", path);
                json.WriteEndObject();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Reads one value of a field's kind, boxed as <see cref="ComponentField.Write"/> takes it, or
    /// <see langword="null"/> when the JSON is not that kind.
    /// </summary>
    public static object? Read(
        JsonElement json, ComponentField field, SceneReferences? references = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        references ??= SceneReferences.Default;

        if (field.Kind == FieldKind.List)
        {
            if (json.ValueKind != JsonValueKind.Array) return null;

            var items = new List<object?>();
            foreach (var item in json.EnumerateArray())
            {
                // An item that cannot be read leaves the list unreadable rather than shorter,
                // since a list missing one item silently would move every item after it.
                if (Item(item, field, references) is not { } read)
                    return null;
                items.Add(read);
            }

            return new ListValue(items);
        }

        if (field.Kind == FieldKind.Map)
        {
            var entries = new List<KeyValuePair<object, object?>>();

            if (json.ValueKind == JsonValueKind.Object && Named(field.KeyKind))
            {
                foreach (var property in json.EnumerateObject())
                {
                    object? key = field.KeyKind == FieldKind.Int
                        ? long.TryParse(property.Name, CultureInfo.InvariantCulture, out var whole) ? whole : null
                        : property.Name;
                    var held = Item(property.Value, field, references);
                    if (key is null || held is null) return null;
                    entries.Add(new(key, held));
                }
            }
            else if (json.ValueKind == JsonValueKind.Array)
            {
                foreach (var pair in json.EnumerateArray())
                {
                    if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2) return null;

                    var key = Read(pair[0], field.KeyKind, null, references);
                    var held = Item(pair[1], field, references);
                    if (key is null || held is null) return null;
                    entries.Add(new(key, held));
                }
            }
            else
            {
                return null;
            }

            return new MapValue(entries);
        }

        return Read(json, field.Kind, field.Hints.Asset, references);
    }

    /// <summary>Reads one value of a kind, or nothing when the JSON is not that kind.</summary>
    private static object? Read(
        JsonElement json, FieldKind kind, string? asset, SceneReferences references)
    {
        try
        {
            return kind switch
            {
                FieldKind.Bool => json.GetBoolean(),
                FieldKind.Int => json.GetInt64(),
                FieldKind.Float => json.GetSingle(),
                FieldKind.Double => json.GetDouble(),
                FieldKind.String => json.GetString(),
                FieldKind.Vec2 => Numbers(json, 2) is { } n ? new Vec2(n[0], n[1]) : null,
                FieldKind.Vec3 => Numbers(json, 3) is { } n ? new Vec3(n[0], n[1], n[2]) : null,
                FieldKind.Vec4 => Numbers(json, 4) is { } n ? new Vec4(n[0], n[1], n[2], n[3]) : null,
                FieldKind.Quat => Numbers(json, 4) is { } n ? new Quat(n[0], n[1], n[2], n[3]) : null,
                FieldKind.Color => Numbers(json, 4) is { } n ? new Color(n[0], n[1], n[2], n[3]) : null,
                FieldKind.Enum => json.GetString(),
                FieldKind.Flags => string.Join(", ", json.EnumerateArray().Select(name => name.GetString())),
                FieldKind.Entity => references.ReadEntity(json),
                FieldKind.Asset => references.ReadAsset(json, asset ?? AssetKind.Mesh),
                FieldKind.Data => DataId(json),
                _ => null,
            };
        }
        catch (Exception error) when (error is InvalidOperationException or FormatException)
        {
            // The wrong kind of JSON for the field, such as a file written before the field
            // changed type. A field that cannot be read is left as it is rather than ending the load.
            return null;
        }
    }

    /// <summary>
    /// The name a component object records its type's <see cref="ComponentSchema.Version"/> under,
    /// beside its fields.
    /// </summary>
    /// <remarks>Not a name a C# field can have, so it cannot be taken for one.</remarks>
    public const string VersionName = "$version";

    /// <summary>
    /// A component object brought up to its type's current version, or the object as it is when
    /// it is current or the type has no migration.
    /// </summary>
    /// <remarks>
    /// The object is handed to the type's <c>Migrate</c> as a <see cref="JsonObject"/> without the
    /// version, which is the method's first argument instead, and what it returns is read as the
    /// file. An object recording no version was written before the type had one, which is version 1.
    /// </remarks>
    /// <exception cref="InvalidDataException">The migration threw, which the exception carries.</exception>
    public static JsonElement Migrated(JsonElement json, ComponentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (schema.Migrate is not { } migrate || schema.Version <= 1 || json.ValueKind != JsonValueKind.Object)
            return json;

        var from = json.TryGetProperty(VersionName, out var recorded) && recorded.TryGetInt32(out var number)
            ? number
            : 1;
        if (from >= schema.Version) return json;

        JsonObject result;
        try
        {
            var value = JsonNode.Parse(json.GetRawText())!.AsObject();
            value.Remove(VersionName);
            result = migrate(from, value);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            throw new InvalidDataException(
                $"Bringing {schema.QualifiedName} from version {from} to {schema.Version} failed. {error.Message}",
                error);
        }

        // Through bytes and back, since what the reader takes is an element, and a clone of one
        // outlives the document it was parsed into.
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer)) result.WriteTo(writer);
        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Writes every field of one component on an entity as an object, nesting the fields of a
    /// struct inside the struct's name.
    /// </summary>
    /// <returns>How many fields were written.</returns>
    /// <param name="json">Where to write.</param>
    /// <param name="schema">The component's fields.</param>
    /// <param name="world">The world the entity is in.</param>
    /// <param name="entity">The entity.</param>
    /// <param name="references">How to name entities and assets, or nothing for by path alone.</param>
    /// <param name="kept">
    /// Values a load found no field for, by dotted name with their JSON, written back where they
    /// were so a file outlives a build that does not know them (<see cref="Unread"/>).
    /// </param>
    public static int WriteComponent(
        Utf8JsonWriter json,
        ComponentSchema schema,
        EcsWorld world,
        Entity entity,
        SceneReferences? references = null,
        IReadOnlyList<KeyValuePair<string, string>>? kept = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(world);

        // Gathered first, because the nesting is only known once every name has been seen.
        var root = new Node(string.Empty);
        foreach (var field in schema.Fields)
        {
            if (field.Derived) continue;
            if (field.Read(world, entity) is not { } value) continue;

            var node = root;
            var parts = field.Name.Split('.');
            foreach (var part in parts[..^1]) node = node.Child(part);
            node.Values.Add((parts[^1], field, value));
        }

        foreach (var (name, raw) in kept ?? [])
        {
            var node = root;
            var parts = name.Split('.');
            foreach (var part in parts[..^1]) node = node.Child(part);
            node.Kept.Add((parts[^1], raw));
        }

        var written = 0;
        Emit(root);
        return written;

        void Emit(Node node)
        {
            json.WriteStartObject();

            // The version first, so a reader sees it before the fields it says the shape of.
            if (node == root && schema.Version > 1) json.WriteNumber(VersionName, schema.Version);

            foreach (var (name, field, value) in node.Values)
            {
                json.WritePropertyName(name);

                // A writer refuses before writing anything, so a refused value leaves the name
                // standing alone, which JSON does not allow. A null says the field was there and
                // had nothing that could be written, such as an asset built in memory.
                if (Write(json, field, value, references)) written++;
                else json.WriteNullValue();
            }

            // A name a field has taken since the file was loaded is the field's, since the value it
            // holds is the newer one, and JSON allows a name once per object.
            foreach (var (name, raw) in node.Kept)
            {
                if (node.Values.Exists(value => value.Name == name)) continue;
                if (node.Children.Exists(child => child.Name == name)) continue;

                json.WritePropertyName(name);
                json.WriteRawValue(raw);
            }

            foreach (var child in node.Children)
            {
                json.WritePropertyName(child.Name);
                Emit(child);
            }

            json.WriteEndObject();
        }
    }

    /// <summary>
    /// Writes every field a component object names onto an entity's component, leaving a field the
    /// object leaves out as it is.
    /// </summary>
    /// <returns>How many fields were written.</returns>
    /// <remarks>
    /// A field the file names and the type no longer has is skipped, and one whose value is not its
    /// kind is left as it is, so a file written before a type changed still loads what it can. A
    /// field the file has under a name the field gave up, kept with <see cref="FormerNameAttribute"/>,
    /// is read from there when the file has nothing under the current name.
    /// </remarks>
    public static int ReadComponent(
        JsonElement json,
        ComponentSchema schema,
        EcsWorld world,
        Entity entity,
        SceneReferences? references = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(world);

        var written = 0;
        foreach (var field in schema.Fields)
        {
            if (field.Derived) continue;
            if ((Find(json, field.Name) ?? Former(json, field)) is not { } found) continue;
            if (found.ValueKind == JsonValueKind.Null) continue;
            if (Read(found, field, references) is not { } value) continue;

            if (field.Write(world, entity, value)) written++;
        }

        return written;
    }

    /// <summary>
    /// The values a component object holds that no field of the schema reads, by dotted name with
    /// their JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A file written by a newer build, or by an older one before a field was taken out, holds
    /// values this build has no field for. Reading skips them, and a save that wrote only the
    /// fields would lose them for good, so a load keeps these and <see cref="WriteComponent"/> puts
    /// them back. A value read under a name its field gave up is not among them, since the field
    /// carries it and writes it under the new name.
    /// </para>
    /// <para>
    /// The walk goes into a nested object only where a field's path does, so an object no field
    /// lies under is kept whole rather than taken apart.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<KeyValuePair<string, string>> Unread(JsonElement json, ComponentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (json.ValueKind != JsonValueKind.Object) return [];

        var read = new HashSet<string>(StringComparer.Ordinal);
        var above = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in schema.Fields)
        {
            foreach (var name in field.Hints.FormerNames.Prepend(field.Name))
            {
                read.Add(name);
                for (var dot = name.IndexOf('.'); dot > 0; dot = name.IndexOf('.', dot + 1))
                    above.Add(name[..dot]);
            }
        }

        var unread = new List<KeyValuePair<string, string>>();
        Walk(json, string.Empty);
        return unread;

        void Walk(JsonElement at, string prefix)
        {
            foreach (var property in at.EnumerateObject())
            {
                var path = prefix + property.Name;
                if (read.Contains(path) || path == VersionName) continue;

                if (above.Contains(path) && property.Value.ValueKind == JsonValueKind.Object)
                    Walk(property.Value, path + ".");
                else
                    unread.Add(new(path, property.Value.GetRawText()));
            }
        }
    }

    /// <summary>The value under the first name a field had before that the file holds, or nothing.</summary>
    private static JsonElement? Former(JsonElement json, ComponentField field)
    {
        foreach (var name in field.Hints.FormerNames)
        {
            if (Find(json, name) is { } found) return found;
        }

        return null;
    }

    /// <summary>The value at a dotted name inside nested objects, or nothing.</summary>
    private static JsonElement? Find(JsonElement json, string name)
    {
        var at = json;
        foreach (var part in name.Split('.'))
        {
            if (at.ValueKind != JsonValueKind.Object || !at.TryGetProperty(part, out at)) return null;
        }

        return at;
    }

    /// <summary>
    /// The file id a data reference names: its id when a file still has it, and the id of the file
    /// at its path when none does, which finds a file whose sidecar was lost.
    /// </summary>
    private static object? DataId(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object) return null;

        ulong? id = json.TryGetProperty("uid", out var uid)
            && ulong.TryParse(uid.GetString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;

        if (id is { } known && AssetIds.PathOf(known) is not null) return known;

        if (json.TryGetProperty("path", out var path) && path.GetString() is { Length: > 0 } file
            && AssetIds.IdOf(file) is var found and not 0)
            return found;

        return id;
    }

    /// <summary>Writes numbers as an array, refusing one JSON cannot hold.</summary>
    private static bool Numbers(Utf8JsonWriter json, params ReadOnlySpan<float> numbers)
    {
        foreach (var number in numbers)
            if (!float.IsFinite(number)) return false;

        json.WriteStartArray();
        foreach (var number in numbers) json.WriteNumberValue(number);
        json.WriteEndArray();
        return true;
    }

    /// <summary>Reads an array of exactly <paramref name="count"/> numbers, or nothing.</summary>
    private static float[]? Numbers(JsonElement json, int count)
    {
        if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() != count) return null;
        return json.EnumerateArray().Select(number => number.GetSingle()).ToArray();
    }

    /// <summary>Writes a float, refusing an infinity or a NaN, which JSON has no way to say.</summary>
    private static bool Finite(Utf8JsonWriter json, float number)
    {
        if (!float.IsFinite(number)) return false;
        json.WriteNumberValue(number);
        return true;
    }

    /// <summary>One struct's worth of a component: its own values and the structs inside it.</summary>
    private sealed class Node(string name)
    {
        public string Name { get; } = name;

        public List<(string Name, ComponentField Field, object Value)> Values { get; } = [];

        public List<Node> Children { get; } = [];

        public List<(string Name, string Raw)> Kept { get; } = [];

        public Node Child(string name)
        {
            var found = Children.Find(child => child.Name == name);
            if (found is not null) return found;

            var made = new Node(name);
            Children.Add(made);
            return made;
        }
    }
}

/// <summary>
/// How a scene names what a field refers to: another entity, and an asset.
/// </summary>
/// <remarks>
/// <para>
/// An entity is written as a local id, <c>{ "entity": 4 }</c>, because the entity's own handle means
/// nothing in the next run. Which id stands for which entity is the scene's to say, so a writer
/// numbers what it is writing with <see cref="Name"/>, and a reader says which entity each id
/// spawned as with <see cref="Spawned"/>. A field that refers to an entity the scene did not number
/// is written as nothing, since the reference could not be followed after a reload.
/// </para>
/// <para>
/// An asset is written as the path it was loaded from, <c>{ "path": "models/ship.glb#Mesh0" }</c>,
/// and read back by loading it as the kind of asset the field holds. An asset built in memory has no
/// path and is written as nothing. Ids that survive a file being renamed are a later step, and they
/// arrive as a field of the same object.
/// </para>
/// </remarks>
public class SceneReferences
{
    private readonly Dictionary<Entity, int> _ids = [];
    private readonly Dictionary<int, Entity> _entities = [];

    /// <summary>
    /// References with no entities numbered, which writes assets by path and entities as nothing.
    /// </summary>
    public static SceneReferences Default { get; } = new();

    /// <summary>Numbers an entity that is being written, so a field referring to it can name it.</summary>
    public void Name(Entity entity, int id)
    {
        _ids[entity] = id;
        _entities[id] = entity;
    }

    /// <summary>Says which entity a local id was spawned as, so a field naming it can be read.</summary>
    public void Spawned(int id, Entity entity) => Name(entity, id);

    /// <summary>Writes a reference to an entity, reporting whether it could be named.</summary>
    public virtual bool WriteEntity(Utf8JsonWriter json, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (!_ids.TryGetValue(entity, out var id)) return false;

        json.WriteStartObject();
        json.WriteNumber("entity", id);
        json.WriteEndObject();
        return true;
    }

    /// <summary>Reads a reference to an entity, or nothing when the id names none.</summary>
    public virtual object? ReadEntity(JsonElement json) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty("entity", out var id)
        && _entities.TryGetValue(id.GetInt32(), out var entity)
            ? entity
            : null;

    /// <summary>
    /// Whether a reference to a file with no id gives it one, by writing a sidecar beside it.
    /// </summary>
    /// <remarks>
    /// Off unless asked for, because only something that edits the project writes a sidecar
    /// (<see cref="AssetIds"/>), and a game saving a scene at runtime may be running from a folder
    /// it cannot write. The editor asks for it, so a scene saved there refers to every file by id.
    /// </remarks>
    public bool GiveIds { get; init; }

    /// <summary>Writes a reference to an asset by its file, reporting whether it has one.</summary>
    public virtual bool WriteAsset(Utf8JsonWriter json, AssetHandle asset)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (AssetServer.PathOf(asset) is not { Length: > 0 } path) return false;

        WriteFile(json, path);
        return true;
    }

    /// <summary>Reads a reference to an asset by loading its file as the kind of asset given.</summary>
    public virtual object? ReadAsset(JsonElement json, string kind) =>
        ReadFile(json) is { Length: > 0 } file ? AssetServer.Load(kind, file) : null;

    /// <summary>
    /// Writes a reference to a file under the asset root as its id and its path, or as its path
    /// alone when it has no id.
    /// </summary>
    /// <remarks>
    /// The id is the file's, so a <c>#label</c> naming a part of it, such as one mesh of a model,
    /// stays in the path, and is put back on whichever path the id is found at.
    /// </remarks>
    public void WriteFile(Utf8JsonWriter json, string path)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(path);

        json.WriteStartObject();
        if (IdOf(path) is var id and not 0)
            json.WriteString("uid", id.ToString("x16", CultureInfo.InvariantCulture));
        json.WriteString("path", path);
        json.WriteEndObject();
    }

    /// <summary>
    /// The path a file reference names: where the file holding its id is now, with the label the
    /// reference gave, or the path it recorded when no file holds the id.
    /// </summary>
    /// <remarks>A bare string is read as a path, which is how a scene wrote a file before ids.</remarks>
    public static string? ReadFile(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.String) return json.GetString();
        if (json.ValueKind != JsonValueKind.Object) return null;

        var recorded = json.TryGetProperty("path", out var path) ? path.GetString() : null;

        if (json.TryGetProperty("uid", out var uid)
            && ulong.TryParse(uid.GetString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)
            && AssetIds.PathOf(id) is { } moved)
        {
            var label = recorded?.IndexOf('#') is { } hash and >= 0 ? recorded[hash..] : string.Empty;
            return moved + label;
        }

        return recorded;
    }

    /// <summary>The id of the file a path names, given one when asked to, or zero.</summary>
    private ulong IdOf(string path)
    {
        // A path from another source, such as an embedded one, is not a file under the root.
        if (path.Contains("://", StringComparison.Ordinal)) return 0;

        var hash = path.IndexOf('#');
        var file = hash < 0 ? path : path[..hash];

        try
        {
            return AssetIds.IdOf(file, create: GiveIds && File.Exists(Path.Combine(AssetIds.Root, file)));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A sidecar that cannot be written leaves the reference by path, which still loads.
            return 0;
        }
    }
}

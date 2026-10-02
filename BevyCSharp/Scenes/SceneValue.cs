using System.Globalization;
using System.Text.Json;

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
/// An entity and an asset are objects, which <see cref="SceneReferences"/> writes and reads, since
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
        var plain = CultureInfo.InvariantCulture;

        switch (field.Kind)
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
                foreach (var name in said.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    json.WriteStringValue(name);
                json.WriteEndArray();
                return true;

            case FieldKind.Entity when value is Entity entity:
                return references.WriteEntity(json, entity);

            case FieldKind.Asset when value is AssetHandle asset:
                return references.WriteAsset(json, asset);

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

        try
        {
            return field.Kind switch
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
                FieldKind.Asset => references.ReadAsset(json, field.Hints.Asset ?? AssetKind.Mesh),
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
    /// Writes every field of one component on an entity as an object, nesting the fields of a
    /// struct inside the struct's name.
    /// </summary>
    /// <returns>How many fields were written.</returns>
    public static int WriteComponent(
        Utf8JsonWriter json,
        ComponentSchema schema,
        EcsWorld world,
        Entity entity,
        SceneReferences? references = null)
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

        var written = 0;
        Emit(root);
        return written;

        void Emit(Node node)
        {
            json.WriteStartObject();

            foreach (var (name, field, value) in node.Values)
            {
                json.WritePropertyName(name);

                // A writer refuses before writing anything, so a refused value leaves the name
                // standing alone, which JSON does not allow. A null says the field was there and
                // had nothing that could be written, such as an asset built in memory.
                if (Write(json, field, value, references)) written++;
                else json.WriteNullValue();
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
    /// kind is left as it is, so a file written before a type changed still loads what it can.
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
            if (Find(json, field.Name) is not { } found) continue;
            if (found.ValueKind == JsonValueKind.Null) continue;
            if (Read(found, field, references) is not { } value) continue;

            if (field.Write(world, entity, value)) written++;
        }

        return written;
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

    /// <summary>Writes a reference to an asset by its path, reporting whether it has one.</summary>
    public virtual bool WriteAsset(Utf8JsonWriter json, AssetHandle asset)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (AssetServer.PathOf(asset) is not { Length: > 0 } path) return false;

        json.WriteStartObject();
        json.WriteString("path", path);
        json.WriteEndObject();
        return true;
    }

    /// <summary>Reads a reference to an asset by loading its path as the kind of asset given.</summary>
    public virtual object? ReadAsset(JsonElement json, string kind) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty("path", out var path)
        && path.GetString() is { Length: > 0 } file
            ? AssetServer.Load(kind, file)
            : null;
}

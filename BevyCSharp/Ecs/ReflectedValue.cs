using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Turns one reflected field's JSON into the CLR value C# holds it as, and back.
/// </summary>
/// <remarks>
/// <para>
/// One place for the conversion, because two things use it and they have to agree: a reflected
/// schema's rows, which an inspector draws, and the typed wrappers the generator emits from the
/// checked-in description of Bevy's components. A row that read a vector one way and a wrapper
/// that wrote it another would each look right alone.
/// </para>
/// <para>
/// The typed readers throw where a schema's row answers nothing, because a wrapper is code a
/// program wrote against a component it expects to be there, and an absent one is a fault worth
/// hearing about rather than a frame to skip.
/// </para>
/// </remarks>
internal static class ReflectedValue
{
    /// <summary>Reads a field's JSON as the CLR type its kind is held as.</summary>
    /// <returns>The value, or the JSON itself for a kind with no CLR type of its own.</returns>
    internal static object? Decode(string json, FieldKind kind)
    {
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;

        // Bevy writes a non-finite float as null, which is the one place null means a value.
        if (value.ValueKind == JsonValueKind.Null)
            return kind is FieldKind.Double ? double.NaN : kind is FieldKind.Float ? float.NaN : null;

        return kind switch
        {
            FieldKind.Float => value.GetSingle(),
            FieldKind.Double => value.GetDouble(),
            FieldKind.Bool => value.GetBoolean(),
            // An int where it fits, which every drawer expects, and a long where a u64 does not.
            FieldKind.Int => value.TryGetInt32(out var small) ? small : value.GetInt64(),
            FieldKind.String => value.GetString(),
            FieldKind.Vec2 => new Vec2(Part(value, 0, "x"), Part(value, 1, "y")),
            FieldKind.Vec3 => new Vec3(Part(value, 0, "x"), Part(value, 1, "y"), Part(value, 2, "z")),
            FieldKind.Vec4 => new Vec4(
                Part(value, 0, "x"), Part(value, 1, "y"), Part(value, 2, "z"), Part(value, 3, "w")),
            FieldKind.Quat => new Quat(
                Part(value, 0, "x"), Part(value, 1, "y"), Part(value, 2, "z"), Part(value, 3, "w")),
            FieldKind.Entity => new Entity(value.GetUInt64()),
            _ => json,
        };
    }

    /// <summary>One number of a vector, which glam writes as an array and reflection as an object.</summary>
    /// <remarks>
    /// glam's serde support is a feature of its own, and without it Bevy serializes a vector
    /// through its reflected fields instead. Reading both keeps a build that differs in that one
    /// feature from showing every vector as absent.
    /// </remarks>
    private static float Part(JsonElement value, int index, string name) =>
        value.ValueKind == JsonValueKind.Array
            ? value[index].GetSingle()
            : value.GetProperty(name).GetSingle();

    /// <summary>
    /// Writes a value as the JSON its kind takes, or <see langword="null"/> when the value does not
    /// fit the kind.
    /// </summary>
    /// <remarks>
    /// The value is coerced first, because a slider hands a float field a double and a text box
    /// hands it a string. <see cref="Utf8JsonWriter"/> formats a number in the invariant culture,
    /// and a number JSON cannot hold (an infinity, a NaN) is refused here rather than by the bridge.
    /// </remarks>
    internal static string? Encode(FieldKind kind, object value)
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        using (var json = new Utf8JsonWriter(buffer))
        {
            switch (kind)
            {
                case FieldKind.Float when ComponentSchemas.TryCoerce<float>(value, out var number)
                    && float.IsFinite(number):
                    json.WriteNumberValue(number);
                    break;
                case FieldKind.Double when ComponentSchemas.TryCoerce<double>(value, out var number)
                    && double.IsFinite(number):
                    json.WriteNumberValue(number);
                    break;
                case FieldKind.Int when ComponentSchemas.TryCoerce<long>(value, out var whole):
                    json.WriteNumberValue(whole);
                    break;
                case FieldKind.Bool when ComponentSchemas.TryCoerce<bool>(value, out var on):
                    json.WriteBooleanValue(on);
                    break;
                case FieldKind.String when value is string text:
                    json.WriteStringValue(text);
                    break;
                case FieldKind.Vec2 when value is Vec2 flat:
                    json.WriteStartArray();
                    json.WriteNumberValue(flat.X);
                    json.WriteNumberValue(flat.Y);
                    json.WriteEndArray();
                    break;
                case FieldKind.Vec4 when value is Vec4 four:
                    json.WriteStartArray();
                    json.WriteNumberValue(four.X);
                    json.WriteNumberValue(four.Y);
                    json.WriteNumberValue(four.Z);
                    json.WriteNumberValue(four.W);
                    json.WriteEndArray();
                    break;
                case FieldKind.Vec3 when value is Vec3 v:
                    json.WriteStartArray();
                    json.WriteNumberValue(v.X);
                    json.WriteNumberValue(v.Y);
                    json.WriteNumberValue(v.Z);
                    json.WriteEndArray();
                    break;
                case FieldKind.Quat when value is Quat q:
                    json.WriteStartArray();
                    json.WriteNumberValue(q.X);
                    json.WriteNumberValue(q.Y);
                    json.WriteNumberValue(q.Z);
                    json.WriteNumberValue(q.W);
                    json.WriteEndArray();
                    break;
                case FieldKind.Entity when value is Entity e:
                    json.WriteNumberValue(e.Bits);
                    break;
                default:
                    return null;
            }
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Reads a JSON array as a list of values of one kind, or nothing when it is not one.</summary>
    internal static ListValue? DecodeList(string json, FieldKind item)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return null;

        var items = new List<object?>();
        foreach (var element in document.RootElement.EnumerateArray())
            items.Add(Decode(element.GetRawText(), item));
        return new ListValue(items);
    }

    /// <summary>
    /// Writes a list of values of one kind as a JSON array, or <see langword="null"/> when an item
    /// does not fit the kind.
    /// </summary>
    internal static string? EncodeList(ListValue list, FieldKind item)
    {
        var parts = new List<string>(list.Count);
        foreach (var value in list)
        {
            if (value is null || Encode(item, value) is not { } part) return null;
            parts.Add(part);
        }

        return "[" + string.Join(",", parts) + "]";
    }

    // -- What the generated wrappers call

    /// <summary>Reads a field a wrapper names, throwing when the component is absent.</summary>
    internal static T Get<T>(EcsWorld world, Entity entity, string type, string path, FieldKind kind)
    {
        var json = world.GetReflected(entity, type, path) ?? throw Absent(type, entity);
        return Decode(json, kind) switch
        {
            T exact => exact,
            // A whole number decodes as an int or a long by its size, and the wrapper's property is
            // whatever width the Rust field has.
            IConvertible number => (T)Convert.ChangeType(
                number, typeof(T), System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new BevyNativeException(
                NativeStatus.InvalidState, $"'{path}' of {type} on {entity} holds no value."),
        };
    }

    /// <summary>Writes a field a wrapper names, throwing with the bridge's reason on a refusal.</summary>
    internal static void Set(
        EcsWorld world, Entity entity, string type, string path, FieldKind kind, object value)
    {
        var json = Encode(kind, value) ?? throw new ArgumentException(
            $"{value} cannot be written to '{path}' of {type}.", nameof(value));
        world.SetReflected(entity, type, path, json);
    }

    /// <summary>Reads the variant an enum field a wrapper names holds.</summary>
    internal static string Variant(EcsWorld world, Entity entity, string type, string path) =>
        world.GetVariant(entity, type, path) ?? throw Absent(type, entity);

    /// <summary>Reads an asset handle a wrapper names.</summary>
    internal static AssetHandle Asset(EcsWorld world, Entity entity, string type, string path) =>
        world.GetReflectedAsset(entity, type, path) ?? throw Absent(type, entity);

    /// <summary>Reads a color a wrapper names, as linear RGBA.</summary>
    internal static Color Color(EcsWorld world, Entity entity, string type, string path) =>
        world.GetReflectedColor(entity, type, path) ?? throw Absent(type, entity);

    /// <summary>The failure for a wrapper over a component the entity no longer carries.</summary>
    private static BevyNativeException Absent(string type, Entity entity) =>
        new(NativeStatus.NotPresent, $"{entity} does not carry {type}.");
}

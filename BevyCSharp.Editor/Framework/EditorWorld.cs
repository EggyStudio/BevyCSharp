using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Writes what the editor knows about the world to a file, and reads it back.
/// </summary>
/// <remarks>
/// <para>
/// The world being edited is meant to end up an asset that keeps its edits, and this is the half
/// of that which can be honest today. It saves what this side can describe: an entity's name, and
/// every component with a <see cref="ComponentSchema"/>, field by field.
/// </para>
/// <para>
/// <b>What it does not save.</b> A component the engine owns and this side has no schema for, a
/// mesh handle or a material or a camera's projection, is not written, because nothing here can
/// read its fields. Those come back from the code that spawned them. So this is a file of edits over a
/// scene rather than the scene itself, and it says so: a saved entity is matched back up by name.
/// </para>
/// <para>
/// Deliberately not Bevy's own world serialization, which is compiled in and would write the
/// engine's reflected components properly. It cannot see a C# component at all: those are bytes
/// registered at runtime with no Rust type behind them, so a file written that way would be
/// missing everything the game itself put in the world. Between a file that keeps the engine's
/// half and one that keeps the program's half, the program's half is the one an editor changed.
/// </para>
/// </remarks>
public static class EditorWorld
{
    /// <summary>How the file is written: indented, because it is meant to be read and diffed.</summary>
    private static readonly JsonSerializerOptions Layout = new() { WriteIndented = true };

    /// <summary>UTF-8 without a byte order mark, which nothing here wants and git shows.</summary>
    private static readonly UTF8Encoding Text = new(encoderShouldEmitUTF8Identifier: false);



    /// <summary>
    /// Writes every named entity and its described components.
    /// </summary>
    /// <remarks>
    /// Named entities only. An unnamed one cannot be matched back up on load, and writing it
    /// would produce a file that grows every time it is saved and restores nothing.
    /// </remarks>
    /// <returns>How many entities were written.</returns>
    public static int Save(EcsWorld world, string path)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var entities = new JsonArray();

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) is not { } name) continue;
            if (EditorEntity.IsInterface(world, entity)) continue;

            var components = new JsonObject();

            foreach (var id in world.ComponentsOf(entity))
            {
                if (ComponentSchemas.For(id) is not { } schema) continue;

                var fields = new JsonObject();
                foreach (var field in schema.Fields)
                {
                    if (field.Read(world, entity) is not { } value) continue;
                    fields[field.Name] = Write(value);
                }

                if (fields.Count > 0) components[schema.QualifiedName] = fields;
            }

            if (components.Count == 0) continue;

            entities.Add(new JsonObject
            {
                ["name"] = name,
                ["components"] = components,
            });
        }

        var document = new JsonObject
        {
            ["format"] = "bevycsharp.world.1",
            ["entities"] = entities,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, document.ToJsonString(Layout), Text);

        return entities.Count;
    }

    /// <summary>
    /// Reads a saved world back onto the entities that are already there.
    /// </summary>
    /// <remarks>
    /// Applied by name rather than by spawning: the entities exist because something spawned
    /// them, and what was saved is the edits made to them. An entry naming an entity that is not
    /// in the world is skipped rather than reported, since a scene that changed under a saved
    /// file is ordinary and not an error.
    /// </remarks>
    /// <returns>How many entities were matched and applied.</returns>
    public static int Load(EcsWorld world, string path)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!File.Exists(path)) return 0;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject document) return 0;
        if (document["entities"] is not JsonArray saved) return 0;

        // One pass over the world to find what each name is, rather than one pass per entry.
        var byName = new Dictionary<string, Entity>();
        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) is { } name) byName[name] = entity;
        }

        var applied = 0;

        foreach (var entry in saved)
        {
            if (entry is not JsonObject fields) continue;
            if (fields["name"]?.GetValue<string>() is not { } name) continue;
            if (!byName.TryGetValue(name, out var entity)) continue;
            if (fields["components"] is not JsonObject components) continue;

            foreach (var (type, values) in components)
            {
                if (ComponentSchemas.For(type) is not { } schema) continue;
                if (values is not JsonObject written) continue;

                foreach (var (field, value) in written)
                {
                    if (value is null) continue;
                    if (schema.Field(field) is not { } target) continue;

                    schema.Write(world, entity, field, Read(target, value));
                }
            }

            applied++;
        }

        return applied;
    }

    /// <summary>A field's value as the file holds it.</summary>
    /// <remarks>
    /// Numbers stay numbers and everything with parts is written as text, because a vector split
    /// into three lines of JSON is longer than the entity it belongs to and no easier to read.
    /// </remarks>
    private static JsonNode Write(object value) => value switch
    {
        bool flag => JsonValue.Create(flag),
        float number => JsonValue.Create(number),
        double number => JsonValue.Create(number),
        int number => JsonValue.Create(number),
        Vec3 vector => JsonValue.Create(
            $"{Number(vector.X)}, {Number(vector.Y)}, {Number(vector.Z)}"),
        Quat rotation => JsonValue.Create(
            $"{Number(rotation.X)}, {Number(rotation.Y)}, {Number(rotation.Z)}, {Number(rotation.W)}"),
        _ => JsonValue.Create(value.ToString() ?? string.Empty),
    };

    /// <summary>A saved value as the field's own kind, ready to be written back.</summary>
    private static object Read(ComponentField field, JsonNode value) => field.Kind switch
    {
        FieldKind.Vec3 when Parts(value, 3) is { } parts => new Vec3(parts[0], parts[1], parts[2]),
        FieldKind.Quat when Parts(value, 4) is { } parts =>
            new Quat(parts[0], parts[1], parts[2], parts[3]),

        // Everything else is a number, a flag or a name, and the field itself takes one of those
        // from whatever the file held.
        _ => value.GetValueKind() switch
        {
            JsonValueKind.True or JsonValueKind.False => value.GetValue<bool>(),
            JsonValueKind.Number => value.GetValue<double>(),
            _ => value.ToString(),
        },
    };

    /// <summary>The numbers in a saved "x, y, z", or nothing when it is not one.</summary>
    private static float[]? Parts(JsonNode value, int wanted)
    {
        var parts = value.ToString().Split(',');
        if (parts.Length != wanted) return null;

        var numbers = new float[wanted];
        for (var i = 0; i < wanted; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                return null;
        }

        return numbers;
    }

    /// <summary>A number as the file writes it, which is the same everywhere.</summary>
    private static string Number(float value) =>
        value.ToString("0.#####", CultureInfo.InvariantCulture);
}

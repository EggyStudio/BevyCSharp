using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What the selected entity carries, and what its fields are set to.
/// </summary>
/// <remarks>
/// <para>
/// The payoff of everything under it. The bridge answers what an entity carries in component ids
/// and can name one; the generator emits a field table per component; this puts the two together
/// and gets an editable row without a single type being named here. An entity carrying a
/// component this editor has never heard of shows a heading and no rows, which is the truthful
/// answer rather than a blank.
/// </para>
/// <para>
/// The rows are a pool, as in the hierarchy, and each one is either a component heading or a
/// field of the component above it.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/inspector.html",
    Root = "#inspector",
    Handle = "#inspector-title",
    Region = EditorRegion.TopRight,
    Y = 34f)]
public sealed partial class InspectorPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 24;

    /// <summary>Each row's label: a component's name, or a field's.</summary>
    [Bind("#fname", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>Each field row's value, as text, which is what an edit arrives as.</summary>
    [Bind("#fval", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#frow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Which rows have something to edit, as opposed to being a heading.</summary>
    [Show("#fval", Count = Rows)]
    public bool[] Editable = new bool[Rows];

    /// <summary>Which entity is being shown.</summary>
    [Bind("#insp-entity", Mode = BindMode.OneWay)]
    public string Subject { get; private set; } = string.Empty;

    /// <summary>What each row writes to, or nothing for a heading.</summary>
    private readonly (ComponentSchema? Schema, ComponentField? Field)[] _rows =
        new (ComponentSchema?, ComponentField?)[Rows];

    /// <summary>The entity the rows were filled from.</summary>
    private Entity _subject = Entity.None;

    /// <summary>Which row holds the entity's name, or -1 when it has none.</summary>
    private int _named = -1;

    /// <summary>Fills the rows from whatever is selected.</summary>
    [OnRefresh]
    public void Fill()
    {
        var world = EditorShell.Ecs;
        var entity = EditorSelection.Current;
        _subject = entity;

        var written = 0;
        _named = -1;

        if (!entity.IsNone)
        {
            Subject = world.NameOf(entity) is { } name
                ? $"{name}  {entity.Index}"
                : $"entity {entity.Index}";

            foreach (var id in world.ComponentsOf(entity))
            {
                if (written >= Rows) break;

                var schema = ComponentSchemas.For(id);
                var heading = schema?.Name ?? Short(world.ComponentName(id));

                Write(ref written, heading, string.Empty, editable: false, null, null);

                // Bevy's Name holds a string, so it has no schema and never will: the generic
                // component API carries blittable structs. It is also the one field of an entity
                // that exists for the person looking at it, so it gets a row of its own.
                if (schema is null && heading == "Name")
                {
                    Write(ref written, " Name", world.NameOf(entity) ?? string.Empty,
                        editable: true, null, null);

                    _named = written - 1;
                    continue;
                }

                if (schema is null) continue;

                foreach (var field in schema.Fields)
                {
                    if (written >= Rows) break;

                    Write(
                        ref written,
                        " " + field.Name,
                        Format(field.Read(world, entity)),
                        field.IsWritable,
                        schema,
                        field);
                }
            }
        }
        else
        {
            Subject = "nothing selected";
        }

        for (var i = written; i < Rows; i++)
        {
            Names[i] = string.Empty;
            Values[i] = string.Empty;
            Shown[i] = false;
            Editable[i] = false;
            _rows[i] = (null, null);
        }
    }

    /// <summary>Writes an edited row back into the world.</summary>
    /// <remarks>
    /// Once a frame however many rows were touched, and only the rows that were: a row whose text
    /// did not change is not written, so an entity moved by a script is not fought over by an
    /// inspector writing the value it read last frame.
    /// </remarks>
    [OnChange]
    public void Apply()
    {
        if (_subject.IsNone) return;

        var world = EditorShell.Ecs;

        var subject = _subject;

        if (_named >= 0 && Values[_named] is { Length: > 0 } renamed
            && renamed != world.NameOf(subject))
        {
            var before = world.NameOf(subject);
            world.SetName(subject, renamed);

            EditorHistory.Record(
                $"rename to {renamed}",
                undo => undo.SetName(subject, before),
                redo => redo.SetName(subject, renamed),
                $"{subject.Bits}:name");
        }

        for (var i = 0; i < Rows; i++)
        {
            var (schema, field) = _rows[i];
            if (schema is null || field is null) continue;

            var current = field.Read(world, subject);
            if (Format(current) == Values[i]) continue;

            if (Parse(field, Values[i]) is not { } value) continue;
            if (!field.Write(world, subject, value)) continue;

            // Recorded after the write rather than before, so an edit that the field refused
            // leaves nothing in the history to take back.
            if (current is not null)
            {
                EditorHistory.Record(
                    $"{schema.Name}.{field.Name}",
                    undo => field.Write(undo, subject, current),
                    redo => field.Write(redo, subject, value),
                    $"{subject.Bits}:{schema.Name}.{field.Name}");
            }
        }
    }

    /// <summary>Fills in one row.</summary>
    private void Write(
        ref int row,
        string name,
        string value,
        bool editable,
        ComponentSchema? schema,
        ComponentField? field)
    {
        Names[row] = name;
        Values[row] = value;
        Shown[row] = true;
        Editable[row] = editable;
        _rows[row] = (schema, field);
        row++;
    }

    /// <summary>A value as a row shows it.</summary>
    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        float number => number.ToString("0.###"),
        double number => number.ToString("0.###"),
        Vec3 vector => $"{vector.X:0.###}, {vector.Y:0.###}, {vector.Z:0.###}",
        Quat rotation => $"{rotation.X:0.##}, {rotation.Y:0.##}, {rotation.Z:0.##}, {rotation.W:0.##}",
        Entity entity => entity.IsNone ? "none" : entity.Index.ToString(),
        bool flag => flag ? "true" : "false",
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>
    /// A row's text as the field's own kind, or nothing when it is not one yet.
    /// </summary>
    /// <remarks>
    /// Half-typed input is the ordinary case, not a fault: a person clearing a number field to
    /// type a new one leaves it empty for a keystroke. Nothing is written until the text is a
    /// value, and the field keeps what it had until then.
    /// </remarks>
    private static object? Parse(ComponentField field, string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return null;

        switch (field.Kind)
        {
            case FieldKind.Vec3:
                var parts = trimmed.Split(',');
                if (parts.Length != 3) return null;
                if (!float.TryParse(parts[0], out var x)) return null;
                if (!float.TryParse(parts[1], out var y)) return null;
                if (!float.TryParse(parts[2], out var z)) return null;
                return new Vec3(x, y, z);

            case FieldKind.Quat:
                var turns = trimmed.Split(',');
                if (turns.Length != 4) return null;
                if (!float.TryParse(turns[0], out var qx)) return null;
                if (!float.TryParse(turns[1], out var qy)) return null;
                if (!float.TryParse(turns[2], out var qz)) return null;
                if (!float.TryParse(turns[3], out var qw)) return null;
                return new Quat(qx, qy, qz, qw);

            // Everything else is a number, a flag or a name, and the field itself knows how to
            // take one of those from text.
            default:
                return trimmed;
        }
    }

    /// <summary>The last part of an engine component's path, which is its name.</summary>
    /// <remarks>
    /// Bevy reports a component by its Rust path, and a heading has room for the name.
    /// </remarks>
    private static string Short(string name)
    {
        // The arguments go first. A generic's arguments are paths too, so taking the last path
        // segment of the whole thing answers with the end of the argument rather than the name:
        // MeshMaterial3d<bevy_pbr::StandardMaterial> would come back as "StandardMaterial>".
        var generic = name.IndexOf('<');
        var bare = generic < 0 ? name : name[..generic];

        var cut = bare.LastIndexOf("::", StringComparison.Ordinal);
        return cut < 0 ? bare : bare[(cut + 2)..];
    }
}

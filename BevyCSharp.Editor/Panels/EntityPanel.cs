using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What the selected entity carries, a row per field, with an editor that suits the field.
/// </summary>
/// <remarks>
/// <para>
/// The payoff of everything under it. The bridge answers what an entity carries in component ids
/// and can name one; the generator emits a field table per component; this puts the two together
/// and gets an editable row without a single type being named here. An entity carrying a component
/// this editor has never heard of shows a heading and no rows, which is the truthful answer.
/// </para>
/// <para>
/// A row is not a text box. What a field is decides what is drawn: three boxes for a vector, a
/// checkbox for a flag, a button that opens the list for a choice, one box for a number, and a
/// button for a method. The document declares every one of those on every row and the panel shows
/// the ones that row needs, which is how a fixed document draws a shape it did not know about.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/entity.html",
    Root = "#entity",
    Handle = "#entity-title",
    Dock = EditorDock.Right,
    Fill = true)]
public sealed partial class EntityPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 22;

    /// <summary>What kind of thing a row stands for.</summary>
    private enum RowKind
    {
        /// <summary>Nothing.</summary>
        Empty,

        /// <summary>A component's name, with a button that takes it off.</summary>
        Heading,

        /// <summary>A field with one value: a number, a name, something with no better editor.</summary>
        Value,

        /// <summary>One axis of a vector, or one angle of a rotation.</summary>
        /// <remarks>
        /// A vector is three rows rather than three boxes on one row. The interface draws the text
        /// of only the first input of a row, whatever is written to the others, so three boxes
        /// side by side would show one number and two empty boxes. Three rows is the same three
        /// numbers, each in a box that draws.
        /// </remarks>
        Axis,

        /// <summary>A field that is on or off.</summary>
        Flag,

        /// <summary>A field that is one of a fixed set of names.</summary>
        Choice,

        /// <summary>Something the component can be told to do.</summary>
        Method,

        /// <summary>The entity's name, which is not a component this side can describe.</summary>
        Name,
    }

    /// <summary>What one row is, and what it writes to.</summary>
    /// <param name="Kind">Which editor the row draws.</param>
    /// <param name="Schema">The component it belongs to.</param>
    /// <param name="Field">The field it edits.</param>
    /// <param name="Method">The method it runs.</param>
    /// <param name="Component">The component id, for a heading with no schema.</param>
    /// <param name="Part">Which of a vector's three numbers, or -1.</param>
    private readonly record struct Row(
        RowKind Kind,
        ComponentSchema? Schema = null,
        ComponentField? Field = null,
        ComponentMethod? Method = null,
        int Component = 0,
        int Part = -1);

    /// <summary>Each row's label: a component's name, or a field's.</summary>
    [Bind("#ename", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>The single-value editor, for anything that is one number or one word.</summary>
    [Bind("#ev", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>The checkbox a flag row draws.</summary>
    [Bind("#ec", Count = Rows)]
    public bool[] Flags = new bool[Rows];

    /// <summary>What the row's button says, when it has one.</summary>
    [Bind("#eb", Count = Rows)]
    public string[] Buttons = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#erow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Which rows draw the single-value editor.</summary>
    [Show("#ev", Count = Rows)]
    public bool[] ShowValue = new bool[Rows];

    /// <summary>Which rows draw a checkbox.</summary>
    [Show("#ec", Count = Rows)]
    public bool[] ShowFlag = new bool[Rows];

    /// <summary>Which rows draw a button.</summary>
    [Show("#eb", Count = Rows)]
    public bool[] ShowButton = new bool[Rows];

    /// <summary>Which entity is being shown.</summary>
    [Bind("#e-subject", Mode = BindMode.OneWay)]
    public string Subject { get; private set; } = string.Empty;

    /// <summary>What each row stands for.</summary>
    private readonly Row[] _rows = new Row[Rows];

    /// <summary>Every row the selection has, of which the pool shows a screenful.</summary>
    private readonly List<Row> _all = [];

    /// <summary>The entity the rows were filled from.</summary>
    private Entity _subject = Entity.None;

    /// <summary>How far down the rows the pool is looking.</summary>
    private int _scroll;

    /// <summary>Fills the rows from whatever is selected.</summary>
    [OnRefresh]
    public void Fill()
    {
        var world = EditorShell.Ecs;
        var entity = EditorSelection.Current;

        Roll();

        if (entity != _subject)
        {
            _subject = entity;
            _scroll = 0;
        }

        _all.Clear();

        if (entity.IsNone)
        {
            Subject = "nothing selected";
            Blank(0);
            return;
        }

        Subject = world.NameOf(entity) is { } named ? named : $"entity {entity.Index}";
        _all.Add(new Row(RowKind.Name));

        foreach (var id in world.ComponentsOf(entity))
        {
            var schema = ComponentSchemas.For(id);
            if (schema is null)
            {
                // Named and nothing else. An engine component with no mirror on this side has
                // fields, and none of them can be read from here, so the row says what is there
                // rather than pretending otherwise.
                _all.Add(new Row(RowKind.Heading, Component: id));
                continue;
            }

            _all.Add(new Row(RowKind.Heading, schema, Component: id));

            foreach (var field in schema.Fields)
            {
                if (KindOf(field) != RowKind.Axis)
                {
                    _all.Add(new Row(KindOf(field), schema, field));
                    continue;
                }

                for (var part = 0; part < 3; part++)
                    _all.Add(new Row(RowKind.Axis, schema, field, Part: part));
            }

            foreach (var method in schema.Methods)
            {
                _all.Add(new Row(RowKind.Method, schema, Method: method));
            }
        }

        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _all.Count - Rows));

        var written = 0;
        for (var i = _scroll; i < _all.Count && written < Rows; i++)
        {
            Write(written, _all[i], world, entity);
            written++;
        }

        Blank(written);
    }

    /// <summary>Which editor a field's kind gets.</summary>
    private static RowKind KindOf(ComponentField field) => field.Kind switch
    {
        FieldKind.Vec3 or FieldKind.Quat => RowKind.Axis,
        FieldKind.Bool => RowKind.Flag,
        FieldKind.Enum => RowKind.Choice,
        _ => RowKind.Value,
    };

    /// <summary>What each of a vector's three numbers is called.</summary>
    private static readonly string[] AxisNames = ["x", "y", "z"];

    /// <summary>Fills in one row, drawing only the parts that row needs.</summary>
    private void Write(int row, Row what, EcsWorld world, Entity entity)
    {
        _rows[row] = what;

        Shown[row] = true;
        ShowValue[row] = false;
        ShowFlag[row] = false;
        ShowButton[row] = false;
        Values[row] = string.Empty;
        Buttons[row] = string.Empty;
        Flags[row] = false;

        switch (what.Kind)
        {
            case RowKind.Name:
                Names[row] = "Name";
                Values[row] = world.NameOf(entity) ?? string.Empty;
                ShowValue[row] = true;
                break;

            case RowKind.Heading:
                Names[row] = what.Schema?.Name ?? Short(world.ComponentName(what.Component));
                Buttons[row] = EditorIcons.Remove;
                ShowButton[row] = what.Schema is not null;
                break;

            case RowKind.Method:
                Names[row] = "  " + (what.Method?.Name ?? string.Empty);
                Buttons[row] = EditorIcons.Run;
                ShowButton[row] = true;
                break;

            case RowKind.Flag:
                Names[row] = "  " + what.Field!.Name;
                Flags[row] = what.Field.Read(world, entity) is bool set && set;
                ShowFlag[row] = true;
                break;

            case RowKind.Choice:
                Names[row] = "  " + what.Field!.Name;
                Buttons[row] = what.Field.Read(world, entity)?.ToString() ?? string.Empty;
                ShowButton[row] = true;
                break;

            case RowKind.Axis:
                // The field's name on the first of its three rows and the axis alone on the
                // others, so a column of numbers reads as one thing rather than as three.
                Names[row] = what.Part == 0
                    ? "  " + what.Field!.Name
                    : "     " + AxisNames[what.Part];

                Values[row] = Parts(what.Field!.Read(world, entity))[what.Part];
                ShowValue[row] = true;
                break;

            default:
                Names[row] = "  " + (what.Field?.Name ?? string.Empty);
                Values[row] = Format(what.Field?.Read(world, entity));
                ShowValue[row] = true;
                break;
        }
    }

    /// <summary>Empties the rows from <paramref name="from"/> down.</summary>
    private void Blank(int from)
    {
        for (var i = from; i < Rows; i++)
        {
            _rows[i] = default;
            Names[i] = string.Empty;
            Values[i] = string.Empty;
            Buttons[i] = string.Empty;
            Flags[i] = false;
            Shown[i] = false;
            ShowValue[i] = false;
            ShowFlag[i] = false;
            ShowButton[i] = false;
        }
    }

    /// <summary>Scrolls the rows when the wheel is rolled over the panel.</summary>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        _scroll = Math.Max(0, _scroll - ((int)wheel * 3));
    }

    /// <summary>Writes an edited row back into the world.</summary>
    /// <remarks>
    /// Once a frame however many rows were touched, and only the rows that changed: a row whose
    /// value still reads as what the world says is not written, so an entity being moved by a
    /// script is not fought over by an inspector writing back what it read last frame.
    /// </remarks>
    [OnChange]
    public void Apply()
    {
        if (_subject.IsNone) return;

        var world = EditorShell.Ecs;
        var entity = _subject;

        for (var i = 0; i < Rows; i++)
        {
            var row = _rows[i];

            switch (row.Kind)
            {
                case RowKind.Name:
                    var before = world.NameOf(entity);
                    var renamed = Values[i].Trim();
                    if (renamed.Length == 0 || renamed == before) break;

                    world.SetName(entity, renamed);
                    EditorHistory.Record(
                        $"rename to {renamed}",
                        undo => undo.SetName(entity, before),
                        redo => redo.SetName(entity, renamed),
                        $"{entity.Bits}:name");
                    break;

                case RowKind.Flag when row.Field is { } flag:
                    if (flag.Read(world, entity) is bool current && current == Flags[i]) break;

                    Change(world, entity, flag, Flags[i]);
                    break;

                case RowKind.Axis when row.Field is { } vector:
                    // The other two numbers come from what the field currently holds, so editing
                    // one axis changes one axis even when the other rows are scrolled away.
                    var edited = Parts(vector.Read(world, entity));
                    edited[row.Part] = Values[i].Trim();

                    if (Compose(vector, edited) is not { } composed) break;
                    if (Same(vector.Read(world, entity), composed)) break;

                    Change(world, entity, vector, composed);
                    break;

                case RowKind.Value when row.Field is { } value:
                    var typed = Values[i].Trim();
                    if (typed.Length == 0) break;
                    if (Format(value.Read(world, entity)) == typed) break;

                    Change(world, entity, value, typed);
                    break;
            }
        }
    }

    /// <summary>Writes one field and records how to take it back.</summary>
    private static void Change(EcsWorld world, Entity entity, ComponentField field, object value)
    {
        var before = field.Read(world, entity);
        if (!field.Write(world, entity, value)) return;
        if (before is null) return;

        EditorHistory.Record(
            field.Name,
            undo => field.Write(undo, entity, before),
            redo => field.Write(redo, entity, value),
            $"{entity.Bits}:{field.Name}");
    }

    /// <summary>Runs whatever the row's button offers.</summary>
    [Command("#eb", Count = Rows)]
    public void Press(int row)
    {
        var world = EditorShell.Ecs;
        var what = _rows[row];
        var entity = _subject;

        switch (what.Kind)
        {
            case RowKind.Heading when what.Schema is { } schema:
                if (!schema.Remove(world, entity)) return;

                EditorHistory.Record(
                    $"remove {schema.Name}",
                    undo => schema.Add(undo, entity),
                    redo => schema.Remove(redo, entity));
                return;

            case RowKind.Method when what.Method is { } method:
                method.Run(world, entity);
                return;

            case RowKind.Choice when what.Field is { } field:
                Choose(row, field, entity);
                return;
        }
    }

    /// <summary>Opens the list of names a choice field can take, under the button that asks.</summary>
    private void Choose(int row, ComponentField field, Entity entity)
    {
        var items = new List<MenuItem>();

        foreach (var option in field.Options)
        {
            var chosen = option;

            items.Add(new MenuItem(
                option,
                MenuKind.Command,
                world =>
                {
                    var before = field.Read(world, entity);
                    if (!field.Write(world, entity, chosen)) return;
                    if (before is null) return;

                    EditorHistory.Record(
                        $"{field.Name} to {chosen}",
                        undo => field.Write(undo, entity, before),
                        redo => field.Write(redo, entity, chosen));
                }));
        }

        var (x, y) = Under($"eb-{row}");
        EditorShell.ShowMenu(field.Name, items, x, y);
    }

    /// <summary>Offers the components that can be put on the selection.</summary>
    [Command("#e-add")]
    public void AddComponent()
    {
        if (_subject.IsNone) return;

        var world = EditorShell.Ecs;
        var entity = _subject;
        var items = new List<MenuItem>();

        foreach (var schema in ComponentSchemas.All)
        {
            if (!schema.CanAdd) continue;

            var chosen = schema;
            var already = false;

            // A component cannot resolve an id on a build that has no such component, and a
            // schema for one is skipped rather than allowed to take the menu down with it.
            try
            {
                already = world.HasById(entity, schema.Id);
            }
            catch (Bevy.Interop.BevyNativeException)
            {
                continue;
            }

            if (already) continue;

            items.Add(new MenuItem(
                schema.Name,
                MenuKind.Command,
                w =>
                {
                    if (!chosen.Add(w, entity)) return;

                    EditorHistory.Record(
                        $"add {chosen.Name}",
                        undo => chosen.Remove(undo, entity),
                        redo => chosen.Add(redo, entity));
                }));
        }

        if (items.Count == 0)
        {
            items.Add(new MenuItem("nothing left to add", MenuKind.Separator));
        }

        var (x, y) = Under("e-add");
        EditorShell.ShowMenu("Add component", items, x, y);
    }

    /// <summary>Offers what can be done to the component a row belongs to.</summary>
    [Context("#erow", Count = Rows)]
    public void RowMenu(int row)
    {
        if (_rows[row].Schema is not { } schema) return;

        var entity = _subject;
        var (x, y) = EditorShell.Context?.Input.MousePosition ?? (0f, 0f);

        EditorShell.ShowMenu(
            schema.Name,
            [
                new MenuItem(
                    "Remove",
                    MenuKind.Command,
                    world =>
                    {
                        if (!schema.Remove(world, entity)) return;

                        EditorHistory.Record(
                            $"remove {schema.Name}",
                            undo => schema.Add(undo, entity),
                            redo => schema.Remove(redo, entity));
                    }),
            ],
            x,
            y);
    }

    /// <summary>Where a menu opened from one of this panel's elements should sit.</summary>
    private (float X, float Y) Under(string element)
    {
        if (Window is { } window && Xui.TryRect(window.Element(element), out var rect))
            return (rect.X, rect.Bottom + 4f);

        return EditorShell.Context?.Input.MousePosition ?? (0f, 0f);
    }

    /// <summary>A value as a single box shows it.</summary>
    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        float number => number.ToString("0.###"),
        double number => number.ToString("0.###"),
        Entity entity => entity.IsNone ? "none" : entity.Index.ToString(),
        bool flag => flag ? "true" : "false",
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>
    /// A vector or a rotation as the three boxes show it.
    /// </summary>
    /// <remarks>
    /// A rotation is shown as the three angles it turns through, in degrees, because four numbers
    /// that must stay normalised are not something a person can usefully type.
    /// </remarks>
    private static string[] Parts(object? value) => value switch
    {
        Vec3 vector => [Number(vector.X), Number(vector.Y), Number(vector.Z)],
        Quat rotation => Angles(rotation),
        _ => ["", "", ""],
    };

    /// <summary>A rotation as three angles in degrees.</summary>
    private static string[] Angles(Quat rotation)
    {
        var euler = rotation.ToEuler();
        const float ToDegrees = 180f / MathF.PI;

        return
        [
            Number(euler.X * ToDegrees),
            Number(euler.Y * ToDegrees),
            Number(euler.Z * ToDegrees),
        ];
    }

    /// <summary>The value three numbers describe, or nothing when they are not one yet.</summary>
    private static object? Compose(ComponentField field, string[] parts)
    {
        if (!float.TryParse(parts[0], out var first)) return null;
        if (!float.TryParse(parts[1], out var second)) return null;
        if (!float.TryParse(parts[2], out var third)) return null;

        if (field.Kind != FieldKind.Quat) return new Vec3(first, second, third);

        const float ToRadians = MathF.PI / 180f;
        return Quat.FromEuler(first * ToRadians, second * ToRadians, third * ToRadians);
    }

    /// <summary>Whether a value is close enough to what is on screen to leave alone.</summary>
    /// <remarks>
    /// Rounded to what the boxes actually show, because a box showing three decimals is not a
    /// disagreement with a value that has more of them, and writing back on every frame would
    /// fight anything else moving the entity.
    /// </remarks>
    private static bool Same(object? current, object composed) => (current, composed) switch
    {
        (Vec3 a, Vec3 b) => Number(a.X) == Number(b.X)
            && Number(a.Y) == Number(b.Y)
            && Number(a.Z) == Number(b.Z),
        (Quat a, Quat b) => Angles(a).SequenceEqual(Angles(b)),
        _ => false,
    };

    /// <summary>A number as a box shows it.</summary>
    private static string Number(float value) => value.ToString("0.###");

    /// <summary>The last part of an engine component's path, which is its name.</summary>
    private static string Short(string name)
    {
        // The arguments go first. A generic's arguments are paths too, so taking the last path
        // segment of the whole thing answers with the end of the argument rather than the name.
        var generic = name.IndexOf('<');
        var bare = generic < 0 ? name : name[..generic];

        var cut = bare.LastIndexOf("::", StringComparison.Ordinal);
        return cut < 0 ? bare : bare[(cut + 2)..];
    }
}

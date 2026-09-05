using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// Whatever is selected, in detail: an entity's components or an asset's particulars.
/// </summary>
/// <remarks>
/// <para>
/// One panel rather than one per kind of thing. What a person wants when they pick something is
/// to see what it is, and which sort of thing it happens to be should not move where the answer
/// appears. The panel changes its heading and its rows; it does not change its place.
/// </para>
/// <para>
/// It owns a pool of rows and nothing else. A row is claimed by a field and filled by whichever
/// drawer takes that field, so what a number looks like, what a vector looks like and what a flag
/// looks like are three small classes rather than three branches here. Adding a way to edit a new
/// kind of value adds a drawer, and this file does not change.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/data.html",
    Root = "#data",
    Dock = EditorDock.Right)]
public sealed partial class DataPanel : IInspectorRows
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 24;

    /// <summary>How many tag chips it declares.</summary>
    public const int Chips = 24;

    /// <summary>What a line of the inspector stands for.</summary>
    private enum LineKind
    {
        /// <summary>Nothing.</summary>
        Empty,

        /// <summary>A component's name. It folds, and offers what can be done to the component.</summary>
        Heading,

        /// <summary>The entity's name, which is not a component this side can describe.</summary>
        Name,

        /// <summary>One row of one field, drawn by whichever drawer took it.</summary>
        Field,

        /// <summary>Something the component can be told to do.</summary>
        Method,

        /// <summary>Something about an asset, which is read and not edited.</summary>
        Fact,
    }

    /// <summary>One line of the inspector, before it is given a row.</summary>
    /// <param name="Kind">What the line stands for.</param>
    /// <param name="Schema">The component it belongs to.</param>
    /// <param name="Field">The field it edits.</param>
    /// <param name="Drawer">What draws that field, and reads it back.</param>
    /// <param name="Part">Which of the drawer's rows this is.</param>
    /// <param name="Method">The method it runs.</param>
    /// <param name="Component">The component id, for a heading with no schema.</param>
    private readonly record struct Line(
        LineKind Kind,
        ComponentSchema? Schema = null,
        ComponentField? Field = null,
        IFieldDrawer? Drawer = null,
        int Part = 0,
        ComponentMethod? Method = null,
        int Component = 0);

    /// <summary>Each row's label.</summary>
    [Bind("#dname", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>What is in each row's box.</summary>
    [Bind("#dv", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>What each row's handle says, which is usually nothing.</summary>
    [Bind("#dgt", Count = Rows)]
    public string[] Letters = new string[Rows];

    /// <summary>Each row's tick.</summary>
    [Bind("#dc", Count = Rows)]
    public bool[] Flags = new bool[Rows];

    /// <summary>What each row's button says.</summary>
    [Bind("#dbtext", Count = Rows)]
    public string[] Buttons = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#drow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Which rows show a box.</summary>
    [Show("#dnum", Count = Rows)]
    public bool[] ShowValue = new bool[Rows];

    /// <summary>Which boxes have a handle beside them, which is which of them are numbers.</summary>
    [Show("#dg", Count = Rows)]
    public bool[] ShowGrip = new bool[Rows];

    /// <summary>Which rows show a mark at the start, which is which of them are headings.</summary>
    [Show("#dfold", Count = Rows)]
    public bool[] ShowMark = new bool[Rows];

    /// <summary>Which rows show a tick.</summary>
    [Show("#dc", Count = Rows)]
    public bool[] ShowFlag = new bool[Rows];

    /// <summary>Which rows show a button.</summary>
    [Show("#db", Count = Rows)]
    public bool[] ShowButton = new bool[Rows];

    /// <summary>What each chip says.</summary>
    [Bind("#dchiptext", Count = Chips)]
    public string[] Tags = new string[Chips];

    /// <summary>The word over the strip, which goes away when there is no strip.</summary>
    [Show("#d-tags")]
    public bool AnyTags;

    /// <summary>Which chips stand for anything.</summary>
    [Show("#dchip", Count = Chips)]
    public bool[] TagShown = new bool[Chips];

    /// <summary>What sort of thing is being shown.</summary>
    [Bind("#d-kind", Mode = BindMode.OneWay)]
    public string Kind { get; private set; } = "Data";

    /// <summary>Which one.</summary>
    [Bind("#d-subject", Mode = BindMode.OneWay)]
    public string Subject { get; private set; } = string.Empty;

    /// <summary>What each row stands for.</summary>
    private readonly Line[] _lines = new Line[Rows];

    /// <summary>What picture each row's mark wears, so it is written once.</summary>
    private readonly string[] _marks = new string[Rows];

    /// <summary>Which components are shut, by component id.</summary>
    /// <remarks>
    /// By id rather than by name, because that is what the world answers with and what the rows
    /// already carry. It outlives the selection on purpose: somebody who shuts a component meant
    /// it about every entity they are going to look at, not only this one.
    /// </remarks>
    private readonly HashSet<int> _shut = [];

    /// <summary>What each chip stands for: its schema when there is one, and its id.</summary>
    private readonly (ComponentSchema? Schema, int Component)[] _tags =
        new (ComponentSchema?, int)[Chips];

    /// <summary>Every line the selection has, of which the pool shows a screenful.</summary>
    private readonly List<Line> _all = [];

    /// <summary>The entity the rows were filled from.</summary>
    private Entity _subject = Entity.None;

    /// <summary>How far down the lines the pool is looking.</summary>
    private int _scroll;

    /// <summary>Fills the rows from whatever is selected.</summary>
    [OnRefresh]
    public void Fill()
    {
        Roll();

        if (EditorShell.Context is { } ctx) Scrub(ctx.Input);

        if (EditorSelection.Latest == SelectionKind.Asset)
        {
            FillAsset();
            return;
        }

        FillEntity();
    }

    /// <summary>Shows what an entity carries.</summary>
    private void FillEntity()
    {
        var world = EditorShell.Ecs;
        var entity = EditorSelection.Current;

        if (entity != _subject)
        {
            _subject = entity;
            _scroll = 0;
        }

        Kind = "Entity";
        _all.Clear();

        if (entity.IsNone)
        {
            Subject = "nothing selected";
            Untag(0);
            Blank(0);
            return;
        }

        Subject = world.NameOf(entity) is { } named ? named : $"entity {entity.Index}";
        _all.Add(new Line(LineKind.Name));

        var tags = 0;

        foreach (var id in world.ComponentsOf(entity))
        {
            var schema = ComponentSchemas.For(id);

            // A component with nothing to show is a tag, and a tag belongs on a chip rather than
            // under a heading with an empty space beneath it. Two things end up here: a marker
            // with no fields, and one this side has no description of, and to somebody reading
            // the panel they are the same statement, which is "this is on it, and that is all I
            // can tell you".
            if (schema is null || schema.Fields.Count == 0)
            {
                // Except the engine's own, which are on everything and say nothing about this
                // entity in particular.
                if (schema is null && EditorEntity.IsDerived(world, id)) continue;

                if (tags < Chips)
                {
                    Tags[tags] = schema?.Name ?? Short(world.ComponentName(id));
                    TagShown[tags] = true;
                    _tags[tags] = (schema, id);
                    tags++;
                }

                continue;
            }

            _all.Add(new Line(LineKind.Heading, schema, Component: id));

            // Shut is shut: what a component block is for is being able to put away the ones you
            // are not working on, and an inspector where you cannot is a column of scrolling.
            if (_shut.Contains(id)) continue;

            foreach (var field in schema.Fields)
            {
                var drawer = EditorDrawers.For(field);

                for (var part = 0; part < drawer.Lines(field); part++)
                    _all.Add(new Line(LineKind.Field, schema, field, drawer, part));
            }

            foreach (var method in schema.Methods)
                _all.Add(new Line(LineKind.Method, schema, Method: method));
        }

        Untag(tags);
        AnyTags = tags > 0;

        Draw(world, entity);
    }

    /// <summary>Empties the chips from <paramref name="from"/> on.</summary>
    private void Untag(int from)
    {
        for (var i = from; i < Chips; i++)
        {
            Tags[i] = string.Empty;
            TagShown[i] = false;
            _tags[i] = (null, 0);
        }

        if (from == 0) AnyTags = false;
    }

    /// <summary>Shows what a file is.</summary>
    private void FillAsset()
    {
        Kind = "Asset";
        _all.Clear();
        _subject = Entity.None;

        Untag(0);

        if (EditorAssets.Selected is not { } relative)
        {
            Subject = "nothing selected";
            Blank(0);
            return;
        }

        Subject = Path.GetFileName(relative);

        var file = new FileInfo(EditorAssets.Absolute(relative));
        var written = 0;

        Fact(ref written, "Path", relative);
        Fact(ref written, "Kind", EditorAssets.KindOf(relative));

        if (file.Exists)
        {
            Fact(ref written, "Size", Size(file.Length));
            Fact(ref written, "Changed", file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
        }
        else
        {
            Fact(ref written, "State", "gone from disk");
        }

        Fact(
            ref written,
            "Reloads",
            EditorAssets.Reloads(relative) ? "yes, while running" : "no");

        // The path the engine would be given, which is the one to type into a document or a
        // script. Worth showing because it is not the same as the path on disk.
        Fact(ref written, "Load as", relative);

        Blank(written);
    }

    /// <summary>Writes one thing that is read and not edited.</summary>
    private void Fact(ref int row, string name, string value)
    {
        if (row >= Rows) return;

        Empty(row);
        _lines[row] = new Line(LineKind.Fact);
        Shown[row] = true;
        Name(row, name);
        Box(row, value, null);
        row++;
    }

    /// <summary>Puts a screenful of lines into the rows.</summary>
    private void Draw(EcsWorld world, Entity entity)
    {
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _all.Count - Rows));

        var written = 0;
        for (var i = _scroll; i < _all.Count && written < Rows; i++)
        {
            Write(written, _all[i], world, entity);
            written++;
        }

        Blank(written);
    }

    /// <summary>Fills one row, showing only the pieces that line needs.</summary>
    private void Write(int row, Line line, EcsWorld world, Entity entity)
    {
        Empty(row);

        _lines[row] = line;
        Shown[row] = true;
        _under[row] = line.Kind is LineKind.Field or LineKind.Method;

        switch (line.Kind)
        {
            case LineKind.Name:
                Name(row, "Name");
                Box(row, world.NameOf(entity) ?? string.Empty, null);
                break;

            case LineKind.Heading:
                Name(row, line.Schema?.Name ?? Short(world.ComponentName(line.Component)));
                Mark(row, _shut.Contains(line.Component)
                    ? "icons/ui/next.png"
                    : "icons/ui/down.png");

                break;

            case LineKind.Method:
                Name(row, line.Method?.Name ?? string.Empty);
                Button(row, EditorIcons.Run);
                break;

            case LineKind.Field when line is { Field: { } field, Drawer: { } drawer }:
                drawer.Draw(
                    new InspectorRow(this, row), line.Part, new FieldTarget(field, world, entity));

                break;
        }
    }

    /// <summary>Takes back whatever the last thing in a row left showing.</summary>
    private void Empty(int row)
    {
        _lines[row] = default;
        _under[row] = false;
        Names[row] = string.Empty;
        Values[row] = string.Empty;
        Letters[row] = string.Empty;
        Buttons[row] = string.Empty;
        Flags[row] = false;
        Shown[row] = false;
        ShowValue[row] = false;
        ShowGrip[row] = false;
        ShowMark[row] = false;
        ShowFlag[row] = false;
        ShowButton[row] = false;
    }

    /// <summary>Empties the rows from <paramref name="from"/> down.</summary>
    private void Blank(int from)
    {
        for (var i = from; i < Rows; i++) Empty(i);
    }

    /// <inheritdoc/>
    public void Mark(int row, string? icon)
    {
        ShowMark[row] = icon is not null;
        if (icon is null) return;

        Point(row, "dfold", _marks, icon);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A field's name is set in from a component's, so a block reads as a block. Which rows are
    /// set in is the panel's business rather than a drawer's: a drawer says what its row is
    /// called, and where that sits depends on what the row is under.
    /// </remarks>
    public void Name(int row, string text) =>
        Names[row] = _under[row] && text.Length > 0 ? "  " + text : text;

    /// <summary>Which rows sit under a heading.</summary>
    private readonly bool[] _under = new bool[Rows];

    /// <inheritdoc/>
    public void Box(int row, string value, Grip? grip)
    {
        Values[row] = value;
        ShowValue[row] = true;
        ShowGrip[row] = grip is not null;
        Letters[row] = grip?.Letter ?? string.Empty;

        if (grip is not { } paint) return;
        if (Window is not { IsOpen: true } window) return;

        var found = window.Element($"dg-{row}");
        if (found.IsNone) return;

        // Painted every frame rather than when it changes. The interface writes this component
        // too, whenever it restyles the widget, and a handle painted once goes back to nothing the
        // next time anything in the row is touched.
        Xui.SetColour(found, paint.Red, paint.Green, paint.Blue);
    }

    /// <inheritdoc/>
    public void Tick(int row, bool on)
    {
        Flags[row] = on;
        ShowFlag[row] = true;
    }

    /// <inheritdoc/>
    public void Button(int row, string text)
    {
        Buttons[row] = text;
        ShowButton[row] = true;
    }

    /// <inheritdoc/>
    public string Typed(int row) => Values[row];

    /// <inheritdoc/>
    public bool Ticked(int row) => Flags[row];

    /// <inheritdoc/>
    public (float X, float Y) Below(int row) => Under($"db-{row}");

    /// <summary>
    /// Points a row's mark at a file.
    /// </summary>
    /// <remarks>
    /// An image is a path the interface loads from rather than a value a widget carries, so it is
    /// set rather than bound. Remembered per row so the same path is not written every frame.
    /// </remarks>
    private void Point(int row, string element, string[] worn, string icon)
    {
        if (worn[row] == icon) return;
        if (Window is not { IsOpen: true } window) return;

        var found = window.Element($"{element}-{row}");
        if (found.IsNone) return;

        Xui.SetImage(found, icon);
        worn[row] = icon;
    }

    /// <summary>Which row's number a drag has hold of.</summary>
    private int _held = -1;

    /// <summary>What that number was when the drag started.</summary>
    private double _from;

    /// <summary>Where the pointer was then.</summary>
    private float _went;

    /// <summary>
    /// Changes a number by dragging the handle beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// How a position is set in every other editor, and the reason is worth stating: typing a
    /// number means knowing which number you want, and most of the time somebody wants the thing
    /// a bit further left, which is a gesture rather than a value.
    /// </para>
    /// <para>
    /// Held with shift it moves ten times as fast and with alt a tenth, which is the pair every
    /// tool offers. Held with control it lands on the tool's own step, so a thing dragged into
    /// place sits on the same grid as a thing moved with the handles in the viewport.
    /// </para>
    /// <para>
    /// What is being dragged is asked of the drawer rather than worked out here. A drawer that
    /// answers with a number can be dragged, whatever the value behind it turns out to be.
    /// </para>
    /// </remarks>
    private void Scrub(Input input)
    {
        if (input.MouseReleased(MouseButton.Left) || !input.MouseDown(MouseButton.Left))
        {
            _held = -1;
            return;
        }

        if (input.MousePressed(MouseButton.Left) && _held < 0) Grab(input);
        if (_held < 0) return;

        if (_lines[_held] is not { Field: { } field, Drawer: { } drawer } line) return;

        var target = new FieldTarget(field, EditorShell.Ecs, _subject);
        var step = drawer.Step(line.Part, target);
        if (step <= 0f) return;

        var fine = input.KeyDown(Key.AltLeft) || input.KeyDown(Key.AltRight);
        var fast = input.KeyDown(Key.ShiftLeft) || input.KeyDown(Key.ShiftRight);

        var moved = _from + ((input.MouseX - _went) * step * (fine ? 0.1f : 1f) * (fast ? 10f : 1f));

        if (input.KeyDown(Key.ControlLeft) || input.KeyDown(Key.ControlRight))
        {
            var grid = step * 10f;
            moved = Math.Round(moved / grid) * grid;
        }

        drawer.Nudge(line.Part, target, moved);
    }

    /// <summary>Takes hold of whichever handle the pointer is over.</summary>
    private void Grab(Input input)
    {
        if (Window is not { IsOpen: true } window) return;
        if (_subject.IsNone) return;

        var (x, y) = input.MousePosition;

        for (var row = 0; row < Rows; row++)
        {
            if (!Shown[row] || !ShowGrip[row]) continue;
            if (_lines[row] is not { Field: { } field, Drawer: { } drawer } line) continue;

            var element = window.Element($"dg-{row}");
            if (element.IsNone) continue;
            if (!Xui.TryRect(element, out var rect)) continue;
            if (x < rect.X || x > rect.X + rect.Width) continue;
            if (y < rect.Y || y > rect.Y + rect.Height) continue;

            var target = new FieldTarget(field, EditorShell.Ecs, _subject);
            if (drawer.Number(line.Part, target) is not { } value) continue;

            _held = row;
            _went = x;
            _from = value;
            return;
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
    /// script is not fought over by a panel writing back what it read last frame.
    /// </remarks>
    [OnChange]
    public void Apply()
    {
        if (_subject.IsNone) return;

        var world = EditorShell.Ecs;
        var entity = _subject;

        for (var i = 0; i < Rows; i++)
        {
            var line = _lines[i];

            if (line.Kind == LineKind.Name)
            {
                Rename(world, entity, Values[i].Trim());
                continue;
            }

            if (line is not { Kind: LineKind.Field, Field: { } field, Drawer: { } drawer }) continue;

            drawer.Read(new InspectorRow(this, i), line.Part, new FieldTarget(field, world, entity));
        }
    }

    /// <summary>Gives the entity a new name, and a way back to the old one.</summary>
    private static void Rename(EcsWorld world, Entity entity, string renamed)
    {
        var before = world.NameOf(entity);
        if (renamed.Length == 0 || renamed == before) return;

        world.SetName(entity, renamed);
        EditorHistory.Record(
            $"rename to {renamed}",
            undo => undo.SetName(entity, before),
            redo => redo.SetName(entity, renamed),
            $"{entity.Bits}:name");
    }

    /// <summary>Runs whatever the row's button offers.</summary>
    [Command("#db", Count = Rows)]
    public void Press(int row)
    {
        var world = EditorShell.Ecs;
        var line = _lines[row];

        if (line is { Kind: LineKind.Method, Method: { } method })
        {
            method.Run(world, _subject);
            return;
        }

        if (line is not { Kind: LineKind.Field, Field: { } field, Drawer: { } drawer }) return;

        drawer.Press(
            new InspectorRow(this, row), line.Part, new FieldTarget(field, world, _subject));
    }

    /// <summary>Opens or shuts a component's block.</summary>
    /// <remarks>
    /// Only a heading answers. A click on a field row is a click on whatever editor that row
    /// draws, and the row itself has nothing to do: the box, the tick and the button inside it are
    /// what the click was for.
    /// </remarks>
    [Command("#drow", Count = Rows)]
    public void Fold(int row)
    {
        if (_lines[row].Kind != LineKind.Heading) return;

        var component = _lines[row].Component;
        if (!_shut.Remove(component)) _shut.Add(component);
    }

    /// <summary>Folds a component's block from its name as well as from its row.</summary>
    /// <remarks>
    /// The label takes pointer events so it can be right clicked, and an element that takes them
    /// keeps the click from the row underneath. So it answers the click itself.
    /// </remarks>
    [Command("#dname", Count = Rows)]
    public void FoldByName(int row) => Fold(row);

    /// <summary>
    /// Puts a field back to what it is when the component is first put on something.
    /// </summary>
    /// <remarks>
    /// Asked of the component itself rather than assumed. Nothing here knows that a scale starts
    /// at one and a position at zero, and guessing would be wrong for the first component somebody
    /// adds that this side has never heard of. A component that cannot be built has no answer, and
    /// nothing happens.
    /// </remarks>
    [Context("#dname", Count = Rows)]
    public void Reset(int row)
    {
        var line = _lines[row];

        if (line.Kind != LineKind.Field) return;
        if (line.Field is not { IsWritable: true } field) return;
        if (line.Schema is not { CanAdd: true } schema) return;

        var world = EditorShell.Ecs;
        var entity = _subject;
        if (entity.IsNone) return;

        var probe = world.Spawn();

        try
        {
            if (!schema.Add(world, probe)) return;
            if (field.Read(world, probe) is not { } fresh) return;

            EditorFields.Change(world, entity, field, fresh);
        }
        finally
        {
            world.Despawn(probe);
        }
    }

    /// <summary>Offers what can be done to the component a row belongs to.</summary>
    /// <remarks>
    /// A right click rather than a button on the row. A row with a cross on it says "delete me" in
    /// the corner of the eye all day for the one time it is wanted, and there is more than one
    /// thing to offer anyway.
    /// </remarks>
    [Context("#drow", Count = Rows)]
    public void RowMenu(int row)
    {
        if (_lines[row].Schema is not { } schema) return;

        var entity = _subject;
        var (x, y) = EditorShell.Context?.Input.MousePosition ?? (0f, 0f);

        EditorShell.ShowMenu(
            schema.Name,
            [
                new MenuItem(
                    "Reset",
                    MenuKind.Command,
                    world =>
                    {
                        // Removing and adding again is what "reset" means for a component whose
                        // fields this side can write: the value it comes back as is the one the
                        // type declares.
                        var before = Snapshot(schema, world, entity);
                        if (!schema.Remove(world, entity)) return;
                        if (!schema.Add(world, entity)) return;

                        EditorHistory.Record(
                            $"reset {schema.Name}",
                            undo => Restore(schema, undo, entity, before),
                            redo =>
                            {
                                redo.RemoveById(entity, schema.Id);
                                schema.Add(redo, entity);
                            });
                    },
                    Icon: "icons/ui/undo.png"),
                new MenuItem(
                    "Remove",
                    MenuKind.Command,
                    world =>
                    {
                        var before = Snapshot(schema, world, entity);
                        if (!schema.Remove(world, entity)) return;

                        EditorHistory.Record(
                            $"remove {schema.Name}",
                            undo => Restore(schema, undo, entity, before),
                            redo => schema.Remove(redo, entity));
                    },
                    Icon: "icons/ui/delete.png"),
            ],
            x,
            y);
    }

    /// <summary>Offers what can be done with a tag, which is take it off.</summary>
    [Context("#dchip", Count = Chips)]
    public void TagMenu(int chip)
    {
        if (!TagShown[chip]) return;

        var (schema, _) = _tags[chip];
        var entity = EditorSelection.Current;
        var name = Tags[chip];

        var items = new List<MenuItem>();

        if (schema is { CanAdd: true })
        {
            items.Add(new MenuItem(
                "Remove",
                MenuKind.Command,
                world =>
                {
                    schema.Remove(world, entity);
                    EditorHistory.Record(
                        $"remove {name}",
                        undo => schema.Add(undo, entity),
                        redo => schema.Remove(redo, entity));
                },
                Icon: "icons/ui/delete.png"));
        }
        else
        {
            items.Add(new MenuItem("Nothing to do", MenuKind.Command, null, () => false));
        }

        var (x, y) = Under($"dchip-{chip}");
        EditorShell.ShowMenu(name, items, x, y);
    }

    /// <summary>Every field of a component, so removing it can be taken back.</summary>
    private static Dictionary<string, object> Snapshot(
        ComponentSchema schema, EcsWorld world, Entity entity)
    {
        var values = new Dictionary<string, object>();

        foreach (var field in schema.Fields)
        {
            if (field.Read(world, entity) is { } value) values[field.Name] = value;
        }

        return values;
    }

    /// <summary>Puts a snapshot back onto an entity.</summary>
    private static void Restore(
        ComponentSchema schema, EcsWorld world, Entity entity, Dictionary<string, object> values)
    {
        schema.Add(world, entity);

        foreach (var (name, value) in values) schema.Write(world, entity, name, value);
    }

    /// <summary>Offers the same list on a right click, since the button is a menu either way.</summary>
    [Context("#d-add")]
    public void AddComponentMenu() => AddComponent();

    /// <summary>Offers the components that can be put on the selection.</summary>
    [Command("#d-add")]
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
            bool already;

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

        if (items.Count == 0) items.Add(new MenuItem("nothing left to add", MenuKind.Separator));

        var (x, y) = Under("d-add");
        EditorShell.ShowMenu("Add component", items, x, y);
    }

    /// <summary>Where a menu opened from one of this panel's elements should sit.</summary>
    private (float X, float Y) Under(string element)
    {
        if (Window is { } window && Xui.TryRect(window.Element(element), out var rect))
            return (rect.X, rect.Bottom + 4f);

        return EditorShell.Context?.Input.MousePosition ?? (0f, 0f);
    }

    /// <summary>A byte count as a person reads one.</summary>
    private static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024f:0.#} KB",
        _ => $"{bytes / (1024f * 1024f):0.#} MB",
    };

    /// <summary>The last part of a Rust path, which is the name somebody would recognise.</summary>
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

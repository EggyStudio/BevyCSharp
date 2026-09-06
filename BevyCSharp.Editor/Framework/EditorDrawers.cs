using Bevy;
using BevyCSharp.Editor.Drawers;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Draws one kind of field, and reads back what was done to it.
/// </summary>
/// <remarks>
/// <para>
/// One drawer per kind of value, each in a file of its own. The inspector walks a component's
/// fields and asks the table which drawer takes each one; the drawer says how many rows it wants
/// and fills them. Nothing in the panel knows what a vector is, and adding a way to edit a new
/// kind of value is a class rather than another branch in a panel that already has too many.
/// </para>
/// <para>
/// A drawer answers about a field rather than about a type name, so a game can take over a field
/// by any rule it likes: its kind, its component, its name, or one particular field of one
/// particular component.
/// </para>
/// </remarks>
public interface IFieldDrawer
{
    /// <summary>Whether this drawer takes that field.</summary>
    bool Handles(ComponentField field);

    /// <summary>How many rows it wants. One unless the value has parts.</summary>
    int Lines(ComponentField field) => 1;

    /// <summary>Fills one of its rows from the world.</summary>
    void Draw(InspectorRow row, int part, FieldTarget target);

    /// <summary>Takes what was typed or ticked in one of its rows back to the world.</summary>
    void Read(InspectorRow row, int part, FieldTarget target);

    /// <summary>Answers a click on the row's button.</summary>
    void Press(InspectorRow row, int part, FieldTarget target)
    {
    }

    /// <summary>
    /// What one of its rows currently reads as a number, or <see langword="null"/> when that row
    /// is not a number at all.
    /// </summary>
    /// <remarks>
    /// What makes dragging a handle work without the panel knowing what it is dragging. A drawer
    /// that answers this and <see cref="Nudge"/> can be dragged; one that does not, cannot.
    /// </remarks>
    double? Number(int part, FieldTarget target) => null;

    /// <summary>Puts a number a drag arrived at back into the field.</summary>
    void Nudge(int part, FieldTarget target, double value)
    {
    }

    /// <summary>What one pixel of drag is worth, in the units the row is written in.</summary>
    float Step(int part, FieldTarget target) => 0.025f;
}

/// <summary>
/// Every way the editor knows to draw a field.
/// </summary>
/// <remarks>
/// A table, like the menu and the toolbar, and for the same reason: a game that adds a component
/// with a value nothing here has heard of adds a drawer for it, rather than waiting for the
/// inspector to learn about it. The last drawer added that takes a field wins, so a game overrides
/// a built-in one by registering after it.
/// </remarks>
public static class EditorDrawers
{
    private static readonly List<(IFieldDrawer Drawer, int Priority, int Added)> Table = [];

    static EditorDrawers()
    {
        Add(new TextDrawer());
        Add(new NumberDrawer());
        Add(new FlagDrawer());
        Add(new ChoiceDrawer());
        Add(new VectorDrawer());
        Add(new AngleDrawer());
        Add(new SliderDrawer());
        Add(new ColorDrawer());
        Add(new EntityDrawer());
        Add(new FlagsDrawer());
        Add(new AssetDrawer());
    }

    /// <summary>Adds a drawer, which takes precedence over everything added before it.</summary>
    public static void Add(IFieldDrawer drawer, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(drawer);

        Table.Add((drawer, priority, Table.Count));
        Table.Sort((a, b) =>
        {
            var order = b.Priority.CompareTo(a.Priority);
            return order != 0 ? order : b.Added.CompareTo(a.Added);
        });
    }

    /// <summary>
    /// Takes a drawer back out.
    /// </summary>
    /// <remarks>
    /// For a game that wants its own drawer only while something of its own is open, and for a
    /// test that has to leave the table as it found it.
    /// </remarks>
    public static bool Remove(IFieldDrawer drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);

        return Table.RemoveAll(entry => ReferenceEquals(entry.Drawer, drawer)) > 0;
    }

    /// <summary>Every drawer, in the order they are asked.</summary>
    public static IEnumerable<IFieldDrawer> All => Table.Select(entry => entry.Drawer);

    /// <summary>Whichever drawer takes a field.</summary>
    /// <remarks>
    /// Never nothing: the last drawer asked shows the value as text, which is a truthful answer
    /// for a field the editor has no editor for.
    /// </remarks>
    public static IFieldDrawer For(ComponentField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        foreach (var (drawer, _, _) in Table)
        {
            if (drawer.Handles(field)) return drawer;
        }

        return Fallback;
    }

    /// <summary>What draws a field nothing else claimed.</summary>
    private static readonly IFieldDrawer Fallback = new TextDrawer();
}

/// <summary>
/// Writing a field, with the way back.
/// </summary>
/// <remarks>
/// Every drawer writes through here rather than through the field, so that one edit is one entry
/// in the history wherever it came from: a box, a tick, a menu row or a drag.
/// </remarks>
public static class EditorFields
{
    /// <summary>Writes one field and records how to take it back.</summary>
    public static void Change(EcsWorld world, Entity entity, ComponentField field, object value)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

        var before = field.Read(world, entity);
        if (!field.Write(world, entity, value)) return;

        Told(world, entity, field);
        if (before is null) return;

        EditorHistory.Record(
            field.Name,
            undo =>
            {
                field.Write(undo, entity, before);
                Told(undo, entity, field);
            },
            redo =>
            {
                field.Write(redo, entity, value);
                Told(redo, entity, field);
            },
            $"{entity.Bits}:{field.Name}");
    }

    /// <summary>
    /// Calls whatever the field asked to have called once it has changed.
    /// </summary>
    /// <remarks>
    /// After the write rather than before, so that what the methods read is the new value. Taking a
    /// change back counts as changing it: something derived from a radius is as wrong after an undo
    /// as it was before the write, and an editor where undo leaves the collider the wrong size is
    /// one nobody trusts.
    /// </remarks>
    private static void Told(EcsWorld world, Entity entity, ComponentField field)
    {
        if (field.Hints.Changed.Count == 0) return;
        if (field.Schema is not { } schema) return;

        foreach (var name in field.Hints.Changed)
        {
            schema.Method(name)?.Run(world, entity);
        }
    }

    /// <summary>
    /// A number as a row writes it: enough digits to be exact, no more.
    /// </summary>
    /// <remarks>
    /// Nought is nought. A rotation of a millionth of a degree the wrong way rounds to "-0", which
    /// is a number nobody has ever meant and which reads as something being subtly wrong.
    /// </remarks>
    public static string Text(double value)
    {
        var written = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        return written == "-0" ? "0" : written;
    }

    /// <summary>Reads a number a person typed, in the one culture the editor writes.</summary>
    public static bool TryNumber(string text, out double value) => double.TryParse(
        text,
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out value);
}

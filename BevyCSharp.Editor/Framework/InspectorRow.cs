using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A panel that lends out its rows.
/// </summary>
/// <remarks>
/// The document holds a pool of rows, all the same shape, and each row can show a mark, a name, a
/// box with a handle, a tick and a button. What a row shows is decided when it is filled, so the
/// panel never has to know what is going into it.
/// </remarks>
public interface IInspectorRows
{
    /// <summary>Puts a picture at the start of the row, or nothing.</summary>
    void Mark(int row, string? icon);

    /// <summary>What the row is called.</summary>
    void Name(int row, string text);

    /// <summary>Shows a box, with a handle on its edge or none.</summary>
    /// <remarks>
    /// A box that will not take an edit is drawn differently, so that a row somebody cannot change
    /// says so before they try rather than after.
    /// </remarks>
    void Box(int row, string value, Grip? grip, bool editable);

    /// <summary>Shows what the number is measured in, after the box.</summary>
    void Unit(int row, string suffix);

    /// <summary>Says that the things selected do not all hold this value.</summary>
    void Mixed(int row);

    /// <summary>Shows a bar between two ends, and where the value sits on it.</summary>
    void Bar(int row, double value, double minimum, double maximum);

    /// <summary>Shows a tick.</summary>
    void Tick(int row, bool on);

    /// <summary>Shows a button.</summary>
    void Button(int row, string text);

    /// <summary>Where the bar was left, in the units the field is written in.</summary>
    double Barred(int row, double minimum, double maximum);

    /// <summary>Whether the bar was moved since it was last drawn.</summary>
    bool Slid(int row);

    /// <summary>What is in the row's box now, which is what somebody typed.</summary>
    string Typed(int row);

    /// <summary>Whether the row's tick is on.</summary>
    bool Ticked(int row);

    /// <summary>A point just under the row's button, for a menu to open at.</summary>
    (float X, float Y) Below(int row);
}

/// <summary>
/// One row of an inspector, as a drawer sees it.
/// </summary>
/// <remarks>
/// The pieces a row can show, and no more. A drawer says what to show and reads back what was
/// typed or ticked; where the row is, how it is styled, and how many of them there are belong to
/// the panel.
/// </remarks>
/// <param name="Panel">Who owns the row.</param>
/// <param name="Index">Which row it is.</param>
public readonly record struct InspectorRow(IInspectorRows Panel, int Index)
{
    /// <inheritdoc cref="IInspectorRows.Mark"/>
    public void Mark(string? icon) => Panel.Mark(Index, icon);

    /// <inheritdoc cref="IInspectorRows.Name"/>
    public void Name(string text) => Panel.Name(Index, text);

    /// <inheritdoc cref="IInspectorRows.Box"/>
    public void Box(string value, Grip? grip = null, bool editable = true) =>
        Panel.Box(Index, value, grip, editable);

    /// <inheritdoc cref="IInspectorRows.Unit"/>
    public void Unit(string suffix) => Panel.Unit(Index, suffix);

    /// <inheritdoc cref="IInspectorRows.Mixed"/>
    public void Mixed() => Panel.Mixed(Index);

    /// <inheritdoc cref="IInspectorRows.Bar"/>
    public void Bar(double value, double minimum, double maximum) =>
        Panel.Bar(Index, value, minimum, maximum);

    /// <inheritdoc cref="IInspectorRows.Tick"/>
    public void Tick(bool on) => Panel.Tick(Index, on);

    /// <inheritdoc cref="IInspectorRows.Button"/>
    public void Button(string text) => Panel.Button(Index, text);

    /// <inheritdoc cref="IInspectorRows.Barred"/>
    public double Barred(double minimum, double maximum) => Panel.Barred(Index, minimum, maximum);

    /// <inheritdoc cref="IInspectorRows.Slid"/>
    public bool Slid => Panel.Slid(Index);

    /// <inheritdoc cref="IInspectorRows.Typed"/>
    public string Typed => Panel.Typed(Index);

    /// <inheritdoc cref="IInspectorRows.Ticked"/>
    public bool Ticked => Panel.Ticked(Index);

    /// <inheritdoc cref="IInspectorRows.Below"/>
    public (float X, float Y) Below => Panel.Below(Index);
}

/// <summary>
/// The handle on a box's edge: what colour it is, and what it says.
/// </summary>
/// <param name="Picture">The file it wears, under the asset root.</param>
/// <param name="Letter">
/// What is written on it, or nothing. A handle with nothing on it is a bar the width of a finger;
/// one with a letter grows to the right to take it.
/// </param>
public readonly record struct Grip(string Picture, string Letter = "");

/// <summary>
/// The handles the editor uses.
/// </summary>
/// <remarks>
/// Three colours for the three axes, in the order every tool draws them in, and a grey one for a
/// number that is not part of anything. A row whose handle is red is the x of whatever the row
/// above it named, which is quicker to read than a letter and does not move the box.
/// </remarks>
public static class Grips
{
    /// <summary>Whether the axis handles carry their letters.</summary>
    /// <remarks>
    /// Off. A row that says Translation and then x, y, z has said the same thing twice, and the
    /// three boxes are in the order everybody already reads them in. On for somebody who would
    /// rather read it than know it, and the handles widen to take the letters.
    /// </remarks>
    public static bool Letters { get; set; }

    /// <summary>A number that is not one of three.</summary>
    public static readonly Grip Plain = new("icons/axis/none.png");

    /// <summary>The handle for one of three numbers.</summary>
    public static Grip Axis(int part)
    {
        var grip = Axes[Math.Clamp(part, 0, Axes.Length - 1)];
        return Letters ? grip : grip with { Letter = string.Empty };
    }

    /// <summary>
    /// Red, green and blue, with the letters they would carry.
    /// </summary>
    /// <remarks>
    /// Pictures rather than colours written to the element. Writing a colour makes the interface
    /// restyle the element, and a restyle puts back the display property the panel had decided, so
    /// a panel that painted its handles could not hide anything else in the same row. The pictures
    /// are single colours under the asset root, so what red means is one small file.
    /// </remarks>
    private static readonly Grip[] Axes =
    [
        new("icons/axis/x.png", "x"),
        new("icons/axis/y.png", "y"),
        new("icons/axis/z.png", "z"),
    ];
}

/// <summary>
/// Which field of which things a row is about.
/// </summary>
/// <remarks>
/// One entity is the ordinary case and reads as one. When several are selected the row shows what
/// the first of them says and an edit reaches all of them, which is what somebody who selected
/// three lights and typed a brightness meant. Whether they agree is asked separately, because a
/// drawer that cares says so in what it draws rather than in what it writes.
/// </remarks>
/// <param name="Field">The field being drawn.</param>
/// <param name="World">The world they live in.</param>
/// <param name="Entity">The entity whose value is shown.</param>
/// <param name="Others">The rest of the selection, or nothing.</param>
public readonly record struct FieldTarget(
    ComponentField Field,
    EcsWorld World,
    Entity Entity,
    IReadOnlyList<Entity>? Others = null)
{
    /// <summary>Reads the field.</summary>
    public object? Read() => Field.Read(World, Entity);

    /// <summary>Writes the field on everything selected, and records how to take it back.</summary>
    public void Write(object value)
    {
        EditorFields.Change(World, Entity, Field, value);

        if (Others is not { Count: > 1 }) return;

        foreach (var other in Others)
        {
            if (other == Entity) continue;

            EditorFields.Change(World, other, Field, value);
        }
    }

    /// <summary>Whether everything selected holds the same value for this field.</summary>
    /// <remarks>
    /// A drawer that wants to say so can. Nothing is forced to: showing the first one's value is
    /// truthful as far as it goes, and an edit says what they all are afterwards.
    /// </remarks>
    public bool Agree() => Agree(static value => value);

    /// <summary>
    /// The same, about one part of the value.
    /// </summary>
    /// <remarks>
    /// A vector is three rows, and two entities in different places may still agree about two of
    /// the three. Saying so on all three rows because one of them differs is saying something
    /// false about the other two.
    /// </remarks>
    public bool Agree(Func<object?, object?> part)
    {
        ArgumentNullException.ThrowIfNull(part);

        if (Others is not { Count: > 1 }) return true;

        var first = part(Read());

        foreach (var other in Others)
        {
            if (other == Entity) continue;
            if (!Equals(part(Field.Read(World, other)), first)) return false;
        }

        return true;
    }
}

using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A panel that lends out its rows.
/// </summary>
/// <remarks>
/// <para>
/// The document holds a pool of rows, all the same shape: a mark, a name in a column of its own,
/// and a body holding everything a value can be drawn as. What a row shows is decided when it is
/// filled, so the panel never has to know what is going into it.
/// </para>
/// <para>
/// The name column is the point of the shape. Every value in the panel starts at the same place
/// whatever is drawn there and however long the names are, which is what lets somebody read a
/// column of numbers rather than hunt for each one. A drawer that wants the whole width says so,
/// and then there is no name column for that row at all.
/// </para>
/// </remarks>
public interface IInspectorRows
{
    /// <summary>Puts a picture at the start of the row, or nothing.</summary>
    void Mark(int row, string? icon);

    /// <summary>What the row is called, and how far in it sits.</summary>
    void Name(int row, string text, int indent = 0);

    /// <summary>
    /// Gives the whole row to the value, with no name beside it.
    /// </summary>
    /// <remarks>
    /// For a value the name column has nothing to add to: a sentence, a path, a bar that means
    /// what it is next to. Said before whatever is drawn, because it is about the row rather than
    /// about the value.
    /// </remarks>
    void Wide(int row);

    /// <summary>Shows a box in one of the row's places, with a handle on its edge or none.</summary>
    /// <remarks>
    /// A row has more than one place for a box so that three numbers can sit beside each other. A
    /// value with one number uses the first and nothing else is drawn.
    /// </remarks>
    void Box(int row, int slot, string value, Grip? grip, bool editable);

    /// <summary>Shows what the number is measured in, in a column after everything else.</summary>
    void Unit(int row, string suffix);

    /// <summary>Says that the things selected do not all hold this value.</summary>
    void Mixed(int row);

    /// <summary>Shows a bar between two ends, and where the value sits on it.</summary>
    void Bar(int row, double value, double minimum, double maximum);

    /// <summary>Shows a tick.</summary>
    void Tick(int row, bool on);

    /// <summary>Shows a button, and how much of the row it takes against its neighbours.</summary>
    void Button(int row, int slot, string text, double weight);

    /// <summary>Shows a patch of colour that opens a picker.</summary>
    void Swatch(int row, uint colour);

    /// <summary>Shows words across the row rather than a value.</summary>
    void Note(int row, string text, NoteKind kind);

    /// <summary>Where the bar was left, in the units the field is written in.</summary>
    double Barred(int row, double minimum, double maximum);

    /// <summary>Whether the bar was moved since it was last drawn.</summary>
    bool Slid(int row);

    /// <summary>What is in one of the row's boxes now, which is what somebody typed.</summary>
    string Typed(int row, int slot);

    /// <summary>Whether the row's tick is on.</summary>
    bool Ticked(int row);

    /// <summary>A point just under one of the row's buttons, for a menu to open at.</summary>
    (float X, float Y) Below(int row, int slot);
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
    /// <summary>
    /// How many boxes or buttons one row can hold.
    /// </summary>
    /// <remarks>
    /// Three, because the values that want to share a line are the ones with three parts: a place,
    /// a rotation, a size. A fourth is the alpha of a colour, and a colour is drawn as a patch with
    /// its numbers folded away rather than as four boxes nobody can read.
    /// </remarks>
    public const int Slots = 3;

    /// <inheritdoc cref="IInspectorRows.Mark"/>
    public void Mark(string? icon) => Panel.Mark(Index, icon);

    /// <inheritdoc cref="IInspectorRows.Name"/>
    public void Name(string text, int indent = 0) => Panel.Name(Index, text, indent);

    /// <inheritdoc cref="IInspectorRows.Wide"/>
    public void Wide() => Panel.Wide(Index);

    /// <inheritdoc cref="IInspectorRows.Box"/>
    public void Box(string value, Grip? grip = null, bool editable = true) =>
        Panel.Box(Index, 0, value, grip, editable);

    /// <inheritdoc cref="IInspectorRows.Box"/>
    public void Box(int slot, string value, Grip? grip = null, bool editable = true) =>
        Panel.Box(Index, slot, value, grip, editable);

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
    public void Button(string text) => Panel.Button(Index, 0, text, 1d);

    /// <inheritdoc cref="IInspectorRows.Button"/>
    public void Button(int slot, string text, double weight = 1d) =>
        Panel.Button(Index, slot, text, weight);

    /// <inheritdoc cref="IInspectorRows.Swatch"/>
    public void Swatch(uint colour) => Panel.Swatch(Index, colour);

    /// <inheritdoc cref="IInspectorRows.Note"/>
    public void Note(string text, NoteKind kind = NoteKind.Info) => Panel.Note(Index, text, kind);

    /// <inheritdoc cref="IInspectorRows.Barred"/>
    public double Barred(double minimum, double maximum) => Panel.Barred(Index, minimum, maximum);

    /// <inheritdoc cref="IInspectorRows.Slid"/>
    public bool Slid => Panel.Slid(Index);

    /// <inheritdoc cref="IInspectorRows.Typed"/>
    public string Typed => Panel.Typed(Index, 0);

    /// <inheritdoc cref="IInspectorRows.Typed"/>
    public string TypedIn(int slot) => Panel.Typed(Index, slot);

    /// <inheritdoc cref="IInspectorRows.Ticked"/>
    public bool Ticked => Panel.Ticked(Index);

    /// <inheritdoc cref="IInspectorRows.Below"/>
    public (float X, float Y) Below => Panel.Below(Index, 0);

    /// <inheritdoc cref="IInspectorRows.Below"/>
    public (float X, float Y) Under(int slot) => Panel.Below(Index, slot);
}

/// <summary>
/// The handle on a box's edge: what colour it is, and what it says.
/// </summary>
/// <remarks>
/// A colour rather than a picture. Painting an element used to be impossible here: writing a colour
/// made the interface restyle it, and the restyle put back both the colour and the display property
/// the panel had just decided. What a program decides is now applied after the sheet, so a painted
/// element stays painted and still hides when it is told to.
/// </remarks>
/// <param name="Colour">
/// What it is painted, as red, green, blue and alpha bytes, or nought for whatever the stylesheet
/// says. Nought is not black: it means the handle is left alone, which is how the inspector gets a
/// column of grey handles without writing a colour sixty times a second.
/// </param>
/// <param name="Letter">
/// What is written on it, or nothing. A handle with nothing on it is a bar the width of a finger;
/// one with a letter grows to the right to take it.
/// </param>
public readonly record struct Grip(uint Colour = 0u, string Letter = "");

/// <summary>
/// The handles the editor uses.
/// </summary>
/// <remarks>
/// Grey, and the same grey for all three axes. Three saturated colours down the side of a panel is
/// the loudest thing on the screen for information the order of the boxes already carries, and the
/// viewport handles that do need to be told apart are the ones that should have the colour. A tool
/// that wants them coloured anyway turns them on.
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

    /// <summary>Whether the axis handles are painted red, green and blue.</summary>
    public static bool Colourful { get; set; }

    /// <summary>A number that is not one of three.</summary>
    public static readonly Grip Plain = new();

    /// <summary>The handle for one of three numbers.</summary>
    public static Grip Axis(int part)
    {
        var index = Math.Clamp(part, 0, Axes.Length - 1);
        var grip = new Grip(Colourful ? Axes[index] : 0u, Letters ? Letter(index) : string.Empty);

        return grip;
    }

    /// <summary>Red, green and blue, for whoever asks for them.</summary>
    private static readonly uint[] Axes = [0xD6503CFFu, 0x7DB84AFFu, 0x3D7ED6FFu];

    /// <summary>What an axis is called.</summary>
    private static string Letter(int index) => index switch
    {
        0 => "x",
        1 => "y",
        _ => "z",
    };
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

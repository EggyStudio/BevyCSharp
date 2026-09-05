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
    void Box(int row, string value, Grip? grip);

    /// <summary>Shows a tick.</summary>
    void Tick(int row, bool on);

    /// <summary>Shows a button.</summary>
    void Button(int row, string text);

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
    public void Box(string value, Grip? grip = null) => Panel.Box(Index, value, grip);

    /// <inheritdoc cref="IInspectorRows.Tick"/>
    public void Tick(bool on) => Panel.Tick(Index, on);

    /// <inheritdoc cref="IInspectorRows.Button"/>
    public void Button(string text) => Panel.Button(Index, text);

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
/// <param name="Red">How much red, from nothing to one.</param>
/// <param name="Green">The same for green.</param>
/// <param name="Blue">And blue.</param>
/// <param name="Letter">
/// What is written on it, or nothing. A handle with nothing on it is a bar the width of a finger;
/// one with a letter grows to the right to take it.
/// </param>
public readonly record struct Grip(float Red, float Green, float Blue, string Letter = "");

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
    public static readonly Grip Plain = new(0.29f, 0.29f, 0.31f);

    /// <summary>The handle for one of three numbers.</summary>
    public static Grip Axis(int part)
    {
        var grip = Axes[Math.Clamp(part, 0, Axes.Length - 1)];
        return Letters ? grip : grip with { Letter = string.Empty };
    }

    /// <summary>Red, green and blue, with the letters they would carry.</summary>
    private static readonly Grip[] Axes =
    [
        new(0.75f, 0.22f, 0.17f, "x"),
        new(0.15f, 0.68f, 0.38f, "y"),
        new(0.18f, 0.50f, 0.93f, "z"),
    ];
}

/// <summary>
/// Which field of which thing a row is about.
/// </summary>
/// <param name="Field">The field being drawn.</param>
/// <param name="World">The world it lives in.</param>
/// <param name="Entity">The entity carrying the component.</param>
public readonly record struct FieldTarget(ComponentField Field, EcsWorld World, Entity Entity)
{
    /// <summary>Reads the field.</summary>
    public object? Read() => Field.Read(World, Entity);

    /// <summary>Writes the field and records how to take it back.</summary>
    public void Write(object value) => EditorFields.Change(World, Entity, Field, value);
}

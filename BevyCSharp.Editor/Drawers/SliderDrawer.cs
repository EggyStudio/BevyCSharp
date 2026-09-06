using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// A number with two ends: a bar and a box.
/// </summary>
/// <remarks>
/// <para>
/// Both, rather than one or the other. The bar is for setting a value roughly and says what the
/// ends are; the box is for saying exactly. A field that offers only a bar cannot be given the
/// number somebody was told to use, and one that offers only a box says nothing about what a
/// sensible value would be.
/// </para>
/// <para>
/// The bar in the document runs from nothing to a thousand whatever the field does, because a
/// widget's ends are written in the document and cannot be changed while it runs. The field's own
/// ends are mapped onto it here.
/// </para>
/// </remarks>
public sealed class SliderDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) =>
        field.Hints.HasRange
        && field.Kind is FieldKind.Float or FieldKind.Double or FieldKind.Int;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        var (low, high) = Ends(target.Field);
        var written = TextDrawer.Written(target.Read());

        row.Name(target.Field.Title);
        row.Bar(Value(target), low, high);

        // What sits beside the bar is the field's choice. A weight between nought and one is
        // dragged and its number means nothing on its own; a field of view is typed, and a bar
        // without a box is a bar somebody cannot give the number they were told to use.
        switch (target.Field.Hints.Readout)
        {
            case SliderReadout.Box:
                row.Box(written, Grips.Plain, target.Field.IsWritable);
                break;

            case SliderReadout.Number:
                row.Box(written, null, editable: false);
                break;
        }

        row.Unit(target.Field.Hints.Unit ?? string.Empty);

        if (!target.Agree()) row.Mixed();
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var (low, high) = Ends(target.Field);

        // The bar first: it is the thing that was moved, and a bar and a box holding different
        // numbers means the bar is the one that changed. What the box says is written back only
        // when it disagrees with the field, which is what somebody typing into it produces.
        var slid = row.Barred(low, high);
        if (Math.Abs(slid - Value(target)) > Step(0, target) * 0.5f)
        {
            Set(target, slid);
            return;
        }

        // Only a box that can be typed into is read back. A bar with the number beside it is
        // showing the value, not asking for one, and reading it back would write what was drawn.
        if (target.Field.Hints.Readout != SliderReadout.Box) return;

        var typed = row.Typed.Trim();
        if (typed.Length == 0) return;
        if (TextDrawer.Written(target.Read()) == typed) return;

        target.Write(typed);
    }

    /// <inheritdoc/>
    public double? Number(int part, FieldTarget target) => Value(target);

    /// <inheritdoc/>
    public void Nudge(int part, FieldTarget target, double value)
    {
        var (low, high) = Ends(target.Field);
        Set(target, Math.Clamp(value, low, high));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A hundredth of the range per pixel, so a drag across a panel covers it once whatever the
    /// ends are, unless the field asked for a step of its own.
    /// </remarks>
    public float Step(int part, FieldTarget target)
    {
        if (target.Field.Hints.Step is { } asked) return (float)asked;

        var (low, high) = Ends(target.Field);
        return (float)((high - low) / 200d);
    }

    /// <summary>The two ends the field asked for.</summary>
    private static (double Low, double High) Ends(ComponentField field) =>
        (field.Hints.Minimum ?? 0d, field.Hints.Maximum ?? 1d);

    /// <summary>What the field currently reads as.</summary>
    private static double Value(FieldTarget target) =>
        EditorFields.TryNumber(TextDrawer.Written(target.Read()), out var value) ? value : 0d;

    /// <summary>Writes a number back in whatever shape the field takes.</summary>
    private static void Set(FieldTarget target, double value) =>
        target.Write(target.Field.Kind == FieldKind.Int
            ? ((long)Math.Round(value)).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : EditorFields.Text(value));
}

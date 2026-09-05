using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// One number: a box with a handle beside it.
/// </summary>
/// <remarks>
/// The handle is what makes a number a thing you can push around rather than a thing you have to
/// know. It is beside the box rather than on it because a drag across a text box selects the text,
/// and one gesture cannot mean both.
/// </remarks>
public sealed class NumberDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) =>
        field.Kind is FieldKind.Float or FieldKind.Double or FieldKind.Int;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(target.Field.Name);
        row.Box(TextDrawer.Written(target.Read()), Grips.Plain);
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var typed = row.Typed.Trim();
        if (typed.Length == 0) return;
        if (TextDrawer.Written(target.Read()) == typed) return;

        target.Write(typed);
    }

    /// <inheritdoc/>
    public double? Number(int part, FieldTarget target) =>
        EditorFields.TryNumber(TextDrawer.Written(target.Read()), out var value) ? value : null;

    /// <inheritdoc/>
    public void Nudge(int part, FieldTarget target, double value) =>
        target.Write(target.Field.Kind == FieldKind.Int
            ? ((long)Math.Round(value)).ToString()
            : EditorFields.Text(value));

    /// <inheritdoc/>
    /// <remarks>A whole number moves by whole numbers, however slowly the hand moves.</remarks>
    public float Step(int part, FieldTarget target) =>
        target.Field.Kind == FieldKind.Int ? 0.25f : EditorTools.MoveStep * 0.1f;
}

using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Three numbers: three rows, or one row of three boxes.
/// </summary>
/// <remarks>
/// Down the panel by default, because three boxes in the width a docked panel has are too narrow
/// to read a long number in. A field that says it would rather be read across the line gets that
/// instead, which is what a position wants: it is one value, it is read left to right, and three
/// rows of it costs three times the height for no more information.
/// </remarks>
public sealed class VectorDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Vec3;

    /// <inheritdoc/>
    public int Lines(ComponentField field) => field.Hints.Inline ? 1 : 3;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        var parts = Parts(target.Read());

        if (target.Field.Hints.Inline)
        {
            row.Name(target.Field.Title);

            for (var axis = 0; axis < 3; axis++)
                row.Box(axis, parts[axis], Grips.Axis(axis), target.Field.IsWritable);

            Say(row, target, part: 0);
            return;
        }

        // The name on the first row only, so three rows read as one thing rather than as three.
        row.Name(part == 0 ? target.Field.Title : string.Empty);
        row.Box(parts[part], Grips.Axis(part), target.Field.IsWritable);
        Say(row, target, part);
    }

    /// <summary>
    /// What the numbers are measured in, and whether they agree.
    /// </summary>
    /// <remarks>
    /// The unit is left off a row of three. It sits in a column of its own after the boxes, and
    /// three boxes have already taken the width that column would want; a position in metres is
    /// three numbers whose unit nobody was in doubt about.
    /// </remarks>
    private static void Say(InspectorRow row, FieldTarget target, int part)
    {
        if (!target.Field.Hints.Inline) row.Unit(target.Field.Hints.Unit ?? string.Empty);

        if (!target.Agree(value => Parts(value)[part])) row.Mixed();
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        // The numbers not typed into come from what the field holds, so editing one axis changes
        // one axis even when the other rows have been scrolled away.
        var parts = Parts(target.Read());
        var changed = false;

        var boxes = target.Field.Hints.Inline ? 3 : 1;

        for (var box = 0; box < boxes; box++)
        {
            var axis = target.Field.Hints.Inline ? box : part;
            var typed = row.TypedIn(box).Trim();

            if (typed.Length == 0) continue;
            if (parts[axis] == typed) continue;

            parts[axis] = typed;
            changed = true;
        }

        if (!changed) return;
        if (Compose(parts) is { } composed) target.Write(composed);
    }

    /// <inheritdoc/>
    public double? Number(int part, FieldTarget target) =>
        EditorFields.TryNumber(Parts(target.Read())[part], out var value) ? value : null;

    /// <inheritdoc/>
    public void Nudge(int part, FieldTarget target, double value)
    {
        var parts = Parts(target.Read());
        parts[part] = EditorFields.Text(value);

        if (Compose(parts) is { } composed) target.Write(composed);
    }

    /// <inheritdoc/>
    public float Step(int part, FieldTarget target) => EditorTools.MoveStep * 0.1f;

    /// <summary>What the three numbers are measured in.</summary>
    private static string Suffix(ComponentField field) => field.Hints.Unit ?? string.Empty;

    /// <summary>The three numbers as they are written in their boxes.</summary>
    private static string[] Parts(object? value) => value is Vec3 vector
        ? [EditorFields.Text(vector.X), EditorFields.Text(vector.Y), EditorFields.Text(vector.Z)]
        : ["", "", ""];

    /// <summary>The numbers back into a vector, or nothing when one of them is not a number.</summary>
    private static object? Compose(string[] parts)
    {
        if (!EditorFields.TryNumber(parts[0], out var x)) return null;
        if (!EditorFields.TryNumber(parts[1], out var y)) return null;
        if (!EditorFields.TryNumber(parts[2], out var z)) return null;

        return new Vec3((float)x, (float)y, (float)z);
    }
}

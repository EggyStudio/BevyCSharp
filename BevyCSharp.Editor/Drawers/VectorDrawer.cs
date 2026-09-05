using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Three numbers: three rows, one per axis.
/// </summary>
/// <remarks>
/// Down rather than across. Three boxes in the width a docked panel has are too narrow to read a
/// number in, and what says which axis a row is, is the colour of the handle beside its box, so
/// the letters nobody needs are not written at all.
/// </remarks>
public sealed class VectorDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Vec3;

    /// <inheritdoc/>
    public int Lines(ComponentField field) => 3;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        // The name on the first row only, so three rows read as one thing rather than as three.
        row.Name(part == 0 ? target.Field.Name : string.Empty);
        row.Box(Parts(target.Read())[part], Grips.Axis(part));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var typed = row.Typed.Trim();
        if (typed.Length == 0) return;

        // The other two come from what the field holds, so editing one axis changes one axis even
        // when the other rows have been scrolled away.
        var parts = Parts(target.Read());
        if (parts[part] == typed) return;

        parts[part] = typed;
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

    /// <summary>The three numbers as they are written in their boxes.</summary>
    private static string[] Parts(object? value) => value is Vec3 vector
        ? [EditorFields.Text(vector.X), EditorFields.Text(vector.Y), EditorFields.Text(vector.Z)]
        : ["", "", ""];

    /// <summary>The three numbers back into a vector, or nothing when one of them is not one.</summary>
    private static object? Compose(string[] parts)
    {
        if (!EditorFields.TryNumber(parts[0], out var x)) return null;
        if (!EditorFields.TryNumber(parts[1], out var y)) return null;
        if (!EditorFields.TryNumber(parts[2], out var z)) return null;

        return new Vec3((float)x, (float)y, (float)z);
    }
}

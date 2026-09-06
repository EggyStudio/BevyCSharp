using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// A rotation, as the three angles a person thinks in.
/// </summary>
/// <remarks>
/// Nobody sets a rotation by typing four numbers that have to add up to one, so the rows are
/// degrees about the three axes and the quaternion is built from them. What comes back out is the
/// same set of angles for the same rotation, which is what stops a row from crawling while it is
/// being read.
/// </remarks>
public sealed class AngleDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Quat;

    /// <inheritdoc/>
    public int Lines(ComponentField field) => 3;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(part == 0 ? target.Field.Title : string.Empty);
        row.Box(Parts(target.Read())[part], Grips.Axis(part), target.Field.IsWritable);
        row.Unit(Suffix(target.Field));

        if (!target.Agree(value => Parts(value)[part])) row.Mixed();
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var typed = row.Typed.Trim();
        if (typed.Length == 0) return;

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
    /// <remarks>Degrees, so a drag turns a thing at the rate the tool turns it.</remarks>
    public float Step(int part, FieldTarget target) => EditorTools.RotateStep * 0.1f;

    /// <summary>What the angles are measured in, which is degrees unless told otherwise.</summary>
    private static string Suffix(ComponentField field) => field.Hints.Unit ?? "deg";

    /// <summary>How many degrees are in a radian, and the way back.</summary>
    private const float ToDegrees = 180f / MathF.PI;

    /// <inheritdoc cref="ToDegrees"/>
    private const float ToRadians = MathF.PI / 180f;

    /// <summary>The rotation as three angles in degrees.</summary>
    private static string[] Parts(object? value)
    {
        if (value is not Quat rotation) return ["", "", ""];

        var euler = rotation.ToEuler();

        return
        [
            EditorFields.Text(euler.X * ToDegrees),
            EditorFields.Text(euler.Y * ToDegrees),
            EditorFields.Text(euler.Z * ToDegrees),
        ];
    }

    /// <summary>Three angles back into a rotation.</summary>
    private static object? Compose(string[] parts)
    {
        if (!EditorFields.TryNumber(parts[0], out var x)) return null;
        if (!EditorFields.TryNumber(parts[1], out var y)) return null;
        if (!EditorFields.TryNumber(parts[2], out var z)) return null;

        return Quat.FromEuler((float)x * ToRadians, (float)y * ToRadians, (float)z * ToRadians);
    }
}

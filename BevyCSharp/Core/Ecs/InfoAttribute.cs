namespace Bevy;

/// <summary>
/// A sentence or two above a field, in the panel rather than on hover.
/// </summary>
/// <remarks>
/// Not a tooltip. A tooltip answers somebody who already suspects there is something to know; this
/// is for what has to be read before the field below it is touched, which is the difference
/// between "what does this do" and "this is in meters, not centimeters".
/// </remarks>
/// <param name="text">What it says.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class InfoAttribute(string text) : Attribute
{
    /// <summary>What it says.</summary>
    public string Text { get; } = text;

    /// <summary>How loudly it says it.</summary>
    public NoteKind Kind { get; init; } = NoteKind.Info;
}

namespace Bevy;

/// <summary>
/// A method a tool can offer as a button.
/// </summary>
/// <remarks>
/// <para>
/// Every method that takes nothing is offered already, so this is for saying what the button should
/// be called, how wide it is, and whether it shares a line.
/// </para>
/// <para>
/// A row of buttons is written as a start, any number of middles and an end. Save, Load and Reset
/// are one line of three because they are one decision; three lines of one is a column of buttons
/// that says they are unrelated.
/// </para>
/// </remarks>
/// <param name="label">What the button says, or nothing for the method's own name.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ButtonAttribute(string label = "") : Attribute
{
    /// <summary>What the button says.</summary>
    public string Label { get; } = label;

    /// <summary>Whether it shares a line, and where in it.</summary>
    public ButtonLine Line { get; init; } = ButtonLine.Alone;

    /// <summary>
    /// How much of the line it takes against the others on it.
    /// </summary>
    /// <remarks>
    /// One each is even. Two against one is twice as wide, which suits a line reading Apply,
    /// Cancel, since they are not the same size decision.
    /// </remarks>
    public double Weight { get; init; } = 1d;
}

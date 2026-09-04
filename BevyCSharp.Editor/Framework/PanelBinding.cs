using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The reading and writing a generated panel does, in one place.
/// </summary>
/// <remarks>
/// <para>
/// Generated code stays thin on purpose, the way the behavior runners do: the emitted file is a
/// list of calls a person can read and step through, and fixing how a binding behaves means
/// changing this class rather than regenerating anyone's panel.
/// </para>
/// <para>
/// Every write compares first. An element that already holds the value is left alone, which
/// matters because writing a text field would otherwise move the caret to the end on every frame
/// while someone was typing into it.
/// </para>
/// </remarks>
public static class PanelBinding
{
    /// <summary>
    /// The element the keyboard is going to, read once a frame by the shell.
    /// </summary>
    /// <remarks>
    /// Held here rather than asked for per binding: a panel of two dozen rows would otherwise
    /// ask the same question two dozen times a frame, and the answer cannot change in between.
    /// </remarks>
    public static Entity Focused { get; internal set; } = Entity.None;

    /// <summary>Writes a flag out to a checkbox, a switch or a toggle.</summary>
    public static void PullFlag(Entity element, bool value)
    {
        if (element.IsNone) return;
        if (Xui.GetFlag(element) == value) return;

        Xui.SetFlag(element, value);
    }

    /// <summary>Writes a number out to a slider.</summary>
    public static void PullNumber(Entity element, float value)
    {
        if (element.IsNone) return;
        if (Nearly(Xui.GetNumber(element), value)) return;

        Xui.SetNumber(element, value);
    }

    /// <summary>
    /// Writes text out to an input or an element's inner text.
    /// </summary>
    /// <remarks>
    /// Never to the field somebody is typing in. A panel showing a live value writes it every
    /// frame, and doing that to a focused field puts the program's answer back over what is being
    /// typed, one keystroke at a time. A half-typed number is not a value yet, and the field keeps
    /// what it holds until it is.
    /// </remarks>
    public static void PullText(Entity element, string? value)
    {
        if (element.IsNone) return;
        if (element == Focused) return;

        var text = value ?? string.Empty;
        if (Xui.GetText(element) == text) return;

        Xui.SetText(element, text);
    }

    /// <summary>Shows or hides an element.</summary>
    /// <remarks>
    /// What makes a document with a fixed set of elements show a list of a length nobody knew
    /// when the document was written: the rows past the end of the data are taken off screen
    /// rather than drawn empty.
    /// </remarks>
    public static void PullVisible(Entity element, bool value)
    {
        if (element.IsNone) return;

        Xui.SetVisible(element, value);
    }

    /// <summary>Reads a flag back from an element.</summary>
    public static bool PushFlag(Entity element, bool current) =>
        element.IsNone ? current : Xui.GetFlag(element);

    /// <summary>Reads a number back from an element.</summary>
    public static float PushNumber(Entity element, float current) =>
        element.IsNone ? current : Xui.GetNumber(element);

    /// <summary>Reads text back from an element.</summary>
    public static string PushText(Entity element, string current) =>
        element.IsNone ? current : Xui.GetText(element);

    /// <summary>
    /// Whether two slider values are the same as far as a person is concerned.
    /// </summary>
    /// <remarks>
    /// A slider's value is a float that has been through a step and a range, so writing back the
    /// number that was read is not reliably a no-op. Comparing exactly would make every frame a
    /// write, and every write on a slider being dragged fights the drag.
    /// </remarks>
    private static bool Nearly(float a, float b) => MathF.Abs(a - b) < 1e-4f;
}

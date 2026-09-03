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

    /// <summary>Writes text out to an input or an element's inner text.</summary>
    public static void PullText(Entity element, string? value)
    {
        if (element.IsNone) return;

        var text = value ?? string.Empty;
        if (Xui.GetText(element) == text) return;

        Xui.SetText(element, text);
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

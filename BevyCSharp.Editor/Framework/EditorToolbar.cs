using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>Which corner of the viewport a toolbar button sits in.</summary>
public enum ToolbarSlot
{
    /// <summary>Top left: the menu, and what acts on the whole editor.</summary>
    Left,

    /// <summary>Top center: what changes the mode of the viewport.</summary>
    Center,

    /// <summary>Top right: what the editor is doing.</summary>
    Right,

    /// <summary>Bottom right: what describes the view rather than acting on it.</summary>
    BottomRight,

    /// <summary>Left edge, stacked downwards: the modes a viewport can be in.</summary>
    LeftEdge,
}

/// <summary>
/// One button floating in the viewport.
/// </summary>
/// <param name="Slot">Which corner it sits in.</param>
/// <param name="Icon">
/// A file under the asset root, or <see langword="null"/> for a button that is only words.
/// </param>
/// <param name="Label">What it says beside the icon, if anything.</param>
/// <param name="Run">What pressing it does.</param>
/// <param name="Active">Whether it is drawn as the one in force.</param>
/// <param name="Order">Where it sits among its neighbors. Lower is first.</param>
/// <param name="Enabled">
/// Whether it can be pressed at all, or nothing for one that always can. A button for something
/// there is nothing to do is drawn dim and answers no click, which is how a person learns there is
/// nothing to undo without pressing it and watching nothing happen.
/// </param>
/// <param name="Tip">
/// What it says when the pointer rests on it, or nothing to say what it is called. A button that
/// is only a picture is a button nobody can read until they press it, so one of these is written
/// for every one of those.
/// </param>
/// <remarks>
/// The same shape as a menu row and for the same reason, which is that a game adding a mode to the
/// viewport should add a line rather than edit a panel. Both halves are optional and either is
/// enough. A picture alone makes a round button, a word alone makes a pill, and both together make
/// a labeled one.
/// </remarks>
public sealed record ToolbarButton(
    ToolbarSlot Slot,
    string? Icon,
    Func<string> Label,
    Action<EcsWorld> Run,
    Func<bool>? Active = null,
    int Order = 0,
    string? Tip = null,
    Func<bool>? Enabled = null);

/// <summary>
/// The buttons floating in the viewport's corners.
/// </summary>
/// <remarks>
/// <para>
/// Not a bar. A bar across the top would push the columns down and take a strip of the scene
/// permanently; a handful of buttons in the corners takes only what they cover, and they follow
/// the viewport as panels open and close.
/// </para>
/// <para>
/// A table, like the menu, so what is on the toolbar is a decision a game can change. Four slots
/// rather than nine, because the corners a person looks at are the ones near the panels they are
/// working in, and a toolbar with nine places to look is a search.
/// </para>
/// </remarks>
public static class EditorToolbar
{
    private static readonly List<ToolbarButton> Buttons = [];

    /// <summary>Every button, in the order they were added.</summary>
    public static IReadOnlyList<ToolbarButton> All => Buttons;

    /// <summary>Adds a button.</summary>
    public static void Add(ToolbarButton button)
    {
        ArgumentNullException.ThrowIfNull(button);
        Buttons.Add(button);
    }

    /// <summary>Adds a button with an icon and a label that do not change.</summary>
    public static void Add(
        ToolbarSlot slot,
        string? icon,
        string label,
        Action<EcsWorld> run,
        int order = 0,
        string? tip = null,
        Func<bool>? enabled = null) =>
        Add(new ToolbarButton(
            slot, icon, () => label, run, Order: order, Tip: tip, Enabled: enabled));

    /// <summary>What is in one slot, in order.</summary>
    public static IReadOnlyList<ToolbarButton> Slot(ToolbarSlot slot)
    {
        var wanted = new List<ToolbarButton>();

        foreach (var button in Buttons)
        {
            if (button.Slot == slot) wanted.Add(button);
        }

        wanted.Sort((a, b) => a.Order.CompareTo(b.Order));
        return wanted;
    }

    /// <summary>Forgets everything, which a second editor in one process would need.</summary>
    public static void Clear() => Buttons.Clear();
}

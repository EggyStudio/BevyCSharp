using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>Which corner of the viewport a toolbar button sits in.</summary>
public enum ToolbarSlot
{
    /// <summary>Top left: the menu, and what acts on the whole editor.</summary>
    Left,

    /// <summary>Top centre: what changes the mode of the viewport.</summary>
    Centre,

    /// <summary>Top right: what the editor is doing.</summary>
    Right,

    /// <summary>Bottom right: what describes the view rather than acting on it.</summary>
    BottomRight,
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
/// <param name="Order">Where it sits among its neighbours. Lower is first.</param>
/// <remarks>
/// The same shape as a menu row and for the same reason: a game adding a mode to the viewport
/// should add a line, not edit a panel. Both halves are optional and either is enough: a picture
/// alone makes a round button, a word alone makes a pill, and both together make a labelled one.
/// </remarks>
public sealed record ToolbarButton(
    ToolbarSlot Slot,
    string? Icon,
    Func<string> Label,
    Action<EcsWorld> Run,
    Func<bool>? Active = null,
    int Order = 0);

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
/// rather than nine: the corners a person looks at are the ones near the panels they are working
/// in, and a toolbar with nine places to look is a search.
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
        ToolbarSlot slot, string? icon, string label, Action<EcsWorld> run, int order = 0) =>
        Add(new ToolbarButton(slot, icon, () => label, run, Order: order));

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

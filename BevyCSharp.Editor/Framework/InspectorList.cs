using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A line that draws itself and does something when it is pressed.
/// </summary>
/// <remarks>
/// The smallest useful contributed line, and the one everything else is built from: a name, a
/// button and something to run. What a list's add and remove rows are.
/// </remarks>
/// <param name="Name">What the row is called, or nothing for a row that is only a button.</param>
/// <param name="Button">What the button says.</param>
/// <param name="Pressed">What pressing it does.</param>
/// <param name="Indent">How far in the row sits.</param>
public sealed record InspectorAction(
    string Name, string Button, Action Pressed, int Indent = 0) : IInspectorLine
{
    /// <inheritdoc/>
    public void Draw(InspectorRow row)
    {
        if (Name.Length > 0) row.Name(Name, Indent);
        else row.Wide();

        row.Button(Button);
    }

    /// <inheritdoc/>
    public void Press(InspectorRow row) => Pressed();
}

/// <summary>
/// Drawing a list of things.
/// </summary>
/// <remarks>
/// <para>
/// A component cannot hold a list. Its fields are laid out in memory and read by the engine, so
/// what a component can carry is a fixed set of values, and no attribute can change that. What can
/// hold one is everything else an inspector shows: an asset's contents, a tool's own state, a
/// game's managed objects, the children of an entity.
/// </para>
/// <para>
/// So a list is offered to whatever has one rather than read off a field. A pass says how many
/// elements there are, draws one, and hands over what adding and removing mean; this arranges them
/// as a fold per element with the buttons in the usual places, so every list in the editor looks
/// and behaves the same however different the things in it are.
/// </para>
/// </remarks>
public static class InspectorList
{
    /// <summary>
    /// Adds a list to a plan.
    /// </summary>
    /// <param name="plan">What is being built.</param>
    /// <param name="key">
    /// What the list's fold is remembered by. It outlives the selection, so it should name what the
    /// list is rather than which thing happens to be selected.
    /// </param>
    /// <param name="name">What the list is called.</param>
    /// <param name="count">How many elements it has.</param>
    /// <param name="element">
    /// Draws one element's own lines into the plan. Called once per element with its index, and
    /// only for the elements that are on screen: a shut fold draws nothing inside it.
    /// </param>
    /// <param name="add">What adding an element does, or nothing for a list that cannot grow.</param>
    /// <param name="remove">What removing one does, or nothing for a list nothing can be taken from.</param>
    /// <param name="depth">How far in the whole list sits, for a list inside something else.</param>
    public static void Add(
        InspectorPlan plan,
        string key,
        string name,
        int count,
        Action<InspectorPlan, int> element,
        Action? add = null,
        Action<int>? remove = null,
        int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(element);

        // The count is on the heading, because how many there are is the first thing anybody wants
        // to know about a list and the only thing a shut one can still say.
        plan.Add(new InspectorLine(
            InspectorLineKind.Group,
            Text: $"{name} ({count})",
            Depth: depth,
            Key: key));

        if (plan.Folds.Contains(key)) return;

        for (var index = 0; index < count; index++)
        {
            var inside = $"{key}/{index}";

            plan.Add(new InspectorLine(
                InspectorLineKind.Group,
                Text: Ordinal(name, index),
                Depth: depth + 1,
                Key: inside));

            if (plan.Folds.Contains(inside)) continue;

            element(plan, index);

            if (remove is not { } take) continue;

            var taken = index;
            plan.Add(new InspectorAction(
                string.Empty, "Remove", () => take(taken), depth + 2));
        }

        if (add is not { } grow) return;

        plan.Add(new InspectorAction(string.Empty, $"Add {name}", grow, depth + 1));
    }

    /// <summary>What one element of a list is called.</summary>
    /// <remarks>
    /// The list's own name with a number after it, in the numbering everybody outside a program
    /// uses. A fold called "0" says nothing about what is in it.
    /// </remarks>
    private static string Ordinal(string name, int index) => $"{Singular(name)} {index + 1}";

    /// <summary>A list's name as one of the things in it.</summary>
    /// <remarks>
    /// The obvious rule and no more: a name ending in s loses it. Anything cleverer is a table of
    /// English irregulars inside an editor, and whoever names a list something that reads badly can
    /// name the elements themselves.
    /// </remarks>
    private static string Singular(string name) =>
        name.Length > 1 && name.EndsWith('s') ? name[..^1] : name;
}

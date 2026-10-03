using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Adds an entity, offering every kind there is in a <see cref="PickerWindow"/> and putting the new
/// one under whatever is selected.
/// </summary>
/// <remarks>
/// <para>
/// The list is every row under <c>Spawn/</c> in the menu, and a game adds to it by adding a row
/// there, so <c>EditorMenu.Command("Spawn/Enemy", …)</c> is the whole of offering one here.
/// </para>
/// <para>
/// What is added goes under the entity selected when the window opened, and keeps its place in the
/// world, as a child added in Godot does. With nothing selected it goes at the top of the world.
/// Undoing it takes it away, as one step.
/// </para>
/// </remarks>
public static class AddEntityWindow
{
    /// <summary>The branch of the menu the kinds are read from.</summary>
    private const string Branch = "Spawn/";

    /// <summary>Opens it, over whatever is selected now.</summary>
    public static void Open()
    {
        var parent = EditorSelection.Current;

        // Where the new one goes, said in the box's hint, since the window has no heading.
        var under = !parent.IsNone && EditorShell.Context is { } ctx && ctx.Ecs.IsAlive(parent)
            ? ctx.Ecs.NameOf(parent)
            : null;

        PickerWindow.Open(
            under is { Length: > 0 } ? $"Search, adds under {under}" : "Search, adds at the top level",
            () => Kinds(parent));
    }

    /// <summary>
    /// Every row under the spawn branch, in the order the menu has them.
    /// </summary>
    /// <remarks>
    /// Labeled with the whole path below the branch, so typing <c>light</c> finds every light and
    /// <c>point</c> finds the one.
    /// </remarks>
    private static List<PickerItem> Kinds(Entity parent)
    {
        var rows = new List<MenuItem>();

        foreach (var item in EditorMenu.All)
        {
            if (item.Kind != MenuKind.Command || item.Run is null) continue;
            if (!item.Path.StartsWith(Branch, StringComparison.Ordinal)) continue;

            rows.Add(item);
        }

        // Where the menu lists them, by order, then by where they sit, so a light comes after the
        // shapes whatever order they were registered in.
        rows.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Path, b.Path));

        return rows.ConvertAll(row => new PickerItem(row.Path[Branch.Length..], row.Icon, ctx => Add(ctx, row, parent)));
    }

    /// <summary>Makes one, and puts it under what was selected, where it stands.</summary>
    /// <remarks>
    /// The spawn selects what it made and records taking it away, so the move under the parent
    /// is not recorded again. Undoing the spawn despawns it wherever it has ended up, which takes
    /// the move with it.
    /// </remarks>
    private static void Add(BehaviorContext ctx, MenuItem kind, Entity parent)
    {
        kind.Run?.Invoke(ctx.Ecs);

        var made = EditorSelection.Current;

        if (made.IsNone || parent.IsNone || made == parent || !ctx.Ecs.IsAlive(parent)) return;

        EditorHierarchy.Reparent(ctx.Ecs, made, parent, record: false);
        WorldPanel.Unfold(parent);
    }
}

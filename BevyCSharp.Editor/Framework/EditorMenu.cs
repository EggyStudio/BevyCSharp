using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>What a menu row does when it is clicked.</summary>
public enum MenuKind
{
    /// <summary>Runs something and closes the menu.</summary>
    Command,

    /// <summary>Turns something on or off and shows which it is.</summary>
    Toggle,

    /// <summary>Opens the level below it.</summary>
    Submenu,

    /// <summary>A line, for grouping. Not clickable.</summary>
    Separator,
}

/// <summary>
/// One entry in a menu, named by its path.
/// </summary>
/// <param name="Path">
/// Slash separated, so <c>Panels/World/Show interface</c> is a row two levels down. The path is
/// the whole structure: nothing declares a submenu, they appear because something under them does.
/// </param>
/// <param name="Kind">What the row does.</param>
/// <param name="Run">What clicking it runs, or nothing for a separator or a submenu.</param>
/// <param name="Checked">Whether a toggle currently reads as on.</param>
/// <param name="Enabled">Whether it can be clicked at all.</param>
/// <param name="Order">Where it sits among its siblings. Lower is first.</param>
public sealed record MenuItem(
    string Path,
    MenuKind Kind = MenuKind.Command,
    Action<EcsWorld>? Run = null,
    Func<bool>? Checked = null,
    Func<bool>? Enabled = null,
    int Order = 0)
{
    /// <summary>The part shown on the row, which is the last part of the path.</summary>
    public string Label
    {
        get
        {
            var cut = Path.LastIndexOf('/');
            return cut < 0 ? Path : Path[(cut + 1)..];
        }
    }

    /// <summary>The path of the level this row belongs to, or the empty string for the root.</summary>
    public string Parent
    {
        get
        {
            var cut = Path.LastIndexOf('/');
            return cut < 0 ? string.Empty : Path[..cut];
        }
    }
}

/// <summary>
/// Everything the editor can be told to do, as one table of paths.
/// </summary>
/// <remarks>
/// <para>
/// A menu is data, for the same reason a layout is: a hamburger menu, a right-click on the world,
/// a right-click on an entity and a dropdown on a field are the same mechanism pointed at
/// different paths, and anything that wants to add a command adds a row rather than editing a
/// panel. A game's own code can add to it as readily as the editor does, which is what makes the
/// shipped editor a starting point.
/// </para>
/// <para>
/// Paths build the structure. Adding <c>Panels/Rendering</c> makes <c>Panels</c> a submenu without
/// anything declaring one, and a menu panel showing a level asks for that level's rows.
/// </para>
/// </remarks>
public static class EditorMenu
{
    private static readonly List<MenuItem> Items = [];

    /// <summary>Every row, in the order they were added.</summary>
    public static IReadOnlyList<MenuItem> All => Items;

    /// <summary>Adds a row, replacing any with the same path.</summary>
    public static void Add(MenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Items.RemoveAll(existing => existing.Path == item.Path);
        Items.Add(item);
    }

    /// <summary>Adds a command.</summary>
    public static void Command(string path, Action<EcsWorld> run, int order = 0) =>
        Add(new MenuItem(path, MenuKind.Command, run, Order: order));

    /// <summary>Adds a toggle, which shows a mark when <paramref name="isOn"/> answers true.</summary>
    public static void Toggle(string path, Action<EcsWorld> run, Func<bool> isOn, int order = 0) =>
        Add(new MenuItem(path, MenuKind.Toggle, run, isOn, Order: order));

    /// <summary>Adds a line between groups.</summary>
    public static void Separator(string path, int order = 0) =>
        Add(new MenuItem(path, MenuKind.Separator, Order: order));

    /// <summary>Forgets every row under a path, which a context menu rebuilt per click needs.</summary>
    public static void Clear(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Items.RemoveAll(item => item.Path == path || item.Path.StartsWith(path + "/", StringComparison.Ordinal));
    }

    /// <summary>
    /// The rows of one level, with a submenu row for each level below it.
    /// </summary>
    /// <remarks>
    /// Built rather than stored. The table holds leaves and the structure is implied by their
    /// paths, so a level's rows are whatever sits directly under it plus one row per deeper
    /// branch, and nothing has to be kept in step.
    /// </remarks>
    public static IReadOnlyList<MenuItem> Level(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var rows = new List<MenuItem>();
        var branches = new List<string>();

        foreach (var item in Items)
        {
            if (item.Parent == path)
            {
                rows.Add(item);
                continue;
            }

            // Something deeper: the branch it goes through becomes a row of this level.
            var prefix = path.Length == 0 ? string.Empty : path + "/";
            if (!item.Path.StartsWith(prefix, StringComparison.Ordinal)) continue;

            var rest = item.Path[prefix.Length..];
            var cut = rest.IndexOf('/');
            if (cut < 0) continue;

            var branch = prefix + rest[..cut];
            if (!branches.Contains(branch)) branches.Add(branch);
        }

        foreach (var branch in branches)
        {
            if (rows.Any(row => row.Path == branch)) continue;

            rows.Add(new MenuItem(branch, MenuKind.Submenu, Order: OrderOf(branch)));
        }

        rows.Sort((a, b) =>
        {
            var order = a.Order.CompareTo(b.Order);
            return order != 0 ? order : string.CompareOrdinal(a.Label, b.Label);
        });

        return rows;
    }

    /// <summary>Whether a path has anything under it.</summary>
    public static bool HasLevel(string path) => Level(path).Count > 0;

    /// <summary>The row at a path, or <see langword="null"/>.</summary>
    public static MenuItem? Find(string path) =>
        Items.FirstOrDefault(item => item.Path == path);

    /// <summary>
    /// The order a branch inherits, which is the lowest of anything under it.
    /// </summary>
    /// <remarks>
    /// So that a group can be placed in the menu by ordering one of its rows, rather than by
    /// declaring the group itself somewhere else.
    /// </remarks>
    private static int OrderOf(string branch)
    {
        var lowest = int.MaxValue;

        foreach (var item in Items)
        {
            if (!item.Path.StartsWith(branch + "/", StringComparison.Ordinal)) continue;
            lowest = Math.Min(lowest, item.Order);
        }

        return lowest == int.MaxValue ? 0 : lowest;
    }
}

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// One panel that lives as a tab along the bottom.
/// </summary>
/// <param name="Name">What the tab says.</param>
/// <param name="Create">Builds the panel when the tab is opened.</param>
public sealed record EditorTabEntry(string Name, Func<IEditorPanel> Create)
{
    /// <summary>
    /// The panel once it has been opened, whether or not it is currently on screen.
    /// </summary>
    /// <remarks>
    /// Built on first use and kept for good. Minimising a tab conceals its panel rather than
    /// closing it, so switching between two tabs is two writes to a display property instead of
    /// two documents leaving and joining the interface's list. Doing it the other way makes a tab
    /// blink, come back at the wrong size, and eventually not come back at all.
    /// </remarks>
    public IEditorPanel? Panel { get; internal set; }

    /// <summary>Whether the tab is currently showing its panel.</summary>
    public bool IsOpen => Panel is { } panel && EditorShell.IsShowing(panel);
}

/// <summary>
/// The tabs along the bottom, and the order they are in.
/// </summary>
/// <remarks>
/// <para>
/// A tab is a panel that is usually minimised. Unreal puts its content browser here for the same
/// reason: it is wanted often and not continuously, and a strip of names costs one row of pixels
/// while a docked browser costs a third of the screen.
/// </para>
/// <para>
/// Nothing about which panels these are is built in. A tab is a name and a way to make a panel, so
/// anything can be one, and the order is a list that a drag rearranges.
/// </para>
/// </remarks>
public static class EditorTabs
{
    private static readonly List<EditorTabEntry> Entries = [];

    /// <summary>The tabs, left to right.</summary>
    public static IReadOnlyList<EditorTabEntry> All => Entries;

    /// <summary>Adds a tab, minimised.</summary>
    public static EditorTabEntry Add(string name, Func<IEditorPanel> create)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(create);

        var entry = new EditorTabEntry(name, create);
        Entries.Add(entry);
        return entry;
    }

    /// <summary>The tab of a given name, or <see langword="null"/>.</summary>
    public static EditorTabEntry? Find(string name) =>
        Entries.FirstOrDefault(entry => entry.Name == name);

    /// <summary>Opens a minimised tab, or minimises an open one.</summary>
    public static void Toggle(EditorTabEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsOpen)
        {
            EditorShell.Conceal(entry.Panel!);
            return;
        }

        // One at a time, which is what a tab means. They share the band along the bottom of the
        // viewport, and two of them in it would be two panels in one place.
        foreach (var other in Entries)
        {
            if (other.Panel is { } showing) EditorShell.Conceal(showing);
        }

        // Built the first time it is asked for and concealed thereafter, so exactly one document
        // joins the interface per tab per session and switching costs nothing.
        entry.Panel ??= EditorShell.Show(entry.Create());
        EditorShell.Reveal(entry.Panel);
    }

    /// <summary>Opens a tab if it is not open already.</summary>
    public static void Open(EditorTabEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!entry.IsOpen) Toggle(entry);
    }

    /// <summary>
    /// Moves a tab to another place in the strip.
    /// </summary>
    /// <remarks>
    /// What a drag along the strip does. The order is the whole of a tab's position, so this is
    /// all rearranging them takes.
    /// </remarks>
    public static void Reorder(int from, int to)
    {
        if (from < 0 || from >= Entries.Count) return;

        to = Math.Clamp(to, 0, Entries.Count - 1);
        if (from == to) return;

        var entry = Entries[from];
        Entries.RemoveAt(from);
        Entries.Insert(to, entry);
    }

    /// <summary>Forgets a panel that was closed by something other than its tab.</summary>
    /// <remarks>
    /// Called by the shell when any panel closes, so that a tab whose panel was closed from a menu
    /// reads as minimised rather than as open with nothing behind it.
    /// </remarks>
    internal static void Closed(IEditorPanel panel)
    {
        foreach (var entry in Entries)
        {
            if (ReferenceEquals(entry.Panel, panel)) entry.Panel = null;
        }
    }
}

using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>What a setting is, which decides how it is drawn and edited.</summary>
public enum SettingKind
{
    /// <summary>A line of text.</summary>
    Text,

    /// <summary>A number.</summary>
    Number,

    /// <summary>On or off.</summary>
    Flag,

    /// <summary>One of a fixed set of names, which <see cref="EditorSetting.Options"/> lists.</summary>
    Choice,

    /// <summary>Something to read and not change.</summary>
    Fact,

    /// <summary>Something to press.</summary>
    Action,

    /// <summary>A heading, with nothing to read or write.</summary>
    Heading,
}

/// <summary>
/// One line of a settings page.
/// </summary>
/// <param name="Page">Which page it belongs to.</param>
/// <param name="Label">What it is called.</param>
/// <param name="Kind">How it is drawn.</param>
/// <param name="Read">What it says now, as text. Null for a heading or an action.</param>
/// <param name="Write">What to do with a new value. Null for anything that cannot be changed.</param>
/// <param name="Options">The names a <see cref="SettingKind.Choice"/> may take.</param>
/// <param name="Order">Where it sits on its page. Lower is first.</param>
/// <remarks>
/// Text both ways, whatever the kind. Every editor of a value in this project is a box with
/// characters in it or a checkbox, the panel already knows how to parse each kind back, and a
/// setting that stored its own type would need one of these per type. What a setting actually
/// keeps is the closure's business.
/// </remarks>
public sealed record EditorSetting(
    string Page,
    string Label,
    SettingKind Kind,
    Func<string>? Read = null,
    Action<string>? Write = null,
    IReadOnlyList<string>? Options = null,
    int Order = 0);

/// <summary>
/// The pages of settings, and what is on them.
/// </summary>
/// <remarks>
/// <para>
/// A table, like the menu and the toolbar. Everything an editor keeps that is not part of the
/// world belongs somewhere a person can find it, and the place to put it should be a line of
/// registration rather than a panel to edit. Otherwise every new preference is a new panel and
/// nobody adds one.
/// </para>
/// <para>
/// A page is a name and nothing else; it exists because something on it does. That keeps the two
/// halves from drifting apart: there can be no empty page and no setting with nowhere to live.
/// </para>
/// </remarks>
public static class EditorSettings
{
    private static readonly List<EditorSetting> Entries = [];
    private static readonly List<string> Order = [];

    /// <summary>Every setting, in the order they were added.</summary>
    public static IReadOnlyList<EditorSetting> All => Entries;

    /// <summary>The pages that have anything on them, in the order they were first named.</summary>
    public static IReadOnlyList<string> Pages
    {
        get
        {
            var pages = new List<string>();

            foreach (var page in Order)
            {
                foreach (var entry in Entries)
                {
                    if (entry.Page != page) continue;

                    pages.Add(page);
                    break;
                }
            }

            return pages;
        }
    }

    /// <summary>Adds a setting, naming its page if that page is new.</summary>
    public static void Add(EditorSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        Entries.Add(setting);
        if (!Order.Contains(setting.Page)) Order.Add(setting.Page);
    }

    /// <summary>A line of text somebody can edit.</summary>
    public static void Text(
        string page, string label, Func<string> read, Action<string> write, int order = 0) =>
        Add(new EditorSetting(page, label, SettingKind.Text, read, write, Order: order));

    /// <summary>A number somebody can edit.</summary>
    public static void Number(
        string page, string label, Func<float> read, Action<float> write, int order = 0) =>
        Add(new EditorSetting(
            page,
            label,
            SettingKind.Number,
            () => read().ToString("0.###"),
            text =>
            {
                if (float.TryParse(text, out var value)) write(value);
            },
            Order: order));

    /// <summary>Something that is on or off.</summary>
    public static void Flag(
        string page, string label, Func<bool> read, Action<bool> write, int order = 0) =>
        Add(new EditorSetting(
            page,
            label,
            SettingKind.Flag,
            () => read() ? "1" : "0",
            text => write(text == "1"),
            Order: order));

    /// <summary>One of a fixed set of names.</summary>
    public static void Choice(
        string page,
        string label,
        IReadOnlyList<string> options,
        Func<string> read,
        Action<string> write,
        int order = 0) =>
        Add(new EditorSetting(page, label, SettingKind.Choice, read, write, options, order));

    /// <summary>Something to read and not change.</summary>
    public static void Fact(string page, string label, Func<string> read, int order = 0) =>
        Add(new EditorSetting(page, label, SettingKind.Fact, read, Order: order));

    /// <summary>Something to press.</summary>
    public static void Action(string page, string label, Action run, int order = 0) =>
        Add(new EditorSetting(
            page, label, SettingKind.Action, null, _ => run(), Order: order));

    /// <summary>A heading, for grouping a long page.</summary>
    public static void Heading(string page, string label, int order = 0) =>
        Add(new EditorSetting(page, label, SettingKind.Heading, Order: order));

    /// <summary>What is on one page, in order.</summary>
    public static IReadOnlyList<EditorSetting> On(string page)
    {
        var wanted = new List<EditorSetting>();

        foreach (var entry in Entries)
        {
            if (entry.Page == page) wanted.Add(entry);
        }

        wanted.Sort((a, b) => a.Order.CompareTo(b.Order));
        return wanted;
    }

    /// <summary>
    /// Every setting that can be changed, as lines of text.
    /// </summary>
    /// <remarks>
    /// Only what somebody can change is written down. A fact is read from the thing it describes
    /// and an action does something; writing either would be recording an answer that the next run
    /// works out for itself, and restoring it would be a lie.
    /// </remarks>
    public static string Describe()
    {
        var lines = new List<string>();

        foreach (var entry in Entries)
        {
            if (entry.Read is not { } read) continue;
            if (entry.Write is null) continue;

            lines.Add($"{entry.Page}\t{entry.Label}\t{read()}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Puts saved values back, ignoring anything it no longer recognises.
    /// </summary>
    /// <remarks>
    /// A line naming a setting this build does not have is skipped rather than reported. A saved
    /// file outlives the version that wrote it, and refusing to start because somebody's
    /// preferences mention a setting that has since been renamed helps nobody.
    /// </remarks>
    public static void Restore(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (var line in text.Split('\n'))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;

            foreach (var entry in Entries)
            {
                if (entry.Page != parts[0] || entry.Label != parts[1]) continue;

                entry.Write?.Invoke(parts[2]);
                break;
            }
        }
    }

    /// <summary>Forgets everything, which a second editor in one process would need.</summary>
    public static void Clear()
    {
        Entries.Clear();
        Order.Clear();
    }
}

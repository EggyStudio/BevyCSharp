using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The files the editor is running out of.
/// </summary>
/// <remarks>
/// <para>
/// A browser over the asset directory itself, so what is listed is exactly what a path in a
/// document or a script would find. It lives along the bottom because it wants width rather than
/// height, and it is a tab because it is opened, used and minimised again.
/// </para>
/// <para>
/// Clicking a file points the asset panel at it. Clicking a directory goes into it. Nothing here
/// imports or catalogues anything, because the engine does not work that way either.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/assets.html",
    Root = "#assets",
    Handle = "#assets-title",
    Dock = EditorDock.Bottom,
    Height = 176f)]
public sealed partial class AssetsPanel
{
    /// <summary>How many entries the document can draw.</summary>
    public const int Rows = 12;

    /// <summary>What each row says.</summary>
    [Bind("#arow", Count = Rows)]
    public string[] Labels = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#arow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Which directory is being looked at.</summary>
    [Bind("#a-path", Mode = BindMode.OneWay)]
    public string Where =>
        EditorAssets.Directory.Length == 0 ? "assets" : "assets/" + EditorAssets.Directory;

    /// <summary>Goes up a directory.</summary>
    [Bind("#a-up", Mode = BindMode.OneWay)]
    public string UpIcon => EditorIcons.Up;

    /// <summary>What each row stands for.</summary>
    private readonly AssetEntry[] _entries = new AssetEntry[Rows];

    /// <summary>How far down the listing the pool is looking.</summary>
    private int _scroll;

    /// <summary>Fills the rows from the directory.</summary>
    [OnRefresh]
    public void Fill()
    {
        Roll();

        var entries = EditorAssets.List();
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, entries.Count - Rows));

        var written = 0;
        for (var i = _scroll; i < entries.Count && written < Rows; i++)
        {
            var entry = entries[i];

            // A directory says so by ending in a slash, which is how a path reads anyway, and a
            // selected file is marked the way a selected row is everywhere else.
            Labels[written] = entry.IsDirectory
                ? "  " + entry.Name + EditorIcons.Directory
                : (entry.Path == EditorAssets.Selected ? EditorIcons.Selected + " " : "  ")
                    + entry.Name;
            _entries[written] = entry;
            Shown[written] = true;
            written++;
        }

        for (var i = written; i < Rows; i++)
        {
            Labels[i] = string.Empty;
            _entries[i] = default;
            Shown[i] = false;
        }
    }

    /// <summary>Scrolls the listing when the wheel is rolled over it.</summary>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        _scroll = Math.Max(0, _scroll - ((int)wheel * 2));
    }

    /// <summary>Goes into a directory, or points the asset panel at a file.</summary>
    [Command("#arow", Count = Rows)]
    public void Choose(int row)
    {
        if (!Shown[row]) return;

        var entry = _entries[row];

        if (entry.IsDirectory)
        {
            EditorAssets.Enter(entry.Path);
            _scroll = 0;
            return;
        }

        EditorAssets.Select(entry.Path);

        // The right column answers what is selected, whichever kind of thing that is, so picking
        // a file opens the panel that describes one.
        if (EditorShell.Find<AssetPanel>() is null) EditorShell.Show(new AssetPanel());
    }

    /// <summary>Goes up one directory.</summary>
    [Command("#a-up")]
    public void Up()
    {
        EditorAssets.Up();
        _scroll = 0;
    }

    /// <summary>Compiles the scripts directory again.</summary>
    /// <remarks>
    /// The watcher does this on its own when a file changes. The button is for the case it cannot
    /// see: a file written by something that does not touch the directory it watches.
    /// </remarks>
    [Command("#a-reload")]
    public void Reload() => EditorScripts.Reload();
}

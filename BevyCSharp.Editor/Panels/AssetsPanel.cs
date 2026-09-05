using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The files the editor is running out of: directories on the left, what is in one on the right.
/// </summary>
/// <remarks>
/// <para>
/// A browser over the asset directory itself, so what is listed is exactly what a path in a
/// document or a script would find. It lives along the bottom of the viewport because it wants
/// width rather than height, and it is a tab because it is opened, used and minimised again.
/// </para>
/// <para>
/// Split in two the way every content browser is: the shape of the directory on one side and its
/// contents on the other, so going somewhere and looking at what is there are two motions rather
/// than one list that keeps changing under the pointer.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/assets.html",
    Root = "#assets",
    Handle = "#assets-title",
    Dock = EditorDock.Bottom)]
public sealed partial class AssetsPanel
{
    /// <summary>How many directories the tree can draw.</summary>
    public const int Folders = 10;

    /// <summary>How many files the tiles can draw.</summary>
    public const int Tiles = 24;

    /// <summary>What each row of the tree says.</summary>
    [Bind("#aftext", Count = Folders)]
    public string[] FolderNames = new string[Folders];

    /// <summary>Which rows of the tree stand for anything.</summary>
    [Show("#afolder", Count = Folders)]
    public bool[] FolderShown = new bool[Folders];

    /// <summary>What each tile says.</summary>
    [Bind("#attext", Count = Tiles)]
    public string[] TileNames = new string[Tiles];

    /// <summary>Which tiles stand for anything.</summary>
    [Show("#atile", Count = Tiles)]
    public bool[] TileShown = new bool[Tiles];

    /// <summary>Which directory is being looked at.</summary>
    [Bind("#a-path", Mode = BindMode.OneWay)]
    public string Where =>
        EditorAssets.Directory.Length == 0 ? "assets" : "assets/" + EditorAssets.Directory;

    /// <summary>What each row of the tree stands for: a path, or null for the way up.</summary>
    private readonly string?[] _folders = new string?[Folders];

    /// <summary>What each tile stands for.</summary>
    private readonly AssetEntry[] _tiles = new AssetEntry[Tiles];

    /// <summary>How far down the files the tiles are looking.</summary>
    private int _scroll;

    /// <summary>Fills the tree and the tiles from the directory.</summary>
    [OnRefresh]
    public void Fill()
    {
        Roll();
        Measure();

        var entries = EditorAssets.List();
        var written = 0;

        // The way up is the first row of the tree, so a person is never stuck in a directory.
        if (EditorAssets.Directory.Length > 0)
        {
            FolderNames[written] = "..";
            _folders[written] = null;
            FolderShown[written] = true;
            written++;
        }

        foreach (var entry in entries)
        {
            if (!entry.IsDirectory) continue;
            if (written >= Folders) break;

            FolderNames[written] = entry.Name + EditorIcons.Directory;
            _folders[written] = entry.Path;
            FolderShown[written] = true;
            written++;
        }

        for (var i = written; i < Folders; i++)
        {
            FolderNames[i] = string.Empty;
            _folders[i] = null;
            FolderShown[i] = false;
        }

        var files = new List<AssetEntry>();
        foreach (var entry in entries)
        {
            if (!entry.IsDirectory) files.Add(entry);
        }

        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, files.Count - Tiles));

        var tile = 0;
        for (var i = _scroll; i < files.Count && tile < Tiles; i++)
        {
            var entry = files[i];

            TileNames[tile] = entry.Path == EditorAssets.Selected
                ? EditorIcons.Selected + " " + entry.Name
                : "  " + entry.Name;

            _tiles[tile] = entry;
            TileShown[tile] = true;
            tile++;
        }

        for (var i = tile; i < Tiles; i++)
        {
            TileNames[i] = string.Empty;
            _tiles[i] = default;
            TileShown[i] = false;
        }
    }

    /// <summary>How wide a tile would like to be, before the room is divided up.</summary>
    private const float IdealTile = 132f;

    /// <summary>How far apart the tiles sit, matching the stylesheet's gap.</summary>
    private const float TileGap = 6f;

    /// <summary>The width every tile was last given.</summary>
    private float _tileWidth;

    /// <summary>
    /// Divides the pane into a whole number of columns and gives every tile that width.
    /// </summary>
    /// <remarks>
    /// Worked out here rather than left to the layout, because flex has no way to say "all the
    /// same size and filling the line". Told to stretch, the tiles on a part-filled last line
    /// share the room a full line's worth had and come out twice as wide as the tiles above them;
    /// told not to, the right-hand edge is ragged. A width the panel computes is neither.
    /// </remarks>
    private void Measure()
    {
        if (Window is not { IsOpen: true } window) return;

        var pane = window.Element("atiles");
        if (pane.IsNone || !Xui.TryRect(pane, out var rect)) return;
        if (rect.Width < 1f) return;

        var columns = Math.Max(1, (int)((rect.Width + TileGap) / (IdealTile + TileGap)));
        var width = MathF.Floor(((rect.Width + TileGap) / columns) - TileGap);

        if (MathF.Abs(width - _tileWidth) < 0.5f) return;

        _tileWidth = width;

        for (var i = 0; i < Tiles; i++)
        {
            var tile = window.Element($"atile-{i}");
            if (tile.IsNone) continue;

            // Width alone: naming neither edge leaves the tile where the wrap put it.
            Xui.SetRect(tile, float.NaN, float.NaN, width, float.NaN);
        }
    }

    /// <summary>Scrolls the tiles when the wheel is rolled over the panel.</summary>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        _scroll = Math.Max(0, _scroll - ((int)wheel * 4));
    }

    /// <summary>Goes into a directory, or up out of one.</summary>
    [Command("#afolder", Count = Folders)]
    public void Enter(int row)
    {
        if (!FolderShown[row]) return;

        if (_folders[row] is { } path) EditorAssets.Enter(path);
        else EditorAssets.Up();

        _scroll = 0;
    }

    /// <summary>Points the data panel at a file.</summary>
    [Command("#atile", Count = Tiles)]
    public void Choose(int tile)
    {
        if (!TileShown[tile]) return;

        // The right column answers what is selected, whichever kind of thing that is, and it
        // appears because something is selected rather than because this asked for it.
        EditorAssets.Select(_tiles[tile].Path);
    }

    /// <summary>Offers what can be done with a file.</summary>
    [Context("#atile", Count = Tiles)]
    public void TileMenu(int tile)
    {
        if (!TileShown[tile]) return;

        var entry = _tiles[tile];
        EditorAssets.Select(entry.Path);

        var (x, y) = EditorShell.Context?.Input.MousePosition ?? (0f, 0f);

        EditorShell.ShowMenu(
            entry.Name,
            [
                new MenuItem(
                    "Reload scripts",
                    MenuKind.Command,
                    static _ => EditorScripts.Reload()),
                new MenuItem(
                    "Copy path to the console",
                    MenuKind.Command,
                    _ => Console.WriteLine($"[assets] {entry.Path}")),
            ],
            x,
            y);
    }
}

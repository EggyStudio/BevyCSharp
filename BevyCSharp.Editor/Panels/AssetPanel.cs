using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What one file is.
/// </summary>
/// <remarks>
/// The right column answers what is selected, and an asset is a thing that can be selected, so it
/// gets a panel there beside the entity panel rather than a window of its own. What it can say is
/// what the file system knows plus what the engine would make of the extension, because nothing
/// here opens the file.
/// </remarks>
[EditorPanel(
    "panels/asset.html",
    Root = "#asset",
    Handle = "#asset-title",
    Dock = EditorDock.Right,
    Order = 10)]
public sealed partial class AssetPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 8;

    /// <summary>Each row's label.</summary>
    [Bind("#asname", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>Each row's value.</summary>
    [Bind("#asvalue", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#asrow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Which file is being described.</summary>
    [Bind("#as-name", Mode = BindMode.OneWay)]
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Reads what the file system says about the selected file.</summary>
    [OnRefresh]
    public void Fill()
    {
        var written = 0;

        if (EditorAssets.Selected is { } relative)
        {
            var absolute = EditorAssets.Absolute(relative);
            var file = new FileInfo(absolute);

            Subject = Path.GetFileName(relative);

            Write(ref written, "Path", relative);
            Write(ref written, "Kind", EditorAssets.KindOf(relative));

            if (file.Exists)
            {
                Write(ref written, "Size", Size(file.Length));
                Write(ref written, "Changed", file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
            }
            else
            {
                Write(ref written, "State", "gone from disk");
            }

            Write(
                ref written,
                "Reloads",
                EditorAssets.Reloads(relative) ? "yes, while running" : "no");

            // The path the engine would be given, which is the one to type into a document or a
            // script. Worth showing because it is not the same as the path on disk.
            Write(ref written, "Load as", relative);
        }
        else
        {
            Subject = "nothing selected";
            Write(ref written, "Assets", "pick a file below");
        }

        for (var i = written; i < Rows; i++)
        {
            Names[i] = string.Empty;
            Values[i] = string.Empty;
            Shown[i] = false;
        }
    }

    /// <summary>Fills in one row.</summary>
    private void Write(ref int row, string name, string value)
    {
        if (row >= Rows) return;

        Names[row] = name;
        Values[row] = value;
        Shown[row] = true;
        row++;
    }

    /// <summary>A byte count as a person reads one.</summary>
    private static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024f:0.#} KB",
        _ => $"{bytes / (1024f * 1024f):0.#} MB",
    };
}

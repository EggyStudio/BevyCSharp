using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The files the editor is running out of, as a tab along the bottom.
/// </summary>
/// <remarks>
/// A browser over the asset directory itself: what is listed is exactly what a path in a script or
/// a component would find, because nothing here imports or catalogues anything. What a row shows is
/// <see cref="EditorAssets"/>'s to answer; this draws it.
/// </remarks>
public static class AssetsTab
{
    /// <summary>Which folders are unfolded in the tree.</summary>
    private static readonly HashSet<string> Unfolded = [string.Empty];

    /// <summary>
    /// Draws it: the folders down the left, what is in the chosen one as tiles on the right.
    /// </summary>
    /// <remarks>
    /// The shape every asset browser has, and the reason is navigation. A single pane with a way
    /// back out means every move between two directories goes up through their parent, and where
    /// something is has to be held in the head instead of being on the screen.
    /// </remarks>
    public static void Draw()
    {
        var theme = EditorTheme.Current;

        var split = ImGuiTableFlags.Resizable
            | ImGuiTableFlags.NoBordersInBody
            | ImGuiTableFlags.NoSavedSettings;

        if (!ImGui.BeginTable("##assets", 2, split)) return;

        ImGui.TableSetupColumn("##tree", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("##tiles", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        Tree();

        ImGui.TableNextColumn();

        Tiles(theme);

        ImGui.EndTable();
    }

    /// <summary>The folders, from the asset root down.</summary>
    private static void Tree()
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0u);

        var open = ImGui.BeginChild("##tree", new Vector2(0f, 0f));

        ImGui.PopStyleColor();

        if (open) Branch(string.Empty, "assets");

        ImGui.EndChild();
    }

    /// <summary>One folder and, when it is unfolded, the folders under it.</summary>
    /// <param name="path">Its path under the asset root, empty for the root itself.</param>
    /// <param name="name">What to call it.</param>
    private static void Branch(string path, string name)
    {
        var children = EditorAssets.Directories(path);
        var here = EditorAssets.Directory == path;
        var unfolded = Unfolded.Contains(path);

        var flags = ImGuiTreeNodeFlags.OpenOnArrow
            | ImGuiTreeNodeFlags.SpanAvailWidth
            | ImGuiTreeNodeFlags.FramePadding;

        if (here) flags |= ImGuiTreeNodeFlags.Selected;
        if (children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
        if (unfolded) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        ImGui.SetNextItemOpen(unfolded, ImGuiCond.Always);

        var shown = ImGui.TreeNodeEx($"{name}##{path}", flags);

        // The arrow folds, the word walks. Clicking a folder's name is how somebody says they want
        // to look inside it, and folding is what the arrow is for.
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
        {
            EditorAssets.Enter(path);
            Unfolded.Add(path);
        }
        else if (ImGui.IsItemToggledOpen())
        {
            if (shown) Unfolded.Add(path);
            else Unfolded.Remove(path);
        }

        if (!shown) return;

        foreach (var (child, called) in children) Branch(child, called);

        ImGui.TreePop();
    }

    /// <summary>What is in the chosen folder.</summary>
    private static void Tiles(EditorTheme theme)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0u);

        var started = ImGui.BeginChild("##files", new Vector2(0f, 0f));

        ImGui.PopStyleColor();

        if (!started)
        {
            ImGui.EndChild();
            return;
        }

        var entries = EditorAssets.List();

        if (entries.Count == 0)
        {
            ImGui.TextDisabled("Nothing here");
            ImGui.EndChild();
            return;
        }

        // A grid of tiles rather than a list of names, because most of what is in here is a picture
        // or a mesh, and a name in a column says nothing about which one it is.
        var tile = 96f;
        var across = Math.Max(1, (int)(ImGui.GetContentRegionAvail().X / (tile + ImGui.GetStyle().ItemSpacing.X)));

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];

            if (index % across != 0) ImGui.SameLine();

            Tile(entry, tile, theme);
        }

        ImGui.EndChild();
    }

    /// <summary>One file or directory, as a tile.</summary>
    private static void Tile(AssetEntry entry, float size, EditorTheme theme)
    {
        var picked = EditorAssets.Selected == entry.Path;

        ImGui.BeginGroup();

        var at = ImGui.GetCursorScreenPos();
        var line = ImGui.GetTextLineHeight();

        ImGui.InvisibleButton($"##tile{entry.Path}", new Vector2(size, size));

        var over = ImGui.IsItemHovered();

        if (ImGui.IsItemClicked())
        {
            if (entry.IsDirectory) EditorAssets.Enter(entry.Path);
            else EditorAssets.Select(picked ? null : entry.Path);
        }

        var draw = ImGui.GetWindowDrawList();

        draw.AddRectFilled(
            at,
            at + new Vector2(size, size),
            ImGui.GetColorU32(picked
                ? EditorTheme.LiveAccent
                : over ? EditorTheme.LiveHover : EditorTheme.LiveGroup),
            ImGui.GetStyle().ChildRounding);

        // The picture that says what kind of thing it is, in the middle of the tile.
        var icon = entry.IsDirectory ? "icons/ui/folder.png" : EditorAssets.IconOf(entry.Path);

        if (ImGuiTextures.Load(icon) is var picture && picture != 0)
        {
            const float Mark = 34f;
            var middle = at + new Vector2((size - Mark) * 0.5f, (size - Mark) * 0.5f - (line * 0.6f));

            draw.AddImage(
                (IntPtr)picture,
                middle,
                middle + new Vector2(Mark, Mark),
                Vector2.Zero,
                Vector2.One,
                ImGui.GetColorU32(EditorTheme.IconTint(picked)));
        }

        // And its name under it, cut to what fits rather than spilling into the next tile.
        var name = Fit(entry.Name, size - 10f);
        var width = ImGui.CalcTextSize(name).X;

        draw.AddText(
            at + new Vector2((size - width) * 0.5f, size - line - 8f),
            ImGui.GetColorU32(picked ? theme.Text : EditorTheme.Alpha(theme.Text, 0.8f)),
            name);

        ImGui.EndGroup();

        if (over) ImGui.SetTooltip(entry.IsDirectory ? entry.Name : $"{entry.Name}  ({Say(entry.Size)})");
    }

    /// <summary>As much of a name as fits, with an ellipsis where the rest was.</summary>
    private static string Fit(string name, float room)
    {
        if (ImGui.CalcTextSize(name).X <= room) return name;

        for (var length = name.Length - 1; length > 1; length--)
        {
            var cut = string.Concat(name.AsSpan(0, length), "...");
            if (ImGui.CalcTextSize(cut).X <= room) return cut;
        }

        return name;
    }

    /// <summary>How large a file is, in the units a person reads.</summary>
    private static string Say(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024f:0.#} KB",
        _ => $"{bytes / (1024f * 1024f):0.#} MB",
    };
}

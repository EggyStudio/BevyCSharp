using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>One thing a <see cref="PickerWindow"/> offers.</summary>
/// <param name="Label">What the row says, which the search is matched against.</param>
/// <param name="Icon">The picture at the front of the row, or none.</param>
/// <param name="Pick">What choosing it does.</param>
/// <param name="Group">
/// The heading it is listed under, or nothing. Items of one group are given together, and a heading
/// is drawn where the group changes.
/// </param>
/// <param name="Path">
/// The asset it stands for, under the asset root, whose picture its tile wears in a grid, or
/// nothing for one with no file, which wears its icon.
/// </param>
public sealed record PickerItem(
    string Label, string? Icon, Action<BehaviorContext> Pick, string? Group = null, string? Path = null);

/// <summary>
/// A window in the middle of the screen that offers a list to choose one thing from, with a box to
/// narrow it down by typing. What adds an entity and what adds a component are both one of these.
/// </summary>
/// <remarks>
/// <para>
/// A window rather than a flyout by the button, the way Godot adds a node. A list that grows with a
/// project needs room and a search box, which a flyout hanging off a corner has neither of, and two
/// ways of adding something in one editor that look different read as two kinds of thing.
/// </para>
/// <para>
/// Keys do what they do in any list with a search box over it. Typing narrows it, the arrows move
/// the choice, Enter takes the chosen one, and Escape or a click outside closes it. A double click
/// takes one as well.
/// </para>
/// <para>
/// One at a time, since it is modal. The list is asked for again every frame, so what it offers
/// stays true while it is up.
/// </para>
/// </remarks>
public static class PickerWindow
{
    private const string Name = "##picker";

    private static bool _opening;
    private static string _search = string.Empty;
    private static int _chosen;
    private static string _hint = "Search";
    private static string _empty = "Nothing matches";
    private static string _verb = "Add";
    private static bool _grid;

    /// <summary>How many tiles the grid fitted across last frame, which the arrows move by.</summary>
    private static int _across = 1;
    private static Func<IReadOnlyList<PickerItem>> _items = static () => [];

    /// <summary>Whether it is up.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Opens it.</summary>
    /// <remarks>
    /// A popup belongs to the frame it is opened in, so this is remembered and put up on the next
    /// pass through the interface, the way the menu is.
    /// </remarks>
    /// <param name="hint">What the search box says while nothing is typed, which is the one line of the window with room to say what it is for.</param>
    /// <param name="items">What it offers, asked for every frame it is up.</param>
    /// <param name="empty">What it says when there is nothing to offer at all.</param>
    /// <param name="verb">What the button that takes the chosen one says.</param>
    /// <param name="grid">
    /// Whether to show the items as tiles, each wearing its asset's picture, as the asset browser
    /// does, rather than as rows. For picking an asset, which is found by how it looks.
    /// </param>
    public static void Open(
        string hint,
        Func<IReadOnlyList<PickerItem>> items,
        string empty = "Nothing matches",
        string verb = "Add",
        bool grid = false)
    {
        ArgumentNullException.ThrowIfNull(hint);
        ArgumentNullException.ThrowIfNull(items);

        _opening = true;
        _search = string.Empty;
        _chosen = 0;
        _hint = hint;
        _items = items;
        _empty = empty;
        _verb = verb;
        _grid = grid;
    }

    /// <summary>Draws it while it is up.</summary>
    internal static void Draw(BehaviorContext ctx)
    {
        if (_opening)
        {
            _opening = false;
            ImGui.OpenPopup(Name);
        }

        var window = ImGuiRuntime.Size;
        var size = _grid
            ? new Vector2(MathF.Min(640f, window.X - 40f), MathF.Min(560f, window.Y - 40f))
            : new Vector2(MathF.Min(420f, window.X - 40f), MathF.Min(460f, window.Y - 40f));

        // The dim behind the modal, drawn here rather than by ImGui, which dims the whole display
        // with a square rectangle and so darkens the window's transparent corners and gaps as well.
        // This one is the window's own shape, rounded as its corners are, so what is clear stays
        // clear. Before the modal's place and size are set, since the dim is a window of its own
        // and would take them, leaving the modal to shrink to its first rows.
        if (ImGui.IsPopupOpen(Name)) Dim();

        ImGui.SetNextWindowPos(window * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(size, ImGuiCond.Appearing);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, EditorSurface.Around);

        // A panel's color rather than a flyout's field gray. It holds a box to type in and a list,
        // and those are drawn on a panel everywhere else, so on the field gray the box would be the
        // color of what it sits on and could not be seen.
        ImGui.PushStyleColor(ImGuiCol.PopupBg, EditorTheme.LivePanel);

        // A modal, so the scene and the panels wait while it is up, and a press outside closes it
        // rather than reaching what is under it. No title bar, since ImGui's is a band of another
        // color across a rounded card, and a box to type in says what the window is for.
        ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, 0u);

        IsOpen = ImGui.BeginPopupModal(
            Name,
            ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

        ImGui.PopStyleColor();

        if (!IsOpen)
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            return;
        }

        var kinds = Matching();

        Search(kinds.Count);

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }

        var add = List(kinds);

        ImGui.Spacing();

        // Taken with the list's own keys or its button, and either way it runs once.
        add |= Buttons(kinds.Count > 0);

        if (add && _chosen < kinds.Count)
        {
            // Closed first, since what is picked may open something of its own.
            ImGui.CloseCurrentPopup();
            kinds[_chosen].Pick(ctx);
        }

        // A click outside a modal is not a way out of it in ImGui, and it is in every window of
        // this kind, so it is made one here. Not on the frame it appears, which is the frame of
        // the click that opened it.
        if (!ImGui.IsWindowAppearing()
            && ImGui.IsMouseClicked(ImGuiMouseButton.Left)
            && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.ChildWindows))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
    }

    /// <summary>The box that narrows the list, which has the keyboard from the moment it opens.</summary>
    private static void Search(int count)
    {
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();

        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextWithHint("##search", _hint, ref _search, 128)) _chosen = 0;

        // The arrows move the choice while the box keeps the keyboard, so the hand that is typing
        // can pick without reaching for the pointer.
        // A row of tiles at a time in a grid, which is where down goes on a grid.
        var step = _grid ? Math.Max(1, _across) : 1;
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) _chosen = Math.Min(_chosen + step, Math.Max(0, count - 1));
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) _chosen = Math.Max(_chosen - step, 0);
    }

    /// <summary>What matches, one row each, and whether one was asked to be taken.</summary>
    private static bool List(IReadOnlyList<PickerItem> kinds)
    {
        var add = ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter);

        // What the buttons under it take, so the list has the rest of the window and they stay put.
        var buttons = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y;

        if (!EditorSurface.Region("##kinds", new Vector2(0f, -buttons)))
        {
            EditorSurface.EndRegion();
            return add;
        }

        if (kinds.Count == 0) ImGui.TextDisabled(_search.Trim().Length > 0 ? "Nothing matches" : _empty);

        if (_grid)
        {
            add |= Grid(kinds);
            EditorSurface.EndRegion();
            return add;
        }

        var line = ImGui.GetTextLineHeight();
        var height = ImGui.GetFrameHeight();

        for (var index = 0; index < kinds.Count; index++)
        {
            var kind = kinds[index];

            // A heading where a group starts, so built-in shapes, what the scene already uses and
            // files read as three lists rather than one run of names.
            if (kind.Group is { Length: > 0 } group && (index == 0 || kinds[index - 1].Group != group))
            {
                if (index > 0) ImGui.Spacing();
                ImGui.TextDisabled(group);
            }

            var chosen = index == _chosen;
            var at = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;

            ImGui.InvisibleButton($"##kind{index}", new Vector2(width, height));

            var over = ImGui.IsItemHovered();

            if (ImGui.IsItemClicked()) _chosen = index;

            if (over && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _chosen = index;
                add = true;
            }

            // Kept in view as the arrows walk it past the bottom.
            if (chosen && (ImGui.IsKeyPressed(ImGuiKey.DownArrow) || ImGui.IsKeyPressed(ImGuiKey.UpArrow)))
            {
                ImGui.SetScrollHereY();
            }

            var draw = ImGui.GetWindowDrawList();

            if (chosen || over)
            {
                EditorDraw.Rounded(
                    at,
                    at + new Vector2(width, height),
                    ImGui.GetStyle().FrameRounding,
                    ImGui.GetColorU32(chosen ? EditorTheme.LiveAccent : EditorTheme.LiveLift),
                    draw);
            }

            var middle = at.Y + ((height - line) * 0.5f);

            EditorDraw.Icon(draw, kind.Icon, new Vector2(at.X + EditorSurface.Air, middle), line, chosen);

            draw.AddText(
                new Vector2(at.X + EditorSurface.Air + line + ImGui.GetStyle().ItemSpacing.X, middle),
                ImGui.GetColorU32(EditorTheme.Ink(chosen)),
                kind.Label);
        }

        EditorSurface.EndRegion();

        return add;
    }

    /// <summary>How wide and tall a tile is in the grid.</summary>
    private const float Tile = 88f;

    /// <summary>
    /// What matches, as tiles in rows under their groups' headings, and whether one was taken with
    /// a double click.
    /// </summary>
    /// <remarks>
    /// The asset browser's tiles (<see cref="AssetGrid"/>), so a mesh is found by the picture it is
    /// browsed by. A group starts a row of its own under its heading.
    /// </remarks>
    private static bool Grid(IReadOnlyList<PickerItem> kinds)
    {
        var add = false;
        var gap = ImGui.GetStyle().ItemSpacing.X;
        _across = Math.Max(1, (int)((ImGui.GetContentRegionAvail().X + gap) / (Tile + gap)));

        var column = 0;
        for (var index = 0; index < kinds.Count; index++)
        {
            var kind = kinds[index];

            if (kind.Group is { Length: > 0 } group && (index == 0 || kinds[index - 1].Group != group))
            {
                if (index > 0) ImGui.Spacing();
                ImGui.TextDisabled(group);
                column = 0;
            }

            if (column > 0) ImGui.SameLine();
            column = (column + 1) % _across;

            var chosen = index == _chosen;
            var at = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton($"##tile{index}", new Vector2(Tile, Tile));
            var over = ImGui.IsItemHovered();

            if (ImGui.IsItemClicked()) _chosen = index;

            if (over && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _chosen = index;
                add = true;
            }

            if (chosen && (ImGui.IsKeyPressed(ImGuiKey.DownArrow) || ImGui.IsKeyPressed(ImGuiKey.UpArrow)))
                ImGui.SetScrollHereY();

            AssetGrid.Face(
                at,
                Tile,
                kind.Label,
                kind.Path is { } path ? AssetGrid.PictureOf(path) : null,
                kind.Icon ?? EditorIcons.File,
                chosen,
                over);

            if (over) EditorWidgets.Tip(kind.Label);
        }

        return add;
    }

    /// <summary>Cancel and Add at the bottom right, and whether Add was pressed.</summary>
    private static bool Buttons(bool any)
    {
        var style = ImGui.GetStyle();
        var width = 90f;
        var room = ImGui.GetContentRegionAvail().X;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, room - (width * 2f) - style.ItemSpacing.X));

        if (ImGui.Button("Cancel", new Vector2(width, 0f))) ImGui.CloseCurrentPopup();

        ImGui.SameLine();

        if (!any) ImGui.BeginDisabled();

        ImGui.PushStyleColor(ImGuiCol.Button, EditorTheme.LiveAccent);
        var add = ImGui.Button(_verb, new Vector2(width, 0f));
        ImGui.PopStyleColor();

        if (!any) ImGui.EndDisabled();

        return add;
    }

    /// <summary>
    /// Dims everything behind the modal, in a window of its own laid over the panels, rounded at
    /// the window's corners as the renderer rounds them.
    /// </summary>
    /// <remarks>
    /// Square where the window is maximized, since its corners are square then too. Takes no input,
    /// since the modal already keeps every press to itself.
    /// </remarks>
    private static void Dim()
    {
        var size = ImGuiRuntime.Size;

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        var flags = EditorSurface.Placed
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##modalDim", flags))
        {
            var radius = EditorWindowFrame.Maximized ? 0f : EditorTheme.Current.WindowRounding;
            var shade = EditorTheme.Alpha(EditorTheme.Current.Ground with { W = 1f }, 0.55f);

            EditorDraw.Rounded(Vector2.Zero, size, radius, ImGui.GetColorU32(shade), ImGui.GetWindowDrawList());
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    /// <summary>What matches the search, in the order it was offered.</summary>
    private static List<PickerItem> Matching()
    {
        var wanted = _search.Trim();
        var matching = new List<PickerItem>();

        foreach (var item in _items())
        {
            if (wanted.Length > 0 && !item.Label.Contains(wanted, StringComparison.OrdinalIgnoreCase)) continue;

            matching.Add(item);
        }

        _chosen = Math.Clamp(_chosen, 0, Math.Max(0, matching.Count - 1));

        return matching;
    }
}

using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The window that adds an entity: every kind there is, a box to narrow them down by typing, and
/// the new one put under whatever is selected.
/// </summary>
/// <remarks>
/// <para>
/// A window in the middle of the screen rather than a flyout by the button, the way Godot adds a
/// node. The list is every row under <c>Spawn/</c> in the menu, and a game adds to it by adding a
/// row there, so a list that grows with a project wants room and a search box, which a flyout
/// hanging off a corner has neither of.
/// </para>
/// <para>
/// Keys do what they do in any list with a search box over it. Typing narrows it, the arrows move
/// the choice, Enter adds the chosen one, and Escape or a click outside closes it. Adding one twice
/// over is a double click.
/// </para>
/// <para>
/// What is added goes under the entity selected when the window opened, and keeps its place in the
/// world, as a child added in Godot does. With nothing selected it goes at the top of the world.
/// Undoing it takes it away, as one step.
/// </para>
/// </remarks>
public static class AddEntityWindow
{
    private const string Name = "##addEntity";

    /// <summary>The branch of the menu the kinds are read from.</summary>
    private const string Branch = "Spawn/";

    private static bool _opening;
    private static string _search = string.Empty;
    private static int _chosen;

    /// <summary>What the new entity goes under, fixed when the window opened.</summary>
    private static Entity _parent = Entity.None;

    /// <summary>Whether it is up.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Opens it, over whatever is selected now.</summary>
    /// <remarks>
    /// A popup belongs to the frame it is opened in, so this is remembered and put up on the next
    /// pass through the interface, the way the menu is.
    /// </remarks>
    public static void Open()
    {
        _opening = true;
        _search = string.Empty;
        _chosen = 0;
        _parent = EditorSelection.Current;
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
        var size = new Vector2(MathF.Min(420f, window.X - 40f), MathF.Min(460f, window.Y - 40f));

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
        IsOpen = ImGui.BeginPopupModal(
            Name,
            ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

        if (!IsOpen)
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            return;
        }

        var kinds = Kinds();

        Search(kinds.Count);

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }

        var add = List(kinds);

        ImGui.Spacing();

        // Pressed with the list's own keys or its buttons, and either way it runs once.
        add |= Buttons(kinds.Count > 0);

        if (add && _chosen < kinds.Count)
        {
            Add(ctx, kinds[_chosen]);
            ImGui.CloseCurrentPopup();
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

        // Where the new one goes, said in the box's hint rather than in a heading over it, which is
        // the one line of the window that has room for it while nothing is typed.
        var under = !_parent.IsNone && EditorShell.Context is { } ctx && ctx.Ecs.IsAlive(_parent)
            ? ctx.Ecs.NameOf(_parent)
            : null;

        var hint = under is { Length: > 0 } ? $"Search, adds under {under}" : "Search, adds at the top level";

        if (ImGui.InputTextWithHint("##search", hint, ref _search, 128)) _chosen = 0;

        // The arrows move the choice while the box keeps the keyboard, so the hand that is typing
        // can pick without reaching for the pointer.
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) _chosen = Math.Min(_chosen + 1, Math.Max(0, count - 1));
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) _chosen = Math.Max(_chosen - 1, 0);
    }

    /// <summary>The kinds that match, one row each, and whether one was asked to be added.</summary>
    private static bool List(IReadOnlyList<MenuItem> kinds)
    {
        var add = ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter);

        // What the buttons under it take, so the list has the rest of the window and they stay put.
        var buttons = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y;

        if (!EditorSurface.Region("##kinds", new Vector2(0f, -buttons)))
        {
            EditorSurface.EndRegion();
            return add;
        }

        if (kinds.Count == 0) ImGui.TextDisabled("Nothing matches");

        var line = ImGui.GetTextLineHeight();
        var height = ImGui.GetFrameHeight();

        for (var index = 0; index < kinds.Count; index++)
        {
            var kind = kinds[index];
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
                kind.Path[Branch.Length..]);
        }

        EditorSurface.EndRegion();

        return add;
    }

    /// <summary>Add and Cancel at the bottom right, and whether Add was pressed.</summary>
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
        var add = ImGui.Button("Add", new Vector2(width, 0f));
        ImGui.PopStyleColor();

        if (!any) ImGui.EndDisabled();

        return add;
    }

    /// <summary>
    /// Every row under the spawn branch that matches the search, in the order the menu has them.
    /// </summary>
    /// <remarks>
    /// Matched against the whole path below the branch, so typing <c>light</c> finds every light
    /// and <c>point</c> finds the one.
    /// </remarks>
    private static List<MenuItem> Kinds()
    {
        var wanted = _search.Trim();
        var kinds = new List<MenuItem>();

        foreach (var item in EditorMenu.All)
        {
            if (item.Kind != MenuKind.Command || item.Run is null) continue;
            if (!item.Path.StartsWith(Branch, StringComparison.Ordinal)) continue;

            var shown = item.Path[Branch.Length..];

            if (wanted.Length > 0 && !shown.Contains(wanted, StringComparison.OrdinalIgnoreCase)) continue;

            kinds.Add(item);
        }

        // Where the menu lists them, by order, then by where they sit, so a light comes after the
        // shapes whatever order they were registered in.
        kinds.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Path, b.Path));

        _chosen = Math.Clamp(_chosen, 0, Math.Max(0, kinds.Count - 1));

        return kinds;
    }

    /// <summary>Makes one, and puts it under what was selected, where it stands.</summary>
    /// <remarks>
    /// The spawn selects what it made and records taking it away, so the move under the parent
    /// is not recorded again. Undoing the spawn despawns it wherever it has ended up, which takes
    /// the move with it.
    /// </remarks>
    private static void Add(BehaviorContext ctx, MenuItem kind)
    {
        kind.Run?.Invoke(ctx.Ecs);

        var made = EditorSelection.Current;

        if (made.IsNone || _parent.IsNone || made == _parent || !ctx.Ecs.IsAlive(_parent)) return;

        EditorHierarchy.Reparent(ctx.Ecs, made, _parent, record: false);
        WorldPanel.Unfold(_parent);
    }
}

using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The groups of buttons that float in the scene's corners.
/// </summary>
/// <remarks>
/// What each group holds is <see cref="EditorToolbar"/>'s to say, the way <see cref="ConsoleView"/>
/// draws what <see cref="ConsoleLog"/> holds. This places the groups and draws them.
/// </remarks>
public static class ToolbarView
{
    /// <summary>
    /// How far a group sits from the corner of the scene it is pinned to.
    /// </summary>
    /// <remarks>
    /// The editor's own gap plus a little, because a group in a corner is measured against a
    /// rounded one and reads as tighter than the same gap along a straight edge. Anything that
    /// lines up with the toolbar, such as the menu that opens under it, takes this rather than
    /// repeating the number.
    /// </remarks>
    internal static float Inset => EditorShell.Margin + 4f;

    /// <summary>
    /// What floats in the scene's corners.
    /// </summary>
    /// <remarks>
    /// Not a bar across the top, which would take a strip of the scene permanently. These take
    /// only what they cover. Each group is pinned to a corner of whatever the scene has been left,
    /// so they follow it as the panel is docked or dragged.
    /// </remarks>
    internal static void Draw(BehaviorContext ctx)
    {
        var (center, right) = Rows();

        Group(ctx, ToolbarSlot.Left, new Vector2(0f, 0f), new Vector2(0f, 0f));
        Group(ctx, ToolbarSlot.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), row: center);
        Group(ctx, ToolbarSlot.Right, new Vector2(1f, 0f), new Vector2(1f, 0f), row: right);
        RightRow = right;
        Group(ctx, ToolbarSlot.BottomRight, new Vector2(1f, 1f), new Vector2(1f, 1f));

        // Down the left edge rather than across the top, for the groups that are a list of modes.
        Group(ctx, ToolbarSlot.LeftEdge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), down: true);

        // Under the buttons at the top right, where the button that opens it is.
        EditorStats.Draw(ctx);
    }

    /// <summary>Which line the top right group is on, which what hangs under it is placed by.</summary>
    internal static int RightRow { get; private set; }

    /// <summary>How wide each group came to last frame, which decides whether they share a line.</summary>
    private static readonly Dictionary<ToolbarSlot, float> Widths = [];

    /// <summary>
    /// Which line the center and the right groups go on, so no two groups overlap however narrow
    /// the scene is.
    /// </summary>
    /// <remarks>
    /// All three on the top line where they fit, as a title bar holds them. Where they do not, the
    /// center group goes on a line of its own under them, since it is the one in the middle of
    /// both, and where even the left and the right groups would meet, every group has a line of
    /// its own, in order. Worked out from the widths the groups came to last frame, since a group
    /// knows its width only once it has been laid out, and a group is as wide from one frame to
    /// the next as the buttons in it.
    /// </remarks>
    private static (int Center, int Right) Rows()
    {
        var room = EditorShell.Free.Right - EditorShell.Scene.X - (Inset * 2f);
        var gap = EditorSurface.Air * 2f;

        var left = Widths.GetValueOrDefault(ToolbarSlot.Left);
        var center = Widths.GetValueOrDefault(ToolbarSlot.Center);
        var right = Widths.GetValueOrDefault(ToolbarSlot.Right);

        // The center group is centered, so it needs the half of the room either side of the
        // middle to clear whichever of the other two is wider.
        var sides = MathF.Max(left, right);

        if ((sides * 2f) + center + (gap * 2f) <= room) return (0, 0);

        if (left + right + gap <= room) return (1, 0);

        return (1, 2);
    }

    /// <summary>One corner's worth of buttons, in a row.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="slot">Which corner.</param>
    /// <param name="corner">Which corner of the scene it hangs from, as a fraction.</param>
    /// <param name="pivot">Which corner of the group meets it.</param>
    /// <param name="down">Whether the buttons stack downwards rather than running across.</param>
    /// <param name="row">Which line of groups down from the top it is on, where they do not all fit on one.</param>
    internal static void Group(
        BehaviorContext ctx,
        ToolbarSlot slot,
        Vector2 corner,
        Vector2 pivot,
        bool down = false,
        int row = 0)
    {
        var buttons = EditorToolbar.Slot(slot);
        if (buttons.Count == 0) return;

        var inset = Inset;

        // Pinned to the corners of what the scene still has to itself, not of the scene's own
        // rectangle. Floating, the scene is the whole window and the panels lie over it, so a
        // group placed by the window's middle ends up under the panel, which then draws a row of
        // buttons across its own top edge.
        var top = EditorShell.SceneTop;
        var width = EditorShell.Free.Right - EditorShell.Scene.X;
        var height = EditorShell.Free.Bottom - top;

        var at = new Vector2(
            EditorShell.Scene.X + (width * corner.X) + (corner.X > 0.5f ? -inset : corner.X > 0f ? 0f : inset),
            top + (height * corner.Y) + (corner.Y > 0.5f ? -inset : inset) + (row * (EditorSurface.Tall + EditorSurface.Air)));

        ImGui.SetNextWindowPos(at, ImGuiCond.Always, pivot);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));

        var began = ImGui.Begin($"##bar{slot}", EditorSurface.Bare);

        Widths[slot] = ImGui.GetWindowSize().X;

        if (!began)
        {
            ImGui.End();
            ImGui.PopStyleVar();
            return;
        }

        // Round enough that a square button is a circle, the usual shape of a button with a picture
        // in it and no words.
        var size = EditorSurface.Tall;

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, size * 0.5f);

        // Air at the ends of a button with words in it. Nothing above or below, because the height
        // is given outright and padding there would only fight it.
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorSurface.Sides, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(EditorSurface.Air, EditorSurface.Air));

        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];

            if (index > 0 && !down) ImGui.SameLine();

            var on = button.Active?.Invoke() == true;

            // The plate everything lying on the scene wears, until it is in force, when it wears
            // the accent, which is the one thing color means here.
            ImGui.PushStyleColor(
                ImGuiCol.Button,
                on ? EditorTheme.LiveAccent : EditorSurface.Lying());

            ImGui.PushStyleColor(
                ImGuiCol.ButtonHovered,
                on ? EditorTheme.Alpha(EditorTheme.LiveAccent, 0.85f) : EditorTheme.LiveHover);

            ImGui.PushStyleColor(
                ImGuiCol.ButtonActive,
                on ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveHover, 1f));

            var label = button.Label();
            var can = button.Enabled?.Invoke() != false;

            if (!can) ImGui.BeginDisabled();

            var pressed = button.Icon is { Length: > 0 } icon && label.Length == 0
                ? Circle($"{slot}{index}", icon, on, size)
                : ImGui.Button(
                    $"{(label.Length == 0 ? Icon(button.Icon) : label)}##{slot}{index}",
                    new Vector2(0f, size));

            if (pressed) button.Run(ctx.Ecs);

            if (!can) ImGui.EndDisabled();

            // What it is, for a button that is only a picture, and for one with a word in it that
            // has more to say than the word does.
            if (ImGui.IsItemHovered())
            {
                var says = button.Tip ?? (label.Length > 0 ? label : Icon(button.Icon));

                if (says.Length > 0) EditorWidgets.Tip(says);
            }

            ImGui.PopStyleColor(3);
        }

        ImGui.PopStyleVar(3);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    /// <summary>
    /// A round button with a picture in it, and nothing else.
    /// </summary>
    /// <remarks>
    /// Drawn rather than asked for. ImGui rounds an image button by the smaller of its padding and
    /// the style's rounding, so a circle would need padding half the button wide, which is padding
    /// around nothing. This draws a circle and a picture over it.
    /// </remarks>
    internal static bool Circle(string id, string icon, bool on, float size)
    {
        var at = ImGui.GetCursorScreenPos();

        ImGui.InvisibleButton($"##{id}", new Vector2(size, size));

        var pressed = ImGui.IsItemClicked();
        var over = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        // Out of the colors the caller pushed, as a button of ImGui's own would read them. Reaching
        // for the palette directly here would quietly ignore them.
        var fill = held
            ? ImGuiCol.ButtonActive
            : over
                ? ImGuiCol.ButtonHovered
                : ImGuiCol.Button;

        var draw = ImGui.GetWindowDrawList();
        var middle = at + new Vector2(size * 0.5f, size * 0.5f);

        draw.AddCircleFilled(middle, size * 0.5f, ImGui.GetColorU32(fill));

        // A share of the circle rather than a number of pixels, so the picture keeps its margin
        // whatever the button is sized to.
        var mark = MathF.Floor(size * 0.6f);

        EditorDraw.Icon(draw, icon, middle - new Vector2(mark * 0.5f, mark * 0.5f), mark, on);

        return pressed;
    }

    /// <summary>A word standing in for a picture, until the icons are loaded.</summary>
    internal static string Icon(string? path)
    {
        if (path is not { Length: > 0 }) return "?";

        var name = Path.GetFileNameWithoutExtension(path);
        return name.Length == 0 ? "?" : char.ToUpperInvariant(name[0]) + name[1..];
    }

}

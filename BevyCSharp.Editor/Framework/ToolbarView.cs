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
    /// What floats in the scene's corners.
    /// </summary>
    /// <remarks>
    /// Not a bar across the top: a bar would take a strip of the scene permanently, and these take
    /// only what they cover. Each group is pinned to a corner of whatever the scene has been left,
    /// so they follow it as the panel is docked or dragged.
    /// </remarks>
    internal static void Draw(BehaviorContext ctx)
    {
        Group(ctx, ToolbarSlot.Left, new Vector2(0f, 0f), new Vector2(0f, 0f));
        Group(ctx, ToolbarSlot.Centre, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        Group(ctx, ToolbarSlot.Right, new Vector2(1f, 0f), new Vector2(1f, 0f));
        Group(ctx, ToolbarSlot.BottomRight, new Vector2(1f, 1f), new Vector2(1f, 1f));

        // Down the left edge rather than across the top, for the groups that are a list of modes.
        Group(ctx, ToolbarSlot.LeftEdge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), down: true);
    }

    /// <summary>One corner's worth of buttons, in a row.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="slot">Which corner.</param>
    /// <param name="corner">Which corner of the scene it hangs from, as a fraction.</param>
    /// <param name="pivot">Which corner of the group meets it.</param>
    /// <param name="down">Whether the buttons stack downwards rather than running across.</param>
    internal static void Group(
        BehaviorContext ctx,
        ToolbarSlot slot,
        Vector2 corner,
        Vector2 pivot,
        bool down = false)
    {
        var buttons = EditorToolbar.Slot(slot);
        if (buttons.Count == 0) return;

        var inset = EditorShell.Margin + 4f;

        var at = new Vector2(
            EditorShell.Scene.X + (EditorShell.Scene.Width * corner.X) + (corner.X > 0.5f ? -inset : corner.X > 0f ? 0f : inset),
            EditorShell.Scene.Y + (EditorShell.Scene.Height * corner.Y) + (corner.Y > 0.5f ? -inset : inset));

        ImGui.SetNextWindowPos(at, ImGuiCond.Always, pivot);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));

        if (!ImGui.Begin($"##bar{slot}", EditorSurface.Bare))
        {
            ImGui.End();
            ImGui.PopStyleVar();
            return;
        }

        // Round enough that a square button is a circle, which is what a button with a picture in
        // it and no words wants to be.
        const float Size = 34f;

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Size * 0.5f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 6f));

        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];

            if (index > 0 && !down) ImGui.SameLine();

            var on = button.Active?.Invoke() == true;
            var theme = EditorTheme.Current;

            // Nothing behind a button that is not in force or under the hand: the picture is the
            // button. What is in force wears the accent, which is the one thing colour means.
            ImGui.PushStyleColor(
                ImGuiCol.Button,
                on ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveCard, theme.PanelAlpha));

            ImGui.PushStyleColor(
                ImGuiCol.ButtonHovered,
                on ? EditorTheme.Alpha(EditorTheme.LiveAccent, 0.85f) : EditorTheme.LiveHover);

            ImGui.PushStyleColor(
                ImGuiCol.ButtonActive,
                on ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveHover, 1f));

            var label = button.Label();

            var pressed = button.Icon is { Length: > 0 } icon && label.Length == 0
                ? Circle($"{slot}{index}", icon, on, Size)
                : ImGui.Button(
                    $"  {(label.Length == 0 ? Icon(button.Icon) : label)}  ##{slot}{index}",
                    new Vector2(0f, Size));

            if (pressed) button.Run(ctx.Ecs);

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
    /// around nothing. A circle and a picture over it is what was wanted and what this draws.
    /// </remarks>
    internal static bool Circle(string id, string icon, bool on, float size)
    {
        var at = ImGui.GetCursorScreenPos();

        ImGui.InvisibleButton($"##{id}", new Vector2(size, size));

        var pressed = ImGui.IsItemClicked();
        var over = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        // Out of the colours the caller pushed, which is what a button of ImGui's own would use:
        // reading the palette directly here instead would quietly ignore them.
        var fill = held
            ? ImGuiCol.ButtonActive
            : over
                ? ImGuiCol.ButtonHovered
                : ImGuiCol.Button;

        var draw = ImGui.GetWindowDrawList();
        var middle = at + new Vector2(size * 0.5f, size * 0.5f);

        draw.AddCircleFilled(middle, size * 0.5f, ImGui.GetColorU32(fill));

        const float Mark = 18f;

        EditorSurface.Icon(draw, icon, middle - new Vector2(Mark * 0.5f, Mark * 0.5f), Mark, on);

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

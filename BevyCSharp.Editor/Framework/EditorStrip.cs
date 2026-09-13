using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The strip of tabs along the bottom left, with whatever is open growing upwards out of it.
/// </summary>
public static class EditorStrip
{
    /// <summary>How much air the strip keeps around its tabs.</summary>
    /// <remarks>
    /// The gap everything else in the editor is spaced by, so the tab card sits the same distance
    /// from the scene above it as the world card sits from the scene beside it.
    /// </remarks>
    internal const float Padding = EditorSurface.Gutter;

    /// <summary>How tall the strip is with no tab open.</summary>
    /// <remarks>
    /// A tab is one of the things that lie on the scene, so it is as tall as the buttons in the
    /// corners are, and the strip is that plus the air it keeps above and below it. The stock look
    /// keeps none below, because ImGui draws a tab with a flat bottom for the content to join, and
    /// a flat bottom with a gap under it is a tab hanging in the air.
    /// </remarks>
    internal static float Shut =>
        EditorSurface.Tall + (Padding * (EditorTheme.Current.Stock ? 1f : 2f));

    /// <summary>
    /// The strip along the bottom left, with whatever is open growing upwards out of it.
    /// </summary>
    /// <remarks>
    /// The bar is drawn under its content rather than over it, which is what a console does. The
    /// headers stay where the hand last left them and the lines rise out of the bottom of the
    /// screen. ImGui's own tab bar either way, so hovering, ordering and the mark on the one in
    /// force are its to draw.
    /// </remarks>
    internal static void Draw(float width, float strip, float margin)
    {
        if (EditorShell.Tabs.Count == 0 || width < 80f) return;

        var window = ImGuiRuntime.Size;
        var top = window.Y - margin - strip;

        ImGui.SetNextWindowPos(new Vector2(margin, top));
        ImGui.SetNextWindowSize(new Vector2(width, strip));
        ImGui.SetNextWindowBgAlpha(EditorShell.Docked ? 1f : EditorTheme.Current.WindowAlpha);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, EditorSurface.Chrome());

        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowRounding,
            EditorShell.Docked ? 0f : EditorTheme.Current.WindowRounding);

        // The same gap the panel keeps round its cards, and no more above and below the bar than
        // the strip was measured with, or the bar sinks and its text clips.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowPadding,
            new Vector2(EditorSurface.Gutter, Padding));

        if (!ImGui.Begin("##tabs", EditorSurface.Panel))
        {
            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            return;
        }

        // What is open, first, in the room left above the bar.
        if (EditorShell.OpenTab >= 0 && EditorShell.OpenTab < EditorShell.Tabs.Count)
        {
            Grip();

            var room = ImGui.GetContentRegionAvail();

            var bar = EditorSurface.Tall + ImGui.GetStyle().ItemSpacing.Y;

            // A card like the ones in the panel, with the same gap outside it and the same air
            // inside it, rather than a rectangle pushed against its own edges.
            //
            // Docked it runs past the strip's own right padding as far as the panel's edge, so the
            // gap between this card and the one in the panel beside it is the panel's inset and
            // nothing else, which is the gap every other pair of cards has between them.
            var across = EditorShell.Docked ? room.X + EditorSurface.Gutter : 0f;

            EditorSurface.Card(
                "##tab",
                new Vector2(across, room.Y - bar),
                EditorShell.Tabs[EditorShell.OpenTab].Draw);
        }

        // And the bar under it, drawn rather than asked for.
        //
        // ImGui's own tab draws a few pixels of itself below its frame, to join the tab to the
        // content underneath it. Here the content is above the bar, so that join points into the
        // scene, as a dark tongue hanging off whichever tab is open, past the edge of the strip. A
        // pill under the word says which one is open without any of that.
        if (!EditorTheme.Current.Stock)
        {
            Pills();

            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            return;
        }

        // The stock look keeps ImGui's own tabs, because that is what it is for.
        //
        // Against the bottom of the strip rather than where the layout left the cursor. ImGui
        // draws a tab as a shape with a flat bottom, for content to join underneath it, and here
        // the content is above, so the flat edge has to land on the edge of the window.
        ImGui.SetCursorPosY(ImGui.GetWindowHeight() - ImGui.GetFrameHeight());

        if (ImGui.BeginTabBar("##strip", ImGuiTabBarFlags.NoTooltip))
        {
            for (var index = 0; index < EditorShell.Tabs.Count; index++)
            {
                var open = index == EditorShell.OpenTab;

                // The one that is open wears the colour of what is above it, so the header and its
                // contents read as one thing, and the accent marks which it is.
                if (open)
                {
                    ImGui.PushStyleColor(ImGuiCol.Tab, ImGui.GetColorU32(ImGuiCol.ChildBg));
                    ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.Current.Text);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Tab, EditorTheme.Alpha(EditorTheme.Current.Panel, 0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.Current.Dim);
                }

                if (ImGui.TabItemButton(
                        $"  {EditorShell.Tabs[index].Name}  ",
                        open ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    EditorShell.OpenTab = open ? -1 : index;
                }

                ImGui.PopStyleColor(2);
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// The bar across the top of an open tab, which a drag makes it taller or shorter.
    /// </summary>
    /// <remarks>
    /// The console is a thing somebody reads, and how much of it they want to read at once is
    /// theirs to say. The height is kept rather than worked out, so a tab closed and opened again
    /// comes back the size it was left.
    /// </remarks>
    internal static void Grip()
    {
        var width = ImGui.GetContentRegionAvail().X;
        var top = ImGui.GetCursorPosY();

        // In the gap above the card rather than above the gap. The air between the scene and the
        // tab card is the same air every other pair of surfaces has between them, and a handle
        // that added its own height to it would make this one gap twice the size of the rest.
        ImGui.SetCursorPosY(top - Padding);

        ImGui.InvisibleButton("##height", new Vector2(MathF.Max(1f, width), Padding));

        var held = ImGui.IsItemActive();
        var over = ImGui.IsItemHovered();

        if (over || held) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);

        if (held)
        {
            // Up is taller, because the strip grows upward out of the bottom of the window.
            EditorShell.TabHeight = Math.Clamp(
                EditorShell.TabHeight - ImGui.GetIO().MouseDelta.Y,
                80f,
                MathF.Max(120f, ImGuiRuntime.Size.Y * 0.75f));
        }

        EditorSurface.Grab(EditorSurface.Pill, over, held);

        ImGui.SetCursorPosY(top);
    }

    /// <summary>
    /// The tabs as a row of pills, which is the shape the rest of this look is drawn in.
    /// </summary>
    /// <remarks>
    /// A click on the one already open closes it, which is why these are buttons rather than tabs:
    /// what a header means here is ours to say, and a tab bar has its own idea about which of its
    /// tabs is selected.
    /// </remarks>
    internal static void Pills()
    {
        var draw = ImGui.GetWindowDrawList();
        var theme = EditorTheme.Current;

        // The same height, the same air at the ends and the same colours as a button floating in
        // the scene's corner, because a tab is one of those lying along the bottom edge. Two
        // things that are pressed, on the same surface, that do not match are two things to look
        // at rather than one.
        var height = EditorSurface.Tall;
        var padding = EditorSurface.Sides;

        for (var index = 0; index < EditorShell.Tabs.Count; index++)
        {
            if (index > 0) ImGui.SameLine();

            var open = index == EditorShell.OpenTab;
            var name = EditorShell.Tabs[index].Name;
            var word = ImGui.CalcTextSize(name);
            var at = ImGui.GetCursorScreenPos();
            var size = new Vector2(word.X + (padding * 2f), height);

            ImGui.InvisibleButton($"##tab{index}", size);

            if (ImGui.IsItemClicked()) EditorShell.OpenTab = open ? -1 : index;

            var over = ImGui.IsItemHovered();

            // Docked, a tab that is neither open nor under the hand wears nothing, because the
            // strip is black behind it and the word carries on its own. Floating, the strip is the
            // lit scene seen through, and a word on that needs something under it to sit on.
            var idle = EditorShell.Docked
                ? null
                : (Vector4?)EditorTheme.Alpha(EditorTheme.LiveCard, theme.PanelAlpha);

            var fill = open
                ? over ? EditorTheme.Alpha(EditorTheme.LiveAccent, 0.85f) : EditorTheme.LiveAccent
                : over ? EditorTheme.LiveHover : idle;

            if (fill is { } under)
            {
                draw.AddRectFilled(at, at + size, ImGui.GetColorU32(under), height * 0.5f);
            }

            // White whether it is open or not. What says which one is showing is the pill under it,
            // and a grey word reads as a tab that cannot be pressed.
            draw.AddText(
                at + new Vector2(padding, (height - word.Y) * 0.5f),
                ImGui.GetColorU32(EditorTheme.LiveText),
                name);
        }
    }

}

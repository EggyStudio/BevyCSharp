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
    /// Less than a panel's, because a strip that is mostly air reads as an empty panel with some
    /// words in it rather than as a row of tabs.
    /// </remarks>
    internal const float Padding = 5f;

    /// <summary>
    /// How tall the strip is with no tab open, measured rather than guessed.
    /// </summary>
    /// <remarks>
    /// A guess is wrong the moment the font or the padding changes, and what it looks like when it
    /// is wrong is a row of tabs with their text cut off along the bottom of the window.
    /// </remarks>
    internal static float Shut => ImGui.GetFrameHeight() + (Padding * 2f);

    /// <summary>
    /// The strip along the bottom left, with whatever is open growing upwards out of it.
    /// </summary>
    /// <remarks>
    /// The bar is drawn under its content rather than over it, which is what a console does: the
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

        // The strip is measured as Padding above and below the bar, so its own window padding
        // has to be that and no more or the bar sinks and the text clips.
        // The same gutter the panel keeps round its cards, and no more above and below the bar
        // than the strip was measured with.
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

            var bar = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y;

            // A card like the ones in the panel, which is what it is: the same gap outside it and
            // the same air inside it, rather than a rectangle pushed against its own edges.
            EditorSurface.Card("##tab", new Vector2(0f, room.Y - bar), EditorShell.Tabs[EditorShell.OpenTab].Draw);
        }

        // And the bar under it, drawn rather than asked for.
        //
        // ImGui's own tab draws a few pixels of itself below its frame, to join the tab to the
        // content underneath it. Here the content is above the bar, so that join points into the
        // scene: a dark tongue hanging off whichever tab is open, past the edge of the strip. A
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

        ImGui.InvisibleButton("##height", new Vector2(MathF.Max(1f, width), 8f));

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
        var height = ImGui.GetFrameHeight();
        var padding = ImGui.GetStyle().FramePadding.X + 6f;

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

            // A tab is a button and wears a button's three steps: a fill of its own to be seen
            // and pressed, a brighter one under the hand, and the accent when it is the one
            // showing. Nothing at all under the two that are not open reads as two words somebody
            // has left lying on the strip.
            // Docked, a tab that is neither open nor under the hand wears nothing: the strip is
            // black behind it and the word carries on its own. Floating, the strip is the lit
            // scene seen through, and a word on that needs something under it to sit on.
            var idle = EditorShell.Docked ? null : (Vector4?)ImGui.GetStyle().Colors[(int)ImGuiCol.Button];

            var fill = open
                ? EditorTheme.LiveAccent
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

using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The chrome round the scene: the corner it is missing, and the button that docks the panel.
/// </summary>
public static class EditorSceneFrame
{
    /// <summary>
    /// Takes the corners off the scene, so that docked it reads as a card like everything else.
    /// </summary>
    /// <remarks>
    /// The scene is drawn by the engine into a rectangle, and a rectangle has square corners. What
    /// rounds it is four wedges of the ground colour laid over those corners, on the list that
    /// draws under every window, so the panels still cover what they cover.
    /// <para>
    /// Only when docked. Floating, the scene is the whole window and a window with its corners
    /// taken off is a window with four notches of nothing in it.
    /// </para>
    /// </remarks>
    internal static void Round()
    {
        if (!EditorShell.Docked) return;

        // The rounding a card takes, not a window's: what the scene sits among docked is the cards
        // in the panel and the strip, and a corner rounder than theirs is a corner that does not
        // match the ones beside it.
        var radius = EditorTheme.Current.ChildRounding;
        if (radius < 1f) return;

        // Docked, the scene is a card among the other cards, and what a corner taken off a card
        // shows is the surface it is lying on: the panel the cards are laid out in, which is the
        // darkest thing on the screen that is not the ground itself.
        var draw = ImGui.GetBackgroundDrawList();
        var theme = EditorTheme.Current;

        // One coat, opaque. Two of them put the feathered edge an antialiased fill draws down
        // twice, and the second one over the first is a seam along the arc.
        var color = ImGui.GetColorU32(EditorTheme.Alpha(theme.Ground, 1f));

        var right = EditorShell.Scene.X + EditorShell.Scene.Width;
        var bottom = EditorShell.Scene.Y + EditorShell.Scene.Height;

        // The gutter the scene now stops short of, filled in. What is under it otherwise is
        // whatever the camera clears its window to, which is a band of sky between the viewport
        // and the panel: the gap is chrome and has to be the colour the rest of the chrome is.
        if (EditorShell.Panel.X > right)
        {
            draw.AddRectFilled(new Vector2(right, 0f), new Vector2(EditorShell.Panel.X, ImGuiRuntime.Size.Y), color);
        }

        // The bottom right only. That is the one corner of a docked scene with chrome on both
        // sides of it: the strip runs under it and the panel stands beside it, so taking it off is
        // what joins the two. The other three meet the window's own edges, where there is nothing
        // to round against.
        Wedge(
            draw,
            new Vector2(right - radius, bottom - radius),
            radius,
            0f,
            MathF.PI * 0.5f,
            new Vector2(right, bottom),
            color);
    }

    /// <summary>One corner's worth of what a rounded rectangle leaves out.</summary>
    /// <param name="draw">What to draw into.</param>
    /// <param name="middle">Where the corner's arc is centred.</param>
    /// <param name="radius">How large the arc is.</param>
    /// <param name="from">Where the arc starts, in radians.</param>
    /// <param name="to">Where it ends.</param>
    /// <param name="corner">The square corner the arc cuts off.</param>
    /// <param name="color">What to fill it with.</param>
    internal static void Wedge(
        ImDrawListPtr draw,
        Vector2 middle,
        float radius,
        float from,
        float to,
        Vector2 corner,
        uint color)
    {
        // Segments enough that the arc has no steps in it at the sizes a window rounding takes.
        // What ImGui works out for itself is tuned for a whole circle of this radius and leaves a
        // quarter of one with four or five, which is a visible staircase.
        draw.PathClear();
        draw.PathLineTo(corner);
        draw.PathArcTo(middle, radius, from, to, Math.Max(8, (int)(radius * 1.5f)));
        draw.PathFillConvex(color);
    }

    /// <summary>
    /// The button that docks the panel, over everything at the window's top right.
    /// </summary>
    /// <remarks>
    /// Its own window rather than a corner of the panel, so it stays reachable and in the same
    /// place whatever the panel is doing underneath it. Round with a picture in it, like the rest
    /// of what floats over the scene.
    /// </remarks>
    internal static void DockButton()
    {
        // Small enough and high enough to sit in the empty right end of a panel's title row,
        // which is the one row under it with nothing in it. A button that reaches into the row
        // below takes width from whatever is there, and what is there is a search box that should
        // run the whole way across.
        const float Size = 26f;

        // The window's top right corner, and how far into it depends only on where the panel's
        // first row starts: docked the panel is flush against the window and everything in it sits
        // ten pixels higher, so the button has to move with it or it reaches down into the row
        // below and takes width from the filter box there.
        var window = ImGuiRuntime.Size;
        var inset = 4f;

        ImGui.SetNextWindowPos(new Vector2(window.X - inset, inset), ImGuiCond.Always, new Vector2(1f, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));

        if (ImGui.Begin("##dock", EditorSurface.Bare))
        {
            var theme = EditorTheme.Current;

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Size * 0.5f);

            // A step above the panel it sits on, or the disc cannot be told from the panel and what
            // is left is a picture floating in the corner.
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.FrameBg));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EditorTheme.LiveHover);

            // Never in the accent: the accent says what is in force in the scene, and a bright blue
            // disc in the corner of the panel reads as a close button somebody has to think about.
            // Which way it is set is what the picture in it says.
            if (ToolbarView.Circle($"dock{EditorShell.Docked}", EditorShell.Docked ? "icons/ui/close.png" : "icons/ui/pinned.png", false, Size))
            {
                EditorShell.Docked = !EditorShell.Docked;
            }

            if (ImGui.IsItemHovered()) ImGui.SetTooltip(EditorShell.Docked ? "Undock the panel" : "Dock the panel");

            // Where it ended up, so a panel underneath can leave the corner alone.
            DockRect = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    /// <summary>Where the dock button is on the screen.</summary>
    public static (Vector2 Min, Vector2 Max) DockRect { get; private set; }

    /// <summary>
    /// How much width to leave free on the row about to be drawn, so it stays clear of the dock
    /// button floating over it.
    /// </summary>
    /// <remarks>
    /// Asked of the row rather than worked out per panel: which card is under the corner depends on
    /// whether the panel is split beside or above, and a rule written per panel is a rule that is
    /// wrong in one of them.
    /// </remarks>
    public static float DockRoom()
    {
        if (DockRect.Max.X <= DockRect.Min.X) return 0f;

        var at = ImGui.GetCursorScreenPos();
        var right = ImGui.GetWindowPos().X + ImGui.GetWindowWidth();
        var line = ImGui.GetFrameHeight();

        if (at.Y > DockRect.Max.Y || at.Y + line < DockRect.Min.Y) return 0f;
        if (right <= DockRect.Min.X) return 0f;

        return right - DockRect.Min.X + 6f;
    }

}

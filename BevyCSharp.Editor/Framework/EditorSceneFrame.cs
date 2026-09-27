using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The chrome round the scene: its rounded corners, and the buttons at the window's top right.
/// </summary>
public static class EditorSceneFrame
{
    /// <summary>
    /// Takes the corners off the scene, so that docked it reads as a card like everything else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scene is drawn by the engine into a rectangle, and a rectangle has square corners. What
    /// rounds it is four wedges of the ground color laid over those corners, on the list that
    /// draws under every window, so the panels still cover what they cover.
    /// </para>
    /// <para>
    /// A wedge filled with ImGui's antialiasing still meets the scene along a curve the engine
    /// drew square, so the arc steps where the two meet. A one pixel line in the card color along
    /// the whole edge is antialiased on both sides and covers the steps, and it gives the scene the
    /// same edge a card has against the ground.
    /// </para>
    /// <para>
    /// Only when docked. Floating, the scene is the whole window and a window with its corners
    /// taken off is a window with four notches of nothing in it.
    /// </para>
    /// </remarks>
    internal static void Round()
    {
        if (!EditorShell.Docked) return;

        // The rounding a card takes, not a window's, because what the scene sits among docked is
        // the cards in the panel and the strip, and a corner rounder than theirs is a corner that
        // does not match the ones beside it.
        var theme = EditorTheme.Current;
        var radius = theme.ChildRounding;

        var draw = ImGui.GetBackgroundDrawList();

        // One coat, opaque. Two of them put the feathered edge an antialiased fill draws down
        // twice, and the second one over the first is a seam along the arc.
        var color = ImGui.GetColorU32(EditorTheme.Alpha(theme.Ground, 1f));

        var left = EditorShell.Scene.X;
        var top = EditorShell.Scene.Y;
        var right = left + EditorShell.Scene.Width;
        var bottom = top + EditorShell.Scene.Height;

        // The gap the scene keeps from the window's left and top edges, filled in. What is under it
        // otherwise is whatever the camera clears the window to, which is a band of sky round the
        // viewport. The gap is chrome and has to be the color the rest of the chrome is. The panel
        // and the strip paint their own, beside and below, but the top runs the window's whole
        // width, since the strip the window is moved by is above the panel as well.
        draw.AddRectFilled(Vector2.Zero, new Vector2(ImGuiRuntime.Size.X, top), color);
        draw.AddRectFilled(new Vector2(0f, top), new Vector2(left, bottom), color);

        if (EditorShell.Panel.X > right)
        {
            draw.AddRectFilled(new Vector2(right, top), new Vector2(EditorShell.Panel.X, bottom), color);
        }

        if (radius < 1f) return;

        // All four, now that the scene is a card with chrome on every side of it.
        var quarter = MathF.PI * 0.5f;

        Wedge(draw, new Vector2(right - radius, bottom - radius), radius, 0f, quarter, new Vector2(right, bottom), color);
        Wedge(draw, new Vector2(left + radius, bottom - radius), radius, quarter, quarter * 2f, new Vector2(left, bottom), color);
        Wedge(draw, new Vector2(left + radius, top + radius), radius, quarter * 2f, quarter * 3f, new Vector2(left, top), color);
        Wedge(draw, new Vector2(right - radius, top + radius), radius, quarter * 3f, quarter * 4f, new Vector2(right, top), color);

        // Half a pixel in, so the one pixel line lies on the edge rather than half off it.
        draw.AddRect(
            new Vector2(left + 0.5f, top + 0.5f),
            new Vector2(right - 0.5f, bottom - 0.5f),
            ImGui.GetColorU32(EditorTheme.Alpha(theme.Card, 1f)),
            radius,
            ImDrawFlags.None,
            1f);
    }

    /// <summary>One corner's worth of what a rounded rectangle leaves out.</summary>
    /// <param name="draw">What to draw into.</param>
    /// <param name="middle">Where the corner's arc is centered.</param>
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
    /// The buttons at the window's top right: the pin that docks the panel, then minimize,
    /// maximize and close where the editor draws its own frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One window of their own rather than a corner of the panel, so they stay reachable and in the
    /// same place whatever the panel is doing underneath. Round with a picture in each, like the
    /// rest of what floats over the scene, and in the order a title bar has them, with the pin
    /// before them because it belongs to the editor and they belong to the window.
    /// </para>
    /// <para>
    /// The window's three are there only when <see cref="EditorWindowFrame.Borderless"/>, since a
    /// platform frame has its own, and a picture drawn into an image has no window to act on.
    /// </para>
    /// </remarks>
    internal static void DockButton()
    {
        // The size everything else that floats over the scene is, so the buttons in the corner are
        // of that family rather than discs of their own. They sit in the empty right end of a
        // panel's title row, which is the one row under them with nothing in it.
        const float Size = EditorSurface.Tall;

        // The window's top right corner, below the strip the window is moved by, and as far into
        // it as the panel's own first row is: the gap the panel keeps round its cards, then the air
        // a card keeps inside its edge. That lines the row up with the panel's title in either
        // arrangement, since the panel is laid out the same way docked and floating.
        var window = ImGuiRuntime.Size;
        var inset = EditorSurface.Gutter + EditorSurface.Air;

        ImGui.SetNextWindowPos(
            new Vector2(window.X - inset, EditorWindowFrame.Grip + inset),
            ImGuiCond.Always,
            new Vector2(1f, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(EditorSurface.Air, 0f));

        if (ImGui.Begin("##dock", EditorSurface.Bare))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Size * 0.5f);

            // The plate everything lying on the scene wears, so these match the buttons in the
            // scene's own corners rather than being discs of their own shade.
            ImGui.PushStyleColor(ImGuiCol.Button, EditorSurface.Lying());
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EditorTheme.LiveHover);

            // Never in the accent, because the accent says what is in force in the scene, and a
            // bright blue disc in the corner reads as something that needs attention. The picture
            // says which way it is set. The pin as it stands. Pushed in while the panel is docked,
            // and lying loose while it floats, so the picture says what the panel is rather than
            // what the button does.
            var pin = EditorShell.Docked ? EditorIcons.Pinned : EditorIcons.Loose;

            if (ToolbarView.Circle($"dock{EditorShell.Docked}", pin, false, Size))
            {
                EditorShell.Docked = !EditorShell.Docked;
            }

            if (ImGui.IsItemHovered())
            {
                EditorWidgets.Tip(EditorShell.Docked ? "Undock the panel" : "Dock the panel");
            }

            var first = ImGui.GetItemRectMin();

            if (EditorWindowFrame.Borderless)
            {
                ImGui.SameLine();

                if (Marked("minimize", WindowMarks.Minimize, "Minimize", Size)) EditorWindowFrame.Minimize();

                ImGui.SameLine();

                var maximized = EditorWindowFrame.Maximized;

                if (Marked(
                    "maximize",
                    maximized ? WindowMarks.Restore : WindowMarks.Maximize,
                    maximized ? "Restore" : "Maximize",
                    Size))
                {
                    EditorWindowFrame.ToggleMaximized();
                }

                ImGui.SameLine();

                // The one button that ends something, so it says so under the pointer in the color
                // a failure is written in, as a title bar's close button turns red.
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EditorTheme.Current.Bad);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, EditorTheme.Current.Bad);

                if (Marked("close", WindowMarks.Close, "Close the editor", Size)) EditorWindowFrame.Close();

                ImGui.PopStyleColor(2);
            }

            // Where the whole row ended up, so a panel underneath can leave the corner alone.
            DockRect = (first, ImGui.GetItemRectMax());

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar();
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    /// <summary>A round button carrying one of the window's marks, with a word under the pointer.</summary>
    /// <param name="id">What to call it.</param>
    /// <param name="mark">Which mark.</param>
    /// <param name="tip">What it does, said when the pointer rests on it.</param>
    /// <param name="size">How large it is.</param>
    /// <returns>Whether it was pressed.</returns>
    private static bool Marked(string id, WindowMarks mark, string tip, float size)
    {
        var pressed = ToolbarView.Circle(id, string.Empty, false, size);

        var middle = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;

        EditorDraw.WindowMark(
            ImGui.GetWindowDrawList(),
            middle,
            MathF.Floor(size * 0.36f),
            mark,
            ImGui.GetColorU32(EditorTheme.IconTint(false)));

        if (ImGui.IsItemHovered()) EditorWidgets.Tip(tip);

        return pressed;
    }

    /// <summary>Where the dock button is on the screen.</summary>
    public static (Vector2 Min, Vector2 Max) DockRect { get; private set; }

    /// <summary>
    /// How much width to leave free on the row about to be drawn, so it stays clear of the dock
    /// button floating over it.
    /// </summary>
    /// <remarks>
    /// Asked of the row rather than worked out per panel, because which card is under the corner
    /// depends on whether the panel is split beside or above, and a rule written per panel is a
    /// rule that is wrong in one of them.
    /// </remarks>
    public static float DockRoom()
    {
        if (DockRect.Max.X <= DockRect.Min.X) return 0f;

        var at = ImGui.GetCursorScreenPos();
        var right = ImGui.GetWindowPos().X + ImGui.GetWindowWidth();
        var line = ImGui.GetFrameHeight();

        if (at.Y > DockRect.Max.Y || at.Y + line < DockRect.Min.Y) return 0f;
        if (right <= DockRect.Min.X) return 0f;

        return right - DockRect.Min.X + EditorSurface.Air;
    }

}

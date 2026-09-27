using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The frame round the scene: its rounded corners, and the buttons at the window's top right.
/// </summary>
public static class EditorSceneFrame
{
    /// <summary>The radius last given to the camera, so it is only told again on a change.</summary>
    private static float _radius = -1f;

    /// <summary>
    /// Rounds the scene's corners: the viewport's while docked, the window's while floating.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Taken off by the renderer rather than painted over here, because what is outside a corner
    /// is meant to be clear, and nothing an interface draws on top can make a pixel clear. The
    /// renderer multiplies the scene's corners down to nothing, antialiased, and on a see-through
    /// window the desktop shows there.
    /// </para>
    /// <para>
    /// Docked the scene is a card among the others and takes a card's rounding. Floating it is the
    /// whole window and takes a window's, which makes the window itself round. Maximized it is
    /// square, since a window against the screen's edges has no corners to show.
    /// </para>
    /// </remarks>
    /// <param name="camera">The scene's camera.</param>
    internal static void Round(Entity camera)
    {
        if (camera.IsNone) return;

        var theme = EditorTheme.Current;
        var logical = EditorWindowFrame.Maximized
            ? 0f
            : EditorShell.Docked ? theme.ChildRounding : theme.WindowRounding;

        var radius = logical * ImGuiRuntime.Scale;

        if (radius == _radius) return;

        _radius = radius;
        Render.SetRoundedCorners(camera, radius);
    }

    /// <summary>
    /// The buttons at the window's top right: the pin that docks the panel, then minimize,
    /// maximize and close where the editor draws its own frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A row of their own above the panels rather than a corner of one, so they stay where a title
    /// bar's are whatever the panels are doing, and no panel has to leave room for them. Round with
    /// a picture in each, like the rest of what floats over the scene, and in the order a title bar
    /// has them, with the pin before them because it belongs to the editor and they belong to the
    /// window.
    /// </para>
    /// <para>
    /// The window's three are there only when <see cref="EditorWindowFrame.Borderless"/>, since a
    /// platform frame has its own, and a picture drawn into an image has no window to act on.
    /// </para>
    /// </remarks>
    internal static void WindowButtons()
    {
        // The size everything else that floats over the scene is, so the buttons in the corner are
        // of that family rather than discs of their own.
        const float Size = EditorSurface.Tall;

        // In the title row above the panels, as far in from the window's top right as the
        // toolbars are from its top left, so the two ends of the row line up.
        var window = ImGuiRuntime.Size;
        var inset = ToolbarView.Inset;

        ImGui.SetNextWindowPos(
            new Vector2(window.X - inset, inset),
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
            EditorSurface.Backed(() =>
            {
                var pin = EditorShell.Docked ? EditorIcons.Pinned : EditorIcons.Loose;

                if (ToolbarView.Circle($"dock{EditorShell.Docked}", pin, false, Size))
                {
                    EditorShell.Docked = !EditorShell.Docked;
                }

                if (ImGui.IsItemHovered())
                {
                    EditorWidgets.Tip(EditorShell.Docked ? "Undock the panel" : "Dock the panel");
                }


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
            });

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

}

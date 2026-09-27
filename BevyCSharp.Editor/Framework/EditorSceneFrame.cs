using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The frame round the scene: its rounded corners, and the buttons at the window's top right.
/// </summary>
public static class EditorSceneFrame
{
    /// <summary>What the cameras were last told, so they are only told again on a change.</summary>
    private static (float Scene, float Window, (float, float, float, float) Ground) _told = (-1f, -1f, default);

    /// <summary>The camera behind the scene that paints the ground, rounded at the window's corners.</summary>
    private static Entity _frame = Entity.None;

    /// <summary>
    /// Rounds the scene's corners and the window's, and paints the ground round the scene.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All of it is the renderer's to do rather than painted here, because anything the interface
    /// draws lies over the scene, so a ground painted round the viewport would darken its corners
    /// as well, and nothing drawn over a pixel can make it clearer than it is.
    /// </para>
    /// <para>
    /// The ground is a camera of its own, behind the scene, that draws nothing and clears the whole
    /// window to the ground color with its own corners rounded off to clear, so docked the window is
    /// as round as it is floating. The scene's camera then puts its viewport over that, with its
    /// corners rounded and filled with the ground, so they match what surrounds them. Floating the
    /// scene is the whole window, with its corners rounded to clear over the frame's, so the frame
    /// is there but never seen. Maximized the window's corners are square, since a window against
    /// the screen's edges has none to show, and a docked viewport keeps its own.
    /// </para>
    /// <para>
    /// A camera's clear color is fixed when it is made, so a ground changed in the style tab makes
    /// the frame again.
    /// </para>
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    /// <param name="camera">The scene's camera.</param>
    internal static void Round(BehaviorContext ctx, Entity camera)
    {
        if (camera.IsNone) return;

        var theme = EditorTheme.Current;
        var scale = ImGuiRuntime.Scale;

        var window = EditorWindowFrame.Maximized ? 0f : theme.WindowRounding * scale;
        var scene = EditorShell.Docked ? theme.ChildRounding * scale : window;
        var ground = EditorTheme.Linear(theme.Ground);

        if (_told == (scene, window, ground)) return;

        var remade = _told.Ground != ground || _frame.IsNone || !ctx.Ecs.IsAlive(_frame);

        _told = (scene, window, ground);

        if (remade)
        {
            if (!_frame.IsNone && ctx.Ecs.IsAlive(_frame)) ctx.Ecs.Despawn(_frame);

            _frame = Frame(ground);

            // Nothing round the viewport but what the frame drew, so the window's own clear is
            // clear, which is what shows past the frame's rounded corners.
            Render.SetClearColor((0f, 0f, 0f, 0f));
        }

        Render.SetRoundedCorners(_frame, window);
        Render.SetRoundedCorners(camera, scene, EditorShell.Docked ? ground : default);
    }

    /// <summary>
    /// Makes the camera that paints the ground: first to draw, over the whole window, seeing
    /// nothing, and cleared to the ground color.
    /// </summary>
    /// <remarks>
    /// On a layer nothing is on, so it draws no entity and no gizmo. No tonemapping and one sample
    /// a pixel, so the ground comes out the color the theme says and the camera matches the scene
    /// it shares the window with.
    /// </remarks>
    private static Entity Frame((float R, float G, float B, float A) ground)
    {
        var frame = Render.SpawnCamera3d(new CameraSettings
        {
            Order = -1,
            Layers = 1u << 30,
            Clear = ClearMode.Custom,
            ClearColor = ground,
        });

        Render.SetPostProcessing(frame, new PostSettings { Tonemapper = Tonemapper.None, Msaa = 1 });

        return frame;
    }

    /// <summary>
    /// The buttons at the window's top right: the pin that docks the panel, then, where the editor
    /// draws its own frame, an empty one that moves the window, and minimize, maximize and close.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A window of their own over everything, so they stay where a title bar's are whatever the
    /// panels are doing, lying over the top of the panel's column rather than taking a row from it. Round with
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
        // A little under the size everything else that floats over the scene is, since these lie
        // over the top of the panel's first row rather than on the scene, and at full size they
        // stood taller than the field beside them by more than reads as deliberate.
        const float Size = EditorSurface.Tall - 2f;

        // As far in from the window's top right as the toolbars are from its top left, so the two
        // ends of the row line up, and over the panel's top when it reaches up there, which it may.
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
            // Where the row is, so the band along the top that moves the window can leave it be.
            _buttons = (ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());

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

            if (EditorWindowFrame.Borderless)
            {
                ImGui.SameLine();

                // A button with nothing on it, to take hold of. The band along the top moves the
                // window too, but it is a few pixels tall and has no edge to aim for, and this is
                // a button's size where the hand already goes for the window's own buttons. Pressed,
                // it hands the window to the platform to move, and pressed twice it maximizes, as
                // a title bar does.
                ToolbarView.Circle("move", string.Empty, false, Size);

                if (ImGui.IsItemActivated())
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) EditorWindowFrame.ToggleMaximized();
                    else Window.StartDragMove();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                    EditorWidgets.Tip("Move the window");
                }

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

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar();
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    /// <summary>Where the window's buttons were last drawn.</summary>
    private static (Vector2 Min, Vector2 Max) _buttons;

    /// <summary>Where the window's buttons were last drawn, which a field under them stops short of.</summary>
    internal static (Vector2 Min, Vector2 Max) Buttons => _buttons;

    /// <summary>Whether a point is on the window's buttons.</summary>
    internal static bool OverButtons(Vector2 at) =>
        at.X >= _buttons.Min.X && at.X < _buttons.Max.X && at.Y >= _buttons.Min.Y && at.Y < _buttons.Max.Y;

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

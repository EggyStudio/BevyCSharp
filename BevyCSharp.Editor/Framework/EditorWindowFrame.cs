using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The window's own frame, which the editor draws instead of the platform, made of a title row to
/// move it by, edges to resize it from, and the buttons that minimize, maximize and close it.
/// </summary>
/// <remarks>
/// <para>
/// The platform's title bar is a band of somebody else's color across the top of a picture that is
/// otherwise the editor's, and it spends a row on a name the taskbar already shows. So the editor
/// asks for a window without one and does its work itself, the way Blender and most editors on
/// every platform do.
/// </para>
/// <para>
/// None of it is visible. The title row along the top is a gap above the panels that is there to
/// be grabbed, and the edges are a few pixels inside the window's own border, so a person finds them
/// where a title bar and a border would have been and nothing is drawn to say so. The pointer
/// changes shape over an edge, which is how every window says one can be dragged.
/// </para>
/// <para>
/// Only in a window. Drawn into an image there is no window to move, so the frame is not there and
/// the panels start at the top.
/// </para>
/// </remarks>
public static class EditorWindowFrame
{
    /// <summary>How far in from each edge the pointer resizes rather than reaches what is under it.</summary>
    private const float Edge = 4f;

    /// <summary>How far along an edge from a corner the pointer resizes from the corner instead.</summary>
    private const float Corner = 14f;

    /// <summary>Whether the frame is the editor's own, which is whether the window was made without one.</summary>
    public static bool Borderless { get; private set; }

    /// <summary>Whether the window was last asked to be maximized.</summary>
    /// <remarks>
    /// Kept here, because Bevy does not say whether a window is maximized, and a button that
    /// toggles it has to know which way it is going. A window maximized some other way, with a
    /// key the platform owns, is one this does not hear about, and the next press puts it back.
    /// </remarks>
    public static bool Maximized { get; private set; }

    /// <summary>
    /// How tall the title row across the top of the window is, which the panels start below.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The row the window's buttons sit in at the right, and the toolbars at the left while the
    /// panel floats: a button's height, and the same inset above it the toolbars keep from the
    /// window's edge. Whatever in it is not a button moves the window, where the frame is the
    /// editor's own.
    /// </para>
    /// <para>
    /// There whether or not the frame is the editor's own, so the pin that docks the panel always
    /// has a place and nothing moves when the same editor is drawn into an image.
    /// </para>
    /// </remarks>
    public static float Grip => ToolbarView.Inset + EditorSurface.Tall;

    /// <summary>Takes the platform's frame off the window, where there is a window.</summary>
    /// <param name="config">How this run was made, which says whether there is one.</param>
    public static void Start(Config config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!App.HasRenderer || config.Headless || config.Offscreen) return;

        // Resizable still. The platform will not resize a window it draws no border for, but it
        // resizes one on request from an edge the app draws, and that request is refused for a
        // window that is not resizable at all.
        Window.SetStyle(decorations: false, resizable: true);
        Borderless = true;
    }

    /// <summary>Minimizes the window.</summary>
    public static void Minimize()
    {
        if (Borderless) Window.Minimize();
    }

    /// <summary>Maximizes the window, or puts it back.</summary>
    public static void ToggleMaximized()
    {
        if (!Borderless) return;

        Maximized = !Maximized;
        Window.SetMaximized(Maximized);
    }

    /// <summary>Closes the editor.</summary>
    public static void Close() => EditorShell.Context?.Exit();

    /// <summary>
    /// The title row, as a window of its own under every other.
    /// </summary>
    /// <remarks>
    /// Drawn before anything else, so on the first frame it is the first window ImGui makes, and
    /// since the panels are never raised it stays under them, and under the buttons and toolbars
    /// in the row, which take their own presses. Pressing anywhere else in it hands the window to
    /// the platform to move, and pressing it twice maximizes, as a title bar does.
    /// </remarks>
    internal static void DrawGrip()
    {
        if (!Borderless) return;

        var window = ImGuiRuntime.Size;

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(window.X, Grip));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin("##grip", Hitbox))
        {
            ImGui.InvisibleButton("##move", new Vector2(window.X, Grip));

            if (ImGui.IsItemActivated())
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) ToggleMaximized();
                else Window.StartDragMove();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    /// <summary>
    /// The edges the window is resized from, as thin windows over everything else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows rather than a check of where the pointer is, so a press on an edge is the edge's
    /// and not also a click on the panel or the scene under it. Drawn last, so on the first frame
    /// they are the last windows ImGui makes and sit over the panels, which are never raised.
    /// </para>
    /// <para>
    /// The top edge is the first few pixels of the title row, and the rest of the row moves the
    /// window, as a platform's title bar is resized from its top and moved from its middle.
    /// </para>
    /// </remarks>
    internal static void DrawEdges()
    {
        if (!Borderless || Maximized) return;

        var window = ImGuiRuntime.Size;

        Side("##edgeTop", Vector2.Zero, new Vector2(window.X, Edge));
        Side("##edgeBottom", new Vector2(0f, window.Y - Edge), new Vector2(window.X, Edge));
        Side("##edgeLeft", new Vector2(0f, Edge), new Vector2(Edge, window.Y - (Edge * 2f)));
        Side("##edgeRight", new Vector2(window.X - Edge, Edge), new Vector2(Edge, window.Y - (Edge * 2f)));
    }

    /// <summary>What every window of the frame is, which is a place to press and nothing to see.</summary>
    private const ImGuiWindowFlags Hitbox =
        EditorSurface.Placed
        | ImGuiWindowFlags.NoBackground
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoNav;

    /// <summary>One edge, which resizes from itself or from the corner at either end of it.</summary>
    /// <param name="id">What to call its window.</param>
    /// <param name="at">Its top left, in logical pixels.</param>
    /// <param name="size">How large it is.</param>
    private static void Side(string id, Vector2 at, Vector2 size)
    {
        ImGui.SetNextWindowPos(at);
        ImGui.SetNextWindowSize(size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.One);

        if (ImGui.Begin(id, Hitbox))
        {
            ImGui.InvisibleButton("##resize", size);

            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                var edge = Which(ImGui.GetIO().MousePos);

                ImGui.SetMouseCursor(edge switch
                {
                    WindowEdge.Top or WindowEdge.Bottom => ImGuiMouseCursor.ResizeNS,
                    WindowEdge.Left or WindowEdge.Right => ImGuiMouseCursor.ResizeEW,
                    WindowEdge.TopRight or WindowEdge.BottomLeft => ImGuiMouseCursor.ResizeNESW,
                    _ => ImGuiMouseCursor.ResizeNWSE,
                });

                if (ImGui.IsItemActivated()) Window.StartDragResize(edge);
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    /// <summary>Which edge or corner a point near the border is on.</summary>
    /// <param name="at">The point, in logical pixels.</param>
    private static WindowEdge Which(Vector2 at)
    {
        var window = ImGuiRuntime.Size;

        var left = at.X < Corner;
        var right = at.X > window.X - Corner;
        var top = at.Y < Corner;
        var bottom = at.Y > window.Y - Corner;

        return (top, bottom, left, right) switch
        {
            (true, _, true, _) => WindowEdge.TopLeft,
            (true, _, _, true) => WindowEdge.TopRight,
            (_, true, true, _) => WindowEdge.BottomLeft,
            (_, true, _, true) => WindowEdge.BottomRight,
            _ when at.Y < Edge => WindowEdge.Top,
            _ when at.Y > window.Y - Edge => WindowEdge.Bottom,
            _ when at.X < Edge => WindowEdge.Left,
            _ => WindowEdge.Right,
        };
    }
}

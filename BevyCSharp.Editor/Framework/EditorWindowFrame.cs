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

        // Floating, the whole title row, since the scene under it is the whole window and the row
        // is where a title bar would be. Docked, the scene runs to the top beside the panel's
        // column, so the row is only above that column, with the gap along the top of the window
        // besides, and a press on the scene is the scene's.
        if (EditorShell.Docked)
        {
            var column = EditorShell.Panel.X;

            Move("##grip", new Vector2(column, 0f), new Vector2(window.X - column, Grip));
            Move("##gripTop", Vector2.Zero, new Vector2(column, EditorSurface.Gutter));
        }
        else
        {
            Move("##grip", Vector2.Zero, new Vector2(window.X, Grip));
        }
    }

    /// <summary>One place to press that moves the window, and maximizes it when pressed twice.</summary>
    /// <param name="id">What to call its window.</param>
    /// <param name="at">Its top left, in logical pixels.</param>
    /// <param name="size">How large it is.</param>
    private static void Move(string id, Vector2 at, Vector2 size)
    {
        if (size.X < 1f || size.Y < 1f) return;

        ImGui.SetNextWindowPos(at);
        ImGui.SetNextWindowSize(size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.One);

        if (ImGui.Begin(id, Hitbox))
        {
            ImGui.InvisibleButton("##move", size);

            // A press on the top edge is the edge's, which resizes rather than moves.
            if (ImGui.IsItemActivated() && OnEdge is null)
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) ToggleMaximized();
                else Window.StartDragMove();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    /// <summary>The edge or corner the pointer is on, where pressing resizes the window, or none.</summary>
    /// <remarks>
    /// Worked out at the start of the interface's frame, so what would otherwise take the press
    /// (the title row, a panel, the scene) can ask and leave it alone.
    /// </remarks>
    public static WindowEdge? OnEdge { get; private set; }

    /// <summary>Works out whether the pointer is on an edge, before anything is drawn.</summary>
    internal static void Sense()
    {
        OnEdge = null;

        if (!Borderless || Maximized) return;

        var at = ImGui.GetIO().MousePos;
        var window = ImGuiRuntime.Size;

        // Off the window, which is how ImGui says the pointer is somewhere else entirely.
        if (at.X < 0f || at.Y < 0f || at.X >= window.X || at.Y >= window.Y) return;

        var near = at.X < Edge || at.Y < Edge || at.X >= window.X - Edge || at.Y >= window.Y - Edge;

        if (near) OnEdge = Which(at);
    }

    /// <summary>
    /// The edges the window is resized from: the pointer's shape over them, and a press on one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asked of where the pointer is rather than drawn as windows of their own. Windows along the
    /// edges lost to whatever window lay over the same pixels, which is a panel on the right, the
    /// tab strip along the bottom and the title row along the top, so only the left edge, with
    /// nothing over it, ever resized. A few pixels at the very edge are inside every surface's own
    /// padding, so nothing there has anything to press, and the scene and the title row ask
    /// <see cref="OnEdge"/> before they act on a press.
    /// </para>
    /// <para>
    /// Last in the frame, so the shape it sets is the one the frame ends with, over whatever a
    /// panel under the pointer asked for. The top edge is the first few pixels of the title row,
    /// and the rest of the row moves the window, as a platform's title bar is resized from its top
    /// and moved from its middle.
    /// </para>
    /// </remarks>
    internal static void DrawEdges()
    {
        if (OnEdge is not { } edge) return;

        // Not while something is being dragged or a menu is up. A drag that runs off the edge
        // keeps the shape it started with, and a menu over the edge is what the pointer is on.
        if (ImGui.IsAnyItemActive() || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId)) return;

        ImGui.SetMouseCursor(edge switch
        {
            WindowEdge.Top or WindowEdge.Bottom => ImGuiMouseCursor.ResizeNS,
            WindowEdge.Left or WindowEdge.Right => ImGuiMouseCursor.ResizeEW,
            WindowEdge.TopRight or WindowEdge.BottomLeft => ImGuiMouseCursor.ResizeNESW,
            _ => ImGuiMouseCursor.ResizeNWSE,
        });

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) Window.StartDragResize(edge);
    }

    /// <summary>What the title row is, which is a place to press and nothing to see.</summary>
    private const ImGuiWindowFlags Hitbox =
        EditorSurface.Placed
        | ImGuiWindowFlags.NoBackground
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoNav;

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

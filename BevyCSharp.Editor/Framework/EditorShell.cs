using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Where the editor's panels are, and what they are drawn over.
/// </summary>
/// <remarks>
/// <para>
/// The scene fills the window and the panels float over it, spaced from the window's edges and
/// from each other, the way Unity's editor arranges itself. A button on the panel docks it: the
/// margins go, the panel meets the window's edge, and the camera is told to draw into what is left
/// rather than behind it.
/// </para>
/// <para>
/// Everything here is worked out from three numbers a person can change (whether it is docked, how
/// wide the panel is, and which tab is open), and nothing is remembered between frames beyond
/// those. That is what immediate mode buys: the arrangement is a calculation, not a tree of
/// widgets that has to be kept in step with itself.
/// </para>
/// </remarks>
public static class EditorShell
{
    /// <summary>How far a floating panel sits from the window's edge and from its neighbours.</summary>
    public const float Margin = 10f;

    /// <summary>
    /// How narrow the panel has to be before the world sits above the data rather than beside it.
    /// </summary>
    /// <remarks>
    /// Beside is the normal arrangement: two columns of a tree and its details is what an editor
    /// looks like. Stacking is what happens when there is no room for two columns, which is the
    /// panel at its narrowest and nowhere else.
    /// </remarks>
    public const float Stacks = 460f;

    /// <summary>The narrowest the panel goes, and what it opens at.</summary>
    public const float Narrowest = 320f;

    /// <summary>
    /// The face numbers are written in.
    /// </summary>
    /// <remarks>
    /// Monospaced, because a column of numbers that changes while it is being dragged is a column
    /// whose digits are all different widths: the value shifts sideways under the pointer with
    /// every digit that turns over, and three boxes side by side do it out of step with each
    /// other. A figure the same width as every other figure holds still.
    /// </remarks>
    public const string Figures = "PaperMono-Regular.ttf";

    /// <summary>Whether the panel is against the window's edge, with the scene beside it.</summary>
    public static bool Docked { get; set; }

    /// <summary>How wide the panel is, in logical pixels.</summary>
    /// <remarks>
    /// As narrow as it goes, which is the stacked arrangement: an editor opens with the scene
    /// taking the room and the panel taking what it needs, and widening it is a thing somebody
    /// does when they want two columns.
    /// </remarks>
    public static float PanelWidth { get; set; } = Narrowest;

    /// <summary>How much of the panel the world gets, the data taking the rest.</summary>
    public static float WorldShare { get; set; } = 0.3f;

    /// <summary>Which tab is open, or -1 for none.</summary>
    public static int OpenTab { get; set; } = -1;

    /// <summary>How tall an open tab's contents are.</summary>
    public static float TabHeight { get; set; } = 260f;

    /// <summary>What is going on this frame.</summary>
    public static BehaviorContext? Context { get; private set; }

    /// <summary>The world, as of this frame.</summary>
    public static EcsWorld Ecs =>
        Context?.Ecs ?? throw new InvalidOperationException("the editor has not started a frame");

    /// <summary>The frame the editor is on.</summary>
    public static ulong Frame { get; private set; }

    /// <summary>Where the scene is drawn, in logical pixels.</summary>
    public static (float X, float Y, float Width, float Height) Scene { get; private set; }

    /// <summary>Where the panel is.</summary>
    public static (float X, float Y, float Width, float Height) Panel { get; private set; }

    /// <summary>Whether the pointer is over the interface rather than over the scene.</summary>
    /// <remarks>
    /// ImGui answers it: it hit tested every window this frame, and a click it wants is a click
    /// the scene must not also act on. No position is asked for because none is needed, and a
    /// parameter that is ignored reads as one that is not.
    /// </remarks>
    public static bool PointerOverPanel => ImGuiRuntime.WantsMouse;

    /// <summary>The tabs along the bottom, in the order they are listed.</summary>
    public static List<(string Name, Action Draw)> Tabs { get; } = [];

    /// <summary>Opens the editor's interface.</summary>
    public static void Load(string assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(assets);

        // Two faces: what everything is written in, and the one numbers are written in.
        ImGuiRuntime.Start(
            Path.Combine(assets, "fonts"),
            15f,
            "Inter-Regular.ttf",
            Figures);

        // Whatever was dialled in and saved, or the editor's own look when there is no file. A
        // theme is an asset like any other: read at startup, edited by hand or in the style tab.
        var saved = Path.Combine(assets, "theme.txt");

        Wear(File.Exists(saved)
            ? EditorTheme.Restore(File.ReadAllText(saved))
            : EditorTheme.Modern);
    }

    /// <summary>Puts a theme on.</summary>
    public static void Wear(EditorTheme theme) => EditorTheme.Apply(theme);

    /// <summary>Draws the editor, and says where the scene goes.</summary>
    public static void Tick(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        Context = ctx;
        Frame = ctx.Time.FrameCount;
        EditorGizmoSlot.Frame = Frame;

        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);

        EditorPicking.Tick(ctx);

        // A selection whose entity is gone is worse than none: the details panel would read
        // whatever took its place in storage.
        EditorSelection.Prune(ctx.Ecs);

        var window = ImGuiRuntime.Size;
        var margin = Docked ? 0f : Margin;

        PanelWidth = Math.Clamp(PanelWidth, Narrowest, Math.Max(Narrowest + 40f, window.X - 320f));

        var panelX = window.X - margin - PanelWidth;
        Panel = (panelX, margin, PanelWidth, window.Y - (margin * 2f));

        var strip = EditorStrip.Shut + (OpenTab >= 0 ? TabHeight + 1f : 0f);
        var tabsWidth = panelX - margin - (Docked ? 0f : Margin);

        // Docked, the scene keeps the top left corner and the tabs sit under it. Floating, the
        // scene is the whole window and everything else is over it.
        //
        // Docked it also stops a gutter short of the panel rather than hard against it, so its
        // right edge lands on the same line as the right edge of the card in the strip below, and
        // the handle that moves the panel has room on both sides instead of a viewport against one
        // of them.
        Scene = Docked
            ? (0f, 0f, Math.Max(1f, panelX - EditorSurface.Gutter), Math.Max(1f, window.Y - strip))
            : (0f, 0f, window.X, window.Y);

        // Where the scene is still visible, which is what anything drawn over the scene has to
        // stay inside: docked that is the scene itself, floating it is what the panels leave.
        Free = Docked
            ? (Scene.X + Scene.Width, Scene.Y + Scene.Height)
            : (panelX, window.Y - strip - margin);

        EditorSceneFrame.Round();
        EditorPanes.Draw();
        DrawOrientation(ctx);
        EditorStrip.Draw(tabsWidth, strip, margin);
        ToolbarView.Draw(ctx);

        // Last, so it floats over every panel rather than under whichever was drawn after it.
        EditorSceneFrame.DockButton();
        EditorFlyout.Draw(ctx);
        Apply(ctx);
    }

    /// <summary>Ends the frame, handing the engine what the interface came to.</summary>
    public static void Draw()
    {
        if (ImGuiRuntime.IsRunning) ImGuiRuntime.End();
    }

    /// <summary>Whether the world sits above the data rather than beside it.</summary>
    public static bool Stacked => PanelWidth < Stacks;

    /// <summary>
    /// Which way the world faces, at the bottom right of what the scene has to itself.
    /// </summary>
    /// <remarks>
    /// Not the bottom right of the scene: undocked the scene is the whole window and the panel is
    /// over its right, so a cross in that corner is a cross behind the panel.
    /// </remarks>
    private static void DrawOrientation(BehaviorContext ctx)
    {
        var half = OrientationGizmo.Size * 0.5f;
        var gap = (Docked ? 0f : Margin) + half + 12f;

        var right = Docked ? Scene.X + Scene.Width : Panel.X;
        var bottom = Docked ? Scene.Y + Scene.Height : Free.Bottom;

        OrientationGizmo.Draw(ctx, new Vector2(right - gap, bottom - gap));
    }

    /// <summary>What the scene has to itself: the part of it no panel is over.</summary>
    public static (float Right, float Bottom) Free { get; private set; }

    /// <summary>Tells the camera which part of the window it has.</summary>
    private static void Apply(BehaviorContext ctx)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        var scale = ImGuiRuntime.Scale;

        // Floating, the scene is the whole window and there is nothing to say. Docked, it is the
        // rectangle the panels left, in physical pixels because that is what a framebuffer is.
        if (!Docked)
        {
            Render.SetViewport(camera, 0, 0, 0, 0);
            return;
        }

        Render.SetViewport(
            camera,
            (uint)MathF.Round(Scene.X * scale),
            (uint)MathF.Round(Scene.Y * scale),
            (uint)MathF.Round(Scene.Width * scale),
            (uint)MathF.Round(Scene.Height * scale));
    }
}

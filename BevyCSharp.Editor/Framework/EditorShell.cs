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
/// from each other, the way Unity's editor arranges itself. A button on the panel docks it, and
/// then the margins go, the panel meets the window's edge, and the camera is told to draw into what
/// is left rather than behind it.
/// </para>
/// <para>
/// Everything here is worked out from three numbers a person can change (whether it is docked, how
/// wide the panel is, and which tab is open), and nothing is remembered between frames beyond
/// those. That is what immediate mode buys. The arrangement is a calculation rather than a tree of
/// widgets that has to be kept in step with itself.
/// </para>
/// </remarks>
public static class EditorShell
{
    /// <summary>How far anything lying on the scene sits from the edge of what it lies on.</summary>
    /// <remarks>
    /// The same gap the panels keep round their cards, so a group of buttons floating in a corner
    /// of the scene is as far from the edge as a card is from the edge of the panel holding it.
    /// </remarks>
    public const float Margin = EditorSurface.Gutter;

    /// <summary>
    /// How narrow the panel has to be before the world sits above the data rather than beside it.
    /// </summary>
    /// <remarks>
    /// Beside is the normal arrangement, because two columns of a tree and its details is what an
    /// editor looks like. Stacking is what happens when there is no room for two columns, which is
    /// the panel at its narrowest and nowhere else.
    /// </remarks>
    public const float Stacks = 460f;

    /// <summary>The narrowest the panel goes, and what it opens at.</summary>
    public const float Narrowest = 320f;

    /// <summary>
    /// The face numbers are written in.
    /// </summary>
    /// <remarks>
    /// Monospaced, because a column of numbers that changes while it is being dragged is a column
    /// whose digits are all different widths. The value shifts sideways under the pointer with
    /// every digit that turns over, and three boxes side by side do it out of step with each
    /// other. A figure the same width as every other figure holds still.
    /// </remarks>
    public const string Figures = "PaperMono-Regular.ttf";

    /// <summary>Whether the panel is against the window's edge, with the scene beside it.</summary>
    public static bool Docked { get; set; }

    /// <summary>How wide the panel is, in logical pixels.</summary>
    /// <remarks>
    /// As narrow as it goes, which is the stacked arrangement. An editor opens with the scene
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
    /// ImGui answers it, having hit tested every window this frame, and a click it wants is a
    /// click the scene must not also act on. No position is asked for because none is needed, and a
    /// parameter that is ignored reads as one that is not.
    /// </remarks>
    public static bool PointerOverPanel => ImGuiRuntime.WantsMouse;

    /// <summary>The tabs along the bottom, in the order they are listed.</summary>
    /// <remarks>
    /// A list a game adds to, the way the menu and the toolbar are. A tab is a name and something
    /// to draw, and where it goes and what it is drawn on is the strip's business.
    /// </remarks>
    public static List<EditorTab> Tabs { get; } = [];

    /// <summary>
    /// Raises the tab of that name, or puts it away when it is the one showing.
    /// </summary>
    /// <remarks>
    /// By name rather than by number, because a game that adds a tab of its own changes what every
    /// number after it means, and nothing outside this list should have to know the order.
    /// </remarks>
    /// <param name="name">What the tab is called.</param>
    public static void Show(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var which = Tabs.FindIndex(tab => tab.Name == name);

        if (which >= 0) OpenTab = OpenTab == which ? -1 : which;
    }

    /// <summary>Opens the editor's interface.</summary>
    public static void Load(string assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(assets);

        // Two faces, one for everything and one for numbers.
        ImGuiRuntime.Start(
            Path.Combine(assets, "fonts"),
            15f,
            "Inter-Regular.ttf",
            Figures);

        // Whatever was dialled in and saved, or the editor's own look when there is no file. A
        // theme is an asset like any other, read at startup and edited by hand or in the style tab.
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

        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);

        EditorPicking.Tick(ctx);

        // A selection whose entity is gone is worse than none, because the details panel would
        // read whatever took its place in storage.
        EditorSelection.Prune(ctx.Ecs);

        var window = ImGuiRuntime.Size;

        PanelWidth = Math.Clamp(PanelWidth, Narrowest, Math.Max(Narrowest + 40f, window.X - 320f));

        // The panel and the strip are in the same place whether the editor is docked or not, and
        // they reach the window's own edges in both. What a person sees is not the window they are
        // drawn in but the cards inside them, which the padding holds a gap in from every edge.
        // Docking changes what is behind those cards rather than where they are, so nothing on
        // screen moves when it is switched.
        var panelX = window.X - PanelWidth;
        Panel = (panelX, 0f, PanelWidth, window.Y);

        var strip = EditorStrip.Shut + (OpenTab >= 0 ? TabHeight + 1f : 0f);
        var tabsWidth = panelX;

        // Docked, the scene keeps the top left corner and the tabs sit under it. Floating, the
        // scene is the whole window and everything else is over it.
        //
        // Docked it runs right up to the panel's own edge, so the only gap between the picture and
        // the cards beside it is the one the panel keeps inside itself, which is the same gap
        // every other pair of surfaces has between them.
        Scene = Docked
            ? (0f, 0f, Math.Max(1f, panelX), Math.Max(1f, window.Y - strip))
            : (0f, 0f, window.X, window.Y);

        // Where the scene is still visible, which is what anything drawn over the scene has to
        // stay inside. The same rectangle either way, because the panels are.
        Free = (panelX, window.Y - strip);

        EditorSceneFrame.Round();
        EditorPanes.Draw();
        DrawOrientation(ctx);
        EditorStrip.Draw(tabsWidth, strip);
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
    /// Not the bottom right of the scene, because undocked the scene is the whole window and the
    /// panel is over its right, so a cross in that corner is a cross behind the panel.
    /// </remarks>
    private static void DrawOrientation(BehaviorContext ctx)
    {
        var half = OrientationGizmo.Size * 0.5f;
        var gap = Margin + half + 12f;

        OrientationGizmo.Draw(ctx, new Vector2(Free.Right - gap, Free.Bottom - gap));
    }

    /// <summary>What the scene has to itself, which is the part of it no panel is over.</summary>
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

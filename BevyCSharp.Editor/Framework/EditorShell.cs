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
/// those. Immediate mode buys that. The arrangement is a calculation rather than a tree of widgets
/// that has to be kept in step with itself.
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
    /// Beside is the normal arrangement, because an editor looks like two columns of a tree and its
    /// details. It stacks when there is no room for two columns, which is the panel at its
    /// narrowest and nowhere else.
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
    /// <remarks>
    /// Docked unless set otherwise, since the scene beside the panel is the arrangement the editor
    /// opens in. A saved preference says otherwise where somebody has chosen to float it.
    /// </remarks>
    public static bool Docked { get; set; } = true;

    /// <summary>How wide the panel is, in logical pixels.</summary>
    /// <remarks>
    /// As narrow as it goes, which is the stacked arrangement. An editor opens with the scene
    /// taking the room and the panel taking what it needs, and somebody widens it when they want
    /// two columns.
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
    /// ImGui answers it, having hit tested every window this frame, and a click it takes is a click
    /// the scene must not also act on. The few pixels along the window's own edge count as well,
    /// since a press there resizes the window. No position is asked for because none is needed,
    /// and a parameter that is ignored reads as one that is not.
    /// </remarks>
    public static bool PointerOverPanel =>
        ImGuiRuntime.WantsMouse || EditorWindowFrame.OnEdge is not null || EditorWindowFrame.OnBand;

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

    /// <summary>Opens a tab by name, leaving an already open one open.</summary>
    /// <remarks>
    /// Apart from <see cref="Show"/>, which toggles, because the second press on a button in the
    /// strip puts the panel away, and anything asking to be taken to a place arrives whether or not
    /// it was already there.
    /// </remarks>
    public static void Open(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var which = Tabs.FindIndex(tab => tab.Name == name);

        if (which >= 0) OpenTab = which;
    }

    /// <summary>
    /// How large the editor's text is, in logical pixels.
    /// </summary>
    /// <remarks>
    /// Everything else follows from it. A field is as tall as a line of this plus its padding, a
    /// row's pitch is that plus the spacing, and a picture beside a name is the height of the name,
    /// so this is the one number that decides how dense the editor is.
    /// </remarks>
    public const float Lettering = 15f;

    /// <summary>Opens the editor's interface.</summary>
    public static void Load(string assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(assets);

        // Two faces, one for everything and one for numbers.
        ImGuiRuntime.Start(
            Path.Combine(assets, "fonts"),
            Lettering,
            "Inter-Regular.ttf",
            Figures);

        // Whatever was dialed in and saved, then the one the project ships, then the editor's own
        // look. A theme is an asset like any other, read at startup and edited by hand or in the
        // style tab.
        var saved = Path.Combine(assets, "theme.txt");

        Wear(File.Exists(saved)
            ? EditorTheme.Restore(File.ReadAllText(saved))
            : EditorProjectFile.Settings.Theme is { Length: > 0 } shipped
                ? EditorTheme.Restore(shipped)
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

        // Before anything is drawn, and whether or not the console is open, so an answer that
        // arrives while it is shut is there when it opens.
        ConsoleView.AnswerLater(ctx.World);

        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);

        // Before anything asks whether the pointer is on the scene, since a press on the window's
        // edge resizes it and is nobody else's.
        EditorWindowFrame.Sense();

        EditorPicking.Tick(ctx);

        // Every frame, open or not, so the graph is full when it is opened.
        EditorStats.Tick(ctx);

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
        // Up to the top of the window, with the same gap there as at every other edge, which is the
        // panel's own padding. The window's buttons lie over the panel's top.
        const float top = 0f;

        var panelX = window.X - PanelWidth;
        Panel = (panelX, top, PanelWidth, window.Y - top);

        var strip = EditorStrip.Shut + (OpenTab >= 0 ? TabHeight + 1f : 0f);
        var tabsWidth = panelX;

        // Docked, the scene keeps the top left and the tabs sit under it. Floating, the scene is
        // the whole window and everything else is over it.
        //
        // Docked it is a surface like the panels, so it keeps the gap every surface keeps from the
        // window's edges, and its top is level with the panel's cards. On its right it runs up to
        // the panel's own edge, and below it up to the strip's, because the gap there is the one
        // those keep inside themselves, and a second one would double it.
        var inset = EditorSurface.Gutter;

        Scene = Docked
            ? (inset,
                top + inset,
                Math.Max(1f, panelX - inset),
                Math.Max(1f, window.Y - strip - top - inset))
            : (0f, 0f, window.X, window.Y);

        // Where the scene is still visible, which anything drawn over the scene has to stay inside.
        // The same rectangle either way, because the panels are.
        Free = (panelX, window.Y - strip);

        // First, so the strip is the first window ImGui makes and stays under everything after it.
        EditorWindowFrame.Sync();
        EditorWindowFrame.DrawGrip();

        EditorPanes.Draw();
        DrawOrientation(ctx);

        // While a model is dragged from the assets, so it can be dropped on the scene.
        AssetsTab.SceneDrop(ctx);

        // A file's thumbnail at a time, for whichever tiles asked for one, in the browser or in
        // the picker.
        Thumbnails.Tick(ctx);
        EditorStrip.Draw(tabsWidth, strip);
        ToolbarView.Draw(ctx);

        // After the panels, because what decides whether the asset preview is still wanted is
        // whether any of them asked for it while they drew.
        PreviewRenderer.Keep(ctx);

        // Last, so it floats over every panel rather than under whichever was drawn after it.
        EditorSceneFrame.WindowButtons();
        EditorFlyout.Draw(ctx);
        PickerWindow.Draw(ctx);

        // Over everything, so a press at the window's edge resizes it rather than reaching the
        // panel under it.
        EditorWindowFrame.DrawEdges();

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

        // In the corner, as far in as the toolbars are from theirs. Above the buttons pinned to the
        // same corner where a game has put any, since a cross laid over a row of buttons is a cross
        // with a button through one of its arms, and the editor's own sit at the top now.
        var side = Free.Right - ToolbarView.Inset - half;
        var above = EditorToolbar.Slot(ToolbarSlot.BottomRight).Count > 0
            ? EditorSurface.Tall + EditorSurface.Air
            : 0f;
        var up = ToolbarView.Inset + above + half;

        OrientationGizmo.Draw(ctx, new Vector2(side, Free.Bottom - up));
    }

    /// <summary>Where anything pinned to the top of the scene starts.</summary>
    /// <remarks>
    /// The scene's own top. Docked that is below the title row. Floating it is the window's top, so
    /// the toolbars pinned there sit in the title row, level with the window's buttons at the other
    /// end of it, the way a title bar holds a menu at one end and the window's buttons at the
    /// other. The row moves the window only where neither is.
    /// </remarks>
    public static float SceneTop => Scene.Y;

    /// <summary>What the scene has to itself, which is the part of it no panel is over.</summary>
    public static (float Right, float Bottom) Free { get; private set; }

    /// <summary>Tells the camera which part of the window it has, and how round its corners are.</summary>
    private static void Apply(BehaviorContext ctx)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        EditorSceneFrame.Round(ctx, camera);
        SceneCamera.Apply(ctx, camera);

        var scale = ImGuiRuntime.Scale;

        // Floating, the scene is the whole window and there is nothing to say. Docked, it is the
        // rectangle the panels left, in physical pixels because a framebuffer is measured in them.
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

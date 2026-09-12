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
/// Everything here is worked out from three numbers a person can change — whether it is docked, how
/// wide the panel is, and which tab is open — and nothing is remembered between frames beyond
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

    private const float Header = 28f;

    /// <summary>
    /// How tall the strip of tabs is, measured rather than guessed.
    /// </summary>
    /// <remarks>
    /// A guess is wrong the moment the font or the padding changes, and what it looks like when it
    /// is wrong is a row of tabs with their text cut off along the bottom of the window.
    /// </remarks>
    private static float TabStrip => ImGui.GetFrameHeight() + (StripPadding * 2f);

    /// <summary>How much air the strip of tabs keeps around them.</summary>
    /// <remarks>
    /// Less than a panel's, because a strip that is mostly air reads as an empty panel with some
    /// words in it rather than as a row of tabs.
    /// </remarks>
    private const float StripPadding = 5f;

    /// <summary>The frame a click on the scene landed, while it is still waiting to be answered.</summary>
    private static ulong _emptyClick;

    /// <summary>The frame the engine last said a click had hit something.</summary>
    private static ulong _pickedOn;

    /// <summary>The frame the pointer's button last went down.</summary>
    private static ulong _pressedOn;

    /// <summary>Whether that press was on the scene rather than on the interface.</summary>
    private static bool _onScene;

    /// <summary>How many frames the engine gets to say what a click hit before it hit nothing.</summary>
    private const ulong Patience = 3;

    /// <summary>
    /// What a floating window's background is painted in.
    /// </summary>
    /// <remarks>
    /// The panel while it floats over the scene, and the ground while it is docked, where there is
    /// no scene behind it to show through and a grey a shade off black is a frame nobody asked for.
    /// </remarks>
    private static Vector4 Chrome() => EditorTheme.Alpha(
        Docked ? EditorTheme.Current.Ground : EditorTheme.Current.Panel,
        Docked ? 1f : EditorTheme.Current.WindowAlpha);

    /// <summary>The gap between the panel's edge and the cards inside it.</summary>
    private const float Gutter = 6f;

    /// <summary>How much air a card keeps inside its own edge.</summary>
    private const float Air = 8f;

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
    public static bool PointerOverPanel(float x, float y)
    {
        _ = x;
        _ = y;

        // ImGui knows: it hit tested every window this frame, and a click it wants is a click the
        // scene must not also act on.
        return ImGuiRuntime.WantsMouse;
    }

    /// <summary>Which branch of the menu is up, if any.</summary>
    private static string? _menu;

    /// <summary>Whether it still has to be handed to ImGui, which happens once per opening.</summary>
    private static bool _opening;

    /// <summary>Whether a menu is up, for anything asking outside the interface's own frame.</summary>
    /// <remarks>
    /// Asked rather than looked up: ImGui answers questions about its windows only between the
    /// beginning and the end of a frame, and a system that runs before the interface does would be
    /// asking a context that is not in one.
    /// </remarks>
    public static bool MenuOpen => _menu is not null;

    /// <summary>Shows a branch of the editor's menu at a point.</summary>
    /// <remarks>
    /// A menu is a popup, and a popup belongs to the frame it is opened in, so what is asked for
    /// here is remembered and put up on the next pass through the interface.
    /// </remarks>
    public static void ShowMenu(string branch, float x, float y)
    {
        _menu = branch ?? string.Empty;
        _opening = true;
        MenuAt = new Vector2(x, y);
    }

    /// <summary>Shows a branch, or puts it away if it is the one that is up.</summary>
    public static void ToggleMenu(string branch, float x, float y)
    {
        if (_menu is not null)
        {
            _menu = null;
            return;
        }

        ShowMenu(branch, x, y);
    }

    /// <summary>Where the menu was asked for.</summary>
    private static Vector2 MenuAt { get; set; }

    /// <summary>The tabs along the bottom, in the order they are listed.</summary>
    public static List<(string Name, Action Draw)> Tabs { get; } = [];

    /// <summary>Opens the editor's interface.</summary>
    public static void Load(string assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(assets);

        ImGuiRuntime.Start(
            Path.Combine(assets, "fonts"),
            15f,
            "Inter-Regular.ttf");

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

        // A click on a mesh selects it, which is the other half of what the world list does. The
        // shell does this rather than a panel, because a selection belongs to the editor.
        //
        // Not while the interface has the pointer: the scene is raycast by the engine, which knows
        // nothing about the panels drawn over it, so a click on a panel would also land on
        // whatever happens to be behind it.
        MarqueeSelect.Tick(ctx);

        // Where the click that is being answered began. Asked at the press rather than now,
        // because the pointer moves: the engine raycasts the scene and knows nothing of the panels
        // drawn over it, so what decides whether a pick belongs to the editor is where the button
        // went down, and a hand that has since travelled over a panel has not changed that.
        if (ctx.Input.MousePressed(MouseButton.Left))
        {
            _pressedOn = Frame;
            _onScene = !ImGuiRuntime.WantsMouse;
        }

        // A click on the scene that hit nothing means nothing was meant, which is how every editor
        // clears a selection.
        //
        // Only when nothing has answered this click since the button went down. The engine
        // raycasts on its own schedule and the pointer is read on ours, so the answer can arrive
        // on the frame of the press, of the release, or after it; a rule that only waits for one
        // that comes later throws away a selection the moment it is made.
        var answered = _pickedOn >= _pressedOn;

        if (MarqueeSelect.Clicked && _onScene && !answered) _emptyClick = Frame;

        foreach (var picked in Picking.Drain())
        {
            if (!_onScene) break;

            // A release that ended a box is not also a click on whatever the pointer came to rest
            // over: the box already said what it meant.
            if (MarqueeSelect.Dragging) break;

            _pickedOn = Frame;
            _emptyClick = 0;

            EditorSelection.Select(picked);
        }

        if (_emptyClick != 0 && Frame - _emptyClick >= Patience)
        {
            _emptyClick = 0;
            EditorSelection.Clear();
        }

        // A selection whose entity is gone is worse than none: the details panel would read
        // whatever took its place in storage.
        EditorSelection.Prune(ctx.Ecs);

        var window = ImGuiRuntime.Size;
        var margin = Docked ? 0f : Margin;

        PanelWidth = Math.Clamp(PanelWidth, Narrowest, Math.Max(Narrowest + 40f, window.X - 320f));

        var panelX = window.X - margin - PanelWidth;
        Panel = (panelX, margin, PanelWidth, window.Y - (margin * 2f));

        var strip = TabStrip + (OpenTab >= 0 ? TabHeight + 1f : 0f);
        var tabsWidth = panelX - margin - (Docked ? 0f : Margin);

        // Docked, the scene keeps the top left corner and the tabs sit under it. Floating, the
        // scene is the whole window and everything else is over it.
        Scene = Docked
            ? (0f, 0f, Math.Max(1f, panelX), Math.Max(1f, window.Y - strip))
            : (0f, 0f, window.X, window.Y);

        // Where the scene is still visible, which is what anything drawn over the scene has to
        // stay inside: docked that is the scene itself, floating it is what the panels leave.
        Free = Docked
            ? (Scene.X + Scene.Width, Scene.Y + Scene.Height)
            : (panelX, window.Y - strip - margin);

        RoundScene();
        DrawPanel();
        DrawOrientation(ctx);
        DrawTabs(tabsWidth, strip, margin);
        DrawToolbars(ctx);

        // Last, so it floats over every panel rather than under whichever was drawn after it.
        DrawDock();
        DrawMenu(ctx);
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
    private static void RoundScene()
    {
        if (!Docked) return;

        var radius = EditorTheme.Current.WindowRounding;
        if (radius < 1f) return;

        // Docked, the scene is a card among the other cards, and what a corner taken off a card
        // shows is the surface it is lying on: the panel the cards are laid out in, which is the
        // darkest thing on the screen that is not the ground itself.
        var draw = ImGui.GetBackgroundDrawList();
        var theme = EditorTheme.Current;

        // One coat, opaque. Two of them put the feathered edge an antialiased fill draws down
        // twice, and the second one over the first is a seam along the arc.
        var color = ImGui.GetColorU32(EditorTheme.Alpha(theme.Ground, 1f));

        var left = Scene.X;
        var top = Scene.Y;
        var right = Scene.X + Scene.Width;
        var bottom = Scene.Y + Scene.Height;

        Wedge(draw, new Vector2(left + radius, top + radius), radius, MathF.PI, MathF.PI * 1.5f, new Vector2(left, top), color);
        Wedge(draw, new Vector2(right - radius, top + radius), radius, MathF.PI * 1.5f, MathF.PI * 2f, new Vector2(right, top), color);
        Wedge(draw, new Vector2(right - radius, bottom - radius), radius, 0f, MathF.PI * 0.5f, new Vector2(right, bottom), color);
        Wedge(draw, new Vector2(left + radius, bottom - radius), radius, MathF.PI * 0.5f, MathF.PI, new Vector2(left, bottom), color);
    }

    /// <summary>One corner's worth of what a rounded rectangle leaves out.</summary>
    /// <param name="draw">What to draw into.</param>
    /// <param name="middle">Where the corner's arc is centred.</param>
    /// <param name="radius">How large the arc is.</param>
    /// <param name="from">Where the arc starts, in radians.</param>
    /// <param name="to">Where it ends.</param>
    /// <param name="corner">The square corner the arc cuts off.</param>
    /// <param name="color">What to fill it with.</param>
    private static void Wedge(
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

    /// <summary>The world beside the data, or above it when there is no room for two columns.</summary>
    private static void DrawPanel()
    {
        ImGui.SetNextWindowPos(new Vector2(Panel.X, Panel.Y));
        ImGui.SetNextWindowSize(new Vector2(Panel.Width, Panel.Height));

        // Seen through, so the scene is behind the panel rather than cut off by it. Thinner than
        // the cards inside it: this is the layer against the scene.
        //
        // Docked there is no scene behind it at all, so it is solid whatever the alpha says, and
        // what it is solid in is the ground: the darkest there is, and the same thing the corners
        // taken off the viewport are painted in, so the frame round the scene is one colour.
        ImGui.SetNextWindowBgAlpha(Docked ? 1f : EditorTheme.Current.WindowAlpha);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Chrome());

        // Square against the window's edge when it is docked. A rounded corner is what says a
        // thing is floating, and a docked panel is not.
        //
        // Floating, whatever the style says: a value taken from the theme would be written over
        // the style editor's every frame, and nothing dragged there would ever hold.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowRounding,
            Docked ? 0f : ImGui.GetStyle().WindowRounding);

        // One inset rather than two. The panel keeps a gutter wide enough to read as a gap between
        // its cards, and the padding a person sees is the one inside each card; the panel's own
        // padding on top of that is the doubled air the old look was criticised for. The stock look
        // has no card fill at all, so there the window's padding is the only one there is.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowPadding,
            EditorTheme.Current.Stock ? ImGui.GetStyle().WindowPadding : new Vector2(Gutter, Gutter));

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoScrollbar

            // The wheel never moves the panel itself. Without this, rolling over a list that has
            // reached its end hands the wheel to whatever holds it, and the whole panel slides up
            // under its own edge: the world shrinks away, the split appears to climb, and nothing
            // put it there but a scroll that had nowhere else to go.
            | ImGuiWindowFlags.NoScrollWithMouse;

        if (!ImGui.Begin("##panel", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            return;
        }

        Handle();

        var body = ImGui.GetContentRegionAvail();

        if (Stacked)
        {
            var world = MathF.Max(120f, body.Y * WorldShare);

            Card("##world", new Vector2(0f, world), WorldPanel.Draw);
            Splitter(body.X);
            Card("##data", new Vector2(0f, 0f), DetailsPanel.Draw);
        }
        else
        {
            // ImGui's own columns, which come with the grip between them: dragging one edge is
            // something the table already knows how to do, and nothing here has to work out where
            // the pointer went.
            var table = ImGuiTableFlags.Resizable
                | ImGuiTableFlags.NoBordersInBody
                | ImGuiTableFlags.SizingStretchProp
                | ImGuiTableFlags.NoSavedSettings;

            if (ImGui.BeginTable("##split", 2, table, body))
            {
                ImGui.TableSetupColumn("##worldcol", ImGuiTableColumnFlags.WidthStretch, WorldShare);
                ImGui.TableSetupColumn("##datacol", ImGuiTableColumnFlags.WidthStretch, 1f - WorldShare);

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Card("##world", new Vector2(0f, body.Y), WorldPanel.Draw);

                ImGui.TableNextColumn();
                Card("##data", new Vector2(0f, body.Y), DetailsPanel.Draw);

                ImGui.EndTable();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// One card inside the panel: a fill a shade above it, rounded, and no line anywhere.
    /// </summary>
    /// <remarks>
    /// What separates two of these is the gap between them and the step in their fill. A border
    /// around each would be a third thing saying what those two already say, and three of them
    /// nested is the look this replaced.
    /// </remarks>
    private static void Card(string id, Vector2 size, Action draw)
    {
        var stock = EditorTheme.Current.Stock;

        // Less air inside a card than a floating window keeps, because a card is already inside
        // one. The window's padding is the room round a thing standing on its own; repeating it at
        // every step inwards is how a panel ends up mostly margin.
        if (!stock) ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Air, Air));

        // The card holds a scrolling region rather than being one, so the wheel belongs to what is
        // inside it and stops there.
        var open = ImGui.BeginChild(
            id,
            size,
            stock ? ImGuiChildFlags.None : ImGuiChildFlags.AlwaysUseWindowPadding,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        if (!stock) ImGui.PopStyleVar();

        if (open) draw();

        ImGui.EndChild();
    }

    /// <summary>
    /// The button that docks the panel, over everything at the window's top right.
    /// </summary>
    /// <remarks>
    /// Its own window rather than a corner of the panel, so it stays reachable and in the same
    /// place whatever the panel is doing underneath it. Round with a picture in it, like the rest
    /// of what floats over the scene.
    /// </remarks>
    private static void DrawDock()
    {
        const float Size = 34f;

        // The same corner of the window whatever the panel is doing. It belongs to the editor
        // rather than to the panel it happens to sit over, so docking must not move it: a control
        // that jumps when it is used is one somebody has to find again every time.
        var window = ImGuiRuntime.Size;
        const float inset = 14f;

        ImGui.SetNextWindowPos(new Vector2(window.X - inset, inset), ImGuiCond.Always, new Vector2(1f, 0f));

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoBackground;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));

        if (ImGui.Begin("##dock", flags))
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
            if (Round($"dock{Docked}", Docked ? "icons/ui/close.png" : "icons/ui/pinned.png", false, Size))
            {
                Docked = !Docked;
            }

            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Docked ? "Undock the panel" : "Dock the panel");

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

    /// <summary>The bar between the world and the data, which a drag moves.</summary>
    /// <remarks>
    /// A short pill in the middle rather than a line across: it says where to take hold without
    /// drawing a border, which is the one thing this look does not do.
    /// </remarks>
    private static void Splitter(float width)
    {
        ImGui.InvisibleButton("##split", new Vector2(width, 10f));

        var held = ImGui.IsItemActive();
        var over = ImGui.IsItemHovered();

        if (over || held) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);

        if (held)
        {
            var body = ImGui.GetWindowHeight() - Header;
            if (body > 1f) WorldShare = Math.Clamp(WorldShare + (ImGui.GetIO().MouseDelta.Y / body), 0.15f, 0.85f);
        }

        var at = ImGui.GetItemRectMin();
        var to = ImGui.GetItemRectMax();

        var middle = new Vector2((at.X + to.X) * 0.5f, (at.Y + to.Y) * 0.5f);
        var grip = new Vector2(26f, 3f);

        var theme = EditorTheme.Current;

        ImGui.GetWindowDrawList().AddRectFilled(
            middle - grip,
            middle + grip,
            ImGui.GetColorU32(held
                ? EditorTheme.LiveAccent
                : EditorTheme.Alpha(EditorTheme.LiveText, over ? 0.5f : 0.22f)),
            grip.Y);
    }

    /// <summary>The panel's left edge, which a drag widens.</summary>
    private static void Handle()
    {
        var draw = ImGui.GetWindowDrawList();
        var at = ImGui.GetWindowPos();
        var height = ImGui.GetWindowHeight();

        ImGui.SetCursorScreenPos(new Vector2(at.X - 3f, at.Y));
        ImGui.InvisibleButton("##width", new Vector2(6f, height));

        if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);

        if (ImGui.IsItemActive())
        {
            PanelWidth -= ImGui.GetIO().MouseDelta.X;

            draw.AddLine(
                new Vector2(at.X, at.Y),
                new Vector2(at.X, at.Y + height),
                ImGui.GetColorU32(ImGuiCol.ButtonActive),
                2f);
        }

        ImGui.SetCursorScreenPos(at + ImGui.GetStyle().WindowPadding);
    }

    /// <summary>
    /// The strip along the bottom left, with whatever is open growing upwards out of it.
    /// </summary>
    /// <remarks>
    /// The bar is drawn under its content rather than over it, which is what a console does: the
    /// headers stay where the hand last left them and the lines rise out of the bottom of the
    /// screen. ImGui's own tab bar either way, so hovering, ordering and the mark on the one in
    /// force are its to draw.
    /// </remarks>
    private static void DrawTabs(float width, float strip, float margin)
    {
        if (Tabs.Count == 0 || width < 80f) return;

        var window = ImGuiRuntime.Size;
        var top = window.Y - margin - strip;

        ImGui.SetNextWindowPos(new Vector2(margin, top));
        ImGui.SetNextWindowSize(new Vector2(width, strip));
        ImGui.SetNextWindowBgAlpha(Docked ? 1f : EditorTheme.Current.WindowAlpha);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Chrome());

        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowRounding,
            Docked ? 0f : EditorTheme.Current.WindowRounding);

        // The strip is measured as StripPadding above and below the bar, so its own window padding
        // has to be that and no more or the bar sinks and the text clips.
        // The same gutter the panel keeps round its cards, and no more above and below the bar
        // than the strip was measured with.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowPadding,
            new Vector2(Gutter, StripPadding));

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        if (!ImGui.Begin("##tabs", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            return;
        }

        // What is open, first, in the room left above the bar.
        if (OpenTab >= 0 && OpenTab < Tabs.Count)
        {
            Grip();

            var room = ImGui.GetContentRegionAvail();

            var bar = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y;

            // A card like the ones in the panel, which is what it is: the same gap outside it and
            // the same air inside it, rather than a rectangle pushed against its own edges.
            Card("##tab", new Vector2(0f, room.Y - bar), Tabs[OpenTab].Draw);
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
            for (var index = 0; index < Tabs.Count; index++)
            {
                var open = index == OpenTab;

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
                        $"  {Tabs[index].Name}  ",
                        open ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    OpenTab = open ? -1 : index;
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
    private static void Grip()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.InvisibleButton("##height", new Vector2(MathF.Max(1f, width), 8f));

        var held = ImGui.IsItemActive();
        var over = ImGui.IsItemHovered();

        if (over || held) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);

        if (held)
        {
            // Up is taller, because the strip grows upward out of the bottom of the window.
            TabHeight = Math.Clamp(
                TabHeight - ImGui.GetIO().MouseDelta.Y,
                80f,
                MathF.Max(120f, ImGuiRuntime.Size.Y * 0.75f));
        }

        var at = ImGui.GetItemRectMin();
        var to = ImGui.GetItemRectMax();

        var middle = new Vector2((at.X + to.X) * 0.5f, (at.Y + to.Y) * 0.5f);
        var grab = new Vector2(26f, 2f);

        ImGui.GetWindowDrawList().AddRectFilled(
            middle - grab,
            middle + grab,
            ImGui.GetColorU32(held
                ? EditorTheme.LiveAccent
                : EditorTheme.Alpha(EditorTheme.LiveText, over ? 0.5f : 0.22f)),
            grab.Y);
    }

    /// <summary>
    /// The tabs as a row of pills, which is the shape the rest of this look is drawn in.
    /// </summary>
    /// <remarks>
    /// A click on the one already open closes it, which is why these are buttons rather than tabs:
    /// what a header means here is ours to say, and a tab bar has its own idea about which of its
    /// tabs is selected.
    /// </remarks>
    private static void Pills()
    {
        var draw = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight();
        var padding = ImGui.GetStyle().FramePadding.X + 6f;

        for (var index = 0; index < Tabs.Count; index++)
        {
            if (index > 0) ImGui.SameLine();

            var open = index == OpenTab;
            var name = Tabs[index].Name;
            var word = ImGui.CalcTextSize(name);
            var at = ImGui.GetCursorScreenPos();
            var size = new Vector2(word.X + (padding * 2f), height);

            ImGui.InvisibleButton($"##tab{index}", size);

            if (ImGui.IsItemClicked()) OpenTab = open ? -1 : index;

            var over = ImGui.IsItemHovered();

            // A tab is a button and wears a button's three steps: a fill of its own to be seen
            // and pressed, a brighter one under the hand, and the accent when it is the one
            // showing. Nothing at all under the two that are not open reads as two words somebody
            // has left lying on the strip.
            var fill = open
                ? EditorTheme.LiveAccent
                : over
                    ? EditorTheme.LiveHover
                    : ImGui.GetStyle().Colors[(int)ImGuiCol.Button];

            draw.AddRectFilled(at, at + size, ImGui.GetColorU32(fill), height * 0.5f);

            // White whether it is open or not. What says which one is showing is the pill under it,
            // and a grey word reads as a tab that cannot be pressed.
            draw.AddText(
                at + new Vector2(padding, (height - word.Y) * 0.5f),
                ImGui.GetColorU32(EditorTheme.LiveText),
                name);
        }
    }

    /// <summary>
    /// What floats in the scene's corners.
    /// </summary>
    /// <remarks>
    /// Not a bar across the top: a bar would take a strip of the scene permanently, and these take
    /// only what they cover. Each group is pinned to a corner of whatever the scene has been left,
    /// so they follow it as the panel is docked or dragged.
    /// </remarks>
    private static void DrawToolbars(BehaviorContext ctx)
    {
        Group(ctx, ToolbarSlot.Left, new Vector2(0f, 0f), new Vector2(0f, 0f));
        Group(ctx, ToolbarSlot.Centre, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        Group(ctx, ToolbarSlot.Right, new Vector2(1f, 0f), new Vector2(1f, 0f));
        Group(ctx, ToolbarSlot.BottomRight, new Vector2(1f, 1f), new Vector2(1f, 1f));

        // Down the left edge rather than across the top, for the groups that are a list of modes.
        Group(ctx, ToolbarSlot.LeftEdge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), down: true);
    }

    /// <summary>One corner's worth of buttons, in a row.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="slot">Which corner.</param>
    /// <param name="corner">Which corner of the scene it hangs from, as a fraction.</param>
    /// <param name="pivot">Which corner of the group meets it.</param>
    /// <param name="down">Whether the buttons stack downwards rather than running across.</param>
    private static void Group(
        BehaviorContext ctx,
        ToolbarSlot slot,
        Vector2 corner,
        Vector2 pivot,
        bool down = false)
    {
        var buttons = EditorToolbar.Slot(slot);
        if (buttons.Count == 0) return;

        var inset = Margin + 4f;

        var at = new Vector2(
            Scene.X + (Scene.Width * corner.X) + (corner.X > 0.5f ? -inset : corner.X > 0f ? 0f : inset),
            Scene.Y + (Scene.Height * corner.Y) + (corner.Y > 0.5f ? -inset : inset));

        ImGui.SetNextWindowPos(at, ImGuiCond.Always, pivot);

        // No panel behind them. A group of buttons floating over the scene is a group of buttons,
        // and a plate under them is a second thing to look at that says nothing.
        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoBackground;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));

        if (!ImGui.Begin($"##bar{slot}", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar();
            return;
        }

        // Round enough that a square button is a circle, which is what a button with a picture in
        // it and no words wants to be.
        const float Size = 34f;

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Size * 0.5f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 6f));

        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];

            if (index > 0 && !down) ImGui.SameLine();

            var on = button.Active?.Invoke() == true;
            var theme = EditorTheme.Current;

            // Nothing behind a button that is not in force or under the hand: the picture is the
            // button. What is in force wears the accent, which is the one thing colour means.
            ImGui.PushStyleColor(
                ImGuiCol.Button,
                on ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveCard, theme.PanelAlpha));

            ImGui.PushStyleColor(
                ImGuiCol.ButtonHovered,
                on ? EditorTheme.Alpha(EditorTheme.LiveAccent, 0.85f) : EditorTheme.LiveHover);

            ImGui.PushStyleColor(
                ImGuiCol.ButtonActive,
                on ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveHover, 1f));

            var label = button.Label();

            var pressed = button.Icon is { Length: > 0 } icon && label.Length == 0
                ? Round($"{slot}{index}", icon, on, Size)
                : ImGui.Button(
                    $"  {(label.Length == 0 ? Icon(button.Icon) : label)}  ##{slot}{index}",
                    new Vector2(0f, Size));

            if (pressed) button.Run(ctx.Ecs);

            ImGui.PopStyleColor(3);
        }

        ImGui.PopStyleVar(3);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    /// <summary>
    /// A round button with a picture in it, and nothing else.
    /// </summary>
    /// <remarks>
    /// Drawn rather than asked for. ImGui rounds an image button by the smaller of its padding and
    /// the style's rounding, so a circle would need padding half the button wide, which is padding
    /// around nothing. A circle and a picture over it is what was wanted and what this draws.
    /// </remarks>
    private static bool Round(string id, string icon, bool on, float size)
    {
        var at = ImGui.GetCursorScreenPos();

        ImGui.InvisibleButton($"##{id}", new Vector2(size, size));

        var pressed = ImGui.IsItemClicked();
        var over = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        // Out of the colours the caller pushed, which is what a button of ImGui's own would use:
        // reading the palette directly here instead would quietly ignore them.
        var fill = held
            ? ImGuiCol.ButtonActive
            : over
                ? ImGuiCol.ButtonHovered
                : ImGuiCol.Button;

        var draw = ImGui.GetWindowDrawList();
        var middle = at + new Vector2(size * 0.5f, size * 0.5f);

        draw.AddCircleFilled(middle, size * 0.5f, ImGui.GetColorU32(fill));

        const float Mark = 18f;

        if (ImGuiTextures.Load(icon) is var picture && picture != 0)
        {
            draw.AddImage(
                (IntPtr)picture,
                middle - new Vector2(Mark * 0.5f, Mark * 0.5f),
                middle + new Vector2(Mark * 0.5f, Mark * 0.5f),
                Vector2.Zero,
                Vector2.One,
                ImGui.GetColorU32(EditorTheme.IconTint(on)));
        }

        return pressed;
    }

    /// <summary>A word standing in for a picture, until the icons are loaded.</summary>
    private static string Icon(string? path)
    {
        if (path is not { Length: > 0 }) return "?";

        var name = Path.GetFileNameWithoutExtension(path);
        return name.Length == 0 ? "?" : char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Whatever branch of the menu was asked for, where it was asked for.
    /// </summary>
    /// <remarks>
    /// Opened once, when it is asked for. Opening it whenever it is not open is how a menu becomes
    /// one that cannot be dismissed: the click that closes it is followed by a frame that finds it
    /// closed and opens it again.
    /// </remarks>
    private static void DrawMenu(BehaviorContext ctx)
    {
        if (_menu is null) return;

        const string Name = "##menu";

        if (_opening)
        {
            _opening = false;

            ImGui.SetNextWindowPos(MenuAt);
            ImGui.OpenPopup(Name);
        }

        if (!ImGui.BeginPopup(Name))
        {
            // Dismissed by a click somewhere else, which is what a menu is for.
            _menu = null;
            return;
        }

        Branch(ctx, _menu);

        ImGui.EndPopup();
    }

    /// <summary>One level of the menu, with a submenu per branch under it.</summary>
    private static void Branch(BehaviorContext ctx, string path) => RoundedRows.Menu(() =>
    {
        foreach (var item in EditorMenu.Level(path))
        {
            switch (item.Kind)
            {
                case MenuKind.Separator:
                    EditorTheme.Divide();
                    continue;

                case MenuKind.Submenu:
                    var opened = ImGui.BeginMenu(item.Label);

                    RoundedRows.Row(opened);

                    if (opened)
                    {
                        Branch(ctx, item.Path);
                        ImGui.EndMenu();
                    }

                    break;

                case MenuKind.Toggle:
                    var ticked = item.Checked?.Invoke() == true;

                    if (ImGui.MenuItem(item.Label, string.Empty, ticked)) item.Run?.Invoke(ctx.Ecs);

                    RoundedRows.Row();

                    break;

                default:
                    if (ImGui.MenuItem(item.Label, string.Empty, false, item.Enabled?.Invoke() != false))
                    {
                        item.Run?.Invoke(ctx.Ecs);
                    }

                    RoundedRows.Row();

                    break;
            }
        }
    });


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

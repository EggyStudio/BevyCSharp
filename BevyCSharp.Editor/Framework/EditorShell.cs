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

    private const float TabStrip = 30f;
    private const float Header = 28f;

    /// <summary>Whether the panel is against the window's edge, with the scene beside it.</summary>
    public static bool Docked { get; set; }

    /// <summary>How wide the panel is, in logical pixels.</summary>
    public static float PanelWidth { get; set; } = 560f;

    /// <summary>How much of the panel the world gets, the data taking the rest.</summary>
    public static float WorldShare { get; set; } = 0.45f;

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

    /// <summary>Shows a branch of the editor's menu at a point.</summary>
    /// <remarks>
    /// A menu is a popup, and a popup belongs to the frame it is opened in, so what is asked for
    /// here is remembered and put up on the next pass through the interface.
    /// </remarks>
    public static void ShowMenu(string branch, float x, float y)
    {
        _menu = branch ?? string.Empty;
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

        var window = ImGuiRuntime.Size;
        var margin = Docked ? 0f : Margin;

        PanelWidth = Math.Clamp(PanelWidth, 320f, Math.Max(360f, window.X - 320f));

        var panelX = window.X - margin - PanelWidth;
        Panel = (panelX, margin, PanelWidth, window.Y - (margin * 2f));

        var strip = TabStrip + (OpenTab >= 0 ? TabHeight + 1f : 0f);
        var tabsWidth = panelX - margin - (Docked ? 0f : Margin);

        // Docked, the scene keeps the top left corner and the tabs sit under it. Floating, the
        // scene is the whole window and everything else is over it.
        Scene = Docked
            ? (0f, 0f, Math.Max(1f, panelX), Math.Max(1f, window.Y - strip))
            : (0f, 0f, window.X, window.Y);

        DrawPanel();
        DrawTabs(tabsWidth, strip, margin);
        DrawToolbars(ctx);
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

    /// <summary>The world beside the data, or above it when there is no room for two columns.</summary>
    private static void DrawPanel()
    {
        ImGui.SetNextWindowPos(new Vector2(Panel.X, Panel.Y));
        ImGui.SetNextWindowSize(new Vector2(Panel.Width, Panel.Height));

        // Seen through, so the scene is behind the panel rather than cut off by it.
        ImGui.SetNextWindowBgAlpha(EditorTheme.Current.PanelAlpha);

        // Square against the window's edge when it is docked. A rounded corner is what says a
        // thing is floating, and a docked panel is not.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowRounding,
            Docked ? 0f : EditorTheme.Current.WindowRounding);

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoScrollbar;

        if (!ImGui.Begin("##panel", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar();
            return;
        }

        Handle();
        Chrome();

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
        ImGui.PopStyleVar();
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
        if (ImGui.BeginChild(id, size, ImGuiChildFlags.None)) draw();

        ImGui.EndChild();
    }

    /// <summary>The panel's own top row: what it is, and the button that docks it.</summary>
    /// <remarks>
    /// No line under it. The gap to what follows says the same thing, and a line across a panel
    /// that has no border anywhere else is the one rule the rest of the look does not keep.
    /// </remarks>
    private static void Chrome()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(Docked ? "DOCKED" : "EDITOR");

        var label = Docked ? "Undock" : "Dock";
        var width = ImGui.CalcTextSize(label).X + (ImGui.GetStyle().FramePadding.X * 2f);

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - width + ImGui.GetCursorPosX());

        if (ImGui.SmallButton(label)) Docked = !Docked;

        ImGui.Spacing();
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
            ImGui.GetColorU32(held ? theme.Accent : EditorTheme.Alpha(theme.Text, over ? 0.5f : 0.22f)),
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
        ImGui.SetNextWindowBgAlpha(EditorTheme.Current.PanelAlpha);

        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowRounding,
            Docked ? 0f : EditorTheme.Current.WindowRounding);

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoScrollbar;

        if (!ImGui.Begin("##tabs", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar();
            return;
        }

        // What is open, first, in the room left above the bar.
        if (OpenTab >= 0 && OpenTab < Tabs.Count)
        {
            var room = ImGui.GetContentRegionAvail();

            if (ImGui.BeginChild("##tab", new Vector2(0f, room.Y - TabStrip)))
            {
                Tabs[OpenTab].Draw();
            }

            ImGui.EndChild();
        }

        // And the bar under it. A header is a button as far as ImGui is concerned here, because
        // what a click means is ours: the one already open closes rather than staying open.
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
        ImGui.PopStyleVar();
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
        ImGui.SetNextWindowBgAlpha(EditorTheme.Current.PanelAlpha);

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing;

        if (!ImGui.Begin($"##bar{slot}", flags))
        {
            ImGui.End();
            return;
        }

        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];

            if (index > 0 && !down) ImGui.SameLine();

            var on = button.Active?.Invoke() == true;
            if (on) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));

            var label = button.Label();
            var pressed = button.Icon is { Length: > 0 } icon && label.Length == 0
                ? ImGuiTextures.Button($"{slot}{index}", icon, 16f, EditorTheme.IconTint(on))
                : ImGui.Button($"{(label.Length == 0 ? Icon(button.Icon) : label)}##{slot}{index}");

            if (pressed) button.Run(ctx.Ecs);

            if (on) ImGui.PopStyleColor();
        }

        // The bottom right corner also holds the orientation cross, which is a thing in the scene
        // drawn where the interface says. Where that is, is here.
        if (slot == ToolbarSlot.BottomRight)
        {
            const float Square = 56f;

            ImGui.SameLine();
            ImGui.InvisibleButton("##cross", new Vector2(Square, Square));

            var box = ImGui.GetItemRectMin();
            EditorGizmoSlot.Report(new Vector2(box.X, box.Y) + new Vector2(Square * 0.5f), Square, Frame);
        }

        ImGui.End();
    }

    /// <summary>A word standing in for a picture, until the icons are loaded.</summary>
    private static string Icon(string? path)
    {
        if (path is not { Length: > 0 }) return "?";

        var name = Path.GetFileNameWithoutExtension(path);
        return name.Length == 0 ? "?" : char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>Whatever branch of the menu was asked for, where it was asked for.</summary>
    private static void DrawMenu(BehaviorContext ctx)
    {
        if (_menu is null) return;

        const string Name = "##menu";

        if (!ImGui.IsPopupOpen(Name))
        {
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
    private static void Branch(BehaviorContext ctx, string path)
    {
        foreach (var item in EditorMenu.Level(path))
        {
            switch (item.Kind)
            {
                case MenuKind.Separator:
                    ImGui.Separator();
                    break;

                case MenuKind.Submenu:
                    if (ImGui.BeginMenu(item.Label))
                    {
                        Branch(ctx, item.Path);
                        ImGui.EndMenu();
                    }

                    break;

                case MenuKind.Toggle:
                    if (ImGui.MenuItem(item.Label, string.Empty, item.Checked?.Invoke() == true))
                    {
                        item.Run?.Invoke(ctx.Ecs);
                    }

                    break;

                default:
                    if (ImGui.MenuItem(item.Label, string.Empty, false, item.Enabled?.Invoke() != false))
                    {
                        item.Run?.Invoke(ctx.Ecs);
                    }

                    break;
            }
        }
    }

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

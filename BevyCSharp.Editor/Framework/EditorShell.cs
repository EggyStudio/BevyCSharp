using Bevy;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Holds the open panels and hands the interface's reports to them.
/// </summary>
/// <remarks>
/// <para>
/// A window manager rather than a layout. It has no opinion about menubars, docking or where
/// anything sits: a panel is a document, a document positions itself in CSS, and the shell only
/// decides which are open and who hears about a click. That is what makes a different editor a
/// different set of panels rather than a fork of this file.
/// </para>
/// <para>
/// Registration is by instance rather than by type, so a panel can be constructed with whatever
/// it needs to bind against. Two panels of the same type can be open at once, as long as their
/// documents do not use the same CSS ids.
/// </para>
/// </remarks>
public static class EditorShell
{
    private static readonly List<IEditorPanel> Panels = [];

    /// <summary>Panels opened during this tick, which nothing may dismiss yet.</summary>
    private static readonly HashSet<IEditorPanel> Fresh = [];

    /// <summary>
    /// Panels that are loaded but not on screen.
    /// </summary>
    /// <remarks>
    /// The difference between putting a panel away and closing it. Closing one takes its document
    /// out of the interface's list, and changing that list respawns every widget of every panel:
    /// a blink, a frame of the wrong font, and a panel that sometimes does not come back. So
    /// anything that is put away and fetched again — the tabs, chiefly — is concealed instead. The
    /// document stays where it was, the panel keeps reading it, and all that changes is whether it
    /// is drawn and whether the layout makes room for it.
    /// </remarks>
    private static readonly HashSet<IEditorPanel> Concealed = [];

    /// <summary>The panel being dragged, and where the cursor grabbed it.</summary>
    private static (IEditorPanel Panel, float OffsetX, float OffsetY)? _drag;

    /// <summary>The panels currently registered, open or not.</summary>
    public static IReadOnlyList<IEditorPanel> Open => Panels;

    /// <summary>
    /// Where the panels are.
    /// </summary>
    /// <remarks>
    /// A table rather than a set of rules, so the arrangement of the editor is something that can
    /// be written down, saved, restored and changed by dragging a window. That is the difference
    /// between a shell that can be rearranged and one that has an arrangement.
    /// </remarks>
    public static EditorLayout Layout { get; } = new();

    /// <summary>
    /// The frame currently being driven, for panels that read the world.
    /// </summary>
    /// <remarks>
    /// Set at the top of every tick and valid for the whole of it. Null outside a tick, which is
    /// the honest answer: the world is only loaned to a running system.
    /// </remarks>
    public static BehaviorContext? Context { get; private set; }

    /// <summary>The world this frame, for a panel that reads or writes it.</summary>
    /// <exception cref="InvalidOperationException">Asked for outside a tick.</exception>
    public static EcsWorld Ecs =>
        Context?.Ecs ?? throw new InvalidOperationException(
            "The world is only reachable while the shell is ticking, because that is when Bevy "
            + "has loaned it out. Read it from a panel's Pull, Push, Changed or command method.");

    /// <summary>Adds a panel and opens its document.</summary>
    public static T Show<T>(T panel) where T : IEditorPanel
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.Open();
        Panels.Add(panel);
        Fresh.Add(panel);
        Rebuilding();
        return panel;
    }

    /// <summary>
    /// Opens a panel at a point, which is how a flyout is shown.
    /// </summary>
    /// <remarks>
    /// The point is usually where the thing that opened it is: a dropdown under its button, a
    /// context menu under the cursor. Placing it here rather than in the panel's own declaration
    /// is what makes one flyout class usable from everywhere.
    /// </remarks>
    public static T ShowAt<T>(T panel, float x, float y) where T : IEditorPanel
    {
        ArgumentNullException.ThrowIfNull(panel);

        Show(panel);
        Layout.Place(panel, panel.Chrome.Placement.MovedTo(x, y));
        return panel;
    }

    /// <summary>
    /// Opens a menu at a point, closing whichever was open.
    /// </summary>
    /// <remarks>
    /// One menu at a time, because a menu is a question and two of them at once is two questions
    /// with one pointer to answer them.
    /// </remarks>
    public static MenuPanel ShowMenu(string path, float x, float y, string? title = null)
    {
        CloseMenus();

        var (clearX, clearY) = Clear(x, y);
        return ShowAt(new MenuPanel(path, title), clearX, clearY);
    }

    /// <summary>Opens a menu over a list built for the occasion, such as an enum's values.</summary>
    public static MenuPanel ShowMenu(string title, IReadOnlyList<MenuItem> items, float x, float y)
    {
        CloseMenus();

        var (clearX, clearY) = Clear(x, y);
        return ShowAt(new MenuPanel(title, items), clearX, clearY);
    }

    /// <summary>
    /// The nearest point to <paramref name="x"/> that is not over a panel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A menu opened over a panel is unreadable: the panel's text draws through it whatever it is
    /// told about layering, which is the interface's doing and not something this side can fix. So
    /// a menu steps out from under the panel it was opened from, to its right where there is room
    /// and to its left where there is not, which is where a menu belongs anyway.
    /// </para>
    /// <para>
    /// Only out from under the panels that hold reading: the columns and the open tab. The strip
    /// and the corner buttons are furniture, a menu drawn over them is perfectly readable, and a
    /// menu that stepped out of a strip running the width of the window would have to leave the
    /// window to do it. What keeps it on screen instead is the clamp in the arrangement, which
    /// works on what the menu measured rather than on a guess at how big it will be.
    /// </para>
    /// </remarks>
    private static (float X, float Y) Clear(float x, float y)
    {
        const float MenuWidth = 216f;
        const float Gap = 8f;

        var (windowWidth, _) = Window.Size();

        // A handful of steps, since a point can be over two panels stacked in a column and a menu
        // stepping out of one may land on another.
        for (var step = 0; step < 4; step++)
        {
            var moved = false;

            foreach (var panel in Panels)
            {
                if (panel is MenuPanel) continue;
                if (Concealed.Contains(panel)) continue;
                if (!Obscures(Layout.PlacementOf(panel).Dock)) continue;
                if (panel.Window?.Measure() is not { } rect) continue;
                if (!rect.Contains(x + 4f, y + 4f) && !rect.Contains(x + MenuWidth - 4f, y + 4f))
                    continue;

                x = rect.Right + Gap;
                if (x + MenuWidth > windowWidth) x = MathF.Max(Gap, rect.X - MenuWidth - Gap);

                moved = true;
                break;
            }

            if (!moved) break;
        }

        return (x, y);
    }

    /// <summary>Whether a panel in this dock is something a menu has to step out from under.</summary>
    private static bool Obscures(EditorDock dock) =>
        dock is EditorDock.Left or EditorDock.Right or EditorDock.Bottom;

    /// <summary>
    /// Keeps a panel that would otherwise dismiss itself, or lets it go again.
    /// </summary>
    /// <remarks>
    /// What pinning is. A panel declares how it dismisses before it opens, and this is the one
    /// thing about that which a person changes while it is open, so it is a set the shell holds
    /// rather than a property of the declaration.
    /// </remarks>
    public static void Pin(IEditorPanel panel, bool pinned)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (pinned) Pinned.Add(panel);
        else Pinned.Remove(panel);
    }

    /// <summary>Panels that have been pinned, and so no longer dismiss on an outside press.</summary>
    private static readonly HashSet<IEditorPanel> Pinned = [];

    /// <summary>Closes any menu that is open.</summary>
    public static void CloseMenus()
    {
        foreach (var panel in Panels.OfType<MenuPanel>().ToArray()) Hide(panel);
    }

    /// <summary>
    /// Whether a point is over any open panel.
    /// </summary>
    /// <remarks>
    /// What keeps the wheel from being two things at once. The pointer is over the viewport most
    /// of the time and over a panel some of the time, and the same roll should move the camera in
    /// one case and scroll a list in the other.
    /// </remarks>
    public static bool PointerOverPanel(float x, float y)
    {
        foreach (var panel in Panels)
        {
            if (Concealed.Contains(panel)) continue;
            if (panel.Window?.Covers(x, y) == true) return true;
        }

        return false;
    }

    /// <summary>The panels that are on screen, in the order they were opened.</summary>
    private static List<IEditorPanel> Showing()
    {
        var showing = new List<IEditorPanel>(Panels.Count);

        foreach (var panel in Panels)
        {
            if (!Concealed.Contains(panel)) showing.Add(panel);
        }

        return showing;
    }

    /// <summary>The open panel of this type, or <see langword="null"/>.</summary>
    public static T? Find<T>() where T : class, IEditorPanel =>
        Panels.OfType<T>().FirstOrDefault();

    /// <summary>Whether a panel is loaded and on screen, rather than loaded and put away.</summary>
    public static bool IsShowing(IEditorPanel panel) =>
        Panels.Contains(panel) && !Concealed.Contains(panel);

    /// <summary>Takes a panel off the screen without unloading it.</summary>
    public static void Conceal(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (!Panels.Contains(panel)) return;
        if (!Concealed.Add(panel)) return;

        panel.Window?.Show(false);

        if (_drag?.Panel == panel) _drag = null;
    }

    /// <summary>Puts a concealed panel back on the screen.</summary>
    public static void Reveal(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (!Concealed.Remove(panel)) return;

        panel.Window?.Show(true);
    }

    /// <summary>Opens a panel if it is closed and closes it if it is open.</summary>
    /// <remarks>What a toolbar button bound to a panel does, which is most of them.</remarks>
    public static void Toggle<T>(Func<T> create) where T : class, IEditorPanel
    {
        ArgumentNullException.ThrowIfNull(create);

        if (Find<T>() is { } existing)
        {
            Hide(existing);
            return;
        }

        Show(create());
    }

    /// <summary>
    /// Puts a panel away if it is showing and fetches it back if it is not.
    /// </summary>
    /// <remarks>
    /// What the panels a person opens and closes all day want, rather than <see cref="Toggle"/>:
    /// the document is loaded once and stays, so no amount of showing and hiding disturbs anything
    /// else on screen. <see cref="Toggle"/> remains what a panel that is genuinely finished with
    /// wants, and what a flyout wants.
    /// </remarks>
    public static void ToggleShown<T>(Func<T> create) where T : class, IEditorPanel
    {
        ArgumentNullException.ThrowIfNull(create);

        if (Find<T>() is { } existing)
        {
            if (IsShowing(existing)) Conceal(existing);
            else Reveal(existing);

            return;
        }

        Show(create());
    }

    /// <summary>The panel of this type that is on screen, or <see langword="null"/>.</summary>
    public static T? Showing<T>() where T : class, IEditorPanel =>
        Find<T>() is { } panel && IsShowing(panel) ? panel : null;

    /// <summary>Closes a panel and forgets it.</summary>
    public static void Hide(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.Close();
        Panels.Remove(panel);
        Fresh.Remove(panel);
        Concealed.Remove(panel);
        Pinned.Remove(panel);
        EditorTabs.Closed(panel);
        Rebuilding();

        if (_drag?.Panel == panel) _drag = null;
    }

    /// <summary>Closes everything.</summary>
    public static void HideAll()
    {
        foreach (var panel in Panels) panel.Close();
        Panels.Clear();
        Fresh.Clear();
        Concealed.Clear();
        _drag = null;
    }

    /// <summary>
    /// Drives every open panel for one frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order matters. What the person did is applied first, then the panels are given their
    /// chance to write back, so a value a click changed is on screen the same frame rather than
    /// the next one. Doing it the other way round would overwrite the edit with the old value
    /// before anything had read it.
    /// </para>
    /// <para>
    /// An event is offered to each panel until one claims it, since an element belongs to exactly
    /// one document.
    /// </para>
    /// </remarks>
    public static void Tick(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (!App.HasEditor) return;

        // Held for the panels rather than passed to them. A panel's methods are called by
        // generated code that knows nothing about the engine, and every panel worth having needs
        // to read the world, so the frame's context is put somewhere they can reach it.
        Context = ctx;

        var input = ctx.Input;

        // Before anything reads a right click, because whether the pointer travelled is what tells
        // a menu from a camera look, and everything that acts on a right click has to agree.
        TrackRightDrag(input);

        // Which panels took an edit this frame. A drag reports every frame it moves, and a panel
        // that applies its values to the engine should do that once a frame rather than once per
        // binding that happened to change in it.
        var edited = new HashSet<IEditorPanel>();

        foreach (var report in Xui.Drain())
        {
            switch (report.Kind)
            {
                case UiEventKind.Change:
                    foreach (var panel in Panels)
                    {
                        if (!panel.Push(report.Element)) continue;

                        edited.Add(panel);
                        break;
                    }

                    break;

                case UiEventKind.Click:
                    foreach (var panel in Panels.ToArray())
                        if (panel.Invoke(report.Element)) break;
                    break;

                case UiEventKind.Context:
                    // Not after a look. The right button steers the camera as well as asking for a
                    // menu, and the interface reports its own right clicks knowing nothing about
                    // that: a drag that began on a panel and ended somewhere else is the camera
                    // having been turned, and no menu was ever meant by it.
                    if (RightDragged) break;

                    // Offered to the panels first: a row with a menu of its own answers, and an
                    // element with none falls through to the viewport's, which is what a right
                    // click on the background should get.
                    var claimed = false;
                    foreach (var panel in Panels.ToArray())
                    {
                        if (!panel.Context(report.Element)) continue;

                        claimed = true;
                        break;
                    }

                    if (!claimed) _contextWanted = true;
                    break;

                case UiEventKind.Reloading:
                    foreach (var panel in Panels) panel.Window?.Suspend();
                    PanelBinding.Forget();
                    break;

                case UiEventKind.Reloaded:
                    // Everything was respawned, so every kept entity is stale. The values the
                    // panels hold are not: the pull below puts them straight back onto the new
                    // elements, which is what makes editing a document while it is open feel
                    // like editing the document rather than restarting the editor.
                    foreach (var panel in Panels) panel.Window?.Resume();
                    break;
            }
        }

        foreach (var panel in edited) panel.Changed();

        // Pointer work comes after the widgets have had their say, because a click that opened a
        // flyout is one of the events just drained and the flyout must not be dismissed by the
        // same press that opened it.
        Drag(input);
        Resize(input);
        Dismiss(input);
        Menus(input);

        // A click on a mesh selects it, which is the other half of what the hierarchy does. The
        // shell does this rather than a panel, because selection belongs to the editor and not
        // to whichever panel happens to be open.
        var picked = Picking.Drain();
        foreach (var entity in picked) EditorSelection.Select(entity);

        Deselect(input, picked.Length > 0, ctx.Time.FrameCount);

        // A selection whose entity is gone is worse than none: the inspector would read whatever
        // took its place in storage. Checked once here rather than in every panel that reads it.
        EditorSelection.Prune(ctx.Ecs);

        // Asked once a frame, and used by every text binding: whatever is being typed in is left
        // alone rather than overwritten with what the program still says.
        PanelBinding.Focused = Focus(input);
        PanelBinding.Frame = ctx.Time.FrameCount;
        EditorWindow.Frame = ctx.Time.FrameCount;

        // One call for every window, since the answer is about the interface rather than about any
        // one of them.
        EditorWindow.Generation = Xui.Generation;

        // Read the world first, arrange second, write the screen third. A panel that filled its
        // rows during this tick is one whose height changed, and the arrangement has to see that
        // before it stacks anything under it.
        foreach (var panel in Panels)
        {
            if (Concealed.Contains(panel)) continue;

            panel.Refresh();
        }

        // A concealed panel is put out of sight every frame rather than once, because the
        // interface restyles a widget whenever anything is written to it and a restyle puts its
        // display back to whatever the stylesheet said.
        foreach (var panel in Concealed) panel.Window?.Show(false);

        Layout.Arrange(Showing());

        foreach (var panel in Panels) panel.Pull();

        Fresh.Clear();
    }

    /// <summary>
    /// What the keyboard is going to, and letting go of a field that should not still have it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A widget takes the keyboard when it is clicked and gives it up when another widget is
    /// clicked. A click on the scene is not a click on a widget, so without this a person who
    /// types in the search box and goes back to the viewport leaves the box holding the keyboard,
    /// and every key the editor binds is a letter going into a box nobody is looking at.
    /// </para>
    /// <para>
    /// A focused element no panel owns is not believed at all. That is what a rebuild leaves
    /// behind: a widget that is about to be replaced, still saying it has the keyboard, with
    /// nothing on this side able to click anything else to take it away.
    /// </para>
    /// </remarks>
    private static Entity Focus(Input input)
    {
        var focused = Xui.Focused();
        if (focused.IsNone) return Entity.None;

        var owned = false;
        foreach (var panel in Panels)
        {
            if (panel.Window?.Owns(focused) != true) continue;

            owned = true;
            break;
        }

        if (!owned)
        {
            Xui.Blur();
            return Entity.None;
        }

        if (!input.MousePressed(MouseButton.Left)) return focused;

        var (x, y) = input.MousePosition;
        if (Xui.TryRect(focused, out var rect) && rect.Contains(x, y)) return focused;

        Xui.Blur();
        return Entity.None;
    }

    /// <summary>Where a press on the scene began, while the button is still down.</summary>
    private static (float X, float Y)? _pressedOnScene;

    /// <summary>Whether anything was picked since that press.</summary>
    private static bool _pickedSincePress;

    /// <summary>
    /// Lets go of the selection when the scene is clicked and nothing is under the pointer.
    /// </summary>
    /// <remarks>
    /// Choosing nothing is a choice. Without this the only way out of a selection is to pick
    /// something else, and the panel describing it stays open over a scene the person has finished
    /// with. A press and a release in the same place, on the scene, with no mesh reported between
    /// them: a drag is the camera or a handle and says nothing about what is selected.
    /// </remarks>
    private static void Deselect(Input input, bool hitSomething, ulong frame)
    {
        var (x, y) = input.MousePosition;

        if (input.MousePressed(MouseButton.Left))
        {
            _pressedOnScene = PointerOverPanel(x, y) || !Layout.Viewport.Contains(x, y)
                ? null
                : (x, y);

            _pickedSincePress = false;
        }

        // Anywhere between the press and the release. Which of the two frames a pick is reported
        // on is the interface's business and has been both, and a selection that survives only one
        // of the two answers is a selection that clears itself the moment it is made.
        _pickedSincePress |= hitSomething;

        if (!input.MouseReleased(MouseButton.Left)) return;

        var from = _pressedOnScene;
        var hit = _pickedSincePress;

        _pressedOnScene = null;
        _pickedSincePress = false;

        if (hit || from is not { } start) return;

        // A handle being dragged is a transform, however far the pointer moved: letting go of it
        // over empty space would drop the thing that was just being moved.
        if (Behaviors.TransformGizmo.DraggingOn(frame)) return;

        const float Slop = 4f;
        if (MathF.Abs(x - start.X) + MathF.Abs(y - start.Y) > Slop) return;

        EditorSelection.Clear();
        EditorAssets.Select(null);
    }

    /// <summary>
    /// Stops every panel reading its elements until the interface has been rebuilt.
    /// </summary>
    /// <remarks>
    /// Opening or closing a document rebuilds all of them, not only the one that changed: the
    /// interface keeps one list of what is showing, and changing that list respawns every widget
    /// on screen. So every element every panel holds is about to be a dead entity, and the panels
    /// stop reading until the rebuild reports itself finished.
    /// </remarks>
    private static void Rebuilding()
    {
        foreach (var panel in Panels) panel.Window?.Suspend();

        PanelBinding.Forget();
    }

    /// <summary>
    /// Moves a window while it is being dragged by its handle.
    /// </summary>
    /// <remarks>
    /// A drag writes the layout rather than the window: the panel is moved by giving it a
    /// placement of its own, which the arrangement then applies like any other. So a window
    /// dragged out of a region stays where it was put, and saving the layout saves where it was
    /// dragged to, without any of that being a separate mechanism.
    /// </remarks>
    private static void Drag(Input input)
    {
        var (x, y) = input.MousePosition;

        if (input.MouseReleased(MouseButton.Left)) _drag = null;

        if (_drag is null && input.MousePressed(MouseButton.Left))
        {
            // Front to back, so the window on top takes the drag rather than one under it.
            for (var i = Panels.Count - 1; i >= 0; i--)
            {
                var panel = Panels[i];
                if (panel.Window is not { IsOpen: true } window) continue;
                if (window.Handle.IsNone) continue;
                if (!Xui.TryRect(window.Handle, out var handle)) continue;
                if (!handle.Contains(x, y)) continue;
                if (window.Measure() is not { } rect) continue;

                _drag = (panel, x - rect.X, y - rect.Y);
                break;
            }
        }

        if (_drag is not { } drag) return;

        if (!input.MouseDown(MouseButton.Left))
        {
            _drag = null;
            return;
        }

        var placement = Layout.PlacementOf(drag.Panel);
        Layout.Place(drag.Panel, placement.MovedTo(x - drag.OffsetX, y - drag.OffsetY));
    }

    /// <summary>What is being dragged to resize, or none.</summary>
    private enum Edge
    {
        None,
        Left,
        Right,
        Band,
    }

    /// <summary>Which edge a drag has hold of.</summary>
    private static Edge _edge;

    /// <summary>How near an edge the pointer has to be to take hold of it.</summary>
    private const float Reach = 5f;

    /// <summary>
    /// Drags the edge of a column or of the tab band.
    /// </summary>
    /// <remarks>
    /// The edges are not elements: a panel is a document and the gap between two of them belongs
    /// to nothing. So the pointer is tested against where the layout says the edges are, which is
    /// also what keeps the three numbers being dragged the same three the layout arranges from.
    /// </remarks>
    private static void Resize(Input input)
    {
        var (x, y) = input.MousePosition;

        if (input.MouseReleased(MouseButton.Left)) _edge = Edge.None;

        if (input.MousePressed(MouseButton.Left) && _edge == Edge.None && !_drag.HasValue)
        {
            var layout = Layout;

            if (MathF.Abs(x - layout.LeftEdge) < Reach && layout.LeftEdge > 1f) _edge = Edge.Left;
            else if (MathF.Abs(x - layout.RightEdge) < Reach && layout.RightEdge > 1f) _edge = Edge.Right;
            else if (layout.BandOpen && MathF.Abs(y - layout.BottomEdge) < Reach) _edge = Edge.Band;
        }

        if (_edge == Edge.None || !input.MouseDown(MouseButton.Left)) return;

        var (windowWidth, windowHeight) = Window.Size();

        switch (_edge)
        {
            case Edge.Left:
                Layout.LeftWidth = x - (Layout.Margin * 2f);
                break;

            case Edge.Right:
                Layout.RightWidth = windowWidth - x - (Layout.Margin * 2f);
                break;

            case Edge.Band:
                Layout.BottomHeight = windowHeight - y - StripHeight();
                break;
        }
    }

    /// <summary>How tall the tab strip is, which the band sits on top of.</summary>
    private static float StripHeight()
    {
        foreach (var panel in Panels)
        {
            if (panel.Chrome.Placement.Dock != EditorDock.Strip) continue;
            if (panel.Window?.Measure() is { } rect) return rect.Height;
        }

        return 0f;
    }

    /// <summary>
    /// Opens the viewport's own menu when a right click landed on nothing.
    /// </summary>
    /// <remarks>
    /// A right click is also how the camera is steered, so the gesture has to be told apart from
    /// a look: a press and a release in nearly the same place is a click, and anything further is
    /// the camera having been turned. The interface reports its own right clicks through the
    /// event queue, and this is only what is left over.
    /// </remarks>
    private static void Menus(Input input)
    {
        var (x, y) = input.MousePosition;

        var wanted = _contextWanted;
        _contextWanted = false;

        if (RightDragged) return;

        if (input.MouseReleased(MouseButton.Right) && !PointerOverPanel(x, y)) wanted = true;

        if (!wanted) return;

        ViewportMenu?.Invoke(x, y);
    }

    /// <summary>
    /// Watches how far the pointer travels while the right button is held.
    /// </summary>
    /// <remarks>
    /// While the camera is being steered the cursor is locked, so where it is does not change
    /// however far it is dragged. How far the mouse itself moved is the only thing that tells a
    /// look from a click, and that is reported whether the cursor is locked or not.
    /// </remarks>
    private static void TrackRightDrag(Input input)
    {
        if (input.MousePressed(MouseButton.Right))
        {
            _rightWent = (0f, 0f);
            _rightMoved = 0f;
        }

        if (!input.MouseDown(MouseButton.Right) && !input.MouseReleased(MouseButton.Right)) return;

        var (dx, dy) = input.MouseDelta;

        _rightWent = (_rightWent.X + dx, _rightWent.Y + dy);
        _rightMoved += MathF.Abs(dx) + MathF.Abs(dy);
    }

    /// <summary>
    /// Whether the right button has been dragged rather than clicked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chiefly how far the pointer ended up from where it started. A hand holding a button still
    /// is not still: it reports a fraction of a pixel most frames, and adding those up calls any
    /// click held for a moment a drag — which is a context menu that works the first quick time
    /// and never again. Summed as a direction rather than a distance, that jitter cancels itself
    /// and a camera look does not.
    /// </para>
    /// <para>
    /// The distance travelled still counts, but at a threshold no click reaches however long it is
    /// held: a look that comes back to where it began has still been a look. Set generously on
    /// purpose — a menu that never opens is a far worse failure than a menu that opens after
    /// somebody turned the camera in a full circle and let go where they started.
    /// </para>
    /// </remarks>
    private static bool RightDragged =>
        MathF.Sqrt((_rightWent.X * _rightWent.X) + (_rightWent.Y * _rightWent.Y)) >= DragSlop
        || _rightMoved >= WanderSlop;

    /// <summary>How far from the press the pointer may end up and still count as a click.</summary>
    private const float DragSlop = 8f;

    /// <summary>How far it may travel in total, however much of that it came back from.</summary>
    private const float WanderSlop = 200f;

    /// <summary>Where the pointer went while the right button was held, as a direction.</summary>
    private static (float X, float Y) _rightWent;

    /// <summary>What a right click on the scene offers, when nothing else claimed it.</summary>
    /// <remarks>
    /// A hook rather than a menu, because what the world's menu contains is the world panel's
    /// business and the shell should not know the name of a single command.
    /// </remarks>
    public static Action<float, float>? ViewportMenu { get; set; }

    /// <summary>How far the pointer moved while the right button was held.</summary>
    private static float _rightMoved;

    /// <summary>Whether an element reported a right click that nothing claimed.</summary>
    private static bool _contextWanted;

    /// <summary>Closes any flyout the person pressed outside of.</summary>
    /// <remarks>
    /// A panel that has never been measured cannot be dismissed, which is what keeps the press
    /// that opened a flyout from closing it again before it has been laid out.
    /// </remarks>
    private static void Dismiss(Input input)
    {
        if (!input.MousePressed(MouseButton.Left)) return;

        var (x, y) = input.MousePosition;

        foreach (var panel in Panels.ToArray())
        {
            if (panel.Chrome.Dismiss != PanelDismiss.OnOutsideClick) continue;
            if (Pinned.Contains(panel)) continue;
            if (Fresh.Contains(panel)) continue;
            if (panel.Window?.Measure() is not { } rect) continue;
            if (rect.Contains(x, y)) continue;

            Hide(panel);
        }
    }
}

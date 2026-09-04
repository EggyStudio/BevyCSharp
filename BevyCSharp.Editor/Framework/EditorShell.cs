using Bevy;

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
        panel.Window?.Layer(panel.Chrome.Layer);
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
            if (panel.Window?.Covers(x, y) == true) return true;
        }

        return false;
    }

    /// <summary>The open panel of this type, or <see langword="null"/>.</summary>
    public static T? Find<T>() where T : class, IEditorPanel =>
        Panels.OfType<T>().FirstOrDefault();

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

    /// <summary>Closes a panel and forgets it.</summary>
    public static void Hide(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.Close();
        Panels.Remove(panel);
        Fresh.Remove(panel);
        Rebuilding();

        if (_drag?.Panel == panel) _drag = null;
    }

    /// <summary>Closes everything.</summary>
    public static void HideAll()
    {
        foreach (var panel in Panels) panel.Close();
        Panels.Clear();
        Fresh.Clear();
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
                    foreach (var panel in Panels)
                        if (panel.Invoke(report.Element)) break;
                    break;

                case UiEventKind.Reloading:
                    foreach (var panel in Panels) panel.Window?.Suspend();
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
        Dismiss(input);

        // A click on a mesh selects it, which is the other half of what the hierarchy does. The
        // shell does this rather than a panel, because selection belongs to the editor and not
        // to whichever panel happens to be open.
        foreach (var picked in Picking.Drain()) EditorSelection.Select(picked);

        // A selection whose entity is gone is worse than none: the inspector would read whatever
        // took its place in storage. Checked once here rather than in every panel that reads it.
        EditorSelection.Prune(ctx.Ecs);

        // Asked once a frame, and used by every text binding: whatever is being typed in is left
        // alone rather than overwritten with what the program still says.
        PanelBinding.Focused = Xui.Focused();

        // Read the world first, arrange second, write the screen third. A panel that filled its
        // rows during this tick is one whose height changed, and the arrangement has to see that
        // before it stacks anything under it.
        foreach (var panel in Panels) panel.Refresh();

        Layout.Arrange(Panels);

        foreach (var panel in Panels) panel.Pull();

        Fresh.Clear();
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
            if (Fresh.Contains(panel)) continue;
            if (panel.Window?.Measure() is not { } rect) continue;
            if (rect.Contains(x, y)) continue;

            Hide(panel);
        }
    }
}

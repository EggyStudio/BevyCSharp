namespace Bevy;

/// <summary>
/// The panels a game has open, and the frame's work of keeping them and the program agreeing.
/// </summary>
/// <remarks>
/// <para>
/// A panel is three files: a document that says what is in it, a stylesheet that says what it
/// looks like, and a class whose fields and methods are tied to the two by attributes. This holds
/// however many of them are open and does the same four things every frame: hand each panel the
/// frame, deliver what the interface reported, let each panel read the world, and write each
/// panel's values back out to the screen.
/// </para>
/// <para>
/// Nothing here arranges anything. Where a panel sits is its stylesheet's business, or the
/// program's through <see cref="UiWindow.PlaceAt"/>; a game that wants docked columns and a menu
/// that dismisses itself writes that on top, which is what this project's editor is.
/// </para>
/// <example>
/// <code>
/// [UiPanel("ui/hud.html", Root = "#hud")]
/// public sealed partial class Hud
/// {
///     [Bind("#score", Mode = BindMode.OneWay)]
///     public string Score { get; private set; } = "0";
/// }
///
/// // once, when the game starts
/// var hud = host.Show(new Hud());
///
/// // every frame
/// host.Tick(ctx);
/// </code>
/// </example>
/// </remarks>
public sealed class UiHost
{
    private readonly List<IUiPanel> _panels = [];
    private readonly HashSet<IUiPanel> _edited = [];

    /// <summary>The panels that are open, in the order they were opened.</summary>
    public IReadOnlyList<IUiPanel> Panels => _panels;

    /// <summary>
    /// The frame being drawn, for a panel that has to read the world.
    /// </summary>
    /// <remarks>
    /// A panel's methods are called by generated code that knows nothing about the engine, and
    /// every panel worth having reads something: how many of a thing there are, what is selected,
    /// what the player has. So the frame's context is put where they can reach it. It is set for
    /// the length of a tick and is whatever ticked last, which for a game with one host is the
    /// only host there is.
    /// </remarks>
    public static BehaviorContext? Context { get; private set; }

    /// <summary>
    /// What a right click landed on when no panel claimed it.
    /// </summary>
    /// <remarks>
    /// Where a game puts its own context menu. A click on a panel's own row is that panel's
    /// business and never arrives here.
    /// </remarks>
    public Action<Entity>? OnUnclaimedContext { get; set; }

    /// <summary>Opens a panel's document and starts driving it.</summary>
    /// <returns>The panel, so a caller can keep it.</returns>
    public T Show<T>(T panel) where T : IUiPanel
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (!_panels.Contains(panel))
        {
            panel.Open();
            _panels.Add(panel);
        }

        return panel;
    }

    /// <summary>Closes a panel and stops driving it.</summary>
    public void Close(IUiPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (!_panels.Remove(panel)) return;

        panel.Close();
    }

    /// <summary>Closes every panel.</summary>
    public void CloseAll()
    {
        foreach (var panel in _panels.ToArray()) Close(panel);
    }

    /// <summary>The open panel of a given kind, or <see langword="null"/>.</summary>
    public T? Find<T>() where T : class, IUiPanel
    {
        foreach (var panel in _panels)
        {
            if (panel is T found) return found;
        }

        return null;
    }

    /// <summary>
    /// Does the frame's work: delivers what the interface reported, then reads and writes.
    /// </summary>
    /// <remarks>
    /// Called once a frame, after the game has done whatever it does. The order is not arbitrary:
    /// the frame is stamped before a single element is looked up, because every window drops the
    /// elements it remembers when the interface rebuilds and a click delivered against a stale one
    /// reaches nothing; what was reported is delivered before the panels are read, so that a panel
    /// acts on this frame's edits rather than last frame's; and the values are written out last,
    /// so what is on screen is what the program says now.
    /// </remarks>
    /// <param name="ctx">The frame, which the panels read the world through.</param>
    public void Tick(BehaviorContext ctx)
    {
        Context = ctx;

        var frame = ctx.Time.FrameCount;

        UiWindow.Frame = frame;
        UiWindow.Generation = Xui.Generation;
        PanelBinding.Frame = frame;
        PanelBinding.Focused = Xui.Focused();

        _edited.Clear();

        foreach (var report in Xui.Drain()) Deliver(report);

        foreach (var panel in _edited) panel.Changed();

        foreach (var panel in _panels)
        {
            panel.Refresh();
            panel.Pull();
        }
    }

    /// <summary>Hands one thing the interface reported to whichever panel it belongs to.</summary>
    private void Deliver(UiEvent report)
    {
        switch (report.Kind)
        {
            case UiEventKind.Change:
                foreach (var panel in _panels)
                {
                    if (!panel.Push(report.Element)) continue;

                    _edited.Add(panel);
                    break;
                }

                break;

            case UiEventKind.Click:
                foreach (var panel in _panels.ToArray())
                {
                    if (panel.Invoke(report.Element)) break;
                }

                break;

            case UiEventKind.Context:
                foreach (var panel in _panels.ToArray())
                {
                    if (panel.Context(report.Element)) return;
                }

                OnUnclaimedContext?.Invoke(report.Element);
                break;

            case UiEventKind.Reloading:
                // The documents are being built again, so every element every panel remembers is
                // about to be replaced. What the panels hold is not: the write at the end of the
                // next tick puts it straight back onto the new elements, which is what makes
                // editing a document while the game runs feel like editing rather than restarting.
                foreach (var panel in _panels) panel.Window?.Suspend();
                PanelBinding.Forget();
                break;

            case UiEventKind.Reloaded:
                foreach (var panel in _panels) panel.Window?.Resume();
                break;
        }
    }
}

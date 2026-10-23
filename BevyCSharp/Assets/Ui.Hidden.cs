namespace Bevy;

// The hidden scope, the part of a program's interface F1 hides, as Minecraft hides its HUD while
// the player's view shows and never a screen drawn over it, so a pause menu or an inventory drawn
// outside the scope stays whatever F1 says, and the engine's debug overlay stays too.
public static unsafe partial class Ui
{
    /// <summary>A node made inside the scope, and what its visibility was before the scope hid it.</summary>
    private sealed class ScopedNode(Entity node)
    {
        public readonly Entity Node = node;
        public VisibilityMode Was;
        public bool HiddenByScope;
    }

    private static readonly List<ScopedNode> Scoped = [];
    private static App? _scopedApp;

    // How deep the scope is on this thread, since a system opens and closes it on the one thread it
    // runs on, and systems run on several at once.
    [ThreadStatic]
    private static int _scope;

    /// <summary>Whether what is drawn inside the hidden scope is hidden, which <see cref="HiddenKey"/> turns on and off.</summary>
    /// <remarks>
    /// Read by <see cref="BeginHidden"/> as a frame's scope opens, and by the engine once a frame,
    /// which hides the scope's nodes and shows them again as they were. Settable, so a program
    /// hides its interface for a picture or a cut scene without the key.
    /// </remarks>
    public static bool Hidden { get; set; }

    /// <summary>The key that hides and shows the scope's interface, F1 unless set, or nothing for none.</summary>
    /// <remarks>
    /// Pressed in a run that draws, while no field of an ImGui window has the caret, it turns
    /// <see cref="Hidden"/> over, and the program still sees the press, so a program that asked for
    /// F1 for itself moves this or turns it off.
    /// </remarks>
    public static Key? HiddenKey { get; set; } = Key.F1;

    /// <summary>
    /// Opens the hidden scope, answering whether what is drawn inside it is drawn this frame, false
    /// while <see cref="Hidden"/> holds. Closed by <see cref="EndHidden"/>, however it answers.
    /// </summary>
    /// <returns>Whether to draw the scope's ImGui windows this frame.</returns>
    /// <remarks>
    /// <para>
    /// ImGui draws whatever is submitted in a frame, so a program's own ImGui windows go inside an
    /// <c>if</c> on the answer, as they go inside one on <c>ImGui.Begin</c>, and are not submitted
    /// while the scope is hidden. The engine hides by itself what it keeps of the scope across
    /// frames, the interface nodes made inside it with <see cref="SpawnNode"/> and
    /// <see cref="SpawnText(string, UiSettings, UiTextSettings)"/>, and what is made under such a
    /// node, which takes its visibility, so a HUD made inside the scope as the program starts hides
    /// on F1 whatever the program does after.
    /// </para>
    /// <para>
    /// A screen, a pause menu, a map or an inventory, is drawn outside the scope, and stays.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (Ui.BeginHidden())
    /// {
    ///     ImGui.Begin("hud");
    ///     ImGui.Text($"health {health}");
    ///     ImGui.End();
    /// }
    /// Ui.EndHidden();
    /// </code>
    /// </example>
    public static bool BeginHidden()
    {
        _scope++;
        return !Hidden;
    }

    /// <summary>Closes the hidden scope <see cref="BeginHidden"/> opened.</summary>
    /// <exception cref="InvalidOperationException">No scope is open.</exception>
    public static void EndHidden()
    {
        if (_scope == 0) throw new InvalidOperationException("EndHidden closes the scope BeginHidden opened, and none is open.");
        _scope--;
    }

    /// <summary>Records a node made inside the scope, so it is hidden with it, and answers the node.</summary>
    private static Entity InScope(Entity node)
    {
        if (_scope == 0) return node;

        lock (Scoped)
        {
            Current();
            Scoped.Add(new ScopedNode(node));
        }

        return node;
    }

    /// <summary>
    /// Takes the key, and hides the scope's nodes or gives them back the visibility they had, once a
    /// frame from the engine's tick in a run that draws.
    /// </summary>
    internal static void TickHidden(EcsWorld world, Input input)
    {
        if (HiddenKey is { } key && input.KeyPressed(key) && !ImGuiRuntime.Typing) Hidden = !Hidden;

        lock (Scoped)
        {
            Current();
            Scoped.RemoveAll(scoped => !world.IsAlive(scoped.Node));

            foreach (var scoped in Scoped)
            {
                if (Hidden == scoped.HiddenByScope) continue;

                if (Hidden)
                {
                    scoped.Was = world.TryGet<Visibility>(scoped.Node, out var visibility) ? visibility.Mode : VisibilityMode.Inherited;
                    world.Set(scoped.Node, new Visibility(VisibilityMode.Hidden));
                }
                else
                {
                    world.Set(scoped.Node, new Visibility(scoped.Was));
                }

                scoped.HiddenByScope = Hidden;
            }
        }
    }

    /// <summary>The scope's nodes, emptied where another app runs than the one they were made in.</summary>
    private static void Current()
    {
        var app = App.Running;
        if (app is null || ReferenceEquals(app, _scopedApp)) return;

        Scoped.Clear();
        _scopedApp = app;
    }
}

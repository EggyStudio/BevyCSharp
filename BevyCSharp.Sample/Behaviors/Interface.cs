using Bevy;
using BevyCSharp.Sample.Panels;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Opens the game's own interface and keeps it running.
/// </summary>
/// <remarks>
/// <para>
/// The whole of what a game has to write to have an interface built from documents and
/// stylesheets: make a host, show a panel, and tick the host once a frame. Everything else is in
/// the three files the panel is made of.
/// </para>
/// <para>
/// It needs a bridge with the interface compiled in (<c>build/build-native.sh --editor</c>) and
/// <c>Config.HtmlUi</c> asked for, and says so and stops rather than drawing nothing.
/// </para>
/// </remarks>
[Behavior]
public partial struct Interface
{
    /// <summary>The host, kept for the life of the program.</summary>
    private static UiHost? _host;

    /// <summary>Opens the panel once the interface is up.</summary>
    [OnStartup]
    public static void Open(BehaviorContext ctx)
    {
        if (!App.HasEditor)
        {
            Console.WriteLine(
                "[sample] this bridge has no interface compiled in, so the panel is not opened."
                + " Rebuild it with build/build-native.sh --editor.");
            return;
        }

        _host = new UiHost();
        _host.Show(new HudPanel());
    }

    /// <summary>Does the interface's frame: what was clicked, what changed, what to draw.</summary>
    [OnUpdate]
    public static void Tick(BehaviorContext ctx) => _host?.Tick(ctx);
}

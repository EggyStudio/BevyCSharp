using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// The game's own interface, which is a page.
/// </summary>
/// <remarks>
/// <para>
/// The whole of what a game writes to have one: read the markup and the stylesheet, open them, and
/// write into the parts that change. There is no panel type, no binding declaration and no
/// generator: an element has an id, and an id is what is written to.
/// </para>
/// <para>
/// It needs a bridge with the interface compiled in (<c>build/build-native.sh --editor</c>) and
/// <c>Config.HtmlUi</c> asked for, and says so and stops rather than drawing nothing.
/// </para>
/// </remarks>
[Behavior]
public partial struct Interface
{
    private static readonly DomEvent[] Reports = new DomEvent[16];

    private static bool _open;
    private static int _score;

    /// <summary>Opens the page once the engine is up.</summary>
    [OnStartup]
    public static void Open(BehaviorContext ctx)
    {
        if (!App.HasEditor)
        {
            Console.WriteLine(
                "[sample] this bridge has no interface compiled in, so the page is not opened."
                + " Rebuild it with build/build-native.sh --editor.");
            return;
        }

        var assets = Path.Combine(AppContext.BaseDirectory, "assets");
        var ui = Path.Combine(assets, "ui");

        var markup = File.ReadAllText(Path.Combine(ui, "hud.html"));
        var theme = File.ReadAllText(Path.Combine(ui, "hud.css"));

        // Spliced rather than linked, so the stylesheet is one file on disk and one read here.
        Dom.Open(
            markup.Replace("<style id=\"theme\"></style>", $"<style id=\"theme\">{theme}</style>"),
            assets);

        _open = true;
    }

    /// <summary>Says what happened and shows what changed.</summary>
    [OnUpdate]
    public static void Tick(BehaviorContext ctx)
    {
        if (!_open) return;

        var count = Dom.Drain(Reports);

        for (var index = 0; index < count; index++)
        {
            if (Reports[index].Kind != DomEventKind.Click) continue;

            if (Dom.ClosestId(Reports[index].Target) == "score-up") _score++;
        }

        var score = Dom.Element("score");
        if (score.Exists) Dom.SetText(score, _score.ToString());

        var rate = Dom.Element("rate");
        if (rate.Exists) Dom.SetText(rate, $"{1f / MathF.Max(ctx.Time.Delta, 0.0001f):0} fps");
    }
}

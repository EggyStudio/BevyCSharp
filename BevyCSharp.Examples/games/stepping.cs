// Bevy's stepping helper, examples/showcase/stepping.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

namespace BevyCSharp.Examples.Games;

// A panel listing the systems of the stages it steps, with an arrow at the one stepping runs next,
// shown while stepping is on, and the keys that control it. ` turns stepping on and off, S runs the
// next system, Space the rest of the frame, and / logs Bevy's account of the stepping.
//
// Bevy's runs its systems in a schedule of its own after Update, and these run in PostUpdate, the
// stage after it, which the panel leaves to run if it is stepped. Bevy's leaves its own systems to
// run, known by their paths beginning with bevy, and steps the rest of the stages it is given. In
// this app every example's behaviors are in the same stages, so the panel steps the systems the
// game names as its own and leaves the rest to run.
internal static class SteppingPanel
{
    private static readonly Color FontColor = Color.FromSrgb(0.2f, 0.2f, 0.2f);
    private const string FontBold = "fonts/FiraSans-Bold.ttf";
    private const string Hint = "Press ` to toggle stepping mode (S: step system, Space: step frame)";

    /// <summary>
    /// Steps <paramref name="stages"/>, the systems <paramref name="steps"/> passes among them, from
    /// keys, with the panel at <paramref name="left"/> and <paramref name="top"/>.
    /// </summary>
    public static App AddSteppingPanel(this App app, Stage[] stages, Length left, Length top, Func<string, bool> steps)
    {
        var panel = Entity.None;

        // Each shown system's place in Stepping.Systems, and the run of text its arrow is drawn in.
        var arrows = new Dictionary<int, Entity>();

        app.Startup(ctx =>
        {
            Log.Info(Hint);
            var hint = Ui.SpawnText(Hint, new UiSettings { Absolute = true, Bottom = Length.Px(5f), Left = Length.Px(5f) }, new UiTextSettings { FontSize = 15f });
            ctx.Ecs.Wrap<TextColorRef>(hint).Value = FontColor;

            foreach (var stage in stages) Stepping.Add(stage);
        }, "stepping.BuildSteppingHint");

        app.On(Stage.PostUpdate, ctx =>
        {
            // The panel waits for the list of the stepped systems, which is there from the second
            // frame.
            if (panel == Entity.None) panel = BuildUi(ctx, left, top, steps, arrows);
            HandleInput(ctx);
            if (panel != Entity.None) UpdateUi(ctx, panel, arrows);
        }, PanelSystem);

        return app;
    }

    // The name of the panel's own system, which it never steps.
    private const string PanelSystem = "stepping.Panel";

    private static Entity BuildUi(BehaviorContext ctx, Length left, Length top, Func<string, bool> steps, Dictionary<int, Entity> arrows)
    {
        if (Stepping.Systems is not { } systems) return Entity.None;

        var bold = AssetServer.Load(AssetKind.Font, FontBold);
        var panel = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = top, Left = left, Padding = Length.Px(10f) }, new UiTextSettings());
        ctx.Ecs.Insert<BackgroundColorRef>(panel).Value = Color.FromSrgb(1f, 1f, 1f, 0.33f);
        ctx.Ecs.Wrap<VisibilityRef>(panel).Value = Visibility.Hidden;

        Stage? heading = null;
        var running = new HashSet<string>();
        for (var index = 0; index < systems.Length; index++)
        {
            var (stage, name) = systems[index];
            if (stage != heading)
            {
                Ui.SpawnTextSpan(panel, $"{stage}\n", new UiTextSettings { Font = bold }, FontColor);
                heading = stage;
            }

            // Left to run, once a name however many systems carry it.
            if (name == PanelSystem || !steps(name))
            {
                if (running.Add(name)) Stepping.AlwaysRun(stage, name);
                continue;
            }

            arrows[index] = Ui.SpawnTextSpan(panel, "   ", new UiTextSettings(), FontColor);
            Ui.SpawnTextSpan(panel, $"{name}\n", new UiTextSettings(), FontColor);
        }

        return panel;
    }

    private static void HandleInput(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Slash)) Log.Info(Stepping.Describe() ?? string.Empty);

        if (ctx.Input.KeyPressed(Key.Backquote))
        {
            if (Stepping.IsEnabled) Stepping.Disable();
            else Stepping.Enable();
        }

        if (!Stepping.IsEnabled) return;

        // Space runs the rest of the frame, and S the next system.
        if (ctx.Input.KeyPressed(Key.Space)) Stepping.ContinueFrame();
        else if (ctx.Input.KeyPressed(Key.S)) Stepping.StepFrame();
    }

    private static void UpdateUi(BehaviorContext ctx, Entity panel, Dictionary<int, Entity> arrows)
    {
        // Shown only while stepping is on.
        var enabled = Stepping.IsEnabled;
        var visibility = ctx.Ecs.Wrap<VisibilityRef>(panel);
        var shown = enabled ? Visibility.Inherited : Visibility.Hidden;
        if (visibility.Value != shown) visibility.Value = shown;

        if (!enabled || Stepping.Cursor is not { } cursor) return;

        foreach (var (index, arrow) in arrows)
            ctx.Ecs.Wrap<TextSpanRef>(arrow).Value = index == cursor ? "-> " : "   ";
    }
}

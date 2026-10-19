// Bevy's feathers_counter example, examples/ui/widgets/feathers_counter.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// A counter with Feathers' buttons, -1 and +1 either side of the count, on the window's background
// from Bevy's dark theme.
internal static class FeathersCounter
{
    private static int _counter;
    private static bool _changed;
    private static Entity _text;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            // Changed on its first frame, as Bevy counts a resource on the frame it is inserted.
            (_counter, _changed) = (0, true);
            Render2d.SpawnCamera2d();

            var root = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Percent(100f),
                Height = Length.Percent(100f),
                Align = UiAlign.Center,
                Justify = UiJustify.Center,
            });
            Feathers.ThemeColor(root, FeathersToken.WindowBg);

            var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Justify = UiJustify.Center });
            ecs.SetParent(row, root);

            var minus = Feathers.Button(row, "-1");
            ecs.Observe<Activate>(minus, _ => (_counter, _changed) = (_counter - 1, true));

            _text = Feathers.Caption(row, "0");
            var margin = ecs.Wrap<NodeRef>(_text);
            (margin.MarginLeft, margin.MarginRight) = (new Val.Px(10f), new Val.Px(10f));

            var plus = Feathers.Button(row, "+1");
            ecs.Observe<Activate>(plus, _ => (_counter, _changed) = (_counter + 1, true));
        }, "feathers_counter.Setup");

        // Bevy's runs while the counter has changed.
        app.Update(_ =>
        {
            if (!_changed) return;
            _changed = false;
            Log.Info("Counter updated");
            Ui.SetText(_text, _counter.ToString());
        }, "feathers_counter.UpdateCounterText");
    }
}

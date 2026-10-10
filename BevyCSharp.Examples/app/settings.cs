// Bevy's settings example, examples/app/settings.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Text.Json.Serialization;
using Bevy;

namespace BevyCSharp.Examples.Application;

// Demonstrates settings kept between runs, a counter that Space raises and Backspace lowers, saved
// a tenth of a second after it changes and again as the app stops, and read back the next time.
internal static class Settings
{
    internal sealed record Counter(int Count);

    // Bevy keeps this beside the counter in the same group, and it only ever reads it.
    internal sealed record OtherSettings(bool Enabled);

    internal static Persistent<Counter>? CounterSetting;
    internal static Persistent<OtherSettings>? Other;
    private static float _saveIn = -1f;

    public static void Build(App app)
    {
        CounterSetting = new Persistent<Counter>("settings-counter", SettingsJson.Default.Counter, () => new Counter(0));
        Other = new Persistent<OtherSettings>("settings-counter-other", SettingsJson.Default.OtherSettings, () => new OtherSettings(true));

        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            var column = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Percent(100f),
                Height = Length.Percent(100f),
                Direction = UiDirection.Column,
                Align = UiAlign.Center,
                Justify = UiJustify.Center,
            });

            var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
            var display = Ui.SpawnText("---", new UiSettings { Color = light }, 33f);
            ctx.Ecs.Add(display, new CounterDisplay());
            ctx.Ecs.SetParent(display, column);
            ctx.Ecs.SetParent(Ui.SpawnText("Press SPACE to increment, BACKSPACE to decrement.", new UiSettings(), 20f), column);
        }, "settings.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var change = (input.KeyPressed(Key.Space) ? 1 : 0) - (input.KeyPressed(Key.Backspace) || input.KeyPressed(Key.Delete) ? 1 : 0);
            if (change != 0)
            {
                CounterSetting!.Update(counter => counter with { Count = counter.Count + change });
                _saveIn = 0.1f;
            }

            // Saved a moment after the last change, as Bevy's SaveSettingsDeferred waits, so keys
            // pressed quickly write the file once.
            if (_saveIn >= 0f && (_saveIn -= ctx.Time.Delta) < 0f) CounterSetting!.Persist();
        }, "settings.ChangeCount");

        // Whatever has not been written yet is written as the app stops, as Bevy's is when its
        // window is asked to close.
        app.On(Stage.Cleanup, _ => CounterSetting!.Persist(), "settings.SaveOnExit");
    }
}

/// <summary>The text that shows the count, and the count it shows.</summary>
[Behavior]
public partial struct CounterDisplay
{
    /// <summary>The count it shows.</summary>
    public int Shown;

    /// <summary>Whether it has shown one yet.</summary>
    public bool Showing;

    /// <summary>
    /// The count written when it differs from what is shown, as Bevy's <c>show_count</c> writes it
    /// when the counter has changed, or that the counter is disabled.
    /// </summary>
    [OnUpdate]
    public void ShowCount(BehaviorContext ctx)
    {
        if (!Settings.Other!.Value.Enabled)
        {
            Ui.SetText(ctx.Entity, "Disabled");
            return;
        }

        var count = Settings.CounterSetting!.Value.Count;
        if (Showing && count == Shown) return;
        (Shown, Showing) = (count, true);
        Ui.SetText(ctx.Entity, $"Count: {count}");
    }
}

[JsonSerializable(typeof(Settings.Counter))]
[JsonSerializable(typeof(Settings.OtherSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext;

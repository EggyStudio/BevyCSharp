// Bevy's low_power example, examples/window/low_power.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// Runs the window in each of winit's update modes in turn, Space moving to the next, continuous, as
// a game is played, then reactive, as a desktop tool rests until something happens, then reactive
// with a redraw asked for every update, and then with the loop woken every update. The cube turns
// only while the app updates, and the count says how many updates there have been.
//
// Vertical sync is off, as Bevy's opens its window with AutoNoVsync, so a continuous app updates as
// fast as it can. An offscreen run has no window and runs its own loop, so it updates every frame
// in every mode.
internal static class LowPower
{
    private enum ExampleMode
    {
        Game,
        Application,
        ApplicationWithRequestRedraw,
        ApplicationWithWakeUp,
    }

    private static ExampleMode _mode;

    // Bevy's update_text counts its updates in a Local.
    private static int _frame;

    // The two runs of the text that change, the mode's name and the count.
    private static Entity _modeSpan, _frameSpan;

    public static void Configure(Config config) => config.Vsync = false;

    public static void Build(App app)
    {
        (_mode, _frame) = (ExampleMode.Game, 0);
        app.Startup(Setup, "low_power.Setup");
        app.Update(CycleModes, "low_power.CycleModes");
        app.Update(UpdateText, "low_power.UpdateText");
        app.Update(UpdateWinit, "low_power.UpdateWinit");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's own settings of these, before the first update sets them by the mode.
        Window.SetUpdateSettings(new UpdateSettings(UpdateMode.Continuous, UpdateMode.ReactiveLowPower(TimeSpan.FromMilliseconds(10))));

        var cube = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f),
            Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)),
            Transform.Identity);
        ecs.Add(cube, new TurningCube());

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ecs.Add(light, Transform.LookingAt(new Vec3(1f, 1f, 1f), Vec3.Zero, Vec3.UnitY));
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2f, 2f), Vec3.Zero, Vec3.UnitY));
        Window.RequestRedraw();

        var text = Ui.SpawnText("", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        var style = new UiTextSettings { FontSize = 20f };
        Ui.SpawnTextSpan(text, "Press space bar to cycle modes\n", style, (1f, 1f, 1f, 1f));
        _modeSpan = Ui.SpawnTextSpan(text, "", style, (0f, 1f, 0f, 1f));
        Ui.SpawnTextSpan(text, "\nFrame: ", style, (1f, 1f, 0f, 1f));
        _frameSpan = Ui.SpawnTextSpan(text, "", style, (1f, 1f, 0f, 1f));
    }

    private static void CycleModes(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;

        _mode = _mode switch
        {
            ExampleMode.Game => ExampleMode.Application,
            ExampleMode.Application => ExampleMode.ApplicationWithRequestRedraw,
            ExampleMode.ApplicationWithRequestRedraw => ExampleMode.ApplicationWithWakeUp,
            _ => ExampleMode.Game,
        };
    }

    private static void UpdateText(BehaviorContext ctx)
    {
        _frame++;
        ctx.Ecs.Wrap<TextSpanRef>(_modeSpan).Value = _mode switch
        {
            ExampleMode.Game => "game(), continuous, default",
            ExampleMode.Application => "desktop_app(), reactive",
            ExampleMode.ApplicationWithRequestRedraw => "desktop_app(), reactive, RequestRedraw sent",
            _ => "desktop_app(), reactive, WakeUp sent",
        };
        ctx.Ecs.Wrap<TextSpanRef>(_frameSpan).Value = _frame.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // The mode's settings, every update, as Bevy's update_winit writes them, with a redraw asked
    // for or the loop woken where the mode says, which updates the app again at once.
    private static void UpdateWinit(BehaviorContext ctx)
    {
        if (_mode == ExampleMode.Game)
        {
            Window.SetUpdateSettings(UpdateSettings.Game);
            return;
        }

        Window.SetUpdateSettings(UpdateSettings.DesktopApp);
        if (_mode == ExampleMode.ApplicationWithRequestRedraw) Window.RequestRedraw();
        else if (_mode == ExampleMode.ApplicationWithWakeUp) Window.WakeUp();
    }
}

/// <summary>Turns its entity about X and then about its own Y, a radian a second each, while the app updates.</summary>
[Behavior]
public partial struct TurningCube
{
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationX(ctx.Time.Delta) * transform.Rotation * Quat.FromRotationY(ctx.Time.Delta);
}

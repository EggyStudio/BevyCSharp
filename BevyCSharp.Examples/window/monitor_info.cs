// Bevy's monitor_info example, examples/window/monitor_info.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// Opens a fullscreen window on each monitor the platform reports, saying what the monitor is, and
// closes a monitor's window when it is unplugged, or the focused one on Escape. The app opens no
// window of its own and runs on with none open, as Bevy's does with its primary window set to none.
//
// Bevy's notices a monitor by its Added filter and one unplugged by RemovedComponents, and this
// compares the monitors found each frame with the ones it has already met. A run with no display
// has no monitors, so offscreen it opens nothing and draws nothing.
internal static class MonitorInfoExample
{
    // Each monitor met, with the window opened on it and the text drawn there, which Bevy's tags
    // with a MonitorRef to find again once the monitor is gone.
    private static readonly Dictionary<Entity, (Entity Window, Entity Text)> Opened = [];

    public static void Configure(Config config) => config.PrimaryWindow = false;

    public static void Build(App app)
    {
        Opened.Clear();
        app.Update(Update, "monitor_info.Update");
        app.Update(CloseOnEsc, "monitor_info.CloseOnEsc");
    }

    private static void Update(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var found = ecs.All<MonitorRef>();
        var monitors = found.Select(monitor => monitor.Entity).ToHashSet();

        foreach (var monitor in found.Where(monitor => !Opened.ContainsKey(monitor.Entity)))
        {
            var entity = monitor.Entity;
            var name = monitor.Name ?? "<no name>";
            var size = $"{monitor.PhysicalHeight}x{monitor.PhysicalWidth}px";
            var hz = monitor.RefreshRateMillihertz is { } milli ? FormattableString.Invariant($"{milli / 1000f}Hz") : "<unknown>";
            var position = $"x={monitor.PhysicalPositionX} y={monitor.PhysicalPositionY}";
            var scale = FormattableString.Invariant($"{monitor.ScaleFactor:0.00}");

            var window = ecs.Spawn();
            ecs.Insert<WindowRef>(window).Title = name;
            Window.SetFullscreenOn(window, entity);

            var camera = Render2d.SpawnCamera2d();
            Render.SetCameraTarget(camera, window);

            var text = Ui.SpawnText($"Monitor: {name}\nSize: {size}\nRefresh rate: {hz}\nPosition: {position}\nScale: {scale}\n\n",
                new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Camera = camera });
            Opened[entity] = (window, text);
        }

        // What was opened for each monitor unplugged.
        foreach (var gone in Opened.Keys.Where(entity => !monitors.Contains(entity)).ToList())
        {
            foreach (var opened in new[] { Opened[gone].Window, Opened[gone].Text }.Where(ecs.IsAlive)) ecs.Despawn(opened);
            Opened.Remove(gone);
        }
    }

    private static void CloseOnEsc(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Escape)) return;
        var ecs = ctx.Ecs;
        foreach (var window in Opened.Values.Select(opened => opened.Window).Where(ecs.IsAlive))
        {
            if (ecs.Get<WindowRef>(window)?.Focused == true) ecs.Despawn(window);
        }
    }
}

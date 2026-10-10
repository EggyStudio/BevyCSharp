// Bevy's log_diagnostics example, examples/diagnostics/log_diagnostics.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Measuring;

// Bevy's diagnostics of frame times, the entity count and the render passes, which its log prints
// once a second over a small scene, the render passes' only while something is drawn. Q filters the
// log to the groups 1, 2 and 3 choose, and the rows at the top left say which.
//
// Bevy's system information diagnostics, its row 3, measure the process's and the machine's CPU and
// memory through a crate the bridge does not compile in, so filtering to them here leaves them out
// of a log that has none of them.
internal static class LogDiagnostics
{
    private static readonly string[] FrameTimeDiagnostics = [Diagnostics.Fps, Diagnostics.FrameCount, Diagnostics.FrameTime];
    private static readonly string[] EntityCountDiagnostics = [Diagnostics.EntityCount];
    private static readonly string[] SystemInfoDiagnostics = ["process/cpu_usage", "process/mem_usage", "system/cpu_usage", "system/mem_usage"];

    // Tailwind's green-400 and red-400.
    private static readonly Color Green = Color.FromHex("4ade80");
    private static readonly Color Red = Color.FromHex("f87171");

    // Bevy's LogDiagnosticsStatus, whether the log is filtered, and LogDiagnosticsFilters, which
    // groups the filter lets through.
    private static bool _filtering, _frameTime, _entityCount, _systemInfo;
    private static readonly HashSet<string> Filter = [];
    private static readonly List<(Entity Label, Entity Value)> Rows = [];

    public static void Configure(Config config) =>
        config.DiagnosticPlugins = DiagnosticPlugins.Log | DiagnosticPlugins.FrameTime | DiagnosticPlugins.EntityCount | DiagnosticPlugins.Render;

    public static void Build(App app)
    {
        (_filtering, _frameTime, _entityCount, _systemInfo) = (false, false, false, false);
        Filter.Clear();
        Rows.Clear();

        app.Startup(Setup, "log_diagnostics.Setup");
        app.Update(FiltersInputs, "log_diagnostics.FiltersInputs");
    }

    // Bevy's 3D scene, since its render passes are measured only while something is drawn, and the
    // rows that say how the log is filtered.
    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Circle, 4f),
            Render.CreateMaterial((1f, 1f, 1f, 1f)),
            new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)), Transform.At(0f, 0.5f, 0f));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));

        var commands = Ui.SpawnNode(new UiSettings { Top = Length.Px(5f), Left = Length.Px(5f), Direction = UiDirection.Column });
        foreach (var label in new[] { "[Q] Toggle filtering:", "[1] Frame times:", "[2] Entity count:", "[3] System info:", "[4] Render diagnostics:" })
        {
            var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(5f) });
            ecs.SetParent(row, commands);
            var name = Ui.SpawnText(label, new UiSettings());
            var value = Ui.SpawnText("", new UiSettings());
            ecs.SetParent(name, row);
            ecs.SetParent(value, row);
            Rows.Add((name, value));
        }

        UpdateCommands(ecs);
    }

    private static void FiltersInputs(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var changed = false;

        // Unfiltered, the log prints everything. Filtered, it prints the groups chosen.
        if (input.KeyPressed(Key.Q))
        {
            _filtering = !_filtering;
            changed = true;
        }

        if (input.KeyPressed(Key.Digit1)) (_frameTime, changed) = (!_frameTime, true);
        if (input.KeyPressed(Key.Digit2)) (_entityCount, changed) = (!_entityCount, true);
        if (input.KeyPressed(Key.Digit3)) (_systemInfo, changed) = (!_systemInfo, true);
        if (!changed) return;

        Filter.Clear();
        if (_frameTime) Filter.UnionWith(FrameTimeDiagnostics);
        if (_entityCount) Filter.UnionWith(EntityCountDiagnostics);
        if (_systemInfo) Filter.UnionWith(SystemInfoDiagnostics);
        Diagnostics.SetLogFilter(_filtering ? Filter : null);

        UpdateCommands(ctx.Ecs);
    }

    // Each row's name white and its value green or red, both faded while the log is unfiltered,
    // except the first row's, which says whether it is. Bevy's render diagnostics' paths are its
    // own, so their row says they are private.
    private static void UpdateCommands(EcsWorld ecs)
    {
        var alpha = _filtering ? 1f : 0.25f;
        (string Text, bool On)[] values =
        [
            (_filtering ? "Enabled" : "Disabled", _filtering),
            (_frameTime ? "true" : "false", _frameTime),
            (_entityCount ? "true" : "false", _entityCount),
            (_systemInfo ? "true" : "false", _systemInfo),
            ("Private", false),
        ];

        for (var i = 0; i < Rows.Count; i++)
        {
            var (label, value) = Rows[i];
            var faded = i == 0 ? 1f : alpha;
            var color = values[i].On ? Green : Red;

            ecs.Wrap<TextColorRef>(label).Value = new Color(1f, 1f, 1f, faded);
            ecs.Wrap<TextColorRef>(value).Value = new Color(color.R, color.G, color.B, faded);
            Ui.SetText(value, values[i].Text);
        }
    }
}

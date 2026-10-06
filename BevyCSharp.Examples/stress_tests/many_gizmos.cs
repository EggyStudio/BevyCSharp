// Bevy's many_gizmos example, examples/stress_tests/many_gizmos.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// Draws a great many gizmo lines a frame, split across ten systems. Up and Down raise and lower
// the count by ten thousand, and Space turns on a fancy mode, where the lines fan out in a circle
// in gradients that move with the clock.
internal static class ManyGizmos
{
    private const int SystemCount = 10;

    // Bevy's Config resource, the lines drawn a frame and whether they are fancy.
    private static int _lineCount;
    private static bool _fancy;

    private static Entity _text;

    // Each system's lines, built in a buffer of its own and drawn in one call. Bevy's systems draw
    // a line a call into a buffer Bevy keeps, and here a run of lines crosses into the bridge at
    // once, as the bridge asks of anything drawn in runs.
    private static readonly GizmoSegment[][] Buffers = new GizmoSegment[SystemCount][];

    public static void Configure(Config config) => StressTest.Configure(config);

    public static void Build(App app)
    {
        (_lineCount, _fancy) = (50_000, false);
        StressTest.Add(app);

        app.Startup(Setup, "many_gizmos.Setup");
        app.Update(Input, "many_gizmos.Input");
        app.Update(UiSystem, "many_gizmos.UiSystem");
        for (var i = 0; i < SystemCount; i++)
        {
            var index = i;
            app.Update(ctx => DrawLines(ctx, index), $"many_gizmos.System{i}");
        }
    }

    private static void Input(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.ArrowUp)) _lineCount += 10_000;
        if (input.KeyPressed(Key.ArrowDown)) _lineCount = Math.Max(_lineCount - 10_000, 0);
        if (input.KeyPressed(Key.Space)) _fancy = !_fancy;
    }

    private static void DrawLines(BehaviorContext ctx, int system)
    {
        var count = _lineCount / SystemCount;
        if (Buffers[system] is not { } lines || lines.Length < count) lines = Buffers[system] = new GizmoSegment[count];

        if (!_fancy)
        {
            for (var i = 0; i < count; i++) lines[i] = new GizmoSegment(-Vec3.UnitY, Vec3.UnitY, (0f, 0f, 0f, 1f));
        }
        else
        {
            var wave = MathF.Sin(ctx.Time.Elapsed);
            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)count * MathF.Tau;
                var vector = new Vec3(MathF.Sin(angle), MathF.Cos(angle), wave);
                lines[i] = GizmoSegment.Fading(vector, -vector, (vector.X, vector.Z, 0.5f, 1f), (-vector.Z, -vector.Y, 0.5f, 1f));
            }
        }

        Gizmos.Lines(lines.AsSpan(0, count));
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(3f, 1f, 5f), Vec3.Zero, Vec3.UnitY));
        _text = Ui.SpawnText("", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // The count, the frames a second as Bevy's diagnostics smooth them, and the controls.
    private static void UiSystem(BehaviorContext ctx) => Ui.SetText(_text, FormattableString.Invariant(
        $"Line count: {_lineCount}\nFPS: {ctx.Time.SmoothedFps:F0}\n\nControls:\nUp/Down: Raise or lower the line count.\nSpacebar: Toggle fancy mode."));
}

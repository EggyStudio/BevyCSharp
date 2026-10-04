using System.Globalization;

namespace Bevy;

/// <summary>A frame's costs measured from the console, the command line and the editor's console alike.</summary>
internal static class ConsoleProfileCommands
{
    /// <summary>Measures a number of frames and says what each spent on average.</summary>
    /// <remarks>
    /// <para>
    /// The answer comes once the frames have run, through <see cref="ConsoleHost.Later"/>, so a
    /// script asking from <c>bcs</c> gets the figures as the command's answer. The last line holds
    /// every figure as a name and a number, for a script to read without parsing the sentences.
    /// </para>
    /// <para>
    /// At most <see cref="ConsoleHost.LaterFrames"/> frames, which is as long as a caller waits.
    /// </para>
    /// </remarks>
    [Command("frame.profile", "Measures what a frame spends over N frames: frame.profile [frames]")]
    internal static string Profile(string line)
    {
        var frames = int.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var asked) ? asked : 120;
        frames = Math.Clamp(frames, 1, (int)ConsoleHost.LaterFrames - 2);

        // From the next frame whole, since this one is partway through.
        var from = ConsoleHost.Time.FrameCount + 1;
        var until = from + (ulong)frames;
        var started = false;

        ConsoleHost.Later(() =>
        {
            var now = ConsoleHost.Time.FrameCount;
            if (!started)
            {
                if (now < from) return null;
                FrameProfile.Start();
                started = true;
                return null;
            }

            if (now < until) return null;

            var costs = FrameProfile.Take(ConsoleHost.Ecs);
            FrameProfile.Stop();
            return Describe(costs);
        });

        return $"measuring {frames} frames";
    }

    /// <summary>The figures as sentences, then as one line of names and numbers.</summary>
    internal static string Describe(FrameCosts costs)
    {
        static string N(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
        static string Whole(double value) => value.ToString("0", CultureInfo.InvariantCulture);

        var lines = new List<string>
        {
            $"{costs.Frames} frames, {N(costs.FrameMilliseconds)} ms a frame ({Whole(1000d / Math.Max(costs.FrameMilliseconds, 1e-6))} a second), {costs.Entities} entities",
            $"schedule {N(costs.ScheduleMilliseconds)} ms: managed systems {N(costs.ManagedMilliseconds)} ms in {Whole(costs.ManagedRuns)} runs, Bevy {N(costs.BevyMilliseconds)} ms",
            $"crossings {Whole(costs.Crossings)} a frame, {N(costs.CrossingMilliseconds)} ms going into the bridge, through it and back"
                + (costs.Crossings > 0 ? $" ({N(costs.CrossingMilliseconds * 1e6 / costs.Crossings)} ns each), within the managed systems' time" : string.Empty),
            costs.RenderMilliseconds is null
                ? "no render"
                : $"render schedule {N(costs.RenderMilliseconds.Value)} ms beside the main one, of which the passes are "
                    + (costs.RenderCpuMilliseconds is null && costs.RenderGpuMilliseconds is null
                        ? "not measured; run with Config.GpuTimings to measure them"
                        : $"{N(costs.RenderCpuMilliseconds ?? 0d)} ms cpu and {N(costs.RenderGpuMilliseconds ?? 0d)} ms gpu"),
        };

        foreach (var (name, milliseconds) in costs.Systems.Take(5))
            lines.Add($"  {N(milliseconds),8} ms  {name}");

        foreach (var (name, milliseconds) in costs.Phases.Take(3))
            lines.Add($"  {N(milliseconds),8} ms  render: {name}");

        foreach (var pass in costs.Passes.Take(5))
            lines.Add($"  {N(pass.GpuMilliseconds ?? 0d),8} ms gpu {N(pass.CpuMilliseconds ?? 0d),8} ms cpu  {pass.Name}");

        lines.Add(string.Join(' ', new[]
        {
            $"frames={costs.Frames}",
            $"frame_ms={N(costs.FrameMilliseconds)}",
            $"schedule_ms={N(costs.ScheduleMilliseconds)}",
            $"managed_ms={N(costs.ManagedMilliseconds)}",
            $"managed_only_ms={N(costs.ManagedOnlyMilliseconds)}",
            $"bevy_ms={N(costs.BevyMilliseconds)}",
            $"crossings={Whole(costs.Crossings)}",
            $"crossing_ms={N(costs.CrossingMilliseconds)}",
            $"render_ms={N(costs.RenderMilliseconds ?? 0d)}",
            $"render_cpu_ms={N(costs.RenderCpuMilliseconds ?? 0d)}",
            $"render_gpu_ms={N(costs.RenderGpuMilliseconds ?? 0d)}",
            $"entities={costs.Entities}",
        }));

        return string.Join("\n", lines);
    }
}

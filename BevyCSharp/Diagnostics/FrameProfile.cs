using System.Diagnostics;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Measures what a frame holds, split where the cost of a C# engine over Bevy lies.
/// </summary>
/// <remarks>
/// <para>
/// A frame here is Bevy's schedule with managed systems inside it, and the managed systems make
/// calls back across the C ABI, which Bevy alone does not have. So a frame is split into the
/// managed systems, the crossings they make, counted and timed by the bridge, Bevy's schedule
/// around them, and the render, which runs beside the schedule and is timed per pass where the app
/// asked for it with <see cref="Config.GpuTimings"/>.
/// </para>
/// <para>
/// Off until <see cref="Start"/>, since counting costs something on every crossing. A crossing's
/// cost is a call that does nothing, timed as the measurement begins, which is the way into the
/// bridge and out, plus its own time inside, which the bridge times for one call in 32 and scales
/// up. A measured frame is a few percent slower than the same frame unmeasured.
/// <c>frame.profile</c> runs a measurement from a console or <c>bcs</c>.
/// </para>
/// </remarks>
public static class FrameProfile
{
    /// <summary>Whether a measurement is running.</summary>
    public static bool On => SystemTimes.On;

    /// <summary>What a call into the bridge and back costs with nothing done inside, in nanoseconds.</summary>
    private static double _roundTrip;

    /// <summary>Starts measuring, from nothing.</summary>
    /// <remarks>
    /// Times a call that does nothing first, before anything is counted, since the bridge's clock
    /// starts inside it and cannot see the way in and out.
    /// </remarks>
    public static void Start()
    {
        _roundTrip = RoundTrip();
        Native.Check(NativeDiagnostics.bcs_profile_enable(1), "starting the frame profile");
        SystemTimes.Start();
    }

    /// <summary>Stops measuring.</summary>
    public static void Stop()
    {
        SystemTimes.Stop();
        Native.Check(NativeDiagnostics.bcs_profile_enable(0), "stopping the frame profile");
    }

    /// <summary>What was measured since <see cref="Start"/> or the last take, which starts the count over.</summary>
    /// <param name="world">The world, for its count of entities. Only valid inside a system.</param>
    public static unsafe FrameCosts Take(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        NativeProfile counted;
        Native.Check(NativeDiagnostics.bcs_profile_take(&counted), "reading the frame profile");

        var frames = Math.Max(1UL, counted.Frames);
        List<(string Name, double Milliseconds)> systems =
        [
            .. SystemTimes.Take()
                // The dispatchers that run systems added while the app runs hold those
                // systems' time, which is counted under their own names.
                .Where(system => !system.Name.StartsWith("DynamicSystems.", StringComparison.Ordinal))
                .Select(system => (system.Name, Milliseconds: system.Ticks * 1000d / Stopwatch.Frequency / frames))
                .OrderByDescending(system => system.Milliseconds),
        ];

        var passes = Render.Timings().OrderByDescending(pass => pass.GpuMilliseconds ?? pass.CpuMilliseconds ?? 0d).ToList();
        double? cpu = passes.Any(pass => pass.CpuMilliseconds is not null) ? passes.Sum(pass => pass.CpuMilliseconds ?? 0d) : null;
        double? gpu = passes.Any(pass => pass.GpuMilliseconds is not null) ? passes.Sum(pass => pass.GpuMilliseconds ?? 0d) : null;

        static double Ms(ulong nanos, ulong count) => nanos / 1e6 / count;

        return new FrameCosts(
            (int)counted.Frames,
            Ms(counted.FrameNanos, frames),
            Ms(counted.ScheduleNanos, frames),
            Ms(counted.ManagedNanos, frames),
            counted.ManagedCalls / (double)frames,
            counted.Crossings / (double)frames,
            (counted.CrossingNanos + (counted.Crossings * _roundTrip)) / 1e6 / frames,
            counted.Renders > 0 ? counted.RenderNanos / 1e6 / counted.Renders : null,
            cpu,
            gpu,
            world.All().Length,
            systems,
            passes,
            Phases(counted.Renders));
    }

    /// <summary>The render schedule's phases since the last read, each a render's average, the costliest first.</summary>
    private static unsafe List<(string Name, double Milliseconds)> Phases(ulong renders)
    {
        if (renders == 0) return [];

        // The names first, which reads no times, then the times, which clears them.
        var names = Native.ReadText((buffer, capacity) => NativeDiagnostics.bcs_profile_phases(null, 0, buffer, capacity), "naming the render phases")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var nanos = new ulong[names.Length];
        fixed (ulong* times = nanos)
            Native.Check(NativeDiagnostics.bcs_profile_phases(times, nanos.Length, null, 0), "reading the render phases");

        return [.. names.Select((name, i) => (name, nanos[i] / 1e6 / renders)).OrderByDescending(phase => phase.Item2)];
    }

    /// <summary>Times calls that do nothing, after a few to warm them, and gives one's average.</summary>
    private static double RoundTrip()
    {
        const int Warm = 2_000;
        const int Timed = 50_000;

        for (var i = 0; i < Warm; i++) NativeDiagnostics.bcs_profile_noop();

        var started = Stopwatch.GetTimestamp();
        for (var i = 0; i < Timed; i++) NativeDiagnostics.bcs_profile_noop();
        var ticks = Stopwatch.GetTimestamp() - started;

        return ticks * 1e9 / Stopwatch.Frequency / Timed;
    }
}

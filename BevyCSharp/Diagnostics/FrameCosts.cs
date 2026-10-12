using System.Diagnostics;
using Bevy.Interop;

namespace Bevy;

/// <summary>What a span of frames spent, each figure the average for one frame.</summary>
/// <param name="Frames">How many frames it averages.</param>
/// <param name="FrameMilliseconds">A frame, from the top of one to the top of the next.</param>
/// <param name="ScheduleMilliseconds">Bevy's main schedule, from the top of <c>First</c> to the end of <c>Last</c>.</param>
/// <param name="ManagedMilliseconds">The managed systems, the calls they make into the bridge included.</param>
/// <param name="ManagedRuns">How many managed systems ran.</param>
/// <param name="Crossings">How many calls the managed systems made into the bridge.</param>
/// <param name="CrossingMilliseconds">
/// Those calls' cost, each a call that does nothing timed when the measurement began, which is the
/// way into the bridge and out, and its own time inside, sampled.
/// </param>
/// <param name="RenderMilliseconds">
/// Bevy's render schedule, first system to last, on the thread it runs on beside the main one,
/// or nothing where no renderer runs.
/// </param>
/// <param name="RenderCpuMilliseconds">The render passes on the CPU, where the app measures them.</param>
/// <param name="RenderGpuMilliseconds">The render passes on the GPU, where the app measures them.</param>
/// <param name="Entities">How many entities the world held at the end.</param>
/// <param name="Systems">The managed systems by name, the costliest first.</param>
/// <param name="Passes">The render passes by name, the costliest first.</param>
/// <param name="Phases">The render schedule's phases by name, each a render's average, the costliest first.</param>
public sealed record FrameCosts(
    int Frames,
    double FrameMilliseconds,
    double ScheduleMilliseconds,
    double ManagedMilliseconds,
    double ManagedRuns,
    double Crossings,
    double CrossingMilliseconds,
    double? RenderMilliseconds,
    double? RenderCpuMilliseconds,
    double? RenderGpuMilliseconds,
    int Entities,
    IReadOnlyList<(string Name, double Milliseconds)> Systems,
    IReadOnlyList<PassTiming> Passes,
    IReadOnlyList<(string Name, double Milliseconds)> Phases)
{
    /// <summary>The schedule less the managed systems, which is Bevy's own work and the bridge's.</summary>
    public double BevyMilliseconds => Math.Max(0d, ScheduleMilliseconds - ManagedMilliseconds);

    /// <summary>The managed systems less their calls into the bridge, which is C# alone.</summary>
    public double ManagedOnlyMilliseconds => Math.Max(0d, ManagedMilliseconds - CrossingMilliseconds);
}

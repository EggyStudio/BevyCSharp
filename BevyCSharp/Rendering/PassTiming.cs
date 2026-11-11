namespace Bevy;

/// <summary>How long one render pass took. See <see cref="Render.Timings"/>.</summary>
/// <param name="Name">The pass, as Bevy names it, or <c>shader</c> and a program's file name.</param>
/// <param name="CpuMilliseconds">How long recording it took, or null where it was not measured.</param>
/// <param name="GpuMilliseconds">
/// How long the GPU spent running it, or null where the adapter has no timestamp queries.
/// </param>
public readonly record struct PassTiming(string Name, double? CpuMilliseconds, double? GpuMilliseconds);

namespace Bevy;

/// <summary>One system stepping walks, from <see cref="Stepping.Systems"/>.</summary>
/// <param name="Stage">
/// The stage it runs in, <see cref="Stage.Last"/> for a system of <see cref="Stage.Render"/>, since
/// the two are one schedule of Bevy's.
/// </param>
/// <param name="Name">The name it was added under, or the path of a Bevy system's function.</param>
public readonly record struct SteppedSystem(Stage Stage, string Name);

namespace Bevy;

/// <summary>
/// The numbers from <see cref="Start"/> up to <see cref="End"/>, Rust's <c>Range&lt;f32&gt;</c>, as
/// Bevy's <c>VisibilityRange</c> holds the distances a mesh fades in and out over.
/// </summary>
/// <remarks>
/// Bevy reflects a range as a value of its own with no fields a path reaches, so a wrapper reads
/// and writes one whole, as its two ends.
/// </remarks>
/// <param name="Start">Where it starts.</param>
/// <param name="End">Where it ends, which Rust leaves out of the range.</param>
public readonly record struct FloatRange(float Start, float End);

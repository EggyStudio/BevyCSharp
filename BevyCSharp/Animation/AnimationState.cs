namespace Bevy;

/// <summary>What a model is playing, as <see cref="Animation.StateOf"/> reads it.</summary>
/// <param name="Clip">The clip, by name, or nothing when none is playing.</param>
/// <param name="Seconds">Seconds into it.</param>
/// <param name="Speed">How fast it plays.</param>
/// <param name="Paused">Whether it is held where it is.</param>
/// <param name="Finished">Whether a clip that plays once has reached its end.</param>
/// <param name="Completions">How many times a repeating clip has come round to its start.</param>
public readonly record struct AnimationState(
    string? Clip, float Seconds, float Speed, bool Paused, bool Finished, uint Completions);

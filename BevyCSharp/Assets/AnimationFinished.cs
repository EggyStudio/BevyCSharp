namespace Bevy;

/// <summary>
/// A clip that plays once reached its end, posted as a message the frame after it did.
/// </summary>
/// <param name="Scene">The entity the model's scene was spawned under, as it was played on.</param>
/// <param name="Clip">The clip, by name.</param>
public readonly record struct AnimationFinished(Entity Scene, string Clip);

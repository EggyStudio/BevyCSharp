namespace Bevy.Physics;

/// <summary>Two bodies that were touching stopped, or one of them was removed.</summary>
/// <param name="A">One of the two entities.</param>
/// <param name="B">The other.</param>
public readonly record struct ContactEnded(Entity A, Entity B);

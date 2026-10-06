namespace Bevy.Physics;

/// <summary>Two bodies started touching.</summary>
/// <remarks>
/// Sent on the message bus the step they first touch, once for the pair, whichever of them moved.
/// A sensor sends this for what enters it without pushing it, which is how a trigger volume works.
/// </remarks>
/// <param name="A">One of the two entities.</param>
/// <param name="B">The other.</param>
public readonly record struct ContactStarted(Entity A, Entity B);

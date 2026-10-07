namespace Bevy.Physics;

/// <summary>Two bodies started touching.</summary>
/// <remarks>
/// Sent on the message bus the step they first touch, once for the pair, whichever of them moved.
/// A sensor sends this for what enters it without pushing it, which is how a trigger volume works.
/// </remarks>
/// <param name="A">One of the two entities.</param>
/// <param name="B">The other.</param>
/// <param name="Point">Where they touched first, in the world, at their deepest contact.</param>
/// <param name="Normal">The contact's normal, from <paramref name="B"/> toward <paramref name="A"/>, the way <paramref name="A"/> is pushed.</param>
/// <param name="Speed">
/// How fast they closed along the normal as they met, in units a second, which says how hard they
/// hit, for the loudness of a sound or the damage done. Two bodies that came to rest against each
/// other met at nearly nothing. Read as they approach as well as as they touch, since the solver
/// slows a pair in the step before it touches.
/// </param>
public readonly record struct ContactStarted(Entity A, Entity B, Vec3 Point = default, Vec3 Normal = default, float Speed = 0f);

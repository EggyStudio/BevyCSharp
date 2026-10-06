namespace Bevy.Physics;

/// <summary>How a body's surface behaves where it touches another.</summary>
/// <param name="Friction">
/// How hard it is to slide, from zero for ice upward. Two bodies touching slide against each other
/// with the geometric mean of their frictions, so ice on rubber is still slippery.
/// </param>
/// <param name="Bounce">
/// How much of its speed into a surface it keeps coming back out, from zero, which lands and stays,
/// to one, which bounces about as high as it fell. Two bodies touching bounce as the bouncier does.
/// </param>
/// <remarks>
/// Bepu has no restitution coefficient, and its contacts are springs integrated stiffly enough to
/// stay stable, which takes nearly all of a bounce's speed out at a game's step rate. So a bounce
/// is given after the step instead. A bouncy body that met a surface moving into it faster than a
/// fifth of a unit a second leaves it at <paramref name="Bounce"/> times that speed, along the
/// surface's normal, keeping the speed it has along the surface. Measured against the surface as
/// though it held still, which is right for a floor or a wall and close for anything slower than
/// the body.
/// </remarks>
public readonly record struct PhysicsMaterial(float Friction = 1f, float Bounce = 0f);

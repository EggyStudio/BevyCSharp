namespace Bevy;

/// <summary>How hard a finger or a stylus presses, as Bevy's <c>ForceTouch</c> holds it.</summary>
/// <param name="Force">
/// How hard, from zero to one where the platform scales it, and in its own measure where it gives
/// <paramref name="MaxPossibleForce"/>, one being an average touch.
/// </param>
/// <param name="MaxPossibleForce">The most force the platform reports, or null where the force is scaled from zero to one.</param>
/// <param name="AltitudeAngle">A stylus's angle from the screen, in radians, a half pi standing straight up, or null.</param>
public readonly record struct TouchForce(float Force, float? MaxPossibleForce, float? AltitudeAngle);

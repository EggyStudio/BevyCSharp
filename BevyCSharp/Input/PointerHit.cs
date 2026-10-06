namespace Bevy;

/// <summary>Where a pointer met what it is over, as Bevy's <c>HitData</c>.</summary>
/// <param name="Camera">The camera it was seen through.</param>
/// <param name="Depth">
/// How far into the view it is, nearer being less, for ordering what it met.
/// </param>
/// <param name="Position">
/// Where it met it, in the world for a mesh or a sprite, or nothing where the backend does not say,
/// as the interface's does not.
/// </param>
/// <param name="Normal">
/// Which way the surface it met faces there, or nothing where the backend does not say.
/// </param>
public readonly record struct PointerHit(Entity Camera, float Depth, Vec3? Position, Vec3? Normal);

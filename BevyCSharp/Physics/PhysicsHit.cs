namespace Bevy.Physics;

/// <summary>Where a ray met a body.</summary>
/// <param name="Entity">The entity whose body it met.</param>
/// <param name="Point">Where, in world space.</param>
/// <param name="Normal">Which way the surface faces there.</param>
/// <param name="Distance">How far along the ray, in world units.</param>
public readonly record struct PhysicsHit(Entity Entity, Vec3 Point, Vec3 Normal, float Distance);

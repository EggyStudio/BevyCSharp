namespace Bevy.Physics;

/// <summary>What a <see cref="Collider"/> comes to on its entity, in world units.</summary>
/// <param name="Shape">The shape, ready for <see cref="PhysicsWorld.Add"/>.</param>
/// <param name="Kind">Which shape it is.</param>
/// <param name="Size">Its full size along each of the entity's axes, after the entity's scale.</param>
/// <param name="Center">Where its middle is from the entity's origin, in the entity's turned frame.</param>
public readonly record struct ColliderFit(PhysicsShape Shape, ColliderShape Kind, Vec3 Size, Vec3 Center);

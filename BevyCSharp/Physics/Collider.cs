namespace Bevy.Physics;

/// <summary>The shape a <see cref="RigidBody"/> collides as.</summary>
/// <remarks>
/// <para>
/// Sized in the entity's own units and scaled with its transform, so a cube stretched into a wall
/// has a wall's collider. A size left at zero takes the bounds of the mesh the entity is drawn
/// with, and of a unit cube where it is drawn with none, so putting a collider on what the editor
/// spawned fits it without a number typed. <see cref="Colliders.TryFit"/> says what it comes to.
/// </para>
/// <para>
/// <see cref="ColliderShape.Hull"/> and <see cref="ColliderShape.Mesh"/> are the drawn meshes
/// themselves, the entity's and those of the entities under it, placed as they are under it, so a
/// collider on the entity that places a model takes the model's shape. They wait until there is
/// one and every one has loaded, which for a model is once its parts have been spawned, and what
/// is spawned under the entity after its body is made does not change the body.
/// </para>
/// </remarks>
[Behavior]
public partial struct Collider
{
    /// <summary>What it collides as.</summary>
    public ColliderShape Shape;

    /// <summary>Its full size along each axis before the entity's scale, or zero to fit the mesh.</summary>
    [Tooltip("Zero fits what the entity is drawn with. A sphere is as wide as the widest side, and a capsule or a cylinder stands along Y.")]
    public Vec3 Size;

    /// <summary>How far its middle is from the entity's origin, before the entity's scale.</summary>
    public Vec3 Offset;
}

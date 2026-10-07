namespace Bevy.Physics;

/// <summary>The shape a <see cref="Collider"/> collides as.</summary>
public enum ColliderShape
{
    /// <summary>A box.</summary>
    Box,

    /// <summary>A ball as wide as the collider's widest side.</summary>
    Sphere,

    /// <summary>A cylinder with rounded ends, standing along Y, as a character is.</summary>
    Capsule,

    /// <summary>A cylinder standing along Y.</summary>
    Cylinder,

    /// <summary>The tightest convex shape around the mesh the entity is drawn with.</summary>
    Hull,

    /// <summary>
    /// The triangles of the mesh the entity is drawn with, for a static body such as a level's
    /// floor, since Bepu collides a moving body with a mesh only from outside.
    /// </summary>
    Mesh,
}

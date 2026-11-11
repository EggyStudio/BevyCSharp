namespace Bevy;

/// <summary>A rotation and then a translation in the plane, Bevy's <c>Isometry2d</c>, which places a shape.</summary>
/// <param name="Translation">Where the shape's center goes.</param>
/// <param name="Rotation">How it is turned about its center.</param>
/// <remarks>
/// What a 2D shape's bounds are taken under, as a transform places an entity, without a scale,
/// since a bounding volume of a scaled shape is the scaled shape's own.
/// </remarks>
public readonly record struct Isometry2d(Vec2 Translation, Rot2 Rotation)
{
    /// <summary>A placement where the shape is turned by its transform's rotation about Z and moved to its translation's X and Y, as Bevy's examples place theirs.</summary>
    public static Isometry2d FromTransform(Transform transform)
    {
        // The angle about Z, the third of the rotation's Euler angles in the order YXZ, as Bevy's
        // examples read it, which for a turn about Z alone is that turn.
        var q = transform.Rotation;
        var angle = MathF.Atan2(2f * (q.X * q.Y + q.W * q.Z), 1f - 2f * (q.X * q.X + q.Z * q.Z));
        return new Isometry2d(new Vec2(transform.Translation.X, transform.Translation.Y), Rot2.Radians(angle));
    }

    /// <summary>A point of the shape where the placement puts it.</summary>
    public static Vec2 operator *(Isometry2d isometry, Vec2 point) => isometry.Rotation * point + isometry.Translation;
}

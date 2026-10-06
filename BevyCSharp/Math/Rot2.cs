namespace Bevy;

/// <summary>A rotation in the plane, Bevy's <c>Rot2</c>, held as the cosine and sine of its angle.</summary>
/// <param name="Cos">The cosine of the angle.</param>
/// <param name="Sin">The sine of the angle.</param>
/// <remarks>Counterclockwise is positive, as Bevy's 2D world turns with Y up.</remarks>
public readonly record struct Rot2(float Cos, float Sin)
{
    /// <summary>No rotation.</summary>
    public static Rot2 Identity => new(1f, 0f);

    /// <summary>A rotation by an angle, in radians, counterclockwise.</summary>
    public static Rot2 Radians(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>The angle, in radians, from minus pi to pi.</summary>
    public float AsRadians => MathF.Atan2(Sin, Cos);

    /// <summary>Turns a vector by the rotation.</summary>
    public static Vec2 operator *(Rot2 rotation, Vec2 v) =>
        new(rotation.Cos * v.X - rotation.Sin * v.Y, rotation.Sin * v.X + rotation.Cos * v.Y);
}

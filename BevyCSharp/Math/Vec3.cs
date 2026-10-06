using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>A three-component vector, laid out exactly as Bevy's <c>Vec3</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vec3 : IEquatable<Vec3>, System.Numerics.IAdditionOperators<Vec3, Vec3, Vec3>, System.Numerics.IMultiplyOperators<Vec3, float, Vec3>
{
    /// <summary>The X component.</summary>
    public float X;

    /// <summary>The Y component.</summary>
    public float Y;

    /// <summary>The Z component.</summary>
    public float Z;

    /// <summary>Creates a vector from its components.</summary>
    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Creates a vector with every component set to <paramref name="value"/>.</summary>
    public Vec3(float value) : this(value, value, value)
    {
    }

    /// <summary>All zeroes.</summary>
    public static Vec3 Zero => default;

    /// <summary>All ones, which is the identity scale.</summary>
    public static Vec3 One => new(1f);

    /// <summary>One unit along X.</summary>
    public static Vec3 UnitX => new(1f, 0f, 0f);

    /// <summary>One unit along Y.</summary>
    public static Vec3 UnitY => new(0f, 1f, 0f);

    /// <summary>One unit along Z.</summary>
    public static Vec3 UnitZ => new(0f, 0f, 1f);

    /// <summary>The vector's length.</summary>
    public readonly float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>The squared length, which avoids the square root.</summary>
    public readonly float LengthSquared => X * X + Y * Y + Z * Z;

    /// <summary>
    /// The vector scaled to unit length, or <see cref="UnitZ"/> if it has no length to scale.
    /// </summary>
    /// <remarks>
    /// The fallback keeps a degenerate basis from producing NaNs that then spread through a
    /// rotation and out into the world, which is far harder to trace back than a wrong axis.
    /// </remarks>
    public readonly Vec3 Normalized
    {
        get
        {
            var length = Length;
            return length > 0f ? this * (1f / length) : UnitZ;
        }
    }

    /// <summary>The dot product.</summary>
    public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>The cross product, perpendicular to both operands.</summary>
    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>Adds two vectors.</summary>
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Subtracts one vector from another.</summary>
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Negates a vector.</summary>
    public static Vec3 operator -(Vec3 v) => new(-v.X, -v.Y, -v.Z);

    /// <summary>Scales a vector.</summary>
    public static Vec3 operator *(Vec3 v, float scale) => new(v.X * scale, v.Y * scale, v.Z * scale);

    /// <summary>Scales a vector.</summary>
    public static Vec3 operator *(float scale, Vec3 v) => v * scale;

    /// <inheritdoc/>
    public readonly bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is Vec3 other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>Compares two vectors.</summary>
    public static bool operator ==(Vec3 a, Vec3 b) => a.Equals(b);

    /// <summary>Compares two vectors.</summary>
    public static bool operator !=(Vec3 a, Vec3 b) => !a.Equals(b);

    /// <inheritdoc/>
    public readonly override string ToString() => $"({X}, {Y}, {Z})";
}

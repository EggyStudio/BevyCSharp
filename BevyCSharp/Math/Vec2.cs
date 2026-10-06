using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>A two-component vector, laid out exactly as Bevy's <c>Vec2</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vec2 : IEquatable<Vec2>, System.Numerics.IAdditionOperators<Vec2, Vec2, Vec2>, System.Numerics.IMultiplyOperators<Vec2, float, Vec2>
{
    /// <summary>The X component.</summary>
    public float X;

    /// <summary>The Y component.</summary>
    public float Y;

    /// <summary>Creates a vector from its components.</summary>
    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Creates a vector with both components set to <paramref name="value"/>.</summary>
    public Vec2(float value) : this(value, value)
    {
    }

    /// <summary>Both zero.</summary>
    public static Vec2 Zero => default;

    /// <summary>Both one.</summary>
    public static Vec2 One => new(1f);

    /// <summary>The vector's length.</summary>
    public readonly float Length => MathF.Sqrt(X * X + Y * Y);

    /// <summary>The length squared, which compares lengths without a square root.</summary>
    public readonly float LengthSquared => X * X + Y * Y;

    /// <summary>The same direction at length one, or zero for a vector with no length.</summary>
    public readonly Vec2 Normalized
    {
        get
        {
            var length = Length;
            return length > 0f ? new Vec2(X / length, Y / length) : Zero;
        }
    }

    /// <summary>The dot product.</summary>
    public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>
    /// The dot product of <paramref name="a"/> turned a quarter turn counterclockwise with
    /// <paramref name="b"/>, Bevy's <c>perp_dot</c>, positive where <paramref name="b"/> lies counterclockwise of <paramref name="a"/>.
    /// </summary>
    public static float PerpDot(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>The smaller of each component.</summary>
    public static Vec2 Min(Vec2 a, Vec2 b) => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));

    /// <summary>The larger of each component.</summary>
    public static Vec2 Max(Vec2 a, Vec2 b) => new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));

    /// <summary>Each component held between its bounds.</summary>
    public static Vec2 Clamp(Vec2 v, Vec2 min, Vec2 max) => new(Math.Clamp(v.X, min.X, max.X), Math.Clamp(v.Y, min.Y, max.Y));

    /// <summary>The vector pointing the other way.</summary>
    public static Vec2 operator -(Vec2 v) => new(-v.X, -v.Y);

    /// <summary>Scales a vector.</summary>
    public static Vec2 operator *(float scale, Vec2 v) => v * scale;

    /// <summary>Divides each component.</summary>
    public static Vec2 operator /(Vec2 v, float divisor) => new(v.X / divisor, v.Y / divisor);

    /// <summary>Adds two vectors.</summary>
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Subtracts one vector from another.</summary>
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>Scales a vector.</summary>
    public static Vec2 operator *(Vec2 v, float scale) => new(v.X * scale, v.Y * scale);

    /// <inheritdoc/>
    public readonly bool Equals(Vec2 other) => X == other.X && Y == other.Y;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is Vec2 other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Compares two vectors.</summary>
    public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);

    /// <summary>Compares two vectors.</summary>
    public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

    /// <inheritdoc/>
    public readonly override string ToString() => $"({X}, {Y})";
}

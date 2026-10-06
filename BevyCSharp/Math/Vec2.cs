using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>A two-component vector, laid out exactly as Bevy's <c>Vec2</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vec2 : IEquatable<Vec2>
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

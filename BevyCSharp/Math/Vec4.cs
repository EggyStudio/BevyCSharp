using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>A four-component vector, laid out exactly as Bevy's <c>Vec4</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vec4 : IEquatable<Vec4>
{
    /// <summary>The X component.</summary>
    public float X;

    /// <summary>The Y component.</summary>
    public float Y;

    /// <summary>The Z component.</summary>
    public float Z;

    /// <summary>The W component.</summary>
    public float W;

    /// <summary>Creates a vector from its components.</summary>
    public Vec4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>All zeroes.</summary>
    public static Vec4 Zero => default;

    /// <inheritdoc/>
    public readonly bool Equals(Vec4 other) =>
        X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is Vec4 other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <summary>Compares two vectors.</summary>
    public static bool operator ==(Vec4 a, Vec4 b) => a.Equals(b);

    /// <summary>Compares two vectors.</summary>
    public static bool operator !=(Vec4 a, Vec4 b) => !a.Equals(b);

    /// <inheritdoc/>
    public readonly override string ToString() => $"({X}, {Y}, {Z}, {W})";
}

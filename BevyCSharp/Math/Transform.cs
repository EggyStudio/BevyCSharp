using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>A three-component vector, laid out exactly as Bevy's <c>Vec3</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vec3 : IEquatable<Vec3>
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

/// <summary>
/// A rotation quaternion, laid out exactly as Bevy's <c>Quat</c>.
/// </summary>
/// <remarks>
/// Sixteen bytes of X, Y, Z, W. Bevy's is SIMD-backed on most targets and therefore sixteen-byte
/// aligned, which is what pads <see cref="Transform"/> out past the size its fields suggest.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Quat : IEquatable<Quat>
{
    /// <summary>The X component of the vector part.</summary>
    public float X;

    /// <summary>The Y component of the vector part.</summary>
    public float Y;

    /// <summary>The Z component of the vector part.</summary>
    public float Z;

    /// <summary>The scalar part.</summary>
    public float W;

    /// <summary>Creates a quaternion from its components, without normalising.</summary>
    public Quat(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>The rotation that does nothing.</summary>
    public static Quat Identity => new(0f, 0f, 0f, 1f);

    /// <summary>A rotation of <paramref name="radians"/> about an arbitrary axis.</summary>
    /// <param name="axis">The axis to turn about. Should be unit length.</param>
    /// <param name="radians">The angle in radians.</param>
    public static Quat FromAxisAngle(Vec3 axis, float radians)
    {
        var half = radians * 0.5f;
        var sin = MathF.Sin(half);
        return new Quat(axis.X * sin, axis.Y * sin, axis.Z * sin, MathF.Cos(half));
    }

    /// <summary>A rotation about the X axis.</summary>
    public static Quat FromRotationX(float radians) => FromAxisAngle(Vec3.UnitX, radians);

    /// <summary>A rotation about the Y axis.</summary>
    public static Quat FromRotationY(float radians) => FromAxisAngle(Vec3.UnitY, radians);

    /// <summary>A rotation about the Z axis.</summary>
    public static Quat FromRotationZ(float radians) => FromAxisAngle(Vec3.UnitZ, radians);

    /// <inheritdoc/>
    public readonly bool Equals(Quat other) =>
        X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is Quat other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <summary>Compares two quaternions.</summary>
    public static bool operator ==(Quat a, Quat b) => a.Equals(b);

    /// <summary>Compares two quaternions.</summary>
    public static bool operator !=(Quat a, Quat b) => !a.Equals(b);

    /// <inheritdoc/>
    public readonly override string ToString() => $"({X}, {Y}, {Z}, {W})";
}

/// <summary>
/// Where an entity sits: position, rotation and scale, relative to its parent.
/// </summary>
/// <remarks>
/// <para>
/// This is Bevy's own <c>Transform</c>, not a copy kept in sync. Writing to one through a query
/// updates the component Bevy's own systems read, so transform propagation, rendering and
/// anything else built on it all see the change.
/// </para>
/// <para>
/// The two padding fields are not decoration. Bevy's <c>Quat</c> is SIMD-backed and sixteen-byte
/// aligned, so the real struct places rotation at offset 16 and runs to 48 bytes rather than the
/// 40 its fields add up to. <see cref="NativeComponents"/> checks that against the engine at
/// startup, so a layout that drifts fails loudly instead of corrupting memory.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Transform
{
    /// <summary>Position relative to the parent.</summary>
    public Vec3 Translation;

    private readonly float _paddingAfterTranslation;

    /// <summary>Rotation relative to the parent.</summary>
    public Quat Rotation;

    /// <summary>Scale relative to the parent.</summary>
    public Vec3 Scale;

    private readonly float _paddingAfterScale;

    /// <summary>The size Bevy gives this struct, which the padding above accounts for.</summary>
    internal const int NativeSize = 48;

    /// <summary>Creates a transform at <paramref name="translation"/> with no rotation.</summary>
    public Transform(Vec3 translation)
    {
        Translation = translation;
        Rotation = Quat.Identity;
        Scale = Vec3.One;
        _paddingAfterTranslation = 0f;
        _paddingAfterScale = 0f;
    }

    /// <summary>Creates a transform from all three parts.</summary>
    public Transform(Vec3 translation, Quat rotation, Vec3 scale)
    {
        Translation = translation;
        Rotation = rotation;
        Scale = scale;
        _paddingAfterTranslation = 0f;
        _paddingAfterScale = 0f;
    }

    /// <summary>The identity transform: at the origin, unrotated, unscaled.</summary>
    public static Transform Identity => new(Vec3.Zero, Quat.Identity, Vec3.One);

    /// <summary>A transform at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>.</summary>
    public static Transform At(float x, float y, float z) => new(new Vec3(x, y, z));

    /// <inheritdoc/>
    public readonly override string ToString() =>
        $"Transform(at {Translation}, scale {Scale})";
}

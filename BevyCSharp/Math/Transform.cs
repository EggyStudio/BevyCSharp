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

    /// <summary>
    /// The rotation whose local axes are <paramref name="x"/>, <paramref name="y"/> and
    /// <paramref name="z"/>.
    /// </summary>
    /// <remarks>
    /// The three vectors are the columns of a rotation matrix, so they must be unit length and
    /// mutually perpendicular; scale and shear are not representable as a quaternion and are not
    /// removed here. Which of the four branches runs is decided by the largest diagonal term,
    /// because the others divide by something near zero for that matrix and lose most of their
    /// precision to cancellation.
    /// </remarks>
    public static Quat FromBasis(Vec3 x, Vec3 y, Vec3 z)
    {
        var trace = x.X + y.Y + z.Z;

        if (trace > 0f)
        {
            var s = MathF.Sqrt(trace + 1f) * 2f;
            return new Quat(
                (y.Z - z.Y) / s, (z.X - x.Z) / s, (x.Y - y.X) / s,
                0.25f * s);
        }

        if (x.X > y.Y && x.X > z.Z)
        {
            var s = MathF.Sqrt(1f + x.X - y.Y - z.Z) * 2f;
            return new Quat(
                0.25f * s, (y.X + x.Y) / s, (z.X + x.Z) / s,
                (y.Z - z.Y) / s);
        }

        if (y.Y > z.Z)
        {
            var s = MathF.Sqrt(1f + y.Y - x.X - z.Z) * 2f;
            return new Quat(
                (y.X + x.Y) / s, 0.25f * s, (z.Y + y.Z) / s,
                (z.X - x.Z) / s);
        }

        var t = MathF.Sqrt(1f + z.Z - x.X - y.Y) * 2f;
        return new Quat(
            (z.X + x.Z) / t, (z.Y + y.Z) / t, 0.25f * t,
            (x.Y - y.X) / t);
    }

    /// <summary>
    /// Combines two rotations, applying <paramref name="b"/> first and then <paramref name="a"/>.
    /// </summary>
    public static Quat operator *(Quat a, Quat b) => new(
        a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
        a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
        a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
        a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

    /// <summary>Turns a point by this rotation.</summary>
    /// <remarks>
    /// The usual expansion of <c>q v q*</c>, which is a handful of multiplications rather than
    /// building a matrix for one point. Rotating a direction and rotating a position are the same
    /// operation here, because a rotation has no translation to add.
    /// </remarks>
    public static Vec3 operator *(Quat rotation, Vec3 point)
    {
        var axis = new Vec3(rotation.X, rotation.Y, rotation.Z);
        var scaled = Vec3.Cross(axis, point) + (point * rotation.W);

        return point + (Vec3.Cross(axis, scaled) * 2f);
    }

    /// <summary>
    /// The rotation that rolls about Z, then pitches about X, then turns about Y, in radians.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Y outermost, which is what an editor wants and why every editor does it. One of the three
    /// angles has to be the middle one, and a middle angle only spans half a turn: past a quarter
    /// turn its neighbours have to jump to a half turn to describe the rest. Standing that angle
    /// up is the difference between a thing spinning on the spot reading 0, 120, 240 and reading
    /// 180, 60, 180 — the same rotation, and unreadable.
    /// </para>
    /// <para>
    /// So Y, which is what a thing standing on the ground turns about, gets the full circle, and X
    /// is the one clamped to a quarter turn either way — where looking straight up or down is, and
    /// where the other two stop being separable. <see cref="ToEuler"/> is the inverse.
    /// </para>
    /// </remarks>
    public static Quat FromEuler(float x, float y, float z) =>
        FromRotationY(y) * FromRotationX(x) * FromRotationZ(z);

    /// <summary>
    /// The turns about X, Y and Z this rotation is made of, in radians.
    /// </summary>
    /// <remarks>
    /// The inverse of <see cref="FromEuler"/>. A rotation has more than one decomposition, so what
    /// comes back is the one with the pitch between straight down and straight up; at either pole
    /// the turn and the roll describe the same thing and the turn is given all of it.
    /// </remarks>
    public readonly Vec3 ToEuler()
    {
        // Read off the rotation's matrix rather than out of the quaternion's terms directly. The
        // three entries each angle needs are named here, which is the only way this stays checkable
        // against the order `FromEuler` builds in.
        var xx = X * X;
        var yy = Y * Y;
        var zz = Z * Z;

        var m13 = 2f * ((X * Z) + (W * Y));
        var m21 = 2f * ((X * Y) + (W * Z));
        var m22 = 1f - (2f * (xx + zz));
        var m23 = 2f * ((Y * Z) - (W * X));
        var m31 = 2f * ((X * Z) - (W * Y));
        var m33 = 1f - (2f * (xx + yy));
        var m11 = 1f - (2f * (yy + zz));

        var pitch = MathF.Asin(Math.Clamp(-m23, -1f, 1f));

        // At the pole the turn and the roll are the same turn about the same line, and only their
        // sum is a fact. It is given to the turn, because that is the one an editor's first box
        // shows and the one that stays continuous as something spins.
        if (MathF.Abs(m23) >= 0.999999f)
        {
            return new Vec3(pitch, MathF.Atan2(-m31, m11), 0f);
        }

        return new Vec3(pitch, MathF.Atan2(m13, m33), MathF.Atan2(m21, m22));
    }

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
/// The field order and padding here reproduce Bevy's memory layout exactly, which is not the
/// order its source declares. <see cref="NativeComponents"/> checks every offset against the
/// engine the first time the component is resolved, so a layout that drifts fails loudly rather
/// than reading each value from the wrong place.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Transform : INativeComponent
{
    /// <summary>Rotation relative to the parent.</summary>
    public Quat Rotation;

    /// <summary>Position relative to the parent.</summary>
    public Vec3 Translation;

    /// <summary>Scale relative to the parent.</summary>
    public Vec3 Scale;

    /// <summary>
    /// The engine's name for this component, which is what makes the ordinary generic API land on
    /// Bevy's own <c>Transform</c> rather than register a second one that merely shares the name.
    /// </summary>
    /// <remarks>
    /// Implemented explicitly: it answers a question about the type, and nothing holding a
    /// transform wants it on the value's surface.
    /// </remarks>
    readonly string INativeComponent.NativeName => "Transform";

    private readonly float _tailPaddingA;
    private readonly float _tailPaddingB;

    /// <summary>The layout Bevy gives this struct, which the field order above reproduces.</summary>
    /// <remarks>
    /// Rotation first is not a stylistic choice. Bevy's <c>Transform</c> uses Rust's default
    /// representation, which permits the compiler to reorder fields, and it does: the
    /// sixteen-byte-aligned <c>Quat</c> is moved ahead of the two vectors to save padding.
    /// Declaring the fields in source order here would compile, pass a size check, and read
    /// every value from the wrong place.
    /// </remarks>
    internal const int NativeSize = 48;

    /// <summary>Offset Bevy places the rotation at.</summary>
    internal const int RotationOffset = 0;

    /// <summary>Offset Bevy places the translation at.</summary>
    internal const int TranslationOffset = 16;

    /// <summary>Offset Bevy places the scale at.</summary>
    internal const int ScaleOffset = 28;

    /// <summary>Creates a transform at <paramref name="translation"/> with no rotation.</summary>
    public Transform(Vec3 translation)
    {
        Rotation = Quat.Identity;
        Translation = translation;
        Scale = Vec3.One;
        _tailPaddingA = 0f;
        _tailPaddingB = 0f;
    }

    /// <summary>Creates a transform from all three parts.</summary>
    public Transform(Vec3 translation, Quat rotation, Vec3 scale)
    {
        Rotation = rotation;
        Translation = translation;
        Scale = scale;
        _tailPaddingA = 0f;
        _tailPaddingB = 0f;
    }

    /// <summary>The identity transform: at the origin, unrotated, unscaled.</summary>
    public static Transform Identity => new(Vec3.Zero, Quat.Identity, Vec3.One);

    /// <summary>A transform at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>.</summary>
    public static Transform At(float x, float y, float z) => new(new Vec3(x, y, z));

    /// <summary>
    /// A transform at <paramref name="eye"/> oriented so that its forward axis points at
    /// <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// Forward is negative Z, matching Bevy's convention, so this is what aims a camera or a
    /// directional light. The maths is done here rather than in the engine because it needs no
    /// world state, and a call across the boundary for arithmetic would be waste.
    /// </remarks>
    /// <param name="eye">Where the transform sits.</param>
    /// <param name="target">What it points at.</param>
    /// <param name="up">Which way is up, used to settle the roll.</param>
    public static Transform LookingAt(Vec3 eye, Vec3 target, Vec3 up)
    {
        var back = (eye - target).Normalized;         // negative Z, so back rather than forward
        var right = Vec3.Cross(up, back).Normalized;
        var trueUp = Vec3.Cross(back, right);

        return new Transform(eye, Quat.FromBasis(right, trueUp, back), Vec3.One);
    }

    /// <inheritdoc/>
    public readonly override string ToString() =>
        $"Transform(at {Translation}, scale {Scale})";
}

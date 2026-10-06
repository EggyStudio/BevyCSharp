using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// A rotation quaternion, laid out exactly as Bevy's <c>Quat</c>.
/// </summary>
/// <remarks>
/// Sixteen bytes of X, Y, Z, W. Bevy's is SIMD-backed on most targets and therefore sixteen-byte
/// aligned, which pads <see cref="Transform"/> out past the size its fields suggest.
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

    /// <summary>Creates a quaternion from its components, without normalizing.</summary>
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

    /// <summary>
    /// The rotation that undoes this one.
    /// </summary>
    /// <remarks>
    /// The conjugate, which is the inverse for a rotation, since a rotation is a unit quaternion.
    /// It answers how one orientation differs from another. <c>b * a.Conjugate</c> is the turn that
    /// takes <c>a</c> to <c>b</c>, which applying somebody's drag to a second thing needs.
    /// </remarks>
    public Quat Conjugate => new(-X, -Y, -Z, W);

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
    /// Y outermost, as an editor needs and every editor does. One of the three angles has to be the
    /// middle one, and a middle angle only spans half a turn. Past a quarter turn its neighbors
    /// have to jump to a half turn to describe the rest. Standing that angle up is the difference
    /// between a thing spinning on the spot reading 0, 120, 240 and reading 180, 60, 180, which is
    /// the same rotation and unreadable.
    /// </para>
    /// <para>
    /// So Y, which a thing standing on the ground turns about, gets the full circle, and X is the
    /// one clamped to a quarter turn either way, which is where looking straight up or down is and
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

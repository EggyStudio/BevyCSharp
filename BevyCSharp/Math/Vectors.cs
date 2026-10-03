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

/// <summary>
/// A color as linear red, green, blue and alpha, laid out as Bevy's <c>LinearRgba</c>.
/// </summary>
/// <remarks>
/// <para>
/// Linear rather than sRGB, because light adds up in linear and the renderer works in it, so a value
/// held here is the one Bevy draws with, unconverted. A color somebody picked from a
/// palette or typed as hex is sRGB, which <see cref="FromSrgb"/> converts, and
/// <see cref="ToSrgb"/> converts back for showing it.
/// </para>
/// <para>
/// A component field of this type is drawn as a swatch and written to a scene as four linear
/// numbers. One of Bevy's own <c>Color</c> fields, which can hold a color in any of ten spaces,
/// reads as this whatever space it holds, converted by Bevy.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Color : IEquatable<Color>
{
    /// <summary>Red, linear.</summary>
    public float R;

    /// <summary>Green, linear.</summary>
    public float G;

    /// <summary>Blue, linear.</summary>
    public float B;

    /// <summary>Alpha, where one is opaque.</summary>
    public float A;

    /// <summary>Creates a color from linear components.</summary>
    public Color(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>Opaque white.</summary>
    public static Color White => new(1f, 1f, 1f);

    /// <summary>Opaque black.</summary>
    public static Color Black => new(0f, 0f, 0f);

    /// <summary>Nothing at all, which is black with no alpha.</summary>
    public static Color Transparent => default;

    /// <summary>A color from sRGB components between zero and one, as a color picker gives them.</summary>
    public static Color FromSrgb(float r, float g, float b, float a = 1f) =>
        new(Linear(r), Linear(g), Linear(b), a);

    /// <summary>
    /// A color from an sRGB hex string such as <c>#ff8800</c> or <c>ff8800cc</c>.
    /// </summary>
    /// <exception cref="FormatException">The text is not six or eight hex digits.</exception>
    public static Color FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        var digits = hex.StartsWith('#') ? hex[1..] : hex;
        if (digits.Length is not (6 or 8)
            || !uint.TryParse(digits, System.Globalization.NumberStyles.HexNumber, null, out var packed))
            throw new FormatException($"'{hex}' is not a color in hex, such as #ff8800.");

        if (digits.Length == 6) packed = (packed << 8) | 0xFF;
        return FromSrgb(
            ((packed >> 24) & 0xFF) / 255f,
            ((packed >> 16) & 0xFF) / 255f,
            ((packed >> 8) & 0xFF) / 255f,
            (packed & 0xFF) / 255f);
    }

    /// <summary>The color as sRGB components between zero and one, alpha unchanged.</summary>
    public readonly Vec4 ToSrgb() => new(Gamma(R), Gamma(G), Gamma(B), A);

    /// <summary>The color with another alpha.</summary>
    public readonly Color WithAlpha(float alpha) => new(R, G, B, alpha);

    // The sRGB transfer function, as Bevy's own Srgba applies it, so a color converted on this side
    // and one Bevy converted agree.

    private static float Linear(float value) => value <= 0.04045f
        ? value / 12.92f
        : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);

    private static float Gamma(float value) => value <= 0.0031308f
        ? value * 12.92f
        : (1.055f * MathF.Pow(value, 1f / 2.4f)) - 0.055f;

    /// <inheritdoc/>
    public readonly bool Equals(Color other) =>
        R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is Color other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <summary>Compares two colors.</summary>
    public static bool operator ==(Color a, Color b) => a.Equals(b);

    /// <summary>Compares two colors.</summary>
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    /// <inheritdoc/>
    public readonly override string ToString() => $"Color({R}, {G}, {B}, {A})";
}

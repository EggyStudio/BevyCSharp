using System.Globalization;

namespace Bevy;

/// <summary>A point or a way of a level file, in the file's units with Z up, as the file writes it.</summary>
/// <param name="X">Along the file's X.</param>
/// <param name="Y">Along the file's Y.</param>
/// <param name="Z">Along the file's Z, which is up.</param>
/// <remarks>
/// Kept in double precision, as TrenchBroom keeps a face's points, since a brush's corners are
/// found where its planes meet and a plane through points read in single precision moves a corner
/// by as much as the digits it dropped. <see cref="Meters"/> turns it into the engine's meters with
/// Y up.
/// </remarks>
public readonly record struct LevelPoint(double X, double Y, double Z)
{
    /// <summary>The point in meters with Y up, the file's (x, y, z) at (x, z, -y) and divided by its units a meter.</summary>
    /// <param name="unitsPerMeter">How many of the file's units make a meter, <see cref="Level.UnitsPerMeter"/>.</param>
    /// <remarks>
    /// A turn of a quarter about X, which keeps the file's right-handed axes right-handed, so a face
    /// wound one way in the file is wound the same way in the engine.
    /// </remarks>
    public Vec3 Meters(double unitsPerMeter) => new((float)(X / unitsPerMeter), (float)(Z / unitsPerMeter), (float)(-Y / unitsPerMeter));

    /// <summary>The point a place in meters with Y up stands at in a file of the given units a meter.</summary>
    /// <param name="meters">The place, Y up.</param>
    /// <param name="unitsPerMeter">How many of the file's units make a meter.</param>
    public static LevelPoint FromMeters(Vec3 meters, double unitsPerMeter) =>
        new(meters.X * unitsPerMeter, -meters.Z * unitsPerMeter, meters.Y * unitsPerMeter);

    /// <summary>The three numbers as a level file writes them, apart by spaces.</summary>
    public override string ToString() => $"{Number(X)} {Number(Y)} {Number(Z)}";

    /// <summary>The sum of two points or ways.</summary>
    public static LevelPoint operator +(LevelPoint a, LevelPoint b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>The way from the second point to the first.</summary>
    public static LevelPoint operator -(LevelPoint a, LevelPoint b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>A way scaled.</summary>
    public static LevelPoint operator *(LevelPoint a, double s) => new(a.X * s, a.Y * s, a.Z * s);

    internal static double Dot(LevelPoint a, LevelPoint b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    internal static LevelPoint Cross(LevelPoint a, LevelPoint b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    internal double Length => Math.Sqrt(Dot(this, this));

    /// <summary>A number as a level file writes it, the shortest text that reads back as the same double.</summary>
    internal static string Number(double value) => value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
}

namespace Bevy;

/// <summary>
/// A face of a brush in a level file, the plane through three points and the material the face is
/// drawn with, mapped onto it along two axes of the world, as the Valve 220 map format writes it.
/// </summary>
/// <param name="First">The plane's first point.</param>
/// <param name="Second">Its second point.</param>
/// <param name="Third">Its third point.</param>
/// <param name="Material">The material, a name with no spaces, which the face is drawn with as a material slot of that name.</param>
/// <param name="UAxis">The way across the world the material's width runs.</param>
/// <param name="UOffset">How many texels the material is moved along its width.</param>
/// <param name="VAxis">The way its height runs.</param>
/// <param name="VOffset">How many texels it is moved along its height.</param>
/// <param name="Rotation">The turn in degrees the axes were given, which an editor shows and the axes hold turned.</param>
/// <param name="ScaleU">How many of the file's units a texel spans along the width.</param>
/// <param name="ScaleV">How many it spans along the height.</param>
/// <remarks>
/// <para>
/// A brush is the solid behind every one of its faces' planes. The three points run clockwise
/// seen from outside the brush, as Quake's tools and TrenchBroom write them, so the plane's normal,
/// the second point's way to the first crossed with its way to the third, points out of the brush.
/// </para>
/// <para>
/// A point on the face falls at texel (p · UAxis / ScaleU + UOffset, p · VAxis / ScaleV + VOffset)
/// of the material, so a face moved across the world slides under a material that stays where the
/// world puts it, the world-aligned mapping a level is blocked out in. The rotation is kept for the
/// editor that wrote it, since the mapping reads the axes it turned.
/// </para>
/// </remarks>
public sealed record LevelFace(
    LevelPoint First,
    LevelPoint Second,
    LevelPoint Third,
    string Material,
    LevelPoint UAxis,
    double UOffset,
    LevelPoint VAxis,
    double VOffset,
    double Rotation,
    double ScaleU,
    double ScaleV)
{
    /// <summary>The way out of the brush, a length of one, or zero where the three points lie on one line.</summary>
    public LevelPoint Normal
    {
        get
        {
            var normal = LevelPoint.Cross(First - Second, Third - Second);
            var length = normal.Length;
            return length > 0 ? normal * (1 / length) : default;
        }
    }

    /// <summary>How far the plane stands from the file's origin along its normal.</summary>
    public double Distance => LevelPoint.Dot(Normal, Second);
}

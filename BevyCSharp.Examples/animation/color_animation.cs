// Bevy's color_animation example, examples/animation/color_animation.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Vector4 = System.Numerics.Vector4;

namespace BevyCSharp.Examples.Animations;

// Shows how the same four colors, white, yellow, red and black, change in between when animated in
// different color spaces: the top three along a cubic curve in linear RGB, XYZ and Oklab, the
// bottom three mixed a pair at a time in HSL, sRGB and Oklch.
//
// Bevy's colors are its own types with the space each belongs to, and the conversions between
// them are written here as Bevy writes them, each four numbers in its space.
internal static class ColorAnimation
{
    public static void Build(App app) => app.Startup(Setup, "color_animation.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        Vector4[] colors = [new(1f, 1f, 1f, 1f), new(1f, 1f, 0f, 1f), new(1f, 0f, 0f, 1f), new(0f, 0f, 0f, 1f)];
        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);

        foreach (var (space, y) in new[] { (ColorSpace.LinearRgb, 275f), (ColorSpace.Xyz, 175f), (ColorSpace.Oklab, 75f), (ColorSpace.Hsl, -75f), (ColorSpace.Srgb, -175f), (ColorSpace.Oklch, -275f) })
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, Transform.At(0f, y, 0f));
            Render2d.SetSprite(ecs, sprite, white, new SpriteSettings { Size = (75f, 75f) });

            // The four colors in the sprite's own space, as Bevy maps them into it before spawning.
            var points = new InlineList4<Vec4>();
            foreach (var color in colors)
            {
                var inSpace = From(space, color);
                points.Add(new Vec4(inSpace.X, inSpace.Y, inSpace.Z, inSpace.W));
            }

            if (space is ColorSpace.LinearRgb or ColorSpace.Xyz or ColorSpace.Oklab) ecs.Add(sprite, new Curve { Space = space, Points = points });
            else ecs.Add(sprite, new Mixed { Space = space, Colors = points });
        }
    }

    // How far along the colors the sprites are, from zero to one and back as a sine.
    internal static float Progress(float elapsed) => (MathF.Sin(elapsed) + 1f) / 2f;

    // A sprite given a color in a space, and moved across by how far along it is.
    internal static void Paint(BehaviorContext ctx, ref Transform transform, ColorSpace space, Vector4 color, float t)
    {
        var linear = To(space, color);
        ctx.Ecs.Wrap<SpriteRef>(ctx.Entity).Color = new Color(linear.X, linear.Y, linear.Z, linear.W);
        transform.Translation.X = 600f * (t - 0.5f);
    }

    // One of the four as the vector the arithmetic here is done in, the package's Vec4 being a value
    // to keep on an entity, with no arithmetic of its own.
    internal static Vector4 At(InlineList4<Vec4> points, int index)
    {
        var point = points.ItemAt(index);
        return new Vector4(point.X, point.Y, point.Z, point.W);
    }

    // One cubic Bézier segment through the four, every channel alike, as Bevy's CubicBezier is.
    internal static Vector4 Bezier(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, float t)
    {
        var u = 1f - t;
        return p0 * (u * u * u) + p1 * (3f * u * u * t) + p2 * (3f * u * t * t) + p3 * (t * t * t);
    }

    // The two colors either side of where the time is, mixed by how far between them it is, as
    // Bevy's Mix is in each space, a hue turning the short way around.
    internal static Vector4 Mix(ColorSpace space, Vector4[] colors, float t)
    {
        var intervals = colors.Length - 1f;
        var start = MathF.Min(MathF.Floor(t * intervals), intervals - 1f);
        var local = t * intervals - start;
        var (a, b) = (colors[(int)start], colors[(int)start + 1]);
        var mixed = a + (b - a) * local;

        switch (space)
        {
            case ColorSpace.Hsl: return mixed with { X = LerpHue(a.X, b.X, local) };
            case ColorSpace.Oklch: return mixed with { Z = LerpHue(a.Z, b.Z, local) };
            default: return mixed;
        }
    }

    private static float LerpHue(float a, float b, float t)
    {
        var difference = (b - a) % 360f;
        if (difference > 180f) difference -= 360f;
        else if (difference < -180f) difference += 360f;
        return ((a + difference * t) % 360f + 360f) % 360f;
    }

    // A linear RGBA color in a space.
    internal static Vector4 From(ColorSpace space, Vector4 linear) => space switch
    {
        ColorSpace.LinearRgb => linear,
        ColorSpace.Xyz => new Vector4(
            0.4124564f * linear.X + 0.3575761f * linear.Y + 0.1804375f * linear.Z,
            0.2126729f * linear.X + 0.7151522f * linear.Y + 0.0721750f * linear.Z,
            0.0193339f * linear.X + 0.1191920f * linear.Y + 0.9503041f * linear.Z,
            linear.W),
        ColorSpace.Oklab => ToOklab(linear),
        ColorSpace.Oklch => ToOklch(ToOklab(linear)),
        ColorSpace.Srgb => ToSrgb(linear),
        _ => ToHsl(ToSrgb(linear)),
    };

    // A color in a space as linear RGBA.
    internal static Vector4 To(ColorSpace space, Vector4 color) => space switch
    {
        ColorSpace.LinearRgb => color,
        ColorSpace.Xyz => new Vector4(
            3.2404542f * color.X - 1.5371385f * color.Y - 0.4985314f * color.Z,
            -0.9692660f * color.X + 1.8760108f * color.Y + 0.0415560f * color.Z,
            0.0556434f * color.X - 0.2040259f * color.Y + 1.0572252f * color.Z,
            color.W),
        ColorSpace.Oklab => FromOklab(color),
        ColorSpace.Oklch => FromOklab(FromOklch(color)),
        ColorSpace.Srgb => FromSrgb(color),
        _ => FromSrgb(FromHsl(color)),
    };

    private static Vector4 ToOklab(Vector4 c)
    {
        var l = MathF.Cbrt(0.4122214708f * c.X + 0.5363325363f * c.Y + 0.0514459929f * c.Z);
        var m = MathF.Cbrt(0.2119034982f * c.X + 0.6806995451f * c.Y + 0.1073969566f * c.Z);
        var s = MathF.Cbrt(0.0883024619f * c.X + 0.2817188376f * c.Y + 0.6299787005f * c.Z);
        return new Vector4(
            0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
            1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
            0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s,
            c.W);
    }

    private static Vector4 FromOklab(Vector4 c)
    {
        var l = MathF.Pow(c.X + 0.3963377774f * c.Y + 0.2158037573f * c.Z, 3f);
        var m = MathF.Pow(c.X - 0.1055613458f * c.Y - 0.0638541728f * c.Z, 3f);
        var s = MathF.Pow(c.X - 0.0894841775f * c.Y - 1.2914855480f * c.Z, 3f);
        return new Vector4(
            4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
            -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
            -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s,
            c.W);
    }

    private static Vector4 ToOklch(Vector4 lab)
    {
        var chroma = MathF.Sqrt(lab.Y * lab.Y + lab.Z * lab.Z);
        var hue = MathF.Atan2(lab.Z, lab.Y) * 180f / MathF.PI;
        return new Vector4(lab.X, chroma, hue < 0f ? hue + 360f : hue, lab.W);
    }

    private static Vector4 FromOklch(Vector4 lch)
    {
        var radians = lch.Z * MathF.PI / 180f;
        return new Vector4(lch.X, lch.Y * MathF.Cos(radians), lch.Y * MathF.Sin(radians), lch.W);
    }

    private static Vector4 ToSrgb(Vector4 linear)
    {
        var srgb = new Color(linear.X, linear.Y, linear.Z, linear.W).ToSrgb();
        return new Vector4(srgb.X, srgb.Y, srgb.Z, linear.W);
    }

    private static Vector4 FromSrgb(Vector4 srgb)
    {
        var linear = Color.FromSrgb(srgb.X, srgb.Y, srgb.Z, srgb.W);
        return new Vector4(linear.R, linear.G, linear.B, srgb.W);
    }

    // Hue in degrees, saturation and lightness, from sRGB.
    private static Vector4 ToHsl(Vector4 c)
    {
        var (max, min) = (MathF.Max(c.X, MathF.Max(c.Y, c.Z)), MathF.Min(c.X, MathF.Min(c.Y, c.Z)));
        var lightness = (max + min) / 2f;
        var chroma = max - min;
        if (chroma == 0f) return new Vector4(0f, 0f, lightness, c.W);

        var hue = max == c.X ? 60f * (((c.Y - c.Z) / chroma) % 6f)
            : max == c.Y ? 60f * ((c.Z - c.X) / chroma + 2f)
            : 60f * ((c.X - c.Y) / chroma + 4f);
        var saturation = chroma / (1f - MathF.Abs(2f * lightness - 1f));
        return new Vector4(hue < 0f ? hue + 360f : hue, saturation, lightness, c.W);
    }

    private static Vector4 FromHsl(Vector4 hsl)
    {
        var chroma = (1f - MathF.Abs(2f * hsl.Z - 1f)) * hsl.Y;
        var h = hsl.X / 60f;
        var x = chroma * (1f - MathF.Abs(h % 2f - 1f));
        var (r, g, b) = h switch
        {
            < 1f => (chroma, x, 0f),
            < 2f => (x, chroma, 0f),
            < 3f => (0f, chroma, x),
            < 4f => (0f, x, chroma),
            < 5f => (x, 0f, chroma),
            _ => (chroma, 0f, x),
        };
        var m = hsl.Z - chroma / 2f;
        return new Vector4(r + m, g + m, b + m, hsl.W);
    }
}

/// <summary>The color spaces the sprites are animated in.</summary>
public enum ColorSpace : byte
{
    /// <summary>Linear RGB, the space the renderer blends in.</summary>
    LinearRgb,

    /// <summary>CIE XYZ.</summary>
    Xyz,

    /// <summary>Oklab, made so equal steps look equal.</summary>
    Oklab,

    /// <summary>Hue, saturation and lightness, from sRGB.</summary>
    Hsl,

    /// <summary>sRGB, the space a screen shows.</summary>
    Srgb,

    /// <summary>Oklab's lightness, chroma and hue.</summary>
    Oklch,
}

/// <summary>
/// A sprite whose color moves along a cubic Bézier curve through four colors in a space, Bevy's
/// <c>Curve</c> of a color type, the type held here as the space.
/// </summary>
[Behavior]
public partial struct Curve
{
    /// <summary>The space the curve runs through.</summary>
    public ColorSpace Space;

    /// <summary>The curve's four control points, each a color in the space.</summary>
    public InlineList4<Vec4> Points;

    /// <summary>Colored by where along the curve the time is, and moved across as far.</summary>
    [OnUpdate]
    public void AnimateCurve(BehaviorContext ctx, ref Transform transform)
    {
        var t = ColorAnimation.Progress(ctx.Time.Elapsed);
        var color = ColorAnimation.Bezier(ColorAnimation.At(Points, 0), ColorAnimation.At(Points, 1), ColorAnimation.At(Points, 2), ColorAnimation.At(Points, 3), t);
        ColorAnimation.Paint(ctx, ref transform, Space, color, t);
    }
}

/// <summary>
/// A sprite whose color is mixed from the two of four colors in a space either side of where the
/// time is, Bevy's <c>Mixed</c> of a color type, the type held here as the space.
/// </summary>
[Behavior]
public partial struct Mixed
{
    /// <summary>The space the colors are mixed in.</summary>
    public ColorSpace Space;

    /// <summary>The four colors, each in the space.</summary>
    public InlineList4<Vec4> Colors;

    /// <summary>Colored by mixing the two colors either side of the time, and moved across as far.</summary>
    [OnUpdate]
    public void AnimateMixed(BehaviorContext ctx, ref Transform transform)
    {
        var t = ColorAnimation.Progress(ctx.Time.Elapsed);
        var color = ColorAnimation.Mix(Space, [ColorAnimation.At(Colors, 0), ColorAnimation.At(Colors, 1), ColorAnimation.At(Colors, 2), ColorAnimation.At(Colors, 3)], t);
        ColorAnimation.Paint(ctx, ref transform, Space, color, t);
    }
}

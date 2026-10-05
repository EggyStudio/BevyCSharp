using Bevy;
using Bevy.Reflected;
using Vec4 = System.Numerics.Vector4;

namespace BevyCSharp.Examples.Animations;

// Shows how the same four colors, white, yellow, red and black, change in between when animated in
// different color spaces: the top three along a cubic curve in linear RGB, XYZ and Oklab, the
// bottom three mixed a pair at a time in HSL, sRGB and Oklch.
//
// Bevy's colors are its own types with the space each belongs to, and the conversions between
// them are written here as Bevy writes them, each four numbers in its space.
internal static class ColorAnimation
{
    private enum Space { LinearRgb, Xyz, Oklab, Hsl, Srgb, Oklch }

    private static readonly List<(Entity Sprite, Space Space, Vec4[] Points)> Sprites = [];

    public static void Build(App app)
    {
        app.Startup(Setup, "color_animation.Setup");
        app.Update(Animate, "color_animation.Animate");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Sprites.Clear();
        Render2d.SpawnCamera2d();

        Vec4[] colors = [new(1f, 1f, 1f, 1f), new(1f, 1f, 0f, 1f), new(1f, 0f, 0f, 1f), new(0f, 0f, 0f, 1f)];
        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);

        foreach (var (space, y) in new[] { (Space.LinearRgb, 275f), (Space.Xyz, 175f), (Space.Oklab, 75f), (Space.Hsl, -75f), (Space.Srgb, -175f), (Space.Oklch, -275f) })
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, Transform.At(0f, y, 0f));
            Render2d.SetSprite(ecs, sprite, white, new SpriteSettings { Size = (75f, 75f) });
            Sprites.Add((sprite, space, [.. colors.Select(color => From(space, color))]));
        }
    }

    private static void Animate(BehaviorContext ctx)
    {
        var t = (MathF.Sin(ctx.Time.Elapsed) + 1f) / 2f;
        foreach (var (sprite, space, points) in Sprites)
        {
            var color = space switch
            {
                Space.LinearRgb or Space.Xyz or Space.Oklab => Bezier(points, t),
                _ => Mixed(space, points, t),
            };
            var linear = To(space, color);
            ctx.Ecs.Wrap<SpriteRef>(sprite).Color = new Color(linear.X, linear.Y, linear.Z, linear.W);
            ctx.Ecs.Set(sprite, Transform.At(600f * (t - 0.5f), ctx.Ecs.GetOrDefault<Transform>(sprite).Translation.Y, 0f));
        }
    }

    // One cubic Bézier segment through the four, every channel alike, as Bevy's CubicBezier is.
    private static Vec4 Bezier(Vec4[] p, float t)
    {
        var u = 1f - t;
        return p[0] * (u * u * u) + p[1] * (3f * u * u * t) + p[2] * (3f * u * t * t) + p[3] * (t * t * t);
    }

    // The two colors either side of where the time is, mixed by how far between them it is, as
    // Bevy's Mix is in each space, a hue turning the short way around.
    private static Vec4 Mixed(Space space, Vec4[] colors, float t)
    {
        var intervals = colors.Length - 1f;
        var start = MathF.Min(MathF.Floor(t * intervals), intervals - 1f);
        var local = t * intervals - start;
        var (a, b) = (colors[(int)start], colors[(int)start + 1]);
        var mixed = a + (b - a) * local;

        switch (space)
        {
            case Space.Hsl: return mixed with { X = LerpHue(a.X, b.X, local) };
            case Space.Oklch: return mixed with { Z = LerpHue(a.Z, b.Z, local) };
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
    private static Vec4 From(Space space, Vec4 linear) => space switch
    {
        Space.LinearRgb => linear,
        Space.Xyz => new Vec4(
            0.4124564f * linear.X + 0.3575761f * linear.Y + 0.1804375f * linear.Z,
            0.2126729f * linear.X + 0.7151522f * linear.Y + 0.0721750f * linear.Z,
            0.0193339f * linear.X + 0.1191920f * linear.Y + 0.9503041f * linear.Z,
            linear.W),
        Space.Oklab => ToOklab(linear),
        Space.Oklch => ToOklch(ToOklab(linear)),
        Space.Srgb => ToSrgb(linear),
        _ => ToHsl(ToSrgb(linear)),
    };

    // A color in a space as linear RGBA.
    private static Vec4 To(Space space, Vec4 color) => space switch
    {
        Space.LinearRgb => color,
        Space.Xyz => new Vec4(
            3.2404542f * color.X - 1.5371385f * color.Y - 0.4985314f * color.Z,
            -0.9692660f * color.X + 1.8760108f * color.Y + 0.0415560f * color.Z,
            0.0556434f * color.X - 0.2040259f * color.Y + 1.0572252f * color.Z,
            color.W),
        Space.Oklab => FromOklab(color),
        Space.Oklch => FromOklab(FromOklch(color)),
        Space.Srgb => FromSrgb(color),
        _ => FromSrgb(FromHsl(color)),
    };

    private static Vec4 ToOklab(Vec4 c)
    {
        var l = MathF.Cbrt(0.4122214708f * c.X + 0.5363325363f * c.Y + 0.0514459929f * c.Z);
        var m = MathF.Cbrt(0.2119034982f * c.X + 0.6806995451f * c.Y + 0.1073969566f * c.Z);
        var s = MathF.Cbrt(0.0883024619f * c.X + 0.2817188376f * c.Y + 0.6299787005f * c.Z);
        return new Vec4(
            0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
            1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
            0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s,
            c.W);
    }

    private static Vec4 FromOklab(Vec4 c)
    {
        var l = MathF.Pow(c.X + 0.3963377774f * c.Y + 0.2158037573f * c.Z, 3f);
        var m = MathF.Pow(c.X - 0.1055613458f * c.Y - 0.0638541728f * c.Z, 3f);
        var s = MathF.Pow(c.X - 0.0894841775f * c.Y - 1.2914855480f * c.Z, 3f);
        return new Vec4(
            4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
            -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
            -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s,
            c.W);
    }

    private static Vec4 ToOklch(Vec4 lab)
    {
        var chroma = MathF.Sqrt(lab.Y * lab.Y + lab.Z * lab.Z);
        var hue = MathF.Atan2(lab.Z, lab.Y) * 180f / MathF.PI;
        return new Vec4(lab.X, chroma, hue < 0f ? hue + 360f : hue, lab.W);
    }

    private static Vec4 FromOklch(Vec4 lch)
    {
        var radians = lch.Z * MathF.PI / 180f;
        return new Vec4(lch.X, lch.Y * MathF.Cos(radians), lch.Y * MathF.Sin(radians), lch.W);
    }

    private static Vec4 ToSrgb(Vec4 linear)
    {
        var srgb = new Color(linear.X, linear.Y, linear.Z, linear.W).ToSrgb();
        return new Vec4(srgb.X, srgb.Y, srgb.Z, linear.W);
    }

    private static Vec4 FromSrgb(Vec4 srgb)
    {
        var linear = Color.FromSrgb(srgb.X, srgb.Y, srgb.Z, srgb.W);
        return new Vec4(linear.R, linear.G, linear.B, srgb.W);
    }

    // Hue in degrees, saturation and lightness, from sRGB.
    private static Vec4 ToHsl(Vec4 c)
    {
        var (max, min) = (MathF.Max(c.X, MathF.Max(c.Y, c.Z)), MathF.Min(c.X, MathF.Min(c.Y, c.Z)));
        var lightness = (max + min) / 2f;
        var chroma = max - min;
        if (chroma == 0f) return new Vec4(0f, 0f, lightness, c.W);

        var hue = max == c.X ? 60f * (((c.Y - c.Z) / chroma) % 6f)
            : max == c.Y ? 60f * ((c.Z - c.X) / chroma + 2f)
            : 60f * ((c.X - c.Y) / chroma + 4f);
        var saturation = chroma / (1f - MathF.Abs(2f * lightness - 1f));
        return new Vec4(hue < 0f ? hue + 360f : hue, saturation, lightness, c.W);
    }

    private static Vec4 FromHsl(Vec4 hsl)
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
        return new Vec4(r + m, g + m, b + m, hsl.W);
    }
}

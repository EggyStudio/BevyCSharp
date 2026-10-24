using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A camera's look (<see cref="Render.SetLook"/>), SHARED.md's ramp drawn through it and held to a
/// model of it worked out here, and the model held to the figures 3DEngine's own tests hold its
/// curve to, so a look with the same numbers shows the same picture in both engines.
/// </summary>
[Collection("engine")]
public sealed class LookTests
{
    // The curve 3DEngine's block game is graded by, at lights of 0.05, 0.1, 0.25, 0.5, 1, 2 and 4,
    // as its TonemapCurveTests read it, levels shown from 0 to 1.
    [Theory]
    [InlineData(0.05, 0.230)]
    [InlineData(0.1, 0.317)]
    [InlineData(0.25, 0.475)]
    [InlineData(0.5, 0.620)]
    [InlineData(1.0, 0.763)]
    [InlineData(2.0, 0.874)]
    [InlineData(4.0, 0.941)]
    public void TheNumberedCurveReads3DEnginesFigures(double light, double shown)
    {
        var bent = Numbered(light, 0.9, 1.3, 1.05, 1.0);
        Assert.InRange(Encode(bent), shown - 0.001, shown + 0.001);
    }

    [SkippableTheory]
    [InlineData("knee")]
    [InlineData("held")]
    [InlineData("vivid")]
    [InlineData("graded")]
    public void TheRampThroughALookIsTheModels(string name)
    {
        Needs.Shaders();
        var look = Named(name);
        var drawn = Ramp(look);

        var worst = (Difference: 0, X: 0u, Y: 0u, Model: (R: (byte)0, G: (byte)0, B: (byte)0));
        for (var y = 0u; y < drawn.Height; y++)
        {
            for (var x = 0u; x < drawn.Width; x++)
            {
                var v = Math.Pow(2, (20.0 * x / 1023.0) - 12.0);
                var (r, g, b) = Rows[Math.Min(y, 7)];
                var model = Shown(look, r * v, g * v, b * v);
                var (dr, dg, db, _) = drawn.At(x, y);
                var difference = Math.Max(Math.Abs(dr - model.R), Math.Max(Math.Abs(dg - model.G), Math.Abs(db - model.B)));
                if (difference > worst.Difference) worst = (difference, x, y, model);
            }
        }

        Assert.True(
            worst.Difference <= 2,
            $"the {name} look drew the ramp {worst.Difference} levels from the model at ({worst.X}, {worst.Y}), "
                + $"{drawn.At(worst.X, worst.Y)} where {worst.Model} is worked out.");
    }

    [Fact]
    public void TheVividLookDeepensTheSkysBlue()
    {
        // A middle gray and 3DEngine's noon zenith through the vivid look, the gray a little
        // brighter and the blue deeper, as its block game shows them.
        var vivid = LookSettings.Vivid();
        var gray = Shown(vivid, 0.18, 0.18, 0.18);
        var zenith = Shown(vivid, Decode(78 / 255.0), Decode(134 / 255.0), Decode(222 / 255.0));
        Assert.InRange(gray.R, 125, 133);
        Assert.True(zenith.B - zenith.R > 222 - 78, $"the zenith {zenith} is no deeper than (78, 134, 222)");
    }

    [SkippableFact]
    public void ACameraInEightBitsKeepsItsTonemapper()
    {
        Needs.Shaders();
        // Gray by the look, which a camera with no linear picture past white leaves to Bevy's
        // tonemapper, its red row staying red.
        var drawn = Ramp(new LookSettings { Saturation = 0f }, hdr: false);
        var (r, g, b, _) = drawn.At(600, 1);
        Assert.True(r - Math.Max(g, b) > 100, $"the red row reads {r}, {g}, {b}");
    }

    [SkippableFact]
    public void TakingTheLookOffGivesThePictureBackToTheTonemapper()
    {
        Needs.Shaders();
        var camera = Entity.None;
        var run = RampRun(new LookSettings { Saturation = 0f }, hdr: true, spawned: c => camera = c);
        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady() && Looks.All().All(l => l.Ready))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("gray")
            .Do("taking the look off", _ => Render.SetLook(camera, null))
            .Wait(10)
            .Capture("back")
            .Go();

        var gray = run.Picture("gray").At(600, 1);
        var back = run.Picture("back").At(600, 1);
        Assert.True(Math.Abs(gray.R - gray.G) <= 2, $"the red row under a gray look reads {gray}");
        Assert.True(back.R - Math.Max(back.G, back.B) > 100, $"the red row with the look off reads {back}");
    }

    [Fact]
    public void ALookThatIsNotANumberIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetLook(default, new LookSettings { Saturation = float.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetLook(default, new LookSettings { CurveWhite = float.PositiveInfinity }));
    }

    // The ramp's rows, gray, red, green and blue alone, cyan, magenta and yellow, and the value with
    // half of it and a quarter of it.
    private static readonly (double R, double G, double B)[] Rows =
    [
        (1, 1, 1), (1, 0, 0), (0, 1, 0), (0, 0, 1), (0, 1, 1), (1, 0, 1), (1, 1, 0), (1, 0.5, 0.25),
    ];

    private static LookSettings Named(string name) => name switch
    {
        "knee" => new LookSettings(),
        "held" => new LookSettings { KneeStrength = 0f },
        "vivid" => LookSettings.Vivid(),
        _ => new LookSettings { Exposure = 2f, Saturation = 0.5f, Contrast = 1.3f, Tint = (1f, 0.9f, 0.8f) },
    };

    // SHARED.md's ramp drawn through a look with no dither, on a camera in high dynamic range or not.
    private static CapturedImage Ramp(LookSettings look, bool hdr = true)
    {
        var run = RampRun(look, hdr);
        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady() && Looks.All().All(l => !hdr || l.Ready))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("ramp")
            .Go();
        return run.Picture("ramp");
    }

    private static PictureRun RampRun(LookSettings look, bool hdr, Action<Entity>? spawned = null) => new()
    {
        Width = 1024,
        Height = 8,
        Scene = ecs =>
        {
            var camera = PictureRun.Camera(ecs);
            Render.SetPostProcessing(camera, new PostSettings { Hdr = hdr, Tonemapper = Tonemapper.TonyMcMapface, Dither = false, Msaa = 1 });
            var plane = ecs.Spawn();
            Render.SetMesh(ecs, plane, Render.CreateMesh(MeshShape.Plane, 2000f, 2000f));
            Render.SetMaterial(ecs, plane, Shaders.CreateMaterial(Shaders.CreateProgram("shaders/tonemap_ramp.slang")));
            ecs.Add(plane, new Transform(Vec3.Zero, Quat.FromRotationX(MathF.PI / 2f), Vec3.One));
            look.Dither = false;
            Render.SetLook(camera, look);
            spawned?.Invoke(camera);
        },
    };

    // The look worked out as its pass works it out, from a linear light to the levels shown.
    private static (byte R, byte G, byte B) Shown(LookSettings look, double r, double g, double b)
    {
        double[] light = [Math.Max(r, 0) * look.Exposure, Math.Max(g, 0) * look.Exposure, Math.Max(b, 0) * look.Exposure];
        var bent = look.Curve == LookCurve.Numbered
            ? light.Select(c => Numbered(c, look.CurveExposure, look.CurveWhite, look.CurveLower, look.CurveUpper)).ToArray()
            : Knee(light, look.Knee, look.KneeStrength);

        var luminance = (0.2126 * bent[0]) + (0.7152 * bent[1]) + (0.0722 * bent[2]);
        double[] tint = [look.Tint.R, look.Tint.G, look.Tint.B];
        var shown = new byte[3];
        for (var i = 0; i < 3; i++)
        {
            var graded = Math.Max((luminance + ((bent[i] - luminance) * look.Saturation)) * tint[i], 0);
            var level = Math.Clamp(((Encode(graded) - 0.5) * look.Contrast) + 0.5, 0, 1);
            shown[i] = (byte)Math.Round(level * 255);
        }
        return (shown[0], shown[1], shown[2]);
    }

    // The curve with four numbers, decoded so the encoding after shows it at a gamma of 2.2.
    private static double Numbered(double c, double exposure, double white, double lower, double upper)
    {
        c = Math.Max(c, 0) * exposure;
        c /= Math.Pow(Math.Pow(c, white) + 1, 1 / white);
        c = Math.Pow(c, lower + ((upper - lower) * Math.Sqrt(c)));
        return Decode(Math.Pow(c, 1 / 2.2));
    }

    // The engine's curve, the brightest channel held under a knee of 0.9 with its hue, mixed by a
    // strength with each channel past the knee bent on its own.
    private static double[] Knee(double[] c, double knee, double strength)
    {
        static double Bend(double v, double at) => at + ((1 - at) * (1 - Math.Exp(-(v - at) / (1 - at))));
        var peak = c.Max();
        var held = peak <= 0.9 ? c : c.Select(v => v * (Bend(peak, 0.9) / peak)).ToArray();
        if (strength <= 0 || knee >= 1) return held;
        return c.Select((v, i) => held[i] + (((v > knee ? Bend(v, knee) : v) - held[i]) * strength)).ToArray();
    }

    private static double Decode(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double Encode(double c)
    {
        c = Math.Clamp(c, 0, 1);
        return c <= 0.0031308 ? c * 12.92 : (1.055 * Math.Pow(c, 1 / 2.4)) - 0.055;
    }
}

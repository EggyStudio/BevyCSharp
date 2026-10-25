using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>The sky behind the atmosphere <see cref="Render.SetNightSky"/> draws, read off pictures of the sky.</summary>
[Collection("engine")]
public sealed class NightSkyTests
{
    [SkippableFact]
    public void TheStarsShineAtNight()
    {
        Needs.Shaders();
        var clock = new SkyClock { Hour = 0f };
        var bare = Sky(null, clock);
        var starry = Sky(new NightSkySettings { Clock = clock, Moon = 0f }, clock);
        Assert.Equal(0, Bright(bare));
        Assert.True(Bright(starry) > 10, $"{Bright(starry)} stars shine over the night");
    }

    [SkippableFact]
    public void TheDaysSkyDrownsThem()
    {
        Needs.Shaders();
        var clock = new SkyClock { Hour = 12f };
        var bare = Sky(null, clock, atmosphere: true);
        var starry = Sky(new NightSkySettings { Clock = clock, Moon = 0f }, clock, atmosphere: true);
        var worst = 0;
        for (var y = 0u; y < bare.Height; y += 2)
            for (var x = 0u; x < bare.Width; x += 2)
            {
                var (a, b) = (bare.At(x, y), starry.At(x, y));
                worst = Math.Max(worst, Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B))));
            }
        Assert.True(worst <= 3, $"the stars show by day by {worst} levels");
    }

    [SkippableFact]
    public void AHalfMoonIsLitOnTheSunsSide()
    {
        Needs.Shaders();
        // A first quarter moon, a quarter of a month old, high in the evening, six degrees wide.
        var clock = new SkyClock { DayOfYear = 79f, Hour = 19f, MoonAgeAtStart = (SkyClock.SynodicMonth / 4f) - 79f };
        var settings = new NightSkySettings { Clock = clock, Stars = 0f, Galaxy = 0f, MoonSize = 6f };
        var picture = Sky(settings, clock, towardMoon: true);

        // Half the moon's radius toward the sun and away from it, along the sun's way across the picture.
        var forward = clock.MoonDirection;
        var right = Vec3.Cross(forward, Vec3.UnitY).Normalized;
        var up = Vec3.Cross(right, forward);
        var (across, down) = (Vec3.Dot(clock.SunDirection, right), -Vec3.Dot(clock.SunDirection, up));
        var length = MathF.Sqrt((across * across) + (down * down));
        var pixels = 1.5f / (30f / picture.Height);
        var (cx, cy) = (picture.Width / 2f, picture.Height / 2f);
        var lit = Luma(picture.At((uint)(cx + (across / length * pixels)), (uint)(cy + (down / length * pixels))));
        var dark = Luma(picture.At((uint)(cx - (across / length * pixels)), (uint)(cy - (down / length * pixels))));
        Assert.True(lit > dark + 60, $"the moon reads {lit:0} toward the sun and {dark:0} away from it");
    }

    [Fact]
    public void ASkyOutOfRangeIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetNightSky(default, new NightSkySettings { Stars = -1f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetNightSky(default, new NightSkySettings { StarGrid = 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetNightSky(default, new NightSkySettings { MoonSize = float.NaN }));
    }

    private static double Luma((byte R, byte G, byte B, byte A) pixel) => (0.2126 * pixel.R) + (0.7152 * pixel.G) + (0.0722 * pixel.B);

    private static int Bright(CapturedImage picture)
    {
        var count = 0;
        for (var y = 0u; y < picture.Height; y++)
            for (var x = 0u; x < picture.Width; x++)
                if (Luma(picture.At(x, y)) > 60) count++;
        return count;
    }

    // The sky from two meters up, looking sixty degrees up toward the south, or at the moon, under
    // an atmosphere lit by the clock's sun or under none, with the night sky or none.
    private static CapturedImage Sky(NightSkySettings? settings, SkyClock clock, bool atmosphere = false, bool towardMoon = false)
    {
        var run = new PictureRun
        {
            Width = 320,
            Height = 240,
            Frames = 1200,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = towardMoon ? 30f : 60f });
                var eye = new Vec3(0f, 2f, 0f);
                var toward = towardMoon ? clock.MoonDirection : new Vec3(0f, MathF.Sin(MathF.PI / 3f), MathF.Cos(MathF.PI / 3f));
                ecs.Add(camera, Transform.LookingAt(eye, eye + toward, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1, Tonemapper = Tonemapper.None });
                if (atmosphere)
                {
                    Render.SetAtmosphere(camera, new AtmosphereSettings());
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = false });
                    ecs.Add(sun, Transform.LookingAt(clock.SunDirection, Vec3.Zero, Vec3.UnitY));
                }
                if (settings is not null) Render.SetNightSky(camera, settings);
            },
        };
        run.Wait(10)
            .Until("compiled", _ => NightSky.Ready())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(20)
            .Capture("sky")
            .Go();
        return run.Picture("sky");
    }
}

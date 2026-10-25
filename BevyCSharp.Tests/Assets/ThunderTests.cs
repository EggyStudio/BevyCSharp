using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The thunderstorm <see cref="Render.SetThunder"/> brings, its strikes' timing, flash, bolt and
/// thunder worked out alone, and a strike read off pictures of a night it lights.
/// </summary>
[Collection("engine")]
public sealed class ThunderTests
{
    [Fact]
    public void TheFlashFlickersAndDiesAway()
    {
        const float Life = 0.55f;
        Assert.Equal(0f, SkyThunder.Envelope(-0.01f, Life, 7));
        Assert.Equal(0f, SkyThunder.Envelope(Life + 0.01f, Life, 7));
        var peak = Enumerable.Range(0, 20).Max(i => SkyThunder.Envelope(i * 0.005f, Life, 7));
        Assert.True(peak > 0.6f, $"the flash peaks at {peak} in its first tenth of a second");
        Assert.True(SkyThunder.Envelope(Life * 0.95f, Life, 7) < 0.1f, "the flash has not died away by its end");
        Assert.Equal(SkyThunder.Envelope(0.2f, Life, 7), SkyThunder.Envelope(0.2f, Life, 7));
    }

    [Fact]
    public void StrikesComeAtTheirRateOnAverage()
    {
        var random = new Random(3);
        var gaps = Enumerable.Range(0, 4000).Select(_ => SkyThunder.Gap(6f, random)).ToArray();
        var mean = gaps.Average();
        Assert.InRange(mean, 9.4, 10.6);
        Assert.True(gaps.Min() < 0.5f && gaps.Max() > 40f, "the gaps keep to a beat rather than spreading as a storm's do");
    }

    [Fact]
    public void ABoltRunsFromTheCloudsToTheGroundAndBranches()
    {
        var top = new Vec3(100f, 800f, -500f);
        var ground = new Vec3(100f, 0f, -500f);
        var segments = LightningBolt.Grow(top, ground, 11);
        var channel = segments.Where(s => s.Width == 1f).ToList();
        Assert.Equal(128, channel.Count);
        Assert.Equal(top, channel[0].From);
        Assert.Equal(ground, channel[^1].To);
        for (var i = 1; i < channel.Count; i++) Assert.Equal(channel[i - 1].To, channel[i].From);
        Assert.InRange(segments.Count(s => s.Width < 1f), 3 * 32, 6 * 32);
        Assert.All(channel, s => Assert.True(MathF.Abs(s.From.X - 100f) < 400f && MathF.Abs(s.From.Z + 500f) < 400f, $"the channel wanders to {s.From}"));
    }

    [Fact]
    public void ThunderFromAfarIsLowerAndLonger()
    {
        var near = new ThunderSound(200f, 1f, 5);
        var far = new ThunderSound(3_000f, 1f, 5);
        Assert.True(far.Duration > near.Duration + 2f, $"far thunder lasts {far.Duration} s and near {near.Duration} s");

        static (double Loud, double Crossings) Listen(ThunderSound sound)
        {
            var samples = new float[ThunderSound.SampleRate];
            sound.Fill(samples);
            var loud = Math.Sqrt(samples.Average(s => (double)s * s));
            var crossings = Enumerable.Range(1, samples.Length - 1).Count(i => (samples[i - 1] < 0f) != (samples[i] < 0f));
            return (loud, crossings);
        }

        var (nearLoud, nearCrossings) = Listen(near);
        var (farLoud, farCrossings) = Listen(far);
        Assert.True(nearLoud > farLoud && farLoud > 0.005, $"near thunder's first second is {nearLoud} loud and far's {farLoud}");
        Assert.True(nearCrossings > farCrossings * 2, $"near thunder crosses zero {nearCrossings} times in its first second and far {farCrossings}");
        Assert.True(Enumerable.Range(0, 200).All(_ => { var s = new float[512]; near.Fill(s); return s.All(v => v is >= -1f and <= 1f); }), "thunder leaves the samples' range");
    }

    [SkippableFact]
    public void AStrikeLightsTheNightAndDrawsItsBolt()
    {
        Needs.Shaders();
        Entity camera = default;
        var strikes = new List<LightningStrike>();
        var run = new PictureRun
        {
            Width = 320,
            Height = 180,
            Frames = 1500,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f, Far = 5_000f });
                var eye = new Vec3(0f, 1.7f, 0f);
                ecs.Add(camera, Transform.LookingAt(eye, eye + new Vec3(0f, 0.3f, -1f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1, Dither = false });
                var ground = ecs.Spawn();
                Render.SetMesh(ecs, ground, Render.CreateMesh(MeshShape.Cuboid, 3000f, 0.2f, 3000f));
                Render.SetMaterial(ecs, ground, Render.CreateMaterial(0.3f, 0.3f, 0.3f, roughness: 0.9f));
                ecs.Add(ground, Transform.At(0f, -0.1f, 0f));
                Render.SetThunder(camera, new ThunderSettings { Struck = strikes.Add, Sound = false });
            },
        };
        run.Wait(10)
            .Until("compiled", _ => SkyThunder.Ready())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(10)
            .Capture("dark")
            .Do("strike", _ => Render.StrikeLightning(camera, new Vec3(0f, 0f, -600f)))
            .Wait(4)
            .Capture("lit")
            .Go();

        var strike = Assert.Single(strikes);
        Assert.True(strike.ToGround);
        Assert.InRange(strike.Distance, 599f, 601f);
        Assert.Equal(strike.Distance / 343f, strike.ThunderDelay, 3);

        var (dark, lit) = (run.Picture("dark"), run.Picture("lit"));
        double Mean(CapturedImage picture, uint from, uint to)
        {
            var sum = 0.0;
            for (var y = from; y < to; y++)
                for (var x = 0u; x < picture.Width; x++)
                    sum += picture.At(x, y).G;
            return sum / ((to - from) * picture.Width);
        }

        // The ground lit by the strike's light, and the bolt a bright thread up the middle of the sky.
        Assert.True(Mean(lit, lit.Height * 3 / 4, lit.Height) > Mean(dark, dark.Height * 3 / 4, dark.Height) + 10, "the strike does not light the ground");
        var bolt = 0;
        for (var y = 0u; y < lit.Height / 2; y++)
            for (var x = lit.Width / 3; x < lit.Width * 2 / 3; x++)
                if (lit.At(x, y).G > 200 && dark.At(x, y).G < 60) bolt++;
        Assert.True(bolt > 20, $"{bolt} pixels show the bolt");
    }

    [Fact]
    public void SettingsOutOfRangeAreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetThunder(default, new ThunderSettings { Rate = -1f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetThunder(default, new ThunderSettings { Brightness = float.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetThunder(default, new ThunderSettings { Nearest = 500f, Farthest = 400f }));
    }
}

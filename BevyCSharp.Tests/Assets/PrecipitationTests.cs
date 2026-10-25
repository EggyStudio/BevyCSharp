using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The rain and the snow <see cref="Render.SetPrecipitation"/> lets fall, read off pictures of a
/// dark wall seen through them and off the counts the drops are drawn by.
/// </summary>
[Collection("engine")]
public sealed class PrecipitationTests
{
    [SkippableFact]
    public void RainFallsUnderAnOpenSky()
    {
        Needs.Shaders();
        var (bare, _) = Look(null);
        var (rainy, counts) = Look(new PrecipitationSettings { Rain = 0.8f });
        var changed = Changed(bare, rainy);
        Assert.True(changed > 300, $"{changed} pixels show rain");
        Assert.True(counts[1] > 500, $"{counts[1]} drops are drawn");
    }

    [SkippableFact]
    public void SnowFallsInFlakes()
    {
        Needs.Shaders();
        var (bare, _) = Look(null);
        var (snowy, counts) = Look(new PrecipitationSettings { Snow = 0.8f });
        var changed = Changed(bare, snowy);
        Assert.True(changed > 100, $"{changed} pixels show snow");
        Assert.True(counts[1] > 500, $"{counts[1]} flakes are drawn");
    }

    [SkippableFact]
    public void NothingFallsAtNone()
    {
        Needs.Shaders();
        var (_, counts) = Look(new PrecipitationSettings());
        Assert.Equal(0u, counts[1]);
    }

    [SkippableFact]
    public void ARoofKeepsTheRainOut()
    {
        Needs.Shaders();
        var (bare, _) = Look(null, roof: true);
        var (covered, _) = Look(new PrecipitationSettings { Rain = 1f }, roof: true);
        var (open, _) = Look(new PrecipitationSettings { Rain = 1f, Roofs = false }, roof: true);
        var under = Changed(bare, covered);
        var through = Changed(bare, open);
        Assert.True(under < 20, $"{under} pixels show rain under the roof");
        Assert.True(through > 300, $"{through} pixels show rain falling through a roof no map holds");
    }

    [SkippableFact]
    public void TheRainSplashesWhereItLands()
    {
        Needs.Shaders();
        var (_, splashing) = Look(new PrecipitationSettings { Rain = 1f });
        var (_, dry) = Look(new PrecipitationSettings { Rain = 1f, Splashes = false });
        Assert.True(splashing[4] > 100, $"{splashing[4]} drops splashed");
        Assert.Equal(0u, dry[4]);
    }

    [Fact]
    public void SettingsOutOfRangeAreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPrecipitation(default, new PrecipitationSettings { Rain = 1.5f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPrecipitation(default, new PrecipitationSettings { Snow = -0.1f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPrecipitation(default, new PrecipitationSettings { Wind = new Vec3(float.NaN, 0f, 0f) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPrecipitation(default, new PrecipitationSettings { Reach = 2f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetPrecipitation(default, new PrecipitationSettings { Quality = (PrecipitationQuality)7 }));
    }

    // How many pixels two pictures differ at by more than a few levels.
    private static int Changed(CapturedImage a, CapturedImage b)
    {
        var changed = 0;
        for (var y = 0u; y < a.Height; y++)
            for (var x = 0u; x < a.Width; x++)
            {
                var (p, q) = (a.At(x, y), b.At(x, y));
                if (Math.Abs(p.R - q.R) + Math.Abs(p.G - q.G) + Math.Abs(p.B - q.B) > 12) changed++;
            }
        return changed;
    }

    // A dark wall 8 meters off across ground lit by the sky and a lamp, from 1.7 meters up, with the
    // rain or snow given or none, under a roof 3 meters up and 40 across, which reaches past the wall,
    // or under the open sky. The picture, and the drops' counts as they were drawn.
    private static (CapturedImage Picture, uint[] Counts) Look(PrecipitationSettings? settings, bool roof = false)
    {
        Entity camera = default;
        BufferRead read = default;
        uint[]? counts = [0u, 0u, 0u, 0u, 0u];
        var run = new PictureRun
        {
            Width = 320,
            Height = 180,
            Frames = 1500,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                var eye = new Vec3(0f, 1.7f, 0f);
                ecs.Add(camera, Transform.LookingAt(eye, eye + new Vec3(0f, 0.1f, -1f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1, Dither = false });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0.3f, 0.8f, 0.5f), Vec3.Zero, Vec3.UnitY));
                var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 100_000f, Range = 15f, Shadows = false });
                ecs.Add(lamp, Transform.At(0f, 2.5f, -4f));

                void Block(Vec3 at, Vec3 size, AssetHandle material)
                {
                    var block = ecs.Spawn();
                    Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
                    Render.SetMaterial(ecs, block, material);
                    ecs.Add(block, Transform.At(at.X, at.Y, at.Z));
                }

                var dark = Render.CreateMaterial(0.05f, 0.05f, 0.06f, roughness: 0.9f);
                Block(new Vec3(0f, -0.1f, 0f), new Vec3(200f, 0.2f, 200f), Render.CreateMaterial(0.2f, 0.2f, 0.2f, roughness: 0.9f));
                Block(new Vec3(0f, 4f, -8f), new Vec3(30f, 8f, 0.5f), dark);
                if (roof) Block(new Vec3(0f, 3.15f, -2f), new Vec3(40f, 0.3f, 40f), dark);

                if (settings is not null) Render.SetPrecipitation(camera, settings);
            },
        };

        run.Wait(10)
            .Until("compiled", _ => Precipitation.Ready())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(60)
            .Capture("picture")
            .Do("asking", _ =>
            {
                if (settings is not null) read = Shaders.BeginBufferRead(Precipitation.CountsOf(camera));
            })
            .Until("read", _ => settings is null || Shaders.TryReadBuffer(read, out counts))
            .Go();

        return (run.Picture("picture"), counts!);
    }
}

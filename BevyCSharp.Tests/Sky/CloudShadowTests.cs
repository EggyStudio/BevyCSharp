using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The clouds' shadow laid over the sun (<see cref="CloudSettings.ShadowLight"/>), read off pictures
/// of sunlit ground seen from above.
/// </summary>
[Collection("engine")]
public sealed class CloudShadowTests
{
    [SkippableFact]
    public void TheCloudsShadowTheGroundInPatches()
    {
        Needs.Shaders();
        var lit = Ground(null);
        var shadowed = Ground(new CloudSettings { Coverage = 0.6f });
        var (litMean, _) = Spread(lit);
        var (shadowedMean, shadowedSpread) = Spread(shadowed);
        Assert.True(shadowedMean < litMean - 10, $"the ground reads {shadowedMean:0.0} under the clouds where {litMean:0.0} under none");
        Assert.True(shadowedSpread > 8, $"the ground under clouds spreads {shadowedSpread:0.0} levels, with no patches of sun and shade");
    }

    [SkippableFact]
    public void AClearSkysShadowLeavesTheSunAsItWas()
    {
        Needs.Shaders();
        // The sun framed over the shadow's square lights the ground as it did, where nothing shadows it.
        var lit = Ground(null);
        var clear = Ground(new CloudSettings { Coverage = 0f });
        var (litMean, _) = Spread(lit);
        var (clearMean, _) = Spread(clear);
        Assert.True(Math.Abs(litMean - clearMean) < 2, $"the ground reads {clearMean:0.0} framed for a clear sky's shadow where {litMean:0.0} plain");
    }

    [SkippableFact]
    public void TakingTheCloudsOffTakesTheirShadowOff()
    {
        Needs.Shaders();
        var camera = Entity.None;
        var run = Run(new CloudSettings { Coverage = 0.9f }, c => camera = c);
        run.Wait(10)
            .Until("compiled", _ => Clouds.Ready())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(30)
            .Capture("shadowed")
            .Do("taking the clouds off", _ => Render.SetClouds(camera, null))
            .Wait(10)
            .Capture("bare")
            .Go();
        var lit = Ground(null);
        var (shadowedMean, _) = Spread(run.Picture("shadowed"));
        var (bareMean, _) = Spread(run.Picture("bare"));
        var (litMean, _) = Spread(lit);
        Assert.True(shadowedMean < litMean - 10, $"under the clouds the ground reads {shadowedMean:0.0} where {litMean:0.0} lit");
        Assert.True(Math.Abs(bareMean - litMean) < 2, $"with the clouds off the ground reads {bareMean:0.0} where {litMean:0.0} lit");
    }

    // The mean light of the picture and how far its pixels spread from it, in levels of luma.
    private static (double Mean, double Spread) Spread(CapturedImage picture)
    {
        var levels = new List<double>();
        for (var y = 0u; y < picture.Height; y += 4)
            for (var x = 0u; x < picture.Width; x += 4)
            {
                var (r, g, b, _) = picture.At(x, y);
                levels.Add((0.2126 * r) + (0.7152 * g) + (0.0722 * b));
            }
        var mean = levels.Average();
        return (mean, Math.Sqrt(levels.Average(l => (l - mean) * (l - mean))));
    }

    private static CapturedImage Ground(CloudSettings? clouds)
    {
        var run = Run(clouds, null);
        run.Wait(10)
            .Until("compiled", _ => clouds is null || Clouds.Ready())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(30)
            .Capture("ground")
            .Go();
        return run.Picture("ground");
    }

    // Gray ground under a high sun seen straight down from a kilometer up, the clouds, where there
    // are any, shadowing the sun.
    private static PictureRun Run(CloudSettings? clouds, Action<Entity>? spawned) => new()
    {
        Width = 240,
        Height = 240,
        Frames = 1500,
        Scene = ecs =>
        {
            var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 90f });
            ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1000f, 0f), new Vec3(0f, 0f, 0.001f), Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1, Tonemapper = Tonemapper.None });

            var ground = ecs.Spawn();
            Render.SetMesh(ecs, ground, Render.CreateMesh(MeshShape.Cuboid, 6000f, 1f, 6000f));
            Render.SetMaterial(ecs, ground, Render.CreateMaterial(0.5f, 0.5f, 0.5f, roughness: 1f));
            ecs.Add(ground, Transform.At(0f, -0.5f, 0f));

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 5_000f, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(new Vec3(0.3f, 1f, 0.4f), Vec3.Zero, Vec3.UnitY));

            if (clouds is not null)
            {
                clouds.ShadowLight = sun;
                Render.SetClouds(camera, clouds);
            }
            spawned?.Invoke(camera);
        },
    };
}

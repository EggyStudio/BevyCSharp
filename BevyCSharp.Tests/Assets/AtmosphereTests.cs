using System.Runtime.InteropServices;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the sky of another planet, Mars's air and the way the sky is worked out, and the disk of
/// the sun a light stands for in it, as Bevy's atmosphere example switches between them.
/// </summary>
[Collection("engine")]
public sealed class AtmosphereTests
{
    /// <summary>The fields the bridge reads, at the places it reads them.</summary>
    [Fact]
    public void TheConfigIsLaidOutAsTheBridgeReadsIt()
    {
        Assert.Equal(20, Marshal.OffsetOf<NativeAtmosphereConfig>(nameof(NativeAtmosphereConfig.GroundAlbedo)).ToInt32());
        Assert.Equal(24, Marshal.OffsetOf<NativeAtmosphereConfig>(nameof(NativeAtmosphereConfig.Planet)).ToInt32());
        Assert.Equal(28, Marshal.OffsetOf<NativeAtmosphereConfig>(nameof(NativeAtmosphereConfig.DustPhase)).ToInt32());
        Assert.Equal(32, Marshal.OffsetOf<NativeAtmosphereConfig>(nameof(NativeAtmosphereConfig.Method)).ToInt32());
        Assert.Equal(36, Marshal.SizeOf<NativeAtmosphereConfig>());
    }

    /// <summary>
    /// Earth's planet, then Mars's in its place, then Mars's at half its size, each with its center
    /// below the origin by its radius in the world, so the ground stays where the scene is. Bevy
    /// places a planet only as it first arrives, so Mars after earth would otherwise keep earth's
    /// center, two thousand kilometers under Mars's ground.
    /// </summary>
    [SkippableFact]
    public void ThePlanetsGroundStaysAtTheOriginFromEarthToMars()
    {
        using var harness = new EngineHarness(frames: 10);
        Needs.Renderer();

        var camera = Entity.None;
        var centers = new List<float>();
        var dust = AssetHandle.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            camera = Render.SpawnCamera3d();
            Render.SetAtmosphere(camera, new AtmosphereSettings());
            dust = AssetServer.Load(AssetKind.Image, "textures/checker.png");
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            var frame = ctx.Time.FrameCount;
            if (frame is not (3 or 6 or 9)) return;

            var planet = Assert.Single(ctx.Ecs.EntitiesWith<Atmosphere>());
            centers.Add(ctx.Ecs.GetOrDefault<GlobalTransform>(planet).Translation.Y);

            if (frame == 3)
            {
                Render.SetAtmosphere(camera, new AtmosphereSettings
                {
                    Planet = SkyPlanet.Mars,
                    DustPhase = dust,
                    Method = SkyMethod.Raymarched,
                });
            }
            else if (frame == 6)
            {
                Render.SetAtmosphere(camera, new AtmosphereSettings { Planet = SkyPlanet.Mars, DustPhase = dust, Scale = 0.5f });
            }
        });

        harness.Run();

        Assert.Equal(3, centers.Count);
        Assert.Equal(-6_360_000f, centers[0], 1f);
        Assert.Equal(-3_389_500f, centers[1], 1f);
        Assert.Equal(-1_694_750f, centers[2], 1f);
    }

    /// <summary>
    /// A sky of Mars without the picture its dust scatters by is refused before the bridge is asked,
    /// since Bevy would have nothing to build it from.
    /// </summary>
    [SkippableFact]
    public void AMarsSkyWithoutItsDustIsRefused()
    {
        using var harness = new EngineHarness(frames: 2);
        Needs.Renderer();

        harness.OnContext(Stage.Startup, _ =>
        {
            var camera = Render.SpawnCamera3d();
            Assert.Throws<ArgumentException>(
                () => Render.SetAtmosphere(camera, new AtmosphereSettings { Planet = SkyPlanet.Mars }));
        });

        harness.Run();
    }

    /// <summary>
    /// The sun's disk goes on a directional light, Mars's or none, and is refused on a light of
    /// another kind, which Bevy's disk would turn into a directional one, on an entity that is gone,
    /// and for a size below zero.
    /// </summary>
    [SkippableFact]
    public void ASunDiskGoesOnADirectionalLightAlone()
    {
        using var harness = new EngineHarness(frames: 2);
        Needs.Renderer();

        harness.OnContext(Stage.Startup, _ =>
        {
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional });
            Render.SetSunDisk(sun, SunDisk.Mars);
            Render.SetSunDisk(sun, SunDisk.Off);

            var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point });
            var notDirectional = Assert.Throws<BevyNativeException>(() => Render.SetSunDisk(lamp, SunDisk.Earth));
            Assert.Equal(NativeStatus.InvalidState, notDirectional.Status);

            var gone = Assert.Throws<BevyNativeException>(() => Render.SetSunDisk(Entity.None, SunDisk.Earth));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);

            Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSunDisk(sun, new SunDisk(-0.01f)));
        });

        harness.Run();
    }
}

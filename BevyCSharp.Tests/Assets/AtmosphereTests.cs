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
        Assert.Equal(36, Marshal.OffsetOf<NativeAtmosphereConfig>(nameof(NativeAtmosphereConfig.TermCount)).ToInt32());
        Assert.Equal(40, Marshal.OffsetOf<NativeAtmosphereConfig>(nameof(NativeAtmosphereConfig.Terms)).ToInt32());
        Assert.Equal(48, Marshal.SizeOf<NativeAtmosphereConfig>());
        Assert.Equal(24, Marshal.OffsetOf<NativeScatteringTerm>(nameof(NativeScatteringTerm.Falloff)).ToInt32());
        Assert.Equal(36, Marshal.OffsetOf<NativeScatteringTerm>(nameof(NativeScatteringTerm.Phase)).ToInt32());
        Assert.Equal(44, Marshal.SizeOf<NativeScatteringTerm>());
    }

    /// <summary>
    /// An air of a game's own draws its own sky, a gas that scatters red more than blue a sky
    /// redder than it is blue where earth's air, the same gas with blue and red the other way
    /// about, draws one bluer than it is red.
    /// </summary>
    [SkippableFact]
    public void AnAirOfTheGamesOwnDrawsItsOwnSky()
    {
        Needs.Renderer();

        var earth = Sky(medium: null);
        var rust = Sky(medium:
        [
            new ScatteringTerm
            {
                Scattering = new Vec3(33.1e-6f, 13.558e-6f, 5.802e-6f),
                Falloff = ScatteringFalloff.Exponential,
                FalloffScale = 8f / 60f,
                Phase = ScatteringPhase.Rayleigh,
            },
        ]);

        Assert.NotNull(earth);
        Assert.NotNull(rust);
        var (earthRed, earthBlue) = Mean(earth);
        var (rustRed, rustBlue) = Mean(rust);
        Assert.True(earthBlue > earthRed + 10, $"earth's sky reads {earthRed:0} red and {earthBlue:0} blue");
        Assert.True(rustRed > rustBlue + 10, $"the red gas's sky reads {rustRed:0} red and {rustBlue:0} blue");
    }

    /// <summary>
    /// A term that describes no air is refused with its reason, one that scatters less than nothing
    /// and a dust that sends its light wholly forward, before anything reaches Bevy.
    /// </summary>
    [SkippableFact]
    public void ATermThatDescribesNoAirIsRefused()
    {
        Needs.Renderer();

        ArgumentException? negative = null, forward = null;
        using var harness = new EngineHarness(frames: 2);
        harness.On(Stage.Update, _ =>
        {
            var camera = Render.SpawnCamera3d();
            negative = Assert.Throws<ArgumentException>(() => Render.SetAtmosphere(camera, new AtmosphereSettings
            {
                Medium = [new ScatteringTerm { Scattering = new Vec3(-1e-6f, 0f, 0f) }],
            }));
            forward = Assert.Throws<ArgumentException>(() => Render.SetAtmosphere(camera, new AtmosphereSettings
            {
                Medium = [new ScatteringTerm(), new ScatteringTerm { Phase = ScatteringPhase.Mie, Asymmetry = 1f }],
            }));
        });

        harness.Run();

        Assert.Contains("Term 0", negative!.Message);
        Assert.Contains("less than nothing", negative.Message);
        Assert.Contains("Term 1", forward!.Message);
        Assert.Contains("asymmetry", forward.Message);
    }

    /// <summary>The mean red and blue of a picture.</summary>
    private static (double Red, double Blue) Mean(CapturedImage picture)
    {
        double red = 0, blue = 0;
        for (uint y = 0; y < picture.Height; y++)
        {
            for (uint x = 0; x < picture.Width; x++)
            {
                var pixel = picture.At(x, y);
                (red, blue) = (red + pixel.R, blue + pixel.B);
            }
        }

        var count = picture.Width * (double)picture.Height;
        return (red / count, blue / count);
    }

    /// <summary>
    /// Draws the sky above the horizon under a sun high in it, through earth's air or one of the
    /// game's own.
    /// </summary>
    private static CapturedImage? Sky(IReadOnlyList<ScatteringTerm>? medium)
    {
        const ulong Settled = 60;
        CapturedImage? picture = null;
        Capture? capture = null;

        using var app = new App(Config.OffscreenFor(32, 32, frames: (uint)Settled + 40));
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
            ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 0f), new Vec3(0f, 3f, -5f), Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
            Render.SetAtmosphere(camera, new AtmosphereSettings { Medium = medium });

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
            ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 3f, 2f), Vec3.Zero, Vec3.UnitY));
        }, "Test.Sky"));
        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            if (world.Resource<Time>().FrameCount == Settled) capture = Render.BeginCapture();
            if (picture is null && capture is { } asked && Render.TryReadCapture(asked, out var arrived)) picture = arrived;
        }, "Test.Read"));

        Assert.Equal(0, app.Run());
        return picture;
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

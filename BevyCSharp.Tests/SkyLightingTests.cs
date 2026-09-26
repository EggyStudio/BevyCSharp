using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers lighting a scene from the sky rather than from a lamp.
/// </summary>
/// <remarks>
/// The side of a surface a lamp does not reach is black in a scene lit only by lamps, and is the
/// color of the sky in one lit by the sky as well. This asserts that difference, because an
/// environment map that is accepted and never generated looks exactly like one that worked until
/// something is in shadow.
/// </remarks>
[Collection("engine")]
public sealed class SkyLightingTests
{
    /// <summary>Frames to let the pipelines compile and the map be generated.</summary>
    private const ulong Settled = 160;

    [Fact]
    public void TheSkyLightsWhatTheSunDoesNot()
    {
        if (!App.HasRenderer) return;

        var unlit = Draw(sky: false);
        var lit = Draw(sky: true);

        Assert.NotNull(unlit);
        Assert.NotNull(lit);

        var shaded = Brightness(unlit.At(20, 40));
        var skylit = Brightness(lit.At(20, 40));

        Assert.True(
            skylit > shaded + 4,
            $"the shaded side reads {shaded} without the sky and {skylit} with it");
    }

    [Fact]
    public void ASizeThatIsNotAPowerOfTwoIsRefused()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, _ =>
        {
            var camera = Render.SpawnCamera3d();

            var refused = Assert.Throws<BevyNativeException>(
                () => Render.SetSkyLighting(camera, size: 300));

            Assert.Equal(NativeStatus.NullArgument, refused.Status);
        });

        harness.Run();
    }

    /// <summary>A cubemap can light the scene as well as be seen behind it.</summary>
    /// <remarks>
    /// The same file the skybox draws, filtered on the GPU into the two halves an environment map
    /// carries. What is asserted is the same thing the sky test asserts, that the side no lamp
    /// reaches is no longer black.
    /// </remarks>
    [Fact]
    public void AnImageLightsWhatTheSunDoesNot()
    {
        if (!App.HasRenderer) return;

        var unlit = Draw(sky: false);
        var lit = Draw(sky: false, image: true);

        Assert.NotNull(unlit);
        Assert.NotNull(lit);

        var shaded = Brightness(unlit.At(20, 40));
        var imageLit = Brightness(lit.At(20, 40));

        Assert.True(
            imageLit > shaded + 4,
            $"the shaded side reads {shaded} without the map and {imageLit} with it");
    }

    /// <summary>A baked pair of maps lights the shaded side the way a filtered one does.</summary>
    /// <remarks>
    /// The same picture for both halves, which is not what a baking tool would produce and is
    /// exactly what the bridge has to carry. What is checked is that both maps become cubes before
    /// the light is applied, since a map sampled while it is still a tall picture is sampled
    /// wrongly rather than refused, and the shaded side would read as it did without one.
    /// </remarks>
    [Fact]
    public void ABakedPairLightsTheShadedSide()
    {
        if (!App.HasRenderer) return;

        var unlit = Draw(sky: false);
        var lit = Draw(sky: false, baked: true);

        Assert.NotNull(unlit);
        Assert.NotNull(lit);

        var shaded = Brightness(unlit.At(20, 40));
        var mapped = Brightness(lit.At(20, 40));

        Assert.True(
            mapped > shaded + 4,
            $"the shaded side reads {shaded} without the maps and {mapped} with them");
    }

    /// <summary>How bright a pixel is, which is all this test needs of a color.</summary>
    private static int Brightness((byte R, byte G, byte B, byte A) pixel) =>
        pixel.R + pixel.G + pixel.B;

    /// <summary>Draws a sphere lit by one low sun, with or without the sky helping.</summary>
    private static CapturedImage? Draw(bool sky, bool image = false, bool baked = false)
    {
        CapturedImage? picture = null;

        var config = Config.OffscreenFor(64, 64, frames: (uint)Settled + 40);
        config.AssetRoot = EngineHarness.AssetDirectory;

        using var app = new App(config);

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 50f });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));

                // The sky has to be there for anything to be derived from it, and a high dynamic
                // range target for the scattering to have anywhere to put its brightest values.
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
                Render.SetAtmosphere(camera, new AtmosphereSettings());

                if (sky) Render.SetSkyLighting(camera, intensity: 4f, size: 64);

                if (image)
                {
                    Render.SetImageLighting(
                        camera,
                        AssetServer.Load(AssetKind.Image, "textures/cubemap.png"),
                        intensity: 3000f);
                }

                if (baked)
                {
                    var cubemap = AssetServer.Load(AssetKind.Image, "textures/cubemap.png");

                    Render.SetEnvironmentMap(camera, cubemap, cubemap, intensity: 3000f);
                }

                // Low and to one side, so one side of the sphere is in shadow and the sky is the
                // only thing that could light it.
                var sun = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Directional,
                    Intensity = 4_000f,
                });

                ecs.Add(sun, Transform.LookingAt(new Vec3(6f, 1f, 2f), Vec3.Zero, Vec3.UnitY));

                var ball = ecs.Spawn();

                Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 1.6f));
                Render.SetMaterial(ecs, ball, Render.CreateMaterial(0.8f, 0.8f, 0.8f, roughness: 0.4f));
                ecs.Add(ball, Transform.Identity);
            },
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == Settled)
                {
                    world.InsertResource(new Ticket(Render.BeginCapture()));
                    return;
                }

                if (picture is not null) return;
                if (!world.TryGetResource<Ticket>(out var ticket)) return;

                if (Render.TryReadCapture(ticket.Capture, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());
        return picture;
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}

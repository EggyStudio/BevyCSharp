using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers what a loading screen waits on, an asset with all it depends on and the renderer's pipelines.</summary>
[Collection("engine")]
public sealed class LoadingTests
{
    /// <summary>
    /// A glTF scene comes to be loaded with everything it depends on, a file that is not there
    /// fails, the pipelines are not ready before anything has been drawn and are once the scene
    /// has been, and an offscreen run has no window entity.
    /// </summary>
    [SkippableFact]
    [ExpectsError("bevy", "nothing-here.gltf")]
    public void ASceneLoadsWithItsDependenciesAndThePipelinesComeReadyOnceItIsDrawn()
    {
        Needs.Renderer();

        AssetHandle scene = default, missing = default;
        var (frame, loadedAt, readyAt) = (0, -1, -1);
        bool? readyAtFirst = null;
        var window = new Entity(1);
        var missingState = AssetLoadState.Unknown;

        var config = Config.OffscreenFor(32, 32, frames: 120);
        config.AssetRoot = EngineHarness.AssetDirectory;
        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = new BehaviorContext(world).Ecs;
            var camera = Render.SpawnCamera3d();
            ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 4f), Vec3.Zero, Vec3.UnitY));
            scene = AssetServer.LoadGltfScene("models/bend.gltf");
            ecs.SpawnScene(scene);
            missing = AssetServer.LoadGltfScene("models/nothing-here.gltf");
            window = Window.Entity();
        }, "Test.Spawn"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            readyAtFirst ??= Render.PipelinesReady();
            if (loadedAt < 0 && AssetServer.StateWithDependenciesOf(scene) == AssetLoadState.Loaded) loadedAt = frame;
            if (loadedAt >= 0 && readyAt < 0 && Render.PipelinesReady()) readyAt = frame;
            missingState = AssetServer.StateWithDependenciesOf(missing);
        }, "Test.Watch"));

        Assert.Equal(0, app.Run());

        Assert.Equal(Entity.None, window);
        Assert.False(readyAtFirst);
        Assert.True(loadedAt > 0, "the scene never loaded with its dependencies");
        Assert.True(readyAt >= loadedAt, "the pipelines never came ready after the scene loaded");
        Assert.Equal(AssetLoadState.Failed, missingState);
    }

    /// <summary>
    /// An app begun on a set clock draws a cube whole in the first frame that shows it, as it does
    /// ten frames on, the cube behind the camera until then so nothing has asked for its pipelines.
    /// </summary>
    /// <remarks>
    /// What a capture on a set clock rests on, the examples' captures among them, since only an app
    /// begun on one compiles each pipeline in the frame that asks for it. The feature test given its
    /// clock once it ran compiled off the main thread, and its meadow, first seen forty frames
    /// before its shot, lost its grass or its walls' light in ten fresh sessions of ten. Begun on
    /// the machine's clock, this test's first frame drew no cube.
    /// </remarks>
    [SkippableFact]
    public void AnAppBegunOnASetClockDrawsWhatComesIntoViewWholeInItsFirstFrame()
    {
        Needs.Renderer();

        var camera = Entity.None;
        CapturedImage? first = null, later = null;
        Capture? ticket = null;

        bool Read(ref CapturedImage? picture) =>
            Render.TryReadCapture(ticket!.Value, out picture) && picture is not null;

        var run = new PictureRun
        {
            Width = 64,
            Height = 64,
            Frames = 600,
            Configure = config => config.FrameSeconds = 1.0 / 60.0,
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 2_000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(2f, 4f, 4f), new Vec3(0f, 0f, 12f), Vec3.UnitY));
                PictureRun.Cube(ecs, Render.CreateMaterial(0.9f, 0.1f, 0.1f, roughness: 0.6f), at: new Vec3(0f, 0f, 12f));
            },
        };

        run.Until("drawn", world => world.Resource<Time>().FrameCount >= 3 && Render.PipelinesReady()).Wait(5)
            .Do("turned to the cube", world =>
            {
                world.Resource<EcsWorld>().Set(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), new Vec3(0f, 0f, 12f), Vec3.UnitY));
                ticket = Render.BeginCapture();
            })
            .Until("the first frame", _ => Read(ref first))
            .Wait(10)
            .Do("later", _ => ticket = Render.BeginCapture())
            .Until("a later frame", _ => Read(ref later))
            .Go();

        Assert.True(PictureRun.Red(later!) > 300, $"the later frame shows {PictureRun.Red(later!)} red pixels of the cube");
        var apart = first!.Pixels.Zip(later!.Pixels, (a, b) => Math.Abs(a - b)).Average();
        Assert.True(apart < 3, $"the first frame is {apart:0.00} levels from the later one a channel, its red pixels {PictureRun.Red(first)} against {PictureRun.Red(later)}");
    }
}

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
}

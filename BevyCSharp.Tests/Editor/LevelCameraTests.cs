using Bevy.Reflected;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a level's cameras in the editor, held off while the level is edited and written on.
/// </summary>
/// <remarks>
/// A camera needs the renderer, so these are skipped on the headless bridge. In the engine
/// collection, since each runs an app.
/// </remarks>
[Collection("engine")]
public sealed class LevelCameraTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-cameras-" + Guid.NewGuid().ToString("n"));

    public LevelCameraTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [SkippableFact]
    public void ALevelCameraIsHeldOffWhileEditedAndSavedOn()
    {
        Needs.Renderer();

        var level = Path.Combine(_root, "camera.scene.json");
        using var harness = new EngineHarness(frames: 2);
        var ran = false;
        bool? heldOff = null, offAfterSaving = null, savedOff = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (ran) return;
            ran = true;

            var camera = Render.SpawnCamera3d(new CameraSettings());
            ctx.Ecs.SetName(camera, "Level camera");

            // One the level holds off of its own accord, which is written off.
            var spare = Render.SpawnCamera3d(new CameraSettings());
            ctx.Ecs.SetName(spare, "Spare camera");
            var spareCamera = ctx.Ecs.Get<CameraRef>(spare)!.Value;
            spareCamera.IsActive = false;

            Assert.True(LevelCameras.IsLevelCamera(ctx.Ecs, camera));
            LevelCameras.Hold(ctx.Ecs);
            heldOff = !ctx.Ecs.Get<CameraRef>(camera)!.Value.IsActive;

            EditorScene.Save(ctx.Ecs, level);
            offAfterSaving = !ctx.Ecs.Get<CameraRef>(camera)!.Value.IsActive;
            savedOff = !ctx.Ecs.Get<CameraRef>(spare)!.Value.IsActive;
        });

        harness.Run();

        Assert.True(heldOff);
        Assert.True(offAfterSaving);
        Assert.True(savedOff);

        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(level));
        bool Active(string name) => document.RootElement.GetProperty("entities").EnumerateArray()
            .Single(entity => entity.TryGetProperty("name", out var called) && called.GetString() == name)
            .GetProperty("components").GetProperty(CameraRef.TypePath).GetProperty("is_active").GetBoolean();

        Assert.True(Active("Level camera"));
        Assert.False(Active("Spare camera"));
    }
}

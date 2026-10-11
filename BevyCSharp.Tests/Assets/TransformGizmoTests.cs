using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's transform gizmo, drawn on the entity carrying <c>TransformGizmoFocus</c>.
/// </summary>
[Collection("engine")]
public sealed class TransformGizmoTests
{
    /// <summary>
    /// The gizmo's green arrow stands above a cube carrying the focus, on a black clear, its settings
    /// are there to read, and the arrow goes once the focus is taken off.
    /// </summary>
    [SkippableFact]
    public void TheGizmoIsDrawnOnTheEntityWithTheFocus()
    {
        Needs.Renderer();

        var cube = Entity.None;
        TransformGizmoSettingsRef.ModeVariant? mode = null;

        // Large enough for the gizmo, which keeps one size on the screen, to be more than a few
        // pixels.
        var run = new PictureRun
        {
            Width = 320,
            Height = 320,
            Configure = config => config.TransformGizmo = true,
            Scene = ecs =>
            {
                ecs.Insert<TransformGizmoCameraRef>(PictureRun.Camera(ecs));
                cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 0.3f, 0.3f, 0.3f), Render.CreateMaterial((0.2f, 0.2f, 0.2f, 1f)), Transform.Identity);
                ecs.Insert<TransformGizmoFocusRef>(cube);
            },
        };

        run.Until("compiled", world => world.Resource<Time>().FrameCount >= 3 && Render.PipelinesReady()).Wait(5)
            .Do("read", world => mode = world.Resource<EcsWorld>().Resource<TransformGizmoSettingsRef>()?.Mode)
            .Capture("focused")
            .Do("unfocusing", world => world.Resource<EcsWorld>().Wrap<TransformGizmoFocusRef>(cube).Remove())
            .Wait(5)
            .Capture("unfocused")
            .Go();

        Assert.Equal(TransformGizmoSettingsRef.ModeVariant.Translate, mode);
        Assert.True(Green(run.Picture("focused")) > 20, $"the focused cube's arrow drew {Green(run.Picture("focused"))} green pixels");
        Assert.Equal(0, Green(run.Picture("unfocused")));
    }

    // The pixels above the picture's middle green enough to be the Y arrow.
    private static int Green(CapturedImage picture)
    {
        var count = 0;
        for (var y = 0u; y < picture.Height / 2; y++)
        {
            for (var x = 0u; x < picture.Width; x++)
            {
                if (picture.At(x, y) is { G: > 120, R: < 100, B: < 100 }) count++;
            }
        }

        return count;
    }
}

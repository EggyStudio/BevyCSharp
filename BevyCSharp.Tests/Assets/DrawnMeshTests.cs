using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the meshes the GPU drew, read back from Bevy's indirect draws
/// (<see cref="Render.CountDrawnMeshes"/>).
/// </summary>
[Collection("engine")]
public sealed class DrawnMeshTests
{
    /// <summary>
    /// Of eight cubes, four in front of the camera and four behind it, the count read back is the
    /// four the GPU's culling left, on a GPU whose culling can be counted.
    /// </summary>
    [SkippableFact]
    public void TheCountIsTheMeshesTheGpuDrew()
    {
        Needs.Renderer();

        var state = DrawnMeshCount.Pending;
        uint drawn = 0;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
                var cube = Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f);
                var material = Render.CreateMaterial((1f, 1f, 1f, 1f));
                foreach (var z in new[] { 0f, 10f })
                {
                    for (var i = 0; i < 4; i++) ecs.SpawnMesh(cube, material, Transform.At(i - 1.5f, 0f, z));
                }

                Render.CountDrawnMeshes(true);
            },
        };

        // Nothing is drawn while its pipeline compiles, and none is asked for in the first frames,
        // as a capture waits for, and a count comes back a frame or two after the frame it counts.
        run.Until("compiled", world => world.Resource<Time>().FrameCount >= 3 && Render.PipelinesReady()).Wait(5)
            .Do("read", _ => state = Render.DrawnMeshes(out drawn))
            .Go();

        Skip.If(state is DrawnMeshCount.NoGpuCulling or DrawnMeshCount.Uncountable, $"the GPU's culling cannot be counted here: {state}");
        Assert.Equal(DrawnMeshCount.Known, state);
        Assert.Equal(4u, drawn);
    }
}

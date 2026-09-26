using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's meshlets, which run only in a bridge built with <c>--meshlet</c> on a GPU with
/// 64-bit texture atomics, and are skipped anywhere else.
/// </summary>
[Collection("engine")]
public sealed class MeshletTests
{
    /// <summary>
    /// A sphere cut into clusters on a worker draws where an ordinary sphere would, lit by a light,
    /// once the conversion is done.
    /// </summary>
    [Fact]
    public void AMeshletMeshDrawsLikeTheMeshItWasMadeFrom()
    {
        if (!App.HasRenderer) return;

        var active = false;

        var run = new PictureRun
        {
            Configure = config => config.MeshletClusters = 1 << 20,
            Scene = ecs =>
            {
                active = Render.MeshletsActive;
                if (!active) return;

                var camera = PictureRun.Camera(ecs);

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 2f, 3f), Vec3.Zero, Vec3.UnitY));

                var ball = ecs.Spawn();
                var meshlet = Render.CreateMeshletMesh(Render.CreateMesh(MeshShape.Sphere, 1.5f));

                Render.SetMeshletMesh(ecs, ball, meshlet);
                Render.SetMaterial(ecs, ball, Render.CreateMaterial(0.9f, 0.1f, 0.1f));
                ecs.Add(ball, Transform.Identity);
            },
        };

        run.Wait(ShaderMaterialTests.Settled + 60).Capture("picture").Go();

        if (!active) return;

        var picture = run.Picture("picture");
        var middle = picture.At(48, 48);
        var corner = picture.At(2, 2);

        Assert.True(middle.R > 80 && middle.R > middle.G * 2, $"the sphere's middle was {middle}");
        Assert.True(corner is { R: < 30, G: < 30, B: < 30 }, $"the empty corner was {corner}");
    }
}

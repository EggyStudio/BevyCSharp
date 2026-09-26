using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's screen-space reflections, and the deferred rendering they read.</summary>
[Collection("engine")]
public sealed class ReflectionTests
{
    /// <summary>
    /// A glowing green cube over a smooth dark floor shows in the floor with reflections on, and not
    /// with them off.
    /// </summary>
    [Fact]
    public void ASmoothFloorReflectsWhatStandsOnIt()
    {
        if (!App.HasRenderer) return;

        var without = GreenInTheFloor(reflect: false);
        var with = GreenInTheFloor(reflect: true);

        Assert.True(with > without + 30, $"the floor showed {with} green pixels with reflections and {without} without");
    }

    /// <summary>How many green pixels there are in the floor in front of the cube.</summary>
    private static int GreenInTheFloor(bool reflect)
    {
        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 7f), new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });

                if (reflect) Render.SetScreenSpaceReflections(camera, new ReflectionSettings());

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 20f, 0.1f, 20f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.02f, 0.02f, 0.02f, 1f),
                    Metallic = 1f,
                    // Inside the range reflections are whole in, which smoother than 0.08 is not.
                    Roughness = 0.2f,
                }));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 1.5f));
                Render.SetMaterial(ecs, cube, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0f, 0f, 0f, 1f),
                    Emissive = (0f, 6f, 0f, 1f),
                }));
                ecs.Add(cube, Transform.At(0f, 0.9f, 0f));
            },
        };

        run.Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        // Only the lower half, which is floor, so the cube itself is not counted.
        var picture = run.Picture("picture");
        var count = 0;

        for (uint y = 80; y < 128; y++)
        {
            for (uint x = 0; x < 128; x++)
            {
                var pixel = picture.At(x, y);
                if (pixel.G > 30 && pixel.G > pixel.R + 15) count++;
            }
        }

        return count;
    }
}

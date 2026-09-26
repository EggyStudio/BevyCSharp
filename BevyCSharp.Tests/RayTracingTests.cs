using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's ray-traced lighting, which runs only in a bridge built with <c>--solari</c> on an
/// adapter that traces rays, and is skipped anywhere else.
/// </summary>
[Collection("engine")]
public sealed class RayTracingTests
{
    /// <summary>
    /// A glowing red wall beside a white floor, with no lamp and no ambient light, lights the floor
    /// red when the camera traces rays, since an emissive surface is a light to them, and leaves it
    /// dark when it does not.
    /// </summary>
    [Fact]
    public void AGlowingWallLightsTheFloorWhenRaysAreTraced()
    {
        if (!App.HasRenderer) return;

        var traced = Floor(traced: true);
        if (traced is not { } lit) return;

        var raster = Floor(traced: false)!.Value;

        Assert.True(lit.R > 40 && lit.R > lit.G + 20, $"the floor by the wall was {lit} with rays traced");
        Assert.True(raster.R < 15, $"the floor by the wall was {raster} without");
    }

    /// <summary>The floor's color beside the wall, or null where ray tracing is not running.</summary>
    private static (byte R, byte G, byte B, byte A)? Floor(bool traced)
    {
        var active = false;

        var run = new PictureRun
        {
            Configure = config => config.RayTracedLighting = true,
            Scene = ecs =>
            {
                active = Render.RayTracingActive;
                if (!active) return;

                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 6f));
                Render.SetAmbientLight(camera, (0f, 0f, 0f), 0f);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                if (traced) Render.SetRayTracedLighting(camera, true);

                var floorMesh = Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f);
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, floorMesh);
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f }));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var wallMesh = Render.CreateMesh(MeshShape.Cuboid, 0.2f, 3f, 4f);
                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, wallMesh);
                Render.SetMaterial(ecs, wall, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0f, 0f, 0f, 1f),
                    Emissive = (4000f, 0f, 0f, 1f),
                }));
                ecs.Add(wall, Transform.At(-1.5f, 1.5f, 0f));

                if (traced)
                {
                    Render.SetRayTraced(floor, floorMesh);
                    Render.SetRayTraced(wall, wallMesh);
                }
            },
        };

        run.Wait(ShaderMaterialTests.Settled + 90).Capture("picture").Go();

        if (!active) return null;

        // The floor just right of the wall, below the middle of the picture.
        return run.Picture("picture").At(52, 70);
    }
}

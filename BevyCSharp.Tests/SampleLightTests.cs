using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the sample's screen-space GI, run from the sample's own shaders, so the reference it is
/// meant to be keeps working as the pieces under it change.
/// </summary>
[Collection("engine")]
public sealed class SampleLightTests
{
    /// <summary>The sample's assets, which the sample's shaders are compiled from.</summary>
    private static readonly string SampleAssets = Path.GetFullPath(
        Path.Combine(EngineHarness.AssetDirectory, "..", "..", "..", "..", "..", "BevyCSharp.Sample", "assets"));

    /// <summary>
    /// A sunlit red wall beside a white floor tints the floor near it red once the bounce is
    /// traced, gathered and added, and leaves it white without.
    /// </summary>
    [Fact]
    public void TheSamplesBounceTintsTheFloorBesideARedWall()
    {
        if (!ShaderMaterialTests.CanRun || !Directory.Exists(SampleAssets)) return;

        var bounced = Floor(bounce: true);
        var plain = Floor(bounce: false);

        // Pixels of the lower half, which is floor, that the bounce made redder than they were.
        var tinted = 0;

        for (var y = 64u; y < 128; y++)
        {
            for (var x = 0u; x < 128; x++)
            {
                var a = bounced.At(x, y);
                var b = plain.At(x, y);
                if ((a.R - a.G) - (b.R - b.G) > 6) tinted++;
            }
        }

        Assert.True(tinted > 200, $"the bounce tinted {tinted} pixels of the floor red");
    }

    /// <summary>
    /// The sample's ray-traced occlusion, painted in place of the picture, is dark where a box
    /// hangs just above the floor and open far from it.
    /// </summary>
    [Fact]
    public void TheSamplesTracedOcclusionDarkensTheFloorUnderABox()
    {
        if (!ShaderMaterialTests.CanRun || !Directory.Exists(SampleAssets)) return;

        var supported = false;

        var run = new PictureRun
        {
            AssetRoot = SampleAssets,
            Scene = ecs =>
            {
                supported = Shaders.SupportsRayQueries;
                if (!supported) return;

                var camera = PictureRun.Camera(ecs, new Vec3(0f, 5f, 3f));
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                // Held a little above the floor, so the camera sees the floor right under its edge.
                var box = ecs.Spawn();
                ecs.Add(box, Transform.At(0f, 0.7f, 0f));

                var pool = Shaders.CreateGeometryPool();
                var plane = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f));
                var cube = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));

                var scene = Shaders.CreateRayScene(pool, 2);
                Shaders.SetRaySceneInstance(scene, 0, floor, plane);
                Shaders.SetRaySceneInstance(scene, 1, box, cube);

                var trace = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Compute = "shaders/rtao.slang",
                        ComputeTarget = ShaderTarget.SpirV,
                    }))
                    .SetRayScene("scene", scene)
                    .Set("radius", 1.5f)
                    .Set("strength", 1f);

                Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.Low);
                Shaders.SetPrepass(camera, depth: true, normals: true);
                Shaders.SetViewDispatches(camera, ViewDispatch.PerPixel(trace, FramePoint.AfterPrepass));
                Shaders.SetPasses(
                    camera,
                    new ShaderPass(
                        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/rtao_show.slang" })),
                        AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => !supported || ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        if (!supported) return;

        var picture = run.Picture("picture");

        // Where the box meets the floor it hangs over, and a corner of the floor far from anything.
        var near = picture.At(48, 48);
        var far = picture.At(8, 90);

        Assert.True(far.R > 230, $"open floor came out {far}, not unoccluded");
        Assert.True(near.R < far.R - 40, $"the floor by the box came out {near} against {far} in the open");
    }

    /// <summary>The picture of the wall and the floor.</summary>
    private static CapturedImage Floor(bool bounce)
    {
        var run = new PictureRun
        {
            AssetRoot = SampleAssets,
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 6f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (0f, 0f, 0f), 0f);

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 3000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 3f, 2f), Vec3.Zero, Vec3.UnitY));

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, 0.2f, 3f, 4f));
                Render.SetMaterial(ecs, wall, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0f, 0f, 1f) }));
                ecs.Add(wall, Transform.At(-1.5f, 1.5f, 0f));

                Shaders.SetPrepass(camera, depth: true, motion: true, deferred: true);

                if (!bounce) return;

                var trace = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/gi_trace.slang" }))
                    .Set("steps", 16u).Set("reach", 3f).Set("thickness", 0.4f);
                var accumulate = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/gi_accumulate.slang" }))
                    .Set("blend", 0.08f);
                var composite = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    DrawVertex = "shaders/gi_composite.slang",
                    DrawFragment = "shaders/gi_composite.slang",
                })).Set("strength", 1f);

                Shaders.SetViewImages(
                    camera,
                    new ViewImage("lit", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true, CopyAt: FramePoint.BeforeTonemapping),
                    new ViewImage("gathered", ShaderImageFormat.Rgba16Float, Scale: 0.5f),
                    new ViewImage("gi", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true));

                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(trace, FramePoint.AfterOpaque, scale: 0.5f),
                    ViewDispatch.PerPixel(accumulate, FramePoint.AfterOpaque, scale: 0.5f));

                Shaders.SetViewDraws(
                    camera,
                    ViewDraw.Fixed(composite, FramePoint.AfterOpaque, 3, blend: DrawBlend.Add, writesDepth: false));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(60).Capture("picture").Go();

        return run.Picture("picture");
    }
}

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

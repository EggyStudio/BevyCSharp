using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers geometry a camera draws inside its prepass (<see cref="FramePoint.InPrepass"/>) into
/// Bevy's deferred buffers, which Bevy's deferred lighting then lights as a standard material, and
/// a draw's shadow stage (<see cref="ShaderProgramSettings.DrawShadow"/>), through a sphere found
/// by a ray from each pixel as Bevy's deferred_raymarch example finds its surface.
/// </summary>
[Collection("engine")]
public sealed class DeferredDrawTests
{
    /// <summary>
    /// The G-buffer holds the surface the draw packed, its base color, metal and roughness, and a
    /// normal facing the camera at the middle of the sphere, as a pass unpacking it finds them.
    /// </summary>
    [SkippableTheory]
    [InlineData(0u)]
    [InlineData(1u)]
    public void ADrawInsideThePrepassPacksItsSurfaceIntoTheGBuffer(uint show)
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = DeferredCamera(ecs);
                Shaders.SetViewDraws(camera, SphereDraw(FramePoint.InPrepass));

                var pass = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Pass = "shaders/show_gbuffer.slang",
                })).Set("show", show);
                Shaders.SetPasses(camera, new ShaderPass(pass, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var middle = picture.At(48, 48);

        // Linear numbers in a picture that stores them encoded as sRGB, so 0.8 comes back as 231
        // and 0.6 as 203, and plus Z mapped into a color as (188, 188, 255). Where the ray misses,
        // the buffer stays empty, which reads as black but for its normal.
        if (show == 0)
        {
            Assert.True(Near(middle.R, 231) && middle.G < 10 && Near(middle.B, 203), $"the buffer held {middle} where the sphere is");
            Assert.True(picture.At(4, 4) is { R: < 10, G: < 10, B: < 10 }, $"the draw wrote {picture.At(4, 4)} where the ray misses");
        }
        else
        {
            Assert.True(Near(middle.R, 188) && Near(middle.G, 188) && middle.B > 245, $"the sphere's middle faced {middle}");
        }
    }

    /// <summary>
    /// What the draw writes inside the prepass is lit by Bevy's deferred lighting by a point light
    /// in front of it, which lights it only at the depth the draw wrote, so the camera's depth
    /// reached the prepass's copy. By the next point of the frame the lighting has taken which
    /// pixels to light, so the draw is refused there and nothing is lit.
    /// </summary>
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADrawInsideThePrepassIsLitByTheDeferredLighting(bool insidePrepass)
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = DeferredCamera(ecs);
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);

                var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 100_000f, Range = 20f });
                ecs.Add(light, Transform.At(0f, 1f, 3f));

                Shaders.SetViewDraws(camera, SphereDraw(insidePrepass ? FramePoint.InPrepass : FramePoint.AfterPrepass));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);

        if (insidePrepass)
            Assert.True(middle.R > 80 && middle.R > middle.G + 50 && middle.R > middle.B + 50, $"the sphere was lit {middle}");
        else
            Assert.True(middle is { R: < 10, G: < 10, B: < 10 }, $"a draw after the prepass was lit {middle}");
    }

    /// <summary>
    /// The sphere's shadow stage writes the depth of the surface it finds into the sun's shadow
    /// map, so the floor in front of it is darker with the draw casting shadows than without.
    /// </summary>
    [SkippableFact]
    public void ASurfaceADrawFindsCastsAShadowThroughItsShadowStage()
    {
        Needs.Shaders();

        var shadowed = SunlitSphere(castsShadow: true);
        var open = SunlitSphere(castsShadow: false);
        var darker = 0;

        for (var y = 0u; y < shadowed.Height; y++)
        {
            for (var x = 0u; x < shadowed.Width; x++)
            {
                var a = shadowed.At(x, y);
                var b = open.At(x, y);
                if (b.R + b.G + b.B - (a.R + a.G + a.B) > 120) darker++;
            }
        }

        Assert.True(darker > 200, $"only {darker} pixels were darker with the sphere's shadow cast");
    }

    /// <summary>
    /// A shadow stage is a draw's, so a program with one and no draw stages is refused.
    /// </summary>
    [Fact]
    public void AShadowStageNeedsTheDrawStages()
    {
        var error = Assert.Throws<ArgumentException>(() => Shaders.CreateProgram(new ShaderProgramSettings
        {
            Pass = "shaders/show_gbuffer.slang",
            DrawShadow = "shaders/deferred_sphere.slang",
        }));

        Assert.Contains("draw vertex", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A floor in the sun with the sphere above it, the sun low toward the camera so the shadow
    /// falls in front of the sphere, and the sphere casting its shadow or none.
    /// </summary>
    private static CapturedImage SunlitSphere(bool castsShadow)
    {
        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = DeferredCamera(ecs);
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 9f, 9f), new Vec3(0f, 0f, 1f), Vec3.UnitY));

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, 1f), Vec3.UnitY));

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.1f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                Shaders.SetViewDraws(
                    camera,
                    SphereDraw(FramePoint.InPrepass, center: new Vector4(0f, 2f, 0f, 1f), shadowStage: true) with { CastsShadows = castsShadow });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();
        return run.Picture("picture");
    }

    /// <summary>A camera drawing deferred, once a pixel, as the deferred buffers need.</summary>
    private static Entity DeferredCamera(EcsWorld ecs)
    {
        var camera = PictureRun.Camera(ecs);
        Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
        Shaders.SetPrepass(camera, depth: true, deferred: true);
        return camera;
    }

    /// <summary>
    /// The sphere drawn over the whole view into the deferred buffers at a point of the frame, red,
    /// a unit across at the origin unless placed.
    /// </summary>
    private static ViewDraw SphereDraw(FramePoint point, Vector4? center = null, bool shadowStage = false)
    {
        var program = Shaders.CreateProgram(new ShaderProgramSettings
        {
            DrawVertex = "shaders/deferred_sphere.slang",
            DrawFragment = "shaders/deferred_sphere.slang",
            DrawShadow = shadowStage ? new ShaderStage("shaders/deferred_sphere.slang", "shadow") : default,
        });

        var sphere = Shaders.CreateInstance(program)
            .Set("sphere", center ?? new Vector4(0f, 0f, 0f, 1f))
            .Set("color", new Vector4(0.8f, 0.2f, 0.2f, 1f));

        return ViewDraw.Fixed(sphere, point, vertices: 3) with { Targets = ["gbuffer", "lighting_pass"] };
    }

    private static bool Near(byte value, int expected) => Math.Abs(value - expected) <= 12;
}

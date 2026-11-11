using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a material a Slang program draws into Bevy's deferred buffers, through a deferred stage
/// (<see cref="ShaderProgramSettings.Deferred"/>). The buffer holds its surface, Bevy's deferred
/// lighting pass lights it, and a camera that draws forward does not draw it, as Bevy's own
/// deferred materials are not.
/// </summary>
[Collection("engine")]
public sealed class DeferredMaterialTests
{
    /// <summary>
    /// The surface a deferred stage writes is the one Bevy's G-buffer holds, its base color, metal
    /// and roughness, as a pass reading the buffer finds them.
    /// </summary>
    [SkippableFact]
    public void ADeferredStageWritesItsSurfaceIntoTheBuffer()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, deferred: true);

                PictureRun.Cube(ecs, Surface(new Vector4(0.8f, 0.2f, 0.2f, 1f), metallic: 0f, roughness: 0.3f));

                var show = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Pass = "shaders/show_gbuffer.slang",
                })).Set("show", 0u);
                Shaders.SetPasses(camera, new ShaderPass(show, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        // Written as linear numbers into a picture that stores them encoded as sRGB, so 0.8 comes
        // back as 231 and 0.3 as 149, as GBufferTests reads a standard material's.
        var cube = run.Picture("picture").At(48, 48);
        Assert.True(Near(cube.R, 231) && cube.G < 10 && Near(cube.B, 149), $"the buffer held {cube} where the cube is");
    }

    /// <summary>
    /// A deferred material is lit by Bevy's deferred lighting pass, the sun on its red faces red,
    /// on a camera that draws deferred, and a camera that draws forward does not draw it.
    /// </summary>
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADeferredMaterialIsLitByTheDeferredPassAndDrawnByNoOtherCamera(bool deferred)
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                if (deferred) Shaders.SetPrepass(camera, depth: true, deferred: true);

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 2_000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(2f, 4f, 6f), Vec3.Zero, Vec3.UnitY));

                PictureRun.Cube(ecs, Surface(new Vector4(0.9f, 0.1f, 0.1f, 1f), metallic: 0f, roughness: 0.6f));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var cube = run.Picture("picture").At(48, 48);
        if (deferred)
            Assert.True(cube.R > 80 && cube.R > cube.G + 50 && cube.R > cube.B + 50, $"drawn deferred the cube was {cube}");
        else
            Assert.True(cube is { R: < 10, G: < 10, B: < 10 }, $"a camera drawing forward drew the deferred cube, {cube}");
    }

    /// <summary>
    /// A deferred stage with no prepass vertex shader of the program's own is refused, since it
    /// reads more of the prepass than Bevy's vertex shader writes for every mesh.
    /// </summary>
    [Fact]
    public void ADeferredStageNeedsAPrepassVertexShader()
    {
        var error = Assert.Throws<ArgumentException>(() => Shaders.CreateProgram(new ShaderProgramSettings
        {
            Deferred = "shaders/deferred_surface.slang",
        }));

        Assert.Contains("prepass vertex shader", error.Message, StringComparison.Ordinal);
    }

    private static ShaderMaterial Surface(Vector4 color, float metallic, float roughness) =>
        Shaders.CreateMaterial(Shaders.CreateProgram(new ShaderProgramSettings
            {
                PrepassVertex = new ShaderStage("shaders/deferred_surface.slang", "prepass_vertex"),
                Deferred = "shaders/deferred_surface.slang",
            }))
            .Set("color", color)
            .Set("metallic", metallic)
            .Set("roughness", roughness);

    private static bool Near(byte value, int expected) => Math.Abs(value - expected) <= 12;
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers pipeline constants, a Slang <c>[SpecializationConstant]</c> a material sets by name and
/// its pipelines are compiled with.
/// </summary>
[Collection("engine")]
public sealed class PipelineConstantTests
{
    /// <summary>
    /// Two materials of one program draw apart by the constant each sets, the one setting none
    /// drawing the shader's own value, and a constant changed draws the material again with it.
    /// </summary>
    [SkippableFact]
    public void EachMaterialDrawsWithItsOwnConstant()
    {
        Needs.Shaders();

        ShaderMaterial red = default;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/constant_color.slang" });
                red = Shaders.CreateMaterial2d(program).Set("RED", 1f);
                Square(ecs, red, -24f);
                Square(ecs, Shaders.CreateMaterial2d(program), 24f);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("apart")
            .Do("setting it to zero", _ => red.Set("RED", 0f))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("together")
            .Go();

        var apart = run.Picture("apart");
        Assert.True(apart.At(24, 48) is { R: > 240, G: < 10 }, $"the square set to one was {apart.At(24, 48)}");
        Assert.True(apart.At(72, 48) is { G: > 240, R: < 10 }, $"the square left unset was {apart.At(72, 48)}");
        Assert.True(run.Picture("together").At(24, 48) is { G: > 240, R: < 10 }, $"the square set to zero was {run.Picture("together").At(24, 48)}");
    }

    /// <summary>
    /// A pass's instance refuses a constant once its program has compiled, since a pass has one
    /// pipeline for every instance of its program and runs with the shader's own value.
    /// </summary>
    [SkippableFact]
    public void AnInstanceRefusesAConstant()
    {
        Needs.Shaders();

        ShaderInstance instance = default;
        Exception? refused = null;

        new PictureRun
        {
            Scene = _ => instance = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/constant_pass.slang" })),
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("setting the constant", _ => refused = Record.Exception(() => instance.Set("STRENGTH", 2f)))
            .Go();

        Assert.IsType<ArgumentException>(refused);
        Assert.Contains("pipeline constant", refused.Message, StringComparison.Ordinal);
    }

    // A square forty pixels across, `x` pixels right of the middle.
    private static void Square(EcsWorld ecs, ShaderMaterial material, float x)
    {
        var square = ecs.Spawn();
        ecs.Add(square, Transform.At(x, 0f, 0f) with { Scale = new Vec3(40f) });
        Render2d.SetMesh(ecs, square, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
        Render2d.SetMaterial(ecs, square, material);
    }
}

using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a material a Slang program draws a 2D mesh with, through its 2D stages
/// (<see cref="ShaderProgramSettings.Fragment2d"/>), drawn by a 2D camera as Bevy draws a
/// <c>Material2d</c>.
/// </summary>
[Collection("engine")]
public sealed class ShaderMaterial2dTests
{
    /// <summary>
    /// A square drawn by a 2D material is the color the material is given, and turns another as a
    /// value is set on it by name, through the material and through the entity drawn with it.
    /// </summary>
    [SkippableFact]
    public void A2dMaterialDrawsTheColorItIsGivenAndTakesANewOne()
    {
        Needs.Shaders();

        ShaderMaterial material = default;
        var square = Entity.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                material = Shaders.CreateMaterial2d(Program(vertex: false)).Set("color", new Vector4(0f, 1f, 0f, 1f));
                square = Square(ecs, material);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("green")
            .Do("turning it red", _ => Shaders.MaterialOn(square).Set("color", new Vector4(1f, 0f, 0f, 1f)))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("red")
            .Go();

        var green = run.Picture("green");
        Assert.True(green.At(48, 48) is { G: > 120, R: < 90, B: < 90 }, $"the square was {green.At(48, 48)}");
        Assert.True(green.At(4, 4) is { G: < 90 }, $"outside the square was {green.At(4, 4)}");
        Assert.True(run.Picture("red").At(48, 48) is { R: > 120, G: < 90, B: < 90 }, $"the square turned {run.Picture("red").At(48, 48)}");
    }

    /// <summary>
    /// A 2D vertex shader of the program's own places the mesh, here thirty pixels right.
    /// </summary>
    [SkippableFact]
    public void A2dVertexShaderOfTheProgramsOwnMovesTheMesh()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                Square(ecs, Shaders.CreateMaterial2d(Program(vertex: true))
                    .Set("color", new Vector4(0f, 1f, 0f, 1f))
                    .Set("offset", new Vector2(30f, 0f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        Assert.True(picture.At(30, 48) is { G: < 90 }, $"the square stayed at the left, {picture.At(30, 48)}");
        Assert.True(picture.At(78, 48) is { G: > 120 }, $"the square did not reach the right, {picture.At(78, 48)}");
    }

    /// <summary>
    /// A blended 2D material is mixed with the clear color behind it by its alpha, half green over
    /// Bevy's dark gray coming out between the two, where an opaque one of the same color covers
    /// it.
    /// </summary>
    [SkippableTheory]
    [InlineData(AlphaMode2d.Blend)]
    [InlineData(AlphaMode2d.Opaque)]
    public void ABlended2dMaterialIsMixedWithWhatIsBehindIt(AlphaMode2d alpha)
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                Square(ecs, Shaders.CreateMaterial2d(Program(vertex: false), alpha).Set("color", new Vector4(0f, 1f, 0f, 0.5f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        if (alpha == AlphaMode2d.Blend)
            Assert.True(middle is { G: > 100 and < 230, R: > 10 }, $"half green over the gray was {middle}");
        else
            Assert.True(middle is { G: > 240, R: < 10 }, $"an opaque square was {middle}");
    }

    /// <summary>
    /// A 2D vertex shader draws with the 2D fragment shader beside it, so one alone is refused.
    /// </summary>
    [Fact]
    public void A2dVertexShaderNeedsA2dFragmentShader()
    {
        var error = Assert.Throws<ArgumentException>(() => Shaders.CreateProgram(new ShaderProgramSettings
        {
            Pass = "shaders/show_gbuffer.slang",
            Vertex2d = "shaders/flat_material_2d.slang",
        }));

        Assert.Contains("2D fragment shader", error.Message, StringComparison.Ordinal);
    }

    private static ShaderProgram Program(bool vertex) => Shaders.CreateProgram(new ShaderProgramSettings
    {
        Fragment2d = "shaders/flat_material_2d.slang",
        Vertex2d = vertex ? new ShaderStage("shaders/flat_material_2d.slang") : default,
    });

    // A square forty pixels across in the middle of the picture.
    private static Entity Square(EcsWorld ecs, ShaderMaterial material)
    {
        var square = ecs.Spawn();
        ecs.Add(square, Transform.Identity with { Scale = new Vec3(40f) });
        Render2d.SetMesh(ecs, square, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
        Render2d.SetMaterial(ecs, square, material);
        return square;
    }
}

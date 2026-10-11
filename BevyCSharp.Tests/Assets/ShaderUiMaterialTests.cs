using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a material a Slang program draws an interface node with, through its interface stages
/// (<see cref="ShaderProgramSettings.FragmentUi"/>), drawn as Bevy draws a <c>UiMaterial</c>.
/// </summary>
[Collection("engine")]
public sealed class ShaderUiMaterialTests
{
    /// <summary>
    /// A node drawn by an interface material is the color the material is given, and turns another
    /// as a value is set on it by name, through the material and through the node drawn with it.
    /// </summary>
    [SkippableFact]
    public void AnInterfaceMaterialDrawsTheColorItIsGivenAndTakesANewOne()
    {
        Needs.Shaders();

        var node = Entity.None;
        ShaderProgram program = default, drawnBy = default;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                program = Program(vertex: false);
                node = Node(Render2d.SpawnCamera2d(), Shaders.CreateUiMaterial(program).Set("color", new Vector4(0f, 1f, 0f, 1f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("green")
            .Do("turning it red", _ =>
            {
                drawnBy = Shaders.ProgramOn(node);
                Shaders.MaterialOn(node).Set("color", new Vector4(1f, 0f, 0f, 1f));
            })
            .Wait(ShaderMaterialTests.Settled)
            .Capture("red")
            .Go();

        var green = run.Picture("green");
        Assert.True(green.At(48, 48) is { G: > 240, R: < 10, B: < 10 }, $"the node was {green.At(48, 48)}");
        Assert.True(green.At(8, 8) is { G: < 90 }, $"outside the node was {green.At(8, 8)}");
        Assert.True(run.Picture("red").At(48, 48) is { R: > 240, G: < 10 }, $"the node turned {run.Picture("red").At(48, 48)}");
        Assert.Equal(program, drawnBy);
    }

    /// <summary>
    /// The fragment shader is handed the node's size and borders, which this one draws its border
    /// color within, eight pixels in from each side of a node forty across.
    /// </summary>
    [SkippableFact]
    public void TheFragmentShaderReadsTheNodesBorders()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = _ => Node(
                Render2d.SpawnCamera2d(),
                Shaders.CreateUiMaterial(Program(vertex: false))
                    .Set("color", new Vector4(0f, 1f, 0f, 1f))
                    .Set("border_color", new Vector4(0f, 0f, 1f, 1f)),
                border: 8f),
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        Assert.True(picture.At(48, 48) is { G: > 240, B: < 10 }, $"the middle was {picture.At(48, 48)}");
        Assert.True(picture.At(31, 48) is { B: > 240, G: < 10 }, $"the left border was {picture.At(31, 48)}");
        Assert.True(picture.At(48, 65) is { B: > 240, G: < 10 }, $"the bottom border was {picture.At(48, 65)}");
    }

    /// <summary>
    /// An interface vertex shader of the program's own places the node, here twenty pixels right.
    /// </summary>
    [SkippableFact]
    public void AnInterfaceVertexShaderOfTheProgramsOwnMovesTheNode()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = _ => Node(
                Render2d.SpawnCamera2d(),
                Shaders.CreateUiMaterial(Program(vertex: true))
                    .Set("color", new Vector4(0f, 1f, 0f, 1f))
                    .Set("offset", new Vector2(20f, 0f))),
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        Assert.True(picture.At(32, 48) is { G: < 90 }, $"the node stayed at the left, {picture.At(32, 48)}");
        Assert.True(picture.At(80, 48) is { G: > 240 }, $"the node did not reach the right, {picture.At(80, 48)}");
    }

    /// <summary>
    /// An interface vertex shader draws with the interface fragment shader beside it, so one alone
    /// is refused.
    /// </summary>
    [Fact]
    public void AnInterfaceVertexShaderNeedsAnInterfaceFragmentShader()
    {
        var error = Assert.Throws<ArgumentException>(() => Shaders.CreateProgram(new ShaderProgramSettings
        {
            Pass = "shaders/show_gbuffer.slang",
            VertexUi = "shaders/flat_material_ui.slang",
        }));

        Assert.Contains("interface fragment shader", error.Message, StringComparison.Ordinal);
    }

    private static ShaderProgram Program(bool vertex) => Shaders.CreateProgram(new ShaderProgramSettings
    {
        FragmentUi = "shaders/flat_material_ui.slang",
        VertexUi = vertex ? new ShaderStage("shaders/flat_material_ui.slang") : default,
    });

    // A node forty pixels across in the middle of the picture, on the camera the run draws.
    private static Entity Node(Entity camera, ShaderMaterial material, float border = 0f)
    {
        var node = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(28f),
            Top = Length.Px(28f),
            Width = Length.Px(40f),
            Height = Length.Px(40f),
            Border = Sides.All(Length.Px(border)),
            Camera = camera,
        });

        Ui.SetMaterial(node, material);
        return node;
    }
}

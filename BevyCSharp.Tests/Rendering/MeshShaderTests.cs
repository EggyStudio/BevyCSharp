using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers geometry a camera draws with mesh shaders, a program's draw task and draw mesh stages
/// writing what its draw fragment shader colors, as Bevy's <c>mesh_shader_intro</c> draws.
/// </summary>
/// <remarks>
/// Each runs only where the device has mesh shaders, which the run says before any of it is drawn.
/// The shaders draw halves of the view in clip space, so nothing depends on where the camera is.
/// </remarks>
[Collection("engine")]
public sealed class MeshShaderTests
{
    /// <summary>
    /// A task shader runs the mesh shader's workgroups and hands them a color from the program's own
    /// value, and the two workgroups it runs draw the two halves of the view in it.
    /// </summary>
    [SkippableFact]
    public void ATaskShaderRunsTheMeshShaderWithItsPayload()
    {
        Needs.Shaders();

        var (supported, picture) = Draw(task: true, "mesh", program => ViewDraw.Meshes(
            Shaders.CreateInstance(program).Set("tint", new Vector4(1f, 0f, 0f, 1f)), FramePoint.AfterOpaque, 1));

        Skip.IfNot(supported, "This device does not run mesh shaders.");
        Assert.True(picture.At(24, 48) is { R: > 240, G: < 10, B: < 10 }, $"the left half was {picture.At(24, 48)}");
        Assert.True(picture.At(72, 48) is { R: > 240, G: < 10, B: < 10 }, $"the right half was {picture.At(72, 48)}");
    }

    /// <summary>
    /// Without a task shader a draw's workgroups are the mesh shader's, each drawing the half its
    /// number says.
    /// </summary>
    [SkippableFact]
    public void WithoutATaskShaderTheDrawRunsTheMeshShadersWorkgroups()
    {
        Needs.Shaders();

        var (supported, picture) = Draw(task: false, "mesh_alone", program => ViewDraw.Meshes(
            Shaders.CreateInstance(program), FramePoint.AfterOpaque, 2));

        Skip.IfNot(supported, "This device does not run mesh shaders.");
        Assert.True(picture.At(24, 48) is { G: > 240, R: < 10, B: < 10 }, $"the left half was {picture.At(24, 48)}");
        Assert.True(picture.At(72, 48) is { B: > 240, R: < 10, G: < 10 }, $"the right half was {picture.At(72, 48)}");
    }

    /// <summary>
    /// An indirect draw runs as many workgroups as a buffer says, here one, which draws the left
    /// half and leaves the right as the camera cleared it.
    /// </summary>
    [SkippableFact]
    public void AnIndirectDrawRunsAsManyWorkgroupsAsItsBufferSays()
    {
        Needs.Shaders();

        var (supported, picture) = Draw(task: false, "mesh_alone", program => ViewDraw.Indirect(
            Shaders.CreateInstance(program), FramePoint.AfterOpaque, Shaders.CreateBuffer<uint>([1u, 1u, 1u])));

        Skip.IfNot(supported, "This device does not run mesh shaders.");
        Assert.True(picture.At(24, 48) is { G: > 240 }, $"the left half was {picture.At(24, 48)}");
        Assert.True(picture.At(72, 48) is { R: < 10, G: < 10, B: < 10 }, $"the right half was {picture.At(72, 48)}");
    }

    /// <summary>
    /// A program draws on a camera with a vertex stage or a mesh stage, not both, and a task stage
    /// has a mesh stage to run.
    /// </summary>
    [Fact]
    public void AProgramHasAVertexStageOrAMeshStageAndATaskStageNeedsAMeshStage()
    {
        var both = Assert.Throws<ArgumentException>(() => Shaders.CreateProgram(new ShaderProgramSettings
        {
            DrawVertex = "shaders/mesh_halves.slang",
            DrawMesh = "shaders/mesh_halves.slang",
            DrawFragment = "shaders/mesh_halves.slang",
        }));
        Assert.Contains("one of the two", both.Message, StringComparison.Ordinal);

        var task = Assert.Throws<ArgumentException>(() => Shaders.CreateProgram(new ShaderProgramSettings
        {
            DrawTask = "shaders/mesh_halves.slang",
            DrawFragment = "shaders/mesh_halves.slang",
        }));
        Assert.Contains("draw mesh shader", task.Message, StringComparison.Ordinal);
    }

    // Draws the program on a camera, answering whether the device runs mesh shaders and the
    // picture.
    private static (bool Supported, CapturedImage Picture) Draw(bool task, string mesh, Func<ShaderProgram, ViewDraw> draw)
    {
        var supported = false;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                supported = Shaders.SupportsMeshShaders;
                if (!supported) return;

                var camera = PictureRun.Camera(ecs);
                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    DrawTask = task ? new ShaderStage("shaders/mesh_halves.slang", "task") : default,
                    DrawMesh = new ShaderStage("shaders/mesh_halves.slang", mesh),
                    DrawFragment = "shaders/mesh_halves.slang",
                });
                Shaders.SetViewDraws(camera, draw(program));
            },
        };

        run.Until("compiled", _ => !supported || ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        return (supported, run.Picture("picture"));
    }
}

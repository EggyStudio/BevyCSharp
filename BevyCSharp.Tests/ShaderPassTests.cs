using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers full-screen passes a camera runs over what it drew.
/// </summary>
/// <remarks>
/// A green cube on black, which a pass turns into something that could not have been drawn any
/// other way: magenta on white for an inversion, red for a channel swap. Either picture says the
/// pass ran, read the picture the camera drew, and wrote the one that reached the screen.
/// </remarks>
[Collection("engine")]
public sealed class ShaderPassTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const uint Settled = 120;

    [Fact]
    public void APassChangesThePictureAndItsNumbersChangeThePass()
    {
        if (!App.HasRenderer) return;

        var camera = Entity.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs);

                var flat = Shaders.CreateProgram("shaders/flat.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(flat, [0f, 1f, 0f, 1f]));

                Shaders.SetPasses(camera, new ShaderPassSettings
                {
                    Program = Shaders.CreateProgram("shaders/invert.wgsl"),
                    Parameters = [1f],
                    AfterTonemapping = true,
                });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("inverted")
            .Do("inverting none of it", _ => Shaders.SetPassParameters(camera, 0, [0f]))
            .Wait(10)
            .Capture("plain")
            .Do("taking the pass off", _ => Shaders.SetPasses(camera))
            .Wait(10)
            .Capture("without")
            .Go();

        var inverted = run.Picture("inverted");
        Assert.True(PictureRun.Magenta(inverted) > 100, "the green cube did not come out magenta");

        var corner = inverted.At(2, 2);
        Assert.True(
            corner.R > 200 && corner.G > 200 && corner.B > 200,
            $"the black background came out {corner} rather than white");

        Assert.True(PictureRun.Green(run.Picture("plain")) > 100, "a pass inverting none of it changed the picture");
        Assert.True(PictureRun.Green(run.Picture("without")) > 100, "taking the pass off broke the picture");
    }

    /// <summary>A pass written in Slang runs over the picture the way a WGSL one does.</summary>
    [Fact]
    public void ASlangPassChangesThePicture()
    {
        if (!App.HasRenderer || !Shaders.SlangAvailable) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                var flat = Shaders.CreateProgram("shaders/flat.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(flat, [0f, 1f, 0f, 1f]));

                Shaders.SetPasses(camera, new ShaderPassSettings
                {
                    Program = Shaders.CreateProgram("shaders/swap.slang"),
                });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        Assert.True(
            PictureRun.Red(run.Picture("picture")) > 100,
            "the green cube did not come out red");
    }

    /// <summary>Two passes run in the order given, each reading what the one before wrote.</summary>
    /// <remarks>
    /// Inverting twice is the picture unchanged, and inverting once is not, so a second pass that
    /// read the camera's picture rather than the first pass's would give it away.
    /// </remarks>
    [Fact]
    public void PassesRunInOrderOverEachOther()
    {
        if (!App.HasRenderer) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                var flat = Shaders.CreateProgram("shaders/flat.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(flat, [0f, 1f, 0f, 1f]));

                var invert = Shaders.CreateProgram("shaders/invert.wgsl");

                Shaders.SetPasses(
                    camera,
                    new ShaderPassSettings { Program = invert, Parameters = [1f], AfterTonemapping = true },
                    new ShaderPassSettings { Program = invert, Parameters = [1f], AfterTonemapping = true });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "inverting twice did not give the picture back");
    }

    /// <summary>
    /// A pass reads the camera's depth where the camera draws it, and the far plane where it does
    /// not.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APassReadsTheCamerasDepth(bool drawn)
    {
        if (!App.HasRenderer) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                // One sample a pixel, since a multisampled prepass cannot be bound as a texture.
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                if (drawn) Shaders.SetPrepass(camera, depth: true);

                var flat = Shaders.CreateProgram("shaders/flat.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(flat, [0f, 0f, 1f, 1f]));

                Shaders.SetPasses(camera, new ShaderPassSettings
                {
                    Program = Shaders.CreateProgram("shaders/depth_mask.wgsl"),
                    AfterTonemapping = true,
                });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var picture = run.Picture("picture");
        var middle = picture.At(48, 48);
        var corner = picture.At(2, 2);

        Assert.True(corner.R > 200 && corner.G < 60, $"the empty corner came out {corner} rather than red");

        if (drawn)
            Assert.True(middle.G > 200 && middle.R < 60, $"the cube came out {middle} rather than green");
        else
            Assert.True(middle.R > 200 && middle.G < 60, $"with no prepass the cube came out {middle} rather than red");
    }

    /// <summary>
    /// The Slang prelude turns depth into a distance in world units, which is the front of a cube
    /// two units across seen from six units away: five.
    /// </summary>
    [Fact]
    public void ASlangPassMeasuresDistance()
    {
        if (!App.HasRenderer || !Shaders.SlangAvailable) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true);

                var flat = Shaders.CreateProgram("shaders/flat.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(flat, [0f, 0f, 1f, 1f]));

                Shaders.SetPasses(camera, new ShaderPassSettings
                {
                    Program = Shaders.CreateProgram("shaders/distance.slang"),
                    Parameters = [4.8f, 5.2f],
                    AfterTonemapping = true,
                });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle.G > 200 && middle.R < 60, $"the cube's front came out {middle}, so not five units away");
    }

    /// <summary>A pass needs a program, and only a camera takes one.</summary>
    [Fact]
    public void APassWithNoProgramIsRefused()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.SetPasses(Entity.None, new ShaderPassSettings()));
    }
}

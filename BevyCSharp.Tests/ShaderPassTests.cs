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
        if (!ShaderMaterialTests.CanRun) return;

        var camera = Entity.None;
        var invert = default(ShaderInstance);

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Green));

                invert = Invert().Set("amount", 1f);
                Shaders.SetPasses(camera, new ShaderPass(invert, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("inverted")
            .Do("inverting none of it", _ => invert.Set("amount", 0f))
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

    /// <summary>A pass that declares nothing of its own runs before tonemapping.</summary>
    [Fact]
    public void APassWithNothingOfItsOwnChangesThePicture()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Green));
                Shaders.SetPasses(camera, Pass("shaders/swap.slang"));
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
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Green));

                // Two instances of one program, which are two sets of values.
                Shaders.SetPasses(
                    camera,
                    new ShaderPass(Invert().Set("amount", 1f), AfterTonemapping: true),
                    new ShaderPass(Invert().Set("amount", 1f), AfterTonemapping: true));
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
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                // One sample a pixel, since a multisampled prepass cannot be bound as a texture.
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                if (drawn) Shaders.SetPrepass(camera, depth: true);

                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue));
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/depth_mask.slang"), AfterTonemapping: true));
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
    /// The prelude turns depth into a distance in world units, which is the front of a cube two
    /// units across seen from six units away: five.
    /// </summary>
    [Fact]
    public void APassMeasuresDistance()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true);

                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue));

                var distance = Pass("shaders/distance.slang").Set("nearest", 4.8f).Set("farthest", 5.2f);
                Shaders.SetPasses(camera, new ShaderPass(distance, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle.G > 200 && middle.R < 60, $"the cube's front came out {middle}, so not five units away");
    }

    /// <summary>
    /// A pass binds textures and buffers of its own by name, beside the picture the bridge binds.
    /// </summary>
    [Fact]
    public void APassHasTexturesAndBuffersOfItsOwn()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Red));

                // A white stamp tinted by the last of three colors, which is green, so the whole
                // picture comes out green whatever the camera drew.
                var overlay = Pass("shaders/overlay.slang")
                    .SetTexture("stamp", Render.CreateImage([255, 255, 255, 255], 1, 1))
                    .SetBuffer("tints", Shaders.CreateBuffer<System.Numerics.Vector4>(
                        [ShaderMaterialTests.Red, ShaderMaterialTests.Blue, ShaderMaterialTests.Green]));

                Shaders.SetPasses(camera, new ShaderPass(overlay, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var corner = run.Picture("picture").At(2, 2);
        Assert.True(corner.G > 200 && corner.R < 60, $"the corner came out {corner} rather than green");
    }

    /// <summary>A pass reads Bevy's time through the prelude.</summary>
    [Fact]
    public void APassReadsTheTime()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Shaders.SetPasses(camera, Pass("shaders/clock_pass.slang"));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var corner = run.Picture("picture").At(2, 2);
        Assert.True(corner.G > 200 && corner.R < 60, $"the pass came out {corner}, so it did not see time pass");
    }

    /// <summary>A pass needs an instance.</summary>
    [Fact]
    public void APassWithNoInstanceIsRefused()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.SetPasses(Entity.None, new ShaderPass(default)));
    }

    /// <summary>An instance of the program in <paramref name="file"/> as a pass.</summary>
    /// <summary>
    /// A pass after opaque geometry runs before transparent geometry is drawn, so a see-through pane
    /// in front shows over what it painted; the same pass before tonemapping paints over the pane.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APassAfterOpaqueGeometryIsUnderTransparentGeometry(bool afterOpaque)
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });

                var pane = ecs.Spawn();
                Render.SetMesh(ecs, pane, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 0.1f));
                Render.SetMaterial(ecs, pane, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0f, 0f, 1f, 0.6f),
                    AlphaMode = AlphaMode.Blend,
                    Unlit = true,
                }));
                ecs.Add(pane, Transform.At(0f, 0f, 1f));

                Shaders.SetPasses(
                    camera,
                    new ShaderPass(Pass("shaders/paint_red.slang"), At: afterOpaque ? FramePoint.AfterOpaque : FramePoint.BeforeTonemapping));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var behindPane = picture.At(48, 48);
        var aside = picture.At(4, 4);

        Assert.True(aside.R > 200 && aside.B < 60, $"the picture beside the pane was {aside}");

        if (afterOpaque)
            Assert.True(behindPane.B > 100, $"the pane did not show over the pass: {behindPane}");
        else
            Assert.True(behindPane.R > 200 && behindPane.B < 60, $"the pass did not paint over the pane: {behindPane}");
    }

    private static ShaderInstance Pass(string file) =>
        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = file }));

    /// <summary>An instance of <c>invert.slang</c>, inverting by as much as its <c>amount</c> says.</summary>
    private static ShaderInstance Invert() => Pass("shaders/invert.slang");
}

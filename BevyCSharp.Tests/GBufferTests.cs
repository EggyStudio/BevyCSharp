using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reading Bevy's G-buffer from a shader on a camera drawing deferred, and last frame's
/// prepass beside this frame's.
/// </summary>
[Collection("engine")]
public sealed class GBufferTests
{
    /// <summary>
    /// Two cubes of different materials come out of the G-buffer with their own base color,
    /// metallic and roughness, as <c>bcs_pass::surface_of</c> unpacks them.
    /// </summary>
    [Fact]
    public void APassReadsEachSurfacesMaterial()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var picture = Draw(show: 0);

        // Written as linear numbers into a picture that stores them encoded as sRGB, so 0.8 comes
        // back as 231, 0.3 as 149, 0.1 as 89 and 0.9 as 243.
        var left = picture.At(24, 48);
        var right = picture.At(72, 48);

        Assert.True(Near(left.R, 231) && left.G < 10 && Near(left.B, 149), $"the left cube was {left}");
        Assert.True(Near(right.R, 89) && right.G > 245 && Near(right.B, 243), $"the right cube was {right}");
        Assert.True(picture.At(48, 4) is { R: < 10, G: < 10, B: < 10 }, $"the empty top was {picture.At(48, 4)}");
    }

    /// <summary>
    /// The faces the camera sees come out of the G-buffer with the normals they have: the fronts
    /// facing it, and the tops facing up.
    /// </summary>
    [Fact]
    public void APassReadsEachSurfacesNormal()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var picture = Draw(show: 1);

        // A normal mapped into a color and encoded: plus Z is (188, 188, 255), plus Y (188, 255, 188).
        var front = 0;
        var top = 0;

        for (var y = 0u; y < picture.Height; y++)
        {
            for (var x = 0u; x < picture.Width; x++)
            {
                var pixel = picture.At(x, y);
                if (Near(pixel.R, 188) && Near(pixel.G, 188) && pixel.B > 245) front++;
                if (Near(pixel.R, 188) && pixel.G > 245 && Near(pixel.B, 188)) top++;
            }
        }

        Assert.True(front > 200, $"{front} pixels faced the camera");
        Assert.True(top > 50, $"{top} pixels faced up");
    }

    /// <summary>
    /// A cube jumping between left and right every frame is on one side in this frame's depth and
    /// on the other in <c>depth_previous</c>, so the previous frame's really is the previous one.
    /// </summary>
    [Fact]
    public void APassReadsLastFramesDepth()
    {
        if (!ShaderMaterialTests.CanRun) return;

        Entity cube = default;
        var left = false;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, previous: true);

                cube = PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue), size: 1.5f);

                var pass = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Pass = "shaders/depth_then_and_now.slang",
                }));

                Shaders.SetPasses(camera, new ShaderPass(pass, AfterTonemapping: true));
            },

            EachFrame = world =>
            {
                if (cube == default) return;

                left = !left;
                world.Resource<EcsWorld>().Set(cube, Transform.At(left ? -1.5f : 1.5f, 0f, 0f));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        var picture = run.Picture("picture");
        var onLeft = picture.At(24, 48);
        var onRight = picture.At(72, 48);

        // Whichever side the cube was on when the picture was taken, each side has it in exactly
        // one of the two frames, and the two sides disagree about which.
        var leftNow = onLeft.R > 128;
        var leftThen = onLeft.G > 128;
        var rightNow = onRight.R > 128;
        var rightThen = onRight.G > 128;

        Assert.True(leftNow != leftThen, $"the left was {onLeft}");
        Assert.True(rightNow != rightThen, $"the right was {onRight}");
        Assert.True(leftNow != rightNow, $"both sides agreed: {onLeft} and {onRight}");
    }

    /// <summary>
    /// A camera image filled from the picture before tonemapping, with history, holds last frame's
    /// picture as <c>_previous</c>. A cube flipping between red and green every frame is one color
    /// in the picture and the other in the copy.
    /// </summary>
    [Fact]
    public void ACopyOfThePictureKeepsLastFrames()
    {
        if (!ShaderMaterialTests.CanRun) return;

        ShaderMaterial flat = default;
        var red = false;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Shaders.SetViewImages(camera, new ViewImage(
                    "lit",
                    ShaderImageFormat.Rgba16Float,
                    Scale: 0.5f,
                    History: true,
                    CopyAt: FramePoint.BeforeTonemapping));

                flat = ShaderMaterialTests.Flat(ShaderMaterialTests.Red);
                PictureRun.Cube(ecs, flat);

                var pass = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Pass = "shaders/picture_then_and_now.slang",
                }));

                Shaders.SetPasses(camera, new ShaderPass(pass, AfterTonemapping: true));
            },

            EachFrame = _ =>
            {
                if (flat == default) return;

                red = !red;
                flat.Set("color", red ? ShaderMaterialTests.Red : ShaderMaterialTests.Green);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        var middle = run.Picture("picture").At(48, 48);

        Assert.True(middle.B > 200, $"the copy was empty: {middle}");
        Assert.True((middle.R > 128) != (middle.G > 128), $"the copy was the same frame as the picture: {middle}");
    }

    /// <summary>After the prepass there is nothing to copy, so a copy asked for there is refused.</summary>
    [Fact]
    public void ACopyAfterThePrepassIsRefused()
    {
        if (!App.HasRenderer) return;

        Exception? refused = null;

        new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                refused = Record.Exception(() => Shaders.SetViewImages(camera, new ViewImage(
                    "lit", ShaderImageFormat.Rgba16Float, CopyAt: FramePoint.AfterPrepass)));
            },
        }.Wait(1).Go();

        Assert.NotNull(refused);
    }

    /// <summary>
    /// A pass reading the camera's environment map along each pixel's ray paints what a skybox of
    /// the same cubemap draws, turned the same way, so the direction a shader samples by is the one
    /// Bevy's own sky uses.
    /// </summary>
    [Fact]
    public void APassSeesTheCamerasEnvironment()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var sky = Environment(painted: false);
        var painted = Environment(painted: true);

        long difference = 0;
        long spread = 0;
        var first = sky.At(0, 0);

        for (var y = 0u; y < sky.Height; y++)
        {
            for (var x = 0u; x < sky.Width; x++)
            {
                var a = sky.At(x, y);
                var b = painted.At(x, y);
                difference += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                spread += Math.Abs(a.R - first.R) + Math.Abs(a.G - first.G) + Math.Abs(a.B - first.B);
            }
        }

        var pixels = sky.Width * sky.Height;

        // The sky is not one color, so matching it means matching where each part of it is.
        Assert.True(spread / pixels > 10, $"the sky was nearly one color, {spread / pixels} apart on average");
        Assert.True(difference / pixels < 12, $"the painted sky was {difference / pixels} apart from the skybox on average");
    }

    /// <summary>
    /// A camera turned partway round, lit by and showing the test cubemap, with or without a pass
    /// painting the environment over the whole picture.
    /// </summary>
    private static CapturedImage Environment(bool painted)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1f, 6f));
                // At a corner of the cube, where three of its six flat faces meet.
                ecs.Add(camera, Transform.LookingAt(Vec3.Zero, new Vec3(1f, 0.8f, 1f), Vec3.UnitY));

                var cubemap = AssetServer.Load(AssetKind.Image, "textures/cubemap.png");
                var turn = Quat.FromAxisAngle(Vec3.UnitY, 0.6f);

                Render.SetSkybox(camera, cubemap, brightness: 1000f, rotation: turn);
                Render.SetEnvironmentMap(camera, cubemap, cubemap, intensity: 1000f, rotation: turn);

                if (painted)
                {
                    var pass = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Pass = "shaders/show_environment.slang",
                    }));

                    Shaders.SetPasses(camera, new ShaderPass(pass));
                }
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        return run.Picture("picture");
    }

    /// <summary>
    /// A pass sees Bevy's blue noise: many different values across the picture rather than the
    /// gray that stands in for it, and a different layer on the next frame.
    /// </summary>
    [Fact]
    public void APassSeesBlueNoiseThatMovesEachFrame()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                var pass = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Pass = "shaders/show_blue_noise.slang",
                }));

                Shaders.SetPasses(camera, new ShaderPass(pass, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("first")
            .Capture("second")
            .Go();

        var first = run.Picture("first");
        var second = run.Picture("second");

        var values = new HashSet<byte>();
        var changed = 0;

        for (var y = 0u; y < first.Height; y++)
        {
            for (var x = 0u; x < first.Width; x++)
            {
                values.Add(first.At(x, y).R);
                if (first.At(x, y).R != second.At(x, y).R) changed++;
            }
        }

        Assert.True(values.Count > 100, $"the noise had only {values.Count} different values");
        Assert.True(changed > first.Width * first.Height / 2, $"only {changed} pixels changed from one frame to the next");
    }

    /// <summary>
    /// Light added over the whole picture after opaque geometry, times each pixel's color from the
    /// G-buffer, lights the surfaces and nothing else, which is how a screen-space GI result goes
    /// into the picture before transparency and tonemapping.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IndirectLightIsAddedTimesEachSurfacesColor(bool added)
    {
        if (!ShaderMaterialTests.CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Render.SetAmbientLight(camera, (0f, 0f, 0f), 0f);
                Shaders.SetPrepass(camera, depth: true, deferred: true);

                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f) }), size: 2f);

                if (added)
                {
                    var indirect = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        DrawVertex = "shaders/add_indirect.slang",
                        DrawFragment = "shaders/add_indirect.slang",
                    })).Set("light", new System.Numerics.Vector4(0f, 0.5f, 0f, 0f));

                    Shaders.SetViewDraws(
                        camera,
                        ViewDraw.Fixed(indirect, FramePoint.AfterOpaque, 3, blend: DrawBlend.Add, writesDepth: false));
                }
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        var picture = run.Picture("picture");
        var cube = picture.At(48, 48);
        var empty = picture.At(4, 4);

        Assert.True(empty is { R: < 20, G: < 20, B: < 20 }, $"the empty corner was {empty}");

        if (added)
            Assert.True(cube.G > 80 && cube.R < 30 && cube.B < 30, $"the cube was {cube}");
        else
            Assert.True(cube is { R: < 20, G: < 20, B: < 20 }, $"with nothing added the cube was {cube}");
    }

    private static bool Near(byte value, int expected) => Math.Abs(value - expected) <= 12;

    /// <summary>Two cubes seen from a little above, painted by <c>show_gbuffer.slang</c>.</summary>
    private static CapturedImage Draw(uint show)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 7f));

                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, deferred: true);

                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.8f, 0.2f, 0.2f, 1f),
                    Metallic = 0f,
                    Roughness = 0.3f,
                }), size: 1.5f, at: new Vec3(-1.3f, 0f, 0f));

                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.1f, 0.2f, 0.9f, 1f),
                    Metallic = 1f,
                    Roughness = 0.9f,
                }), size: 1.5f, at: new Vec3(1.3f, 0f, 0f));

                var pass = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Pass = "shaders/show_gbuffer.slang",
                })).Set("show", show);

                Shaders.SetPasses(camera, new ShaderPass(pass, AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        return run.Picture("picture");
    }
}

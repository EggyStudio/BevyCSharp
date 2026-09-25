using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a camera owns for the shaders that run on it: motion vectors, images kept from frame
/// to frame, compute run at a point in its frame, mip levels a level at a time, and dispatches
/// whose size the GPU decides.
/// </summary>
/// <remarks>
/// These are the pieces a screen-space technique is built from, so each test is a small version of
/// one: a temporal accumulation, a depth pyramid, a compaction followed by the work it counted.
/// </remarks>
[Collection("engine")]
public sealed class ViewShaderTests
{
    private const uint Settled = ShaderMaterialTests.Settled;

    private static bool CanRun => ShaderMaterialTests.CanRun;

    private static bool Ready() => ShaderMaterialTests.ProgramsReady();

    private static ShaderInstance Pass(string file) =>
        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = file }));

    private static ShaderInstance Compute(string file) =>
        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = file }));

    /// <summary>
    /// A pass reads the camera's motion vectors, which are there where something moves and zero
    /// where nothing does.
    /// </summary>
    [Fact]
    public void APassSeesWhatMoved()
    {
        if (!CanRun) return;

        var cube = Entity.None;
        var angle = 0f;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, motion: true);

                cube = PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue));
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/motion_mask.slang"), AfterTonemapping: true));
            },

            // Turning every frame, so the capture is of a frame in which it moved.
            EachFrame = world =>
            {
                if (cube == Entity.None) return;

                angle += 0.05f;
                world.Resource<EcsWorld>().Set(cube, Transform.Identity with
                {
                    Rotation = Quat.FromAxisAngle(Vec3.UnitY, angle),
                });
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var middle = picture.At(48, 48);
        var corner = picture.At(2, 2);

        Assert.True(middle.G > 200 && middle.R < 60, $"the turning cube came out {middle}, so no motion was read");
        Assert.True(corner.R > 200 && corner.G < 60, $"the still background came out {corner}, so motion was read where there is none");
    }

    /// <summary>
    /// A compute shader on a camera adds to what it wrote last frame, through an image the camera
    /// keeps with its history, and a pass later in the same frame reads this frame's.
    /// </summary>
    [Fact]
    public void HistoryCarriesFromFrameToFrame()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                Shaders.SetViewImages(camera, new ViewImage("frames", ShaderImageFormat.R32Float, History: true));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(Compute("shaders/count_frames.slang"), FramePoint.BeforeTonemapping));
                Shaders.SetPasses(camera, Pass("shaders/show_frames.slang").Set("threshold", 60f));
            },
        };

        run.Until("compiled", _ => Ready())
            .Wait(5)
            .Capture("early")
            .Wait(Settled)
            .Capture("late")
            .Go();

        var early = run.Picture("early").At(48, 48);
        var late = run.Picture("late").At(48, 48);

        Assert.True(early.R > 200 && early.G < 60, $"after a few frames the count came out {early}, already past sixty");
        Assert.True(late.G > 200 && late.R < 60, $"after many frames the count came out {late}, so history did not carry");
    }

    /// <summary>
    /// A camera's image with mip levels is written a level at a time, each from the one before,
    /// and read whole by a pass sampling its second level.
    /// </summary>
    [Fact]
    public void APyramidIsBuiltALevelAtATime()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                Shaders.SetViewImages(camera, new ViewImage("pyramid", ShaderImageFormat.R32Float, Mips: 4));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(Compute("shaders/fill_level.slang").Set("value", 0.75f), FramePoint.AfterPrepass),
                    ViewDispatch.PerPixel(Compute("shaders/next_level.slang"), FramePoint.AfterPrepass, scale: 0.5f));

                Shaders.SetPasses(camera, Pass("shaders/show_level.slang").Set("expected", 1.5f));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle.G > 200 && middle.R < 60, $"the second level came out {middle} rather than twice the first");
    }

    /// <summary>
    /// One dispatch writes how many workgroups the next runs, and the next runs exactly that many,
    /// which is read back as a count.
    /// </summary>
    [Fact]
    public void TheGpuDecidesHowManyWorkgroupsRun()
    {
        if (!CanRun) return;

        var write = default(ShaderInstance);
        var count = default(ShaderInstance);
        var counts = AssetHandle.None;
        var counter = AssetHandle.None;
        var read = default(BufferRead);
        uint[]? result = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                counts = Shaders.CreateBuffer(16);
                counter = Shaders.CreateBuffer(16);
                write = Compute("shaders/write_counts.slang").SetBuffer("counts", counts).Set("groups", 37u);
                count = Compute("shaders/count_groups.slang").SetBuffer("counter", counter);
            },
        };

        run.Until("compiled", _ => Ready())
            .Do("dispatching", _ =>
            {
                Shaders.Dispatch(write, 1);
                Shaders.DispatchIndirect(count, counts);
            })
            .Wait(2)
            .Do("asking for the count", _ => read = Shaders.BeginBufferRead(counter))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out result))
            .Go();

        Assert.NotNull(result);
        Assert.Equal(37u, result[0]);
    }

    /// <summary>
    /// Occlusion a compute shader writes into the texture Bevy's lighting reads darkens the ambient
    /// light on Bevy's own materials, which is the way in for a package's own ambient occlusion.
    /// </summary>
    [Fact]
    public void OcclusionOfYourOwnReachesBevysLighting()
    {
        if (!CanRun) return;

        var open = AmbientlyLit(1f);
        var hemmed = AmbientlyLit(0f);

        Assert.True(open > 40, $"the cube lit by ambient light alone came out {open}, too dark to compare");
        Assert.True(hemmed < open / 4, $"fully occluded, the cube came out {hemmed} against {open} unoccluded");
    }

    /// <summary>
    /// How bright a white cube lit by ambient light alone comes out with Bevy's occlusion replaced
    /// by <paramref name="occlusion"/> everywhere.
    /// </summary>
    private static int AmbientlyLit(float occlusion)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 4000f);
                Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.Low);

                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(
                        Compute("shaders/write_occlusion.slang").Set("value", occlusion),
                        FramePoint.AfterPrepass));

                PictureRun.Cube(ecs, Render.CreateMaterial(1f, 1f, 1f));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        return (middle.R + middle.G + middle.B) / 3;
    }

    /// <summary>A compute shader on a camera sees the scene's lights and counts them.</summary>
    [Fact]
    public void AComputeShaderSeesTheLights()
    {
        if (!CanRun) return;

        var counts = AssetHandle.None;
        var read = default(BufferRead);
        uint[]? result = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional });
                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, 0f), Vec3.UnitZ));

                foreach (var x in new[] { -3f, 3f })
                {
                    var bulb = Render.SpawnLight(new LightSettings { Kind = LightKind.Point });
                    ecs.Add(bulb, Transform.At(x, 2f, 0f));
                }

                counts = Shaders.CreateBuffer(16);
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.Fixed(
                        Compute("shaders/count_lights.slang").SetBuffer("counts", counts),
                        FramePoint.AfterPrepass,
                        1));

                PictureRun.Cube(ecs, Render.CreateMaterial(1f, 1f, 1f));
            },
        };

        run.Until("compiled", _ => Ready())
            .Wait(10)
            .Do("asking for the counts", _ => read = Shaders.BeginBufferRead(counts))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out result))
            .Go();

        Assert.NotNull(result);
        Assert.Equal(1u, result[0]);
        Assert.Equal(2u, result[1]);
    }

    /// <summary>
    /// A compute shader on a camera reads the directional light's shadow the way Bevy's materials
    /// do, which is what shading a ray's hit needs: the floor under a cube is shadowed, and the
    /// floor away from it is not.
    /// </summary>
    [Fact]
    public void AComputeShaderReadsTheSunsShadow()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 9f, 9f), new Vec3(0f, 0f, 1.5f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, normals: true);

                var sun = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Directional,
                    Intensity = 8000f,
                    Shadows = true,
                });

                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, 0f), Vec3.UnitZ));

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.1f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var block = ecs.Spawn();
                Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));
                Render.SetMaterial(ecs, block, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(block, Transform.At(0f, 3f, 0f));

                Shaders.SetViewImages(camera, new ViewImage("sunlit", ShaderImageFormat.R32Float));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(Compute("shaders/sun_shadow.slang"), FramePoint.AfterPrepass));
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_sunlit.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var shadowed = PictureRun.Green(picture);
        var lit = PictureRun.Red(picture);

        Assert.True(shadowed > 50, $"only {shadowed} pixels came out shadowed");
        Assert.True(lit > shadowed, $"{lit} pixels came out lit against {shadowed} shadowed, under one small block");
    }

    /// <summary>What cannot be a camera's image or dispatch is refused before reaching the engine.</summary>
    [Fact]
    public void AMalformedImageOrDispatchIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Shaders.SetViewImages(
            Entity.None,
            new ViewImage("a", ShaderImageFormat.R32Float),
            new ViewImage("a", ShaderImageFormat.R32Float)));

        Assert.Throws<ArgumentException>(() => Shaders.SetViewImages(
            Entity.None,
            new ViewImage("a", ShaderImageFormat.R32Float, Scale: 0f)));

        Assert.Throws<ArgumentException>(() => Shaders.SetViewDispatches(
            Entity.None,
            ViewDispatch.Fixed(default, FramePoint.AfterPrepass, 1)));

        Assert.Throws<ArgumentException>(() => Shaders.DispatchIndirect(default, AssetHandle.None));
    }
}

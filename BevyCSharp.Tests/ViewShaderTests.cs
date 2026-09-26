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
    /// do, as shading a ray's hit needs. The floor under a cube is shadowed, and the floor away
    /// from it is not.
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

    /// <summary>
    /// A spot light shining down past a block onto a floor is blocked behind the block, read from
    /// its layer of the directional shadow maps, and reaches the floor around it inside its cone.
    /// </summary>
    [Fact]
    public void AComputeShaderReadsASpotLightsShadow()
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

                var spot = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Spot,
                    Intensity = 400_000f,
                    Range = 30f,
                    OuterAngle = 0.7f,
                    InnerAngle = 0.5f,
                    Shadows = true,
                });

                ecs.Add(spot, Transform.LookingAt(new Vec3(0f, 8f, -3f), new Vec3(0f, 0f, 1.5f), Vec3.UnitY));

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
                    ViewDispatch.PerPixel(Compute("shaders/spot_shadow.slang"), FramePoint.AfterPrepass));
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_sunlit.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var shadowed = PictureRun.Green(picture);
        var lit = PictureRun.Red(picture);

        Assert.True(shadowed > 50, $"only {shadowed} pixels came out shadowed");
        Assert.True(lit > 50, $"only {lit} pixels came out lit");

        // The light is behind the block, so its shadow falls on the floor in front of it, toward
        // the camera, and the floor to either side of the shadow is lit.
        var inShadow = picture.At(64, 95);
        var beside = picture.At(15, 95);

        Assert.True(inShadow.G > 200 && inShadow.R < 60, $"the floor in front of the block was {inShadow}");
        Assert.True(beside.R > 200 && beside.G < 60, $"the floor beside the shadow was {beside}");
    }

    private static ShaderInstance Draw(string file) =>
        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
        {
            DrawVertex = file,
            DrawFragment = file,
        }));

    /// <summary>
    /// An orthographic camera eight units across over black, looking down the z axis from six
    /// units away, which puts a world unit sixteen pixels across a picture of 128.
    /// </summary>
    private static Entity Ortho(EcsWorld ecs)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings
        {
            Projection = CameraProjection.Orthographic,
            Height = 8f,
            Clear = ClearMode.Custom,
            ClearColor = (0f, 0f, 0f, 1f),
        });

        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
        return camera;
    }

    private static bool GreenAt(CapturedImage picture, uint x, uint y) =>
        picture.At(x, y) is var pixel && pixel.G > 120 && pixel.R < 90 && pixel.B < 90;

    /// <summary>A camera draws squares placed from a buffer, one instance a square.</summary>
    [Fact]
    public void ACameraDrawsWhatABufferPlaces()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, 0f, 1f), new(2f, 0f, 0f, 1f)]);

                var squares = Draw("shaders/draw_quads.slang")
                    .SetBuffer("centers", centers)
                    .Set("size", 0.5f)
                    .Set("color", ShaderMaterialTests.Green);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(squares, FramePoint.AfterOpaque, 6, 2));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");

        Assert.True(GreenAt(picture, 32, 64) && GreenAt(picture, 96, 64), "the squares were not where the buffer put them");
        Assert.False(GreenAt(picture, 64, 64), "a square was drawn in the middle, where no center puts one");
    }

    /// <summary>
    /// A compute shader writes how many squares a draw draws, and the draw reads the count when it
    /// runs.
    /// </summary>
    [Fact]
    public void AComputeShaderDecidesHowManyAreDrawn()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, 0f, 1f), new(2f, 0f, 0f, 1f)]);
                var counts = Shaders.CreateBuffer(16);

                var count = Compute("shaders/write_draw_counts.slang").SetBuffer("counts", counts).Set("squares", 1u);
                var squares = Draw("shaders/draw_quads.slang")
                    .SetBuffer("centers", centers)
                    .Set("size", 0.5f)
                    .Set("color", ShaderMaterialTests.Green);

                Shaders.SetViewDispatches(camera, ViewDispatch.Fixed(count, FramePoint.AfterOpaque, 1));
                Shaders.SetViewDraws(camera, ViewDraw.Indirect(squares, FramePoint.AfterOpaque, counts));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");

        Assert.True(GreenAt(picture, 32, 64), "the one square counted was not drawn");
        Assert.False(GreenAt(picture, 96, 64), "the square past the count was drawn");
    }

    /// <summary>What a camera draws is hidden by the scene in front of it, through the depth it shares.</summary>
    [Fact]
    public void WhatACameraDrawsIsHiddenBehindTheScene()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);

                // A large square behind a cube in front of it.
                var centers = Shaders.CreateBuffer<Vector4>([new(0f, 0f, -3f, 1f)]);
                var square = Draw("shaders/draw_quads.slang")
                    .SetBuffer("centers", centers)
                    .Set("size", 2f)
                    .Set("color", ShaderMaterialTests.Green);

                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue), 2f);
                Shaders.SetViewDraws(camera, ViewDraw.Fixed(square, FramePoint.AfterOpaque, 6));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var middle = picture.At(64, 64);

        Assert.True(middle.B > 120 && middle.G < 90, $"the middle came out {middle}, so the square was drawn over the cube");
        Assert.True(GreenAt(picture, 64, 40), "the square around the cube was not drawn");
    }

    /// <summary>
    /// Squares drawn into an unsigned integer image the camera clears every frame keep which
    /// instance is nearest at each pixel, a visibility buffer, and the scene in front hides them
    /// there through the camera's depth.
    /// </summary>
    [Fact]
    public void ACameraDrawsIdsIntoAVisibilityBuffer()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetViewImages(camera, new ViewImage("visibility", ShaderImageFormat.R32UInt, ClearEachFrame: true));

                // Two squares behind a cube, each reaching in behind it.
                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, -3f, 1f), new(2f, 0f, -3f, 1f)]);
                var squares = Draw("shaders/draw_ids.slang")
                    .SetBuffer("centers", centers)
                    .Set("size", 1.5f);

                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue), 2f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(squares, FramePoint.AfterOpaque, 6, 2) with { Into = "visibility" });
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_ids.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var left = picture.At(32, 64);
        var right = picture.At(96, 64);
        var hidden = picture.At(56, 64);
        var empty = picture.At(64, 20);

        Assert.True(left.R > 200 && left.G < 60, $"the first square's pixel was {left}");
        Assert.True(right.G > 200 && right.R < 60, $"the second square's pixel was {right}");
        Assert.True(hidden is { R: < 60, G: < 60 }, $"the first square showed through the cube: {hidden}");
        Assert.True(empty is { R: < 60, G: < 60 }, $"a pixel no square covers held an id: {empty}");
    }

    /// <summary>
    /// A square jumping between the left and the right every frame is only where it is this frame
    /// in an image cleared every frame, and leaves its other place behind in one that is not.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnImageClearedEachFrameForgetsLastFramesDraws(bool cleared)
    {
        if (!CanRun) return;

        var centers = AssetHandle.None;
        var left = false;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetViewImages(camera, new ViewImage("visibility", ShaderImageFormat.R32UInt, ClearEachFrame: cleared));

                centers = Shaders.CreateBuffer<Vector4>([new(2f, 0f, 0f, 1f)]);
                var square = Draw("shaders/draw_ids.slang").SetBuffer("centers", centers).Set("size", 1f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(square, FramePoint.AfterOpaque, 6) with { Into = "visibility" });
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_ids.slang"), AfterTonemapping: true));
            },

            EachFrame = _ =>
            {
                if (!centers.IsValid) return;

                left = !left;
                Shaders.WriteBuffer<Vector4>(centers, [new(left ? -2f : 2f, 0f, 0f, 1f)]);
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var marked = (picture.At(32, 64).R > 200 ? 1 : 0) + (picture.At(96, 64).R > 200 ? 1 : 0);

        Assert.Equal(cleared ? 1 : 2, marked);
    }

    /// <summary>
    /// One draw writes two of the camera's images at once, one for each of its fragment shader's
    /// outputs, an integer one and a float one.
    /// </summary>
    [Fact]
    public void ADrawWritesSeveralImagesAtOnce()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetViewImages(
                    camera,
                    new ViewImage("ids", ShaderImageFormat.R32UInt, ClearEachFrame: true),
                    new ViewImage("values", ShaderImageFormat.R32Float, ClearEachFrame: true));

                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, 0f, 1f)]);
                var square = Draw("shaders/draw_two_targets.slang").SetBuffer("centers", centers).Set("size", 1f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(square, FramePoint.AfterOpaque, 6) with { Targets = ["ids", "values"] });
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_two_targets.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var drawn = picture.At(32, 64);
        var empty = picture.At(96, 64);

        // A half written linear into a picture stored as sRGB comes back as 188.
        Assert.True(drawn.R > 200 && drawn.G is > 170 and < 205, $"the square's pixel was {drawn}");
        Assert.True(empty is { R: < 30, G: < 30 }, $"a pixel no square covers was {empty}");
    }

    /// <summary>
    /// A draw after the prepass writes motion into the prepass's own motion texture, which a pass
    /// reading motion then sees where the draw covered and nowhere else, so geometry drawn out of
    /// buffers can give Bevy's temporal effects its motion.
    /// </summary>
    [Fact]
    public void ADrawWritesThePrepassMotion()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, motion: true);

                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, 0f, 1f)]);
                var square = Draw("shaders/draw_motion.slang").SetBuffer("centers", centers).Set("size", 1f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(square, FramePoint.AfterPrepass, 6) with { Into = "motion" });
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/motion_mask.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");

        Assert.True(GreenAt(picture, 32, 64), $"the square's pixel was {picture.At(32, 64)}, with no motion");
        Assert.False(GreenAt(picture, 96, 64), $"a pixel the draw did not cover had motion: {picture.At(96, 64)}");
    }

    /// <summary>
    /// A draw after the prepass writing the nearest depth through <c>SV_Depth</c> hides what Bevy
    /// draws afterward where it wrote, and nowhere else.
    /// </summary>
    [Fact]
    public void ADrawWritesDepthThatHidesTheSceneBehindIt()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Ortho(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, motion: true);

                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Blue), 4f);

                // Over the left half of the cube only.
                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, 3f, 1f)]);
                var cover = Draw("shaders/draw_depth.slang").SetBuffer("centers", centers).Set("size", 1.5f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(cover, FramePoint.AfterPrepass, 6) with { Into = "motion" });
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var covered = picture.At(40, 64);
        var open = picture.At(88, 64);

        Assert.True(covered is { B: < 60 }, $"the cube showed where the draw wrote the nearest depth: {covered}");
        Assert.True(open.B > 150, $"the cube was hidden where the draw did not reach: {open}");
    }

    /// <summary>
    /// A square drawn out of a buffer above a floor shadows it under a sun when it casts shadows,
    /// and not when it does not.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADrawOutOfABufferCastsShadows(bool casts)
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

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, 0f), Vec3.UnitZ));

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.1f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var centers = Shaders.CreateBuffer<Vector4>([new(0f, 3f, 0f, 1f)]);
                var square = Draw("shaders/draw_flat_square.slang").SetBuffer("centers", centers).Set("size", 1f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(square, FramePoint.AfterOpaque, 6) with { CastsShadows = casts });

                Shaders.SetViewImages(camera, new ViewImage("sunlit", ShaderImageFormat.R32Float));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(Compute("shaders/sun_shadow.slang"), FramePoint.AfterPrepass));
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_sunlit.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var shadowed = PictureRun.Green(run.Picture("picture"));

        if (casts)
            Assert.True(shadowed > 50, $"only {shadowed} pixels were shadowed by a square that casts shadows");
        else
            Assert.True(shadowed < 10, $"{shadowed} pixels were shadowed by a square that casts none");
    }

    /// <summary>
    /// A square drawn out of a buffer casts a spot or point light's shadow as well, through the
    /// light's shadow map, which is shared between cameras rather than owned by one, and for a
    /// point light is six views, one a face of its cube.
    /// </summary>
    [Theory]
    [InlineData(true, LightKind.Spot)]
    [InlineData(false, LightKind.Spot)]
    [InlineData(true, LightKind.Point)]
    [InlineData(false, LightKind.Point)]
    public void ADrawOutOfABufferCastsASpotOrPointLightsShadow(bool casts, LightKind kind)
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

                var light = Render.SpawnLight(new LightSettings
                {
                    Kind = kind,
                    Intensity = 400_000f,
                    Range = 30f,
                    OuterAngle = 0.7f,
                    InnerAngle = 0.5f,
                    Shadows = true,
                });
                ecs.Add(light, Transform.LookingAt(new Vec3(0f, 8f, -3f), new Vec3(0f, 0f, 1.5f), Vec3.UnitY));

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.1f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(1f, 1f, 1f));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var centers = Shaders.CreateBuffer<Vector4>([new(0f, 3f, 0f, 1f)]);
                var square = Draw("shaders/draw_flat_square.slang").SetBuffer("centers", centers).Set("size", 1f);

                Shaders.SetViewDraws(camera, ViewDraw.Fixed(square, FramePoint.AfterOpaque, 6) with { CastsShadows = casts });

                Shaders.SetViewImages(camera, new ViewImage("sunlit", ShaderImageFormat.R32Float));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(Compute("shaders/spot_shadow.slang"), FramePoint.AfterPrepass));
                Shaders.SetPasses(camera, new ShaderPass(Pass("shaders/show_sunlit.slang"), AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var shadowed = PictureRun.Green(run.Picture("picture"));

        if (casts)
            Assert.True(shadowed > 50, $"only {shadowed} pixels were shadowed by a square that casts shadows");
        else
            Assert.True(shadowed < 10, $"{shadowed} pixels were shadowed by a square that casts none");
    }

    /// <summary>
    /// A draw reads one of the camera's images that a dispatch at the same point wrote.
    /// </summary>
    [Fact]
    public void ADrawReadsAnImageADispatchWrote()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetViewImages(camera, new ViewImage("level", ShaderImageFormat.R32Float));

                var fill = Compute("shaders/fill_image_level.slang").Set("value", 1f);
                Shaders.SetViewDispatches(camera, ViewDispatch.PerPixel(fill, FramePoint.AfterOpaque));
                Shaders.SetViewDraws(camera, ViewDraw.Fixed(Draw("shaders/draw_reads_image.slang"), FramePoint.AfterOpaque, 3, writesDepth: false));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle.R > 200 && middle.B < 60, $"the draw read {middle}, not the image the dispatch filled");
    }

    /// <summary>
    /// A watch draws a camera's single-channel float image, scaled, into an image anything can show
    /// and a capture can read.
    /// </summary>
    [Fact]
    public void AWatchShowsACamerasImage()
    {
        if (!CanRun) return;

        var watched = AssetHandle.None;
        var capture = default(Capture);
        CapturedImage? shown = null;
        IReadOnlyList<string> names = [];

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);

                Shaders.SetViewImages(camera, new ViewImage("amount", ShaderImageFormat.R32Float));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(Compute("shaders/fill_gradient.slang"), FramePoint.AfterPrepass));

                // A quarter doubled, so a half, which shows as middle gray.
                watched = Shaders.Watch(camera, "amount", 16, 16, scale: 2f);
                names = Shaders.ViewImageNames(camera);
            },
        };

        run.Until("compiled", _ => Ready())
            .Wait(Settled)
            .Do("capturing the watch", _ => capture = Render.BeginCapture(watched))
            .Until("captured", _ => Render.TryReadCapture(capture, out shown))
            .Go();

        Assert.Contains("amount", names);
        Assert.NotNull(shown);
        var middle = shown.At(8, 8);

        Assert.InRange(middle.R, 120, 136);
        Assert.Equal(middle.R, middle.G);
        Assert.Equal(middle.R, middle.B);
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

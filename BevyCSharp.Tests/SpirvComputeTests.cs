using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers compute shaders compiled to SPIR-V and handed to the driver untouched, and the rays they
/// trace through <c>bcs_ray</c> against the scene Solari keeps.
/// </summary>
/// <remarks>
/// Nothing checks SPIR-V passed through before the GPU runs it, so each test reads back numbers
/// that only a shader bound exactly as it declared itself could produce.
/// </remarks>
[Collection("engine")]
public sealed class SpirvComputeTests
{
    /// <summary>
    /// The same shader the WGSL test triples a buffer with, compiled to SPIR-V instead, does the
    /// same, which puts its loose number in a uniform buffer, its buffer at the binding Slang gave
    /// it, and the time it imports in group one.
    /// </summary>
    [Fact]
    public void ASpirvComputeShaderChangesABufferThatIsReadBack()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var scale = default(ShaderInstance);
        var buffer = AssetHandle.None;
        var read = default(BufferRead);
        float[]? numbers = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                buffer = Shaders.CreateBuffer<float>(Enumerable.Range(0, 100).Select(i => (float)i).ToArray());
                scale = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Compute = "shaders/scale.slang",
                        ComputeTarget = ShaderTarget.SpirV,
                    }))
                    .SetBuffer("numbers", buffer)
                    .Set("factor", 3f);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("tripling", _ => Shaders.Dispatch(scale, 2))
            .Wait(2)
            .Do("asking for it back", _ => read = Shaders.BeginBufferRead(buffer))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out numbers))
            .Go();

        Assert.NotNull(numbers);
        Assert.Equal(100, numbers.Length);

        for (var i = 0; i < 100; i++) Assert.Equal(i * 3f, numbers[i]);
    }

    /// <summary>
    /// A program compiled both ways is two programs, each with the layout its own compile says,
    /// and both declare the same number by name.
    /// </summary>
    [Fact]
    public void BothTargetsOfOneFileAreSeparatePrograms()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var wgsl = default(ShaderProgram);
        var spirv = default(ShaderProgram);

        var run = new PictureRun
        {
            Scene = _ =>
            {
                wgsl = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/scale.slang" });
                spirv = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Compute = "shaders/scale.slang",
                    ComputeTarget = ShaderTarget.SpirV,
                });
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Go();

        Assert.NotEqual(wgsl, spirv);
        Assert.Contains("factor", wgsl.Layout);
        Assert.Contains("factor", spirv.Layout);
        Assert.Contains("numbers", spirv.Layout);
    }

    /// <summary>
    /// A dispatch on a camera, compiled to SPIR-V, reads the camera's depth and normals through
    /// <c>bcs_pass</c> and traces a ray toward the sun from every surface, which shadows the floor
    /// under a cube and nowhere else.
    /// </summary>
    /// <remarks>
    /// Shadows traced this way need no shadow map, so the floor under the cube is dark here with
    /// no light in the scene at all. Runs only where Solari runs.
    /// </remarks>
    [Fact]
    public void ShadowsAreTracedPerPixelOnACamera()
    {
        if (!App.HasRenderer) return;

        var active = false;

        var run = new PictureRun
        {
            Configure = config => config.RayTracedLighting = true,
            Scene = ecs =>
            {
                active = Render.RayTracingActive;
                if (!active) return;

                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 6f));
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, normals: true);

                var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f) });

                var floorMesh = Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f);
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, floorMesh);
                Render.SetMaterial(ecs, floor, white);
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));
                Render.SetRayTraced(floor, floorMesh);

                // Hanging a unit above the floor, so the camera sees the floor under it.
                var cubeMesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, cubeMesh);
                Render.SetMaterial(ecs, cube, white);
                ecs.Add(cube, Transform.At(0f, 1.5f, 0f));
                Render.SetRayTraced(cube, cubeMesh);

                Shaders.SetViewImages(camera, new ViewImage("lit", ShaderImageFormat.R32Float));
                Shaders.SetViewDispatches(
                    camera,
                    ViewDispatch.PerPixel(
                        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                            {
                                Compute = "shaders/trace_sun.slang",
                                ComputeTarget = ShaderTarget.SpirV,
                            }))
                            .Set("toward_sun", new Vector3(0f, 1f, 0f)),
                        FramePoint.AfterPrepass));
                Shaders.SetPasses(
                    camera,
                    new ShaderPass(
                        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/show_lit.slang" })),
                        AfterTonemapping: true));
            },
        };

        run.Until("compiled", _ => !active || ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("picture")
            .Go();

        if (!active) return;

        var picture = run.Picture("picture");

        // The middle of the picture is the floor right under the cube, and the right edge is floor
        // well clear of it.
        var under = picture.At(48, 48);
        var clear = picture.At(90, 60);

        Assert.True(under.R < 30, $"the floor under the cube came out {under}, so no shadow was traced");
        Assert.True(clear.R > 225, $"the floor beside the cube came out {clear}, so it was shadowed");
    }

    /// <summary>
    /// Rays traced straight down across a red cube Solari keeps meet its top at the height it is,
    /// with its color, and meet nothing beside it.
    /// </summary>
    /// <remarks>
    /// Runs only where Solari runs, in a bridge built with <c>--solari</c> on an adapter with ray
    /// queries, and returns early anywhere else.
    /// </remarks>
    [Fact]
    public void RaysTracedByAComputeShaderMeetTheSceneSolariKeeps()
    {
        if (!App.HasRenderer) return;

        var active = false;
        var trace = default(ShaderInstance);
        var hits = AssetHandle.None;
        var read = default(BufferRead);
        Vector4[]? found = null;

        var run = new PictureRun
        {
            Configure = config => config.RayTracedLighting = true,
            Scene = ecs =>
            {
                active = Render.RayTracingActive;
                if (!active) return;

                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 6f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });

                // Two units across with its top at one, so a ray from ten meets it after nine.
                var mesh = Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f);
                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, mesh);
                Render.SetMaterial(ecs, cube, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0f, 0f, 1f) }));
                ecs.Add(cube, Transform.At(0f, 0f, 0f));
                Render.SetRayTraced(cube, mesh);

                // Nine rays a unit apart, from four to the left to four to the right.
                hits = Shaders.CreateBuffer<Vector4>(new Vector4[9]);
                trace = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Compute = "shaders/trace_down.slang",
                        ComputeTarget = ShaderTarget.SpirV,
                    }))
                    .SetBuffer("hits", hits)
                    .Set("height", 10f)
                    .Set("spacing", 1f);
            },
        };

        run.Until("compiled", _ => !active || ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Do("tracing", _ =>
            {
                if (active) Shaders.Dispatch(trace, 1);
            })
            .Wait(2)
            .Do("asking for it back", _ =>
            {
                if (active) read = Shaders.BeginBufferRead(hits);
            })
            .Until("read back", _ => !active || Shaders.TryReadBuffer(read, out found))
            .Go();

        if (!active) return;

        Assert.NotNull(found);

        // Two, three and four units to either side are clear of a cube reaching one either way,
        // and the ray down the middle lands on its top.
        foreach (var index in new[] { 0, 1, 2, 6, 7, 8 })
        {
            Assert.True(found[index].W < 0f, $"ray {index} met something ({found[index]}) beside the cube");
        }

        var middle = found[4];
        Assert.InRange(middle.W, 8.99f, 9.01f);
        Assert.True(middle.X > 0.9f && middle.Y < 0.1f && middle.Z < 0.1f, $"the middle ray met {middle} rather than red");
    }
}

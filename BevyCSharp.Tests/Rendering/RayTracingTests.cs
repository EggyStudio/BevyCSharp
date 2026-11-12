using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's ray-traced lighting, which runs only in a bridge built with <c>--solari</c> on an
/// adapter that traces rays, and is skipped anywhere else.
/// </summary>
[Collection("engine")]
public sealed class RayTracingTests
{
    /// <summary>
    /// An emissive red wall beside a white floor, with no lamp and no ambient light, lights the
    /// floor red when the camera traces rays, since an emissive surface is a light to them, and
    /// leaves it dark when it does not.
    /// </summary>
    [SkippableFact]
    public void AGlowingWallLightsTheFloorWhenRaysAreTraced()
    {
        Needs.Renderer();

        var traced = Floor(traced: true);
        Needs.RayTracing(traced is not null);
        var lit = traced!.Value;

        var raster = Floor(traced: false)!.Value;

        Assert.True(lit.R > 40 && lit.R > lit.G + 20, $"the floor by the wall was {lit} with rays traced");
        Assert.True(raster.R < 15, $"the floor by the wall was {raster} without");
    }

    /// <summary>
    /// A camera given ray-traced lighting has Bevy's ReSTIR on, which Bevy leaves off for a
    /// denoiser, so the bridge's own denoiser is left less noise to take away.
    /// </summary>
    [SkippableFact]
    public void RayTracedLightingReusesItsSamples()
    {
        Needs.Renderer();

        bool? restir = null;
        var run = new PictureRun
        {
            Configure = config => config.RayTracedLighting = true,
            Scene = ecs =>
            {
                if (!Render.RayTracingActive) return;

                var camera = PictureRun.Camera(ecs);
                Render.SetRayTracedLighting(camera, true);
                restir = ecs.Wrap<Bevy.Reflected.SolariLightingRef>(camera).Restir;
            },
        };

        run.Wait(2).Go();
        Needs.RayTracing(restir is not null);

        Assert.True(restir, "the camera traced rays with ReSTIR off");
    }

    /// <summary>
    /// An app with no ray-traced lighting, which a headless bridge never has, has no denoiser, says
    /// so, and refuses to have it turned on rather than pretending.
    /// </summary>
    [Fact]
    public void AnAppWithoutRayTracedLightingHasNoDenoiser()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            Assert.False(Render.RayTracedDenoiserActive);

            var refused = Assert.Throws<BevyNativeException>(() => Render.SetRayTracedDenoiser(true));
            Assert.Equal(NativeStatus.Unsupported, refused.Status);
        });

        harness.Run();
    }

    /// <summary>
    /// Settings the filter cannot use, a history under a frame or a width of nought, are refused
    /// before they reach the bridge, which needs no ray tracing to see.
    /// </summary>
    [Fact]
    public void DenoiserSettingsItCannotUseAreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetRayTracedDenoiser(true, new DenoiserSettings { HistoryLimit = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetRayTracedDenoiser(true, new DenoiserSettings { DepthWidth = 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetRayTracedDenoiser(true, new DenoiserSettings { NormalPower = float.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetRayTracedDenoiser(true, new DenoiserSettings { LuminanceWidth = -1f }));
    }

    /// <summary>
    /// Where ray-traced lighting runs, the denoiser is on without being asked, and is turned off and
    /// on again.
    /// </summary>
    [SkippableFact]
    public void TheDenoiserIsOnWhereRaysAreTracedAndTurnsOff()
    {
        Needs.Renderer();

        bool? first = null, off = null, on = null;
        var run = new PictureRun
        {
            Configure = config => config.RayTracedLighting = true,
            Scene = _ =>
            {
                if (!Render.RayTracingActive) return;

                first = Render.RayTracedDenoiserActive;
                Render.SetRayTracedDenoiser(false);
                off = Render.RayTracedDenoiserActive;
                Render.SetRayTracedDenoiser(true, new DenoiserSettings { HistoryLimit = 8 });
                on = Render.RayTracedDenoiserActive;
            },
        };

        run.Wait(2).Go();
        Needs.RayTracing(first is not null);

        Assert.True(first, "the denoiser was off before it was asked for anything");
        Assert.False(off, "the denoiser was still on after it was turned off");
        Assert.True(on, "the denoiser was off after it was turned on again");
    }

    /// <summary>
    /// The denoiser's five stages over a picture of 1920 by 1080 with a floor and a glowing wall
    /// filling it, every pixel filtered, take no more of the GPU than their budget, as Bevy's
    /// diagnostics measure them, the least of 600 frames' measurements.
    /// </summary>
    /// <remarks>
    /// A pass's time on the GPU counts the time the GPU spent on another program's work between its
    /// two timestamps, so a reading smoothed over frames, as <c>render.timings</c> gives it, runs
    /// high beside a game drawing on the same GPU. A frame's own measurement taken between the
    /// other program's is the node's alone, so the test takes the least of many.
    /// </remarks>
    [SkippableFact]
    public void TheDenoiserKeepsToItsBudgetAt1080p()
    {
        Needs.Renderer();

        // The budget is the stages' least frame on an RTX 4070 Laptop GPU, 2.5 ms, with a quarter
        // more, so a change that makes them slower fails.
        const double Budget = 3.2;

        var active = false;
        double? least = null;
        var frames = 0;
        new PictureRun
        {
            Width = 1920,
            Height = 1080,
            Configure = config => (config.RayTracedLighting, config.GpuTimings) = (true, true),
            Scene = ecs =>
            {
                active = Render.RayTracingActive;
                if (!active) return;

                // Looking down at the floor, which fills the picture to its top edge.
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 4f, 2f));
                Render.SetRayTracedLighting(camera, true);

                var floorMesh = Render.CreateMesh(MeshShape.Cuboid, 40f, 0.1f, 40f);
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, floorMesh);
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f }));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));
                Render.SetRayTraced(floor, floorMesh);

                var wallMesh = Render.CreateMesh(MeshShape.Cuboid, 0.2f, 3f, 4f);
                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, wallMesh);
                Render.SetMaterial(ecs, wall, Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Emissive = (4000f, 0f, 0f, 0f) }));
                ecs.Add(wall, Transform.At(-1.5f, 1.5f, 0f));
                Render.SetRayTraced(wall, wallMesh);
            },
        }
            .Wait(ShaderMaterialTests.Settled + 90)
            .Until("reading the timings over 600 frames", _ =>
            {
                if (Diagnostics.TryRead("render/bcs_denoiser/elapsed_gpu", out var reading) && reading.Value is { } taken)
                    least = Math.Min(least ?? taken, taken);
                return ++frames >= 600;
            })
            .Go();

        Needs.RayTracing(active);

        Assert.True(least is not null, "the denoiser was not timed");
        Assert.InRange(least.Value, 0, Budget);
    }

    /// <summary>
    /// A still view's denoised light keeps the slope its grainy frames' mean holds beside a lamp
    /// hung under a ceiling, the band one to five pixels from the lamp within 15% of that mean, read
    /// before tonemapping.
    /// </summary>
    /// <remarks>
    /// A filter that weighed each tap against one frame's variance rather than its history's mean's,
    /// and kept its own filtered light as the next frame's history, would carry light down the
    /// slope and out from the lamp, leaving this band dark and a ring further out bright.
    /// </remarks>
    [SkippableFact]
    public void TheDenoiserKeepsTheSlopeBesideALamp()
    {
        Needs.Renderer();

        const int Grainy = 24;
        var supported = true;
        var reading = false;
        var taken = 0;
        float[]? sum = null;
        float[]? denoised = null;

        // Reads the camera's light before tonemapping over the frames it takes, handing it on once read.
        bool Read(World world, Action<float[]> keep)
        {
            var ecs = world.Resource<EcsWorld>();
            if (!reading)
            {
                reading = true;
                Assert.Null(GiReference.BeginPicture(ecs, null));
                return false;
            }
            if (GiReference.Step(ecs) is null) return false;
            reading = false;
            keep(GiReference.LastPicture!.Value.Light);
            return true;
        }

        new PictureRun
        {
            Width = 256,
            Height = 256,
            Configure = config => config.RayTracedLighting = true,
            Scene = ecs =>
            {
                supported = Render.RayTracingActive;
                if (!supported) return;

                var camera = PictureRun.Camera(ecs, new Vec3(0f, 0f, 3f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetRayTracedLighting(camera, true);
                Render.SetRayTracedDenoiser(false);

                // A closed white room round the camera, so its light bounces as in the feature
                // test's Cornell box and is as grainy, and a lamp of that box's light hung two
                // centimeters under its ceiling, whose back and sides light the ceiling by its edge.
                // The lamp's emissive takes none of the exposure, which rays pass over, so the
                // emissive's mend (render/mended.rs) is no part of what is read.
                var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.9f, 0.9f, 0.9f, 1f), Roughness = 1f });
                void Slab(float x, float y, float z, float width, float height, float depth)
                {
                    var mesh = Render.CreateMesh(MeshShape.Cuboid, width, height, depth);
                    var slab = ecs.Spawn();
                    Render.SetMesh(ecs, slab, mesh);
                    Render.SetMaterial(ecs, slab, white);
                    ecs.Add(slab, Transform.At(x, y, z));
                    Render.SetRayTraced(slab, mesh);
                }
                Slab(0f, 0f, 0f, 4f, 4f, 0.1f);
                Slab(0f, 0f, 4f, 4f, 4f, 0.1f);
                Slab(-2f, 0f, 2f, 0.1f, 4f, 4f);
                Slab(2f, 0f, 2f, 0.1f, 4f, 4f);
                Slab(0f, -2f, 2f, 4f, 0.1f, 4f);
                Slab(0f, 2f, 2f, 4f, 0.1f, 4f);

                var lampMesh = Render.CreateMesh(MeshShape.Cuboid, 0.8f, 0.8f, 0.06f);
                var lamp = ecs.Spawn();
                Render.SetMesh(ecs, lamp, lampMesh);
                Render.SetMaterial(ecs, lamp, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Emissive = (40_000f, 38_000f, 34_000f, 0f) }));
                ecs.Add(lamp, Transform.At(0f, 0f, 0.1f));
                Render.SetRayTraced(lamp, lampMesh);
            },
        }
            .Until("ready", _ => Render.PipelinesReady())
            // Solari's world cache settled first, since grainy frames taken sixty frames in read
            // the room darker than the denoised frame taken after them.
            .Wait(300)
            .Until("the grainy frames", world => !supported || (Read(world, light =>
            {
                sum ??= new float[light.Length];
                for (var i = 0; i < light.Length; i++) sum[i] += light[i];
                taken++;
            }) && taken >= Grainy))
            .Do("the denoiser on", _ =>
            {
                if (supported) Render.SetRayTracedDenoiser(true);
            })
            // The history's 32 frames and as many again, so the view is settled.
            .Wait(64)
            .Until("the denoised frame", world => !supported || Read(world, light => denoised = light))
            .Go();

        Needs.RayTracing(supported);

        const int Width = 256;
        var pixels = sum!.Length / 3;
        float Luminance(float[] light, int i) => 0.2126f * light[i * 3] + 0.7152f * light[i * 3 + 1] + 0.0722f * light[i * 3 + 2];
        var mean = Enumerable.Range(0, pixels).Select(i => Luminance(sum, i) / taken).ToArray();
        var filtered = Enumerable.Range(0, pixels).Select(i => Luminance(denoised!, i)).ToArray();

        // The lamp's face, as bright as the middle of the denoised frame, which is the lamp's light
        // with no grain, where the grainy frames' brightest pixel can be one that caught a sample
        // far over the lamp, and each other pixel's distance from it in steps across and down.
        var face = filtered[Width / 2 * Width + Width / 2];
        var distance = Enumerable.Repeat(-1, pixels).ToArray();
        var queue = new Queue<int>();
        for (var i = 0; i < pixels; i++)
        {
            if (filtered[i] < 0.9f * face) continue;
            distance[i] = 0;
            queue.Enqueue(i);
        }
        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            var (x, y) = (i % Width, i / Width);
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx < 0 || ny < 0 || nx >= Width || ny >= pixels / Width) continue;
                var next = ny * Width + nx;
                if (distance[next] >= 0) continue;
                distance[next] = distance[i] + 1;
                queue.Enqueue(next);
            }
        }

        var edge = Enumerable.Range(0, pixels).Where(i => distance[i] > 1 && distance[i] <= 5).ToArray();
        Assert.True(edge.Length > 500, $"the band by the lamp holds {edge.Length} pixels");
        var share = edge.Sum(i => filtered[i]) / edge.Sum(i => mean[i]);
        Assert.True(Math.Abs(share - 1f) < 0.15f, $"the band by the lamp is denoised to {share:0.000} of its grainy frames' mean");
    }

    /// <summary>
    /// An emissive wall made of a mesh with no texture coordinates, as a model drawn in plain
    /// colors often is, is given coordinates of zero rather than refused, and lights the floor as
    /// one with coordinates does.
    /// </summary>
    [SkippableFact]
    public void AMeshWithNoTextureCoordinatesIsTracedAllTheSame()
    {
        Needs.Renderer();

        var traced = Floor(traced: true, bareWall: true);
        Needs.RayTracing(traced is not null);
        var lit = traced!.Value;

        Assert.True(lit.R > 40 && lit.R > lit.G + 20, $"the floor by a wall with no texture coordinates was {lit}");
    }

    /// <summary>
    /// A camera drawn by Bevy's path tracer, in an app that asked for it, sees the emissive wall
    /// light the floor red from its rays alone, the raster meshes taken away as Bevy's solari
    /// example takes them.
    /// </summary>
    [SkippableFact]
    public void ThePathTracerLightsTheFloorFromItsRaysAlone()
    {
        Needs.Renderer();

        var traced = Floor(traced: true, pathTraced: true);
        Needs.RayTracing(traced is not null);
        var lit = traced!.Value;

        Assert.True(lit.R > 40 && lit.R > lit.G + 20, $"the floor by the wall was {lit} with the path tracer");
    }

    /// <summary>A camera is refused the path tracer in an app that did not ask for it.</summary>
    [SkippableFact]
    public void ThePathTracerIsRefusedWhereTheAppDidNotAskForIt()
    {
        Needs.Renderer();

        var active = false;
        Exception? refused = null;
        var run = new PictureRun
        {
            Configure = config => config.RayTracedLighting = true,
            Scene = ecs =>
            {
                active = Render.RayTracingActive;
                if (active) refused = Record.Exception(() => Render.SetPathTraced(PictureRun.Camera(ecs), true));
            },
        };

        run.Wait(2).Go();
        Needs.RayTracing(active);

        Assert.Equal(NativeStatus.Unsupported, Assert.IsType<BevyNativeException>(refused).Status);
    }

    /// <summary>
    /// A lens and occlusion set after the rays are turned on leave the rays lighting the floor,
    /// though taking motion blur and occlusion off takes away prepasses of their own.
    /// </summary>
    /// <remarks>
    /// The rays read the motion vectors, so a lens set after them that takes off a motion blur
    /// never on keeps the motion vectors the rays ask for, where taking them away with it would
    /// leave a scene drawn unlit.
    /// </remarks>
    [SkippableFact]
    public void EffectsSetAfterTheRaysLeaveThemLighting()
    {
        Needs.Renderer();

        var traced = Floor(traced: true, effectsAfter: true);
        Needs.RayTracing(traced is not null);
        var lit = traced!.Value;

        Assert.True(lit.R > 40 && lit.R > lit.G + 20, $"the floor by the wall was {lit} with effects set after the rays");
    }

    /// <summary>
    /// The floor's color beside the wall, or null where ray tracing is not running. A bare wall is
    /// a single face built point by point, with normals and no texture coordinates.
    /// </summary>
    private static (byte R, byte G, byte B, byte A)? Floor(bool traced, bool bareWall = false, bool effectsAfter = false, bool pathTraced = false)
    {
        var active = false;

        var run = new PictureRun
        {
            Configure = config => (config.RayTracedLighting, config.PathTracer) = (true, pathTraced),
            Scene = ecs =>
            {
                active = Render.RayTracingActive;
                if (!active) return;

                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 6f));
                Render.SetAmbientLight(camera, (0f, 0f, 0f), 0f);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                if (pathTraced) Render.SetPathTraced(camera, true);
                else if (traced) Render.SetRayTracedLighting(camera, true);

                // A lens with no motion blur and occlusion put on and taken off, each of which
                // takes off prepasses of its own as it goes.
                if (effectsAfter)
                {
                    Render.SetEffects(camera, new EffectSettings { Vignette = 0.3f });
                    Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.Low);
                    Render.SetAmbientOcclusion(camera, null);
                }

                var floorMesh = Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f);
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, floorMesh);
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f }));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var wallMesh = bareWall
                    ? Render.CreateMesh(new MeshData
                    {
                        Positions = [new(0f, -1.5f, -2f), new(0f, 1.5f, -2f), new(0f, 1.5f, 2f), new(0f, -1.5f, 2f)],
                        Normals = [Vec3.UnitX, Vec3.UnitX, Vec3.UnitX, Vec3.UnitX],
                        Indices = [0, 1, 2, 0, 2, 3],
                    })
                    : Render.CreateMesh(MeshShape.Cuboid, 0.2f, 3f, 4f);
                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, wallMesh);
                Render.SetMaterial(ecs, wall, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0f, 0f, 0f, 1f),
                    Emissive = (4000f, 0f, 0f, 0f),
                }));
                ecs.Add(wall, Transform.At(-1.5f, 1.5f, 0f));

                if (traced)
                {
                    Render.SetRayTraced(floor, floorMesh);
                    Render.SetRayTraced(wall, wallMesh);
                }

                if (pathTraced)
                {
                    ecs.Wrap<Bevy.Reflected.Mesh3dRef>(floor).Remove();
                    ecs.Wrap<Bevy.Reflected.Mesh3dRef>(wall).Remove();
                }
            },
        };

        run.Wait(ShaderMaterialTests.Settled + 90).Capture("picture").Go();

        if (!active) return null;

        // The floor right beside the wall, below the middle of the picture.
        return run.Picture("picture").At(52, 70);
    }
}

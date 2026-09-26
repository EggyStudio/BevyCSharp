using System.Numerics;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers light probes: reflection probes lit by baked cubemaps, and irradiance volumes lit from a
/// 3D image, including one a compute shader writes.
/// </summary>
[Collection("engine")]
public sealed class LightProbeTests
{
    /// <summary>
    /// A white cube inside an irradiance volume a compute shader filled takes each face's light from
    /// the direction that face looks, so the packing the shader wrote through <c>bcs_scene</c> is
    /// the one Bevy reads.
    /// </summary>
    [Fact]
    public void AComputedVolumeLightsEachFaceFromItsOwnDirection()
    {
        if (!App.HasRenderer) return;

        var picture = VolumeScene(filled: true);

        // The top faces up and the front faces the camera. Were the halves of an axis the other way
        // round, the front would be blue rather than red, and were the axes swapped, the top would
        // be red.
        var top = picture.At(48, 30);
        var front = picture.At(48, 62);

        Assert.True(top.G > 60 && top.G > top.R + 40 && top.G > top.B + 40, $"the top was {top}");
        Assert.True(front.R > 60 && front.R > front.G + 40 && front.R > front.B + 40, $"the front was {front}");
    }

    /// <summary>The same scene with the volume never written is dark, so the light came from it.</summary>
    [Fact]
    public void AnEmptyVolumeLightsNothing()
    {
        if (!App.HasRenderer) return;

        var picture = VolumeScene(filled: false);
        var top = picture.At(48, 30);
        var front = picture.At(48, 62);

        Assert.True(top.R + top.G + top.B < 30, $"the top was {top}");
        Assert.True(front.R + front.G + front.B < 30, $"the front was {front}");
    }

    /// <summary>
    /// A flat image cannot be a volume and a camera cannot be a probe, and both are refused at the
    /// call rather than failing a frame later.
    /// </summary>
    [Fact]
    public void AFlatImageOrACameraIsRefused()
    {
        if (!App.HasRenderer) return;

        Exception? flat = null;
        Exception? camera = null;

        new PictureRun
        {
            Scene = ecs =>
            {
                var probe = ecs.Spawn();
                ecs.Add(probe, Transform.Identity);

                var image = Shaders.CreateImage(4, 4, ShaderImageFormat.Rgba16Float);
                flat = Record.Exception(() => Render.SetIrradianceVolume(probe, image));

                var view = Render.SpawnCamera3d(new CameraSettings());
                var volume = Shaders.CreateImage(2, 4, ShaderImageFormat.Rgba16Float, depth: 6);
                camera = Record.Exception(() => Render.SetIrradianceVolume(view, volume));
            },
        }.Wait(1).Go();

        Assert.IsType<BevyNativeException>(flat);
        Assert.IsType<BevyNativeException>(camera);
    }

    /// <summary>
    /// A smooth sphere inside a reflection probe reflects the probe's cubemap, and one outside it
    /// reflects nothing, since the camera has no environment of its own.
    /// </summary>
    [Fact]
    public void AReflectionProbeLightsOnlyWhatIsInsideIt()
    {
        if (!App.HasRenderer) return;

        var picture = new PictureRun
        {
            Width = 128,
            Height = 64,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                    FieldOfView = 40f,
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
                Render.SetAmbientLight((0f, 0f, 0f), 0f);

                var cubemap = AssetServer.Load(AssetKind.Image, "textures/cubemap.png");

                // A box three units across around the left sphere only.
                var probe = ecs.Spawn();
                ecs.Add(probe, new Transform
                {
                    Translation = new Vec3(-1.2f, 0f, 0f),
                    Rotation = Quat.Identity,
                    Scale = new Vec3(2f, 2f, 2f),
                });
                Render.SetReflectionProbe(probe, cubemap, cubemap, intensity: 3000f);

                foreach (var x in new[] { -1.2f, 1.2f })
                {
                    var ball = ecs.Spawn();
                    Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 0.8f));
                    Render.SetMaterial(ecs, ball, Render.CreateMaterial(new MaterialSettings
                    {
                        BaseColor = (0.9f, 0.9f, 0.9f, 1f),
                        Metallic = 1f,
                        Roughness = 0.2f,
                    }));
                    ecs.Add(ball, Transform.At(x, 0f, 0f));
                }
            },
        };

        picture.Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var shot = picture.Picture("picture");
        var inside = Brightness(shot, 0, 64);
        var outside = Brightness(shot, 64, 128);

        Assert.True(inside > outside * 4 + 100, $"inside the probe was {inside}, outside {outside}");
    }

    /// <summary>
    /// A mirror sphere inside a probe that captures its surroundings reflects each glowing block on
    /// the side it stands, so every face camera drew into the layer Bevy reads for that direction.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACapturedProbeReflectsEachSideWhereItIs(bool live)
    {
        if (!App.HasRenderer) return;

        var picture = CaptureScene(live);

        var right = picture.At(68, 48);
        var left = picture.At(28, 48);
        var top = picture.At(48, 28);
        var middle = picture.At(48, 48);

        Assert.True(right.R > right.G + 30 && right.R > right.B + 30, $"the right was {right}");
        Assert.True(left.G > left.R + 30 && left.G > left.B + 30, $"the left was {left}");
        Assert.True(top.B > top.R + 30 && top.B > top.G + 30, $"the top was {top}");
        Assert.True(middle.R > middle.B + 30 && middle.G > middle.B + 30, $"the middle was {middle}");
    }

    /// <summary>A probe capture needs a size Bevy can filter, so anything else is refused.</summary>
    [Fact]
    public void ACaptureSizeThatIsNotAPowerOfTwoIsRefused()
    {
        if (!App.HasRenderer) return;

        Exception? refused = null;

        new PictureRun
        {
            Scene = ecs =>
            {
                var probe = ecs.Spawn();
                ecs.Add(probe, Transform.Identity);
                refused = Record.Exception(() => Render.SetProbeCapture(probe, new ProbeCaptureSettings { Size = 100 }));
            },
        }.Wait(1).Go();

        Assert.IsType<BevyNativeException>(refused);
    }

    /// <summary>
    /// A smooth sphere in a large capturing probe, a red block to its right, green to its left,
    /// blue above and yellow behind the camera, all glowing and all out of the camera's view.
    /// </summary>
    private static CapturedImage CaptureScene(bool live)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                    FieldOfView = 40f,
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
                Render.SetAmbientLight((0f, 0f, 0f), 0f);

                var probe = ecs.Spawn();
                ecs.Add(probe, new Transform
                {
                    Translation = Vec3.Zero,
                    Rotation = Quat.Identity,
                    Scale = new Vec3(24f, 24f, 24f),
                });
                Render.SetProbeCapture(probe, new ProbeCaptureSettings { Size = 64, Live = live });

                var ball = ecs.Spawn();
                Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 1f));
                Render.SetMaterial(ecs, ball, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.9f, 0.9f, 0.9f, 1f),
                    Metallic = 1f,
                    Roughness = 0.15f,
                }));
                ecs.Add(ball, Transform.Identity);

                void Glow(Vec3 at, Vec3 size, (float, float, float, float) color)
                {
                    var block = ecs.Spawn();
                    Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
                    Render.SetMaterial(ecs, block, Render.CreateMaterial(new MaterialSettings
                    {
                        BaseColor = (0f, 0f, 0f, 1f),
                        Emissive = color,
                    }));
                    ecs.Add(block, Transform.At(at.X, at.Y, at.Z));
                }

                Glow(new Vec3(5f, 0f, 0f), new Vec3(1f, 8f, 8f), (4f, 0f, 0f, 1f));
                Glow(new Vec3(-5f, 0f, 0f), new Vec3(1f, 8f, 8f), (0f, 4f, 0f, 1f));
                Glow(new Vec3(0f, 5f, 0f), new Vec3(8f, 1f, 8f), (0f, 0f, 4f, 1f));
                Glow(new Vec3(0f, 0f, 9f), new Vec3(8f, 8f, 1f), (4f, 4f, 0f, 1f));
            },
        };

        run.Wait(ShaderMaterialTests.Settled).Capture("picture").Go();
        return run.Picture("picture");
    }

    /// <summary>
    /// A white cube in a volume lit green from above, red from the front and blue from behind,
    /// with nothing else lighting the scene.
    /// </summary>
    private static CapturedImage VolumeScene(bool filled)
    {
        ShaderInstance writer = default;
        AssetHandle volume = AssetHandle.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                    FieldOfView = 40f,
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
                Render.SetAmbientLight((0f, 0f, 0f), 0f);

                const uint size = 4;

                volume = Shaders.CreateImage(size, size * 2, ShaderImageFormat.Rgba16Float, depth: size * 3);

                var probe = ecs.Spawn();
                ecs.Add(probe, new Transform
                {
                    Translation = Vec3.Zero,
                    Rotation = Quat.Identity,
                    Scale = new Vec3(6f, 6f, 6f),
                });
                Render.SetIrradianceVolume(probe, volume, intensity: 1000f);

                writer = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Compute = "shaders/write_irradiance.slang",
                }))
                    .Set("size", size)
                    .Set("light", new[]
                    {
                        new Vector4(0f, 0f, 0f, 1f),   // +X
                        new Vector4(0f, 0f, 0f, 1f),   // -X
                        new Vector4(0f, 1f, 0f, 1f),   // +Y, the top
                        new Vector4(0f, 0f, 0f, 1f),   // -Y
                        new Vector4(1f, 0f, 0f, 1f),   // +Z, the front
                        new Vector4(0f, 0f, 1f, 1f),   // -Z, the back
                    })
                    .SetTexture("volume", volume);

                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 1.5f));
                Render.SetMaterial(ecs, cube, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (1f, 1f, 1f, 1f),
                    Roughness = 1f,
                }));
                ecs.Add(cube, Transform.Identity);
            },

            // Every frame, which is how a technique that answers into the volume runs, and which
            // also covers the frames before the pipeline is ready, when a dispatch does nothing.
            EachFrame = _ =>
            {
                if (filled && writer.IsValid) Shaders.Dispatch(writer, 1, 1, 1);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();
        return run.Picture("picture");
    }

    /// <summary>The summed channels of the columns from <paramref name="from"/> up to <paramref name="to"/>.</summary>
    private static long Brightness(CapturedImage picture, uint from, uint to)
    {
        long sum = 0;

        for (var y = 0u; y < picture.Height; y++)
        {
            for (var x = from; x < to; x++)
            {
                var pixel = picture.At(x, y);
                sum += pixel.R + pixel.G + pixel.B;
            }
        }

        return sum;
    }
}

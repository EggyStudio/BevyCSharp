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
    [SkippableFact]
    public void AComputedVolumeLightsEachFaceFromItsOwnDirection()
    {
        Needs.Renderer();

        var picture = VolumeScene(filled: true);

        // The top faces up and the front faces the camera. Were the halves of an axis the other way
        // round, the front would be blue rather than red, and were the axes swapped, the top would
        // be red.
        var top = picture.At(48, 30);
        var front = picture.At(48, 62);

        Assert.True(top.G > 60 && top.G > top.R + 40 && top.G > top.B + 40, $"the top was {top}");
        Assert.True(front.R > 60 && front.R > front.G + 40 && front.R > front.B + 40, $"the front was {front}");
    }

    /// <summary>
    /// A volume made from pixels and reshaped by <see cref="Render.MakeVolume"/> lights the cube as
    /// the computed one does, given to the probe in the system that made it, before the reshape
    /// has been applied, as the reshape's documentation allows.
    /// </summary>
    /// <remarks>
    /// The image was flat until the next frame, so the probe refused it, and the feature test's
    /// light hall stopped building at its irradiance volume.
    /// </remarks>
    [SkippableFact]
    public void AVolumeMadeFromPixelsIsTakenAtOnceAndLightsEachFaceFromItsOwnDirection()
    {
        Needs.Renderer();

        var picture = VolumeScene(filled: true, fromPixels: true);
        var top = picture.At(48, 30);
        var front = picture.At(48, 62);

        Assert.True(top.G > 60 && top.G > top.R + 40 && top.G > top.B + 40, $"the top was {top}");
        Assert.True(front.R > 60 && front.R > front.G + 40 && front.R > front.B + 40, $"the front was {front}");
    }

    // The light the volume gives each point, shown as it is, unlit by anything else.
    private const string ShowsIrradiance = """
        import bcs;

        [shader("fragment")]
        float4 fragment(bcs::VertexOutput mesh) : SV_Target
        {
            return float4(bcs::irradiance(mesh, mesh.world_normal) / 1000.0, 1.0);
        }
        """;

    /// <summary>
    /// A shader shows the light the volume gives each face through <c>bcs::irradiance</c>, green
    /// on top and red in front as the standard material is lit, and the volume's image reads as
    /// the size it was made.
    /// </summary>
    [SkippableFact]
    public void AShaderReadsTheLightTheVolumeGivesEachFace()
    {
        Needs.Shaders();

        var picture = VolumeScene(filled: true, ShowsIrradiance);
        var top = picture.At(48, 30);
        var front = picture.At(48, 62);

        Assert.True(top.G > 60 && top.G > top.R + 40 && top.G > top.B + 40, $"the top was {top}");
        Assert.True(front.R > 60 && front.R > front.G + 40 && front.R > front.B + 40, $"the front was {front}");
        Assert.Equal((4u, 8u, 12u), _volumeSize);
    }

    /// <summary>The volume's image's size, read where the scene made it.</summary>
    private static (uint, uint, uint) _volumeSize;

    /// <summary>The same scene with the volume never written is dark, so the light came from it.</summary>
    [SkippableFact]
    public void AnEmptyVolumeLightsNothing()
    {
        Needs.Renderer();

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
    [SkippableFact]
    public void AFlatImageOrACameraIsRefused()
    {
        Needs.Renderer();

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
    [SkippableFact]
    public void AReflectionProbeLightsOnlyWhatIsInsideIt()
    {
        Needs.Renderer();

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
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACapturedProbeReflectsEachSideWhereItIs(bool live)
    {
        Needs.Renderer();

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
    [SkippableFact]
    public void ACaptureSizeThatIsNotAPowerOfTwoIsRefused()
    {
        Needs.Renderer();

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
    /// with nothing else lighting the scene, or the same cube drawn by a shader of its own.
    /// </summary>
    private static CapturedImage VolumeScene(bool filled, string? shader = null, bool fromPixels = false)
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

                if (fromPixels)
                {
                    volume = PixelVolume((int)size);
                }
                else
                {
                    volume = Shaders.CreateImage(size, size * 2, ShaderImageFormat.Rgba16Float, depth: size * 3);
                    _volumeSize = Render.TryImageSize(volume, out var across, out var down, out var deep) ? (across, down, deep) : default;
                }

                var probe = ecs.Spawn();
                ecs.Add(probe, new Transform
                {
                    Translation = Vec3.Zero,
                    Rotation = Quat.Identity,
                    Scale = new Vec3(6f, 6f, 6f),
                });
                Render.SetIrradianceVolume(probe, volume, intensity: 1000f);

                if (!fromPixels)
                {
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
                }

                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 1.5f));
                Render.SetMaterial(ecs, cube, shader is null
                    ? Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f })
                    : Shaders.CreateMaterial(Shaders.CreateProgram(ShaderStage.Slang(shader))));
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

    /// <summary>
    /// The light the compute shader writes, as pixels of an image made a volume, green for a
    /// surface facing up, red for one facing the front and blue for one facing the back, at every
    /// point of a grid <paramref name="size"/> across.
    /// </summary>
    private static AssetHandle PixelVolume(int size)
    {
        var pixels = new byte[size * size * 2 * size * 3 * 4];
        (int Side, byte R, byte G, byte B)[] lit = [(2, 0, 255, 0), (4, 255, 0, 0), (5, 0, 0, 255)];
        foreach (var (side, r, g, b) in lit)
        {
            for (var point = 0; point < size * size * size; point++)
            {
                // The packing bcs_scene's irradiance_texel writes, the two signs of an axis down
                // each slice and the three axes along the depth.
                var (x, y, z) = (point % size, point / size % size, point / (size * size));
                var row = ((z + (side / 2 * size)) * 2 * size) + y + (side % 2 * size);
                var at = ((row * size) + x) * 4;
                (pixels[at], pixels[at + 1], pixels[at + 2]) = (r, g, b);
            }
        }

        for (var at = 3; at < pixels.Length; at += 4) pixels[at] = 255;

        var image = Render.CreateImage(pixels, (uint)size, (uint)(size * 2 * size * 3), srgb: false);
        Render.MakeVolume(image, size * 3);
        return image;
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

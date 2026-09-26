using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers compute shaders, the buffers they run over, and reading those buffers back.
/// </summary>
/// <remarks>
/// A dispatch is only worth anything for what it leaves in a buffer, so each test either reads the
/// buffer back or draws from it. The numbers are chosen so that running zero times, twice, or over
/// the wrong elements each gives a different answer from running once.
/// </remarks>
[Collection("engine")]
public sealed class ComputeShaderTests
{
    [Fact]
    public void AComputeShaderChangesABufferThatIsReadBack()
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
                // More than one workgroup's worth, and not a multiple of one, so the shader's own
                // bounds check is exercised as well.
                buffer = Shaders.CreateBuffer<float>(Enumerable.Range(0, 100).Select(i => (float)i).ToArray());
                scale = Compute("shaders/scale.slang").SetBuffer("numbers", buffer).Set("factor", 3f);
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

    /// <summary>A material bound to a buffer draws what a compute shader wrote into it.</summary>
    [Fact]
    public void AMaterialDrawsWhatAComputeShaderWrote()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var fill = default(ShaderInstance);

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                // Red to begin with, so a dispatch that never ran leaves the cube red.
                var colors = Shaders.CreateBuffer<float>([1f, 0f, 0f, 1f]);
                fill = Compute("shaders/fill.slang").SetBuffer("colors", colors).Set("color", ShaderMaterialTests.Green);

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(Shaders.CreateProgram("shaders/data.slang")).SetBuffer("colors", colors));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(30)
            .Capture("before")
            .Do("painting it green", _ => Shaders.Dispatch(fill, 1))
            .Wait(10)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Red(run.Picture("before")) > 100, "the cube did not start red");
        Assert.True(PictureRun.Green(run.Picture("after")) > 100, "the dispatch did not turn it green");
    }

    /// <summary>
    /// A C# struct written into a buffer is stepped by the same struct in Slang and read back
    /// field by field.
    /// </summary>
    [Fact]
    public void AComputeShaderStepsCSharpStructs()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var step = default(ShaderInstance);
        var buffer = AssetHandle.None;
        var read = default(BufferRead);
        Particle[]? particles = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                buffer = Shaders.CreateBuffer<Particle>(
                    Enumerable.Range(0, 70)
                        .Select(i => new Particle(i, 0f, 0f, 1f, i, -1f))
                        .ToArray());

                step = Compute("shaders/particles_step.slang").SetBuffer("particles", buffer);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("stepping twice", _ =>
            {
                // Two dispatches in one frame, which run in order, each over what the last wrote,
                // and each with the value the instance held when it was asked for.
                Shaders.Dispatch(step.Set("step", 0.25f), 2);
                Shaders.Dispatch(step.Set("step", 0.75f), 2);
            })
            .Wait(2)
            .Do("asking for it back", _ => read = Shaders.BeginBufferRead(buffer))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out particles))
            .Go();

        Assert.NotNull(particles);
        Assert.Equal(70, particles.Length);

        for (var i = 0; i < 70; i++)
        {
            Assert.Equal(new Particle(i + 1f, i, -1f, 1f, i, -1f), particles[i]);
        }
    }

    /// <summary>
    /// Quads a mesh built from vertices draws where a buffer puts them, and move when a compute
    /// shader moves the buffer.
    /// </summary>
    /// <remarks>
    /// Two quads, left and right of the middle, lifted by a dispatch. What is checked is where the
    /// green is: at the sides before, above them after, and never in the middle, which is where
    /// both quads would be if the buffer were not read at all.
    /// </remarks>
    [Fact]
    public void QuadsFollowTheBufferThatPlacesThem()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var shift = default(ShaderInstance);

        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Projection = CameraProjection.Orthographic,
                    Height = 8f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));

                var centers = Shaders.CreateBuffer<float>([-2f, 0f, 0f, 0f, 2f, 0f, 0f, 0f]);
                shift = Compute("shaders/shift.slang")
                    .SetBuffer("centers", centers)
                    .Set("by", new System.Numerics.Vector3(0f, 2f, 0f));

                // Each quad's corners, as offsets from its center.
                var corners = new List<Vec3>();
                var indices = new List<uint>();

                for (uint quad = 0; quad < 2; quad++)
                {
                    corners.AddRange([new(-0.5f, -0.5f, 0f), new(0.5f, -0.5f, 0f), new(0.5f, 0.5f, 0f), new(-0.5f, 0.5f, 0f)]);
                    indices.AddRange([quad * 4, (quad * 4) + 1, (quad * 4) + 2, quad * 4, (quad * 4) + 2, (quad * 4) + 3]);
                }

                var quads = ecs.Spawn();
                Render.SetMesh(ecs, quads, Render.CreateMesh(new MeshData
                {
                    Positions = [.. corners],
                    Indices = [.. indices],
                }));

                var material = Shaders.CreateMaterial(new ShaderMaterialSettings
                {
                    Program = Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Vertex = "shaders/quads.slang",
                        Fragment = "shaders/quads.slang",
                    }),
                    Cull = CullMode.None,
                });

                Render.SetMaterial(ecs, quads, material.SetBuffer("centers", centers));

                ecs.Add(quads, Transform.Identity);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(30)
            .Capture("before")
            .Do("lifting them", _ => Shaders.Dispatch(shift, 1))
            .Wait(10)
            .Capture("after")
            .Go();

        // Eight units across a hundred and twenty-eight pixels is sixteen to a unit, so the quads
        // stand thirty-two pixels either side of the middle, and a lift of two is thirty-two up.
        static bool GreenAt(CapturedImage picture, uint x, uint y) =>
            picture.At(x, y) is var pixel && pixel.G > 120 && pixel.R < 90;

        var before = run.Picture("before");
        var after = run.Picture("after");

        Assert.True(GreenAt(before, 32, 64) && GreenAt(before, 96, 64), "the quads were not at the sides");
        Assert.False(GreenAt(before, 64, 64), "a quad was drawn in the middle, where no center puts one");
        Assert.True(GreenAt(after, 32, 32) && GreenAt(after, 96, 32), "the quads did not move up");
        Assert.False(GreenAt(after, 32, 64), "a quad stayed where it was");
    }

    /// <summary>
    /// A material samples what a compute shader wrote into an image, and a float image beside it
    /// holds numbers no eight-bit image could.
    /// </summary>
    [Fact]
    public void AComputeShaderWritesImagesOfAnyFormat()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var paint = default(ShaderInstance);
        var copy = default(ShaderInstance);
        var into = AssetHandle.None;
        var read = default(BufferRead);
        System.Numerics.Vector4[]? copied = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                var image = Shaders.CreateImage(16, 16);
                var precise = Shaders.CreateImage(16, 16, ShaderImageFormat.Rgba32Float);
                into = Shaders.CreateBuffer(16);

                paint = Compute("shaders/paint_image.slang")
                    .Set("color", ShaderMaterialTests.Green)
                    .SetTexture("image", image)
                    .SetTexture("precise", precise);

                copy = Compute("shaders/copy_image.slang")
                    .SetTexture("source", precise)
                    .SetBuffer("into", into);

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(Shaders.CreateProgram("shaders/sampled.slang")).SetTexture("picture", image));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("painting", _ =>
            {
                Shaders.Dispatch(paint, 2, 2);
                Shaders.Dispatch(copy, 1);
            })
            .Wait(10)
            .Capture("picture")
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the cube did not show the painted image");

        Assert.NotNull(copied);
        Assert.Equal(new System.Numerics.Vector4(1000.5f, -2.25f, 3f, 1f), copied[0]);
    }

    /// <summary>A compute shader reads Bevy's time through the prelude.</summary>
    [Fact]
    public void AComputeShaderReadsTheTime()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var clock = default(ShaderInstance);
        var into = AssetHandle.None;
        var read = default(BufferRead);
        float[]? numbers = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                into = Shaders.CreateBuffer(12);
                clock = Compute("shaders/clock_compute.slang").SetBuffer("into", into);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(10)
            .Do("reading the clock", _ => Shaders.Dispatch(clock, 1))
            .Wait(2)
            .Do("asking for it back", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out numbers))
            .Go();

        Assert.NotNull(numbers);
        Assert.True(numbers[0] > 0f, $"the time read {numbers[0]}");
        Assert.True(numbers[1] > 0f, $"the frame's length read {numbers[1]}");
        Assert.True(numbers[2] > 10f, $"the frame count read {numbers[2]}");
    }

    /// <summary>
    /// An image's mip levels are bound one at a time, so one dispatch reads a level while the next
    /// writes the level below, and the whole image is read afterwards.
    /// </summary>
    [Fact]
    public void AnImagePyramidIsBuiltALevelAtATime()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var fill = default(ShaderInstance);
        var halve = default(ShaderInstance);
        var readLevels = default(ShaderInstance);
        var into = AssetHandle.None;
        var read = default(BufferRead);
        float[]? levels = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                var pyramid = Shaders.CreateImage(16, 16, ShaderImageFormat.R32Float, mips: 3);
                into = Shaders.CreateBuffer(16);

                fill = Compute("shaders/fill_image_level.slang").Set("value", 0.75f).SetTexture("level", pyramid, mip: 0);
                halve = Compute("shaders/halve_level.slang")
                    .SetTexture("above", pyramid, mip: 0)
                    .SetTexture("below", pyramid, mip: 1);
                readLevels = Compute("shaders/read_level.slang").SetTexture("pyramid", pyramid).SetBuffer("into", into);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("building", _ =>
            {
                Shaders.Dispatch(fill, 2, 2);
                Shaders.Dispatch(halve, 1, 1);
                Shaders.Dispatch(readLevels, 1);
            })
            .Wait(2)
            .Do("asking for the levels", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out levels))
            .Go();

        Assert.NotNull(levels);
        Assert.Equal(0.75f, levels[0]);
        Assert.Equal(1.5f, levels[1]);
    }

    /// <summary>A buffer grows keeping what it held, and the rest is zeros.</summary>
    [Fact]
    public void ABufferGrowsKeepingItsContents()
    {
        if (!App.HasRenderer) return;

        var buffer = AssetHandle.None;
        var size = 0;
        var read = default(BufferRead);
        float[]? numbers = null;

        new PictureRun
        {
            Scene = _ =>
            {
                buffer = Shaders.CreateBuffer<float>([1f, 2f, 3f, 4f]);
            },
        }
            .Wait(3)
            .Do("growing it", _ => size = Shaders.GrowBuffer(buffer, 64))
            .Wait(3)
            .Do("asking for it back", _ => read = Shaders.BeginBufferRead(buffer))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out numbers))
            .Go();

        Assert.Equal(64, size);
        Assert.NotNull(numbers);
        Assert.Equal(16, numbers.Length);
        Assert.Equal([1f, 2f, 3f, 4f], numbers[..4]);
        Assert.All(numbers[4..], number => Assert.Equal(0f, number));
    }

    /// <summary>
    /// An instance buffer holds an entity's transform this frame and on the previous one, which a
    /// compute shader reads through <c>bcs_scene</c>.
    /// </summary>
    [Fact]
    public void AnInstanceBufferHoldsThisFrameAndTheLast()
    {
        if (!ShaderMaterialTests.CanRun) return;

        var mover = Entity.None;
        var x = 0f;
        var read = default(BufferRead);
        var copy = default(ShaderInstance);
        var into = AssetHandle.None;
        Vector4[]? copied = null;
        var frozenAt = 0f;
        var moving = true;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                mover = ecs.Spawn();
                ecs.Add(mover, Transform.At(0f, 0f, 0f));

                var instances = Shaders.CreateInstanceBuffer(4);
                Shaders.SetInstance(instances, 0, mover);

                into = Shaders.CreateBuffer(32);
                copy = Compute("shaders/read_instance.slang")
                    .SetBuffer("instances", instances)
                    .SetBuffer("into", into);
            },

            // A tenth of a unit a frame along x, until the copy has been asked for.
            EachFrame = world =>
            {
                if (mover == Entity.None || !moving) return;

                x += 0.1f;
                world.Resource<EcsWorld>().Set(mover, Transform.At(x, 0f, 0f));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(10)
            .Do("copying the slot", _ =>
            {
                moving = false;
                frozenAt = x;
                Shaders.Dispatch(copy, 1);
            })
            .Wait(2)
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);

        // The copy ran on the frame the mover was last moved, so this frame is where it was left
        // and the previous one a tenth short of it.
        Assert.Equal(frozenAt, copied[0].X, 3);
        Assert.Equal(frozenAt - 0.1f, copied[1].X, 3);
    }

    /// <summary>
    /// A material buffer holds what each entity in it is made of, read from its standard material,
    /// and a shader reads it through <c>bcs_scene::Material</c>.
    /// </summary>
    [Fact]
    public void AMaterialBufferHoldsWhatEachEntityIsMadeOf()
    {
        if (!ShaderMaterialTests.CanRun) return;

        AssetHandle into = default;
        ShaderInstance copy = default;
        BufferRead read = default;
        Vector4[]? copied = null;

        new PictureRun
        {
            Scene = ecs =>
            {
                var red = ecs.Spawn();
                Render.SetMesh(ecs, red, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
                Render.SetMaterial(ecs, red, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (1f, 0f, 0f, 1f),
                    Roughness = 0.25f,
                    Metallic = 0f,
                }));

                var shiny = ecs.Spawn();
                Render.SetMesh(ecs, shiny, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
                Render.SetMaterial(ecs, shiny, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0f, 0f, 1f, 1f),
                    Emissive = (0f, 2f, 0f, 1f),
                    Roughness = 0.75f,
                    Metallic = 1f,
                }));

                var materials = Shaders.CreateMaterialBuffer(2);
                Shaders.SetInstance(materials, 0, red);
                Shaders.SetInstance(materials, 1, shiny);

                into = Shaders.CreateBuffer(6 * 16);
                copy = Compute("shaders/read_materials.slang").SetBuffer("materials", materials).SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(5)
            .Do("copying the slots", _ => Shaders.Dispatch(copy, 1))
            .Wait(2)
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);

        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), copied[0]);
        Assert.Equal(0.25f, copied[2].X, 3);
        Assert.Equal(0f, copied[2].Y, 3);

        Assert.Equal(new Vector4(0f, 0f, 1f, 1f), copied[3]);
        Assert.Equal(2f, copied[4].Y, 3);
        Assert.Equal(0.75f, copied[5].X, 3);
        Assert.Equal(1f, copied[5].Y, 3);
        Assert.Equal(0f, copied[5].W, 3);
    }

    /// <summary>
    /// Writing a region of an image changes those texels and no others, at the level asked for,
    /// and a region outside the image is refused.
    /// </summary>
    [Fact]
    public void ARegionOfAnImageIsWritten()
    {
        if (!ShaderMaterialTests.CanRun) return;

        AssetHandle into = default;
        ShaderInstance copy = default;
        BufferRead read = default;
        float[]? copied = null;
        Exception? outside = null;

        new PictureRun
        {
            Scene = _ =>
            {
                var image = Shaders.CreateImage(8, 8, ShaderImageFormat.R32Float, mips: 2);

                // Two by two at three, four, and one texel of the second level.
                Shaders.WriteImage<float>(image, [1f, 2f, 3f, 4f], x: 3, y: 4, width: 2, height: 2);
                Shaders.WriteImage<float>(image, [9f], x: 0, y: 0, width: 1, height: 1, mip: 1);

                outside = Record.Exception(() => Shaders.WriteImage<float>(image, [1f, 2f], x: 7, y: 0, width: 2, height: 1));

                into = Shaders.CreateBuffer(65 * 4);
                copy = Compute("shaders/read_image.slang").SetTexture("image", image).SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("copying the image", _ => Shaders.Dispatch(copy, 1))
            .Wait(2)
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);
        Assert.IsType<BevyNativeException>(outside);

        Assert.Equal(1f, copied[4 * 8 + 3]);
        Assert.Equal(2f, copied[4 * 8 + 4]);
        Assert.Equal(3f, copied[5 * 8 + 3]);
        Assert.Equal(4f, copied[5 * 8 + 4]);
        Assert.Equal(9f, copied[64]);

        // Everything else is as it was made.
        Assert.Equal(60, copied.Take(64).Count(value => value == 0f));
    }

    /// <summary>
    /// A block-compressed image made empty takes one four by four block written into it, which the
    /// GPU decodes where the block is and nowhere else, and a region off the block grid is refused.
    /// </summary>
    [Fact]
    public void ABlockIsWrittenIntoACompressedImage()
    {
        if (!ShaderMaterialTests.CanRun) return;

        AssetHandle into = default;
        ShaderInstance copy = default;
        BufferRead read = default;
        float[]? copied = null;
        Exception? offGrid = null;

        new PictureRun
        {
            Scene = _ =>
            {
                var image = Shaders.CreateImage(8, 8, ShaderImageFormat.Bc4);

                // A BC4 block whose two end values are both 200 and whose sixteen indices all pick
                // the first, so every texel of it decodes to 200 out of 255.
                byte[] block = [200, 200, 0, 0, 0, 0, 0, 0];
                Shaders.WriteImage<byte>(image, block, x: 4, y: 4, width: 4, height: 4);

                offGrid = Record.Exception(() => Shaders.WriteImage<byte>(image, block, x: 2, y: 0, width: 4, height: 4));

                into = Shaders.CreateBuffer(64 * 4);
                copy = Compute("shaders/read_block.slang").SetTexture("image", image).SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("copying the image", _ => Shaders.Dispatch(copy, 1))
            .Wait(2)
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);
        Assert.IsType<BevyNativeException>(offGrid);

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var expected = x >= 4 && y >= 4 ? 200f / 255f : 0f;
                Assert.Equal(expected, copied[y * 8 + x], 2);
            }
        }
    }

    /// <summary>
    /// Slang's wave operations reach the GPU as subgroup operations, so a sum over a subgroup is
    /// its size, and one thread in each is its first.
    /// </summary>
    [Fact]
    public void WaveOperationsRunAsSubgroups()
    {
        if (!ShaderMaterialTests.CanRun) return;

        AssetHandle into = default;
        ShaderInstance wave = default;
        BufferRead read = default;
        uint[]? copied = null;

        new PictureRun
        {
            Scene = _ =>
            {
                into = Shaders.CreateBuffer(64 * 4);
                wave = Compute("shaders/wave_sum.slang").SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("summing", _ => Shaders.Dispatch(wave, 1))
            .Wait(2)
            .Do("asking for the sums", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);

        var lanes = copied[0] % 100000 / 1000;
        Assert.True(lanes is > 0 and <= 64, $"a subgroup had {lanes} threads");

        foreach (var value in copied)
        {
            var rest = value % 100000;
            Assert.Equal(lanes, rest / 1000);
            Assert.Equal(lanes, rest % 1000);
        }

        Assert.Equal(64 / (int)lanes, copied.Count(value => value >= 100000));
    }

    /// <summary>
    /// A prefix sum, a read from another lane and a ballot over a subgroup give what they promise.
    /// Each thread's count of those before it is its index, the first lane's value reaches every
    /// lane, and a unanimous vote sets a bit for every lane.
    /// </summary>
    [Fact]
    public void WavePrefixSumsReadsAndBallotsWork()
    {
        if (!ShaderMaterialTests.CanRun) return;

        AssetHandle into = default;
        ShaderInstance wave = default;
        BufferRead read = default;
        uint[]? copied = null;

        new PictureRun
        {
            Scene = _ =>
            {
                into = Shaders.CreateBuffer(64 * 16);
                wave = Compute("shaders/wave_prefix.slang").SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("running", _ => Shaders.Dispatch(wave, 1))
            .Wait(2)
            .Do("asking for the results", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);

        // Every lane votes, so the low word has a bit for each of the first thirty two lanes.
        var lanes = Enumerable.Range(0, 64).Max(thread => copied[thread * 4 + 1]) + 1;
        var everyone = lanes >= 32 ? uint.MaxValue : (1u << (int)lanes) - 1;

        for (var thread = 0; thread < 64; thread++)
        {
            var before = copied[thread * 4];
            var lane = copied[thread * 4 + 1];
            var first = copied[thread * 4 + 2];
            var votes = copied[thread * 4 + 3];

            Assert.Equal(lane, before);
            Assert.Equal((uint)thread - lane, first);
            Assert.Equal(everyone, votes);
        }
    }

    /// <summary>
    /// A ray traced through every triangle of a cube in a geometry pool hits its front face where
    /// it is, facing the ray, and the cube's box and the second mesh's counts are in the table.
    /// </summary>
    [Fact]
    public void ARayIsTracedThroughAGeometryPool()
    {
        if (!ShaderMaterialTests.CanRun) return;

        AssetHandle into = default;
        ShaderInstance trace = default;
        BufferRead read = default;
        Vector4[]? copied = null;
        var first = -1;
        var second = -1;

        new PictureRun
        {
            Scene = _ =>
            {
                var pool = Shaders.CreateGeometryPool();
                first = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));
                second = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Sphere, 1f));

                into = Shaders.CreateBuffer(3 * 16);
                trace = Compute("shaders/trace_pool.slang")
                    .SetBuffer("vertices", pool.Vertices)
                    .SetBuffer("indices", pool.Indices)
                    .SetBuffer("meshes", pool.Meshes)
                    .SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("tracing", _ => Shaders.Dispatch(trace, 1))
            .Wait(2)
            .Do("asking for the hit", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.NotNull(copied);

        // The front face is at z = 1, four units from the ray's start, and faces back along it.
        Assert.Equal(4f, copied[0].X, 3);
        Assert.Equal(1f, copied[0].W, 3);
        Assert.Equal(4f, copied[1].X, 3);

        // A cube is twelve triangles, and the sphere's vertices start after the cube's.
        Assert.Equal(12f, copied[2].X);
        Assert.True(copied[2].Y > 12f, $"the sphere had {copied[2].Y} triangles");
        Assert.True(copied[2].Z >= 8f, $"the sphere's vertices started at {copied[2].Z}");
    }

    /// <summary>A buffer's size is fixed, so writing more than it holds is refused.</summary>
    [Fact]
    public void ABufferDoesNotGrow()
    {
        if (!App.HasRenderer) return;

        var size = 0;
        Exception? refused = null;

        new PictureRun
        {
            Scene = _ =>
            {
                var buffer = Shaders.CreateBuffer(10);
                size = Shaders.BufferSize(buffer);

                Shaders.WriteBuffer<byte>(buffer, new byte[size]);
                refused = Record.Exception(() => Shaders.WriteBuffer<byte>(buffer, new byte[size + 1]));
            },
        }.Wait(2).Go();

        Assert.Equal(16, size);
        Assert.IsType<ArgumentException>(refused);
    }

    /// <summary>A dispatch needs an instance.</summary>
    [Fact]
    public void ADispatchWithNoInstanceIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Shaders.Dispatch(default, 1));
    }

    /// <summary>An instance of the compute shader in <paramref name="file"/>.</summary>
    private static ShaderInstance Compute(string file) =>
        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = file }));

    /// <summary>What the Slang shader calls a particle, as C# lays it out.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Particle(float X, float Y, float Z, float Vx, float Vy, float Vz);
}

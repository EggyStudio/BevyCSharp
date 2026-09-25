using System.Runtime.InteropServices;
using Bevy;
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
        if (!App.HasRenderer) return;

        var program = ShaderProgram.None;
        var buffer = AssetHandle.None;
        var read = default(BufferRead);
        float[]? numbers = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                program = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/scale.wgsl" });

                // More than one workgroup's worth, and not a multiple of one, so the shader's own
                // bounds check is exercised as well.
                buffer = Shaders.CreateBuffer<float>(Enumerable.Range(0, 100).Select(i => (float)i).ToArray());
            },
        };

        run.Until("compiled", _ => program.State == ShaderProgramState.Ready)
            .Do("tripling", _ => Shaders.Dispatch(new DispatchSettings
            {
                Program = program,
                X = 2,
                Parameters = [3f],
                Buffers = { [0] = buffer },
            }))
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
        if (!App.HasRenderer) return;

        var fill = ShaderProgram.None;
        var colors = AssetHandle.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                fill = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/fill.wgsl" });

                // Red to begin with, so a dispatch that never ran leaves the cube red.
                colors = Shaders.CreateBuffer<float>([1f, 0f, 0f, 1f]);

                PictureRun.Cube(ecs, Shaders.CreateMaterial(new ShaderMaterialSettings
                {
                    Program = Shaders.CreateProgram("shaders/data.wgsl"),
                    Buffer = colors,
                }));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(30)
            .Capture("before")
            .Do("painting it green", _ => Shaders.Dispatch(new DispatchSettings
            {
                Program = fill,
                Parameters = [0f, 1f, 0f, 1f],
                Buffers = { [0] = colors },
            }))
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
    public void ASlangComputeShaderStepsCSharpStructs()
    {
        if (!App.HasRenderer || !Shaders.SlangAvailable) return;

        var program = ShaderProgram.None;
        var buffer = AssetHandle.None;
        var read = default(BufferRead);
        Particle[]? particles = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                program = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/particles_step.slang" });

                buffer = Shaders.CreateBuffer<Particle>(
                    Enumerable.Range(0, 70)
                        .Select(i => new Particle(i, 0f, 0f, 1f, i, -1f))
                        .ToArray());
            },
        };

        run.Until("compiled", _ => program.State == ShaderProgramState.Ready)
            .Do("stepping twice", _ =>
            {
                // Two dispatches in one frame, which run in order, each over what the last wrote.
                Shaders.Dispatch(new DispatchSettings { Program = program, X = 2, Parameters = [0.5f], Buffers = { [0] = buffer } });
                Shaders.Dispatch(new DispatchSettings { Program = program, X = 2, Parameters = [0.5f], Buffers = { [0] = buffer } });
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
        if (!App.HasRenderer) return;

        var shift = ShaderProgram.None;
        var centers = AssetHandle.None;

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

                shift = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/shift.wgsl" });
                centers = Shaders.CreateBuffer<float>([-2f, 0f, 0f, 0f, 2f, 0f, 0f, 0f]);

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

                Render.SetMaterial(ecs, quads, Shaders.CreateMaterial(new ShaderMaterialSettings
                {
                    Program = Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Vertex = "shaders/quads.wgsl",
                        Fragment = "shaders/quads.wgsl",
                    }),
                    Buffer = centers,
                    Cull = CullMode.None,
                }));

                ecs.Add(quads, Transform.Identity);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(30)
            .Capture("before")
            .Do("lifting them", _ => Shaders.Dispatch(new DispatchSettings
            {
                Program = shift,
                Parameters = [0f, 2f, 0f],
                Buffers = { [0] = centers },
            }))
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

    /// <summary>A material samples what a compute shader wrote into an image.</summary>
    [Fact]
    public void AMaterialSamplesAnImageAComputeShaderWrote()
    {
        if (!App.HasRenderer) return;

        var paint = ShaderProgram.None;
        var image = AssetHandle.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                paint = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/paint_image.wgsl" });
                image = Shaders.CreateImage(16, 16);

                var settings = new ShaderMaterialSettings { Program = Shaders.CreateProgram("shaders/sampled.wgsl") };
                settings.Textures[0] = image;
                PictureRun.Cube(ecs, Shaders.CreateMaterial(settings));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("painting it green", _ => Shaders.Dispatch(new DispatchSettings
            {
                Program = paint,
                X = 2,
                Y = 2,
                Parameters = [0f, 1f, 0f, 1f],
                Images = { [0] = image },
            }))
            .Wait(10)
            .Capture("picture")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the cube did not show the painted image");
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

    /// <summary>A dispatch needs a program.</summary>
    [Fact]
    public void ADispatchWithNoProgramIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Shaders.Dispatch(new DispatchSettings()));
    }

    /// <summary>What the Slang shader calls a particle, as C# lays it out.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Particle(float X, float Y, float Z, float Vx, float Vy, float Vz);
}

using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers materials drawn by shaders the game wrote in WGSL.
/// </summary>
/// <remarks>
/// Whether the numbers reached the shader is not a question anything but a picture can answer. A
/// material that never compiled draws nothing, one whose bindings are laid out differently from
/// what the shader declares draws the wrong color, and both look the same from the managed side.
/// </remarks>
[Collection("engine")]
public sealed class ShaderMaterialTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const uint Settled = 120;

    [Fact]
    public void AShaderMaterialDrawsTheColorItWasGiven()
    {
        if (!App.HasRenderer) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                // Green, in the first four floats, which is what the shader paints with. Green
                // rather than the material's own, so nothing else could have drawn it.
                var program = Shaders.CreateProgram("shaders/flat.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program, [0f, 1f, 0f, 1f]));
            },
        };

        run.Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);

        Assert.True(
            middle.G > 120 && middle.R < 90 && middle.B < 90,
            $"the cube came out {middle} rather than green");
    }

    /// <summary>A vertex shader moves the mesh, which shows as the shape covering more of it.</summary>
    /// <remarks>
    /// The same cube drawn twice by the same program, pushed along its own normals by the first
    /// number the material carries. A vertex shader that never ran would leave the two pictures the
    /// same size, which is the only thing that tells it apart from a fragment shader.
    /// </remarks>
    [Fact]
    public void AVertexShaderMovesTheMesh()
    {
        if (!App.HasRenderer) return;

        var still = Swell("shaders/ripple.wgsl", 0f);
        var swollen = Swell("shaders/ripple.wgsl", 0.6f);

        Assert.True(still > 0, "the cube was not drawn at all");
        Assert.True(
            swollen > still + 200,
            $"the shape covered {still} pixels still and {swollen} pushed out");
    }

    /// <summary>
    /// How many pixels a cube swollen by <paramref name="swell"/> covers, drawn by a file whose
    /// vertex shader reads the first float and whose fragment shader paints the second row.
    /// </summary>
    internal static int Swell(string file, float swell)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = file,
                    Fragment = file,
                });

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(program, [swell, 0f, 0f, 0f, 0f, 1f, 0f, 1f]));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();
        return PictureRun.Green(run.Picture("picture"));
    }

    /// <summary>Whether every program the app made has finished compiling or loading.</summary>
    internal static bool ProgramsReady() =>
        Shaders.Programs.All(program => program.State == ShaderProgramState.Ready);

    /// <summary>
    /// Six programs drawn at once, each its own, with no limit to run into.
    /// </summary>
    /// <remarks>
    /// One file compiled six ways by its defines, which also covers a define reaching WGSL through
    /// naga_oil's substitution. Each cube is a different primary or secondary color, and each has
    /// to come out that color where it stands.
    /// </remarks>
    [Fact]
    public void ManyProgramsDrawAtOnce()
    {
        if (!App.HasRenderer) return;

        (int R, int G, int B)[] colors = [(1, 0, 0), (0, 1, 0), (0, 0, 1), (1, 1, 0), (0, 1, 1), (1, 0, 1)];
        var programs = new List<ShaderProgram>();

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Projection = CameraProjection.Orthographic,
                    Height = 2f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));

                for (var i = 0; i < colors.Length; i++)
                {
                    var program = Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Fragment = "shaders/defines.wgsl",
                        Defines =
                        {
                            ["RED"] = colors[i].R,
                            ["GREEN"] = colors[i].G,
                            ["BLUE"] = colors[i].B,
                        },
                    });

                    programs.Add(program);

                    // Twelve units across at an orthographic height of two, so each cube sits in
                    // the middle of its own sixth of the picture.
                    var x = -5f + (2f * i);
                    PictureRun.Cube(ecs, Shaders.CreateMaterial(program), 1f, new Vec3(x, 0f, 0f));
                }
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        Assert.Equal(colors.Length, programs.Distinct().Count());

        var picture = run.Picture("picture");

        for (var i = 0; i < colors.Length; i++)
        {
            var pixel = picture.At((uint)(32 + (64 * i)), 32);
            var (r, g, b) = colors[i];

            Assert.True(
                (pixel.R > 120) == (r == 1) && (pixel.G > 120) == (g == 1) && (pixel.B > 120) == (b == 1),
                $"cube {i} came out {pixel} rather than {colors[i]}");
        }
    }

    /// <summary>The same settings answer the same program, however often they are asked for.</summary>
    [Fact]
    public void TheSameSettingsAreTheSameProgram()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            // A headless run has no renderer to compile for, so this is refused rather than
            // answered, which is itself worth pinning down.
            var refused = Assert.Throws<BevyNativeException>(
                () => Shaders.CreateProgram("shaders/flat.wgsl"));

            Assert.Equal(NativeStatus.Unsupported, refused.Status);
        });

        harness.Run();

        var first = ShaderProgram.None;
        var second = ShaderProgram.None;
        var other = ShaderProgram.None;

        new PictureRun
        {
            Scene = _ =>
            {
                first = Shaders.CreateProgram("shaders/flat.wgsl");
                second = Shaders.CreateProgram("shaders/flat.wgsl");
                other = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Fragment = "shaders/flat.wgsl",
                    Defines = { ["UNUSED"] = true },
                });
            },
        }.Wait(2).Go();

        Assert.True(first.IsValid);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    /// <summary>A material needs a program, and a program needs a fragment shader.</summary>
    [Fact]
    public void WhatCannotDrawIsRefusedBeforeReachingTheEngine()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.CreateMaterial(new ShaderMaterialSettings()));

        Assert.Throws<ArgumentException>(
            () => Shaders.CreateProgram(new ShaderProgramSettings { Vertex = "shaders/ripple.wgsl" }));

        Assert.Throws<ArgumentException>(
            () => Shaders.CreateProgram("shaders/flat.glsl"));
    }

    /// <summary>More numbers than a material carries is a mistake rather than a truncation.</summary>
    [Fact]
    public void TooManyNumbersAreRefused()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.CreateMaterial(new ShaderMaterialSettings
            {
                Program = default,
                Parameters = new float[Shaders.ParameterCount + 1],
            }));

        Assert.Throws<ArgumentException>(
            () => Shaders.SetParameters(AssetHandle.None, new float[2], Shaders.ParameterCount - 1));
    }

    /// <summary>
    /// The storage buffer takes as many bytes as it is given, and the shader reads the last of
    /// them.
    /// </summary>
    [Fact]
    public void TheDataBufferHoldsAsMuchAsItIsGiven()
    {
        if (!App.HasRenderer) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                // A thousand colors, all red but the last, which is sixteen kilobytes and far past
                // anything a uniform holds.
                var colors = new Vec4F[1000];
                Array.Fill(colors, new Vec4F(1f, 0f, 0f, 1f));
                colors[^1] = new Vec4F(0f, 1f, 0f, 1f);

                var program = Shaders.CreateProgram("shaders/data.wgsl");
                var material = Shaders.CreateMaterial(new ShaderMaterialSettings
                {
                    Program = program,
                    Data = MemoryMarshal.AsBytes(colors.AsSpan()).ToArray(),
                });

                PictureRun.Cube(ecs, material);
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle.G > 120 && middle.R < 90, $"the cube came out {middle} rather than green");
    }

    /// <summary>
    /// A change to a material's numbers reaches the picture while it is being drawn.
    /// </summary>
    [Fact]
    public void ParametersChangeWhileTheMaterialIsDrawn()
    {
        if (!App.HasRenderer) return;

        var material = AssetHandle.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/flat.wgsl"), [0f, 1f, 0f, 1f]);
                PictureRun.Cube(ecs, material);
            },
        };

        run.Wait(Settled)
            .Capture("before")
            .Do("turning it red", _ =>
            {
                Shaders.SetParameters(material, [1f, 0f], 0);
                Assert.Equal(1f, Shaders.GetParameters(material)[0]);
            })
            .Wait(10)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("before")) > 100, "the cube did not start green");
        Assert.True(PictureRun.Red(run.Picture("after")) > 100, "the cube did not turn red");
    }

    /// <summary>
    /// The last 2D texture slot and the first array texture are bound where the layout says.
    /// </summary>
    /// <remarks>
    /// Covers the reshaping as well, because an array texture is a tall picture told how many
    /// layers it has, and the shader reads the second layer, which is green while the first is red.
    /// </remarks>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TexturesPastTheFirstAreBound(bool array)
    {
        if (!App.HasRenderer) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                byte[] green = [0, 255, 0, 255];
                byte[] redOverGreen = [255, 0, 0, 255, 0, 255, 0, 255];

                var settings = new ShaderMaterialSettings
                {
                    Program = Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Fragment = "shaders/textures.wgsl",
                        Defines = { ["ARRAY"] = array },
                    }),
                };

                if (array)
                {
                    var layered = Render.CreateImage(redOverGreen, 1, 2);
                    Render.MakeTextureArray(layered, 2);
                    settings.TextureArrays[0] = layered;
                }
                else
                {
                    settings.Textures[Shaders.TextureCount - 1] = Render.CreateImage(green, 1, 1);
                }

                PictureRun.Cube(ecs, Shaders.CreateMaterial(settings));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle.G > 120 && middle.R < 90, $"the cube came out {middle} rather than green");
    }

    /// <summary>
    /// A material that moves its vertices in the prepass as well casts the shadow of the shape it
    /// drew rather than of the mesh it started as.
    /// </summary>
    /// <remarks>
    /// A ball held above a floor under a light shining straight down, drawn swollen, twice: once by
    /// a program whose prepass moves the vertices the same way, and once by one that leaves the
    /// prepass to Bevy. What is counted is the dark floor, which is the shadow.
    /// </remarks>
    [Fact]
    public void APrepassVertexShaderMovesTheShadowToo()
    {
        if (!App.HasRenderer) return;

        var followed = Shadow("shaders/swell.wgsl", prepass: true);
        var left = Shadow("shaders/swell.wgsl", prepass: false);

        Assert.True(left > 30, $"the plain ball cast a shadow of only {left} pixels");
        Assert.True(
            followed > left + (left / 2),
            $"the shadow covered {left} pixels from the plain mesh and {followed} from the swollen one");
    }

    /// <summary>How many floor pixels are in shadow under a swollen ball.</summary>
    internal static int Shadow(string file, bool prepass)
    {
        var run = new PictureRun
        {
            Width = 128,
            Height = 128,
            Scene = ecs =>
            {
                // From above and to the side, so the ball and its shadow are in different parts of
                // the picture.
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 9f, 9f), new Vec3(0f, 0f, 1.5f), Vec3.UnitY));

                var sun = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Directional,
                    Intensity = 8000f,
                    Shadows = true,
                });

                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, 0f), Vec3.UnitZ));

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.1f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (1f, 1f, 1f, 1f),
                }));
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = file,
                    Fragment = file,
                    PrepassVertex = prepass ? new ShaderStage(file, "prepass_vertex") : default,
                });

                // A sphere rather than a cube, because a cube pushed along its face normals from
                // straight above still casts a square of the same size, since its sides move out
                // edge-on to the light. A sphere pushed out is a larger sphere from every direction, and
                // swelling it by half its radius again more than doubles the shadow's area.
                var ball = ecs.Spawn();
                Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 0.5f));
                Render.SetMaterial(
                    ecs,
                    ball,
                    Shaders.CreateMaterial(program, [0.25f, 0f, 0f, 0f, 0f, 1f, 0f, 1f]));
                ecs.Add(ball, Transform.At(0f, 3f, 0f));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        // The floor is lit near white. What is dark and grey, rather than black background or the
        // green ball, is shadow.
        return PictureRun.Count(
            run.Picture("picture"),
            (r, g, b) => r < 90 && g < 90 && b < 90 && r > 3 && Math.Abs(r - g) < 20);
    }

    /// <summary>
    /// A shader that disagrees with its pipeline is survived and reported when the app asks for
    /// that, rather than closing it.
    /// </summary>
    /// <remarks>
    /// What the editor turns on, so a shader edited into a shape the pipeline rejects is something
    /// to read about and fix. The app is run on past the error, and ending cleanly afterwards is
    /// what says it was survived.
    /// </remarks>
    [Fact]
    public void AMismatchedShaderIsSurvivedWhenAsked()
    {
        if (!App.HasRenderer) return;

        var reported = string.Empty;
        Shaders.KeepRenderingAfterErrors = true;

        try
        {
            new PictureRun
            {
                Scene = ecs =>
                {
                    PictureRun.Camera(ecs);
                    PictureRun.Cube(ecs, Shaders.CreateMaterial(Shaders.CreateProgram("shaders/mismatch.wgsl")));
                },
            }
                .Until("reported", _ => (reported = Shaders.LastRenderError).Length > 0)
                .Wait(30)
                .Go();
        }
        finally
        {
            Shaders.KeepRenderingAfterErrors = false;
        }

        Assert.NotEmpty(reported);
    }

    /// <summary>
    /// A WGSL file edited while the app runs reaches the picture without anything being called.
    /// </summary>
    [Fact]
    public void AnEditedWgslFileReloads()
    {
        if (!App.HasRenderer) return;

        using var assets = PictureRun.Temporary();
        assets.Write("paint.wgsl", Paint("0.0, 1.0, 0.0"));

        var program = ShaderProgram.None;
        var seen = 0;

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("paint.wgsl");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("loaded", _ => program.State == ShaderProgramState.Ready)
            .Wait(Settled)
            .Capture("before")
            .Do("editing the file", _ =>
            {
                seen = program.Generation;
                assets.Write("paint.wgsl", Paint("1.0, 0.0, 0.0"));
            })
            .Until("reloaded", _ => program.Generation > seen)
            .Wait(30)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("before")) > 100, "the cube did not start green");
        Assert.True(PictureRun.Red(run.Picture("after")) > 100, "the edit did not turn it red");
    }

    /// <summary>A program made from WGSL handed over as text draws like one from a file.</summary>
    [Fact]
    public void AProgramCanBeMadeFromText()
    {
        if (!App.HasRenderer) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                var program = Shaders.CreateProgram(ShaderStage.Wgsl(Paint("0.0, 1.0, 0.0")));
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the cube was not drawn green");
    }

    /// <summary>A fragment shader that paints one color, written out as WGSL.</summary>
    private static string Paint(string rgb) => $$"""
        #import bevy_pbr::forward_io::VertexOutput

        @fragment
        fn fragment(mesh: VertexOutput) -> @location(0) vec4<f32> {
            return vec4<f32>({{rgb}}, 1.0);
        }
        """;

    /// <summary>Four floats, laid out as a <c>vec4</c> is in a storage buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Vec4F(float X, float Y, float Z, float W);
}

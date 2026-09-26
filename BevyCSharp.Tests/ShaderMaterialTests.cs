using System.Numerics;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers materials drawn by shaders the game wrote in Slang: that they draw, reload, fail
/// visibly, and answer for themselves.
/// </summary>
/// <remarks>
/// <para>
/// Whether a value reached the shader is not a question anything but a picture can answer. A
/// material that never compiled draws nothing, one whose bind group is laid out differently from
/// what the shader declares draws the wrong color, and both look the same from the managed side.
/// </para>
/// <para>
/// Each test needs <c>slangc</c>, which the build fetches, and returns without asserting anything
/// where there is none or no renderer, the way every picture test does on a headless bridge.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class ShaderMaterialTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    internal const uint Settled = 120;

    /// <summary>Whether this machine can compile and draw a shader at all.</summary>
    internal static bool CanRun => App.HasRenderer && Shaders.SlangAvailable;

    internal static readonly Vector4 Green = new(0f, 1f, 0f, 1f);
    internal static readonly Vector4 Red = new(1f, 0f, 0f, 1f);
    internal static readonly Vector4 Blue = new(0f, 0f, 1f, 1f);

    /// <summary>A material drawn by <c>flat.slang</c> in one color.</summary>
    internal static ShaderMaterial Flat(Vector4 color) =>
        Shaders.CreateMaterial(Shaders.CreateProgram("shaders/flat.slang")).Set("color", color);

    [Fact]
    public void AShaderMaterialDrawsTheColorItWasGiven()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                // Green rather than anything a default could be, so nothing else could have drawn
                // it.
                PictureRun.Cube(ecs, Flat(Green));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);

        Assert.True(
            middle.G > 120 && middle.R < 90 && middle.B < 90,
            $"the cube came out {middle} rather than green");
    }

    /// <summary>A vertex shader moves the mesh, which shows as the shape covering more of it.</summary>
    /// <remarks>
    /// The same cube drawn twice by the same program, pushed along its own normals by
    /// <c>swell</c>. A vertex shader that never ran would leave the two pictures the same size,
    /// which is the only thing that tells it apart from a fragment shader.
    /// </remarks>
    [Fact]
    public void AVertexShaderMovesTheMesh()
    {
        if (!CanRun) return;

        var still = Swell(0f);
        var swollen = Swell(0.6f);

        Assert.True(still > 0, "the cube was not drawn at all");
        Assert.True(
            swollen > still + 200,
            $"the shape covered {still} pixels still and {swollen} pushed out");
    }

    /// <summary>How many green pixels a cube swollen by <paramref name="swell"/> covers.</summary>
    private static int Swell(float swell)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = "shaders/swell.slang",
                    Fragment = "shaders/swell.slang",
                });

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(program).Set("swell", swell).Set("color", Green));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();
        return PictureRun.Green(run.Picture("picture"));
    }

    /// <summary>Whether every program the app made has finished compiling.</summary>
    internal static bool ProgramsReady() =>
        Shaders.Programs.All(program => program.State == ShaderProgramState.Ready);

    /// <summary>A material reads Bevy's time through the prelude.</summary>
    [Fact]
    public void AMaterialReadsTheTime()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, Shaders.CreateMaterial(Shaders.CreateProgram("shaders/clock.slang")));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the material did not see time pass");
    }

    /// <summary>
    /// Six programs drawn at once, each its own, with no limit to run into.
    /// </summary>
    /// <remarks>
    /// One file compiled six ways by its defines. Each cube is a different primary or secondary
    /// color, and each has to come out that color where it stands.
    /// </remarks>
    [Fact]
    public void ManyProgramsDrawAtOnce()
    {
        if (!CanRun) return;

        (int R, int G, int B)[] colors = [(1, 0, 0), (0, 1, 0), (0, 0, 1), (1, 1, 0), (0, 1, 1), (1, 0, 1)];
        var programs = new List<ShaderProgram>();

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            Scene = ecs =>
            {
                Row(ecs);

                for (var i = 0; i < colors.Length; i++)
                {
                    var program = Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        Fragment = "shaders/defines.slang",
                        Defines =
                        {
                            ["RED"] = colors[i].R,
                            ["GREEN"] = colors[i].G,
                            ["BLUE"] = colors[i].B,
                        },
                    });

                    programs.Add(program);
                    PictureRun.Cube(ecs, Shaders.CreateMaterial(program), 1f, InRow(i));
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

    /// <summary>
    /// An orthographic camera over a row of six unit cubes, one to each sixth of a picture 384
    /// pixels across.
    /// </summary>
    internal static void Row(EcsWorld ecs)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings
        {
            Projection = CameraProjection.Orthographic,
            Height = 2f,
            Clear = ClearMode.Custom,
            ClearColor = (0f, 0f, 0f, 1f),
        });

        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
    }

    /// <summary>Where the cube in the <paramref name="index"/>th sixth of <see cref="Row"/> stands.</summary>
    /// <remarks>Twelve units across at an orthographic height of two.</remarks>
    internal static Vec3 InRow(int index) => new(-5f + (2f * index), 0f, 0f);

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
                () => Shaders.CreateProgram("shaders/flat.slang"));

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
                first = Shaders.CreateProgram("shaders/flat.slang");
                second = Shaders.CreateProgram("shaders/flat.slang");
                other = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Fragment = "shaders/flat.slang",
                    Defines = { ["UNUSED"] = true },
                });
            },
        }.Wait(2).Go();

        Assert.True(first.IsValid);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    /// <summary>
    /// A material needs a program, and a program needs something to run and has to be Slang.
    /// </summary>
    [Fact]
    public void WhatCannotRunIsRefusedBeforeReachingTheEngine()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.CreateMaterial(new ShaderMaterialSettings()));

        Assert.Throws<ArgumentException>(
            () => Shaders.CreateProgram(new ShaderProgramSettings { Vertex = "shaders/swell.slang" }));

        Assert.Throws<ArgumentException>(
            () => Shaders.CreateProgram("shaders/flat.wgsl"));

        Assert.Throws<ArgumentException>(
            () => Shaders.CreateInstance(ShaderProgram.None));
    }

    /// <summary>A change to a material's value reaches the picture while it is being drawn.</summary>
    [Fact]
    public void AValueChangesWhileTheMaterialIsDrawn()
    {
        if (!CanRun) return;

        var material = default(ShaderMaterial);

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                material = Flat(Green);
                PictureRun.Cube(ecs, material);
            },
        };

        run.Until("compiled", _ => ProgramsReady())
            .Wait(Settled)
            .Capture("before")
            .Do("turning it red", _ =>
            {
                material.Set("color", Red);
                Assert.Equal([1f, 0f, 0f, 1f], material.GetFloats("color"));
            })
            .Wait(10)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("before")) > 100, "the cube did not start green");
        Assert.True(PictureRun.Red(run.Picture("after")) > 100, "the cube did not turn red");
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
        if (!CanRun) return;

        var followed = Shadow(prepass: true);
        var left = Shadow(prepass: false);

        Assert.True(left > 30, $"the plain ball cast a shadow of only {left} pixels");
        Assert.True(
            followed > left + (left / 2),
            $"the shadow covered {left} pixels from the plain mesh and {followed} from the swollen one");
    }

    /// <summary>How many floor pixels are in shadow under a swollen ball.</summary>
    private static int Shadow(bool prepass)
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

                const string File = "shaders/swell.slang";

                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = File,
                    Fragment = File,
                    PrepassVertex = prepass ? new ShaderStage(File, "prepass_vertex") : default,
                });

                // A sphere rather than a cube, because a cube pushed along its face normals from
                // straight above still casts a square of the same size, since its sides move out
                // edge-on to the light. A sphere pushed out is a larger sphere from every direction,
                // and swelling it by half its radius again more than doubles the shadow's area.
                var ball = ecs.Spawn();
                Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 0.5f));
                Render.SetMaterial(
                    ecs,
                    ball,
                    Shaders.CreateMaterial(program).Set("swell", 0.25f).Set("color", Green));
                ecs.Add(ball, Transform.At(0f, 3f, 0f));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        // The floor is lit near white. What is dark and gray, rather than black background or the
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
    /// The fragment shader reads an input at a location Bevy's vertex shader never writes, which
    /// nothing sees until the pipeline is built. What the editor turns on, so a shader edited into
    /// a shape the pipeline rejects is something to read about and fix. The app is run on past the
    /// error, and ending cleanly afterwards says it was survived.
    /// </remarks>
    [Fact]
    public void AMismatchedShaderIsSurvivedWhenAsked()
    {
        if (!CanRun) return;

        var reported = string.Empty;
        Shaders.KeepRenderingAfterErrors = true;

        try
        {
            new PictureRun
            {
                Scene = ecs =>
                {
                    PictureRun.Camera(ecs);

                    var program = Shaders.CreateProgram(ShaderStage.Slang("""
                        struct Unwritten
                        {
                            float4 position : SV_Position;
                            float4 nowhere : TEXCOORD9;
                        };

                        [shader("fragment")]
                        float4 fragment(Unwritten input) : SV_Target
                        {
                            return input.nowhere;
                        }
                        """));

                    PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
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
    /// An edit to a file a shader imports recompiles the shader, which the asset server alone could
    /// not do, since it never sees the import.
    /// </summary>
    [Fact]
    public void AnEditToAnImportedModuleRecompilesTheShader()
    {
        if (!CanRun) return;

        using var assets = PictureRun.Temporary();
        assets.Write("tint.slang", Tint("0.0, 1.0, 0.0"));
        assets.Write("main.slang", """
            import bcs;
            import tint;

            [shader("fragment")]
            float4 fragment(bcs::VertexOutput mesh) : SV_Target
            {
                return tint();
            }
            """);

        var program = ShaderProgram.None;
        var seen = 0;

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("main.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("compiled", _ => program.State == ShaderProgramState.Ready)
            .Wait(Settled)
            .Capture("before")
            .Do("editing the import", _ =>
            {
                seen = program.Generation;
                assets.Write("tint.slang", Tint("1.0, 0.0, 0.0"));
            })
            .Until("recompiled", _ => program.Generation > seen)
            .Wait(30)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("before")) > 100, "the cube did not start green");
        Assert.True(PictureRun.Red(run.Picture("after")) > 100, "the edit did not turn it red");

        // Written beside the assets with its layout, so a machine without slangc could draw this
        // next time.
        var cache = Path.Combine(assets.Root, ".slang-cache");
        Assert.NotEmpty(Directory.GetFiles(cache, "*.wgsl"));
        Assert.NotEmpty(Directory.GetFiles(cache, "*.json"));
    }

    /// <summary>
    /// A shader that stops compiling keeps drawing with the version that last did, and says what
    /// is wrong.
    /// </summary>
    [Fact]
    public void AMistakeKeepsTheLastShaderThatCompiled()
    {
        if (!CanRun) return;

        using var assets = PictureRun.Temporary();
        assets.Write("paint.slang", Paint("0.0, 1.0, 0.0"));

        var program = ShaderProgram.None;
        var diagnostics = string.Empty;

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("paint.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("compiled", _ => program.State == ShaderProgramState.Ready)
            .Wait(Settled)
            .Do("breaking it", _ => assets.Write("paint.slang", "this is not Slang"))
            .Until("failed", _ => program.State == ShaderProgramState.Failed)
            .Do("reading why", _ => diagnostics = program.Diagnostics)
            .Wait(30)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("after")) > 100, "the last good shader was not kept");
        Assert.Contains("paint.slang", diagnostics);
    }

    /// <summary>A shader that has never compiled draws magenta rather than nothing.</summary>
    [Fact]
    public void AShaderThatNeverCompiledDrawsMagenta()
    {
        if (!CanRun) return;

        using var assets = PictureRun.Temporary();
        assets.Write("broken.slang", "[shader(\"fragment\")] float4 fragment() : SV_Target { return nope; }");

        var program = ShaderProgram.None;

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("broken.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("failed", _ => program.State == ShaderProgramState.Failed)
            .Wait(Settled)
            .Capture("picture")
            .Go();

        Assert.True(
            PictureRun.Magenta(run.Picture("picture")) > 100,
            "the cube was not drawn with the fallback");
    }

    /// <summary>
    /// A binding placed in a group the bridge does not bind is a failed compile, named in the
    /// diagnostics, rather than a pipeline that fails where nothing could say which shader was
    /// wrong.
    /// </summary>
    [Fact]
    public void ABindingInAGroupNothingBindsIsAFailedCompile()
    {
        if (!CanRun) return;

        using var assets = PictureRun.Temporary();
        assets.Write("stray.slang", """
            [[vk::binding(0, 7)]] Texture2D stray;

            [shader("fragment")]
            float4 fragment(float4 position : SV_Position) : SV_Target
            {
                return stray.Load(int3(0, 0, 0));
            }
            """);

        var program = ShaderProgram.None;
        var diagnostics = string.Empty;

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("stray.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("failed", _ => program.State == ShaderProgramState.Failed)
            .Do("reading why", _ => diagnostics = program.Diagnostics)
            .Wait(Settled)
            .Capture("picture")
            .Go();

        Assert.Contains("stray", diagnostics);
        Assert.True(
            PictureRun.Magenta(run.Picture("picture")) > 100,
            "the cube was not drawn with the fallback");
    }

    /// <summary>A program made from Slang handed over as text compiles and draws like a file.</summary>
    [Fact]
    public void AProgramCanBeMadeFromText()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                var program = Shaders.CreateProgram(ShaderStage.Slang(Paint("0.0, 1.0, 0.0")));
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("compiled", _ => ProgramsReady()).Wait(Settled).Capture("picture").Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the cube was not drawn green");
    }

    /// <summary>
    /// An entity says which program draws it and hands over its values, which an inspector reads,
    /// and an entity drawn by Bevy's own material says none.
    /// </summary>
    [Fact]
    public void AnEntityAnswersForItsShaderMaterial()
    {
        if (!CanRun) return;

        var made = ShaderProgram.None;
        var asked = ShaderProgram.None;
        var plain = ShaderProgram.None;
        var cube = Entity.None;
        float[]? read = null;
        IReadOnlyList<ShaderParameter> parameters = [];

        new PictureRun
        {
            Scene = ecs =>
            {
                made = Shaders.CreateProgram("shaders/flat.slang");
                cube = PictureRun.Cube(ecs, Shaders.CreateMaterial(made).Set("color", Green));
                var other = PictureRun.Cube(ecs, Render.CreateMaterial(1f, 1f, 1f));

                asked = Shaders.ProgramOn(cube);
                plain = Shaders.ProgramOn(other);
            },
        }
            .Until("compiled", _ => ProgramsReady())
            .Do("reading it", _ =>
            {
                var material = Shaders.MaterialOn(cube);
                material.Set("color", new Vector4(0.5f, 1f, 0f, 1f));
                read = material.GetFloats("color");
                parameters = material.Parameters;
            })
            .Go();

        Assert.Equal(made, asked);
        Assert.False(plain.IsValid);
        Assert.Equal([0.5f, 1f, 0f, 1f], read);
        Assert.Equal(
            [new ShaderParameter(ShaderParameterKind.Number, "color", ShaderScalar.Float, 4, 1)],
            parameters);
    }

    /// <summary>A fragment shader that paints one color, written out as Slang.</summary>
    private static string Paint(string rgb) => $$"""
        import bcs;

        [shader("fragment")]
        float4 fragment(bcs::VertexOutput mesh) : SV_Target
        {
            return float4({{rgb}}, 1.0);
        }
        """;

    /// <summary>A module with one function answering one color.</summary>
    private static string Tint(string rgb) => $$"""
        module tint;

        public float4 tint()
        {
            return float4({{rgb}}, 1.0);
        }
        """;
}

using System.Runtime.InteropServices;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers materials drawn by shaders written in Slang.
/// </summary>
/// <remarks>
/// <para>
/// Each test needs <c>slangc</c>, found through <c>BCS_SLANGC</c> or the <c>PATH</c>, and returns
/// without asserting anything on a machine without one, the way the picture tests do on a bridge
/// without a renderer.
/// </para>
/// <para>
/// What these pin down is the part that is Slang's rather than Bevy's: that the prelude's bindings
/// and stage structs line up with what Bevy hands a shader, that a C# struct and a Slang one agree
/// on layout, and that an edit, an import and a mistake each do what the documentation says.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class SlangShaderTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const uint Settled = 120;

    /// <summary>Whether this machine can run these at all.</summary>
    private static bool CanRun => App.HasRenderer && Shaders.SlangAvailable;

    [Fact]
    public void ASlangFragmentShaderDrawsTheColorItWasGiven()
    {
        if (!CanRun) return;

        var program = ShaderProgram.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("shaders/flat.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program, [0f, 1f, 0f, 1f]));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(
            middle.G > 120 && middle.R < 90 && middle.B < 90,
            $"the cube came out {middle} rather than green");
    }

    /// <summary>
    /// A Slang vertex shader moves the mesh, which says the prelude's vertex struct, mesh
    /// transforms and view line up with Bevy's.
    /// </summary>
    [Fact]
    public void ASlangVertexShaderMovesTheMesh()
    {
        if (!CanRun) return;

        var still = ShaderMaterialTests.Swell("shaders/swell.slang", 0f);
        var swollen = ShaderMaterialTests.Swell("shaders/swell.slang", 0.6f);

        Assert.True(still > 0, "the cube was not drawn at all");
        Assert.True(
            swollen > still + 200,
            $"the shape covered {still} pixels still and {swollen} pushed out");
    }

    /// <summary>
    /// The same cube in the same place drawn by Slang and by WGSL covers the same pixels, which
    /// says the Slang prelude's transforms are Bevy's rather than merely close to them.
    /// </summary>
    [Fact]
    public void SlangAndWgslPutTheMeshInTheSamePlace()
    {
        if (!CanRun) return;

        var slang = ShaderMaterialTests.Swell("shaders/swell.slang", 0.3f);
        var wgsl = ShaderMaterialTests.Swell("shaders/swell.wgsl", 0.3f);

        Assert.True(
            Math.Abs(slang - wgsl) < Math.Max(20, wgsl / 50),
            $"Slang covered {slang} pixels and WGSL {wgsl}");
    }

    /// <summary>A Slang prepass vertex shader moves the shadow along with the mesh.</summary>
    [Fact]
    public void ASlangPrepassVertexShaderMovesTheShadowToo()
    {
        if (!CanRun) return;

        var followed = ShaderMaterialTests.Shadow("shaders/swell.slang", prepass: true);
        var left = ShaderMaterialTests.Shadow("shaders/swell.slang", prepass: false);

        Assert.True(left > 30, $"the plain ball cast a shadow of only {left} pixels");
        Assert.True(
            followed > left + (left / 2),
            $"the shadow covered {left} pixels from the plain mesh and {followed} from the swollen one");
    }

    /// <summary>
    /// A C# struct in the material's data is read back field by field by the same struct declared
    /// in Slang.
    /// </summary>
    [Fact]
    public void ACSharpStructIsReadByTheSameStructInSlang()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/particles.slang"));

                // Only the third is green at full size, so a stride or an offset that is wrong
                // anywhere reads one of the others, or a mixture.
                Shaders.SetData<Particle>(material,
                [
                    new Particle(1f, 1f, 0f, 0f),
                    new Particle(0.2f, 0f, 0f, 1f),
                    new Particle(1f, 0f, 1f, 0f),
                    new Particle(1f, 1f, 1f, 1f),
                ]);

                PictureRun.Cube(ecs, material);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(
            middle.G > 120 && middle.R < 90 && middle.B < 90,
            $"the cube came out {middle} rather than green");
    }

    /// <summary>
    /// An edit to a file a Slang shader imports recompiles the shader, which the asset server
    /// alone could not do, since it never sees the import.
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

        // Written beside the assets, so a machine without slangc could draw this next time.
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(assets.Root, ".slang-cache"), "*.wgsl"));
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
    /// A shader whose bindings disagree with what the material binds is a failed compile, named
    /// in the diagnostics, rather than a pipeline that fails where nothing could say which shader
    /// was wrong.
    /// </summary>
    [Fact]
    public void ABindingOfTheWrongKindIsAFailedCompile()
    {
        if (!CanRun) return;

        using var assets = PictureRun.Temporary();
        assets.Write("mismatch.slang", """
            [[vk::binding(0, 3)]] Texture2D wrong;

            [shader("fragment")]
            float4 fragment(float4 position : SV_Position) : SV_Target
            {
                return wrong.Load(int3(0, 0, 0));
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
                program = Shaders.CreateProgram("mismatch.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        };

        run.Until("failed", _ => program.State == ShaderProgramState.Failed)
            .Do("reading why", _ => diagnostics = program.Diagnostics)
            .Wait(Settled)
            .Capture("picture")
            .Go();

        Assert.Contains("wrong", diagnostics);
        Assert.True(
            PictureRun.Magenta(run.Picture("picture")) > 100,
            "the cube was not drawn with the fallback");
    }

    /// <summary>A program made from Slang handed over as text compiles and draws like a file.</summary>
    [Fact]
    public void ASlangProgramCanBeMadeFromText()
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

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(Settled)
            .Capture("picture")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the cube was not drawn green");
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

    /// <summary>The C# side of the struct <c>particles.slang</c> declares.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Particle(float Size, float R, float G, float B);
}

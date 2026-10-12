using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a shader recompiled when a module it imports changes, imported itself or exported again
/// by a module it imports, which the asset server alone could not see, since it never reads an
/// import.
/// </summary>
/// <remarks>
/// Every file a compile read, as slangc's dependency file lists them, is part of what decides
/// whether the cached result still stands and is watched for an edit, so each test edits the
/// innermost module and looks for the cube's color to follow.
/// </remarks>
[Collection("engine")]
public sealed class ShaderImportTests
{
    /// <summary>
    /// An edit to a file a shader imports recompiles the shader, which the asset server alone could
    /// not do, since it never sees the import.
    /// </summary>
    [SkippableFact]
    public void AnEditToAnImportedModuleRecompilesTheShader()
    {
        Needs.Shaders();

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
            .Wait(ShaderMaterialTests.Settled)
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
    /// An edit to a module that another module exports again, with <c>__exported import</c>,
    /// recompiles a shader that imports only the exporter, since every file a compile read is
    /// part of what decides whether its cached result still stands, the exporter's export included.
    /// </summary>
    [SkippableFact]
    public void AnEditToAModuleAnotherExportsRecompilesTheShaderImportingIt()
    {
        Needs.Shaders();

        using var assets = PictureRun.Temporary();
        assets.Write("tint.slang", Tint("0.0, 1.0, 0.0"));
        assets.Write("gather.slang", """
            module gather;

            __exported import tint;
            """);
        assets.Write("main.slang", """
            import bcs;
            import gather;

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
            .Wait(ShaderMaterialTests.Settled)
            .Capture("before")
            .Do("editing the module gather exports", _ =>
            {
                seen = program.Generation;
                assets.Write("tint.slang", Tint("1.0, 0.0, 0.0"));
            })
            .Until("recompiled", _ => program.Generation > seen)
            .Wait(30)
            .Capture("after")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("before")) > 100, "the cube did not start green");
        Assert.True(PictureRun.Red(run.Picture("after")) > 100, "the edit to the exported module did not turn it red");
    }

    /// <summary>A module with one function answering one color.</summary>
    private static string Tint(string rgb) => $$"""
        module tint;

        public float4 tint()
        {
            return float4({{rgb}}, 1.0);
        }
        """;
}

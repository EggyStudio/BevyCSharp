using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What a camera draws after tonemapping comes after Bevy's antialiasing, each pass of which takes
/// the picture into a fresh texture, so a draw blended into the picture there is not left behind in
/// the texture the antialiasing read from.
/// </summary>
[Collection("engine")]
public sealed class AfterAntialiasingTests
{
    [SkippableTheory]
    [InlineData("fxaa")]
    [InlineData("smaa")]
    [InlineData("sharpen")]
    public void ADrawAfterTonemappingOutlastsTheAntialiasing(string pass)
    {
        Needs.Shaders();
        ShaderInstance tint = default;
        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                Render.SetPostProcessing(camera, new PostSettings
                {
                    Hdr = true,
                    Msaa = 1,
                    AntiAlias = pass switch { "fxaa" => AntiAliasPass.Fxaa, "smaa" => AntiAliasPass.Smaa, _ => AntiAliasPass.None },
                    Sharpen = pass == "sharpen" ? 0.6f : 0f,
                });
                PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f));

                // A green triangle over the whole view added to the picture once it is tonemapped.
                tint = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    DrawVertex = ShaderStage.Slang(SceneFieldShadowTests.Tint, "vertex"),
                    DrawFragment = ShaderStage.Slang(SceneFieldShadowTests.Tint, "fragment"),
                }));
                Shaders.SetViewDraws(camera, ViewDraw.Fixed(tint, FramePoint.AfterTonemapping, 3, 1, DrawBlend.Add, writesDepth: false));
            },
        };

        run.Until("compiled", _ => tint.Program.State == ShaderProgramState.Ready && Render.PipelinesReady())
            .Wait(20)
            .Capture("tinted")
            .Go();

        // The background, black where nothing is drawn, takes the draw's green.
        var (r, g, b, _) = run.Picture("tinted").At(2, 2);
        Assert.True(g > 100 && r < 40 && b < 40, $"under {pass} the corner reads {r}, {g}, {b}");
    }
}

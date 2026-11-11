using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What a camera draws at <see cref="FramePoint.AfterTemporalAntialiasing"/> comes after Bevy's
/// temporal antialiasing, whose history averages away a speck that moves with no motion of its own
/// in the prepass, where one drawn before it is.
/// </summary>
[Collection("engine")]
public sealed class AfterTemporalAntialiasingTests
{
    // A square two pixels wide of a picture 64 across, white, at a place hashed from the frame's
    // number, so it stands somewhere new every frame and the antialiasing's history never holds it,
    // at the near plane so nothing hides it.
    private const string Speck = """
        import bcs_pass;

        struct Corner
        {
            float4 position : SV_Position;
        };

        static const float2 corners[6] = {
            float2(-1.0, -1.0), float2(1.0, -1.0), float2(1.0, 1.0),
            float2(-1.0, -1.0), float2(1.0, 1.0), float2(-1.0, 1.0),
        };

        [shader("vertex")]
        Corner vertex(uint vertex_id : SV_VertexID)
        {
            uint h = passGlobals.frame_count * 747796405u + 2891336453u;
            h = ((h >> ((h >> 28u) + 4u)) ^ h) * 277803737u;
            float2 at = float2(float(h & 1023u), float((h >> 10u) & 1023u)) / 1023.0 * 1.6 - 0.8;
            Corner corner;
            corner.position = float4(at + corners[vertex_id] * (2.0 / 64.0), 1.0, 1.0);
            return corner;
        }

        [shader("fragment")]
        float4 fragment(Corner corner) : SV_Target
        {
            return float4(1.0, 1.0, 1.0, 1.0);
        }
        """;

    [SkippableTheory]
    [InlineData(FramePoint.BeforeTonemapping)]
    [InlineData(FramePoint.AfterTemporalAntialiasing)]
    public void ASpeckMovingWithNoMotionOfItsOwnShowsAfterTheAntialiasing(FramePoint point)
    {
        Needs.Shaders();
        ShaderInstance speck = default;
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
                    Tonemapper = Tonemapper.None,
                    Dither = false,
                    AntiAlias = AntiAliasPass.Temporal,
                });
                PictureRun.Cube(ecs, Render.CreateMaterial(0.1f, 0.1f, 0.1f));
                speck = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    DrawVertex = ShaderStage.Slang(Speck, "vertex"),
                    DrawFragment = ShaderStage.Slang(Speck, "fragment"),
                }));
                Shaders.SetViewDraws(camera, ViewDraw.Fixed(speck, point, 6, 1, DrawBlend.Opaque, writesDepth: false));
            },
        };

        run.Until("compiled", _ => speck.Program.State == ShaderProgramState.Ready && Render.PipelinesReady())
            .Wait(30)
            .Capture("speck")
            .Go();

        var picture = run.Picture("speck");
        var brightest = 0;
        for (var y = 0u; y < picture.Height; y++)
            for (var x = 0u; x < picture.Width; x++)
                brightest = Math.Max(brightest, picture.At(x, y).G);

        if (point == FramePoint.AfterTemporalAntialiasing)
            Assert.True(brightest > 230, $"the speck drawn after the antialiasing reads {brightest} at its brightest");
        else
            Assert.True(brightest < 170, $"the speck drawn before the antialiasing reads {brightest} at its brightest");
    }
}

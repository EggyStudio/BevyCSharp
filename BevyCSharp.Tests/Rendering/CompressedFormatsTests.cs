using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The compressed texture formats a device decodes, by which a game loads the copy of a texture the
/// device takes, as Bevy's skybox example cycles through those it does.
/// </summary>
[Collection("engine")]
public sealed class CompressedFormatsTests
{
    private const CompressedImageFormats Known =
        CompressedImageFormats.Astc | CompressedImageFormats.AstcHdr | CompressedImageFormats.Bc | CompressedImageFormats.Etc2;

    [Fact]
    public void ARunThatDrawsNothingDecodesNone()
    {
        using var harness = new EngineHarness(frames: 2);
        var formats = new List<CompressedImageFormats>();

        harness.On(Stage.Update, _ => formats.Add(Render.CompressedFormats));
        harness.Run();

        Assert.NotEmpty(formats);
        Assert.All(formats, format => Assert.Equal(CompressedImageFormats.None, format));
    }

    [SkippableFact]
    public void ADeviceSaysTheSameEveryFrameInBevysBits()
    {
        Needs.Renderer();

        var formats = new List<CompressedImageFormats>();
        var run = new PictureRun { EachFrame = _ => formats.Add(Render.CompressedFormats) };
        run.Wait(5).Go();

        // What the device decodes does not change while it runs, and a bit outside Bevy's four
        // would be a number read from the wrong place.
        Assert.NotEmpty(formats);
        Assert.Single(formats.Distinct());
        Assert.Equal(CompressedImageFormats.None, formats[0] & ~Known);
    }
}

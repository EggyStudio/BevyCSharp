using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Images packed into one atlas as the app runs, as Bevy's texture_atlas packs a folder of sprites.
/// </summary>
[Collection("engine")]
public sealed class AtlasPackingTests
{
    /// <summary>
    /// A red image and a blue one packed into one, each given its own frame, and both colors in the
    /// packed image, which is large enough for the two and the padding between them.
    /// </summary>
    [SkippableFact]
    public void TwoImagesArePackedIntoOneEachWithItsFrame()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 3);
        PackedAtlas? atlas = null;
        ImagePixels? pixels = null;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (atlas is not null) return;
            var red = Render.CreateImage([.. Enumerable.Repeat<byte[]>([255, 0, 0, 255], 8 * 4).SelectMany(texel => texel)], 8, 4);
            var blue = Render.CreateImage([.. Enumerable.Repeat<byte[]>([0, 0, 255, 255], 4 * 8).SelectMany(texel => texel)], 4, 8);
            atlas = Render2d.PackAtlas([red, blue], padding: (2, 2));
            Render.TryReadImage(atlas!.Image, out pixels);
        });
        harness.Run();

        Assert.NotNull(atlas);
        Assert.Equal([0u, 1u], atlas.Indices.Order().ToArray());

        Assert.NotNull(pixels);
        Assert.True(pixels.Width * pixels.Height >= 8 * 4 + 4 * 8 + 2 * 8, $"the packed image is {pixels.Width} by {pixels.Height}");
        var texels = pixels.Data.Chunk(4).ToList();
        Assert.Contains(texels, texel => texel[0] == 255 && texel[2] == 0 && texel[3] == 255);
        Assert.Contains(texels, texel => texel[0] == 0 && texel[2] == 255 && texel[3] == 255);
    }

    /// <summary>An image still loading is not packed yet, and the call says so with nothing.</summary>
    [Fact]
    public void AnImageStillLoadingIsNotPackedYet()
    {
        using var harness = new EngineHarness(frames: 1);
        var answered = new List<PackedAtlas?>();

        harness.OnContext(Stage.Update, ctx => answered.Add(Render2d.PackAtlas([AssetServer.Load(AssetKind.Image, "textures/checker.png")])));
        harness.Run();

        Assert.Equal([null], answered);
    }
}

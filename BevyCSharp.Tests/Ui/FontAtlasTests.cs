using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Bevy's font atlases, counted and emptied as Bevy's text stress tests count and empty them, and
/// their pictures read as Bevy's font_atlas_debug shows them.
/// </summary>
[Collection("engine")]
public sealed class FontAtlasTests
{
    [SkippableFact]
    public void TextDrawnFillsAnAtlasThatEmptiesAndFillsAgain()
    {
        Needs.Renderer();

        (int Count, long Bytes) drawn = default, emptied = default, again = default;
        var text = Entity.None;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                text = Ui.SpawnText("Atlas", new UiSettings(), new UiTextSettings { FontSize = 40f });
            },
        };

        run.Wait(10)
            .Do("count", _ => drawn = Ui.FontAtlases)
            .Do("clear", _ =>
            {
                Ui.ClearFontAtlases();
                emptied = Ui.FontAtlases;

                // Laid out again, as a text that changes is, so its glyphs are drawn again.
                Ui.SetText(text, "Atlas again");
            })
            .Wait(10)
            .Do("count again", _ => again = Ui.FontAtlases)
            .Go();

        Assert.True(drawn.Count > 0 && drawn.Bytes > 0, $"the text drawn filled {drawn}");
        Assert.Equal((0, 0L), emptied);
        Assert.True(again.Count > 0, "the text drawn after the atlases emptied did not fill one again");
    }

    /// <summary>
    /// Text at two sizes fills an atlas a size, the smaller first, each picture an image with
    /// pixels, and reading them again answers the same pictures.
    /// </summary>
    [SkippableFact]
    public void EachSizeOfTextDrawnHasAnAtlasWhosePictureIsAnImage()
    {
        Needs.Renderer();

        IReadOnlyList<FontAtlasPicture> first = [], second = [];
        var sizes = new List<(uint Width, uint Height)>();
        var run = new PictureRun
        {
            Scene = _ =>
            {
                Render2d.SpawnCamera2d();
                Ui.SpawnText("Large", new UiSettings(), new UiTextSettings { FontSize = 50f });
                Ui.SpawnText("small", new UiSettings { Absolute = true, Top = Length.Px(80f) }, new UiTextSettings { FontSize = 20f });
            },
        };

        run.Wait(10)
            .Do("read", world =>
            {
                first = Ui.FontAtlasPictures();
                foreach (var atlas in first)
                {
                    Assert.True(Render.TryImageSize(atlas.Picture, out var width, out var height, out _), $"the atlas of size {atlas.FontSize} has no image");
                    sizes.Add((width, height));
                }
            })
            .Do("read again", _ => second = Ui.FontAtlasPictures())
            .Go();

        Assert.Equal(new[] { 20f, 50f }, first.Select(atlas => atlas.FontSize).Distinct());
        Assert.All(sizes, size => Assert.True(size.Width > 0 && size.Height > 0, $"an atlas's picture is {size.Width} by {size.Height}"));
        Assert.Equal(first, second);
    }
}

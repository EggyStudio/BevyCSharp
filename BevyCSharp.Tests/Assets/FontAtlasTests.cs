using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Bevy's font atlases, counted and emptied as Bevy's text stress tests count and empty them.
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
}

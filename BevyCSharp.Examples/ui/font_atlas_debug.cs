// Bevy's font_atlas_debug example, examples/ui/text/font_atlas_debug.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// This example illustrates how font atlases are populated, the textures Bevy draws each glyph of a
// size of a font into the first time a text needs it, to make drawing text quicker. A character
// picked at random is added to the yellow text along the bottom twenty times a second, and each
// atlas Bevy opens is shown along the top as it opens. The characters are drawn from .NET's
// generator seeded as Bevy's is, which deals other characters than Bevy's.
internal static class FontAtlasDebug
{
    // Bevy's State, how many atlases are shown and the timer that adds a character.
    private static int _atlasCount;
    private static float _sinceAdded;
    private const float AddEvery = 0.05f;

    private static Random _rng = new(0);
    private static Entity _text;
    private static string _string = "a";

    public static void Build(App app)
    {
        (_atlasCount, _sinceAdded, _string) = (0, 0f, "a");

        // Seeded with Bevy's number, cut to the four bytes .NET's generator takes, so the example is
        // the same each run, as Bevy seeds its own for its tests.
        _rng = new Random(unchecked((int)19878367467713L));

        app.Startup(Setup, "font_atlas_debug.Setup");
        app.Update(TextUpdateSystem, "font_atlas_debug.TextUpdateSystem");
        app.Update(AtlasRenderSystem, "font_atlas_debug.AtlasRenderSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        Render.SetClearColor((0f, 0f, 0f, 1f));
        Render2d.SpawnCamera2d();

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        var bottom = Ui.SpawnNode(new UiSettings { Absolute = true, Bottom = Length.Px(0f), Color = (0f, 0f, 0f, 0f) });
        _text = Ui.SpawnText(_string, new UiSettings { Color = Color.FromSrgb(1f, 1f, 0f) }, new UiTextSettings { Font = font, FontSize = 50f });
        ctx.Ecs.SetParent(_text, bottom);
    }

    // The next of the atlases of the text's size, shown beside the last, as each opens.
    private static void AtlasRenderSystem(BehaviorContext ctx)
    {
        var pictures = Ui.FontAtlasPictures();
        if (pictures.Count == 0) return;

        var atlases = pictures.Where(atlas => atlas.FontSize == pictures[0].FontSize).ToList();
        if (_atlasCount == atlases.Count) return;

        var picture = atlases[_atlasCount].Picture;
        if (!Render.TryImageSize(picture, out var width, out _, out _)) return;

        var node = Ui.SpawnNode(new UiSettings { Absolute = true, Top = Length.Px(0f), Left = Length.Px(width * (float)_atlasCount) });
        Ui.SetImage(node, picture);
        _atlasCount++;
    }

    // A character added each time the timer finishes, where the text does not hold it yet.
    private static void TextUpdateSystem(BehaviorContext ctx)
    {
        _sinceAdded += ctx.Time.Delta;
        if (_sinceAdded < AddEvery) return;
        _sinceAdded %= AddEvery;

        var c = (char)_rng.Next(256);
        if (_string.Contains(c)) return;

        _string += c;
        Ui.SetText(_text, _string);
    }
}

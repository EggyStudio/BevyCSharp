// Bevy's many_text2d example, examples/stress_tests/many_text2d.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;

namespace BevyCSharp.Examples.StressTests;

// A great many texts in the world at different sizes, turns, scales and colors, with the camera
// moved over them to see how well what is off screen is left out. --many-glyphs and
// --many-font-sizes fill more font atlases, --recompute lays every text out again each frame,
// --hinting turns on TrueType hinting, --no-frustum-culling draws every text whether it is seen or
// not, and --center centers each.
internal static class ManyText2d
{
    private const float CameraSpeed = 1000f;

    // Some code points of glyphs FiraSans-Bold.ttf has.
    private static readonly (int First, int Last)[] CodePointRanges = [(0x20, 0x7e), (0xa0, 0x17e), (0x180, 0x2b2), (0x3f0, 0x479), (0x48a, 0x52f)];

    private static readonly float[] FontSizes = [10f, 20f, 30f, 40f, 50f, 60f];

    // Bevy's Args, read from the command line.
    private static bool _manyGlyphs, _manyFontSizes, _hinting, _noFrustumCulling, _center;

    private static Entity _camera;
    private static float _printing;
    private static readonly List<Entity> Texts = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        (_manyGlyphs, _manyFontSizes, _hinting, _noFrustumCulling, _center) = (
            arguments.Contains("--many-glyphs"), arguments.Contains("--many-font-sizes"), arguments.Contains("--hinting"),
            arguments.Contains("--no-frustum-culling"), arguments.Contains("--center"));
        (_printing, _camera) = (0f, Entity.None);
        Texts.Clear();

        app.Startup(Setup, "many_text2d.Setup");
        app.Update(MoveCamera, "many_text2d.MoveCamera");
        app.Update(PrintCounts, "many_text2d.PrintCounts");
        if (arguments.Contains("--recompute")) app.Update(Recompute, "many_text2d.Recompute");
    }

    private static void Setup(BehaviorContext ctx)
    {
        // Bevy's warning_string.txt, which its stress tests say as they start.
        Console.Error.WriteLine("This is a stress test used to push Bevy to its limit and debug performance issues. It is not representative of an actual game. It must be built in Release or it will be very slow.");
        var ecs = ctx.Ecs;

        // Seeded, as Bevy seeds its own generator, though .NET's gives other numbers than ChaCha.
        var random = new Random(42);
        const float TileSize = 64f;
        const int Half = 640 / 4;
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        _camera = Render2d.SpawnCamera2d();

        // Spread so that many are on screen and many off it.
        for (var y = -Half; y < Half; y++)
        {
            for (var x = -Half; x < Half; x++)
            {
                var translation = new Vec3(x * TileSize, y * TileSize, random.NextSingle());
                var rotation = Quat.FromRotationZ(random.NextSingle());
                var scale = new Vec3(random.NextSingle() * 2f);
                var (r, g, b, a) = Color.FromHsl(random.NextSingle() * 360f, 0.8f, 0.8f);

                var text = ecs.Spawn();
                ecs.Add(text, new Transform(translation, rotation, scale));
                ecs.Insert<Text2dRef>(text).Value = RandomText(random);
                var style = ecs.Insert<TextFontRef>(text);
                style.Font = new FontSource.Handle(font);
                style.FontSize = new FontSize.Px(_manyFontSizes ? FontSizes[random.Next(FontSizes.Length)] : 60f);
                ecs.Insert<TextColorRef>(text).Value = new Color(r, g, b, a);
                ecs.Insert<TextLayoutRef>(text).Justify = _center ? Justify.Center : Justify.Left;
                ecs.Insert<FontHintingRef>(text).Value = _hinting ? FontHintingRef.ValueVariant.Enabled : FontHintingRef.ValueVariant.Disabled;
                if (_noFrustumCulling) ecs.Insert<NoFrustumCullingRef>(text);
                Texts.Add(text);
            }
        }
    }

    // "Bevy", or four glyphs picked from one of the ranges with --many-glyphs.
    private static string RandomText(Random random)
    {
        if (!_manyGlyphs) return "Bevy";

        var (first, last) = CodePointRanges[random.Next(CodePointRanges.Length)];
        var picked = new HashSet<int>();
        while (picked.Count < 4) picked.Add(random.Next(first, last + 1));
        return string.Concat(picked.Select(codePoint => (char)codePoint));
    }

    private static void MoveCamera(BehaviorContext ctx)
    {
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var delta = ctx.Time.Delta;
        camera.Rotation = Quat.FromRotationZ(delta * 0.5f) * camera.Rotation;
        camera.Translation += camera.Rotation * (Vec3.UnitX * CameraSpeed * delta);
        ctx.Ecs.Set(_camera, camera);
    }

    // The texts and those a camera saw, once a second. Bevy also counts the font atlases and their
    // bytes from its FontAtlasSet, which no wrapper reaches, so those two are left out.
    private static void PrintCounts(BehaviorContext ctx)
    {
        _printing += ctx.Time.Delta;
        if (_printing < 1f) return;

        _printing -= 1f;
        var visible = Texts.Count(text => ctx.Ecs.TryGet<ViewVisibility>(text, out var seen) && seen.IsVisible);
        Console.WriteLine($"Texts: {Texts.Count} Visible: {visible}");
    }

    // Each text marked changed, which Bevy does by reaching it mutably, and here by writing it
    // again as it is.
    private static void Recompute(BehaviorContext ctx)
    {
        foreach (var text in Texts)
        {
            var text2d = ctx.Ecs.Wrap<Text2dRef>(text);
            text2d.Value = text2d.Value;
        }
    }
}

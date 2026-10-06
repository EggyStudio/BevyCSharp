// Bevy's many_gradients example, examples/stress_tests/many_gradients.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Text.Json.Nodes;
using Bevy;

namespace BevyCSharp.Examples.StressTests;

// A grid of interface nodes, each painted with a linear gradient of seven stops at its own angle.
// --gradient-count sets how many, 900 unless given, --animate moves every gradient's colors each
// frame, and --srgb or --hsl mixes the stops in that space rather than Oklab.
internal static class ManyGradients
{
    private const int Cols = 30;
    internal const string Background = "bevy_ui::gradients::BackgroundGradient";

    // Bevy's Args, read from the command line.
    private static int _gradientCount;
    internal static bool Animate;
    internal static string ColorSpace = "Oklaba";

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
        var count = Array.IndexOf(arguments, "--gradient-count");
        _gradientCount = count >= 0 && count + 1 < arguments.Length && int.TryParse(arguments[count + 1], out var given) ? given : 900;
        Animate = arguments.Contains("--animate");
        ColorSpace = arguments.Contains("--srgb") ? "Srgba" : arguments.Contains("--hsl") ? "Hsla" : "Oklaba";

        Console.WriteLine($"Gradient stress test with {_gradientCount} gradients");
        Console.WriteLine($"Color space: {(ColorSpace == "Srgba" ? "sRGB" : ColorSpace == "Hsla" ? "HSL" : "OkLab (default)")}");

        app.Startup(Setup, "many_gradients.Setup");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var rows = (_gradientCount + Cols - 1) / Cols;
        var grid = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Display = UiDisplay.Grid });
        UiGrid.Set(grid, new GridSettings { Columns = [Track.Flex(1f).Repeated(Cols)], Rows = [Track.Flex(1f).Repeated(rows)] });

        // Bevy's palette of CSS colors: red, blue, green, yellow, orange, lime and dark cyan.
        Color[] colors =
        [
            Color.FromSrgb8(255, 0, 0), Color.FromSrgb8(0, 0, 255), Color.FromSrgb8(0, 128, 0), Color.FromSrgb8(255, 255, 0),
            Color.FromSrgb8(255, 165, 0), Color.FromSrgb8(0, 255, 0), Color.FromSrgb8(0, 139, 139),
        ];
        float[] points = [0f, 100f, 20f, 40f, 60f, 80f, 90f];

        for (var i = 0; i < _gradientCount; i++)
        {
            // In degrees, as Bevy's example gives the angle, where the gradient reads radians.
            var angle = i * 10f % 360f;
            var node = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
            ecs.InsertReflected(node, Background, Linear(angle, colors, points).ToJsonString());
            ecs.Add(node, new GradientNode { Index = i, Angle = angle });
            ecs.SetParent(node, grid);
        }
    }

    // One linear gradient, its stops each given a point along it.
    internal static JsonArray Linear(float angle, Color[] colors, float[] points) =>
    [
        new JsonObject
        {
            ["Linear"] = new JsonObject
            {
                ["color_space"] = ColorSpace,
                ["angle"] = angle,
                ["stops"] = new JsonArray([.. colors.Select((color, i) => (JsonNode)new JsonObject
                {
                    ["color"] = new JsonObject { ["LinearRgba"] = new JsonObject { ["red"] = color.R, ["green"] = color.G, ["blue"] = color.B, ["alpha"] = color.A } },
                    ["point"] = new JsonObject { ["Percent"] = points[i] },
                    ["hint"] = 0.5f,
                })]),
            },
        },
    ];
}

/// <summary>A node of the grid, which knows its place in it to move its colors by.</summary>
[Behavior]
public partial struct GradientNode
{
    private static readonly float[] Points = [0f, 100f, 20f, 40f, 60f, 80f, 90f];
    private static readonly float[] Shifts = [0f, 0.3f, 0.1f, 0.15f, 0.2f, 0.25f, 0.28f];

    /// <summary>Its place in the grid.</summary>
    public int Index;

    /// <summary>Its gradient's angle, which Bevy keeps in the gradient it changes the stops of.</summary>
    public float Angle;

    /// <summary>
    /// With --animate, its stops each a hue moved on with the clock, the hues spread along it, and
    /// the grid's hues moving in a wave.
    /// </summary>
    [OnUpdate]
    public void AnimateGradients(BehaviorContext ctx)
    {
        if (!ManyGradients.Animate) return;

        var hueShift = MathF.Sin(ctx.Time.Elapsed + Index * 0.01f) * 0.5f + 0.5f;
        var colors = Shifts.Select(shift => Color.FromHsl((hueShift + shift) * 360f % 360f, 1f, 0.5f)).ToArray();
        ctx.Ecs.SetReflected(ctx.Entity, ManyGradients.Background, string.Empty, ManyGradients.Linear(Angle, colors, Points).ToJsonString());
    }
}

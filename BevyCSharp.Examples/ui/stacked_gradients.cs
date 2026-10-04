using System.Text.Json.Nodes;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows gradients stacked on one node, red and blue rising from two corners, a yellow beam from the
// middle, a sun near the top, and two dark wedges, each laid over the ones before it.
internal static class StackedGradients
{
    // A background gradient is a list of gradients, each holding a list of stops, which a wrapper
    // does not type, so the stack is written as JSON.
    private const string Gradients = "bevy_ui::gradients::BackgroundGradient";

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var grid = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Width = Length.Percent(100f), Height = Length.Percent(100f) });
        var node = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(node, grid);

        // Bevy's gradients as the JSON its reflection reads, a list in the order they are drawn.
        var (red, blue, yellow, white, black) = ((1f, 0f, 0f), (0f, 0f, 1f), (1f, 1f, 0f), (1f, 1f, 1f), (0f, 0f, 0f));
        var stack = new JsonArray
        {
            Linear(MathF.Tau / 8f, Stop(red, 1f), Stop(red, 0f)),
            Linear(7f * MathF.Tau / 8f, Stop(blue, 1f), Stop(blue, 0f)),
            new JsonObject
            {
                ["Conic"] = new JsonObject
                {
                    ["color_space"] = "Oklaba",
                    ["start"] = 0f,
                    ["position"] = Position(0f, 0f, new JsonObject { ["Px"] = 0f }),
                    ["stops"] = new JsonArray(Angular(yellow, 0f), Angular(yellow, 0f), Angular(yellow, 1f), Angular(yellow, 0f), Angular(yellow, 0f)),
                },
            },
            new JsonObject
            {
                ["Radial"] = new JsonObject
                {
                    ["color_space"] = "Oklaba",
                    ["position"] = Position(0f, -0.5f, new JsonObject { ["Percent"] = 5f }),
                    ["shape"] = new JsonObject { ["Circle"] = new JsonObject { ["Vh"] = 30f } },
                    ["stops"] = new JsonArray(Stop(white, 1f), Stop(yellow, 1f), Stop(yellow, 0.1f), Stop(yellow, 0f)),
                },
            },
            Linear(MathF.Tau / 16f, Stop(black, 1f), Stop(black, 0f)),
            Linear(15f * MathF.Tau / 16f, Stop(black, 1f), Stop(black, 0f)),
        };
        ecs.InsertReflected(node, Gradients, stack.ToJsonString());
    }, "stacked_gradients.Setup");

    private static JsonObject Linear(float angle, params JsonNode[] stops) => new()
    {
        ["Linear"] = new JsonObject { ["color_space"] = "Oklaba", ["angle"] = angle, ["stops"] = new JsonArray(stops) },
    };

    // A stop placed where the gradient puts it, given in sRGB as Bevy's palette colors are.
    private static JsonObject Stop((float R, float G, float B) color, float alpha) => new()
    {
        ["color"] = Srgba(color, alpha),
        ["point"] = "Auto",
        ["hint"] = 0.5f,
    };

    private static JsonObject Angular((float R, float G, float B) color, float alpha) => new()
    {
        ["color"] = Srgba(color, alpha),
        ["angle"] = null,
        ["hint"] = 0.5f,
    };

    private static JsonObject Srgba((float R, float G, float B) color, float alpha) => new()
    {
        ["Srgba"] = new JsonObject { ["red"] = color.R, ["green"] = color.G, ["blue"] = color.B, ["alpha"] = alpha },
    };

    // Bevy's UiPosition, an anchor in the node with an offset from it across and down.
    private static JsonObject Position(float anchorX, float anchorY, JsonNode x) => new()
    {
        ["anchor"] = new JsonArray(anchorX, anchorY),
        ["x"] = x,
        ["y"] = new JsonObject { ["Px"] = 0f },
    };
}

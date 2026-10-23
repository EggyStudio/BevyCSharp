using System.Globalization;

namespace Bevy;

/// <summary>
/// What the global illumination is worked out from, read from the console, the command line and
/// the editor's console alike.
/// </summary>
/// <remarks>
/// Named as 3DEngine names its own, where <c>gi.state</c> says the cascades of probes and the
/// field they are traced through. Here the field comes first, built by
/// <see cref="Render.SetSceneField"/>, and the lighting beside it, Solari's rays where the program
/// asked for them.
/// </remarks>
internal static class ConsoleFieldCommands
{
    [Command("gi.state", "How the scene is lit, and the scene's distance field global illumination is traced through: its cascades, where each lies, what this frame built and stamped, and the GPU memory it holds")]
    internal static string State()
    {
        var lines = new List<string>
        {
            Render.RayTracingActive ? "Lighting ray traced, its global illumination Solari's" : "Lighting rasterized, no global illumination",
            SceneField.Said(),
        };

        if (SceneField.Plan is { } plan)
        {
            for (var c = 0; c < plan.Cascades; c++)
            {
                lines.Add(plan.BuiltOrigin(c) is { } origin
                    ? string.Create(CultureInfo.InvariantCulture, $"cascade {c}, cells {plan.CellOf(c):0.###} m, its corner at {origin.X:0.##} {origin.Y:0.##} {origin.Z:0.##}")
                    : string.Create(CultureInfo.InvariantCulture, $"cascade {c}, cells {plan.CellOf(c):0.###} m, not built yet"));
            }

            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"this frame built {SceneField.BuiltLastFrame} cascades of a budget of {plan.Budget}, and stamped {plan.Shapes.Count} boxes in {plan.Bricks.Count} bricks"));
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"GPU memory {SceneField.Bytes() / 1048576.0:0.0} MB"));
        }

        return string.Join("\n", lines);
    }

    [Command("field.set", "Builds the scene's distance field in this many cascades, the finest's cells this wide, at most this many built a frame, or turns it off with 0: field.set <cascades> [cell] [budget]")]
    internal static string Set(int cascades, float cell = 0.25f, int budget = 1)
    {
        Render.SetSceneField(cascades, cell, budget);
        return cascades <= 0
            ? "the scene's distance field is off"
            : string.Create(CultureInfo.InvariantCulture, $"building the scene's distance field in {Math.Min(cascades, 8)} cascades of cells {Math.Max(1e-3f, cell):0.###} m, {Math.Max(1, budget)} a frame");
    }
}

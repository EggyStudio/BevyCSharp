using System.Globalization;

namespace Bevy;

/// <summary>
/// What the global illumination is worked out from, read from the console, the command line and
/// the editor's console alike.
/// </summary>
/// <remarks>
/// Named as 3DEngine names its own, where <c>gi.state</c> says the cascades of probes and the
/// field they are traced through, built by <see cref="Render.SetSceneField"/> and
/// <see cref="Render.SetGlobalIllumination"/>, with the lighting beside them, Solari's rays where the
/// program asked for them.
/// </remarks>
internal static class ConsoleFieldCommands
{
    [Command("gi.state", "How the scene is lit, its global illumination's probes, and the scene's distance field they are traced through: its cascades, where each lies, what this frame built and stamped, and the GPU memory it holds")]
    internal static string State()
    {
        var lines = new List<string>
        {
            Render.RayTracingActive ? "Lighting ray traced, its global illumination Solari's" : "Lighting rasterized",
            WorldProbes.Said(),
        };

        // Each cascade of probes, its directions and the interval its rays cover.
        var (texels, intervals) = (WorldProbes.Texels, WorldProbes.Intervals);
        for (var c = 0; c < texels.Count && c < intervals.Count; c++)
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"probes {c}, {texels[c]} by {texels[c]} directions, {WorldProbes.RaysAt(texels[c])} rays each, from {intervals[c].Start:0.##} m to {intervals[c].End:0.##} m"));

        lines.Add(SceneField.Said());

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

    [Command("gi.set", "Traces global illumination through the scene's distance field at a quality, or turns it off: gi.set <off|low|medium|high>")]
    internal static string SetIllumination(string quality)
    {
        if (!Enum.TryParse<GlobalIllumination>(quality.Trim(), ignoreCase: true, out var asked) || !Enum.IsDefined(asked))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{quality.Trim()}' is not off, low, medium or high.");
            return "gi.set <off|low|medium|high>";
        }

        Render.SetGlobalIllumination(asked);
        return asked == GlobalIllumination.Off ? "global illumination is off" : $"tracing global illumination at {asked}";
    }

    [Command("field.set", "Builds the scene's distance field in this many cascades, the finest's cells this wide, at most this many built a frame, with a far cascade reaching this far ahead, or turns it off with 0: field.set <cascades> [cell] [budget] [far]")]
    internal static string Set(int cascades, float cell = 0.25f, int budget = 1, float far = 0f)
    {
        Render.SetSceneField(cascades, cell, budget, far);
        var reaching = far > 0 ? string.Create(CultureInfo.InvariantCulture, $" and a far one reaching {far:0.#} m") : "";
        return cascades <= 0
            ? "the scene's distance field is off"
            : string.Create(CultureInfo.InvariantCulture, $"building the scene's distance field in {Math.Min(cascades, 8)} cascades of cells {Math.Max(1e-3f, cell):0.###} m{reaching}, {Math.Max(1, budget)} a frame");
    }
}

using System.Globalization;
using System.Runtime.CompilerServices;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The size the engine's interface is drawn at, its panels, its console and the overlay's text, in
/// the Diagnostics module's registry on the settings panel's <c>interface</c> page.
/// </summary>
/// <remarks>
/// One number sizes them all, as a share of the screen's pixels an interface pixel spans, and
/// native is the display's own scale factor, which the system sets for a screen whose pixels are
/// small. The text is drawn from faces made at the size it is shown at, so it stays sharp at any
/// scale. A game's own interface in Bevy's nodes is sized by Bevy's <c>UiScale</c> and not by this.
/// </remarks>
internal static class InterfaceSettings
{
    internal static IReadOnlyList<Setting> All { get; } = [.. Page()];

    // Added as the assembly loads, as the engine's other settings are, so a start finds them.
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The settings join the registry the areas' settings are added to from their initializers.")]
    internal static void Register()
    {
        foreach (var setting in All) Registry.Process.Add(setting);
    }

    private const string Native = "native";

    // What the panel offers, the console taking any share from 50% to 400%.
    private static readonly string[] Scales = [Native, "75%", "100%", "125%", "150%", "175%", "200%"];

    // The share set, none for the display's own.
    private static float? _scale;

    /// <summary>
    /// How many of the window's physical pixels an interface pixel spans, the share set or the
    /// display's own scale factor.
    /// </summary>
    internal static float Scale(float display) => _scale ?? display;

    private static IEnumerable<Setting> Page()
    {
        yield return new Setting<string>("interface.scale",
            "The size the panels, the console and the overlay's text are drawn at, native for the display's own scale, or a share of the screen's pixels from 50% to 400%.",
            Native, () => _scale is { } scale ? Percent(scale) : Native, apply: SetScale)
        { Offer = () => Scales };
    }

    private static string Percent(float scale) => (scale * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    private static string? SetScale(string text)
    {
        var trimmed = text.Trim();
        if (string.Equals(trimmed, Native, StringComparison.OrdinalIgnoreCase))
        {
            _scale = null;
            return null;
        }
        if (!float.TryParse(trimmed.TrimEnd('%').Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) || !float.IsFinite(percent))
            return $"interface.scale takes native or a share such as 150%, not {CommandLine.Quote(text)}.";
        if (percent < 50f || percent > 400f)
            return $"interface.scale takes a share from 50% to 400%, not {Percent(percent / 100f)}.";
        _scale = percent / 100f;
        return null;
    }
}

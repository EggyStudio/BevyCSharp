using System.Globalization;
using System.Runtime.CompilerServices;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The first window's picture in the Diagnostics module's registry, on the settings panel's
/// <c>render</c> page, its scale, the filter that brings it to the window, its shape and how it
/// fits, with the cap on the frames a second under them.
/// </summary>
/// <remarks>
/// <para>
/// Each value is kept by its text, <c>67%</c>, <c>4:3</c>, <c>Sharp</c>, never by its place in a
/// list, so a file reads the same on another screen. The panel offers a few of each, and the
/// console takes any the setting takes, a scale from above 0% to 200% and a shape of any two
/// numbers. Every one defaults to native, the window's own picture, so a player who touches none
/// sees what the window shows.
/// </para>
/// <para>
/// A game sets the whole picture at once through <c>Render.Picture</c>, which writes these values
/// as a program does, read by the panel and kept from the file, which holds only what a player set.
/// It alone sets an exact render size, which no setting keeps, and while it holds one the scale
/// stands unused and its row says the game fixes the size, until the game lets the size go.
/// </para>
/// <para>
/// An app being made reads them from the file with the window's page
/// (<see cref="WindowSettings"/>), so its window draws the picture from its first frame, and a run
/// whose window is not shown has the page only reading with the reason, as the window's page has.
/// </para>
/// </remarks>
internal static class PictureSettings
{
    internal static IReadOnlyList<Setting> All { get; } = [.. Page()];

    // Added as the assembly loads, as the window's page is, so a start finds them.
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The settings join the registry the areas' settings are added to from their initializers.")]
    internal static void Register()
    {
        foreach (var setting in All) Registry.Process.Add(setting);
    }

    private const string Native = "native";
    private const string Off = "off";

    // What the panel offers of each, the console taking any value the setting takes.
    private static readonly string[] Scales = ["50%", "67%", "75%", "100%", "150%", "200%"];
    private static readonly string[] Aspects = [Native, "16:9", "16:10", "21:9", "4:3"];
    private static readonly string[] Limits = [Off, "30", "60", "120", "144", "240"];

    // The picture the page holds, and the exact size a game's code holds beside it.
    private static float _scale = 1f;
    private static (float Width, float Height)? _aspect;
    private static PictureFit _fit;
    private static PictureFilter _filter;
    private static (uint Width, uint Height)? _size;

    // The cap on the frames a second, 0 for none.
    private static int _limit;

    /// <summary>The picture the page and a game's code hold.</summary>
    internal static Picture Current => new() { Scale = _scale, Size = _size, Aspect = _aspect, Fit = _fit, Filter = _filter };

    /// <summary>The cap on the frames a second, 0 for none.</summary>
    internal static int FrameLimit => _limit;

    /// <summary>
    /// Gives every value its default before an app's start reads the file, as the window's page
    /// gives its own.
    /// </summary>
    internal static void Reset() => (_scale, _aspect, _fit, _filter, _size, _limit) = (1f, null, PictureFit.Fit, PictureFilter.Linear, null, 0);

    /// <summary>
    /// Sets the whole picture as a game's code sets it, through its values and not a player's, so
    /// the file keeps the player's, and draws it where the window is shown.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The scale is not above 0 and at most 2, a side of the size is 0 or above 16384, a side of
    /// the shape is not a number above 0, or the fit or the filter is none of its choices.
    /// </exception>
    internal static void Set(Picture picture)
    {
        if (!(picture.Scale > 0f && picture.Scale <= 2f))
            throw new ArgumentOutOfRangeException(nameof(picture), picture.Scale, "A picture's scale is above 0 and at most 2.");
        if (picture.Size is { } size && (size.Width is 0 or > 16384 || size.Height is 0 or > 16384))
            throw new ArgumentOutOfRangeException(nameof(picture), size, "A picture's size is 1 to 16384 pixels along each side.");
        if (picture.Aspect is { } shape && !(float.IsFinite(shape.Width) && float.IsFinite(shape.Height) && shape.Width > 0f && shape.Height > 0f))
            throw new ArgumentOutOfRangeException(nameof(picture), shape, "A picture's shape is two numbers above 0.");
        if (!Enum.IsDefined(picture.Fit))
            throw new ArgumentOutOfRangeException(nameof(picture), picture.Fit, "A picture's fit is Fit or Stretch.");
        if (!Enum.IsDefined(picture.Filter))
            throw new ArgumentOutOfRangeException(nameof(picture), picture.Filter, "A picture's filter is Linear, Nearest or Sharp.");

        (_scale, _size, _aspect, _fit, _filter) = (picture.Scale, picture.Size, picture.Aspect, picture.Fit, picture.Filter);
        if (!WindowSettings.Opening && WindowSettings.Unreached() is null) Window.SetPicture(Current);
    }

    private static IEnumerable<Setting> Page()
    {
        // Each carries its place on the page, the scale and the filter first, since the panel lists
        // a page's settings by name where none is given.
        yield return new Setting<string>("render.scale",
            "The size the scene is drawn at as a share of the window's, below 100% softer and quicker and above it finer and slower, up to 200%.",
            "100%", () => Percent(_scale), apply: SetScale)
        { Order = 0, Offer = () => Scales, Lock = () => WindowSettings.Unshown() ?? Fixed() };

        yield return new Setting<PictureFilter>("render.filter",
            "How a picture smaller than the window is brought up to it, filtered and soft, from the nearest picture pixel and sharp, which is even only at a whole ratio, or sharp and even at any ratio, a picture pixel's edge blended in one window pixel at most. A larger picture is brought down smoothly with any.",
            PictureFilter.Linear, () => _filter, apply: filter => Apply(() => _filter = filter))
        { Order = 1, Lock = WindowSettings.Unshown };

        yield return new Setting<string>("render.aspect",
            "The picture's shape as a width to a height, native for the window's own, the picture the largest of the shape inside the window's.",
            Native, () => _aspect is { } shape ? Ratio(shape) : Native, apply: SetAspect)
        { Order = 2, Offer = () => Aspects, Lock = WindowSettings.Unshown };

        yield return new Setting<PictureFit>("render.fit",
            "How a picture of a shape unlike the window's stands in it, its shape kept with black at the sides, or stretched over the whole window.",
            PictureFit.Fit, () => _fit, apply: fit => Apply(() => _fit = fit))
        { Order = 3, Lock = WindowSettings.Unshown };

        yield return new Setting<string>("render.fps_limit",
            "The most frames drawn a second, beside the display's vertical sync, or off for no cap.",
            Off, () => _limit > 0 ? _limit.ToString(CultureInfo.InvariantCulture) : Off, apply: SetLimit)
        { Order = 4, Offer = () => Limits, Lock = WindowSettings.Unshown };
    }

    // Why the scale stands unused, or none where it gives the size.
    private static string? Fixed() =>
        _size is { } size ? $"The game fixes the render size at {size.Width}x{size.Height}, so the scale waits until it lets the size go." : null;

    private static string Percent(float scale) => (scale * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    private static string Ratio((float Width, float Height) shape) =>
        string.Create(CultureInfo.InvariantCulture, $"{shape.Width:0.###}:{shape.Height:0.###}");

    // Sets a value at once where the window is shown and while a start reads the file, or refuses
    // it with the reason where no window is reached.
    private static string? Apply(Action set)
    {
        if (!WindowSettings.Opening && WindowSettings.Unreached() is { } why) return why;
        set();
        if (!WindowSettings.Opening) Window.SetPicture(Current);
        return null;
    }

    private static string? SetScale(string text)
    {
        var number = text.Trim().TrimEnd('%').Trim();
        if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) || !float.IsFinite(percent))
            return $"render.scale takes a share of the window's size such as 67%, not {CommandLine.Quote(text)}.";
        if (percent <= 0f || percent > 200f)
            return $"render.scale takes a share above 0% and up to 200%, not {Percent(percent / 100f)}.";
        return Apply(() => _scale = percent / 100f);
    }

    private static string? SetAspect(string text)
    {
        if (string.Equals(text.Trim(), Native, StringComparison.OrdinalIgnoreCase)) return Apply(() => _aspect = null);
        var sides = text.Split(':', StringSplitOptions.TrimEntries);
        if (sides.Length != 2
            || !float.TryParse(sides[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
            || !float.TryParse(sides[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
            || !(float.IsFinite(width) && float.IsFinite(height) && width > 0f && height > 0f))
            return $"render.aspect takes native or a width to a height above 0 such as 4:3, not {CommandLine.Quote(text)}.";
        return Apply(() => _aspect = (width, height));
    }

    private static string? SetLimit(string text)
    {
        if (string.Equals(text.Trim(), Off, StringComparison.OrdinalIgnoreCase)) return Limit(0);
        if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fps) || fps < 1 || fps > 1000)
            return $"render.fps_limit takes off or a whole number of frames a second from 1 to 1000, not {CommandLine.Quote(text)}.";
        return Limit(fps);
    }

    private static string? Limit(int fps)
    {
        if (!WindowSettings.Opening && WindowSettings.Unreached() is { } why) return why;
        _limit = fps;
        if (!WindowSettings.Opening) Window.SetFrameLimit(fps);
        return null;
    }
}

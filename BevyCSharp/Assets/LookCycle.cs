namespace Bevy;

/// <summary>
/// A look that changes with the sun's height, a timecycle of looks kept at heights of the sun, the
/// look at any height worked out between the two kept about it.
/// </summary>
/// <remarks>
/// <para>
/// Keyed by the sun's height above the horizon rather than by the hour, so the same cycle serves any
/// latitude and season, and the golden hour falls where the sun is low whenever that is. Below the
/// lowest key the lowest look holds and above the highest the highest, and between two the look is
/// <see cref="LookSettings.Lerp"/>'s, the numbers moving evenly with the sun's height.
/// </para>
/// <para>
/// The program reads the cycle as its day turns and gives the camera the look,
/// <c>Render.SetLook(camera, cycle.At(degrees))</c>, which changes the look's numbers alone on a
/// camera that has one, so it costs nothing to do every frame.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var cycle = LookCycle.Day();
/// Render.SetLook(camera, cycle.At(sunDegrees));
/// </code>
/// </example>
public sealed class LookCycle
{
    private readonly SortedList<float, LookSettings> _keys = [];

    /// <summary>The looks kept, by the sun's height in degrees, lowest first.</summary>
    public IReadOnlyList<(float Degrees, LookSettings Look)> Keys => _keys.Select(key => (key.Key, key.Value)).ToList();

    /// <summary>Keeps a look at a height of the sun, replacing one kept there.</summary>
    /// <param name="degrees">How high the sun stands above the horizon, below it under zero.</param>
    /// <param name="look">The look there.</param>
    /// <returns>The cycle, for the next key.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="degrees"/> is not a number.</exception>
    public LookCycle Add(float degrees, LookSettings look)
    {
        ArgumentNullException.ThrowIfNull(look);
        if (!float.IsFinite(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "The sun's height is in degrees.");
        _keys[degrees] = look;
        return this;
    }

    /// <summary>The look at a height of the sun, between the two kept about it.</summary>
    /// <param name="degrees">How high the sun stands above the horizon, below it under zero.</param>
    /// <exception cref="InvalidOperationException">The cycle keeps no look.</exception>
    public LookSettings At(float degrees)
    {
        if (_keys.Count == 0) throw new InvalidOperationException("The cycle keeps no look. Add one with Add.");
        var heights = _keys.Keys;
        if (!(degrees > heights[0])) return LookSettings.Lerp(_keys.Values[0], _keys.Values[0], 0f);
        if (degrees >= heights[^1]) return LookSettings.Lerp(_keys.Values[^1], _keys.Values[^1], 0f);

        var above = 1;
        while (heights[above] < degrees) above++;
        var share = (degrees - heights[above - 1]) / (heights[above] - heights[above - 1]);
        return LookSettings.Lerp(_keys.Values[above - 1], _keys.Values[above], share);
    }

    /// <summary>
    /// A day's cycle on <see cref="LookSettings.Vivid"/>, the vivid look by day, warmer and deeper
    /// as the sun goes down to the horizon, bluer and calmer through the twilight, and cool at
    /// night.
    /// </summary>
    /// <remarks>
    /// Keyed at 30 degrees and above, the vivid look itself; at 8, the late afternoon warming; at 2,
    /// the sunset, its saturation and contrast highest and its tint warmest; at minus 6, the end of
    /// the civil twilight, the blue hour's tint; and at minus 18, the night, its saturation brought
    /// down to none added and its tint cool, since the eye sees little color in the dark. Every key shares the vivid look's curve and
    /// exposure, so the picture moves evenly from one to the next.
    /// </remarks>
    public static LookCycle Day()
    {
        static LookSettings Vivid(float saturation, float contrast, (float R, float G, float B) tint)
        {
            var look = LookSettings.Vivid();
            look.Saturation = saturation;
            look.Contrast = contrast;
            look.Tint = tint;
            return look;
        }

        return new LookCycle()
            .Add(-18f, Vivid(1f, 1.06f, (0.9f, 0.95f, 1.08f)))
            .Add(-6f, Vivid(1.2f, 1.08f, (0.93f, 0.96f, 1.08f)))
            .Add(2f, Vivid(1.55f, 1.16f, (1.08f, 1f, 0.9f)))
            .Add(8f, Vivid(1.48f, 1.14f, (1.04f, 1f, 0.95f)))
            .Add(30f, LookSettings.Vivid());
    }
}

namespace Bevy;

/// <summary>Finds the <see cref="CompassOctant"/> a direction points in.</summary>
public static class CompassOctants
{
    /// <summary>
    /// The octant a direction points in, north being positive Y as on a stick, or null for no
    /// direction at all.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>CompassOctant::from(Dir2)</c>, each octant the forty-five degrees around its own
    /// direction, so the net of the arrows held, right less left and up less down, gives the
    /// diagonal where two are held.
    /// </remarks>
    /// <param name="direction">The direction, of any length but zero.</param>
    public static CompassOctant? Of(Vec2 direction)
    {
        if (direction.X == 0f && direction.Y == 0f) return null;

        // Clockwise from north, in eighths of a turn, rounded to the nearest.
        var turn = MathF.Atan2(direction.X, direction.Y) / (MathF.PI / 4f);
        return (CompassOctant)(((int)MathF.Round(turn) % 8 + 8) % 8);
    }
}

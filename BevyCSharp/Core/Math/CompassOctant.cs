namespace Bevy;

/// <summary>One of eight directions on a screen or a map, counted clockwise from north, as Bevy's <c>CompassOctant</c>.</summary>
/// <remarks>
/// North is up the screen and east to its right. <see cref="Navigation.Move"/> moves the input
/// focus by one, and <see cref="CompassOctants.Of(Vec2)"/> finds the one a stick or a pair of keys
/// points in.
/// </remarks>
public enum CompassOctant
{
    /// <summary>Up.</summary>
    North = 0,

    /// <summary>Up and to the right.</summary>
    NorthEast = 1,

    /// <summary>To the right.</summary>
    East = 2,

    /// <summary>Down and to the right.</summary>
    SouthEast = 3,

    /// <summary>Down.</summary>
    South = 4,

    /// <summary>Down and to the left.</summary>
    SouthWest = 5,

    /// <summary>To the left.</summary>
    West = 6,

    /// <summary>Up and to the left.</summary>
    NorthWest = 7,
}

namespace BevyCSharp.Sample;

/// <summary>Where something is.</summary>
/// <remarks>
/// A plain blittable struct is all a component needs to be. It carries no attribute and no
/// interface; the first time a behavior touches it, its layout is registered with Bevy and it
/// becomes a real component in Bevy's tables.
/// </remarks>
public struct Position
{
    /// <summary>Horizontal position.</summary>
    public float X;

    /// <summary>Vertical position.</summary>
    public float Y;
}

/// <summary>How fast something is moving, in units per second.</summary>
public struct Velocity
{
    /// <summary>Horizontal speed.</summary>
    public float X;

    /// <summary>Vertical speed.</summary>
    public float Y;
}

/// <summary>Marks an entity that should be affected by gravity.</summary>
/// <remarks>
/// A zero-field struct is a tag: it costs nothing to store and is used purely to include or
/// exclude entities with <c>[With]</c> and <c>[Without]</c>.
/// </remarks>
public struct Falls;

/// <summary>Marks an entity that has come to rest.</summary>
public struct Grounded;

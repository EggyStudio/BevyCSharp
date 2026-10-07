namespace Bevy;

/// <summary>
/// Where a component's description came from, which says what kind of component it is.
/// </summary>
/// <remarks>
/// A tool sometimes has to tell a game's own components from the engine's: an icon that marks an
/// entity as scripted, or a file that saves what a project wrote and leaves the engine's own state
/// to the engine. The schema knows which it is, so it says so rather than each tool
/// guessing from a name.
/// </remarks>
public enum SchemaOrigin
{
    /// <summary>A C# component, described by the generator.</summary>
    Declared,

    /// <summary>One of Bevy's components with a mirror on this side, described by hand.</summary>
    Mirrored,

    /// <summary>One of Bevy's components, described from Bevy's own reflection.</summary>
    Reflected,
}

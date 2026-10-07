namespace Bevy;

/// <summary>What loading a scene made, and what it could not.</summary>
/// <param name="Entities">The entities spawned, in the order the file lists them.</param>
/// <param name="Unknown">
/// The component types the file names that this build has no schema for, which were kept as they
/// were for the next save to write back.
/// </param>
/// <param name="Refused">
/// Bevy's components the file holds that Bevy would not take from the JSON written for them, each
/// with Bevy's reason.
/// </param>
public sealed record SceneLoad(
    IReadOnlyList<Entity> Entities, IReadOnlyList<string> Unknown, IReadOnlyList<string> Refused);

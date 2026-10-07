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
/// <param name="Problem">
/// Why the file was not read at all, naming it, where it was missing, empty, cut short or not what
/// its name says, or nothing for a file that was read. Nothing was spawned for one that was not.
/// </param>
/// <remarks>
/// A bad file is answered here rather than thrown, as the asset server answers one with
/// <see cref="AssetLoadFailed"/>, so a game whose save was cut short goes on from where it was
/// rather than ending at the call, as N 2.6 of NORM.md has it.
/// </remarks>
public sealed record SceneLoad(
    IReadOnlyList<Entity> Entities, IReadOnlyList<string> Unknown, IReadOnlyList<string> Refused, string? Problem = null)
{
    /// <summary>
    /// A file that was not read, with why, said on the log as well for a game that does not ask.
    /// </summary>
    internal static SceneLoad Unread(string problem)
    {
        Log.Warn(problem);
        return new SceneLoad([], [], [], problem);
    }
}

namespace Bevy;

/// <summary>
/// The local id an entity had in the scene file it was loaded from, kept so the next save gives it
/// the same one.
/// </summary>
/// <remarks>
/// Kept across saves so that a scene under version control diffs as what changed, rather than as
/// every entity renumbered because one was added near the top. It has no schema, so a tool does not
/// show it and a scene does not write it as a component.
/// </remarks>
public struct SceneId
{
    /// <summary>The id, unique within one file.</summary>
    public int Value;
}

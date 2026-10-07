using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Marks an entity the editor spawned for itself, such as its own cameras, so a scene saved from
/// the editor leaves it out.
/// </summary>
/// <remarks>
/// A marker rather than a list of names the editor skips, so a game's own entity called Ground or
/// Sun is saved like any other.
/// </remarks>
public struct EditorOnly;

/// <summary>
/// The scene the editor is editing, written to a scene file and spawned from one.
/// </summary>
/// <remarks>
/// <para>
/// The editor's document is a scene file in the project's assets, <see cref="EditorPaths.Scene"/>,
/// written by <see cref="SceneFile"/> with everything the editor spawned for itself left out. Loading one
/// takes away the scene that is there first, so the file is the scene rather than edits laid over
/// whatever code made.
/// </para>
/// <para>
/// A project saved before the scene file existed has a <c>world.json</c> of edits by name instead.
/// Loading reads that the old way when there is no scene file yet, and the next save writes the
/// scene file, which is read from then on.
/// </para>
/// </remarks>
public static class EditorScene
{
    /// <summary>Whether an entity is the editor's own rather than part of the scene.</summary>
    public static bool IsEditors(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.Has<EditorOnly>(entity) || EditorEntity.IsInterface(world, entity);
    }

    /// <summary>Writes the scene to a file.</summary>
    /// <returns>How many entities were written.</returns>
    /// <remarks>
    /// Every file the scene refers to is given an id as it is written, since the editor edits the
    /// project, so a model renamed afterward is still found by the scene.
    /// </remarks>
    public static int Save(EcsWorld world, string path)
    {
        ArgumentNullException.ThrowIfNull(world);
        // With the level's cameras on, as the game runs them, rather than as the editor holds them.
        return LevelCameras.Saving(world, () => SceneFile.Save(world, path, entity => InScene(world, entity), giveIds: true));
    }

    /// <summary>Takes away the scene that is there and spawns the one in a file.</summary>
    /// <remarks>
    /// The file is spawned first and the scene that was there taken away after, so one that does
    /// not read leaves the scene being edited as it was, with why in <see
    /// cref="SceneLoad.Problem"/>.
    /// </remarks>
    public static SceneLoad Load(EcsWorld world, string path)
    {
        ArgumentNullException.ThrowIfNull(world);

        var was = world.All().Where(entity => InScene(world, entity)).ToArray();
        var loaded = SceneFile.Load(world, path);
        if (loaded.Problem is not null) return loaded;

        // Children before their parents would be despawned with them, so only the top of each tree
        // is despawned, which takes what is under it along.
        foreach (var entity in was)
        {
            if (world.IsAlive(entity) && !InScene(world, world.ParentOf(entity))) world.Despawn(entity);
        }

        EditorSelection.Clear();
        return loaded;
    }

    /// <summary>
    /// Whether an entity is part of the scene, meaning it carries a component of the project's own
    /// or a mirrored one of Bevy's, such as a transform, and not the editor's.
    /// </summary>
    /// <remarks>
    /// Bevy's reflected components alone do not make an entity part of the scene, because the engine
    /// keeps a good many entities of its own that carry them, and a placed thing has a transform.
    /// </remarks>
    private static bool InScene(EcsWorld world, Entity entity) =>
        !entity.IsNone
        && world.IsAlive(entity)
        && !IsEditors(world, entity)
        && world.ComponentsOf(entity).Any(id => ComponentSchemas.For(id) is { Origin: not SchemaOrigin.Reflected });
}

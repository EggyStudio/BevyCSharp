namespace Bevy;

/// <summary>Building a level file into the running app's world, from the console and the command line.</summary>
/// <remarks>
/// The same <see cref="Levels.Load"/> a game calls, so a level saved from TrenchBroom is tried in a
/// running app with <c>./bcs command level.load levels/room.map</c>, and loaded again after each save
/// once the last load's root is despawned.
/// </remarks>
internal static class ConsoleLevelCommands
{
    /// <summary>Builds a level file's brushes and entities into the world, under a root of its own.</summary>
    [Command("level.load", "Builds a level file of brushes and entities into the world, under a root named for it: level.load <path>")]
    internal static string Load(string path)
    {
        if (!AssetFiles.Exists(path))
        {
            ConsoleHost.Fail("NO_SUCH_FILE", $"There is no level at {path}.");
            return $"there is no level at {path}";
        }

        try
        {
            var ecs = ConsoleHost.Ecs;
            var root = Levels.Load(ecs, path);
            var entities = ecs.ChildrenOf(root);
            return $"built {path} under {root}, {entities.Length} entities";
        }
        catch (FormatException fault)
        {
            ConsoleHost.Fail("BAD_FILE", fault.Message);
            return fault.Message;
        }
    }
}

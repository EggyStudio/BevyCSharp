namespace Bevy;

/// <summary>
/// Saving the world to a scene file and spawning one, from the console and the command line.
/// </summary>
/// <remarks>
/// The same <see cref="SceneFile"/> a game calls, so a scene written from a running app by hand is
/// the scene the game loads.
/// </remarks>
internal static class ConsoleSceneCommands
{
    /// <summary>Writes the world's entities to a scene file.</summary>
    [Command("scene.save", "Writes the world to a scene file: scene.save <path>")]
    internal static string Save(string path)
    {
        var written = SceneFile.Save(ConsoleHost.Ecs, path);
        return $"wrote {written} entities to {path}";
    }

    /// <summary>Spawns everything a scene file holds.</summary>
    [Command("scene.load", "Spawns a scene file: scene.load <path>")]
    internal static string Load(string path)
    {
        if (!File.Exists(SceneFile.Resolve(path)))
        {
            ConsoleHost.Fail("NO_SUCH_FILE", $"There is no scene at {path}.");
            return $"there is no scene at {path}";
        }

        var loaded = SceneFile.Load(ConsoleHost.Ecs, path);
        var kept = loaded.Unknown.Count == 0
            ? string.Empty
            : $", keeping {string.Join(", ", loaded.Unknown)} as written, since this build has no schema for them";

        var refused = loaded.Refused.Count == 0
            ? string.Empty
            : $", and Bevy refused {string.Join("; ", loaded.Refused)}";

        return $"spawned {loaded.Entities.Count} entities from {path}{kept}{refused}";
    }
}

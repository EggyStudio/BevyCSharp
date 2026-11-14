using System.Security.Cryptography;

namespace Bevy;

/// <summary>
/// The scene files a world loaded at its top level while it watches its assets, each with what its
/// load spawned and a digest of the file as this process last read or wrote it.
/// </summary>
/// <remarks>
/// <para>
/// What <see cref="SceneReloads"/> spawns again in place when the file changes on disk. A file that
/// holds the bytes it held when it was read, or when this process saved it, has not changed, so the
/// app's own save spawns nothing again, nor does another hand writing the same bytes over it.
/// </para>
/// <para>
/// A resource of the world, so a load in one app is none of another's, and kept only while the app
/// watches its assets, since nothing reads it otherwise. A file the game's assembly carries is on
/// no disk to change, and is not kept.
/// </para>
/// </remarks>
internal sealed class LoadedScenes
{
    /// <summary>One scene file loaded at the top level.</summary>
    internal sealed class Loaded
    {
        /// <summary>The path as the load named it.</summary>
        public required string Path { get; init; }

        /// <summary>The file on disk it was read from.</summary>
        public required string Full { get; init; }

        /// <summary>What its last load spawned, the file's entities and the scenes it places.</summary>
        public List<Entity> Entities { get; set; } = [];

        /// <summary>A digest of the file's bytes as this process last read or wrote them.</summary>
        public byte[] Digest { get; set; } = [];
    }

    /// <summary>Every scene file kept, in the order they were loaded.</summary>
    internal List<Loaded> All { get; } = [];

    /// <summary>Keeps a scene file loaded at the top level, where the app watches its assets.</summary>
    internal static void Note(EcsWorld world, string path, IReadOnlyList<Entity> spawned)
    {
        if (!DataAssets.Watching || world.Owner is not { } owner) return;

        var full = System.IO.Path.GetFullPath(SceneFile.Resolve(path));
        if (DigestOf(full) is not { } digest) return;

        // A load whose entities have all gone, as a save's load ends the game before it, is a load
        // nothing follows any more.
        var loaded = owner.GetOrInsertResource(new LoadedScenes());
        loaded.All.RemoveAll(each => !each.Entities.Any(world.IsAlive));
        loaded.All.Add(new Loaded { Path = path, Full = full, Entities = [.. spawned], Digest = digest });
    }

    /// <summary>Takes the bytes this process wrote to a file as what each load of it was read from.</summary>
    internal static void Saved(EcsWorld world, string full)
    {
        if (world.Owner?.TryResource<LoadedScenes>() is not { } loaded) return;

        foreach (var each in loaded.All)
        {
            if (each.Full == full && DigestOf(full) is { } digest) each.Digest = digest;
        }
    }

    /// <summary>A digest of a file's bytes, or nothing where it cannot be read.</summary>
    internal static byte[]? DigestOf(string full)
    {
        try
        {
            return SHA256.HashData(File.ReadAllBytes(full));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

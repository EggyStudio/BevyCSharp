using System.Collections.Concurrent;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Spawns a placed scene file again when it is written while the level runs, under the entity that
/// placed it, with the level's overrides applied again, and a scene file loaded at the top level
/// again in place.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="DataAssets.Watching"/> on, as the editor has it, a scene file under the asset
/// root changed on disk is read again at the top of the next frame for every instance placing it.
/// The entities its old copy spawned are despawned, which gives back what they held, the meshes,
/// the materials and the values their components kept, and the file is read under the same root,
/// so whatever holds the root keeps it and the root's own place and name stay.
/// </para>
/// <para>
/// What the level placing it added under the old copy's nodes is kept and put back under the new
/// node at the same path, and the level's overrides are applied again as on loading, a node missing
/// from the file keeping its override as missed. The instance is posted as
/// <see cref="WorldInstanceReady"/> again, so code waiting for it hears of the new copy. A file
/// that does not read, as one caught halfway through being written, leaves the old copy where it
/// is until it is written whole.
/// </para>
/// <para>
/// A scene file loaded at the top level (<see cref="LoadedScenes"/>) is spawned again where its
/// bytes differ from what this process last read or wrote. What its load spawned is despawned and
/// its entities are read again, the world's resources left as the game has them, and what the
/// program spawned itself stays, one it put under an entity of the scene going back under the
/// entity of the same id in the file. A save laid over the scene is not laid again, since the save
/// is the game's to load. Each file spawned again is said in the log and posted as
/// <see cref="AssetReloaded"/> of the kind <c>SceneFile</c>, its path under the asset root.
/// </para>
/// </remarks>
internal static class SceneReloads
{
    private static readonly ConcurrentQueue<string> Touched = new();
    private static FileSystemWatcher? _watcher;
    private static string? _watched;

    /// <summary>
    /// Spawns again every placed scene file that changed on disk since the last frame. Called by the
    /// app each frame, before the instances ready are posted.
    /// </summary>
    internal static void ReloadTouched(EcsWorld world, MessageBus bus)
    {
        Watch();
        if (Touched.IsEmpty) return;

        var changed = new HashSet<string>(StringComparer.Ordinal);
        while (Touched.TryDequeue(out var full)) changed.Add(Path.GetFullPath(full));

        foreach (var file in changed)
        {
            // A file caught halfway through being written reads at its next change, the whole one.
            if (!Reads(file)) continue;

            var relative = Path.GetRelativePath(AssetIds.Root, file).Replace('\\', '/');
            var placings = 0;
            foreach (var root in world.EntitiesWith<SceneInstance>())
            {
                if (SceneInstances.DataOf(world, root) is not { } data || !SceneInstances.IsSubscene(data.Path)) continue;
                if (Path.GetFullPath(SceneFile.Resolve(data.Path)) != file) continue;

                Respawn(world, bus, root, data);
                placings++;
            }

            if (placings > 0)
            {
                Log.Info($"Spawned {relative} again under {(placings == 1 ? "the entity placing it" : $"the {placings} entities placing it")} after its file changed.");
                bus.Send(new AssetReloaded(relative, "SceneFile"));
            }

            Top(world, bus, file, relative);
        }
    }

    /// <summary>Spawns again in place each load at the top level of a file whose bytes changed.</summary>
    private static void Top(EcsWorld world, MessageBus bus, string file, string relative)
    {
        if (world.Owner?.TryResource<LoadedScenes>() is not { } loaded) return;

        foreach (var scene in loaded.All)
        {
            if (scene.Full != file || LoadedScenes.DigestOf(file) is not { } digest || digest.AsSpan().SequenceEqual(scene.Digest)) continue;

            // Taken as read either way, so a change is spawned or said once.
            scene.Digest = digest;
            if (SceneFile.KeptWhenChanged is { } told)
            {
                told(scene.Path);
                continue;
            }

            var (count, kept) = RespawnTop(world, scene);
            var entities = count == 1 ? "one entity" : $"{count} entities";
            Log.Info(kept == 0
                ? $"Spawned {relative} again in place after its file changed, {entities}."
                : $"Spawned {relative} again in place after its file changed, {entities}, with {kept} of the program's own put back under them.");
            bus.Send(new AssetReloaded(relative, "SceneFile"));
        }
    }

    /// <summary>
    /// Despawns what a load at the top level spawned and reads its file again, and reports how many
    /// entities the file spawned and how many of the program's own went back under them.
    /// </summary>
    /// <remarks>
    /// What the program put under an entity of the scene is parked at the top level while the old
    /// copy goes, so it is not despawned with it, and goes back under the new entity holding the
    /// same <see cref="SceneId"/>, or stays at the top level where the file has none. What a placed
    /// scene's root holds is the placed scene's, spawned again with it.
    /// </remarks>
    private static (int Count, int Kept) RespawnTop(EcsWorld world, LoadedScenes.Loaded scene)
    {
        var ours = scene.Entities.Where(world.IsAlive).ToHashSet();
        var parked = new List<(Entity Entity, int? Under)>();
        foreach (var entity in ours)
        {
            if (SceneInstances.DataOf(world, entity) is not null) continue;

            foreach (var child in world.ChildrenOf(entity))
            {
                if (!ours.Contains(child)) parked.Add((child, world.TryGet<SceneId>(entity, out var id) ? id.Value : null));
            }
        }

        foreach (var (entity, _) in parked) world.ClearParent(entity);
        foreach (var entity in ours)
        {
            if (world.IsAlive(entity)) world.Despawn(entity);
        }

        var load = SceneFile.Respawn(world, scene.Path);
        scene.Entities = [.. load.Entities];

        var byId = new Dictionary<int, Entity>();
        foreach (var entity in load.Entities)
        {
            if (world.TryGet<SceneId>(entity, out var id)) byId[id.Value] = entity;
        }

        var kept = 0;
        foreach (var (entity, under) in parked)
        {
            if (!world.IsAlive(entity) || under is not { } id || !byId.TryGetValue(id, out var parent)) continue;

            world.SetParent(entity, parent);
            kept++;
        }

        return (load.Entities.Count, kept);
    }

    /// <summary>Despawns an instance's old copy and reads its file under the same root again.</summary>
    /// <remarks>
    /// What the level added under the old nodes is parked outside the root while the new copy is
    /// read and its model taken, so it is not taken for part of the file, and goes back under the
    /// node at the path it was under, or under the root where the file has no such node. One the
    /// level's file records is put back by its override as the overrides are applied, at the same
    /// node.
    /// </remarks>
    private static void Respawn(EcsWorld world, MessageBus bus, Entity root, SceneInstances.Data data)
    {
        var parked = new List<(Entity Entity, string? Under)>();
        foreach (var node in data.Model)
        {
            if (!world.IsAlive(node)) continue;

            foreach (var child in world.ChildrenOf(node))
            {
                if (!data.Model.Contains(child)) parked.Add((child, SceneInstances.PathOf(world, root, node)));
            }
        }

        foreach (var (entity, _) in parked) world.ClearParent(entity);

        foreach (var child in world.ChildrenOf(root))
        {
            if (data.Model.Contains(child)) world.Despawn(child);
        }

        data.Model.Clear();
        data.Originals.Clear();
        data.Tagged = false;

        // A file saved half way through is said on the log by the load, and the instance holds
        // nothing until it is saved whole and read again.
        SceneFile.Load(world, data.Path, root);

        SceneInstances.Apply(world, root);

        foreach (var (entity, under) in parked)
        {
            if (!world.IsAlive(entity) || !world.ParentOf(entity).IsNone) continue;

            var node = under is null ? Entity.None : SceneInstances.Find(world, root, under);
            world.SetParent(entity, node.IsNone ? root : node);
        }

        bus.Send(new WorldInstanceReady(root, Reloaded: true));
    }

    /// <summary>Whether a file is there and reads as JSON, so the old copy is despawned only for one that will spawn.</summary>
    private static bool Reads(string file)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            return true;
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Starts, moves or stops the watcher so it matches <see cref="DataAssets.Watching"/> and the root.</summary>
    private static void Watch()
    {
        var root = AssetIds.Root;
        var wanted = DataAssets.Watching && Directory.Exists(root) ? root : null;
        if (wanted == _watched) return;

        _watcher?.Dispose();
        _watcher = null;
        _watched = wanted;
        if (wanted is null) return;

        _watcher = new FileSystemWatcher(wanted, "*.scene.json")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.Created += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.Renamed += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.EnableRaisingEvents = true;
    }
}

namespace Bevy;

/// <summary>
/// The members the scenes give <see cref="App"/>, spawning a glTF file's scene once it has loaded,
/// which a game calls as the app's own.
/// </summary>
/// <remarks>
/// They extend <see cref="App"/> rather than being a part of it, since the asset server loads the
/// file and the scenes say when its scene is in the world, and the app stands in Core, which every
/// area names.
/// </remarks>
public static class AppScenes
{
    extension(App app)
    {
        /// <summary>
        /// Spawns a glTF file's scene once it has loaded, as Bevy's <c>SceneRoot</c> of a glTF does, and
        /// hands the root to <paramref name="spawned"/> once the scene is in the world under it.
        /// </summary>
        /// <param name="path">The file, under the asset root.</param>
        /// <param name="spawned">What to do with the scene's root, or none.</param>
        /// <param name="scene">Which of the file's scenes, by its place in the file.</param>
        /// <remarks>
        /// Bevy spawns the root at once and its scene under it when the file has loaded, a frame or more
        /// later. The root is handed over with <see cref="WorldInstanceReady"/>, once its meshes,
        /// lights and players are among its <see cref="EcsWorld.Descendants"/>, so
        /// <paramref name="spawned"/> can reach into the scene at once. A file that never loads hands
        /// nothing over.
        /// </remarks>
        public App SpawnGltf(string path, Action<BehaviorContext, Entity>? spawned = null, int scene = 0)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            var handle = AssetHandle.None;
            var root = Entity.None;
            var done = false;

            return app.Update(ctx =>
            {
                if (done) return;
                if (handle == AssetHandle.None) handle = AssetServer.LoadGltfScene(path, scene);

                if (root == Entity.None)
                {
                    if (AssetServer.StateOf(handle) == AssetLoadState.Loaded) root = ctx.Ecs.SpawnScene(handle);
                    return;
                }

                foreach (var ready in ctx.Read<WorldInstanceReady>())
                {
                    if (ready.Entity != root) continue;
                    done = true;
                    spawned?.Invoke(ctx, root);
                    return;
                }
            }, $"SpawnGltf({path})");
        }
    }
}

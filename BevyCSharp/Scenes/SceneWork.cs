namespace Bevy;

/// <summary>The scenes' part in the app and its frame, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class SceneWork : IPlugin
{
    public void Build(App app)
    {
        // A game in progress is one app's, and its entities would name others in the next, and so
        // are the scenes placed.
        SaveGame.Forget();
        SceneInstances.Forget();

        // A game loaded said, and a placed scene file written since the last frame spawned again
        // before the instances ready are posted, as one of them, after the scenes Bevy spawned last
        // frame are in the world, so an instance's overrides find their nodes, and before anything
        // reads that the instance is ready.
        app.AreasWork.Add(4100, world => SaveGame.PostLoaded(world.Resource<MessageBus>()));
        app.AreasWork.Add(4300, world => SceneReloads.ReloadTouched(world.Resource<EcsWorld>(), world.Resource<MessageBus>()));
        app.AreasWork.Add(4400, world => SceneInstances.PostReady(world.Resource<EcsWorld>(), world.Resource<MessageBus>()));
    }
}

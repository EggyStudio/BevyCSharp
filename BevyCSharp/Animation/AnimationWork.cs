namespace Bevy;

/// <summary>The animation's part in the app and its frame, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class AnimationWork : IPlugin
{
    public void Build(App app)
    {
        // The state machines playing belong to the app whose entities they play on.
        Animation.ForgetMachines();

        // An observer of an event placed on clips asks Bevy for the clips' reports.
        ObserverRegistry.AddWatcher(ClipEvents.Watch);

        // The clips that finished last frame said, and the state machines carried on.
        app.AreasWork.Add(1000, world =>
        {
            if (!App.HasRenderer) return;
            Animation.PostFinished(world.Resource<MessageBus>());
            Animation.AdvanceMachines(world.Resource<EcsWorld>());
        });
    }
}

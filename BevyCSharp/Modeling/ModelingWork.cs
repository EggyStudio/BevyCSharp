namespace Bevy;

/// <summary>The modeling's part in the app's frame, which <c>EnginePlugin</c> adds to every app.</summary>
internal sealed class ModelingWork : IPlugin
{
    public void Build(App app)
    {
        // The models the console shows drawn again where its commands changed them, and the
        // modeling windows' outlines in a run that draws.
        app.AreasWork.Add(1200, world =>
        {
            if (App.HasRenderer) Modeling.Tick(world.Resource<EcsWorld>(), !world.Resource<Config>().Headless);
        });
    }
}

namespace Bevy;

/// <summary>The interface's part in the app's frame, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class UiWork : IPlugin
{
    public void Build(App app)
    {
        // F1's hidden scope, in a run that draws and has a window to show it.
        app.AreasWork.Add(2900, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) Ui.TickHidden(world.Resource<EcsWorld>(), world.Resource<Input>());
        });
    }
}

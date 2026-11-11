namespace Bevy;

/// <summary>The diagnostics' part in the app's frame, which <c>EnginePlugin</c> adds to every app.</summary>
internal sealed class DiagnosticsWork : IPlugin
{
    public void Build(App app)
    {
        // F3's debug overlay, in a run that draws and has a window to show it, once the scene's
        // field it says is built.
        app.AreasWork.Add(3000, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) Debug.TickOverlay(world);
        });
    }
}

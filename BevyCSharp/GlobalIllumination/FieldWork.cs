namespace Bevy;

/// <summary>The scene's distance field's and global illumination's part in the app's frame, which <c>EnginePlugin</c> adds to every app.</summary>
internal sealed class FieldWork : IPlugin
{
    public void Build(App app)
    {
        // In a run that draws and has a window to show it, the scene's distance field first, which
        // the debug overlay says, then the probes global illumination traces through it, and once
        // the weather has given the sky its numbers, the engine's own drawing of the field.
        app.AreasWork.Add(2000, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) SceneField.Tick(world);
        });
        app.AreasWork.Add(2100, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) WorldProbes.Tick(world);
        });
        app.AreasWork.Add(2800, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) SceneField.DrawEngine(world);
        });
    }
}

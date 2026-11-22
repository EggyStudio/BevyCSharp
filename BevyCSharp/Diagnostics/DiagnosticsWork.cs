namespace Bevy;

/// <summary>
/// The diagnostics' part in the app's frame, which <c>EnginePlugin</c> adds to every app.
/// </summary>
internal sealed class DiagnosticsWork : IPlugin
{
    public void Build(App app)
    {
        Debug.FreshKeys();

        // Escape is the console's, the settings panel's or a field's while one is open, and the
        // program's with none of them open, in every app, a headless one with a panel opened by a
        // call included.
        Input.InterfaceTakesEscape = Debug.TakesEscape;

        // Every key, button and stick is the console's or the settings panel's while one is open,
        // and the program reads none of them, the overlay, which only shows, holding nothing.
        Input.InterfaceHoldsInput = () => Debug.Keys.Panels.HoldsInput;

        // The player's settings file, read as the app starts in a run that shows a window, and
        // written from then on.
        app.AddSystem(Stage.Startup, new SystemDescriptor(KeptSettings.Open, "Engine.Settings"));

        // The keys F1 to F5, in a run that draws and has a window to read them from, before the
        // hidden scope F1 turns over is kept (2900).
        app.AreasWork.Add(2800, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) Debug.TickKeys(world);
        });

        // F3's debug overlay, in a run that draws and has a window to show it, once the scene's
        // field it says is built.
        app.AreasWork.Add(3000, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) Debug.TickOverlay(world);
        });

        // The files read again, kept for live.status from the reloads each frame posts.
        app.World.InsertResource(new LiveReloads());
        app.AreasWork.Add(3100, world => world.Resource<LiveReloads>().Read(world));
    }
}

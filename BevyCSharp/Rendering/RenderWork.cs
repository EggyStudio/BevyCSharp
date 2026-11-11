namespace Bevy;

/// <summary>The renderer's part in the app and its frame, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class RenderWork : IPlugin
{
    public void Build(App app)
    {
        // Meshes and materials belong to the app that made them, what is kept of a mesh goes with
        // its handle, and the wireframes are the app's to ask for.
        Render.ForgetMade();
        MaterialFiles.Forget();
        MeshFiles.Forget();
        AssetServer.AddForgetter(Render.Forget);
        Render.Wireframes = app.Config.Wireframes;

        // A recording's picture of this frame asked for, and those of frames before kept as they
        // come back off the GPU.
        app.AreasWork.Add(1100, world =>
        {
            if (App.HasRenderer) Recorder.Tick(world.Resource<Time>());
        });

        // A picture of this frame where the screenshot key was pressed, the press left for the
        // program to read as well, in a run that draws.
        app.AreasWork.Add(1300, world =>
        {
            if (App.HasRenderer && Capture.ScreenshotKey is { } shot && world.Resource<Input>().KeyPressed(shot) && !world.Resource<Config>().Headless)
                Capture.Screenshot();
        });

        // The material files changed on disk since the frame before, after the data files are.
        app.AreasWork.Add(4200, _ => MaterialFiles.ReloadTouched());
    }
}

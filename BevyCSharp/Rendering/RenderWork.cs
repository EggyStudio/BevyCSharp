namespace Bevy;

/// <summary>
/// The renderer's part in the app and its frame, which <c>EnginePlugin</c> adds to every app.
/// </summary>
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

        // The material files changed on disk since the frame before, after the data files are.
        app.AreasWork.Add(4200, world => MaterialFiles.ReloadTouched(world.Resource<MessageBus>()));
    }
}

namespace Bevy;

/// <summary>ImGui's part in the app, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class ImGuiWork : IPlugin
{
    public void Build(App app)
    {
        // The textures and the frame ImGui drew belong to the app that drew them.
        ImGuiRuntime.Forget();
    }
}

namespace Bevy;

/// <summary>ImGui's part in the app, which <c>EnginePlugin</c> adds to every app.</summary>
internal sealed class ImGuiWork : IPlugin
{
    public void Build(App app)
    {
        // The textures and the frame ImGui drew belong to the app that drew them.
        ImGuiRuntime.Forget();

        // A pointer put in as a hand would is put into the interface's queue too, while it runs.
        SyntheticInput.InterfaceRunning = () => ImGuiRuntime.IsRunning;
        SyntheticInput.ToInterface = SyntheticInputImGui.Send;
    }
}

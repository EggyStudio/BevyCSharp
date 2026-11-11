namespace Bevy;

/// <summary>The audio's part in the app, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class AudioWork : IPlugin
{
    public void Build(App app)
    {
        // Sounds and buses belong to the app that played them.
        Audio.ResetMixer();
    }
}

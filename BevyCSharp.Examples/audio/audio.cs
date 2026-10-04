using Bevy;

namespace BevyCSharp.Examples.Sound;

// Shows how to load and play an audio file, a piece of music played once in a window that draws
// nothing.
internal static class AudioExample
{
    public static void Build(App app) =>
        app.Startup(_ => Audio.Play(AssetServer.Load(AssetKind.Audio, "sounds/Windless Slopes.ogg")), "audio.Setup");
}

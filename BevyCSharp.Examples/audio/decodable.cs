// Bevy's decodable example, examples/audio/decodable.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// A sine wave at 440 hertz, the A above middle C, made sample by sample as it plays, at a fifth of
// the volume. Bevy's makes each sample in a decoder of its own on the audio thread. Here the game
// writes them ahead into a stream each frame, a tenth of a second's worth kept waiting, stepping
// through each period as Bevy's decoder does.
internal static class DecodableExample
{
    private const int SampleRate = 44_100;
    private const float Frequency = 440f;

    private static AssetHandle _stream;
    private static float _progress;
    private static readonly float[] Samples = new float[SampleRate / 10];

    public static void Build(App app)
    {
        app.Startup(Setup, "decodable.Setup");
        app.Update(KeepWritten, "decodable.KeepWritten");
    }

    // Bevy's setup, and its AudioPlugin's global volume of a fifth.
    private static void Setup(BehaviorContext ctx)
    {
        Audio.SetGlobalVolume(0.2f);
        _stream = Audio.CreateSampleStream(SampleRate);
        Audio.Play(_stream);
    }

    // Bevy's SineDecoder, how far along one period the wave is moving on each sample, the period
    // started again at one, so a tenth of a second waits to be played.
    private static void KeepWritten(BehaviorContext ctx)
    {
        var wanted = Samples.Length - Audio.QueuedSamples(_stream);
        if (wanted <= 0) return;

        for (var i = 0; i < wanted; i++)
        {
            _progress = (_progress + Frequency / SampleRate) % 1f;
            Samples[i] = MathF.Sin(MathF.Tau * _progress);
        }

        Audio.QueueSamples(_stream, Samples.AsSpan(0, wanted));
    }
}

namespace Bevy;

/// <summary>
/// A clap of thunder made as it plays, a crack and a rumble from noise, shaped by how far off its
/// strike was, for <see cref="SkyThunder"/> to write into a stream of samples.
/// </summary>
/// <remarks>
/// <para>
/// Thunder is the sound of the whole channel heating at once, heard from its nearest part first and
/// its farthest last, so it rolls on for seconds, swelling where a stretch of the channel lies at the
/// same distance. Here that is noise shaped by a few rolls, each swelling fast and dying slowly at a
/// time of its own, through a low-pass filter whose cutoff falls as the clap goes on. A near strike
/// opens with a crack, noise through a high cutoff dying in a twentieth of a second, and its rumble
/// starts bright. A far one has no crack, a rumble whose cutoff starts low, as the air takes away the
/// high sound first, and lasts longer, the channel's farthest part being farther behind its nearest.
/// </para>
/// <para>
/// Low-passed noise is quieter the lower its cutoff, so the rumble is lifted by the square root of
/// how much of the band the cutoff lets through, and the sum is softly clipped, so a loud clap rounds
/// off rather than tearing.
/// </para>
/// </remarks>
internal sealed class ThunderSound
{
    internal const int SampleRate = 44_100;

    private readonly float _gain;
    private readonly float _crack;
    private readonly float _bright;
    private readonly (float At, float Strength, float Decay)[] _rolls;
    private uint _noise;
    private float _low, _lower, _crackLow;
    private long _written;

    /// <summary>A clap from a strike so far off, as loud as <paramref name="volume"/> at 150 meters.</summary>
    internal ThunderSound(float distance, float volume, uint seed)
    {
        var near = Math.Clamp(1f - ((distance - 150f) / 1850f), 0f, 1f);
        Duration = Math.Clamp(2.5f + (distance / 900f), 2.5f, 9f);
        _crack = near * near;
        _bright = 250f + (2_750f * near * near);
        _gain = volume * Math.Clamp(MathF.Pow(150f / Math.Max(distance, 150f), 0.5f), 0.12f, 1f);
        _noise = (seed * 747796405u) + 2891336453u;
        if (_noise == 0) _noise = 1;

        var random = new Random((int)(seed % int.MaxValue));
        var count = 3 + random.Next(4);
        _rolls = new (float, float, float)[count];
        for (var i = 0; i < count; i++)
        {
            // The first roll at once, the rest spread over the first two thirds of the clap.
            var at = i == 0 ? 0.02f : (0.05f + (random.NextSingle() * 0.6f)) * Duration;
            _rolls[i] = (at, 0.4f + (0.6f * random.NextSingle()), 0.4f + random.NextSingle() + (Duration * 0.08f));
        }
    }

    /// <summary>How long the clap lasts, in seconds.</summary>
    internal float Duration { get; }

    /// <summary>Whether every sample of the clap has been made.</summary>
    internal bool Done => _written >= (long)(Duration * SampleRate);

    /// <summary>Makes the next samples into <paramref name="into"/>, as many as fit or are left, and answers how many.</summary>
    internal int Fill(Span<float> into)
    {
        var left = (long)(Duration * SampleRate) - _written;
        var count = (int)Math.Min(into.Length, Math.Max(left, 0));
        for (var i = 0; i < count; i++)
        {
            var t = (float)_written / SampleRate;
            _written++;

            // The rumble's cutoff falling from bright to a low growl as the clap goes on.
            var through = t / Duration;
            var cutoff = _bright + ((110f - _bright) * Smooth(0f, 0.8f, through));
            var a = 1f - MathF.Exp(-2f * MathF.PI * cutoff / SampleRate);
            var white = Noise();
            _low += a * (white - _low);
            _lower += a * (_low - _lower);
            var rumble = _lower / MathF.Sqrt(cutoff / (SampleRate * 0.5f));

            var swell = 0f;
            foreach (var (at, strength, decay) in _rolls)
            {
                var since = t - at;
                if (since > 0f) swell += strength * Smooth(0f, 0.07f, since) * MathF.Exp(-since / decay);
            }

            // The crack, bright noise dying in a twentieth of a second, for a near strike alone.
            _crackLow += 0.45f * (white - _crackLow);
            var crack = _crack * _crackLow * MathF.Exp(-t / 0.05f) * 1.6f;

            var fade = 1f - Smooth(0.85f, 1f, through);
            into[i] = MathF.Tanh(_gain * ((0.35f * rumble * swell) + crack) * fade);
        }
        return count;
    }

    // White noise from -1 to 1, xorshift's.
    private float Noise()
    {
        _noise ^= _noise << 13;
        _noise ^= _noise >> 17;
        _noise ^= _noise << 5;
        return (_noise / (float)uint.MaxValue * 2f) - 1f;
    }

    private static float Smooth(float from, float to, float x)
    {
        var t = Math.Clamp((x - from) / (to - from), 0f, 1f);
        return t * t * (3f - (2f * t));
    }
}

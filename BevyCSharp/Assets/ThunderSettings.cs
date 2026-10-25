namespace Bevy;

/// <summary>
/// The thunderstorm <see cref="Render.SetThunder"/> brings about a camera, lightning striking at a
/// rate, lighting the scene and the clouds, its bolt drawn, and its thunder heard after the time
/// sound takes to come.
/// </summary>
/// <remarks>
/// <para>
/// Strikes come at random, <see cref="Rate"/> a minute on average and none at a set beat, between
/// <see cref="Nearest"/> and <see cref="Farthest"/> from the camera, more of them far than near, as
/// many as fall on each ring of ground. Two in three come down to the ground as a branching bolt from
/// the clouds' base, and the rest stay in the cloud, lighting it from within. Each flickers over half
/// a second or so, as the strokes of a real strike come down the same channel one after another, and
/// while it does, a light at the bolt lights the scene and the fog, and the clouds glow about it.
/// </para>
/// <para>
/// The thunder is made as it plays, a crack and a rumble from noise, sharper and louder from a near
/// strike and lower and longer from a far one, as the air takes away the high sound first, and is
/// heard once sound at 343 meters a second has come from the strike. <see cref="Struck"/> is told
/// of each strike as it happens, with the delay, for a game's own shaking, damage or sounds.
/// </para>
/// </remarks>
public sealed class ThunderSettings
{
    /// <summary>How many strikes a minute, on average, 0 unless set.</summary>
    public float Rate { get; set; }

    /// <summary>The nearest a strike lands to the camera, in meters, 150 unless set.</summary>
    public float Nearest { get; set; } = 150f;

    /// <summary>The farthest a strike lands from the camera, in meters, 4,000 unless set.</summary>
    public float Farthest { get; set; } = 4_000f;

    /// <summary>How bright a strike's light is, in lumens at its brightest, 4 billion unless set.</summary>
    public float Brightness { get; set; } = 4e9f;

    /// <summary>Whether a strike lights the scene with a light at its bolt, on unless set.</summary>
    public bool Light { get; set; } = true;

    /// <summary>Whether a strike to the ground draws its bolt, on unless set.</summary>
    public bool Bolts { get; set; } = true;

    /// <summary>Whether a strike's thunder is heard, on unless set.</summary>
    public bool Sound { get; set; } = true;

    /// <summary>How loud the thunder of a strike at <see cref="Nearest"/> is, 1 unless set.</summary>
    public float Volume { get; set; } = 1f;

    /// <summary>The audio bus the thunder plays on, or none for the master volume alone. See <see cref="Audio.SetBusVolume"/>.</summary>
    public string? Bus { get; set; }

    /// <summary>The seed the strikes' times, places and shapes are made from, 0 unless set.</summary>
    public uint Seed { get; set; }

    /// <summary>Told of each strike as it happens, or none.</summary>
    public Action<LightningStrike>? Struck { get; set; }
}

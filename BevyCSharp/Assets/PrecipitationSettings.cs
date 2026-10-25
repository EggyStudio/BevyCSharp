namespace Bevy;

/// <summary>
/// The rain and the snow <see cref="Render.SetPrecipitation"/> lets fall about a camera, drops and
/// flakes moved on the GPU, kept out from under roofs and splashing where the rain lands.
/// </summary>
/// <remarks>
/// <para>
/// The drops fall in two boxes about the camera, a near one a quarter as wide holding two in three
/// and a far one <see cref="Reach"/> to each side holding the rest, so the air close by holds the
/// most. A box is held in the world rather than carried with the camera, so walking moves through the
/// rain, and a drop that leaves one side comes in at the other. A drop of rain falls as fast as one
/// its size does in still air, from some 2 meters a second for drizzle to 9 for a downpour's largest,
/// and is drawn as the streak it makes over a camera's shutter. A flake falls at about a meter a
/// second and flutters as it goes. Both drift on <see cref="Wind"/>.
/// </para>
/// <para>
/// With <see cref="Roofs"/>, a camera looking down draws the depth of what stands over the ground
/// about the camera, 96 meters across, into a map of heights, again only once the camera has moved
/// some 12 meters from where the map was drawn. A drop ends where it falls below that height, under a
/// roof, a tree or the ground, so the rain keeps out from under them, and a drop of rain that lands
/// close by splashes there. Whatever moves is drawn into the map where it stood, so a game puts what
/// moves on a layer of its own and leaves it out of <see cref="RoofLayers"/>.
/// </para>
/// </remarks>
public sealed class PrecipitationSettings
{
    /// <summary>How hard it rains, from none at 0 to a downpour at 1, 0 unless set.</summary>
    public float Rain { get; set; }

    /// <summary>How hard it snows, from none at 0 to a blizzard at 1, 0 unless set.</summary>
    public float Snow { get; set; }

    /// <summary>How fast and which way the wind blows the drops and the flakes, in meters a second, still air unless set.</summary>
    public Vec3 Wind { get; set; }

    /// <summary>How many fall at the heaviest, <see cref="PrecipitationQuality.Medium"/> unless set.</summary>
    public PrecipitationQuality Quality { get; set; } = PrecipitationQuality.Medium;

    /// <summary>How far from the camera to each side the far drops fall, in meters, 36 unless set.</summary>
    public float Reach { get; set; } = 36f;

    /// <summary>Whether a map of what stands over the ground keeps the rain out from under it, on unless set.</summary>
    public bool Roofs { get; set; } = true;

    /// <summary>
    /// The render layers the map of what stands over the ground draws, as a bit a layer, the default
    /// layer at 0 unless set. See <see cref="CameraSettings.Layers"/>.
    /// </summary>
    public uint RoofLayers { get; set; }

    /// <summary>Whether the rain splashes where it lands near the camera, on unless set.</summary>
    public bool Splashes { get; set; } = true;
}

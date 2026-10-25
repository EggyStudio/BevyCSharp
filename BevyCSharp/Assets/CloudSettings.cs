namespace Bevy;

/// <summary>
/// The clouds <see cref="Render.SetClouds"/> draws in a camera's sky, a layer of them over the
/// planet lit by the sun through the atmosphere and by the sky.
/// </summary>
/// <remarks>
/// <para>
/// A weather map says where the sky has cloud and what kind, flat heaps of fair weather or towers,
/// and <see cref="Coverage"/> how much of it does. Each cloud's body is cut from tiling noise
/// <see cref="ShapeSize"/> across and its edges worn into wisps by finer noise
/// <see cref="DetailSize"/> across, and the wind carries all of it.
/// </para>
/// <para>
/// The sun's light reaches a cloud through the atmosphere, red at a low sun, so a sunset's clouds
/// glow from below, and a cloud's edge against the sun silvers. A far cloud fades into the sky's
/// color along the way, and the sun's disc does not show through a cloud in front of it.
/// </para>
/// </remarks>
public sealed class CloudSettings
{
    /// <summary>How finely they are marched, <see cref="CloudQuality.Medium"/> unless set.</summary>
    public CloudQuality Quality { get; set; } = CloudQuality.Medium;

    /// <summary>How much of the sky has cloud, from none at 0 to overcast at 1, 0.45 unless set.</summary>
    public float Coverage { get; set; } = 0.45f;

    /// <summary>The height of the layer's base above the ground, in meters, 1,500 unless set.</summary>
    public float Bottom { get; set; } = 1500f;

    /// <summary>The height of the layer's top, in meters, 4,500 unless set.</summary>
    public float Top { get; set; } = 4500f;

    /// <summary>How much light a meter of the densest cloud takes away, 0.04 unless set, higher darker and harder edged.</summary>
    public float Density { get; set; } = 0.04f;

    /// <summary>How large the noise a cloud's body is cut from is, in meters, 6,000 unless set, a cloud some quarter of it across.</summary>
    public float ShapeSize { get; set; } = 6_000f;

    /// <summary>How large the noise that wears a cloud's edges is, in meters, 800 unless set.</summary>
    public float DetailSize { get; set; } = 800f;

    /// <summary>How far the detail wears the edges, from none at 0, 1 unless set.</summary>
    public float Erosion { get; set; } = 1f;

    /// <summary>How large the weather map is, in meters, 24,000 unless set, a patch of cloud some sixth of it across.</summary>
    public float WeatherSize { get; set; } = 24_000f;

    /// <summary>How fast and which way the wind carries the clouds, in meters a second, a light wind unless set.</summary>
    public Vec3 Wind { get; set; } = new(10f, 0f, 4f);

    /// <summary>How far along a way the clouds are marched at most, in meters, 40,000 unless set, past which they fade out.</summary>
    public float Distance { get; set; } = 40_000f;

    /// <summary>How strongly the sky's own light lights them, 0.6 unless set.</summary>
    public float Ambient { get; set; } = 0.6f;

    /// <summary>The seed the noise is made from, so two games' skies differ, 0 unless set.</summary>
    public uint Seed { get; set; }

    /// <summary>
    /// The directional light the clouds shadow, the sun, or none to cast no shadow, none unless set.
    /// </summary>
    /// <remarks>
    /// The clouds' shadow is laid over the light as its picture across a square
    /// <see cref="ShadowSize"/> wide about the camera, repeating past it, and the light's global
    /// transform is given that square's scale and middle each frame after Bevy works it out, its turn
    /// left the program's. A directional light lights by its turn alone, so its light and its shadow
    /// maps are as they were. The sky's fog in froxels darkens in the shadow too.
    /// </remarks>
    public Entity ShadowLight { get; set; } = Entity.None;

    /// <summary>How wide the square the shadow is worked out over is, in meters, 16,000 unless set.</summary>
    public float ShadowSize { get; set; } = 16_000f;

    /// <summary>How much of the sun's light a thick cloud's shadow takes away, from none at 0 to all at 1, 1 unless set.</summary>
    public float ShadowStrength { get; set; } = 1f;
}

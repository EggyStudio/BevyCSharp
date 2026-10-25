namespace Bevy;

/// <summary>
/// The fog near the camera worked out in froxels, cells of the view's frustum, where shadows cut it
/// into shafts, lamps glow in it and it drifts in lumps on the wind. See
/// <see cref="SkyFogSettings.Froxels"/>.
/// </summary>
/// <remarks>
/// <para>
/// The froxels hold the same layer <see cref="SkyFogSettings"/> describes, lit by the same sky, and
/// past <see cref="Distance"/> the layer goes on worked out in closed form, so the two meet with no
/// seam and the far ground still fades into the horizon. Inside, the sky's light reaches every point
/// and the sun's share of it reaches only points the sun's shadow does not cover, so a wall's shadow
/// is a darker shaft through the fog and the light between two buildings a bright one.
/// </para>
/// <para>
/// Each froxel's point moves through it from frame to frame and its light is blended with what it
/// held last frame where the point stood in last frame's view, so the slices blur into a smooth fog
/// over a few frames, and something moving fast leaves a short trail in it.
/// </para>
/// </remarks>
public sealed class FroxelFogSettings
{
    /// <summary>How many froxels, <see cref="FroxelQuality.Medium"/> unless set.</summary>
    public FroxelQuality Quality { get; set; } = FroxelQuality.Medium;

    /// <summary>How far from the eye the froxels reach, in world units, 250 unless set, past which the layer is worked out in closed form.</summary>
    public float Distance { get; set; } = 250f;

    /// <summary>How far the lumps move the fog's density from the layer's, from none at 0 to doubling and thinning it out at 1, 0.5 unless set.</summary>
    public float Lumps { get; set; } = 0.5f;

    /// <summary>How large a lump is, in world units, 30 unless set.</summary>
    public float LumpSize { get; set; } = 30f;

    /// <summary>How fast and which way the wind carries the lumps, in world units a second, a light breeze unless set.</summary>
    public Vec3 Wind { get; set; } = new(1.5f, 0f, 0.8f);

    /// <summary>How many point and spot lights scatter in the fog at most, 16 unless set, none at 0.</summary>
    public int Lights { get; set; } = 16;

    /// <summary>The boxes the fog is thicker inside, eight at most, none unless set.</summary>
    public IReadOnlyList<FogBox> Boxes { get; set; } = [];
}

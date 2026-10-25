namespace Bevy;

/// <summary>
/// The outline of a tree's crown, the eight of Weber and Penn's paper of 1995, which set how long a
/// tree's first branches are by how high up the trunk they leave it.
/// </summary>
/// <remarks>
/// Each is a share of the crown's widest reach at each height, from the crown's foot, where
/// <see cref="TreeSpecies.BaseSize"/> puts it, to the top. The branches grow inside it, from the
/// attractors <see cref="Trees.Grow"/> scatters through it, so the outline is the crown's as seen
/// from afar, and its edge is as ragged as the branches that reach it.
/// </remarks>
public enum TreeShape
{
    /// <summary>Widest at its foot and narrowing to a fifth of that at the top, as a spruce's.</summary>
    Conical,

    /// <summary>Widest halfway up, rounding to a fifth of that at its foot and its top.</summary>
    Spherical,

    /// <summary>Widest at its foot, rounding over to a fifth of that at the top.</summary>
    Hemispherical,

    /// <summary>As wide all the way up.</summary>
    Cylindrical,

    /// <summary>Wide at its foot, narrowing to half that at the top.</summary>
    TaperedCylindrical,

    /// <summary>Widest three tenths of the way up, narrowing to nothing at its foot and its top.</summary>
    Flame,

    /// <summary>A fifth of its width at its foot, widening to the top.</summary>
    InverseConical,

    /// <summary>A <see cref="Flame"/> that keeps half its width at both ends, as a poplar's or an aspen's.</summary>
    TendFlame,
}

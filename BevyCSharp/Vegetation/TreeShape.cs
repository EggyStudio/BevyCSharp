namespace Bevy;

/// <summary>
/// The outline of a tree's crown, the eight of Weber and Penn's paper of 1995 and its pruning
/// envelope, which set how long a tree's first branches are by how high up the trunk they leave it.
/// </summary>
/// <remarks>
/// Each is a share of the crown's widest reach at each height, from the crown's foot, where
/// <see cref="TreeSpecies.BaseSize"/> puts it, to the top. The first branches are as long as the
/// share at the height they leave the trunk, and the paths they take as the paper bends them make
/// the outline <see cref="Trees.Grow(TreeSpecies, uint)"/> scatters its attractors through, so a crown's outline
/// follows from its shape and its branches' angles together, as the paper's does.
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

    /// <summary>
    /// The pruning envelope the species' <see cref="TreeSpecies.PruneWidth"/>, peak and powers draw,
    /// the paper's custom shape, as a weeping willow's.
    /// </summary>
    Envelope,
}

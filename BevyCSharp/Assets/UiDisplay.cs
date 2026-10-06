namespace Bevy;

/// <summary>
/// Whether a node lays out at all, and by which model.
/// </summary>
public enum UiDisplay
{
    /// <summary>Flexbox, which every other layout field here describes.</summary>
    Flex = 0,

    /// <summary>Children stacked as blocks, each on its own line.</summary>
    Block = 1,

    /// <summary>
    /// Not laid out, drawn or measured, and neither are its children.
    /// </summary>
    /// <remarks>
    /// How a screen is put away and brought back without despawning it. Different from
    /// <see cref="Visibility.Hidden"/>, which stops the node drawing but keeps the space it
    /// occupies, so its siblings do not move.
    /// </remarks>
    None = 2,

    /// <summary>
    /// A grid, whose rows and columns are stated up front by <see cref="UiGrid.Set"/>.
    /// </summary>
    /// <remarks>
    /// Setting this alone gives a grid of one column, since the tracks are lists and arrive
    /// through their own call. <see cref="UiGrid.Set"/> sets this as well, so a node laid out by
    /// it needs nothing said here.
    /// </remarks>
    Grid = 3,
}

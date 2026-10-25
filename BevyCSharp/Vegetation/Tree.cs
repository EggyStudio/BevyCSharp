namespace Bevy;

/// <summary>
/// Makes an entity a tree, a species by its registered name and a seed, which the library grows and
/// dresses with its stems, its leaves and its colliders under the entity.
/// </summary>
/// <remarks>
/// <para>
/// A component, so a level or a scene places a tree by its species and seed alone, and an editor
/// shows the two fields. The library grows the tree on a worker the first time its species' variant
/// is asked for, one of <see cref="Trees.Variants"/> a species grows, which every tree of that
/// variant shares, and dresses the entity once it has, under a child turned and sized by the seed,
/// so the entity's own transform stays where the level put it. A tree whose species or seed changes
/// is dressed again, and one whose species is not registered is left bare with a warning in the log.
/// </para>
/// <para>
/// The names are those <see cref="Trees.Register"/> gives, the presets' among them, as
/// <c>quaking-aspen</c>, so a level keeps a name and never a species' numbers.
/// </para>
/// </remarks>
[Behavior]
public partial struct Tree
{
    /// <summary>The species' registered name, as <c>quaking-aspen</c>.</summary>
    [Tooltip("A species' registered name, quaking-aspen, black-tupelo, weeping-willow, california-black-oak, bush, fern or one a game registers.")]
    public EcsText Species;

    /// <summary>The seed, which picks the variant and turns and sizes the tree.</summary>
    public uint Seed;
}

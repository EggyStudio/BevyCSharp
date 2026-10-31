namespace Bevy;

/// <summary>What a stretch of grass is planted with and how thick it stands (<see cref="Grass.Plant"/>).</summary>
/// <remarks>
/// The same settings and the same stretch give the same grass, every clump where it stood and as
/// tall, since the seed scatters, turns and sizes them.
/// </remarks>
public sealed record GrassSettings
{
    /// <summary>
    /// The set of the texture collection the cards are cut from, one of the kind
    /// <see cref="TreeTextureKind.Grass"/>, <c>wheat-grass</c> unless given.
    /// </summary>
    public string Set { get; init; } = "wheat-grass";

    /// <summary>How many clumps stand on a square meter, each two cards crossed.</summary>
    public float Density { get; init; } = 3f;

    /// <summary>How tall a clump stands on average, in meters, its card as wide as the set's picture is for that height.</summary>
    public float Height { get; init; } = 0.6f;

    /// <summary>How far a clump's height strays from the average either way, as a share of it.</summary>
    public float Variation { get; init; } = 0.3f;

    /// <summary>The seed the clumps are scattered, turned, sized and swayed by.</summary>
    public uint Seed { get; init; }

    /// <summary>
    /// The side of the square patches the grass is drawn in, in meters, each a mesh of its own that
    /// the camera culls as one.
    /// </summary>
    public float PatchSize { get; init; } = 8f;

    /// <summary>
    /// Whether the blades cast shadows, which many small cut-out cards cost more than they show and
    /// whose thin blades flicker in the sun's shadow map as they sway. They receive shadows either way.
    /// </summary>
    public bool CastsShadows { get; init; }
}

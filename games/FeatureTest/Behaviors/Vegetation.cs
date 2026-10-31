using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The vegetation south of the hub, a meadow of grass swaying in the wind, a ring of trees grown
/// from a seed round it and a forest behind it.
/// </summary>
/// <remarks>
/// <para>
/// The meadow's grass is the engine's (<see cref="Grass.Plant"/>), clumps of crossed cards cut from
/// the texture collection's wheat grass and drawn with the trees' leaf material, so its blades take
/// the light as the leaves do and let the light behind them through, scattered, turned and sized
/// from a seed, so the meadow is the same every run. It sways in the wind the trees sway in, the
/// engine's weather's where it runs over the map.
/// </para>
/// <para>
/// The trees are planted by species and seed with <see cref="Trees.Plant"/>, quaking aspens and
/// California black oaks in a ring round the meadow, bushes and a fern in it, and a forest of black
/// tupelos and aspens behind it from <see cref="Trees.PlantForest"/>, each grown on a worker as the
/// map starts and dressed once its variant has grown, its trunk and thick limbs capsules the player
/// stops at. The guide's page on trees names this forest as where to see them.
/// </para>
/// <para>
/// The blades cast no shadows, which thousands of small ones would cost more than they show, and
/// the trees do.
/// </para>
/// </remarks>
[Behavior]
public partial struct Vegetation
{
    private const float Ground = Scene.GroundHeight;

    /// <summary>The meadow's middle, and how far it runs each way from it.</summary>
    private static readonly Vec3 Middle = new(0f, Ground, 73f);

    private const float Half = 15f;

    /// <summary>Sows the meadow and plants the trees.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var ecs = ctx.Ecs;

        // Soil under the meadow, so the blades stand on earth rather than the level's floor.
        Blocks.Static(ecs, "Meadow", Middle + new Vec3(0f, 0.02f, 0f), new Vec3((Half * 2f) + 2f, 0.04f, (Half * 2f) + 2f), Render.CreateMaterial(0.16f, 0.17f, 0.07f, roughness: 1f));

        // Three clumps a square meter, 0.6 m tall a third either way, as tall as the meadow's blades
        // stood when each was an entity of its own.
        Grass.Plant(ecs, Middle + new Vec3(0f, 0.04f, 0f), new Vec2(Half * 2f, Half * 2f), new GrassSettings { Density = 3f, Height = 0.6f, Seed = 70 });
        Plant(ecs);

        // A board at the meadow's edge, facing the road from the hub.
        Blocks.Static(ecs, "Vegetation board", new Vec3(0f, Ground + 1.9f, 56.5f), new Vec3(6.6f, 0.8f, 0.1f), Render.CreateMaterial(0.9f, 0.86f, 0.75f, roughness: 0.7f));
        var post = Render.CreateMaterial(0.45f, 0.32f, 0.2f, roughness: 0.85f);
        Blocks.Static(ecs, "Vegetation board, left post", new Vec3(-3f, Ground + 0.75f, 56.5f), new Vec3(0.15f, 1.5f, 0.15f), post);
        Blocks.Static(ecs, "Vegetation board, right post", new Vec3(3f, Ground + 0.75f, 56.5f), new Vec3(0.15f, 1.5f, 0.15f), post);
    }

    /// <summary>Writes on the board, every frame, as gizmos do.</summary>
    [OnUpdate]
    public static void Label(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        // Turned half round, since text unturned faces along Z and the board faces back to the hub.
        Gizmos.Text(
            "Grass in a wind, trees grown from a seed",
            new Vec3(0f, Ground + 1.9f, 56.44f),
            Quat.FromRotationY(MathF.PI),
            0.2f,
            (0f, 0f),
            (0.12f, 0.08f, 0.05f, 1f),
            inFront: false);
    }

    /// <summary>
    /// Aspens and oaks by turns in a ring round the meadow, two bushes and a fern in it, and a forest
    /// of tupelos and aspens behind it, to the south.
    /// </summary>
    private static void Plant(EcsWorld ecs)
    {
        for (var k = 0; k < 10; k++)
        {
            var angle = (k + 0.5f) / 10f * MathF.Tau;
            var spot = Middle + new Vec3(MathF.Sin(angle) * (Half + 2.5f), 0f, MathF.Cos(angle) * (Half + 2.5f));
            Trees.Plant(ecs, k % 2 == 0 ? "quaking-aspen" : "california-black-oak", (uint)(70 + k), spot);
        }

        Trees.Plant(ecs, "bush", 81, Middle + new Vec3(-6f, 0f, 5f));
        Trees.Plant(ecs, "bush", 82, Middle + new Vec3(7f, 0f, -3f));
        Trees.Plant(ecs, "fern", 83, Middle + new Vec3(2f, 0f, 11f));

        // The tupelos to the west and the aspens to the east, each a forest of its own.
        var forest = Middle + new Vec3(0f, 0f, 33f);
        Trees.PlantForest(ecs, "black-tupelo", 84, forest + new Vec3(-11f, 0f, 0f), 10f, 6);
        Trees.PlantForest(ecs, "quaking-aspen", 85, forest + new Vec3(11f, 0f, 0f), 10f, 9);
    }
}

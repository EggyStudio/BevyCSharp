using System.Numerics;
using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The vegetation south of the hub, a meadow of grass bent by a wind a Slang shader blows, a ring of
/// trees grown from a seed round it and a forest behind it.
/// </summary>
/// <remarks>
/// <para>
/// Every blade is an entity of its own with one mesh and one material, which Bevy draws as one
/// instanced batch, each placed, turned and sized at random from a seed, so the meadow is the same
/// every run. The wind is the vertex shader's (<c>shaders/wind.slang</c>), which bends each blade
/// by how high its vertices stand and takes the gusts' phase from where the blade stands, so the
/// meadow ripples rather than swaying as one. Where the engine's weather runs over the map, the
/// wind's way and strength are its.
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

    /// <summary>How many blades the meadow holds.</summary>
    private const int Blades = 14000;

    /// <summary>The meadow's middle, and how far it runs each way from it.</summary>
    private static readonly Vec3 Middle = new(0f, Ground, 73f);

    private const float Half = 15f;

    /// <summary>Which way the wind blows, across the ground, where no weather says.</summary>
    private static readonly Vector2 Wind = Vector2.Normalize(new Vector2(0.8f, 0.6f));

    // The material the wind is given to, whether it is made, and the wind it was last given.
    private static ShaderMaterial _grass;
    private static bool _sown;
    private static Vector2 _blowing;

    /// <summary>Sows the meadow and plants the trees.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        _sown = false;
        _blowing = Wind;
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var ecs = ctx.Ecs;
        var program = Shaders.CreateProgram(new ShaderProgramSettings
        {
            Vertex = "shaders/wind.slang",
            Fragment = "shaders/wind.slang",
            PrepassVertex = new ShaderStage("shaders/wind.slang", "prepass_vertex"),
        });

        // Soil under the meadow, so the blades stand on earth rather than the hub's checker.
        Blocks.Static(ecs, "Meadow", Middle + new Vec3(0f, 0.02f, 0f), new Vec3((Half * 2f) + 2f, 0.04f, (Half * 2f) + 2f), Render.CreateMaterial(0.16f, 0.17f, 0.07f, roughness: 1f));

        Meadow(ecs, program);
        Plant(ecs);
        _sown = true;

        // A board at the meadow's edge, facing the road from the hub.
        Blocks.Static(ecs, "Vegetation board", new Vec3(0f, Ground + 1.9f, 56.5f), new Vec3(6.6f, 0.8f, 0.1f), Render.CreateMaterial(0.9f, 0.86f, 0.75f, roughness: 0.7f));
        var post = Render.CreateMaterial(0.45f, 0.32f, 0.2f, roughness: 0.85f);
        Blocks.Static(ecs, "Vegetation board, left post", new Vec3(-3f, Ground + 0.75f, 56.5f), new Vec3(0.15f, 1.5f, 0.15f), post);
        Blocks.Static(ecs, "Vegetation board, right post", new Vec3(3f, Ground + 0.75f, 56.5f), new Vec3(0.15f, 1.5f, 0.15f), post);
    }

    /// <summary>
    /// Turns the meadow's wind to the map's weather's, its way and how hard it blows, where the
    /// engine's weather runs.
    /// </summary>
    /// <remarks>
    /// The conditions' wind is given rather than the gusting one, and given again only once it has
    /// moved by more than a little, since every value a material is given costs it a new bind group,
    /// and the shader's own waves are the gusts.
    /// </remarks>
    [OnUpdate]
    public static void Blow(BehaviorContext ctx)
    {
        if (!_sown) return;

        var blowing = Wind;
        if (Applied.MapWeatherOn)
        {
            // Coming from the north, at 0, it blows south, along positive Z. A stronger wind bends
            // further, by the square root of its speed against the 4.5 meters a second of the
            // partly cloudy day the meadow was tuned in.
            var now = Applied.MapWeather.Current;
            var from = now.WindFrom * MathF.PI / 180f;
            blowing = new Vector2(-MathF.Sin(from), MathF.Cos(from)) * MathF.Sqrt(Math.Clamp(now.WindSpeed / 4.5f, 0.05f, 6f));
        }

        if (Vector2.Distance(blowing, _blowing) < 0.02f) return;
        _blowing = blowing;
        _grass.Set("wind", blowing);
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

    /// <summary>The blades, one entity each, sharing a mesh and a material.</summary>
    private static void Meadow(EcsWorld ecs, ShaderProgram program)
    {
        var grass = _grass = Shaders.CreateMaterial(new ShaderMaterialSettings { Program = program, Cull = CullMode.None })
            .Set("sway", 0.22f)
            .Set("root", 0f)
            .Set("reach", 0.6f)
            .Set("speed", 1.8f)
            .Set("wind", Wind)
            .Set("lean", 0.7f)
            .Set("low", new Vector4(0.06f, 0.2f, 0.04f, 1f))
            .Set("high", new Vector4(0.42f, 0.7f, 0.16f, 1f));

        var blade = Render.CreateMesh(Blade(0.6f, 0.09f));
        var random = new Random(70);
        for (var i = 0; i < Blades; i++)
        {
            var at = Middle + new Vec3((random.NextSingle() * 2f - 1f) * Half, 0.04f, (random.NextSingle() * 2f - 1f) * Half);
            var tall = 0.6f + (random.NextSingle() * 0.9f);
            var turn = Quat.FromRotationY(random.NextSingle() * MathF.Tau);

            var entity = ecs.Spawn();
            Render.SetMesh(ecs, entity, blade);
            Render.SetMaterial(ecs, entity, grass);
            ecs.Add(entity, new Transform(at, turn, new Vec3(1f, tall, 1f)));
            Render.SetMeshFlags(ecs, entity, MeshFlags.NoShadowCasting);
        }
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

    /// <summary>
    /// A blade of grass, a strip tapering from a width at its root to a point at its top, curving
    /// forward a little as it rises, its texture coordinates running up it.
    /// </summary>
    private static MeshData Blade(float height, float width)
    {
        Vec3[] positions =
        [
            new(-width / 2f, 0f, 0f), new(width / 2f, 0f, 0f),
            new(-width / 3f, height / 3f, 0.01f), new(width / 3f, height / 3f, 0.01f),
            new(-width / 6f, height * 2f / 3f, 0.04f), new(width / 6f, height * 2f / 3f, 0.04f),
            new(0f, height, 0.09f),
        ];

        return new MeshData
        {
            Positions = positions,
            Normals = [.. positions.Select(_ => Vec3.UnitZ)],
            Uvs = [.. positions.SelectMany(p => new[] { (p.X / width) + 0.5f, p.Y / height })],
            Indices = [0, 1, 3, 0, 3, 2, 2, 3, 5, 2, 5, 4, 4, 5, 6],
        };
    }
}

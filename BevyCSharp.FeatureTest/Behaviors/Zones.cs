using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>A place on the map, where a signpost points and a teleport goes.</summary>
/// <param name="Name">What it is called.</param>
/// <param name="Center">The middle of it.</param>
/// <param name="Eye">Where the view is put on arriving.</param>
public readonly record struct Zone(string Name, Vec3 Center, Vec3 Eye);

/// <summary>
/// The map's zones round the hub, with a signpost at the hub's edge pointing to each, and the
/// teleports the panel offers.
/// </summary>
/// <remarks>
/// <para>
/// The hub is the middle of the map, where the program starts, and each zone lies along a road out
/// of it, the course to the east, the render gallery to the west, the lights to the north, the
/// vegetation to the south and the scenes to the north-east. A zone is filled by the batch that
/// writes it, so a signpost may point at an empty field for a while, its name saying what the
/// field will hold.
/// </para>
/// <para>
/// Each signpost is a post and a board turned to face the hub, with the zone's name written on the
/// board in the gizmos' stroke font every frame, so the names are in a picture taken offscreen with
/// no interface, as the workflow's captures are.
/// </para>
/// </remarks>
[Behavior]
public partial struct Zones
{
    /// <summary>How far from the middle of the hub the signposts stand.</summary>
    private const float SignRadius = 11f;

    /// <summary>Every zone, the hub first.</summary>
    public static IReadOnlyList<Zone> All { get; } =
    [
        new("Hub", Vec3.Zero, new Vec3(3.5f, 3f, 6f)),
        new("Course", new Vec3(70f, 0f, 0f), new Vec3(52f, 4f, 0f)),
        new("Render gallery", new Vec3(-70f, 0f, 0f), new Vec3(-52f, 4f, 0f)),
        new("Lights", new Vec3(0f, 0f, -70f), new Vec3(0f, 4f, -52f)),
        new("Vegetation", new Vec3(0f, 0f, 70f), new Vec3(0f, 4f, 52f)),
        new("Scenes", new Vec3(60f, 0f, -60f), new Vec3(46f, 4f, -46f)),
    ];

    /// <summary>Puts the view at a zone, looking at its middle.</summary>
    public static void Go(BehaviorContext ctx, Zone zone)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (Scene.Camera is not { } camera || !ctx.Ecs.IsAlive(camera)) return;

        var look = zone.Center + new Vec3(0f, 1f, 0f);
        ctx.Ecs.Set(camera, Transform.LookingAt(zone.Eye, look, Vec3.UnitY));
        ctx.Ecs.Set(camera, FlyCamera.LookingAt(zone.Eye, look));
        Console.WriteLine($"[Zones] at {zone.Name}");
    }

    /// <summary>Where a zone's signpost stands, at the hub's edge on the road to it.</summary>
    internal static Vec3 SignOf(Zone zone)
    {
        var flat = new Vec3(zone.Center.X, 0f, zone.Center.Z).Normalized;
        return flat * SignRadius;
    }

    /// <summary>Puts up a signpost for each zone round the hub.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var wood = Render.CreateMaterial(0.45f, 0.32f, 0.2f, roughness: 0.85f);
        var board = Render.CreateMaterial(0.9f, 0.86f, 0.75f, roughness: 0.7f);
        var post = Render.CreateMesh(MeshShape.Cuboid, 0.15f, 2.4f, 0.15f);
        var face = Render.CreateMesh(MeshShape.Cuboid, 3.6f, 0.7f, 0.08f);

        foreach (var zone in All.Skip(1))
        {
            var at = SignOf(zone);
            var facing = Quat.FromAxisAngle(Vec3.UnitY, MathF.Atan2(-at.X, -at.Z));

            var stick = ctx.Ecs.Spawn();
            Render.SetMesh(ctx.Ecs, stick, post);
            Render.SetMaterial(ctx.Ecs, stick, wood);
            ctx.Ecs.Add(stick, new Transform(at + new Vec3(0f, Scene.GroundHeight + 1.2f, 0f), facing, Vec3.One));
            ctx.Ecs.SetName(stick, $"Signpost to {zone.Name}");

            var sign = ctx.Ecs.Spawn();
            Render.SetMesh(ctx.Ecs, sign, face);
            Render.SetMaterial(ctx.Ecs, sign, board);
            ctx.Ecs.Add(sign, new Transform(new Vec3(0f, 0.85f, 0.1f), Quat.Identity, Vec3.One));
            ctx.Ecs.SetParent(sign, stick);
        }
    }

    /// <summary>Writes each zone's name on its board, facing the hub.</summary>
    [OnUpdate]
    public static void Label(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        foreach (var zone in All.Skip(1))
        {
            var at = SignOf(zone);
            var facing = Quat.FromAxisAngle(Vec3.UnitY, MathF.Atan2(-at.X, -at.Z));
            var front = facing * new Vec3(0f, 0f, 0.15f);
            Gizmos.Text(zone.Name, at + front + new Vec3(0f, Scene.GroundHeight + 2.05f, 0f), facing, 0.2f, (0.5f, 0.5f), (0.12f, 0.08f, 0.05f, 1f), inFront: false);
        }
    }
}

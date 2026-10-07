using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>A place on the map, where a signpost points and a teleport goes.</summary>
/// <param name="Name">What it is called.</param>
/// <param name="Center">The middle of it.</param>
/// <param name="Eye">Where the free camera is put on arriving.</param>
/// <param name="Start">
/// Where the player's feet are put on arriving, and after a fall within it.
/// </param>
/// <param name="Reach">
/// How far from its middle it runs, within which a fall puts the player at its start.
/// </param>
public readonly record struct Zone(string Name, Vec3 Center, Vec3 Eye, Vec3 Start, float Reach);

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
        new("Hub", Vec3.Zero, new Vec3(3.5f, 3f, 6f), new Vec3(0f, Scene.GroundHeight, 6f), 15f),
        new("Course", new Vec3(70f, 0f, 0f), new Vec3(48f, 4f, 0f), new Vec3(46f, Scene.GroundHeight, 0f), 30f),
        new("Render gallery", new Vec3(-70f, 0f, 0f), new Vec3(-52f, 4f, 0f), new Vec3(-50f, Scene.GroundHeight, 0f), 25f),
        new("Lights", new Vec3(0f, 0f, -70f), new Vec3(0f, 4f, -52f), new Vec3(0f, Scene.GroundHeight, -50f), 25f),
        new("Vegetation", new Vec3(0f, 0f, 70f), new Vec3(0f, 4f, 52f), new Vec3(0f, Scene.GroundHeight, 50f), 25f),
        new("Scenes", new Vec3(60f, 0f, -60f), new Vec3(46f, 4f, -46f), new Vec3(45f, Scene.GroundHeight, -45f), 25f),
    ];

    /// <summary>
    /// Puts the player at a zone's start facing its middle, or in spectator mode the free camera
    /// at its eye looking at its middle.
    /// </summary>
    public static void Go(BehaviorContext ctx, Zone zone)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        Console.WriteLine($"[Zones] at {zone.Name}");

        var look = zone.Center + new Vec3(0f, 1f, 0f);
        if (Player.Mode != PlayerMode.Spectator && ctx.Ecs.IsAlive(Player.Entity))
        {
            Player.Put(ctx, zone.Start, $"teleported to {zone.Name}");
            Player.Face(ctx, look);
            return;
        }

        if (Scene.Camera is not { } camera || !ctx.Ecs.IsAlive(camera)) return;

        ctx.Ecs.Set(camera, Transform.LookingAt(zone.Eye, look, Vec3.UnitY));
        ctx.Ecs.Set(camera, FlyCamera.LookingAt(zone.Eye, look));
    }

    /// <summary>Teleports from the console, as the panel's teleport page does.</summary>
    [Command("teleport", "Puts the player, or in spectator mode the camera, at a zone: teleport <hub|course|render gallery|lights|vegetation|scenes>")]
    internal static string Teleport(string name)
    {
        var zone = All.FirstOrDefault(zone => zone.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (zone.Name is null)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"There is no zone called {name.Trim()}. The zones are {string.Join(", ", All.Select(z => z.Name.ToLowerInvariant()))}.");
            return "teleport <zone>";
        }

        if (ConsoleHost.World is not { } world) return "there is no world to teleport in";

        Go(new BehaviorContext(world), zone);
        return $"at {zone.Name}";
    }

    /// <summary>
    /// Puts the spectator camera at a point looking at another, for a picture of one thing in a
    /// zone, as the drive script takes them.
    /// </summary>
    /// <remarks>
    /// Refused outside spectator mode, where the camera is the player's and would be put back at
    /// its eye on the next frame.
    /// </remarks>
    [Command("look", "Puts the spectator camera at a point looking at another: look <x> <y> <z> <toward x> <toward y> <toward z>")]
    internal static string Look(float x, float y, float z, float towardX, float towardY, float towardZ)
    {
        const string Usage = "look <x> <y> <z> <toward x> <toward y> <toward z>";
        if (Player.Mode != PlayerMode.Spectator)
        {
            ConsoleHost.Fail("NOT_SPECTATING", "The camera follows the player outside spectator mode. Run mode spectator first.");
            return Usage;
        }

        var ecs = ConsoleHost.Ecs;
        if (Scene.Camera is not { } camera || !ecs.IsAlive(camera)) return "there is no camera to move";

        var (eye, look) = (new Vec3(x, y, z), new Vec3(towardX, towardY, towardZ));
        if ((look - eye).Length < 1e-3f)
        {
            ConsoleHost.Fail("BAD_ARGUMENTS", "The camera cannot look at the point it stands on.");
            return Usage;
        }

        ecs.Set(camera, Transform.LookingAt(eye, look, Vec3.UnitY));
        ecs.Set(camera, FlyCamera.LookingAt(eye, look));
        return $"looking from ({x:0.##}, {y:0.##}, {z:0.##}) toward ({towardX:0.##}, {towardY:0.##}, {towardZ:0.##})";
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
            Gizmos.Text(zone.Name, at + front + new Vec3(0f, Scene.GroundHeight + 2.05f, 0f), facing, 0.2f, (0f, 0f), (0.12f, 0.08f, 0.05f, 1f), inFront: false);
        }
    }
}

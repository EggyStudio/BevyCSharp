using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>What a stretch of the course does to the player standing on it.</summary>
public enum Surface
{
    /// <summary>Nothing more than ground does.</summary>
    Plain,

    /// <summary>Carries it along the belt.</summary>
    Conveyor,

    /// <summary>Lets it keep its speed and gives it little grip to change it.</summary>
    Ice,

    /// <summary>Throws it high in the air.</summary>
    Bounce,
}

/// <summary>
/// The course east of the hub, where the player is tried on everything a character meets.
/// </summary>
/// <remarks>
/// <para>
/// Three rows of stations in the course's court east of its start. The north row climbs, with ramps
/// at 15, 30, 45 and 60 degrees, the last too steep for the character's 46 to stand on, stairs with
/// steps of 0.15, 0.25 and 0.4, the last over its step height of 0.35, a beam a third of a unit
/// wide, and a tunnel too low to stand in, crawled through crouched. The middle row tries the
/// ground underfoot, with ice, a bounce pad beside a ledge too high to jump to, and a pit, whose
/// floor is a sensor that puts the player back at the start. The south row moves, with a platform
/// going to and fro between two ledges, an elevator up to a third, a turning disc, a conveyor belt,
/// and five gaps a little wider each time.
/// </para>
/// <para>
/// What stands still, the ramps, the stairs, the beam, the tunnel and the ledges, is the level's
/// (<c>levels/feature-test.map</c>), drawn in the prototyping palette and colliding as the level's
/// brushes do. What is made here is what the player reads from the piece under it and what moves,
/// each an entity with a <see cref="RigidBody"/> and a <see cref="Collider"/> sized by the
/// transform's scale over a unit box. The moving pieces are kinematic bodies whose transforms are
/// set each frame, which the physics follows and which carry what stands on them. What a surface
/// does to the player is the player's to apply (<see cref="Player"/>), from <see cref="Under"/>,
/// since a character's contacts have no friction and a static belt does not move.
/// </para>
/// </remarks>
[Behavior]
public partial struct Course
{
    /// <summary>The ground's height at the course, where every piece stands.</summary>
    private const float Ground = Scene.GroundHeight;

    private static readonly Dictionary<Entity, Surface> Surfaces = [];
    private static readonly List<(Entity Piece, Func<double, Transform> At)> Moving = [];
    private static Entity _pit = Entity.None;
    private static AssetHandle _cube;

    /// <summary>The direction and speed the conveyor carries, along the course.</summary>
    public static Vec3 Belt { get; } = new(3f, 0f, 0f);

    /// <summary>What the piece of the course an entity is does to the player on it.</summary>
    public static Surface Under(Entity ground) => Surfaces.GetValueOrDefault(ground);

    /// <summary>Builds the course.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        Surfaces.Clear();
        Moving.Clear();
        _pit = Entity.None;
        _cube = AssetHandle.None;

        var drawn = App.HasRenderer && !ctx.Res<Config>().Headless;
        var ecs = ctx.Ecs;
        if (drawn) _cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);

        var ochre = drawn ? Render.CreateMaterial(0.8f, 0.6f, 0.25f, roughness: 0.7f) : AssetHandle.None;
        var steel = drawn ? Render.CreateMaterial(0.55f, 0.6f, 0.65f, metallic: 0.8f, roughness: 0.35f) : AssetHandle.None;
        var ice = drawn ? Render.CreateMaterial(new MaterialSettings { BaseColor = (0.75f, 0.9f, 1f, 1f), Roughness = 0.05f }) : AssetHandle.None;
        var pad = drawn ? Render.CreateMaterial(new MaterialSettings { BaseColor = (0.2f, 0.85f, 0.35f, 1f), Emissive = (0f, 400f, 50f, 1f) }) : AssetHandle.None;
        var dark = drawn ? Render.CreateMaterial(0.04f, 0.04f, 0.05f, roughness: 1f) : AssetHandle.None;
        var belt = drawn ? Render.CreateMaterial(0.15f, 0.15f, 0.17f, roughness: 0.6f) : AssetHandle.None;

        // The middle row, along the court from its start, the ledge over the bounce pad the level's.
        Surfaces[Piece(ecs, "Ice", new Vec3(56f, Ground + 0.025f, 0f), new Vec3(8f, 0.05f, 6f), ice)] = Surface.Ice;
        Surfaces[Piece(ecs, "Bounce pad", new Vec3(66f, Ground + 0.1f, 0f), new Vec3(2f, 0.2f, 2f), pad)] = Surface.Bounce;
        Pit(ecs, dark);

        // The south row, which moves between the level's ledges.
        Moves(ecs, "Moving platform", new Vec3(3f, 0.3f, 3f), steel,
            time => Transform.At(58f + (MathF.Sin((float)time * 0.6f) * 3.5f), Ground + 1.35f, 10f));

        Moves(ecs, "Elevator", new Vec3(3f, 0.3f, 3f), steel,
            time => Transform.At(72f, Ground + 0.15f + ((1f - MathF.Cos((float)time * 0.5f)) * 3.4f), 10.5f));

        Moves(ecs, "Turning disc", new Vec3(6f, 0.3f, 6f), ochre,
            time => new Transform(new Vec3(80f, Ground + 0.15f, 10f), Quat.FromRotationY((float)time * 0.8f), Vec3.One),
            ColliderShape.Cylinder);

        Surfaces[Piece(ecs, "Conveyor", new Vec3(90f, Ground + 0.05f, 10f), new Vec3(10f, 0.1f, 2f), belt)] = Surface.Conveyor;
    }

    /// <summary>Moves the moving pieces, and puts back a player that fell into the pit.</summary>
    [OnUpdate]
    public static void Run(BehaviorContext ctx)
    {
        var time = ctx.Time.ElapsedSeconds;
        foreach (var (piece, at) in Moving)
        {
            if (!ctx.Ecs.IsAlive(piece)) continue;

            var place = at(time);
            ref var transform = ref ctx.Ecs.GetRef<Transform>(piece);
            transform.Translation = place.Translation;
            transform.Rotation = place.Rotation;
        }

        foreach (var contact in ctx.Read<ContactStarted>())
        {
            var other = contact.A == _pit ? contact.B : contact.B == _pit ? contact.A : Entity.None;
            if (other != Entity.None && other == Player.Entity) Player.Put(ctx, Player.Respawn, "fell into the pit");
        }
    }

    /// <summary>
    /// A pit, a dark floor under a sensor that puts the player back at the zone's start.
    /// </summary>
    private static void Pit(EcsWorld ecs, AssetHandle dark)
    {
        var floor = ecs.Spawn();
        if (_cube.IsValid)
        {
            Render.SetMesh(ecs, floor, _cube);
            Render.SetMaterial(ecs, floor, dark);
        }

        ecs.Add(floor, new Transform(new Vec3(78f, Ground + 0.01f, 0f), Quat.Identity, new Vec3(4f, 0.02f, 4f)));
        ecs.SetName(floor, "Pit");

        _pit = ecs.Spawn();
        ecs.Add(_pit, new Transform(new Vec3(78f, Ground + 0.5f, 0f), Quat.Identity, new Vec3(3.6f, 1f, 3.6f)));
        ecs.Add(_pit, new RigidBody { Kind = BodyKind.Static, Sensor = true });
        ecs.Add(_pit, new Collider { Shape = ColliderShape.Box, Size = Vec3.One });
        ecs.SetName(_pit, "Pit's sensor");
    }

    /// <summary>A kinematic piece the course moves each frame, and the physics follows.</summary>
    private static Entity Moves(EcsWorld ecs, string name, Vec3 size, AssetHandle material, Func<double, Transform> at, ColliderShape shape = ColliderShape.Box)
    {
        var piece = ecs.Spawn();
        if (_cube.IsValid)
        {
            Render.SetMesh(ecs, piece, shape == ColliderShape.Cylinder ? Render.CreateMesh(MeshShape.Cylinder, size.X / 2f, size.Y) : _cube);
            Render.SetMaterial(ecs, piece, material);
        }

        var start = at(0);
        var scaled = shape == ColliderShape.Cylinder ? Vec3.One : size;
        ecs.Add(piece, new Transform(start.Translation, start.Rotation, scaled));
        ecs.Add(piece, new RigidBody { Kind = BodyKind.Kinematic });
        ecs.Add(piece, new Collider { Shape = shape, Size = shape == ColliderShape.Cylinder ? size : Vec3.One });
        ecs.SetName(piece, name);
        Moving.Add((piece, at));
        return piece;
    }

    /// <summary>A static piece of the course, a unit box scaled to its size.</summary>
    private static Entity Piece(EcsWorld ecs, string name, Vec3 center, Vec3 size, AssetHandle material, Quat? rotation = null)
    {
        var piece = ecs.Spawn();
        if (_cube.IsValid)
        {
            Render.SetMesh(ecs, piece, _cube);
            Render.SetMaterial(ecs, piece, material);
        }

        ecs.Add(piece, new Transform(center, rotation ?? Quat.Identity, size));
        ecs.Add(piece, new RigidBody { Kind = BodyKind.Static });
        ecs.Add(piece, new Collider { Shape = ColliderShape.Box, Size = Vec3.One });
        ecs.SetName(piece, name);
        return piece;
    }
}

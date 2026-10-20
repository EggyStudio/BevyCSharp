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
/// Three rows of stations along the road from the course's start. The north row climbs, with ramps
/// at 15, 30, 45 and 60 degrees, the last too steep for the character's 46 to stand on, stairs with
/// steps of 0.15, 0.25 and 0.4, the last over its step height of 0.35, a beam a third of a unit
/// wide, and a tunnel too low to stand in, crawled through crouched. The middle row tries the
/// ground underfoot, with ice, a bounce pad beside a ledge too high to jump to, and a pit, whose
/// floor is a sensor that puts the player back at the start. The south row moves, with a platform
/// going to and fro between two ledges, an elevator up to a third, a turning disc, a conveyor belt,
/// and five gaps a little wider each time.
/// </para>
/// <para>
/// Every piece is an entity with a <see cref="RigidBody"/> and a <see cref="Collider"/>, a level's
/// way of making bodies, sized by the transform's scale over a unit box so the course shares one
/// mesh. The moving pieces are kinematic bodies whose transforms are set each frame, which the
/// physics follows and which carry what stands on them. What a surface does to the player is the
/// player's to apply (<see cref="Player"/>), from <see cref="Under"/>, since a character's contacts
/// have no friction and a static belt does not move.
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

        var stone = drawn ? Render.CreateMaterial(0.62f, 0.6f, 0.56f, roughness: 0.9f) : AssetHandle.None;
        var ochre = drawn ? Render.CreateMaterial(0.8f, 0.6f, 0.25f, roughness: 0.7f) : AssetHandle.None;
        var steel = drawn ? Render.CreateMaterial(0.55f, 0.6f, 0.65f, metallic: 0.8f, roughness: 0.35f) : AssetHandle.None;
        var ice = drawn ? Render.CreateMaterial(new MaterialSettings { BaseColor = (0.75f, 0.9f, 1f, 1f), Roughness = 0.05f }) : AssetHandle.None;
        var pad = drawn ? Render.CreateMaterial(new MaterialSettings { BaseColor = (0.2f, 0.85f, 0.35f, 1f), Emissive = (0f, 400f, 50f, 1f) }) : AssetHandle.None;
        var dark = drawn ? Render.CreateMaterial(0.04f, 0.04f, 0.05f, roughness: 1f) : AssetHandle.None;
        var belt = drawn ? Render.CreateMaterial(0.15f, 0.15f, 0.17f, roughness: 0.6f) : AssetHandle.None;

        Ramps(ecs, stone, ochre);
        Stairs(ecs, stone);
        Beam(ecs, stone, steel);
        Tunnel(ecs, stone);

        // The middle row, along the road.
        Surfaces[Piece(ecs, "Ice", new Vec3(56f, Ground + 0.025f, 0f), new Vec3(8f, 0.05f, 6f), ice)] = Surface.Ice;
        Surfaces[Piece(ecs, "Bounce pad", new Vec3(66f, Ground + 0.1f, 0f), new Vec3(2f, 0.2f, 2f), pad)] = Surface.Bounce;
        Piece(ecs, "Ledge over the bounce pad", new Vec3(70f, Ground + 2.5f, 0f), new Vec3(3f, 5f, 4f), stone);
        Pit(ecs, dark);

        // The south row, which moves.
        Piece(ecs, "Ledge before the platform", new Vec3(52f, Ground + 0.75f, 10f), new Vec3(3f, 1.5f, 3f), stone);
        Piece(ecs, "Ledge after the platform", new Vec3(64f, Ground + 0.75f, 10f), new Vec3(3f, 1.5f, 3f), stone);
        Moves(ecs, "Moving platform", new Vec3(3f, 0.3f, 3f), steel,
            time => Transform.At(58f + (MathF.Sin((float)time * 0.6f) * 3.5f), Ground + 1.35f, 10f));

        Piece(ecs, "Ledge at the elevator's top", new Vec3(72f, Ground + 3.5f, 14f), new Vec3(3f, 7f, 3f), stone);
        Moves(ecs, "Elevator", new Vec3(3f, 0.3f, 3f), steel,
            time => Transform.At(72f, Ground + 0.15f + ((1f - MathF.Cos((float)time * 0.5f)) * 3.4f), 10.5f));

        Moves(ecs, "Turning disc", new Vec3(6f, 0.3f, 6f), ochre,
            time => new Transform(new Vec3(80f, Ground + 0.15f, 10f), Quat.FromRotationY((float)time * 0.8f), Vec3.One),
            ColliderShape.Cylinder);

        Surfaces[Piece(ecs, "Conveyor", new Vec3(90f, Ground + 0.05f, 10f), new Vec3(10f, 0.1f, 2f), belt)] = Surface.Conveyor;

        Gaps(ecs, stone);
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

    /// <summary>Four ramps side by side, each up to a landing as high as the others.</summary>
    private static void Ramps(EcsWorld ecs, AssetHandle stone, AssetHandle top)
    {
        const float Rise = 1.5f;
        const float Width = 2.5f;
        float[] angles = [15f, 30f, 45f, 60f];

        for (var i = 0; i < angles.Length; i++)
        {
            var z = -19f + (i * 3.2f);
            var radians = angles[i] * MathF.PI / 180f;
            var run = Rise / MathF.Tan(radians);
            var length = MathF.Sqrt((run * run) + (Rise * Rise));
            const float Thick = 0.3f;

            // Tipped up about Z from its foot at x 50, its top face running from the ground to the
            // landing, so its middle is half the run along and half the rise up, less half its
            // thickness under the face.
            var tilt = Quat.FromAxisAngle(Vec3.UnitZ, radians);
            var middle = new Vec3(50f + (run / 2f), Ground + (Rise / 2f), z) - (tilt * new Vec3(0f, Thick / 2f, 0f));
            Piece(ecs, $"Ramp at {angles[i]:0} degrees", middle, new Vec3(length, Thick, Width), stone, tilt);
            Piece(ecs, $"Landing of the {angles[i]:0} degree ramp", new Vec3(50f + run + 1f, Ground + (Rise / 2f), z), new Vec3(2f, Rise, Width), top);
        }
    }

    /// <summary>
    /// Three flights of stairs, the last with steps over the character's step height.
    /// </summary>
    private static void Stairs(EcsWorld ecs, AssetHandle stone)
    {
        float[] steps = [0.15f, 0.25f, 0.4f];
        const float Depth = 0.35f;
        const float Width = 2.5f;

        for (var flight = 0; flight < steps.Length; flight++)
        {
            var z = -19f + (flight * 3.2f);
            var count = (int)MathF.Round(1.6f / steps[flight]);
            for (var i = 0; i < count; i++)
            {
                var height = steps[flight] * (i + 1);
                Piece(ecs, $"Step {i + 1} of the {steps[flight]:0.00} stairs", new Vec3(62f + (i * Depth), Ground + (height / 2f), z), new Vec3(Depth, height, Width), stone);
            }

            // A landing at the top, as the ramps have, so a walk up does not step off the last
            // step.
            var top = steps[flight] * count;
            Piece(ecs, $"Landing of the {steps[flight]:0.00} stairs", new Vec3(62f + (count * Depth) + 0.825f, Ground + (top / 2f), z), new Vec3(2f, top, Width), stone);
        }
    }

    /// <summary>
    /// A beam a third of a unit wide, a unit up, between two landings with steps to them.
    /// </summary>
    private static void Beam(EcsWorld ecs, AssetHandle stone, AssetHandle steel)
    {
        const float Up = 1f;
        const float Z = -8f;

        for (var i = 0; i < 4; i++)
            Piece(ecs, $"Step {i + 1} to the beam", new Vec3(74f + (i * 0.35f), Ground + (0.125f * (i + 1)), Z), new Vec3(0.35f, 0.25f * (i + 1), 2f), stone);

        Piece(ecs, "Landing before the beam", new Vec3(76.5f, Ground + (Up / 2f), Z), new Vec3(1.5f, Up, 2f), stone);
        Piece(ecs, "Beam", new Vec3(82f, Ground + Up - 0.1f, Z), new Vec3(9.5f, 0.2f, 0.33f), steel);
        Piece(ecs, "Landing after the beam", new Vec3(87.5f, Ground + (Up / 2f), Z), new Vec3(1.5f, Up, 2f), stone);
    }

    /// <summary>A tunnel too low to stand in, between two walls under a slab.</summary>
    private static void Tunnel(EcsWorld ecs, AssetHandle stone)
    {
        const float Inside = 1.3f;
        const float Z = -13f;

        Piece(ecs, "Tunnel's north wall", new Vec3(82f, Ground + (Inside / 2f), Z - 1.2f), new Vec3(8f, Inside, 0.4f), stone);
        Piece(ecs, "Tunnel's south wall", new Vec3(82f, Ground + (Inside / 2f), Z + 1.2f), new Vec3(8f, Inside, 0.4f), stone);
        Piece(ecs, "Tunnel's roof", new Vec3(82f, Ground + Inside + 0.2f, Z), new Vec3(8f, 0.4f, 2.8f), stone);
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

    /// <summary>
    /// Five ledges with gaps between them a little wider each time, up steps at the start.
    /// </summary>
    private static void Gaps(EcsWorld ecs, AssetHandle stone)
    {
        const float Up = 1f;
        const float Z = 2f;

        for (var i = 0; i < 4; i++)
            Piece(ecs, $"Step {i + 1} to the gaps", new Vec3(84f + (i * 0.35f), Ground + (0.125f * (i + 1)), Z + 4f), new Vec3(0.35f, 0.25f * (i + 1), 2f), stone);

        var x = 86.5f;
        float[] gaps = [1f, 1.5f, 2f, 2.5f, 3f];
        Piece(ecs, "First ledge of the gaps", new Vec3(x, Ground + (Up / 2f), Z + 4f), new Vec3(1.5f, Up, 2f), stone);
        x += 0.75f;
        foreach (var gap in gaps)
        {
            x += gap + 0.75f;
            Piece(ecs, $"Ledge after a gap of {gap:0.0}", new Vec3(x, Ground + (Up / 2f), Z + 4f), new Vec3(1.5f, Up, 2f), stone);
            x += 0.75f;
        }
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

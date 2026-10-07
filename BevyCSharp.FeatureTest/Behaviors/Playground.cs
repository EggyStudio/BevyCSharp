using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The physics playground beside the course, things to push, swing, ride and stand on, and a
/// heightmap terrain with balls rolling down it.
/// </summary>
/// <remarks>
/// <para>
/// Crates and balls light enough for the player to push. A door on a hinge between two kinematic
/// anchors' worth of frame, a lift on a slider driven up and down by its joint's drive, a rope of
/// balls held each to the one above by a distance joint, and a pressure plate whose lamp lights
/// while something presses it, read each frame from <see cref="PhysicsWorld.ContactImpulse"/>, the
/// impulse a step between the plate and each body that might stand on it. A joint is an entity of
/// its own (<see cref="JointBetween"/>), placed where the bodies are joined, and both of its bodies
/// move, an anchor that should not being a kinematic body nothing moves.
/// </para>
/// <para>
/// The terrain is a mesh made from a height a point of a grid, flat at its edges where it meets the
/// ground, with a <see cref="ColliderShape.Mesh"/> collider made from that same mesh, so the balls
/// dropped on it roll down its slopes and the player walks over it.
/// </para>
/// </remarks>
[Behavior]
public partial struct Playground
{
    private const float Ground = Scene.GroundHeight;

    /// <summary>
    /// What presses on the plate is weighed against this, about a crate's weight a step.
    /// </summary>
    private const float Pressed = 2f;

    private static Entity _plate = Entity.None;
    private static Entity _lamp = Entity.None;
    private static Entity _lift = Entity.None;
    private static readonly List<Entity> Pressers = [];
    private static AssetHandle _lit;
    private static AssetHandle _unlit;
    private static bool _shining;

    /// <summary>Builds the playground and the terrain.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        var drawn = App.HasRenderer && !ctx.Res<Config>().Headless;
        var ecs = ctx.Ecs;
        Pressers.Clear();
        _shining = false;

        var wood = drawn ? Render.CreateMaterial(0.72f, 0.48f, 0.25f, roughness: 0.8f) : AssetHandle.None;
        var rubber = drawn ? Render.CreateMaterial(0.2f, 0.45f, 0.9f, roughness: 0.45f) : AssetHandle.None;
        var steel = drawn ? Render.CreateMaterial(0.55f, 0.6f, 0.65f, metallic: 0.8f, roughness: 0.35f) : AssetHandle.None;
        var grass = drawn ? Render.CreateMaterial(0.35f, 0.55f, 0.25f, roughness: 0.95f) : AssetHandle.None;
        _lit = drawn ? Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.9f, 0.5f, 1f), Emissive = (8_000f, 6_000f, 2_000f, 1f) }) : AssetHandle.None;
        _unlit = drawn ? Render.CreateMaterial(0.25f, 0.25f, 0.22f, roughness: 0.6f) : AssetHandle.None;

        var box = drawn ? Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f) : AssetHandle.None;
        var ball = drawn ? Render.CreateMesh(MeshShape.Sphere, 0.5f) : AssetHandle.None;

        for (var i = 0; i < 6; i++)
            Pressers.Add(Body(ecs, $"Crate {i + 1}", box, wood, new Vec3(54f + (i % 3 * 1.2f), Ground + 0.4f + (i / 3 * 0.85f), 22f), new Vec3(0.8f), ColliderShape.Box, 20f));

        for (var i = 0; i < 5; i++)
            Pressers.Add(Body(ecs, $"Ball {i + 1}", ball, rubber, new Vec3(54f + (i * 1.1f), Ground + 0.35f, 26f), new Vec3(0.7f), ColliderShape.Sphere, 4f));

        Door(ecs, box, wood, steel);
        Lift(ecs, box, steel);
        Rope(ecs, box, ball, steel, rubber);
        Plate(ecs, box, ball, steel);
        Terrain(ecs, drawn, grass, ball, rubber);
    }

    /// <summary>
    /// Drives the lift up and down, and lights the lamp while the plate is pressed.
    /// </summary>
    [OnUpdate]
    public static void Run(BehaviorContext ctx)
    {
        if (!ctx.World.TryGetResource<PhysicsWorld>(out var physics)) return;

        // Up for five seconds and down for five, by its slider's drive.
        if (physics.JointOf(_lift) is { } slider)
            physics.SetDrive(slider, (int)(ctx.Time.ElapsedSeconds / 5.0) % 2 == 0 ? 0.8f : -0.8f, 6_000f);

        if (_plate == Entity.None || !physics.Has(_plate)) return;

        var pressing = 0f;
        foreach (var presser in Pressers.Append(Player.Entity))
            if (ctx.Ecs.IsAlive(presser) && physics.Has(presser)) pressing += physics.ContactImpulse(_plate, presser);

        var shining = pressing > Pressed;
        if (shining == _shining || !_lit.IsValid || !ctx.Ecs.IsAlive(_lamp)) return;

        _shining = shining;
        Render.SetMaterial(ctx.Ecs, _lamp, shining ? _lit : _unlit);
        Console.WriteLine($"[Playground] the plate is {(shining ? "pressed" : "let go")}");
    }

    /// <summary>A door on a hinge at its edge, between a frame of two posts.</summary>
    private static void Door(EcsWorld ecs, AssetHandle box, AssetHandle wood, AssetHandle steel)
    {
        const float Z = 22f;
        var hinge = new Vec3(62f, Ground + 1.15f, Z);

        Body(ecs, "Door's near post", box, steel, new Vec3(61.9f, Ground + 1.2f, Z), new Vec3(0.2f, 2.4f, 0.2f), ColliderShape.Box, 0f, BodyKind.Static);
        Body(ecs, "Door's far post", box, steel, new Vec3(63.5f, Ground + 1.2f, Z), new Vec3(0.2f, 2.4f, 0.2f), ColliderShape.Box, 0f, BodyKind.Static);

        var anchor = Body(ecs, "Door's hinge", AssetHandle.None, AssetHandle.None, hinge, new Vec3(0.05f), ColliderShape.Box, 0f, BodyKind.Kinematic);
        var door = Body(ecs, "Door", box, wood, new Vec3(62.7f, Ground + 1.15f, Z), new Vec3(1.2f, 2.2f, 0.1f), ColliderShape.Box, 15f);
        Joint(ecs, "Door's hinge joint", hinge, new JointBetween { Kind = JointKind.Hinge, A = anchor, B = door, MinAngle = -110f, MaxAngle = 110f });
    }

    /// <summary>A lift on a slider, which <see cref="Run"/> drives up and down.</summary>
    private static void Lift(EcsWorld ecs, AssetHandle box, AssetHandle steel)
    {
        var at = new Vec3(68f, Ground + 0.2f, 24f);
        var anchor = Body(ecs, "Lift's track", box, steel, new Vec3(at.X, Ground + 0.05f, at.Z + 1.5f), new Vec3(0.3f, 0.1f, 0.3f), ColliderShape.Box, 0f, BodyKind.Kinematic);
        var lift = Body(ecs, "Lift", box, steel, at, new Vec3(2.5f, 0.2f, 2.5f), ColliderShape.Box, 40f);
        _lift = Joint(ecs, "Lift's slider", at, new JointBetween { Kind = JointKind.Slider, A = anchor, B = lift, MinTravel = 0f, MaxTravel = 4f });
    }

    /// <summary>
    /// A rope of balls hung from an anchor, each held to the one above by a distance joint.
    /// </summary>
    private static void Rope(EcsWorld ecs, AssetHandle box, AssetHandle ball, AssetHandle steel, AssetHandle rubber)
    {
        const float Link = 0.45f;
        var top = new Vec3(74f, Ground + 6f, 22f);
        Body(ecs, "Rope's post", box, steel, new Vec3(top.X, Ground + 3f, top.Z - 0.6f), new Vec3(0.25f, 6f, 0.25f), ColliderShape.Box, 0f, BodyKind.Static);
        var above = Body(ecs, "Rope's anchor", box, steel, top, new Vec3(0.3f, 0.2f, 0.3f), ColliderShape.Box, 0f, BodyKind.Kinematic);

        for (var i = 1; i <= 10; i++)
        {
            var last = i == 10;
            var at = top - new Vec3(0f, i * Link, 0f);
            var link = Body(ecs, last ? "Rope's weight" : $"Rope's link {i}", ball, last ? rubber : steel, at,
                new Vec3(last ? 0.5f : 0.2f), ColliderShape.Sphere, last ? 6f : 0.5f);
            Joint(ecs, $"Rope's joint {i}", ecs.GetOrDefault<Transform>(above).Translation,
                new JointBetween { Kind = JointKind.Distance, A = above, B = link, MinDistance = 0f, MaxDistance = Link });
            above = link;
        }
    }

    /// <summary>
    /// A pressure plate, with a lamp on a post that lights while something presses it.
    /// </summary>
    private static void Plate(EcsWorld ecs, AssetHandle box, AssetHandle ball, AssetHandle steel)
    {
        _plate = Body(ecs, "Pressure plate", box, steel, new Vec3(80f, Ground + 0.05f, 24f), new Vec3(2f, 0.1f, 2f), ColliderShape.Box, 0f, BodyKind.Static);
        Body(ecs, "Lamp's post", box, steel, new Vec3(80f, Ground + 1.2f, 26f), new Vec3(0.15f, 2.4f, 0.15f), ColliderShape.Box, 0f, BodyKind.Static);

        _lamp = ecs.Spawn();
        if (ball.IsValid)
        {
            Render.SetMesh(ecs, _lamp, ball);
            Render.SetMaterial(ecs, _lamp, _unlit);
        }

        ecs.Add(_lamp, new Transform(new Vec3(80f, Ground + 2.7f, 26f), Quat.Identity, new Vec3(0.7f)));
        ecs.SetName(_lamp, "Plate's lamp");
    }

    /// <summary>
    /// A heightmap terrain with a mesh collider, and balls dropped onto its hills.
    /// </summary>
    private static void Terrain(EcsWorld ecs, bool drawn, AssetHandle grass, AssetHandle ball, AssetHandle rubber)
    {
        const int Cells = 48;
        const float Size = 24f;
        var center = new Vec3(100f, Ground, 24f);

        var data = Heightmap(Cells, Size);
        var terrain = ecs.Spawn();
        if (drawn)
        {
            Render.SetMesh(ecs, terrain, Render.CreateMesh(data));
            Render.SetMaterial(ecs, terrain, grass);
        }

        ecs.Add(terrain, Transform.At(center.X, center.Y, center.Z));
        ecs.SetName(terrain, "Terrain");
        if (drawn)
        {
            // The mesh itself is the collider, which the renderer holds, so a run with none walks
            // the ground beneath instead.
            ecs.Add(terrain, new RigidBody { Kind = BodyKind.Static });
            ecs.Add(terrain, new Collider { Shape = ColliderShape.Mesh });
        }

        for (var i = 0; i < 4; i++)
            Body(ecs, $"Terrain's ball {i + 1}", ball, rubber, center + new Vec3(-4f + (i * 2.5f), 6f + i, -3f + (i * 1.5f)), new Vec3(0.8f), ColliderShape.Sphere, 3f);
    }

    /// <summary>
    /// A grid of heights over a square, flat where it meets the ground at its edges.
    /// </summary>
    internal static MeshData Heightmap(int cells, float size)
    {
        var side = cells + 1;
        var positions = new Vec3[side * side];
        var normals = new Vec3[side * side];
        var uvs = new float[side * side * 2];
        var indices = new uint[cells * cells * 6];
        var step = size / cells;

        static float Height(float x, float z, float size)
        {
            // Hills of two waves, falling to nothing over the outer fifth of each side.
            var edge = MathF.Min(MathF.Min(x, size - x), MathF.Min(z, size - z)) / (size * 0.2f);
            var fade = Math.Clamp(edge, 0f, 1f);
            fade = fade * fade * (3f - (2f * fade));
            var hills = (1.8f * (1f + MathF.Sin(x * 0.45f) * MathF.Cos(z * 0.38f))) + (0.6f * MathF.Sin((x * 1.1f) + (z * 0.7f)));
            return 0.02f + (MathF.Max(0f, hills) * fade);
        }

        for (var row = 0; row < side; row++)
        {
            for (var column = 0; column < side; column++)
            {
                var x = column * step;
                var z = row * step;
                var at = (row * side) + column;
                positions[at] = new Vec3(x - (size / 2f), Height(x, z, size), z - (size / 2f));

                // The slope from the heights a step either side, across and along.
                var dx = Height(x + step, z, size) - Height(x - step, z, size);
                var dz = Height(x, z + step, size) - Height(x, z - step, size);
                normals[at] = new Vec3(-dx, 2f * step, -dz).Normalized;
                uvs[(at * 2) + 0] = x / 2f;
                uvs[(at * 2) + 1] = z / 2f;
            }
        }

        var written = 0;
        for (var row = 0; row < cells; row++)
        {
            for (var column = 0; column < cells; column++)
            {
                var corner = (uint)((row * side) + column);
                var below = corner + (uint)side;
                uint[] two = [corner, below, corner + 1, corner + 1, below, below + 1];
                two.CopyTo(indices, written);
                written += 6;
            }
        }

        return new MeshData { Positions = positions, Normals = normals, Uvs = uvs, Indices = indices };
    }

    /// <summary>
    /// A body, drawn with a mesh where one is given, sized by its transform's scale.
    /// </summary>
    private static Entity Body(EcsWorld ecs, string name, AssetHandle mesh, AssetHandle material, Vec3 at, Vec3 size, ColliderShape shape, float mass, BodyKind kind = BodyKind.Dynamic)
    {
        var body = ecs.Spawn();
        if (mesh.IsValid)
        {
            Render.SetMesh(ecs, body, mesh);
            Render.SetMaterial(ecs, body, material);
        }

        ecs.Add(body, new Transform(at, Quat.Identity, size));
        ecs.Add(body, new RigidBody { Kind = kind, Mass = mass });
        ecs.Add(body, new Collider { Shape = shape, Size = Vec3.One });
        ecs.SetName(body, name);
        return body;
    }

    /// <summary>A joint, an entity of its own placed where its bodies are joined.</summary>
    private static Entity Joint(EcsWorld ecs, string name, Vec3 at, JointBetween joint)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, Transform.At(at.X, at.Y, at.Z));
        ecs.Add(entity, joint);
        ecs.SetName(entity, name);
        return entity;
    }
}

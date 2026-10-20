using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Crates and balls dropped onto the ground from the panel's spawn page, falling, tumbling and
/// stacking under the physics package.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is ordinary engine calls apart from <see cref="PhysicsWorld"/>: a crate is a mesh
/// and a material on an entity, and the simulation moves it by writing its <see cref="Transform"/>
/// once a fixed step. The ground is a static box under the scene's own plane, and the turning cube
/// is a kinematic one, so a crate landing on it is knocked aside by it. Each body goes through
/// <see cref="Bodies"/>, which keeps its shape for the panel's collider draw.
/// </para>
/// <para>
/// One that falls off the edge is despawned once it is far below, which takes its body with it.
/// </para>
/// </remarks>
[Behavior]
public partial struct Crates
{
    private static AssetHandle _box;
    private static AssetHandle _ball;
    private static AssetHandle _wood;
    private static AssetHandle _rubber;
    private static bool _grounded;
    private static int _dropped;

    /// <summary>Starts each app with the ground not yet made a body and nothing dropped.</summary>
    [OnStartup]
    public static void Reset(BehaviorContext ctx)
    {
        _grounded = false;
        _box = _ball = _wood = _rubber = AssetHandle.None;
    }

    /// <summary>
    /// Makes the ground a body once the scene has made it, and clears away what fell off.
    /// </summary>
    [OnUpdate]
    public static void Keep(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless || !ctx.World.TryGetResource<PhysicsWorld>(out var physics)) return;

        if (!_grounded && Ground(ctx.Ecs, physics)) _grounded = true;

        foreach (var row in ctx.Ecs.Query<Crates>(markChanged: false))
        {
            if (ctx.Ecs.GetOrDefault<Transform>(row.Entity).Translation.Y < -30f) ctx.Ecs.Despawn(row.Entity);
        }
    }

    /// <summary>Drops crates, or balls, onto the hub's ground above the cube.</summary>
    internal static string Drop(BehaviorContext ctx, int count, bool balls = false)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return "there is no renderer to draw them with";
        if (!ctx.World.TryGetResource<PhysicsWorld>(out var physics)) return "this app has no physics";

        return Drop(ctx.Ecs, physics, count, balls);
    }

    /// <summary>Takes away everything dropped.</summary>
    internal static string Clear(BehaviorContext ctx)
    {
        var dropped = new List<Entity>();
        foreach (var row in ctx.Ecs.Query<Crates>(markChanged: false)) dropped.Add(row.Entity);
        foreach (var entity in dropped) ctx.Ecs.Despawn(entity);
        return $"took away {dropped.Count}";
    }

    /// <summary>Drops crates from the console.</summary>
    [Command("feature.crates", "Drops crates onto the hub's ground: feature.crates [count]")]
    internal static string Command(string count)
    {
        if (!App.HasRenderer) return "there is no renderer to draw crates with";
        if (ConsoleHost.World?.TryGetResource<PhysicsWorld>(out var physics) != true) return "this app has no physics";

        return Drop(ConsoleHost.Ecs, physics!, int.TryParse(count, out var asked) ? asked : 5, balls: false);
    }

    private static string Drop(EcsWorld ecs, PhysicsWorld physics, int count, bool balls)
    {
        var many = Math.Clamp(count, 1, 200);

        if (!_box.IsValid)
        {
            _box = Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f);
            _ball = Render.CreateMesh(MeshShape.Sphere, 0.3f);
            _wood = Render.CreateMaterial(0.72f, 0.48f, 0.25f, roughness: 0.8f);
            _rubber = Render.CreateMaterial(0.85f, 0.2f, 0.25f, roughness: 0.45f);
        }

        var random = new Random(_dropped);

        for (var i = 0; i < many; i++)
        {
            var at = new Transform(
                new Vec3(random.NextSingle() * 3f - 1.5f, 4f + i * 0.7f, random.NextSingle() * 3f - 1.5f),
                Quat.FromAxisAngle(Vec3.UnitY, random.NextSingle() * MathF.Tau),
                Vec3.One);

            var dropped = ecs.Spawn();
            Render.SetMesh(ecs, dropped, balls ? _ball : _box);
            Render.SetMaterial(ecs, dropped, balls ? _rubber : _wood);
            ecs.Add(dropped, at);
            ecs.Add(dropped, new Crates());
            ecs.SetName(dropped, balls ? "Ball" : "Crate");

            Bodies.Add(physics, dropped, balls ? BodyShape.Sphere(0.3f) : BodyShape.Box(new Vec3(0.5f)), BodyKind.Dynamic, at);
            _dropped++;
        }

        return $"dropped {many} {(balls ? "balls" : "crates")}, {physics.Count} bodies in all";
    }

    /// <summary>The ground and the cube as bodies, once the scene has made them.</summary>
    private static bool Ground(EcsWorld ecs, PhysicsWorld physics)
    {
        var (ground, cube, _) = Scene.Parts;
        if (ground == Entity.None || !ecs.IsAlive(ground)) return false;

        // The plane is drawn at its entity's height with no thickness, so the box is sunk half its
        // depth under it and its top is where the plane is drawn.
        var plane = ecs.GetOrDefault<Transform>(ground).Translation;
        var slab = ecs.Spawn();
        var under = Transform.At(plane.X, plane.Y - 0.5f, plane.Z);
        ecs.Add(slab, under);
        ecs.SetName(slab, "Ground's body");
        Bodies.Add(physics, slab, BodyShape.Box(new Vec3(Scene.GroundSize, 1f, Scene.GroundSize)), BodyKind.Static, under);

        Bodies.Add(physics, cube, BodyShape.Box(new Vec3(1.6f)), BodyKind.Kinematic, ecs.GetOrDefault<Transform>(cube));
        return true;
    }
}

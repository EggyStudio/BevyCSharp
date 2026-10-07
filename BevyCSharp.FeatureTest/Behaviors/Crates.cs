using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Crates dropped onto the ground on F7, falling, tumbling and stacking under the physics package.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is ordinary engine calls apart from <see cref="PhysicsWorld"/>: a crate is a mesh
/// and a material on an entity, and the simulation moves it by writing its <see cref="Transform"/>
/// once a fixed step. The ground is a static box under the scene's own plane, and the turning cube
/// is a kinematic one, so a crate landing on it is knocked aside by it.
/// </para>
/// <para>
/// A crate that falls off the edge is despawned once it is far below, which takes its body with it.
/// </para>
/// </remarks>
[Behavior]
public partial struct Crates
{
    private static AssetHandle _mesh;
    private static AssetHandle _material;
    private static bool _grounded;
    private static int _dropped;

    /// <summary>Says how to drop them.</summary>
    [OnStartup]
    public static void Announce(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;
        Console.WriteLine("[Crates] F7 drops a handful of crates onto the ground");
    }

    /// <summary>Drops a handful on F7, and clears away the ones that fell off.</summary>
    [OnUpdate]
    public static void Drop(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless || !ctx.World.TryGetResource<PhysicsWorld>(out var physics)) return;

        if (!_grounded && Ground(ctx.Ecs, physics)) _grounded = true;

        if (ctx.Input.KeyPressed(Key.F7))
        {
            Console.WriteLine($"[Crates] {Drop(ctx.Ecs, physics, 5)}");
        }

        foreach (var row in ctx.Ecs.Query<Crates>(markChanged: false))
        {
            if (ctx.Ecs.GetOrDefault<Transform>(row.Entity).Translation.Y < -30f) ctx.Ecs.Despawn(row.Entity);
        }
    }

    /// <summary>Drops crates from above the scene.</summary>
    [Command("sample.crates", "Drops crates onto the sample's ground: sample.crates [count]")]
    internal static string Command(string count)
    {
        if (!App.HasRenderer) return "there is no renderer to draw crates with";
        if (ConsoleHost.World?.TryGetResource<PhysicsWorld>(out var physics) != true) return "this app has no physics";

        return Drop(ConsoleHost.Ecs, physics!, int.TryParse(count, out var asked) ? asked : 5);
    }

    private static string Drop(EcsWorld ecs, PhysicsWorld physics, int count)
    {
        var many = Math.Clamp(count, 1, 200);

        if (!_mesh.IsValid)
        {
            _mesh = Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f);
            _material = Render.CreateMaterial(0.72f, 0.48f, 0.25f, roughness: 0.8f);
        }

        var random = new Random(_dropped);

        for (var i = 0; i < many; i++)
        {
            var at = new Transform(
                new Vec3(random.NextSingle() * 3f - 1.5f, 4f + i * 0.7f, random.NextSingle() * 3f - 1.5f),
                Quat.FromAxisAngle(Vec3.UnitY, random.NextSingle() * MathF.Tau),
                Vec3.One);

            var crate = ecs.Spawn();
            Render.SetMesh(ecs, crate, _mesh);
            Render.SetMaterial(ecs, crate, _material);
            ecs.Add(crate, at);
            ecs.Add(crate, new Crates());

            physics.Add(crate, PhysicsShape.Box(new Vec3(0.5f)), BodyKind.Dynamic, at, mass: 1f);
            _dropped++;
        }

        return $"dropped {many}, {physics.Count} bodies in all";
    }

    /// <summary>
    /// The ground as a static box, and the scene's cube as a kinematic one, once the scene exists.
    /// </summary>
    private static bool Ground(EcsWorld ecs, PhysicsWorld physics)
    {
        var (ground, cube, _) = Scene.Parts;
        if (ground == Entity.None) return false;

        // The plane is drawn at its entity's height with no thickness, so the box is sunk half its
        // depth under it and its top is where the plane is drawn.
        var plane = ecs.GetOrDefault<Transform>(ground).Translation;
        var slab = ecs.Spawn();
        ecs.Add(slab, Transform.At(plane.X, plane.Y - 0.5f, plane.Z));
        physics.Add(slab, PhysicsShape.Box(new Vec3(24f, 1f, 24f)), BodyKind.Static, Transform.At(plane.X, plane.Y - 0.5f, plane.Z));

        physics.Add(cube, PhysicsShape.Box(new Vec3(1.6f)), BodyKind.Kinematic, ecs.GetOrDefault<Transform>(cube));
        return true;
    }
}

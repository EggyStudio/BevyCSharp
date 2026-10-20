using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// A body's shape, for the physics world and for the collider draw, which cannot read it back.
/// </summary>
/// <param name="Physics">The shape the physics world takes.</param>
/// <param name="Kind">Box, sphere, capsule or cylinder, as zero to three.</param>
/// <param name="Size">A box's full size, or a radius and a length.</param>
internal readonly record struct BodyShape(PhysicsShape Physics, int Kind, Vec3 Size)
{
    public static BodyShape Box(Vec3 size) => new(PhysicsShape.Box(size), 0, size);

    public static BodyShape Sphere(float radius) => new(PhysicsShape.Sphere(radius), 1, new Vec3(radius, 0f, 0f));
}

/// <summary>
/// The bodies the program adds to the physics world, kept with their shapes so the panel's
/// collider draw can show them, and that draw.
/// </summary>
/// <remarks>
/// The physics world takes a shape as a body is added and gives no list of them back, so what the
/// program adds goes through <see cref="Add"/>, which keeps the shape beside the entity. An entity
/// given a <see cref="Collider"/> instead, as a scene's are, is drawn from that component. Each
/// collider is drawn in front of everything, green for one that moves and gray for one that does
/// not, so a body is seen where its mesh hides it.
/// </remarks>
[Behavior]
public partial struct Bodies
{
    private static readonly Dictionary<Entity, (BodyShape Shape, BodyKind Kind)> Shapes = [];

    /// <summary>
    /// Adds a body to the physics world, keeping its shape for the collider draw.
    /// </summary>
    internal static void Add(PhysicsWorld physics, Entity entity, BodyShape shape, BodyKind kind, Transform at, float mass = 1f)
    {
        physics.Add(entity, shape.Physics, kind, at, mass: mass);
        Shapes[entity] = (shape, kind);
    }

    /// <summary>Forgets the shapes of the last app's bodies.</summary>
    [OnStartup]
    public static void Forget(BehaviorContext ctx) => Shapes.Clear();

    /// <summary>Draws every collider while the panel's debug page asks for them.</summary>
    [OnUpdate]
    public static void Draw(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        foreach (var entity in Shapes.Keys.Where(entity => !ctx.Ecs.IsAlive(entity)).ToArray()) Shapes.Remove(entity);
        if (!Settings.Current.Colliders) return;

        foreach (var (entity, (shape, kind)) in Shapes)
        {
            if (!ctx.Ecs.TryGet<GlobalTransform>(entity, out var placed)) continue;

            var color = kind == BodyKind.Dynamic ? (0.3f, 1f, 0.45f, 1f) : (0.7f, 0.7f, 0.7f, 1f);
            Shape(placed.Translation, placed.Rotation, shape.Kind, shape.Size, color);
        }

        foreach (var row in ctx.Ecs.Query<Collider>(markChanged: false))
        {
            if (!ctx.Ecs.TryGet<GlobalTransform>(row.Entity, out var placed)) continue;

            // Sized in the entity's own units, so scaled with it, as a level's pieces are sized.
            var collider = row.Component;
            var scale = placed.Scale;
            var size = collider.Size == Vec3.Zero ? Vec3.One : collider.Size;
            size = new Vec3(size.X * scale.X, size.Y * scale.Y, size.Z * scale.Z);
            var offset = new Vec3(collider.Offset.X * scale.X, collider.Offset.Y * scale.Y, collider.Offset.Z * scale.Z);
            var center = placed.Translation + (placed.Rotation * offset);
            var kind = collider.Shape switch
            {
                ColliderShape.Sphere => 1,
                ColliderShape.Capsule => 2,
                ColliderShape.Cylinder => 3,
                _ => 0,
            };
            Shape(center, placed.Rotation, kind, kind == 0 ? size : new Vec3(size.X / 2f, size.Y, 0f), (0.4f, 0.75f, 1f, 1f));
        }
    }

    /// <summary>One shape, by the kind <see cref="BodyShape"/> numbers it.</summary>
    private static void Shape(Vec3 center, Quat rotation, int kind, Vec3 size, (float, float, float, float) color)
    {
        switch (kind)
        {
            case 1:
                Gizmos.Sphere(center, size.X, color: color);
                break;
            case 2:
                Gizmos.Capsule(center, rotation, size.X, size.Y, color);
                break;
            case 3:
                Gizmos.Cylinder(center, rotation, size.X, size.Y / 2f, color);
                break;
            default:
                Gizmos.Box(center, rotation, size, color);
                break;
        }
    }
}

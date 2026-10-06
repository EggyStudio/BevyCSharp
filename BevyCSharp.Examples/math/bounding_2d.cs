// Bevy's bounding_2d example, examples/math/bounding_2d.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Maths;

// This example demonstrates bounding volume intersections. Six shapes, most of them spinning, each
// bounded by a box or a circle, are tested against a volume moving across them, against a ray, or
// against a box or a circle swept along a ray, and a volume is drawn blue where it is met and red
// where it is not. Space moves on to the next test.
internal static class Bounding2d
{
    private const float OffsetX = 125f;
    private const float OffsetY = 75f;

    // Bevy's css palette, as sRGB.
    private static readonly Color Gray = Color.FromSrgb(0.5019608f, 0.5019608f, 0.5019608f);
    private static readonly Color Aqua = Color.FromSrgb(0f, 1f, 1f);
    private static readonly Color OrangeRed = Color.FromSrgb(1f, 0.27058824f, 0f);
    private static readonly Color Yellow = Color.FromSrgb(1f, 1f, 0f);
    private static readonly Color Fuchsia = Color.FromSrgb(1f, 0f, 1f);
    private static readonly Color Lime = Color.FromSrgb(0f, 1f, 0f);

    // Each shape by its entity, since a shape is a value of its own kind, which a component of
    // plain data cannot hold.
    private static readonly Dictionary<Entity, IBounded2d> Shapes = [];
    private static Entity _text;
    private static IntersectionTest? _shown;

    public static void Build(App app)
    {
        app.AddState(IntersectionTest.RayCast);
        app.Startup(Setup, "bounding_2d.Setup");
        app.Update(UpdateText, "bounding_2d.UpdateText");
        app.Update(UpdateVolumes, "bounding_2d.UpdateVolumes");
        app.Update(UpdateTestState, "bounding_2d.UpdateTestState");

        // Bevy's chain, the shapes drawn, the state's test run and the volumes drawn with what it
        // found, in that order within the frame, as one system so nothing comes between them.
        app.On(Stage.PostUpdate, ctx =>
        {
            RenderShapes(ctx);
            Action<BehaviorContext> test = ctx.State<IntersectionTest>() switch
            {
                IntersectionTest.AabbSweep => AabbIntersectionSystem,
                IntersectionTest.CircleSweep => CircleIntersectionSystem,
                IntersectionTest.RayCast => RayCastSystem,
                IntersectionTest.AabbCast => AabbCastSystem,
                _ => BoundingCircleCastSystem,
            };
            test(ctx);
            RenderVolumes(ctx);
        }, "bounding_2d.Test");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Shapes.Clear();
        _shown = null;
        Render2d.SpawnCamera2d();

        void Spawn(float x, float y, IBounded2d shape, bool spin, bool aabb)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(x, y, 0f));
            ecs.Add(entity, new BoundingVolume { WantsAabb = aabb });
            if (spin) ecs.Add(entity, new Spin());
            Shapes[entity] = shape;
        }

        Spawn(-OffsetX, OffsetY, new Circle(45f), spin: false, aabb: true);
        Spawn(0f, OffsetY, Rectangle.FromSize(80f, 80f), spin: true, aabb: false);
        Spawn(OffsetX, OffsetY, new Triangle2d(new Vec2(-40f, -40f), new Vec2(-20f, 40f), new Vec2(40f, 50f)), spin: true, aabb: true);
        Spawn(-OffsetX, -OffsetY, Segment2d.FromDirectionAndLength(new Vec2(1f, 0.3f), 90f), spin: true, aabb: false);
        Spawn(0f, -OffsetY, Capsule2d.FromLength(25f, 50f), spin: true, aabb: true);
        Spawn(OffsetX, -OffsetY, new RegularPolygon(50f, 6), spin: true, aabb: false);

        _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // Bevy's update_test_state, space moving on to the next test.
    private static void UpdateTestState(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        var next = ctx.State<IntersectionTest>() switch
        {
            IntersectionTest.AabbSweep => IntersectionTest.CircleSweep,
            IntersectionTest.CircleSweep => IntersectionTest.RayCast,
            IntersectionTest.RayCast => IntersectionTest.AabbCast,
            IntersectionTest.AabbCast => IntersectionTest.CircleCast,
            _ => IntersectionTest.AabbSweep,
        };
        ctx.SetState(next);
    }

    // Bevy's update_text, the test being run marked among the five.
    private static void UpdateText(BehaviorContext ctx)
    {
        var current = ctx.State<IntersectionTest>();
        if (_shown == current) return;
        _shown = current;

        var lines = Enum.GetValues<IntersectionTest>().Select(test => test == current ? $" * {test} *" : $"   {test}  ");
        Ui.SetText(_text, $"Intersection test:\n{string.Join("\n", lines)}\n\nPress space to cycle");
    }

    // Bevy's update_volumes, each shape's box or circle taken where its transform places it.
    private static void UpdateVolumes(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var (entity, shape) in Shapes)
        {
            var isometry = Isometry2d.FromTransform(ecs.GetOrDefault<Transform>(entity));
            var volume = ecs.GetOrDefault<BoundingVolume>(entity);
            if (volume.WantsAabb) volume.Aabb = shape.AabbAt(isometry);
            else volume.Circle = shape.BoundingCircleAt(isometry);
            ecs.Set(entity, volume);
        }
    }

    // Bevy's render_shapes, each shape drawn in gray where it stands, as Bevy's primitive_2d draws it.
    private static void RenderShapes(BehaviorContext ctx)
    {
        foreach (var (entity, shape) in Shapes)
        {
            var at = Isometry2d.FromTransform(ctx.Ecs.GetOrDefault<Transform>(entity));
            switch (shape)
            {
                case Circle circle:
                    Gizmos.Circle2d((at.Translation.X, at.Translation.Y), circle.Radius, Gray);
                    break;
                case Rectangle rectangle:
                    Gizmos.Rect2d((at.Translation.X, at.Translation.Y), rectangle.HalfSize.X * 2f, rectangle.HalfSize.Y * 2f, Gray, at.Rotation.AsRadians);
                    break;
                case Triangle2d triangle:
                    Outline(at, [triangle.A, triangle.B, triangle.C], closed: true);
                    break;
                case Segment2d segment:
                    Outline(at, [segment.Point1, segment.Point2], closed: false);
                    break;
                case Capsule2d capsule:
                    Outline(at, CapsuleOutline(capsule), closed: true);
                    break;
                case RegularPolygon polygon:
                    var corners = polygon.Vertices(at.Rotation.AsRadians).Select(corner => corner + at.Translation).ToArray();
                    Outline(new Isometry2d(Vec2.Zero, Rot2.Identity), corners, closed: true);
                    break;
            }
        }
    }

    private static void Outline(Isometry2d at, Vec2[] points, bool closed)
    {
        for (var i = 0; i < points.Length - (closed ? 0 : 1); i++)
        {
            var (from, to) = (at * points[i], at * points[(i + 1) % points.Length]);
            Gizmos.Line2d((from.X, from.Y), (to.X, to.Y), Gray);
        }
    }

    // A capsule's outline, up one side, round the top, down the other and round the bottom.
    private static Vec2[] CapsuleOutline(Capsule2d capsule)
    {
        const int Steps = 16;
        IEnumerable<Vec2> Half(float centerY, float start) => Enumerable.Range(0, Steps + 1).Select(i =>
        {
            var angle = start + MathF.PI * i / Steps;
            return new Vec2(MathF.Cos(angle) * capsule.Radius, centerY + MathF.Sin(angle) * capsule.Radius);
        });
        return [.. Half(capsule.HalfLength, 0f), .. Half(-capsule.HalfLength, MathF.PI)];
    }

    // Bevy's render_volumes, each volume drawn blue where the test met it and red where it did not.
    private static void RenderVolumes(BehaviorContext ctx)
    {
        foreach (var entity in Shapes.Keys)
        {
            var volume = ctx.Ecs.GetOrDefault<BoundingVolume>(entity);
            var color = volume.Intersects ? Aqua : OrangeRed;
            if (volume.WantsAabb)
            {
                var (center, size) = (volume.Aabb.Center, volume.Aabb.HalfSize * 2f);
                Gizmos.Rect2d((center.X, center.Y), size.X, size.Y, color);
            }
            else
            {
                Gizmos.Circle2d((volume.Circle.Center.X, volume.Circle.Center.Y), volume.Circle.Radius, color);
            }
        }
    }

    private static void SetIntersects(BehaviorContext ctx, Func<BoundingVolume, bool> test)
    {
        foreach (var entity in Shapes.Keys)
        {
            var volume = ctx.Ecs.GetOrDefault<BoundingVolume>(entity);
            ctx.Ecs.Set(entity, volume with { Intersects = test(volume) });
        }
    }

    private static void FilledCircle(Vec2 position, Color color)
    {
        foreach (var radius in new[] { 1f, 2f, 3f }) Gizmos.Circle2d((position.X, position.Y), radius, color);
    }

    // Bevy's get_and_draw_ray, a ray turning about the middle from 250 out, pointing in, its reach
    // swelling and shrinking.
    private static RayCast2d GetAndDrawRay(BehaviorContext ctx)
    {
        var t = ctx.Time.Elapsed;
        var ray = new Vec2(MathF.Cos(t), MathF.Sin(t));
        var distance = 150f + MathF.Abs(MathF.Sin(0.5f * t)) * 500f;
        var cast = new RayCast2d(new Ray2d(ray * 250f, -ray), distance - 20f);

        var end = cast.Ray.At(cast.Max);
        Gizmos.Line2d((cast.Ray.Origin.X, cast.Ray.Origin.Y), (end.X, end.Y), Color.White);
        FilledCircle(cast.Ray.Origin, Fuchsia);
        return cast;
    }

    private static void RayCastSystem(BehaviorContext ctx)
    {
        var cast = GetAndDrawRay(ctx);
        SetIntersects(ctx, volume =>
        {
            var toi = volume.WantsAabb ? cast.AabbIntersectionAt(volume.Aabb) : cast.CircleIntersectionAt(volume.Circle);
            if (toi is { } at) FilledCircle(cast.Ray.At(at), Lime);
            return toi is not null;
        });
    }

    private static void AabbCastSystem(BehaviorContext ctx)
    {
        var cast = new AabbCast2d(Aabb2d.FromCenter(Vec2.Zero, new Vec2(15f)), GetAndDrawRay(ctx));
        SetIntersects(ctx, volume =>
        {
            var toi = volume.WantsAabb ? cast.AabbCollisionAt(volume.Aabb) : null;
            if (toi is { } at)
            {
                var center = cast.Ray.Ray.At(at);
                Gizmos.Rect2d((center.X, center.Y), 30f, 30f, Lime);
            }

            return toi is not null;
        });
    }

    private static void BoundingCircleCastSystem(BehaviorContext ctx)
    {
        var cast = new BoundingCircleCast(new BoundingCircle(Vec2.Zero, 15f), GetAndDrawRay(ctx));
        SetIntersects(ctx, volume =>
        {
            var toi = volume.WantsAabb ? null : cast.CircleCollisionAt(volume.Circle);
            if (toi is { } at)
            {
                var center = cast.Ray.Ray.At(at);
                Gizmos.Circle2d((center.X, center.Y), cast.Circle.Radius, Lime);
            }

            return toi is not null;
        });
    }

    // Bevy's get_intersection_position, a figure of eight across the shapes.
    private static Vec2 IntersectionPosition(BehaviorContext ctx) =>
        new(MathF.Cos(0.8f * ctx.Time.Elapsed) * 250f, MathF.Sin(0.4f * ctx.Time.Elapsed) * 100f);

    private static void AabbIntersectionSystem(BehaviorContext ctx)
    {
        var center = IntersectionPosition(ctx);
        var box = Aabb2d.FromCenter(center, new Vec2(50f));
        Gizmos.Rect2d((center.X, center.Y), 100f, 100f, Yellow);
        SetIntersects(ctx, volume => volume.WantsAabb ? box.Intersects(volume.Aabb) : box.Intersects(volume.Circle));
    }

    private static void CircleIntersectionSystem(BehaviorContext ctx)
    {
        var center = IntersectionPosition(ctx);
        var circle = new BoundingCircle(center, 50f);
        Gizmos.Circle2d((center.X, center.Y), circle.Radius, Yellow);
        SetIntersects(ctx, volume => volume.WantsAabb ? circle.Intersects(volume.Aabb) : circle.Intersects(volume.Circle));
    }
}

/// <summary>Bevy's Test state of the bounding example, which test the volumes are put to.</summary>
public enum IntersectionTest
{
    /// <summary>A box moving across them.</summary>
    AabbSweep,

    /// <summary>A circle moving across them.</summary>
    CircleSweep,

    /// <summary>A ray.</summary>
    RayCast,

    /// <summary>A box swept along a ray.</summary>
    AabbCast,

    /// <summary>A circle swept along a ray.</summary>
    CircleCast,
}

/// <summary>A shape's bounding volume, the box or the circle it asks for, and whether the test met it.</summary>
[Behavior]
public partial struct BoundingVolume
{
    /// <summary>Whether it is bounded by a box rather than a circle.</summary>
    public bool WantsAabb;

    /// <summary>The box, where it asks for one.</summary>
    public Aabb2d Aabb;

    /// <summary>The circle, where it asks for one.</summary>
    public BoundingCircle Circle;

    /// <summary>Whether the test met it this frame.</summary>
    public bool Intersects;
}

/// <summary>A shape turning slowly about its middle.</summary>
[Behavior]
public partial struct Spin
{
    /// <summary>Bevy's spin, a fifth of a radian a second.</summary>
    [OnUpdate]
    public void Turn(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = transform.Rotation * Quat.FromRotationZ(ctx.Time.Delta / 5f);
}

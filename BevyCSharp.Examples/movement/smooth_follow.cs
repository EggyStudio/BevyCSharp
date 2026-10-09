// Bevy's smooth_follow example, examples/movement/smooth_follow.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Movement;

// Demonstrates smooth following, a red sphere easing after a blue one that flies from one random
// point in a box to the next.
internal static class SmoothFollow
{
    // Bevy's resources, the target's speed, the rate the gap decays at, where the target is headed,
    // and the random source a new point is picked from.
    internal const float TargetSpeed = 5f;
    internal const float DecayRate = 2f;
    internal static Vec3 TargetPosition;
    internal static Random RandomSource = new(68941654);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (RandomSource, TargetPosition) = (new Random(68941654), Vec3.Zero);

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 12f, 12f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.15f, 0.3f)), Transform.At(0f, -2.5f, 0f));
            var sphere = Render.CreateMesh(MeshShape.Sphere, 0.3f);
            ecs.Add(ecs.SpawnMesh(sphere, Render.CreateMaterial(Color.FromSrgb(0.3f, 0.15f, 0.9f)), Transform.Identity), new TargetSphere());
            ecs.Add(ecs.SpawnMesh(sphere, Render.CreateMaterial(Color.FromSrgb(0.9f, 0.3f, 0.3f)), Transform.At(0f, -2f, 0f)), new FollowingSphere());
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true, intensity: 15_000_000f);
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "smooth_follow.Setup");
    }
}

/// <summary>The sphere that flies from one random point in a box to the next.</summary>
[Behavior]
public partial struct TargetSphere
{
    /// <summary>Moved toward its point at its speed, and given a new point anywhere in a cube four units across once there.</summary>
    [OnUpdate]
    public void MoveTarget(BehaviorContext ctx, ref Transform transform)
    {
        var toward = SmoothFollow.TargetPosition - transform.Translation;
        if (toward.Length < 1e-6f)
        {
            var random = SmoothFollow.RandomSource;
            SmoothFollow.TargetPosition = new Vec3(random.NextSingle() * 4f - 2f, random.NextSingle() * 4f - 2f, random.NextSingle() * 4f - 2f);
            return;
        }

        transform.Translation += toward.Normalized * MathF.Min(toward.Length, ctx.Time.Delta * SmoothFollow.TargetSpeed);
    }
}

/// <summary>The sphere easing after the target.</summary>
[Behavior]
public partial struct FollowingSphere
{
    /// <summary>
    /// Nudged toward the target, closing the same share of the gap each second at any frame rate,
    /// as Bevy's <c>smooth_nudge</c> does, after the target has moved, as Bevy chains the two.
    /// </summary>
    [OnUpdate]
    [After("TargetSphere.MoveTarget")]
    public void MoveFollower(BehaviorContext ctx, ref Transform transform)
    {
        foreach (var target in ctx.Ecs.Query<TargetSphere>(markChanged: false))
        {
            var share = 1f - MathF.Exp(-SmoothFollow.DecayRate * ctx.Time.Delta);
            transform.Translation += (ctx.Ecs.GetOrDefault<Transform>(target.Entity).Translation - transform.Translation) * share;
        }
    }
}

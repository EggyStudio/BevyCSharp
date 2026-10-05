// Bevy's smooth_follow example, examples/movement/smooth_follow.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Movement;

// Demonstrates smooth following, a red sphere easing after a blue one that flies from one random
// point in a box to the next.
internal static class SmoothFollow
{
    private const float TargetSpeed = 5f;
    private const float DecayRate = 2f;

    private static Random _random = new(68941654);
    private static Entity _target, _follower;
    private static Vec3 _targetPosition;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_random, _targetPosition) = (new Random(68941654), Vec3.Zero);

            ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 12f, 12f), Scene.Material(Scene.Srgb(0.3f, 0.15f, 0.3f)), Transform.At(0f, -2.5f, 0f));
            var sphere = Render.CreateMesh(MeshShape.Sphere, 0.3f);
            _target = ecs.Mesh(sphere, Scene.Material(Scene.Srgb(0.3f, 0.15f, 0.9f)), Transform.Identity);
            _follower = ecs.Mesh(sphere, Scene.Material(Scene.Srgb(0.9f, 0.3f, 0.3f)), Transform.At(0f, -2f, 0f));
            ecs.PointLight(new Vec3(4f, 8f, 4f), shadows: true, intensity: 15_000_000f);
            ecs.Camera(Transform.LookingAt(new Vec3(-2f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "smooth_follow.Setup");

        // Bevy chains the two, and one system moving the target and then the follower is that order.
        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var target = ecs.GetOrDefault<Transform>(_target);
            var toward = _targetPosition - target.Translation;

            // There, so a new point is picked anywhere in a cube four units across.
            if (toward.Length < 1e-6f)
            {
                _targetPosition = new Vec3(_random.NextSingle() * 4f - 2f, _random.NextSingle() * 4f - 2f, _random.NextSingle() * 4f - 2f);
            }
            else
            {
                target.Translation += toward.Normalized * MathF.Min(toward.Length, ctx.Time.Delta * TargetSpeed);
                ecs.Set(_target, target);
            }

            // Bevy's smooth_nudge, closing the same share of the gap each second at any frame rate.
            var follower = ecs.GetOrDefault<Transform>(_follower);
            var share = 1f - MathF.Exp(-DecayRate * ctx.Time.Delta);
            follower.Translation += (target.Translation - follower.Translation) * share;
            ecs.Set(_follower, follower);
        }, "smooth_follow.MoveTargetAndFollower");
    }
}

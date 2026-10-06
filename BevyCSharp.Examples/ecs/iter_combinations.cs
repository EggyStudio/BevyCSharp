// Bevy's iter_combinations example, examples/ecs/iter_combinations.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to iterate over combinations of query results, a hundred bodies and a star pulling on
// one another, each pair once, under gravity worked out on the fixed timestep.
internal static class IterCombinations
{
    private const float GravityConstant = 0.001f;
    private const int Bodies = 100;

    internal struct Mass
    {
        public float Value;
    }

    internal struct Acceleration
    {
        public Vec3 Value;
    }

    internal struct LastPosition
    {
        public Vec3 Value;
    }

    internal struct Star;

    private static Entity _camera, _star;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetClearColor((0f, 0f, 0f, 1f));

            var mesh = Render.CreateMesh(MeshShape.Sphere, 1f);
            var random = new Random(19878367);
            float Range(float low, float high) => low + random.NextSingle() * (high - low);

            // Each body starts with a small speed, given as where it was a step ago.
            const float Step = 1f / 64f;
            for (var i = 0; i < Bodies; i++)
            {
                var radius = Range(0.1f, 0.7f);
                var position = new Vec3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)).Normalized * MathF.Cbrt(Range(0.2f, 1f)) * 15f;
                var color = Color.FromSrgb(Range(0.5f, 1f), Range(0.5f, 1f), Range(0.5f, 1f));

                var body = ecs.SpawnMesh(mesh, Render.CreateMaterial(color), new Transform(position, Quat.Identity, new Vec3(radius)));
                ecs.Add(body, new Mass { Value = radius * radius * radius * 10f });
                ecs.Add(body, new Acceleration());
                ecs.Add(body, new LastPosition { Value = position - new Vec3(Range(-0.5f, 0.5f), Range(-0.5f, 0.5f), Range(-0.5f, 0.5f)) * Step });
            }

            var orangeRed = Color.FromSrgb8(255, 69, 0);
            _star = ecs.SpawnMesh(mesh, Render.CreateMaterial(new MaterialSettings { BaseColor = orangeRed, Emissive = (orangeRed.R * 2f, orangeRed.G * 2f, orangeRed.B * 2f, 1f) }), Transform.Identity);
            ecs.Add(_star, new Mass { Value = 500f });
            ecs.Add(_star, new Acceleration());
            ecs.Add(_star, new LastPosition());
            ecs.Add(_star, new Star());
            ecs.SetParent(ecs.SpawnPointLight(Vec3.Zero, range: 100f, radius: 1f), _star);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 10.5f, -30f), Vec3.Zero, Vec3.UnitY));
        }, "iter_combinations.GenerateBodies");

        app.On(Stage.FixedUpdate, InteractBodies, "iter_combinations.InteractBodies");
        app.On(Stage.FixedUpdate, Integrate, "iter_combinations.Integrate");
        app.Update(LookAtStar, "iter_combinations.LookAtStar");
    }

    // Every pair once, each pulling the other by the other's mass, as Bevy's iter_combinations_mut
    // hands out each pair of a query's rows.
    private static void InteractBodies(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var bodies = ecs.EntitiesWith<Mass>();
        var at = bodies.Select(body => ecs.GetOrDefault<Transform>(body).Translation).ToArray();
        var mass = bodies.Select(body => ecs.GetOrDefault<Mass>(body).Value).ToArray();
        var pull = bodies.Select(body => ecs.GetOrDefault<Acceleration>(body).Value).ToArray();

        for (var a = 0; a < bodies.Length; a++)
        {
            for (var b = a + 1; b < bodies.Length; b++)
            {
                var delta = at[b] - at[a];
                var force = delta * (GravityConstant / delta.LengthSquared);
                pull[a] += force * mass[b];
                pull[b] -= force * mass[a];
            }
        }

        for (var i = 0; i < bodies.Length; i++) ecs.Set(bodies[i], new Acceleration { Value = pull[i] });
    }

    // Verlet, each body moved by how far it moved last step and by what pulls on it now.
    private static void Integrate(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var dtSquared = ctx.Time.FixedDelta * ctx.Time.FixedDelta;
        foreach (var body in ecs.EntitiesWith<Mass>())
        {
            var transform = ecs.GetOrDefault<Transform>(body);
            var next = transform.Translation * 2f - ecs.GetOrDefault<LastPosition>(body).Value + ecs.GetOrDefault<Acceleration>(body).Value * dtSquared;
            ecs.Set(body, new Acceleration());
            ecs.Set(body, new LastPosition { Value = transform.Translation });
            transform.Translation = next;
            ecs.Set(body, transform);
        }
    }

    // The camera turns a tenth of the way toward the star each frame.
    private static void LookAtStar(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var camera = ecs.GetOrDefault<Transform>(_camera);
        var toward = Transform.LookingAt(camera.Translation, ecs.GetOrDefault<Transform>(_star).Translation, Vec3.UnitY).Rotation;
        camera.Rotation = Quat.Lerp(toward, camera.Rotation, 0.1f);
        ecs.Set(_camera, camera);
    }
}

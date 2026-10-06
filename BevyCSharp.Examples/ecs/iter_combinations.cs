// Bevy's iter_combinations example, examples/ecs/iter_combinations.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to iterate over combinations of query results, a hundred bodies and a star pulling on
// one another, each pair once, under gravity worked out on the fixed timestep.
internal static class IterCombinations
{
    internal const float GravityConstant = 0.001f;
    private const int Bodies = 100;

    internal static Entity Camera;

    public static void Build(App app) => app.Startup(ctx =>
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
            ecs.Add(body, new LastPos { Value = position - new Vec3(Range(-0.5f, 0.5f), Range(-0.5f, 0.5f), Range(-0.5f, 0.5f)) * Step });
        }

        var orangeRed = Color.FromSrgb8(255, 69, 0);
        var star = ecs.SpawnMesh(mesh, Render.CreateMaterial(new MaterialSettings { BaseColor = orangeRed, Emissive = (orangeRed.R * 2f, orangeRed.G * 2f, orangeRed.B * 2f, 1f) }), Transform.Identity);
        ecs.Add(star, new Mass { Value = 500f });
        ecs.Add(star, new Acceleration());
        ecs.Add(star, new LastPos());
        ecs.Add(star, new Star());
        ecs.SetParent(ecs.SpawnPointLight(Vec3.Zero, range: 100f, radius: 1f), star);

        Camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 10.5f, -30f), Vec3.Zero, Vec3.UnitY));
    }, "iter_combinations.GenerateBodies");
}

/// <summary>How heavy a body is, which is how hard it pulls on the rest.</summary>
[Behavior]
public partial struct Mass
{
    /// <summary>The mass.</summary>
    public float Value;

    /// <summary>
    /// Every pair of bodies once, each pulling the other by the other's mass, as Bevy's
    /// <c>iter_combinations_mut</c> hands out each pair of a query's rows.
    /// </summary>
    [OnFixedUpdate]
    public static void InteractBodies(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var bodies = ecs.EntitiesWith<Mass>();
        if (bodies.Length < 2) return;

        var at = bodies.Select(body => ecs.GetOrDefault<Transform>(body).Translation).ToArray();
        var mass = bodies.Select(body => ecs.GetOrDefault<Mass>(body).Value).ToArray();
        var pull = bodies.Select(body => ecs.GetOrDefault<Acceleration>(body).Value).ToArray();

        for (var a = 0; a < bodies.Length; a++)
        {
            for (var b = a + 1; b < bodies.Length; b++)
            {
                var delta = at[b] - at[a];
                var force = delta * (IterCombinations.GravityConstant / delta.LengthSquared);
                pull[a] += force * mass[b];
                pull[b] -= force * mass[a];
            }
        }

        for (var i = 0; i < bodies.Length; i++) ecs.Set(bodies[i], new Acceleration { Value = pull[i] });
    }
}

/// <summary>What pulls on a body this step.</summary>
[Behavior]
public partial struct Acceleration
{
    /// <summary>The pull.</summary>
    public Vec3 Value;

    /// <summary>
    /// The body moved by Verlet's step, by how far it moved last step and by what pulls on it now,
    /// after the pull is worked out, the order Bevy lists the two in and leaves to its schedule.
    /// </summary>
    [OnFixedUpdate]
    [After("Mass.InteractBodies")]
    public void Integrate(BehaviorContext ctx, ref Transform transform, ref LastPos lastPos)
    {
        var dtSquared = ctx.Time.FixedDelta * ctx.Time.FixedDelta;
        var next = transform.Translation * 2f - lastPos.Value + Value * dtSquared;
        Value = Vec3.Zero;
        lastPos.Value = transform.Translation;
        transform.Translation = next;
    }
}

/// <summary>Where a body was a step ago.</summary>
[Behavior]
public partial struct LastPos
{
    /// <summary>The place.</summary>
    public Vec3 Value;
}

/// <summary>The star at the middle, which the camera keeps turning toward.</summary>
[Behavior]
public partial struct Star
{
    /// <summary>The camera turned a tenth of the way toward the star each frame.</summary>
    [OnUpdate]
    public void LookAtStar(BehaviorContext ctx, in Transform transform)
    {
        var camera = ctx.Ecs.GetOrDefault<Transform>(IterCombinations.Camera);
        var toward = Transform.LookingAt(camera.Translation, transform.Translation, Vec3.UnitY).Rotation;
        camera.Rotation = Quat.Lerp(toward, camera.Rotation, 0.1f);
        ctx.Ecs.Set(IterCombinations.Camera, camera);
    }
}

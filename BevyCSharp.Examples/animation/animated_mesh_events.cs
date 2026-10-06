// Bevy's animated_mesh_events example, examples/animation/animated_mesh_events.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Plays an animation on a skinned glTF model of a fox, its run, with an event placed on the clip at
// each foot as it lands, which throws up a puff of white particles where the foot is.
internal static class AnimatedMeshEvents
{
    private const string FoxPath = "models/animated/Fox.glb";

    // Seeded, so a capture plays the same each time, as Bevy's seeds its own.
    internal static Random Rng = new(19878367);

    private static Entity _fox;
    private static AssetHandle _run;
    private static (AssetHandle Graph, uint Node) _animation;
    private static bool _eventsPlaced;
    private static readonly HashSet<Entity> Started = [];
    internal static (AssetHandle Mesh, AssetHandle Material) Particles;

    // Bevy's Step, placed on the run at each foot.
    internal readonly record struct Step : IAnimationEvent;

    public static void Build(App app)
    {
        app.Startup(Setup, "animated_mesh_events.Setup");
        app.Update(SetupSceneOnceLoaded, "animated_mesh_events.SetupSceneOnceLoaded");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Rng = new Random(19878367);
        Started.Clear();
        _eventsPlaced = false;
        Particles = (Render.CreateMesh(MeshShape.Sphere, 10f), Render.CreateMaterial(Color.White));

        // The run, the file's third clip, in a graph of its own.
        _run = Animation.LoadClip($"{FoxPath}#Animation2");
        _animation = Animation.GraphFromClip(_run);

        FoxScene.Setup(ecs);
        _fox = ecs.SpawnScene(AssetServer.LoadGltfScene(FoxPath));

        // Bevy's observe_on_step, a puff of fourteen particles where the foot that landed is.
        ecs.Observe<Step>(on =>
        {
            var translation = on.Ecs.GetOrDefault<GlobalTransform>(on.Entity).Translation;
            for (var i = 0; i < 14; i++)
            {
                var angle = Rng.NextSingle() * MathF.Tau;
                var reach = 8f + Rng.NextSingle() * 4f;
                var vertical = Rng.NextSingle() * 4f;
                var size = 0.2f + Rng.NextSingle() * 0.8f;
                var velocity = new Vec3(MathF.Cos(angle) * reach, vertical, MathF.Sin(angle) * reach) * 10f;

                var particle = on.Ecs.SpawnMesh(Particles.Mesh, Particles.Material, new Transform(translation, Quat.Identity, new Vec3(size)));
                on.Ecs.Add(particle, new Particle { Lifetime = GameTimer.FromSeconds(0.2f + Rng.NextSingle() * 0.4f, TimerMode.Once), Size = size, Velocity = velocity });
            }
        });
    }

    // Bevy's setup_scene_once_loaded. The feet's events are placed on the run once it has arrived,
    // and each player the fox's scene brings plays the run over and over from the graph.
    private static void SetupSceneOnceLoaded(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (!_eventsPlaced)
        {
            string[] hip = ["root", "_rootJoint", "b_Root_00", "b_Hip_01"];
            AnimationTarget Foot(params string[] below) => AnimationTarget.FromNames([.. hip, .. below]);

            var frontLeft = Foot("b_Spine01_02", "b_Spine02_03", "b_LeftUpperArm_09", "b_LeftForeArm_010", "b_LeftHand_011");
            var frontRight = Foot("b_Spine01_02", "b_Spine02_03", "b_RightUpperArm_06", "b_RightForeArm_07", "b_RightHand_08");
            var backLeft = Foot("b_LeftLeg01_015", "b_LeftLeg02_016", "b_LeftFoot01_017", "b_LeftFoot02_018");
            var backRight = Foot("b_RightLeg01_019", "b_RightLeg02_020", "b_RightFoot01_021", "b_RightFoot02_022");

            if (!Animation.AddEvent(_run, frontLeft, 0.625f, new Step())) return;
            Animation.AddEvent(_run, frontRight, 0.5f, new Step());
            Animation.AddEvent(_run, backLeft, 0f, new Step());
            Animation.AddEvent(_run, backRight, 0.125f, new Step());
            _eventsPlaced = true;
        }

        foreach (var entity in ecs.Descendants(_fox))
        {
            if (ecs.Get<Bevy.Reflected.AnimationPlayerRef>(entity) is null || !Started.Add(entity)) continue;
            Animation.PlayGraph(entity, _animation.Graph, _animation.Node, repeat: true);
        }
    }
}

/// <summary>A particle of a puff, shrinking away as it slows.</summary>
[Behavior]
public partial struct Particle
{
    /// <summary>How long it lasts.</summary>
    public GameTimer Lifetime;

    /// <summary>Its size as it starts.</summary>
    public float Size;

    /// <summary>How fast it goes, slowing toward still.</summary>
    public Vec3 Velocity;

    /// <summary>
    /// Bevy's simulate_particles, the particle moved, shrunk by how far through its life it is,
    /// slowed as Bevy's smooth_nudge slows it at a rate of four, and gone at the end of its life.
    /// </summary>
    [OnUpdate]
    public void SimulateParticles(BehaviorContext ctx, ref Transform transform)
    {
        if (Lifetime.Tick(ctx.Time.Delta).JustFinished)
        {
            ctx.Cmd.Despawn(ctx.Entity);
            return;
        }

        transform.Translation += Velocity * ctx.Time.Delta;
        transform.Scale = new Vec3(Size * (1f - Lifetime.Fraction));
        Velocity *= MathF.Exp(-4f * ctx.Time.Delta);
    }
}

// Bevy's clustered_decal_maps example, examples/3d/clustered_decal_maps.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates clustered decals with normal, metallic-roughness and emissive maps: a Bevy logo is
// stamped on a scratched wall each second, grows in, stays ten seconds and shrinks away, with its
// gold outline drawn, and the buttons turn its glow on and off.
internal static class ClusteredDecalMaps
{
    private const float PlaneHalfSize = 2f;
    private const float DecalMinSize = 0.5f, DecalMaxSize = 1.5f;
    internal const float AnimateIn = 0.3f, Idle = 10f, AnimateOut = 0.3f;

    private static AssetHandle _baseColor, _normal, _metallicRoughness, _emissive;
    private static bool _emissiveDecals;
    private static float _nextSpawn;
    private static Random _random = new();
    private static RadioButtons<bool>? _buttons;

    public static void Build(App app)
    {
        (_emissiveDecals, _nextSpawn) = (false, 1f);
        _random = new Random(19878367);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var linear = new TextureSettings { Srgb = false };
            _baseColor = AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png");
            _normal = AssetServer.LoadImage("clustered_decal_maps/BevyLogo-Normal.png", linear);
            _metallicRoughness = AssetServer.LoadImage("clustered_decal_maps/BevyLogo-MetallicRoughness.png", linear);
            _emissive = AssetServer.Load(AssetKind.Image, "clustered_decal_maps/BevyLogo-Emissive.png");

            // A crimson wall facing the camera, scratched by its normal map.
            var crimson = Color.FromSrgb8(220, 20, 60);
            ecs.SpawnMesh(
                Render.CreateMesh(MeshShape.Plane, PlaneHalfSize * 2f, PlaneHalfSize * 2f),
                Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = crimson,
                    NormalMap = AssetServer.LoadImage("textures/ScratchedGold-Normal.png", linear),
                }),
                new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

            ecs.SpawnPointLight(new Vec3(8f, 16f, -8f), intensity: 10_000_000f, range: 100f);

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(2f, 0f, -7f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true });

            _buttons = new RadioButtons<bool>(ecs, RadioButtons<bool>.Column(), "Emissive Decals", [(true, "On"), (false, "Off")], _emissiveDecals);
        }, "clustered_decal_maps.Setup");

        app.Update(SpawnDecal, "clustered_decal_maps.SpawnDecal");
        app.Update(ctx =>
        {
            if (_buttons is null || !_buttons.Pressed(out var on) || on == _emissiveDecals) return;
            _emissiveDecals = on;
            _buttons.Select(ctx.Ecs, on);
        }, "clustered_decal_maps.HandleEmissionTypeChange");
    }

    // A decal each second somewhere on the wall, turned a random way and given a random size.
    private static void SpawnDecal(BehaviorContext ctx)
    {
        var now = ctx.Time.Elapsed;
        if (now < _nextSpawn) return;
        _nextSpawn += 1f;

        var ecs = ctx.Ecs;
        var at = new Vec3(Between(-PlaneHalfSize, PlaneHalfSize), Between(-PlaneHalfSize, PlaneHalfSize), 0f);
        var size = Between(DecalMinSize, DecalMaxSize);
        var theta = Between(0f, MathF.PI);

        var entity = ecs.Spawn();
        var look = Transform.LookingAt(at, at + Vec3.UnitZ, new Vec3(MathF.Cos(theta), MathF.Sin(theta), 0f));
        ecs.Add(entity, look with { Scale = Vec3.Zero });

        var decal = ecs.Insert<ClusteredDecalRef>(entity);
        decal.BaseColorTexture = _baseColor;
        decal.NormalMapTexture = _normal;
        decal.MetallicRoughnessTexture = _metallicRoughness;
        decal.EmissiveTexture = _emissiveDecals ? _emissive : null;
        ecs.Add(entity, new ExampleDecal { Size = size, State = ExampleDecalState.AnimatingIn, Timer = GameTimer.FromSeconds(AnimateIn, TimerMode.Once) });
    }

    private static float Between(float low, float high) => low + _random.NextSingle() * (high - low);
}

/// <summary>Where a decal is in its life, growing in, staying, or shrinking away.</summary>
public enum ExampleDecalState { AnimatingIn, Idling, AnimatingOut }

/// <summary>A decal stamped on the wall, how large it grows, and where it is in its life.</summary>
[Behavior]
public partial struct ExampleDecal
{
    /// <summary>Its width and height at full size.</summary>
    public float Size;

    /// <summary>Where it is in its life.</summary>
    public ExampleDecalState State;

    /// <summary>The time left in that part of its life, which moves it on to the next when it runs out.</summary>
    public GameTimer Timer;

    /// <summary>
    /// Grown in, kept, and shrunk away, each part of its life by its timer, and despawned by a
    /// command once it has shrunk away.
    /// </summary>
    [OnUpdate]
    public void AnimateDecals(BehaviorContext ctx, ref Transform transform)
    {
        var finished = Timer.Tick(ctx.Time.Delta).JustFinished;
        switch (State)
        {
            case ExampleDecalState.AnimatingIn when finished:
                (State, Timer) = (ExampleDecalState.Idling, GameTimer.FromSeconds(ClusteredDecalMaps.Idle, TimerMode.Once));
                break;
            case ExampleDecalState.Idling when finished:
                (State, Timer) = (ExampleDecalState.AnimatingOut, GameTimer.FromSeconds(ClusteredDecalMaps.AnimateOut, TimerMode.Once));
                break;
            case ExampleDecalState.AnimatingOut when finished:
                ctx.Cmd.Despawn(ctx.Entity);
                return;
        }

        var factor = State switch
        {
            ExampleDecalState.AnimatingIn => Timer.Fraction,
            ExampleDecalState.Idling => 1f,
            _ => 1f - Timer.Fraction,
        };
        transform.Scale = new Vec3(Size * factor, Size * factor, 1f);
    }

    /// <summary>Its outline drawn as it stands, in gold.</summary>
    [OnUpdate]
    public void DrawGizmos(BehaviorContext ctx, in Transform transform) =>
        Gizmos.Box(transform.Translation, transform.Rotation, transform.Scale, Color.FromSrgb(1f, 215f / 255f, 0f));
}

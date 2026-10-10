// Bevy's many_foxes example, examples/stress_tests/many_foxes.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// A thousand animated foxes, or as many as --count says, running in rings that turn opposite ways,
// to measure skinned meshes and their animation. --sync runs them all in step and --motion-blur
// blurs them. Space pauses them, Up and Down change their speed, Left and Right seek, and Return
// moves them on to their next animation.
//
// Bevy's example also turns off its static transform optimizations, a resource Bevy reflects but
// not as a resource, which the bridge cannot set, so here they are left on, and its
// --mesh-compression sets the glTF plugin's compression, which the bridge does not reach.
internal static class ManyFoxes
{
    private const float RingSpacing = 2f, FoxSpacing = 2f;
    private const string Path = "models/animated/Fox.glb";
    private static readonly string[] Clips = ["Run", "Walk", "Survey"];

    // Bevy's Foxes resource, how fast the rings turn and whether they do, and its Args.
    internal static float Speed = 2f;
    internal static bool Moving = true, Sync;
    private static int _count = 1000;
    private static bool _motionBlur;

    // Each fox's ring and place, spawned once the model has loaded, and each fox spawned.
    private static readonly List<(Entity Ring, Transform At)> Places = [];
    private static readonly List<Entity> Foxes = [];
    private static AssetHandle _fox;
    private static int _currentAnimation;

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        Sync = arguments.Contains("--sync");
        _motionBlur = arguments.Contains("--motion-blur");
        var count = Array.IndexOf(arguments, "--count");
        _count = count >= 0 && count + 1 < arguments.Length && int.TryParse(arguments[count + 1], out var given) ? given : 1000;
        (Speed, Moving, _currentAnimation, _fox) = (2f, true, 0, AssetHandle.None);
        Places.Clear();
        Foxes.Clear();

        app.Startup(Setup, "many_foxes.Setup");
        app.Update(SpawnFoxes, "many_foxes.SpawnFoxes");
        app.Update(KeyboardAnimationControl, "many_foxes.KeyboardAnimationControl");
    }

    private static void Setup(BehaviorContext ctx)
    {
        // Bevy's warning_string.txt, which its stress tests say as they start.
        Console.Error.WriteLine("This is a stress test used to push Bevy to its limit and debug performance issues. It is not representative of an actual game. It must be built in Release or it will be very slow.");
        var ecs = ctx.Ecs;
        _fox = AssetServer.LoadGltfScene(Path, 0);

        // Rings two apart, their foxes at least two apart around each, the rings turning opposite
        // ways. The model faces along Z.
        var radius = RingSpacing;
        var remaining = _count;
        Console.WriteLine($"Spawning {_count} foxes...");
        for (var ringIndex = 0; remaining > 0; ringIndex++)
        {
            var (baseRotation, sign) = ringIndex % 2 == 0 ? (Quat.FromRotationY(MathF.PI), 1f) : (Quat.Identity, -1f);
            var ring = ecs.Spawn();
            ecs.Add(ring, Transform.Identity);
            ecs.Insert<VisibilityRef>(ring);
            ecs.Add(ring, new Ring { Radius = radius, Sign = sign });

            var circumference = MathF.PI * 2f * radius;
            var inRing = Math.Min((int)(circumference / FoxSpacing), remaining);
            var spacingAngle = circumference / (inRing * radius);
            for (var i = 0; i < inRing; i++)
            {
                var angle = i * spacingAngle;
                Places.Add((ring, new Transform(
                    new Vec3(radius * MathF.Cos(angle), 0f, radius * MathF.Sin(angle)),
                    baseRotation * Quat.FromRotationY(-angle),
                    new Vec3(0.01f))));
            }

            remaining -= inRing;
            radius += RingSpacing;
        }

        const float Zoom = 0.8f;
        var translation = new Vec3(radius * 1.25f * Zoom, radius * 0.5f * Zoom, radius * 1.5f * Zoom);
        var camera = ecs.SpawnCamera3d(Transform.LookingAt(translation, 0.2f * new Vec3(translation.X, 0f, translation.Z), Vec3.UnitY));
        if (_motionBlur) ecs.Insert<MotionBlurRef>(camera).ShutterAngle = 3f;

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5000f, 5000f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        // Bevy's EulerRot::ZYX of nothing about Z, a radian about Y and an eighth of a turn down,
        // its shadow cascades fitted to the rings.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
        ecs.Add(light, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        Render.SetShadowCascades(light, maximum: 2.8f * radius, firstBound: 0.9f * radius);

        Console.WriteLine("Animation controls:");
        Console.WriteLine("  - spacebar: play / pause");
        Console.WriteLine("  - arrow up / down: speed up / slow down animation playback");
        Console.WriteLine("  - arrow left / right: seek backward / forward");
        Console.WriteLine("  - return: change animation");
    }

    // Each fox spawned in its ring once the model has loaded, which Bevy does by spawning the
    // model's root at once and filling it in when it arrives.
    private static void SpawnFoxes(BehaviorContext ctx)
    {
        if (Places.Count == 0 || AssetServer.StateOf(_fox) != AssetLoadState.Loaded) return;

        var ecs = ctx.Ecs;
        foreach (var (ring, at) in Places)
        {
            var fox = ecs.SpawnScene(_fox);
            ecs.Set(fox, at);
            ecs.SetParent(fox, ring);
            ecs.Add(fox, new FoxRunner());
            Foxes.Add(fox);
        }

        Places.Clear();
    }

    private static void KeyboardAnimationControl(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.Space)) Moving = !Moving;
        if (input.KeyPressed(Key.ArrowUp)) Speed *= 1.25f;
        if (input.KeyPressed(Key.ArrowDown)) Speed *= 0.8f;
        if (input.KeyPressed(Key.Enter)) _currentAnimation = (_currentAnimation + 1) % Clips.Length;

        // Each fox is asked after only when a key that changes it is down, since asking is a call
        // a fox where Bevy reads the players where they are.
        if (!input.KeyPressed(Key.Space) && !input.KeyPressed(Key.ArrowUp) && !input.KeyPressed(Key.ArrowDown)
            && !input.KeyPressed(Key.ArrowLeft) && !input.KeyPressed(Key.ArrowRight) && !input.KeyPressed(Key.Enter))
            return;

        foreach (var fox in Foxes)
        {
            if (Animation.StateOf(fox) is not { } state) continue;

            if (input.KeyPressed(Key.Space))
            {
                if (state.Paused) Animation.Resume(fox);
                else Animation.Pause(fox);
            }

            if (input.KeyPressed(Key.ArrowUp)) Animation.SetSpeed(fox, state.Speed * 1.25f);
            if (input.KeyPressed(Key.ArrowDown)) Animation.SetSpeed(fox, state.Speed * 0.8f);
            if (input.KeyPressed(Key.ArrowLeft)) Animation.Seek(fox, state.Seconds - 0.1f);
            if (input.KeyPressed(Key.ArrowRight)) Animation.Seek(fox, state.Seconds + 0.1f);
            if (input.KeyPressed(Key.Enter)) Animation.Play(fox, Clips[_currentAnimation], new AnimationSettings { Repeat = true, Blend = 0.25f });
        }
    }
}

/// <summary>A ring of foxes, turning about the middle at the foxes' speed.</summary>
[Behavior]
public partial struct Ring
{
    /// <summary>How far its foxes are from the middle.</summary>
    public float Radius;

    /// <summary>One for counterclockwise and minus one for clockwise, Bevy's RotationDirection.</summary>
    public float Sign;

    /// <summary>Turned by as much as keeps its foxes at the speed, while the foxes move.</summary>
    [OnUpdate]
    public void UpdateFoxRings(BehaviorContext ctx, ref Transform transform)
    {
        if (!ManyFoxes.Moving) return;
        transform.Rotation = Quat.FromRotationY(Sign * ManyFoxes.Speed / Radius * ctx.Time.Delta) * transform.Rotation;
    }
}

/// <summary>
/// A fox whose run starts once its model is in, at a point of its own unless they run in step,
/// as Bevy starts it when the model's scene is ready.
/// </summary>
[Behavior]
public partial struct FoxRunner
{
    /// <summary>Whether its run has started.</summary>
    public bool Started;

    /// <summary>Its run started once its player is there, and moved on by its entity's number.</summary>
    [OnUpdate]
    public void SetupSceneOnceLoaded(BehaviorContext ctx)
    {
        if (Started) return;
        Started = Animation.Play(ctx.Entity, "Run", new AnimationSettings { Repeat = true });
        if (Started && !ManyFoxes.Sync) Animation.Seek(ctx.Entity, ctx.Entity.Index / 10f);
    }
}

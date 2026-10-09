// Bevy's many_morph_targets example, examples/stress_tests/many_morph_targets.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using BevyCSharp.Examples.Gltf;

namespace BevyCSharp.Examples.StressTests;

// A grid of meshes with morph targets, 1024 unless --count says, each playing one of three
// animations of its weights, to measure how morph targets are blended. --weights one, zero or tiny
// sets every weight still rather than animating it, --camera far zooms out so the vertices rather
// than the pixels cost, --spawning gradual, regular-cycle, random-cycle or random-steady spawns and
// despawns them a frame at a time, and --motion-blur blurs them.
internal static class ManyMorphTargets
{
    private const string Path = "models/animated/MorphStressTest.gltf";

    // The model's three animations, by their place in the file.
    internal static readonly string[] Animations = ["Individuals", "Pulse", "TheWave"];

    // Bevy's Args, read from the command line.
    internal static string Weights = "animated";
    private static string _camera = "near", _spawning = "instant";
    private static int _count = 1024;
    private static bool _motionBlur;

    // Bevy's State, the frames gone, the slots of the grid, which are filled, which are empty, and
    // whether a cycle is spawning or despawning.
    private static int _slotCount;
    private static readonly List<(int Slot, Entity Mesh)> Spawned = [];
    private static readonly List<int> Despawned = [];
    private static bool _despawning;

    private static AssetHandle _scene;
    private static Random _random = new(856673);

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
        string Option(string name, string fallback)
        {
            var at = Array.IndexOf(arguments, name);
            return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : fallback;
        }

        _count = int.TryParse(Option("--count", "1024"), out var count) ? count : 1024;
        (Weights, _camera, _spawning) = (Option("--weights", "animated"), Option("--camera", "near"), Option("--spawning", "instant"));
        _motionBlur = arguments.Contains("--motion-blur");

        // Random-steady keeps twice the slots and half of them filled.
        _slotCount = _spawning == "random-steady" ? _count * 2 : _count;
        Spawned.Clear();
        Despawned.Clear();
        Despawned.AddRange(Enumerable.Range(0, _slotCount));
        (_despawning, _random, _scene) = (false, new Random(856673), AssetHandle.None);

        app.Startup(Setup, "many_morph_targets.Setup");
        app.Update(Update, "many_morph_targets.Update");
    }

    // Columns and rows enough for the slots, close to square.
    private static (int X, int Y) Dims(int count)
    {
        var x = Math.Max((int)MathF.Ceiling(MathF.Sqrt(count)), 1);
        return (x, (count + x - 1) / x);
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render.SetAmbientLight((1f, 1f, 1f), 1000f);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
        ecs.Add(light, new Transform(Vec3.Zero, Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));

        var (across, _) = Dims(_slotCount);
        var distance = across * (_camera == "far" ? 200f : 4f);
        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, distance), Vec3.Zero, Vec3.UnitY));
        if (_motionBlur) ecs.Insert<MotionBlurRef>(camera).ShutterAngle = 3f;

        _scene = AssetServer.LoadGltfScene(Path, 0);
    }

    // Some to take at random out of a list, each taken out as it is chosen.
    private static List<T> TakeRandom<T>(List<T> from, int count)
    {
        var taken = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            var at = _random.Next(from.Count);
            taken.Add(from[at]);
            from[at] = from[^1];
            from.RemoveAt(from.Count - 1);
        }

        return taken;
    }

    private static void Update(BehaviorContext ctx)
    {
        // Bevy spawns the model's root at once and fills it in when it arrives, and here the first
        // meshes wait for it to have arrived.
        if (AssetServer.StateOf(_scene) != AssetLoadState.Loaded) return;

        if (Spawned.Count == 0) _despawning = false;
        else if (Despawned.Count == 0) _despawning = true;

        List<int> toSpawn = [];
        List<(int, Entity)> toDespawn = [];
        switch (_spawning)
        {
            case "gradual":
                if (Despawned.Count > 0) toSpawn.Add(Pop(Despawned));
                break;
            case "regular-cycle":
                if (_despawning) toDespawn.Add(Pop(Spawned));
                else toSpawn.Add(Pop(Despawned));
                break;
            case "random-cycle":
                if (_despawning) toDespawn = TakeRandom(Spawned, 1);
                else toSpawn = TakeRandom(Despawned, 1);
                break;
            case "random-steady":
                if (Spawned.Count == 0)
                {
                    toSpawn = TakeRandom(Despawned, _slotCount / 2);
                }
                else
                {
                    toSpawn = TakeRandom(Despawned, 1);
                    toDespawn = TakeRandom(Spawned, 1);
                }

                break;
            default:
                toSpawn.AddRange(Despawned);
                Despawned.Clear();
                break;
        }

        var ecs = ctx.Ecs;
        foreach (var (slot, mesh) in toDespawn)
        {
            ecs.Despawn(mesh);
            Despawned.Add(slot);
        }

        var (across, up) = Dims(_slotCount);
        foreach (var slot in toSpawn)
        {
            // In a grid, each at its own speed so about as many targets move each frame.
            var x = 2.5f + 5f * (slot % across - across * 0.5f);
            var y = -2.2f - 3f * (slot / across - up * 0.5f);
            var mesh = ecs.SpawnScene(_scene);
            ecs.Set(mesh, Transform.At(x, y, 0f));
            ecs.Add(mesh, new AnimationToPlay { Clip = slot % Animations.Length, Speed = slot * 0.1f % 1f + 0.5f });
            Spawned.Add((slot, mesh));
        }
    }

    private static T Pop<T>(List<T> list)
    {
        var last = list[^1];
        list.RemoveAt(list.Count - 1);
        return last;
    }

    // Every weight of every mesh under a model set to one value.
    internal static bool SetWeights(EcsWorld ecs, Entity root, float value)
    {
        var any = false;
        foreach (var child in ecs.Descendants(root))
        {
            if (ecs.Get<MorphWeightsRef>(child) is not { } weights) continue;

            weights.Weights = Enumerable.Repeat(value, weights.Weights.Count).ToArray();
            any = true;
        }

        return any;
    }
}

/// <summary>A model's animation and its speed, started once the model is in.</summary>
[Behavior]
public partial struct AnimationToPlay
{
    /// <summary>Which of the model's animations, by its place in the file.</summary>
    public int Clip;

    /// <summary>How fast it plays.</summary>
    public float Speed;

    /// <summary>Whether it has been started, or its weights set.</summary>
    public bool Done;

    /// <summary>
    /// The animation played on repeat once the model's player is there, or with --weights every
    /// weight set to one value once its meshes are, as Bevy does when the model's scene is ready.
    /// </summary>
    [OnUpdate]
    public void PlayAnimation(BehaviorContext ctx)
    {
        if (Done) return;

        Done = ManyMorphTargets.Weights switch
        {
            "one" => ManyMorphTargets.SetWeights(ctx.Ecs, ctx.Entity, 1f),
            "zero" => ManyMorphTargets.SetWeights(ctx.Ecs, ctx.Entity, 0f),
            "tiny" => ManyMorphTargets.SetWeights(ctx.Ecs, ctx.Entity, 0.00001f),
            _ => Animation.Play(ctx.Entity, ManyMorphTargets.Animations[Clip], new AnimationSettings { Repeat = true, Speed = Speed }),
        };
    }
}

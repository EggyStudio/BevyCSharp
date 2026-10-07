using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The scenes zone north-east of the hub, where a well-known graphics scene fetched as a pack stands
/// at ground level, the packs listed on the panel's Scenes page.
/// </summary>
/// <remarks>
/// <para>
/// The manifests are the checkout's, copied beside the program, and the packs are fetched into the
/// folder every game on the machine shares (<see cref="ScenePacks"/>), so a pack fetched by
/// <c>bcs scenes fetch</c> is one the page finds fetched. A pack's row fetches it on a thread of
/// its own, its bar filling as the bytes come, then loads it, mounting the pack while the program
/// runs and spawning its model in the zone, and unloads it again.
/// </para>
/// <para>
/// Sponza stands with its main door, at the end of its long axis, turned to the road from the hub
/// and swung open, so the player walks in, and its floors, walls, columns and arches are mesh
/// colliders once their meshes have loaded. Its attribution is written over the doorway while it
/// stands, as its license asks.
/// </para>
/// </remarks>
[Behavior]
public partial struct Packs
{
    private const float Ground = Scene.GroundHeight;

    /// <summary>Where a scene's origin stands, beyond the zone's start from the hub.</summary>
    private static readonly Vec3 Site = new(70f, Ground, -70f);

    /// <summary>Turned so the scene's +X, where Sponza's main door is, faces the hub.</summary>
    private static readonly Quat Facing = Quat.FromRotationY(-3f * MathF.PI / 4f);

    /// <summary>The pieces of a scene the player stands on or is stopped by, by their names.</summary>
    private static readonly Regex Solid = new("floor|wall|column|arch|stair|step|exterior", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly ConcurrentDictionary<string, double> Fetching = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> Problems = new(StringComparer.Ordinal);

    private static ScenePack? _shown;
    private static AssetHandle _scene = AssetHandle.None;
    private static Entity _root = Entity.None;
    private static bool _fitted;

    /// <summary>The packs the manifests name.</summary>
    internal static IReadOnlyList<ScenePack> Known { get; private set; } = [];

    /// <summary>Reads the manifests and says which packs are fetched.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        _shown = null;
        _scene = AssetHandle.None;
        _root = Entity.None;
        _fitted = false;
        Problems.Clear();

        Known = ScenePacks.List(Path.Combine(AppContext.BaseDirectory, "scenes"), out var problems);
        foreach (var problem in problems) Console.WriteLine($"[Packs] {problem}");
        Console.WriteLine($"[Packs] scene packs: {string.Join(", ", Known.Select(pack => $"{pack.Name}, {(ScenePacks.IsFetched(pack) ? "fetched" : "not fetched")}"))}");
    }

    /// <summary>What a pack's row says at its right.</summary>
    internal static string StateOf(ScenePack pack) =>
        Fetching.TryGetValue(pack.Name, out var done) ? $"{done * 100:0}%"
        : _shown?.Name == pack.Name ? (_root != Entity.None ? "standing, unload" : "loading")
        : ScenePacks.IsFetched(pack) ? "load"
        : Problems.ContainsKey(pack.Name) ? "failed, fetch again"
        : $"fetch, {pack.Size / 1_000_000} MB";

    /// <summary>How far a pack's fetch has come, or nothing where it is not being fetched.</summary>
    internal static float? ProgressOf(ScenePack pack) => Fetching.TryGetValue(pack.Name, out var done) ? (float)done : null;

    /// <summary>Fetches, loads or unloads a pack, by where it stands, and says which.</summary>
    internal static string Step(BehaviorContext ctx, ScenePack pack)
    {
        if (Fetching.ContainsKey(pack.Name)) return $"{pack.Title} is still being fetched";
        if (_shown?.Name == pack.Name) return Unload(ctx);
        return ScenePacks.IsFetched(pack) ? Load(ctx, pack) : Fetch(pack);
    }

    /// <summary>Loads a fetched pack into the zone, from the console and the drive script.</summary>
    [Command("scene.load", "Loads a fetched scene pack into the scenes zone: scene.load <name>")]
    internal static string LoadCommand(string name)
    {
        if (Known.FirstOrDefault(pack => pack.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)) is not { } pack)
        {
            ConsoleHost.Fail("NO_SUCH_SCENE", $"There is no scene pack called {name.Trim()}. The packs are {string.Join(", ", Known.Select(known => known.Name))}.");
            return "scene.load <name>";
        }

        if (!ScenePacks.IsFetched(pack))
        {
            ConsoleHost.Fail("NOT_FETCHED", $"{pack.Title} has not been fetched. Run bcs scenes fetch {pack.Name}, or fetch it from the panel's Scenes page.");
            return "scene.load <name>";
        }

        return ConsoleHost.World is { } world ? Load(new BehaviorContext(world), pack) : "there is no world to load into";
    }

    /// <summary>Takes the pack standing in the zone away.</summary>
    [Command("scene.unload", "Takes the scene pack standing in the scenes zone away")]
    internal static string UnloadCommand() =>
        ConsoleHost.World is { } world ? Unload(new BehaviorContext(world)) : "there is no world to unload from";

    /// <summary>Spawns a loaded scene in the zone, then opens its door and makes its walls solid.</summary>
    [OnUpdate]
    public static void Place(BehaviorContext ctx)
    {
        if (_shown is not { } pack) return;

        var ecs = ctx.Ecs;
        if (!ecs.IsAlive(_root))
        {
            var state = AssetServer.StateOf(_scene);
            if (state == AssetLoadState.Failed)
            {
                Problems[pack.Name] = $"{pack.Title}'s model did not load from its pack.";
                Console.WriteLine($"[Packs] {Problems[pack.Name]}");
                Unload(ctx);
                return;
            }

            if (state != AssetLoadState.Loaded) return;

            _root = ecs.SpawnScene(_scene);
            ecs.Add(_root, new Transform(Site, Facing, Vec3.One));
            ecs.SetName(_root, pack.Title);
            Console.WriteLine($"[Packs] {pack.Title} stands in the scenes zone. {pack.Attribution}");
            return;
        }

        if (!_fitted) _fitted = Fit(ecs);
    }

    /// <summary>Writes the attribution over the doorway, every frame, while a scene stands.</summary>
    [OnUpdate]
    public static void Credit(BehaviorContext ctx)
    {
        if (_shown is not { } pack || !ctx.Ecs.IsAlive(_root) || !App.HasRenderer || ctx.Res<Config>().Headless) return;

        // Over Sponza's main door, facing out along its axis toward the hub.
        var outward = Facing * Vec3.UnitX;
        var at = Site + (Facing * new Vec3(20f, 0f, 0f)) + new Vec3(0f, 4.2f, 0f);
        var turned = Facing * Quat.FromRotationY(MathF.PI / 2f);
        Gizmos.Text(Wrap(pack.Attribution, 60), at + (outward * 0.1f), turned, 0.16f, (0f, 0f), (0.95f, 0.92f, 0.8f, 1f), inFront: false);
    }

    /// <summary>The pack's page, its row and its attribution.</summary>
    internal static Page PageOf(ScenePack pack) => new(pack.Title,
    [
        new PackRow(pack),
        .. Wrap(pack.Attribution, 44).Split('\n').Select(line => (Row)new TextRow(line)),
    ]);

    /// <summary>
    /// Opens Sponza's main door and makes its solid pieces mesh colliders, once the scene's entities
    /// are there, answering whether they were.
    /// </summary>
    private static bool Fit(EcsWorld ecs)
    {
        var parts = ecs.Descendants(_root).Select(entity => (Entity: entity, Name: ecs.NameOf(entity) ?? string.Empty)).ToList();
        if (parts.Count == 0) return false;

        // The door is 2.5 wide along the scene's Z, so it swings a quarter turn into the hall about
        // its edge, its middle moving back and aside by half its width.
        foreach (var (door, _) in parts.Where(part => part.Name == "wood_door"))
        {
            var shut = ecs.GetOrDefault<Transform>(door);
            ecs.Set(door, new Transform(shut.Translation + new Vec3(-1.25f, 0f, -1.25f), Quat.FromRotationY(-MathF.PI / 2f) * shut.Rotation, shut.Scale));
        }

        var solid = 0;
        foreach (var (piece, name) in parts)
        {
            if (!Solid.IsMatch(name) || name.Contains('.', StringComparison.Ordinal)) continue;

            ecs.Add(piece, new RigidBody { Kind = BodyKind.Static });
            ecs.Add(piece, new Collider { Shape = ColliderShape.Mesh });
            solid++;
        }

        Console.WriteLine($"[Packs] {solid} pieces of {_shown?.Title} are solid, and its door is open");
        return true;
    }

    private static string Fetch(ScenePack pack)
    {
        Problems.TryRemove(pack.Name, out _);
        Fetching[pack.Name] = 0;

        _ = Task.Run(async () =>
        {
            var problem = await ScenePacks.FetchAsync(pack, new Reporter(pack.Name)).ConfigureAwait(false);
            Fetching.TryRemove(pack.Name, out _);
            if (problem is not null) Problems[pack.Name] = problem;
            Console.WriteLine(problem is null ? $"[Packs] fetched {pack.Title}" : $"[Packs] {problem}");
        });

        return $"fetching {pack.Title}";
    }

    private static string Load(BehaviorContext ctx, ScenePack pack)
    {
        if (_shown is not null) Unload(ctx);

        if (!ScenePacks.TryMount(pack, out var model, out var problem))
        {
            Problems[pack.Name] = problem;
            Console.WriteLine($"[Packs] {problem}");
            return problem;
        }

        _scene = AssetServer.LoadGltfScene(model);
        _shown = pack;
        _fitted = false;
        return $"loading {pack.Title}";
    }

    private static string Unload(BehaviorContext ctx)
    {
        if (_shown is not { } pack) return "no scene pack stands in the zone";

        if (ctx.Ecs.IsAlive(_root)) ctx.Ecs.Despawn(_root);
        _root = Entity.None;
        _scene = AssetHandle.None;
        _shown = null;
        ScenePacks.Unmount(pack);
        return $"took {pack.Title} away";
    }

    /// <summary>Text broken into lines at spaces, none longer than a width where a word allows.</summary>
    private static string Wrap(string text, int width)
    {
        var lines = new List<string>();
        var line = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line);
                line = word;
            }
            else
            {
                line = line.Length == 0 ? word : $"{line} {word}";
            }
        }

        if (line.Length > 0) lines.Add(line);
        return string.Join('\n', lines);
    }

    /// <summary>Takes a fetch's progress as it is told, from whichever thread tells it.</summary>
    private sealed class Reporter(string name) : IProgress<double>
    {
        public void Report(double value) => Fetching[name] = value;
    }
}

/// <summary>A scene pack's row, fetching, loading or unloading it, its bar filling as it fetches.</summary>
internal sealed record PackRow(ScenePack Pack) : Row(Pack.Title)
{
    public override string Value => Packs.StateOf(Pack);

    public override float? Progress => Packs.ProgressOf(Pack);

    public override void Step(BehaviorContext ctx, int direction)
    {
        if (direction > 0) Console.WriteLine($"[Packs] {Packs.Step(ctx, Pack)}");
    }
}

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Bevy;
using Bevy.Physics;
using Bevy.Reflected;
using Diagnostics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The scenes zone north-east of the hub, where a well-known graphics scene fetched as a pack
/// stands at ground level, the packs listed on the panel's Scenes page.
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
/// A scene stands where the level's <c>info_scene</c> point does (<c>levels/feature-test.map</c>),
/// turned the way its arrow points, so a level editor moves and turns it. Sponza stands with its
/// main door, at the end of its long axis, turned down the corridor from the hub and swung open, so
/// the player walks in, and its floors, walls, columns and arches are mesh colliders once their
/// meshes have loaded. Its attribution is written over the doorway while it stands, as its license
/// asks, and the yard's floor under it, drawn to its outline, is hidden, so the scene field holds
/// Sponza's floor alone there (<see cref="SceneFloor"/>). The Bistro's exterior is larger than the
/// yard, so it stands alone far from the level's rooms, the player carried into one of its streets
/// once it stands, where its attribution is written behind them, and back to the scenes yard when
/// it is taken away. A scene's own sun is taken off, since the zone's sky has the sun of the hour.
/// </para>
/// <para>
/// Where meshlets run (the graphics page, from the next start), the scene's heaviest meshes, of
/// more than <see cref="Heavy"/> triangles each, are drawn as meshlets, cut on a worker the first
/// time and kept in the scene packs' folder beside the pack, which is mounted for the runs after.
/// The pack's page switches them back to plain meshes and says the GPU's time for each, and where
/// meshlets do not run the plain meshes are all there is. Each heavy mesh has a twin beside it
/// drawing the meshlet mesh with the same material, one of the two hidden, since an entity that has
/// drawn a meshlet mesh draws nothing given its mesh back.
/// </para>
/// </remarks>
[Behavior]
public partial struct Packs
{
    /// <summary>
    /// The class of the level's point a scene stands at, its arrow the way the scene's +X faces.
    /// </summary>
    private const string SiteClass = "info_scene";

    /// <summary>
    /// How far over the level's point a scene's origin stands, two centimeters, so a scene's ground
    /// at its origin's height lies over the yard's floor rather than in its plane, where the two
    /// would fight for the same depth and flicker as the view moves.
    /// </summary>
    private const float Lift = 0.02f;

    /// <summary>What the zone makes of each scene it knows, by its pack's name.</summary>
    /// <remarks>
    /// <para>
    /// Sponza's names say what each piece is, its outer walls named exterior among them. Every
    /// piece of the Bistro's exterior carries the scene's name, exterior and all, so its street,
    /// sidewalks and buildings are taken by their own words. The Bistro is larger than the scenes
    /// yard, so it stands alone three kilometers north of the hub, out of sight of the level's
    /// rooms, the player carried into one of its streets facing the restaurant. Its streets are lit
    /// by lanterns of some 8,000 lumens, as a town's are, some 20 lux on the paving, which the
    /// moonlit ground's lens of the night shows three stops over, so at a night hour the camera
    /// there holds a lamp-lit street's, f/2.8 at a sixtieth of a second and ISO 3200. A scene the
    /// zone does not know is drawn with no piece solid and its attribution over its middle.
    /// </para>
    /// <para>
    /// While a scene stands and global illumination is on, the field its probes are traced through
    /// and their quality are the scene's own. A building 36 meters long, 20 wide and 18 high takes
    /// four cascades of cells of an eighth of a meter, reaching 8, 16, 32 and 64 meters across
    /// about the eye, and a far cascade of 100 meters holds the whole building from anywhere in it,
    /// the sky through its main door among it, which the counted cascades leave past their reach
    /// from its far end. A street longer than that keeps its cells a quarter of a meter, reaching
    /// 128 meters in the fourth, and a far cascade of 240 holds the whole street. Both are traced
    /// at <see cref="GlobalIllumination.High"/>, the most directions a probe sends. The feature
    /// test's own field, four cascades of a quarter meter with none far, comes back as the scene is
    /// taken away, and the quality the player chose with it.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, Fitting> Fittings = new(StringComparer.Ordinal)
    {
        ["intel-sponza"] = new(Words("floor|wall|column|arch|stair|step|exterior"), new Vec3(20f, 4.2f, 0f), MathF.PI / 2f, Covers: true,
            Light: new PackLight(0.125f, 100f, GlobalIllumination.High)),
        ["bistro-exterior"] = new(Words("street|sidewalk|building"), new Vec3(26f, 3.2f, 20f), -3f * MathF.PI / 4f,
            Alone: new Vec3(0f, Scene.GroundHeight, -3000f), Start: new Vec3(15f, 0.5f, 15f), Toward: new Vec3(0f, 1f, 0f),
            NightLens: new PhysicalLens(Aperture: 2.8f, Shutter: 1f / 60f, Sensitivity: 3200f), Light: new PackLight(0.25f, 240f, GlobalIllumination.High)),
    };

    /// <summary>
    /// How near the place of a scene standing alone the eye is in it, so its lens is held.
    /// </summary>
    private const float LensReach = 500f;

    /// <summary>
    /// The solid pieces of the scene standing, each a body once the physics has made it.
    /// </summary>
    private static readonly List<Entity> Solids = [];

    private static readonly ConcurrentDictionary<string, double> Fetching = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> Problems = new(StringComparer.Ordinal);

    /// <summary>How many triangles a mesh has above which it is drawn as meshlets.</summary>
    private const int Heavy = 40_000;

    /// <summary>
    /// The heaviest meshes of the scene standing, each the entity drawing it plain, its twin
    /// drawing it as meshlets, made the first time it is asked for, and its name.
    /// </summary>
    private static readonly List<(Entity Plain, Entity Twin, string Name)> Heaviest = [];

    /// <summary>Each heaviest mesh's meshlet mesh, made or loaded once a load.</summary>
    private static readonly Dictionary<string, AssetHandle> Cut = new(StringComparer.Ordinal);

    /// <summary>The time in milliseconds, smoothed, with plain meshes and with meshlets.</summary>
    private static (double Plain, double Meshlets) _timings;

    /// <summary>Whether the heaviest meshes are drawn as meshlets now.</summary>
    private static bool _asMeshlets;

    /// <summary>
    /// Whether the times are the GPU's, measured pass by pass, rather than the frame's.
    /// </summary>
    private static bool _onGpu;

    private static ScenePack? _shown;

    /// <summary>
    /// The field and the quality the scene standing names for global illumination, none where no
    /// scene stands or it names none.
    /// </summary>
    internal static PackLight? Light => _shown is { } pack && _root != Entity.None ? FittingOf(pack).Light : null;
    private static AssetHandle _scene = AssetHandle.None;
    private static Entity _root = Entity.None;
    private static bool _fitted;

    /// <summary>
    /// Whether the player has been carried into the scene standing, where it stands alone.
    /// </summary>
    private static bool _carried;

    /// <summary>Where the scene standing stands and how it is turned.</summary>
    private static (Vec3 At, Quat Facing) _site;

    /// <summary>The packs the manifests name.</summary>
    internal static IReadOnlyList<ScenePack> Known { get; private set; } = [];

    /// <summary>
    /// The lens of the night the place the eye stands in holds, the scene standing alone's where it
    /// names one, none elsewhere, which <see cref="Applied"/> holds at a night hour in place of the
    /// moonlit ground's.
    /// </summary>
    internal static PhysicalLens? NightLens { get; private set; }

    /// <summary>
    /// Whether the heaviest meshes are to be drawn as meshlets, where meshlets run.
    /// </summary>
    internal static bool AsMeshlets { get; set; } = true;

    /// <summary>
    /// The times with plain meshes and with meshlets, as the pack's page says them.
    /// </summary>
    internal static string Timings =>
        $"{(_onGpu ? "GPU" : "frame")} plain {(_timings.Plain > 0 ? $"{_timings.Plain:0.0} ms" : "unmeasured")}, meshlets {(_timings.Meshlets > 0 ? $"{_timings.Meshlets:0.0} ms" : "unmeasured")}";

    /// <summary>Reads the manifests and says which packs are fetched.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        _shown = null;
        _scene = AssetHandle.None;
        _root = Entity.None;
        _fitted = false;
        _site = default;
        _asMeshlets = false;
        _onGpu = false;
        _timings = default;
        Heaviest.Clear();
        Cut.Clear();
        AsMeshlets = true;
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

    /// <summary>
    /// How far a pack's fetch has come, or nothing where it is not being fetched.
    /// </summary>
    internal static float? ProgressOf(ScenePack pack) => Fetching.TryGetValue(pack.Name, out var done) ? (float)done : null;

    /// <summary>Fetches, loads or unloads a pack, by where it stands, and says which.</summary>
    internal static string Step(BehaviorContext ctx, ScenePack pack)
    {
        if (Fetching.ContainsKey(pack.Name)) return $"{pack.Title} is still being fetched";
        if (_shown?.Name == pack.Name) return Unload(ctx);
        return ScenePacks.IsFetched(pack) ? Load(ctx, pack) : Fetch(pack);
    }

    /// <summary>
    /// Loads a fetched pack into the zone, from the console and build/drive-feature-test.sh.
    /// </summary>
    /// <remarks>
    /// Named apart from the engine's <c>scene.load</c>, which spawns a scene file, since of two
    /// commands of one name the engine's is the one answered.
    /// </remarks>
    [Command("scene.pack", "Loads a fetched scene pack into the scenes zone: scene.pack <name>")]
    internal static string LoadCommand(string name)
    {
        if (Known.FirstOrDefault(pack => pack.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)) is not { } pack)
        {
            ConsoleHost.Fail("NO_SUCH_SCENE", $"There is no scene pack called {name.Trim()}. The packs are {string.Join(", ", Known.Select(known => known.Name))}.");
            return "scene.pack <name>";
        }

        if (!ScenePacks.IsFetched(pack))
        {
            ConsoleHost.Fail("NOT_FETCHED", $"{pack.Title} has not been fetched. Run bcs scenes fetch {pack.Name}, or fetch it from the panel's Scenes page.");
            return "scene.pack <name>";
        }

        return ConsoleHost.World is { } world ? Load(new BehaviorContext(world), pack) : "there is no world to load into";
    }

    /// <summary>
    /// Switches the heaviest meshes between meshlets and plain meshes, as the pack's page does.
    /// </summary>
    [Command("scene.meshlets", "Draws the standing scene's heaviest meshes as meshlets or plain, and says the frame time of each: scene.meshlets [on|off]")]
    internal static string MeshletsCommand(string state)
    {
        if (!Render.MeshletsActive) return "meshlets do not run, so the meshes are plain";

        var words = state.Trim();
        if (words.Length > 0) AsMeshlets = words is "on" or "1" or "true";
        return $"{(AsMeshlets ? "meshlets" : "plain meshes")}, {Timings}";
    }

    /// <summary>Takes the pack standing in the zone away.</summary>
    [Command("scene.unload", "Takes the scene pack standing in the scenes zone away")]
    internal static string UnloadCommand() =>
        ConsoleHost.World is { } world ? Unload(new BehaviorContext(world)) : "there is no world to unload from";

    /// <summary>
    /// Spawns a loaded scene in the zone, then opens its door and makes its walls solid.
    /// </summary>
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

            var alone = FittingOf(pack).Alone;
            if ((alone is { } far ? (far + new Vec3(0f, Lift, 0f), Quat.Identity) : SiteOf(ecs)) is not { } site)
            {
                Problems[pack.Name] = $"The level has no {SiteClass} for {pack.Title} to stand at.";
                Console.WriteLine($"[Packs] {Problems[pack.Name]}");
                Unload(ctx);
                return;
            }

            _site = site;
            _root = ecs.SpawnScene(_scene);
            ecs.Add(_root, new Transform(site.At, site.Facing, Vec3.One));
            ecs.SetName(_root, pack.Title);
            Applied.Due();
            if (alone is null && FittingOf(pack).Covers) SceneFloor.Show(ecs, shown: false);
            Console.WriteLine($"[Packs] {pack.Title} stands {(alone is null ? "in the scenes zone" : "alone, far from the level's rooms")}. {pack.Attribution}");
            return;
        }

        if (!_fitted)
        {
            _fitted = Fit(ecs);
            return;
        }

        if (!_carried)
        {
            if (!Carry(ctx, pack)) return;
            _carried = true;
        }

        if (!Render.MeshletsActive) return;

        // The GPU's time for the frame where it is measured, the sum of its passes as Bevy smooths
        // them, or the frame's own, of the mode drawn now, smoothed over a second or so.
        var gpu = Render.Timings().Sum(timing => timing.GpuMilliseconds ?? 0);
        _onGpu = gpu > 0;
        var frame = _onGpu ? gpu : ctx.Time.RawDeltaSeconds * 1000.0;
        if (_asMeshlets) _timings.Meshlets = _timings.Meshlets > 0 ? (_timings.Meshlets * 0.98) + (frame * 0.02) : frame;
        else _timings.Plain = _timings.Plain > 0 ? (_timings.Plain * 0.98) + (frame * 0.02) : frame;

        if (AsMeshlets == _asMeshlets || !FindHeaviest(ecs)) return;

        for (var i = 0; i < Heaviest.Count; i++)
        {
            var (plain, twin, name) = Heaviest[i];
            if (!ecs.IsAlive(plain)) continue;

            if (AsMeshlets && twin == Entity.None)
            {
                twin = TwinOf(ecs, pack, plain, name);
                Heaviest[i] = (plain, twin, name);
            }

            ecs.Wrap<VisibilityRef>(plain).Value = AsMeshlets ? VisibilityRef.ValueVariant.Hidden : VisibilityRef.ValueVariant.Inherited;
            if (ecs.IsAlive(twin)) ecs.Insert<VisibilityRef>(twin).Value = AsMeshlets ? VisibilityRef.ValueVariant.Inherited : VisibilityRef.ValueVariant.Hidden;
        }

        _asMeshlets = AsMeshlets;
        Console.WriteLine($"[Packs] {Heaviest.Count} of {pack.Title}'s meshes are drawn as {(_asMeshlets ? "meshlets" : "plain meshes")}");
    }

    /// <summary>
    /// Finds the scene's heaviest meshes once every mesh in it has loaded, answering whether it
    /// has.
    /// </summary>
    private static bool FindHeaviest(EcsWorld ecs)
    {
        if (Heaviest.Count > 0) return true;

        var found = new List<(Entity, Entity, string)>();
        foreach (var entity in ecs.Descendants(_root))
        {
            var mesh = Render.MeshOf(ecs, entity);
            if (!mesh.IsValid) continue;
            if (!Render.TryGetMeshInfo(mesh, out var info)) return false;
            if (info.Triangles > Heavy) found.Add((entity, Entity.None, ecs.NameOf(entity) ?? $"mesh {entity}"));
        }

        Heaviest.AddRange(found);
        return true;
    }

    /// <summary>
    /// A heavy mesh's twin, beside it under the same parent, at the same place and with the same
    /// material, drawing its meshlet mesh.
    /// </summary>
    private static Entity TwinOf(EcsWorld ecs, ScenePack pack, Entity plain, string name)
    {
        var twin = ecs.Spawn();
        ecs.Add(twin, ecs.GetOrDefault<Transform>(plain));
        Render.SetMeshletMesh(ecs, twin, MeshletOf(pack, name, Render.MeshOf(ecs, plain)));
        Render.SetMaterial(ecs, twin, Render.MaterialOf(ecs, plain));
        if (ecs.ParentOf(plain) is { IsNone: false } parent) ecs.SetParent(twin, parent);
        ecs.SetName(twin, $"{name}, as meshlets");
        return twin;
    }

    /// <summary>
    /// A heaviest mesh's meshlet mesh, loaded from the cut kept in the cache or cut now on a worker
    /// and kept there for the next run.
    /// </summary>
    private static AssetHandle MeshletOf(ScenePack pack, string name, AssetHandle mesh)
    {
        if (Cut.TryGetValue(name, out var made)) return made;

        var file = string.Concat(name.Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' ? character : '_')) + ".meshlet_mesh";
        var kept = Path.Combine(CacheOf(pack), file);
        made = AssetFiles.Exists($"{CacheFolder(pack)}/{file}")
            ? AssetServer.Load(AssetKind.MeshletMesh, $"{CacheFolder(pack)}/{file}")
            : Render.CreateMeshletMesh(mesh, saveTo: kept);

        Cut[name] = made;
        return made;
    }

    /// <summary>
    /// Where a pack's meshlets are kept, beside the pack in the folder every game shares.
    /// </summary>
    private static string CacheOf(ScenePack pack) => Path.Combine(ScenePacks.Folder, $"{pack.Name}-meshlets");

    /// <summary>Where that folder is mounted under the asset root while the pack stands.</summary>
    private static string CacheFolder(ScenePack pack) => $"{ScenePacks.MountFolder}/{pack.Name}-meshlets";

    /// <summary>
    /// Gives the camera the lens of the place the eye stands in, the scene standing alone's while
    /// the eye is within <see cref="LensReach"/> of it, and the night's own once it is carried out
    /// or the scene is taken away.
    /// </summary>
    [OnUpdate]
    public static void Lens(BehaviorContext ctx)
    {
        PhysicalLens? lens = null;
        if (_shown is { } pack && FittingOf(pack) is { Alone: not null, NightLens: { } own }
            && Scene.Camera is { } camera && ctx.Ecs.IsAlive(camera))
        {
            var eye = ctx.Ecs.GetOrDefault<Transform>(camera).Translation;
            if (new Vec2(eye.X - _site.At.X, eye.Z - _site.At.Z).Length < LensReach) lens = own;
        }

        if (lens == NightLens) return;
        NightLens = lens;
        Applied.Due();
    }

    /// <summary>
    /// Writes the attribution over the doorway, every frame, while a scene stands.
    /// </summary>
    [OnUpdate]
    public static void Credit(BehaviorContext ctx)
    {
        if (_shown is not { } pack || !ctx.Ecs.IsAlive(_root) || !App.HasRenderer || ctx.Res<Config>().Headless) return;

        // Over Sponza's main door, facing out along its axis toward the hub, or behind where the
        // player is put in the Bistro's street, facing them.
        var (site, facing) = _site;
        var fitting = FittingOf(pack);
        var turned = facing * Quat.FromRotationY(fitting.Turn);
        var outward = turned * Vec3.UnitZ;
        var at = site + (facing * fitting.Credit);
        Gizmos.Text(Wrap(pack.Attribution, 60), at + (outward * 0.1f), turned, 0.16f, (0f, 0f), (0.95f, 0.92f, 0.8f, 1f), inFront: false);
    }

    /// <summary>
    /// The pack's page, its row, its meshlets where they run and its attribution.
    /// </summary>
    internal static Page PageOf(ScenePack pack) => new(pack.Title,
    [
        new PackRow(pack),
        .. Render.MeshletsActive
            ? new Row[]
            {
                new ToggleRow("Heaviest meshes as meshlets", () => AsMeshlets, value => AsMeshlets = value),
                new TimingRow(),
            }
            : [new TextRow("Meshlets do not run, so the meshes are plain.")],
        .. Wrap(pack.Attribution, 44).Split('\n').Select(line => (Row)new TextRow(line)),
    ]);

    /// <summary>
    /// Where a scene stands, <see cref="Lift"/> over the level's <c>info_scene</c>, and how it is
    /// turned, so its +X looks the way the point's arrow does, or nothing where the level has none.
    /// </summary>
    /// <remarks>
    /// A point of a level faces along its minus Z, so the scene is turned a quarter more about Y,
    /// which carries its +X onto the point's minus Z.
    /// </remarks>
    private static (Vec3 At, Quat Facing)? SiteOf(EcsWorld ecs)
    {
        foreach (var row in ecs.Query<LevelKeys>(markChanged: false))
        {
            if (row.Component.ClassName.Value != SiteClass) continue;
            var placed = ecs.GetOrDefault<GlobalTransform>(row.Entity);
            return (placed.Translation + new Vec3(0f, Lift, 0f), placed.Rotation * Quat.FromRotationY(MathF.PI / 2f));
        }

        return null;
    }

    /// <summary>A pattern of words, read in any case.</summary>
    private static Regex Words(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The fitting of a scene, or that of one the zone does not know.</summary>
    private static Fitting FittingOf(ScenePack pack) =>
        Fittings.TryGetValue(pack.Name, out var fitting) ? fitting : new(Words("(?!)"), new Vec3(0f, 4.2f, 0f), 0f);

    /// <summary>
    /// Carries the player into a scene that stands alone, once each of its solid pieces is a body
    /// to stand on, answering whether the player was carried or the scene stands in the yard.
    /// </summary>
    /// <remarks>
    /// The pieces become bodies on the physics' next steps, and a player put on a street with no
    /// body under it falls through it and is put back at the scenes yard.
    /// </remarks>
    private static bool Carry(BehaviorContext ctx, ScenePack pack)
    {
        var fitting = FittingOf(pack);
        if (fitting.Alone is null) return true;
        if (ctx.World.TryGetResource<PhysicsWorld>(out var physics) && !Solids.All(physics.Has)) return false;

        var (site, facing) = _site;
        var start = site + (facing * fitting.Start);
        Zones.Go(ctx, new Zone(pack.Title, site + (facing * fitting.Toward), start + new Vec3(0f, 1.7f, 0f), start, 80f));
        return true;
    }

    /// <summary>
    /// Opens Sponza's main door, makes the scene's solid pieces mesh colliders and takes off the
    /// scene's own sun, once the scene's entities are there, answering whether they were.
    /// </summary>
    /// <remarks>
    /// The zone's sky has the sun of the hour, which lights the scene by day, and a sun the scene
    /// carries would light it a second time by day and alone by night, where the night is the
    /// scene's lamps and signs.
    /// </remarks>
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
        var suns = 0;
        var fitting = _shown is { } pack ? FittingOf(pack) : default;
        foreach (var (piece, name) in parts)
        {
            if (ecs.Get<DirectionalLightRef>(piece) is not null)
            {
                ecs.RemoveReflected(piece, DirectionalLightRef.TypePath);
                suns++;
            }

            if (fitting.Solid?.IsMatch(name) != true || name.Contains('.', StringComparison.Ordinal)) continue;

            ecs.Add(piece, new RigidBody { Kind = BodyKind.Static });
            ecs.Add(piece, new Collider { Shape = ColliderShape.Mesh });
            Solids.Add(piece);
            solid++;
        }

        Console.WriteLine($"[Packs] {solid} pieces of {_shown?.Title} are solid, {suns} suns of its own taken off, and its door is open where it has one");
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
        _carried = false;
        Solids.Clear();
        _asMeshlets = false;
        Heaviest.Clear();
        Cut.Clear();

        // The meshlets cut on a run before, read from the folder they were kept in.
        AssetFiles.Mount(CacheFolder(pack), CacheOf(pack));
        return $"loading {pack.Title}";
    }

    private static string Unload(BehaviorContext ctx)
    {
        if (_shown is not { } pack) return "no scene pack stands in the zone";

        if (ctx.Ecs.IsAlive(_root)) ctx.Ecs.Despawn(_root);
        SceneFloor.Show(ctx.Ecs, shown: true);
        if (_carried && FittingOf(pack).Alone is not null) Zones.Go(ctx, Zones.All.First(zone => zone.Name == "Scenes"));
        _root = Entity.None;
        _scene = AssetHandle.None;
        _shown = null;
        _carried = false;
        Applied.Due();
        Solids.Clear();
        Heaviest.Clear();
        Cut.Clear();
        ScenePacks.Unmount(pack);
        AssetFiles.Unmount(CacheFolder(pack));
        return $"took {pack.Title} away";
    }

    /// <summary>
    /// Text broken into lines at spaces, none longer than a width where a word allows.
    /// </summary>
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

/// <summary>
/// How the zone fits a scene, the pieces the player stands on or is stopped by, by their names,
/// where the attribution is written in the scene's own frame, turned about its Y to face the way it
/// is read from, and, for a scene that stands alone, where it stands, where in it the player is put
/// and looks, in its own frame, and the lens the camera holds there at a night hour, for a scene at
/// the yard's site, whether its ground covers the yard's floor under the site, which stands hidden
/// while it stands (<see cref="SceneFloor"/>), and the field and quality global illumination is
/// traced at while it stands.
/// </summary>
internal readonly record struct Fitting(Regex Solid, Vec3 Credit, float Turn, Vec3? Alone = null, Vec3 Start = default, Vec3 Toward = default,
    PhysicalLens? NightLens = null, bool Covers = false, PackLight? Light = null);

/// <summary>
/// The field a scene's global illumination is traced through, four cascades with cells of
/// <paramref name="Cell"/> meters and a far cascade reaching <paramref name="Far"/> meters ahead,
/// and the quality its probes are traced at.
/// </summary>
internal readonly record struct PackLight(float Cell, float Far, GlobalIllumination Quality);

/// <summary>The time with plain meshes and with meshlets, as each was last measured.</summary>
internal sealed record TimingRow() : Row("Time")
{
    public override string Value => Packs.Timings;

    public override void Step(BehaviorContext ctx, int direction)
    {
    }
}

/// <summary>
/// A scene pack's row, fetching, loading or unloading it, its bar filling as it fetches.
/// </summary>
internal sealed record PackRow(ScenePack Pack) : Row(Pack.Title)
{
    public override string Value => Packs.StateOf(Pack);

    public override float? Progress => Packs.ProgressOf(Pack);

    public override void Step(BehaviorContext ctx, int direction)
    {
        if (direction > 0) Console.WriteLine($"[Packs] {Packs.Step(ctx, Pack)}");
    }
}

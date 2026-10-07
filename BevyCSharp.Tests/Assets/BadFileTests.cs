using System.Text.Json;
using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Every loader given a file that is missing, empty, cut short or random bytes, answering with a
/// message that names the file and no exception, as N 2.6 of NORM.md has it. A loader added later
/// is a row here.
/// </summary>
/// <remarks>
/// <para>
/// Each row judges the form a game reaches first. A scene or a save is answered in the
/// <see cref="SceneLoad"/> it returns, with nothing spawned and the game in progress left as it
/// was. A mesh or material file gives no handle and says why as <see cref="AssetLoadFailed"/>, and
/// its <c>TryLoad</c> says why as it returns. A data asset read through its reference is its
/// type's defaults with the same message, and its <c>TryGet</c> says why. Project settings and a
/// player's persistent value are their defaults with the reason in <c>Problem</c>, and a pack's
/// <see cref="AssetPack.TryOpen"/> says why as it returns. A load through Bevy's asset server
/// fails, which its state says, and <see cref="AssetLoadFailed"/> names the file. Any exception,
/// a load that never ends, or a message that leaves the file out fails the row, and a panic in the
/// bridge would end the run.
/// </para>
/// <para>
/// The file cut short is the first third of a good one, which the test makes or copies, and the
/// random one 4,096 bytes from a seed, the same each run. A file a loader can read as what it is
/// is not refused. An empty script declares nothing, a project or a player with no file of its own
/// starts from the defaults, and a file the asset server reads cut short may be a shorter one.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class BadFileTests : IDisposable
{
    // Under the tests' asset folder, the one the asset server reads from.
    private readonly TestFolder _folder = TestFolder.At(Path.Combine(EngineHarness.AssetDirectory, "bad-" + Guid.NewGuid().ToString("N")[..8]));
    private readonly string _was = Streaming.AssetRoot;

    public BadFileTests()
    {
        Streaming.AssetRoot = EngineHarness.AssetDirectory;
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        UserData.Root = EngineHarness.UserDirectory;
        Streaming.AssetRoot = _was;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
        _folder.Dispose();
    }

    /// <summary>The folder's name, which is its path under the asset root.</summary>
    private string Name => Path.GetFileName(_folder.Path);

    private static readonly string[] Kinds = ["missing", "empty", "cut short", "random"];

    public static TheoryData<string> OwnLoaders => new()
    {
        "scene file", "save", "mesh file", "material file", "data asset", "project settings", "persistent value", "pack",
    };

    public static TheoryData<string> EngineLoaders => new() { "image", "model", "sound" };

    /// <summary>
    /// The bad files of a kind, each by what is wrong with it, written under the folder with the
    /// extension given and a name of their own.
    /// </summary>
    private List<(string Kind, string Path)> BadFiles(string extension, byte[] good)
    {
        var random = new byte[4096];
        new Random(7).NextBytes(random);

        var made = new List<(string, string)>();
        foreach (var kind in Kinds)
        {
            var path = _folder.File($"{kind.Replace(' ', '-')}-{Guid.NewGuid().ToString("N")[..6]}{extension}");
            byte[]? bytes = kind switch
            {
                "empty" => [],
                "cut short" => good[..(good.Length / 3)],
                "random" => random,
                _ => null,
            };
            if (bytes is not null) File.WriteAllBytes(path, bytes);
            made.Add((kind, path));
        }

        return made;
    }

    /// <summary>
    /// What is wrong with how a loader answered a bad file, or nothing, which is no exception and a
    /// problem naming the file, or no problem where it read the file as what it is.
    /// </summary>
    /// <param name="load">
    /// The load, answering with the problem it says, or nothing for none.
    /// </param>
    private static string? Judge(string kind, string named, Func<string?> load, bool refuses = true)
    {
        try
        {
            return load() switch
            {
                null => refuses ? $"{kind}: answered as if the file were good" : null,
                var problem when problem.Contains(named, StringComparison.Ordinal) => null,
                var problem => $"{kind}: '{problem}' does not name {named}",
            };
        }
        catch (Exception error)
        {
            return $"{kind}: {error.GetType().Name}, {error.Message}";
        }
    }

    /// <summary>
    /// What a load answering with a handle said of a bad file, as the <see cref="AssetLoadFailed"/>
    /// it posts, or nothing where it gave a handle.
    /// </summary>
    private static string? Said(AssetHandle handle)
    {
        var said = AssetServer.TakeFailed(out var failed) ? failed.Reason : "gave no handle and said nothing";
        return handle.IsValid ? null : said;
    }

    [SkippableTheory]
    [MemberData(nameof(OwnLoaders))]
    public void ALoaderOfItsOwnAnswersABadFileWithAMessageNamingIt(string loader)
    {
        var wrong = loader switch
        {
            "scene file" => InSystem(ctx =>
            {
                ctx.Ecs.Add(ctx.Ecs.Spawn(), Transform.At(1f, 2f, 3f));
                SceneFile.Save(ctx.Ecs, _folder.File("good.scene.json"));
                return Each(".scene.json", File.ReadAllBytes(_folder.File("good.scene.json")), path => Unread(SceneFile.Load(ctx.Ecs, path)));
            }),
            "save" => InSystem(ctx =>
            {
                var playing = ctx.Ecs.Spawn();
                ctx.Ecs.Add(playing, new SaveId());
                SaveGame.Save(ctx.Ecs, _folder.File("good.save.json"));
                var wrong = Each(".save.json", File.ReadAllBytes(_folder.File("good.save.json")), path => Unread(SaveGame.Load(ctx.Ecs, path)));
                if (!ctx.Ecs.IsAlive(playing)) wrong.Add("a bad save ended the game in progress");
                return wrong;
            }),
            "mesh file" => Rendered(() => InSystem(ctx =>
            {
                MeshFiles.SaveAs(Render.CreateMesh(MeshShape.Cylinder, 0.1f, 2f), $"{Name}/good{MeshFiles.Extension}");
                var good = File.ReadAllBytes(_folder.File($"good{MeshFiles.Extension}"));
                return
                [
                    .. Each(MeshFiles.Extension, good, path => Said(MeshFiles.Load(Under(path)))),
                    .. Each(MeshFiles.Extension, good, path => MeshFiles.TryLoad(Under(path), out _, out var problem) ? null : problem),
                ];
            })),
            "material file" => Rendered(() => InSystem(ctx =>
            {
                MaterialFiles.SaveAs(Render.CreateMaterial(new MaterialSettings()), $"{Name}/good{MaterialFiles.Extension}");
                var good = File.ReadAllBytes(_folder.File($"good{MaterialFiles.Extension}"));
                return
                [
                    .. Each(MaterialFiles.Extension, good, path => Said(MaterialFiles.Load(Under(path)))),
                    .. Each(MaterialFiles.Extension, good, path => MaterialFiles.TryLoad(Under(path), out _, out var problem) ? null : problem),
                ];
            })),
            "data asset" => DataAsset(),
            "project settings" => ProjectFiles(),
            "persistent value" => Persistent(),
            "pack" => Pack(),
            _ => throw new ArgumentOutOfRangeException(nameof(loader)),
        };

        Assert.True(wrong.Count == 0, $"{loader}: {string.Join("; ", wrong)}");
    }

    /// <summary>A loader run over each bad file of a kind, and what was wrong with its answers.</summary>
    private List<string> Each(string extension, byte[] good, Func<string, string?> load) =>
        [.. BadFiles(extension, good).Select(bad => Judge(bad.Kind, Path.GetFileName(bad.Path), () => load(bad.Path))).OfType<string>()];

    /// <summary>
    /// A file's path under the asset root, as the mesh and material files take it.
    /// </summary>
    private string Under(string path) => $"{Name}/{Path.GetFileName(path)}";

    /// <summary>
    /// Why a scene or a save was not read, or a line saying it spawned along with the problem.
    /// </summary>
    private static string? Unread(SceneLoad load) =>
        load.Problem is not null && load.Entities.Count > 0 ? $"spawned {load.Entities.Count} entities of a file it said was bad" : load.Problem;

    /// <summary>A load that needs a world, run in a system.</summary>
    private static List<string> InSystem(Func<BehaviorContext, List<string>> load)
    {
        using var harness = new EngineHarness(frames: 2);
        List<string> wrong = [];
        harness.OnContext(Stage.Startup, ctx => wrong = load(ctx));
        harness.Run();
        return wrong;
    }

    /// <summary>A load that needs the renderer, skipped without one.</summary>
    private static List<string> Rendered(Func<List<string>> load)
    {
        Needs.Renderer();
        return load();
    }

    /// <summary>
    /// A project's settings read from a folder whose project file is each bad one in turn, a folder
    /// with none being a project at its defaults.
    /// </summary>
    private List<string> ProjectFiles()
    {
        var good = _folder.File("project");
        Directory.CreateDirectory(good);
        new ProjectSettings { FixedHz = 30 }.Write(Path.Combine(good, ProjectSettings.FileName));

        var wrong = new List<string>();
        foreach (var (kind, path) in BadFiles(".json", File.ReadAllBytes(Path.Combine(good, ProjectSettings.FileName))))
        {
            var folder = _folder.File("project-" + kind.Replace(' ', '-'));
            Directory.CreateDirectory(folder);
            if (File.Exists(path)) File.Move(path, Path.Combine(folder, ProjectSettings.FileName));

            var named = Path.Combine(Path.GetFileName(folder), ProjectSettings.FileName);
            if (Judge(kind, named, () => ProjectSettings.ReadFrom(folder).Problem, refuses: kind != "missing") is { } answer) wrong.Add(answer);
        }

        return wrong;
    }

    /// <summary>
    /// A data asset whose file is replaced by each bad one in turn, read through its reference, as a
    /// component holding one reads it, and through <c>TryGet</c>.
    /// </summary>
    private List<string> DataAsset()
    {
        var sword = DataAssets.Create<WeaponStats>($"{Name}/sword.data.json");
        DataAssets.Save(sword, new WeaponStats { Damage = 55f });
        var good = File.ReadAllBytes(_folder.File("sword.data.json"));
        while (AssetServer.TakeFailed(out _)) { }

        var wrong = new List<string>();
        foreach (var (kind, path) in BadFiles(".bytes", good))
        {
            if (File.Exists(path)) File.Copy(path, _folder.File("sword.data.json"), overwrite: true);
            else File.Delete(_folder.File("sword.data.json"));
            DataAssets.Reload(sword.Id);

            // A reference whose file is gone has its id alone to be named by.
            var named = kind == "missing" ? sword.Id.ToString("x16") : "sword.data.json";
            if (Judge(kind, named, () => DataAssets.TryGet(sword, out _, out var problem) ? null : problem) is { } tried) wrong.Add(tried);

            // The type's defaults, and the message said as a handle's load says it.
            if (Judge(kind, named, () => sword.Value.Damage == new WeaponStats().Damage && AssetServer.TakeFailed(out var failed) ? failed.Reason : null) is { } read)
                wrong.Add($"through the reference, {read}");
        }

        return wrong;
    }

    /// <summary>
    /// A player's settings read from a bad file, which fall back to their default and say why,
    /// naming the file.
    /// </summary>
    private List<string> Persistent()
    {
        UserData.Root = _folder.Path;
        var settings = new Persistent<Settings>("good", SettingsJson.Default.Settings, () => new Settings());
        settings.Update(value => value with { Volume = 0.25f });
        settings.Persist();

        var wrong = new List<string>();
        foreach (var (kind, path) in BadFiles(".json", File.ReadAllBytes(settings.FullPath)))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            try
            {
                var read = new Persistent<Settings>(name, SettingsJson.Default.Settings, () => new Settings());
                if (read.Value.Volume != new Settings().Volume) wrong.Add($"{kind}: not the default");
                if (kind != "missing" && read.Problem?.Contains(Path.GetFileName(path), StringComparison.Ordinal) != true)
                    wrong.Add($"{kind}: the problem '{read.Problem}' does not name the file");
            }
            catch (Exception error)
            {
                wrong.Add($"{kind}: {error.GetType().Name}, {error.Message}");
            }
        }

        return wrong;
    }

    /// <summary>A pack opened from a bad file, which says what is wrong with it and holds no file open.</summary>
    private List<string> Pack()
    {
        Directory.CreateDirectory(_folder.File("source"));
        File.WriteAllText(_folder.File("source/top.txt"), "at the top");
        AssetPack.Write(_folder.File("source"), _folder.File(AssetPack.DefaultName));

        return Each(".pak", File.ReadAllBytes(_folder.File(AssetPack.DefaultName)), path =>
        {
            if (!AssetPack.TryOpen(path, out var pack, out var problem)) return problem;

            pack.Dispose();
            return null;
        });
    }

    [Fact]
    [ExpectsError("assets", "cut.pack")]
    public void AnAppWhosePackDoesNotOpenRunsWithoutItAndSaysWhy()
    {
        // The pack's first bytes and nothing after, as a copy that stopped leaves one.
        var pack = _folder.File("cut.pack");
        File.WriteAllBytes(pack, "BCSPACK\0"u8.ToArray());

        var frames = 0;
        using var harness = new EngineHarness(frames: 2, pack: pack);
        harness.On(Stage.Update, _ => frames++);
        harness.Run();

        Assert.True(frames > 0, "the app did not run");
        Assert.False(AssetFiles.CarriesFiles, "the app reads from a pack that did not open");
    }

    [SkippableTheory]
    [MemberData(nameof(EngineLoaders))]
    [ExpectsError("bevy", "bad-")]
    public void ALoadThroughTheAssetServerFailsABadFileAndSaysWhichItWas(string loader)
    {
        var (source, kind) = loader switch
        {
            "image" => ("textures/checker.png", AssetKind.Image),
            "model" => ("models/triangle.gltf", AssetKind.Gltf),
            "sound" => ("sounds/beep.wav", AssetKind.Audio),
            _ => throw new ArgumentOutOfRangeException(nameof(loader)),
        };
        if (kind != AssetKind.Image) Needs.Renderer();

        var bad = BadFiles(Path.GetExtension(source), File.ReadAllBytes(Path.Combine(EngineHarness.AssetDirectory, source)));
        var handles = new Dictionary<string, AssetHandle>();
        var states = new Dictionary<string, AssetLoadState>();
        var told = new HashSet<string>(StringComparer.Ordinal);

        // The loads run on IO threads, so the frames they take are the machine's, and a count of
        // frames at a pace stops one that never ends.
        using var harness = new EngineHarness(frames: 0, fps: 240);
        harness.OnContext(Stage.Startup, _ =>
        {
            foreach (var (what, path) in bad) handles[what] = AssetServer.Load(kind, $"{Name}/{Path.GetFileName(path)}");
        });
        harness.OnContext(Stage.Last, ctx =>
        {
            foreach (var failure in ctx.Read<AssetLoadFailed>()) told.Add(failure.Path);
            foreach (var (what, handle) in handles) states[what] = handle.State;

            var done = states.Values.All(state => state is AssetLoadState.Loaded or AssetLoadState.Failed);
            var failed = states.Values.Count(state => state == AssetLoadState.Failed);
            if ((done && told.Count >= failed) || ctx.Time.FrameCount > 2400) ctx.Exit();
        });
        harness.Run();

        var wrong = new List<string>();
        foreach (var (what, path) in bad)
        {
            // A file cut short may still be read as a shorter one, a third of a sound playing a
            // third, and is wrong only where it fails unnamed.
            var state = states.GetValueOrDefault(what);
            if (what == "cut short" && state == AssetLoadState.Loaded) continue;

            if (state != AssetLoadState.Failed) wrong.Add($"{what}: {state}");
            if (!told.Any(said => said.Contains(Path.GetFileName(path), StringComparison.Ordinal))) wrong.Add($"{what}: no AssetLoadFailed named it");
        }

        Assert.True(wrong.Count == 0, $"{loader}: {string.Join("; ", wrong)}");
    }

    [SkippableFact]
    [ExpectsError("bevy", "bad-")]
    public void AShaderProgramFromABadFileFailsAndItsDiagnosticsNameIt()
    {
        Needs.Shaders();

        var bad = BadFiles(".slang", File.ReadAllBytes(Path.Combine(EngineHarness.AssetDirectory, "shaders/flat.slang")));
        var programs = new Dictionary<string, ShaderProgram>();
        var answers = new Dictionary<string, (ShaderProgramState State, string Diagnostics)>();

        new PictureRun
        {
            Scene = _ =>
            {
                foreach (var (what, path) in bad) programs[what] = Shaders.CreateProgram($"{Name}/{Path.GetFileName(path)}");
            },
        }
            .Until("compiling", _ => programs.Values.All(program => program.State != ShaderProgramState.Compiling))
            .Do("reading", _ =>
            {
                foreach (var (what, program) in programs) answers[what] = (program.State, program.Diagnostics);
            })
            .Go();

        var wrong = new List<string>();
        foreach (var (what, path) in bad)
        {
            var (state, diagnostics) = answers[what];
            if (state != ShaderProgramState.Failed) wrong.Add($"{what}: {state}");
            if (!diagnostics.Contains(Path.GetFileName(path), StringComparison.Ordinal)) wrong.Add($"{what}: '{diagnostics}' does not name the file");
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    [Fact]
    public void AScriptFromABadFileFailsToCompileAndTheErrorNamesIt()
    {
        // Its declarations first, so a third of it ends inside one.
        const string Good = """
            [Bevy.Behavior]
            public partial struct Spin
            {
                public float Speed;

                [Bevy.OnUpdate]
                public void Turn(Bevy.BehaviorContext ctx, ref Bevy.Transform place) =>
                    place.Rotation *= Bevy.Quat.FromAxisAngle(Bevy.Vec3.UnitY, Speed * ctx.Time.Delta);
            }
            """;

        var wrong = new List<string>();
        foreach (var (kind, path) in BadFiles(".cs", System.Text.Encoding.UTF8.GetBytes(Good)))
        {
            // Each in a folder of its own, as a game's scripts are.
            var scripts = _folder.File("scripts-" + Path.GetFileNameWithoutExtension(path));
            Directory.CreateDirectory(scripts);
            if (File.Exists(path)) File.Move(path, Path.Combine(scripts, Path.GetFileName(path)));

            using var harness = new EngineHarness(frames: 1);
            harness.App.EnableDynamicSystems();
            var host = new ScriptHost(harness.App, scripts);
            try
            {
                var built = host.Reload();
                var named = kind == "missing" ? scripts : Path.GetFileName(path);
                if (kind == "empty")
                {
                    if (!built) wrong.Add($"empty: refused, {host.LastError}");
                }
                else if (built) wrong.Add($"{kind}: compiled");
                else if (host.LastError?.Contains(named, StringComparison.Ordinal) != true) wrong.Add($"{kind}: '{host.LastError}' does not name {named}");
            }
            catch (Exception error)
            {
                wrong.Add($"{kind}: {error.GetType().Name}, {error.Message}");
            }
            finally
            {
                host.Retire();
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }
}

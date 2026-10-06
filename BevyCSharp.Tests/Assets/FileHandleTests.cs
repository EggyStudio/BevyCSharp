using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Each loader lets go of its file once it has read it, as N 2.9 of NORM.md has it, which only a
/// Windows run could see by chance before, a delete there failing on a file still open.
/// </summary>
/// <remarks>
/// <para>
/// A file read and left open is a file a game cannot save over while it runs, and on Windows one a
/// child process started meanwhile inherits, which ended a run of 3DEngine's. Linux deletes an open
/// file without complaint, so the test asks the system which files are open, under
/// <c>/proc/self/fd</c>, and on Windows opens each for writing with no sharing, which fails while
/// anything holds it.
/// </para>
/// <para>
/// Every file in a folder of the test's own is looked for, rather than the one a loader was given,
/// so what a loader writes beside the file it reads is found as well. A load through the engine is
/// looked at while the app still runs, since an app that stops lets go of everything.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class FileHandleTests : IDisposable
{
    // Under the tests' asset folder, the one the asset server reads from.
    private readonly TestFolder _folder = TestFolder.At(Path.Combine(EngineHarness.AssetDirectory, "handles-" + Guid.NewGuid().ToString("N")[..8]));
    private readonly string _was = Streaming.AssetRoot;

    public FileHandleTests()
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

    public static TheoryData<string> Loaders => new()
    {
        "image by the asset server", "model by the asset server", "sound by the asset server",
        "scene file", "mesh file", "material file", "data asset", "asset id", "project settings",
        "persistent value", "streamed read", "pack, once disposed", "script", "shader program",
    };

    [SkippableTheory]
    [MemberData(nameof(Loaders))]
    public void ALoaderHoldsNoFileOpenOnceItHasReadIt(string loader)
    {
        Needs.OpenFiles();

        var held = loader switch
        {
            "image by the asset server" => ByServer(["textures/checker.png"], files => AssetServer.Load(AssetKind.Image, files[0])),
            "model by the asset server" => Rendered(() => ByServer(["models/triangle.gltf"], files => AssetServer.LoadGltfScene(files[0], 0))),
            "sound by the asset server" => Rendered(() => ByServer(["sounds/beep.wav"], files => AssetServer.Load(AssetKind.Audio, files[0]))),
            "scene file" => InSystem(ctx =>
            {
                ctx.Ecs.Add(ctx.Ecs.Spawn(), Transform.At(1f, 2f, 3f));
                SceneFile.Save(ctx.Ecs, _folder.File("yard.scene.json"));
                File.Copy(_folder.File("yard.scene.json"), _folder.File("court.scene.json"));
                Assert.NotEmpty(SceneFile.Load(ctx.Ecs, _folder.File("court.scene.json")).Entities);
            }),
            "mesh file" => Rendered(() => InSystem(_ =>
            {
                MeshFiles.SaveAs(Render.CreateMesh(MeshShape.Cylinder, 0.1f, 2f), $"{Name}/post{MeshFiles.Extension}");
                File.Copy(_folder.File($"post{MeshFiles.Extension}"), _folder.File($"pole{MeshFiles.Extension}"));
                Assert.True(MeshFiles.Load($"{Name}/pole{MeshFiles.Extension}").IsValid);
            })),
            "material file" => Rendered(() => InSystem(_ =>
            {
                MaterialFiles.SaveAs(Render.CreateMaterial(new MaterialSettings()), $"{Name}/paint{MaterialFiles.Extension}");
                File.Copy(_folder.File($"paint{MaterialFiles.Extension}"), _folder.File($"coat{MaterialFiles.Extension}"));
                Assert.True(MaterialFiles.Load($"{Name}/coat{MaterialFiles.Extension}").IsValid);
            })),
            "data asset" => Read(() =>
            {
                var sword = DataAssets.Create<WeaponStats>($"{Name}/sword.data.json");
                DataAssets.ReloadAll();
                Assert.Equal(10f, sword.Value.Damage);
            }),
            "asset id" => Read(() =>
            {
                File.WriteAllText(_folder.File("ship.glb"), "a model");
                var id = AssetIds.IdOf($"{Name}/ship.glb", create: true);
                AssetIds.Reindex();
                Assert.Equal(id, AssetIds.IdOf($"{Name}/ship.glb"));
            }),
            "project settings" => Read(() =>
            {
                new ProjectSettings { FixedHz = 30 }.Write(_folder.File(ProjectSettings.FileName));
                Assert.Equal(30d, ProjectSettings.ReadFrom(_folder.Path).FixedHz);
            }),
            "persistent value" => Read(() =>
            {
                UserData.Root = _folder.Path;
                var settings = new Persistent<Settings>("settings", SettingsJson.Default.Settings, () => new Settings());
                settings.Update(value => value with { Volume = 0.25f });
                settings.Persist();
                Assert.Equal(0.25f, new Persistent<Settings>("settings", SettingsJson.Default.Settings, () => new Settings()).Value.Volume);
            }),
            "streamed read" => Streamed(),
            "pack, once disposed" => Read(() =>
            {
                Directory.CreateDirectory(_folder.File("source"));
                File.WriteAllText(_folder.File("source/top.txt"), "at the top");
                AssetPack.Write(_folder.File("source"), _folder.File(AssetPack.DefaultName));
                using var pack = AssetPack.Open(_folder.File(AssetPack.DefaultName));
                using var reader = new StreamReader(pack.OpenFile("top.txt")!);
                Assert.Equal("at the top", reader.ReadToEnd());
            }),
            "script" => Script(),
            "shader program" => Shader(),
            _ => throw new ArgumentOutOfRangeException(nameof(loader)),
        };

        Assert.True(held.Count == 0, $"{loader} read and holds {string.Join(", ", held)}");
    }

    [SkippableFact]
    public void AFileLeftOpenIsFoundHeldAndNotOnceClosed()
    {
        Needs.OpenFiles();

        // The check itself, which would pass every loader if it found nothing.
        File.WriteAllBytes(_folder.File("open.bin"), [1]);
        using (File.OpenRead(_folder.File("open.bin")))
            Assert.Equal(new[] { _folder.File("open.bin") }, Held());
        Assert.Empty(Held());
    }

    /// <summary>The files a load that needs no engine holds once it is done.</summary>
    private List<string> Read(Action load)
    {
        load();
        return Held();
    }

    /// <summary>A load that needs the renderer, skipped without one.</summary>
    private static List<string> Rendered(Func<List<string>> load)
    {
        Needs.Renderer();
        return load();
    }

    /// <summary>The files held once a system's load is done, looked at in the same system.</summary>
    private List<string> InSystem(Action<BehaviorContext> load)
    {
        using var harness = new EngineHarness(frames: 2);
        List<string> held = [];
        harness.OnContext(Stage.Startup, ctx =>
        {
            load(ctx);
            held = Held();
        });
        harness.Run();
        return held;
    }

    /// <summary>
    /// The examples' files copied into the folder, loaded through the asset server, and the files
    /// held once the load is done, looked at in the frame it is.
    /// </summary>
    private List<string> ByServer(string[] sources, Func<string[], AssetHandle> load)
    {
        var names = sources.Select(source =>
        {
            File.Copy(Path.Combine(EngineHarness.AssetDirectory, source), _folder.File(Path.GetFileName(source)));
            return $"{Name}/{Path.GetFileName(source)}";
        }).ToArray();

        // The load runs on an IO thread, so the frames it takes are the machine's, and a count of
        // frames at a pace stops one that never ends.
        using var harness = new EngineHarness(frames: 0, fps: 240);
        var handle = AssetHandle.None;
        var state = AssetLoadState.Unknown;
        List<string> held = [];
        harness.OnContext(Stage.Startup, _ => handle = load(names));
        harness.OnContext(Stage.Last, ctx =>
        {
            state = handle.State;
            if (state == AssetLoadState.Loaded) held = Held();
            if (state is AssetLoadState.Loaded or AssetLoadState.Failed || ctx.Time.FrameCount > 2400) ctx.Exit();
        });
        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, state);
        return held;
    }

    /// <summary>A part of a file read on a worker thread, and the files held once it is handed over.</summary>
    private List<string> Streamed()
    {
        File.WriteAllBytes(_folder.File("numbers.bin"), [.. Enumerable.Range(0, 64).Select(value => (byte)value)]);

        using var harness = new EngineHarness(frames: 0, fps: 240);
        var read = default(StreamRead);
        byte[]? bytes = null;
        List<string> held = [];
        harness.OnContext(Stage.Update, ctx =>
        {
            if (read.Ticket == 0) read = Streaming.Read($"{Name}/numbers.bin", offset: 8, length: 8);
            else if (Streaming.TryTake(read, out var arrived)) (bytes, held) = (arrived, Held());

            if (bytes is not null || ctx.Time.FrameCount > 2400) ctx.Exit();
        });
        harness.Run();

        Assert.Equal(Enumerable.Range(8, 8).Select(value => (byte)value), bytes);
        return held;
    }

    /// <summary>A behavior script compiled while the app is made, and the files held once it is in.</summary>
    private List<string> Script()
    {
        Directory.CreateDirectory(_folder.File("scripts"));
        File.WriteAllText(_folder.File("scripts/Spin.cs"), """
            using Bevy;

            namespace Handles;

            [Behavior]
            public partial struct Spin
            {
                public float Speed;
            }
            """);

        using var harness = new EngineHarness(frames: 2);
        harness.App.EnableDynamicSystems();
        var host = new ScriptHost(harness.App, _folder.File("scripts"));
        Assert.True(host.Reload(), host.LastError);
        var held = Held();

        harness.Run();
        host.Retire();
        return held;
    }

    /// <summary>
    /// A program compiled from a Slang file by slangc, in an app that draws, since a headless one
    /// has no renderer to make it, and the files held once it is ready.
    /// </summary>
    private List<string> Shader()
    {
        Needs.Shaders();
        File.Copy(Path.Combine(EngineHarness.AssetDirectory, "shaders/flat.slang"), _folder.File("flat.slang"));

        var program = default(ShaderProgram);
        List<string> held = [];
        new PictureRun { Scene = _ => program = Shaders.CreateProgram($"{Name}/flat.slang") }
            .Until("compiling", _ => program.State != ShaderProgramState.Compiling)
            .Do("looking", _ =>
            {
                Assert.Equal(ShaderProgramState.Ready, program.State);
                held = Held();
            })
            .Go();
        return held;
    }

    /// <summary>
    /// The files in the folder this process has open, which Linux says by a descriptor under
    /// /proc/self/fd naming one and Windows by refusing to open one alone.
    /// </summary>
    private List<string> Held()
    {
        if (OperatingSystem.IsWindows())
        {
            return Directory.EnumerateFiles(_folder.Path, "*", SearchOption.AllDirectories).Where(file =>
            {
                try
                {
                    using var alone = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    return false;
                }
                catch (IOException)
                {
                    return true;
                }
            }).Order(StringComparer.Ordinal).ToList();
        }

        // A descriptor's target is the real path, which may differ from the folder's by a link
        // above it, so it is matched by the folder's unique name and given back in the folder.
        var within = $"/{Name}/";
        var held = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var descriptor in Directory.EnumerateFileSystemEntries("/proc/self/fd"))
        {
            string? target;
            try
            {
                target = new FileInfo(descriptor).LinkTarget;
            }
            catch (IOException)
            {
                // Closed between being listed and being read.
                continue;
            }

            if (target is not null && target.IndexOf(within, StringComparison.Ordinal) is var at and >= 0)
                held.Add(Path.Combine(_folder.Path, target[(at + within.Length)..]));
        }

        return [.. held];
    }
}

using System.Net;
using System.Security.Cryptography;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers scene packs, their manifests read or refused, a pack fetched from a file and over HTTP and
/// kept only where its hash is the manifest's, and a fetched pack mounted for its model.
/// </summary>
/// <remarks>
/// The folder packs are kept in and the files mounted are the process's, so these share the engine
/// collection and put both back.
/// </remarks>
[Collection("engine")]
public sealed class ScenePackTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-scene-packs-");

    public ScenePackTests() => ScenePacks.Folder = _folder.File("cache");

    public void Dispose()
    {
        ScenePacks.Folder = null!;
        AssetFiles.Use(null);
        _folder.Dispose();
    }

    [Fact]
    public void AManifestIsReadByItsFileNameAndOneMissingAFieldIsSaidNotThrown()
    {
        var manifests = _folder.File("scenes");
        Directory.CreateDirectory(manifests);
        File.WriteAllText(Path.Combine(manifests, "hall.json"), """
            {
              // A comment, as a person writing one by hand may leave.
              "title": "A hall",
              "license": "CC BY 4.0",
              "url": "https://example.com/hall.pack",
              "size": 10,
              "sha256": "0000000000000000000000000000000000000000000000000000000000000000",
              "model": "hall.gltf"
            }
            """);
        File.WriteAllText(Path.Combine(manifests, "no-hash.json"), """{ "url": "https://example.com/a.pack", "size": 10, "model": "a.gltf" }""");
        File.WriteAllText(Path.Combine(manifests, "broken.json"), "{ not json");

        var packs = ScenePacks.List(manifests, out var problems);

        var hall = Assert.Single(packs);
        Assert.Equal(("hall", "A hall", "hall.gltf"), (hall.Name, hall.Title, hall.Model));
        Assert.Equal(2, problems.Count);
        Assert.Contains(problems, problem => problem.Contains("broken.json", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("no-hash.json", StringComparison.Ordinal) && problem.Contains("SHA-256", StringComparison.Ordinal));
        Assert.Empty(ScenePacks.List(_folder.File("nowhere"), out _));
    }

    [Fact]
    public async Task APackIsFetchedFromAFileAndKeptOnlyWhereItsHashIsTheManifests()
    {
        var (packed, manifest) = Packed();
        var reported = new List<double>();

        Assert.False(ScenePacks.IsFetched(manifest));
        Assert.Null(await ScenePacks.FetchAsync(manifest, new Progress(reported)));
        Assert.True(ScenePacks.IsFetched(manifest));
        Assert.Equal(File.ReadAllBytes(packed), File.ReadAllBytes(ScenePacks.PathOf(manifest)));
        Assert.Equal(1.0, reported[^1]);

        // A manifest whose hash is another's keeps nothing new, and the pack fetched before stays.
        var wrong = manifest with { Sha256 = new string('a', 64) };
        var problem = await ScenePacks.FetchAsync(wrong);
        Assert.NotNull(problem);
        Assert.Contains("SHA-256", problem, StringComparison.Ordinal);
        Assert.True(ScenePacks.IsFetched(manifest));
        Assert.False(File.Exists(ScenePacks.PathOf(manifest) + ".part"));

        // An address with nothing at it is answered, naming it.
        var missing = manifest with { Name = "missing", Url = _folder.File("missing.pack") };
        Assert.Contains("missing.pack", await ScenePacks.FetchAsync(missing), StringComparison.Ordinal);
        Assert.False(ScenePacks.IsFetched(missing));
    }

    [Fact]
    public async Task APackIsFetchedOverHttp()
    {
        var (packed, manifest) = Packed();
        var bytes = File.ReadAllBytes(packed);

        // A server on this machine that answers every request with the pack, as a release asset's
        // address does once it has redirected.
        using var server = new HttpListener();
        var port = FreePort();
        server.Prefixes.Add($"http://127.0.0.1:{port}/");
        server.Start();
        var serving = Task.Run(async () =>
        {
            var context = await server.GetContextAsync();
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        });

        var problem = await ScenePacks.FetchAsync(manifest with { Url = $"http://127.0.0.1:{port}/scene.pack" });
        await serving;

        Assert.Null(problem);
        Assert.True(ScenePacks.IsFetched(manifest));
    }

    [Fact]
    public async Task AFetchedPackIsMountedForItsModelAndOneWithoutItIsRefused()
    {
        var (_, manifest) = Packed();
        Assert.False(ScenePacks.TryMount(manifest, out _, out var unfetched));
        Assert.Contains("has not been fetched", unfetched, StringComparison.Ordinal);

        Assert.Null(await ScenePacks.FetchAsync(manifest));
        Assert.True(ScenePacks.TryMount(manifest, out var model, out _));
        Assert.Equal("packs/scene/hall.gltf", model);
        Assert.Equal("a model", AssetFiles.ReadAllText(model));
        Assert.True(ScenePacks.Unmount(manifest));
        Assert.False(AssetFiles.Exists(model));

        var elsewhere = manifest with { Model = "elsewhere.gltf" };
        Assert.False(ScenePacks.TryMount(elsewhere, out _, out var problem));
        Assert.Contains("elsewhere.gltf", problem, StringComparison.Ordinal);
    }

    /// <summary>A pack of a model and its texture, and the manifest that names it.</summary>
    private (string Packed, ScenePack Manifest) Packed()
    {
        var source = _folder.File("source");
        Directory.CreateDirectory(Path.Combine(source, "textures"));
        File.WriteAllText(Path.Combine(source, "hall.gltf"), "a model");
        File.WriteAllText(Path.Combine(source, "textures", "wall.ktx2"), "a texture");

        var packed = _folder.File("scene.pack");
        AssetPack.Write(source, packed);

        var bytes = File.ReadAllBytes(packed);
        return (packed, new ScenePack
        {
            Name = "scene",
            Title = "A scene",
            Url = packed,
            Size = bytes.Length,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Model = "hall.gltf",
        });
    }

    private static int FreePort()
    {
        using var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    /// <summary>Takes each fraction as it is told, rather than on the thread pool as Progress does.</summary>
    private sealed class Progress(List<double> into) : IProgress<double>
    {
        public void Report(double value) => into.Add(value);
    }
}

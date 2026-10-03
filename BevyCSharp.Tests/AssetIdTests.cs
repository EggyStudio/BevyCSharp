using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers referring to any file under the asset root by its id as well as its path, so a scene
/// still finds a model or a texture after the file is renamed.
/// </summary>
/// <remarks>
/// No engine, since a file reference is written and read from paths and sidecars alone. Shares the
/// engine collection anyway, because the asset root it points elsewhere is static and the engine
/// tests read it.
/// </remarks>
[Collection("engine")]
public sealed class AssetIdTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-ids-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public AssetIdTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "models"));
        Streaming.AssetRoot = _root;
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        Streaming.AssetRoot = _was;
        AssetIds.Reindex();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AReferenceGivenAnIdFollowsTheFileWhenItMoves()
    {
        File.WriteAllText(Path.Combine(_root, "models/ship.glb"), "not really a model");

        using var written = Written(new SceneReferences { GiveIds = true }, "models/ship.glb#Mesh0");
        var reference = written.RootElement;

        // The id is the file's, so the label stays in the path, and the sidecar is beside the file.
        Assert.True(reference.TryGetProperty("uid", out _));
        Assert.Equal("models/ship.glb#Mesh0", reference.GetProperty("path").GetString());
        Assert.True(File.Exists(Path.Combine(_root, "models/ship.glb.uid")));

        AssetIds.Move("models/ship.glb", "fleet/flagship.glb");
        Assert.Equal("fleet/flagship.glb#Mesh0", SceneReferences.ReadFile(reference));

        // Renamed outside the editor, with its sidecar carried along by hand, it is found again.
        File.Move(Path.Combine(_root, "fleet/flagship.glb"), Path.Combine(_root, "hull.glb"));
        File.Move(Path.Combine(_root, "fleet/flagship.glb.uid"), Path.Combine(_root, "hull.glb.uid"));
        Assert.Equal("hull.glb#Mesh0", SceneReferences.ReadFile(reference));
    }

    [Fact]
    public void WithoutAnIdAReferenceIsItsPath()
    {
        File.WriteAllText(Path.Combine(_root, "models/rock.glb"), "not really a model");

        // Not asked to give ids, so no sidecar is written and the reference holds the path alone.
        using var written = Written(new SceneReferences(), "models/rock.glb");
        Assert.False(written.RootElement.TryGetProperty("uid", out _));
        Assert.False(File.Exists(Path.Combine(_root, "models/rock.glb.uid")));
        Assert.Equal("models/rock.glb", SceneReferences.ReadFile(written.RootElement));

        // A file that is not there, or one from another source, is never given one.
        using var missing = Written(new SceneReferences { GiveIds = true }, "models/gone.glb");
        Assert.False(missing.RootElement.TryGetProperty("uid", out _));
        using var embedded = Written(new SceneReferences { GiveIds = true }, "embedded://cube.glb");
        Assert.False(embedded.RootElement.TryGetProperty("uid", out _));

        // A bare string is how a scene wrote a file before ids, and still reads.
        using var old = JsonDocument.Parse("\"models/rock.glb\"");
        Assert.Equal("models/rock.glb", SceneReferences.ReadFile(old.RootElement));

        // An id no sidecar holds falls back to the path, so a lost sidecar costs only the rename.
        using var lost = JsonDocument.Parse("""{ "uid": "00000000000000aa", "path": "models/rock.glb" }""");
        Assert.Equal("models/rock.glb", SceneReferences.ReadFile(lost.RootElement));
    }

    [Fact]
    public void AShippedGameFindsFilesThroughTheIndexAlone()
    {
        File.WriteAllText(Path.Combine(_root, "models/ship.glb"), "not really a model");
        var id = AssetIds.IdOf("models/ship.glb", create: true);

        Assert.Equal(1, AssetIds.WriteIndex());

        // What an export leaves, the index without the sidecars.
        File.Delete(Path.Combine(_root, "models/ship.glb.uid"));
        AssetIds.Reindex();

        Assert.Equal("models/ship.glb", AssetIds.PathOf(id));
        Assert.Equal(id, AssetIds.IdOf("models/ship.glb"));

        // A file the index names that is not shipped is not answered with.
        File.Delete(Path.Combine(_root, "models/ship.glb"));
        AssetIds.Reindex();
        Assert.Null(AssetIds.PathOf(id));
    }

    /// <summary>A file reference as a scene would write it.</summary>
    private static JsonDocument Written(SceneReferences references, string path)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer)) references.WriteFile(json, path);
        return JsonDocument.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}

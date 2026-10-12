using System.Buffers.Binary;
using System.Text;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers listing what a glTF file holds from its JSON, as Bevy labels the parts, for both the text
/// and the binary form.
/// </summary>
[Collection("engine")]
public sealed class GltfContentsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-gltf-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    private const string Json = """
        { "asset": { "version": "2.0" },
          "meshes": [ { "name": "Hull", "primitives": [ {}, {} ] }, { "primitives": [ {} ] } ],
          "materials": [ { "name": "Steel" }, {} ] }
        """;

    public GltfContentsTests()
    {
        Directory.CreateDirectory(_root);
        Streaming.AssetRoot = _root;
    }

    public void Dispose()
    {
        Streaming.AssetRoot = _was;
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ATextFileListsEveryPrimitiveAndMaterialByItsLabel()
    {
        File.WriteAllText(Path.Combine(_root, "ship.gltf"), Json);

        var parts = GltfContents.Read("ship.gltf");

        Assert.NotNull(parts);
        Assert.Equal(
            [
                new GltfPart("Mesh0/Primitive0", "Hull 1", AssetKind.Mesh),
                new GltfPart("Mesh0/Primitive1", "Hull 2", AssetKind.Mesh),
                new GltfPart("Mesh1/Primitive0", "Mesh 1", AssetKind.Mesh),
                new GltfPart("Material0/std", "Steel", AssetKind.StandardMaterial),
                new GltfPart("Material1/std", "Material 1", AssetKind.StandardMaterial),
            ],
            parts);
    }

    [Fact]
    public void ABinaryFileIsReadFromItsJsonChunk()
    {
        // A header, then the JSON chunk padded to four bytes, as the format lays them out.
        var json = Encoding.UTF8.GetBytes(Json);
        var padded = (json.Length + 3) & ~3;
        var file = new byte[12 + 8 + padded];
        Encoding.ASCII.GetBytes("glTF").CopyTo(file, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)file.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)padded);
        Encoding.ASCII.GetBytes("JSON").CopyTo(file, 16);
        json.CopyTo(file, 20);
        for (var i = 20 + json.Length; i < file.Length; i++) file[i] = (byte)' ';
        File.WriteAllBytes(Path.Combine(_root, "ship.glb"), file);

        var parts = GltfContents.Read("ship.glb");
        Assert.NotNull(parts);
        Assert.Equal(5, parts.Count);
        Assert.Equal("Steel", parts[3].Name);

        // A file that is not glTF at all, or not there, lists nothing.
        File.WriteAllText(Path.Combine(_root, "broken.gltf"), "{ not json");
        Assert.Null(GltfContents.Read("broken.gltf"));
        Assert.Null(GltfContents.Read("missing.gltf"));
    }

    [Fact]
    public void ATextureInsideTheFileIsLabeledAndOneBesideItIsItsOwnFile()
    {
        Directory.CreateDirectory(Path.Combine(_root, "ships"));
        File.WriteAllText(Path.Combine(_root, "ships", "ship.gltf"), """
            { "asset": { "version": "2.0" },
              "images": [ { "uri": "data:image/png;base64,AAAA" },
                          { "uri": "paint/hull%20color.png" },
                          { "name": "Decal", "bufferView": 0, "mimeType": "image/png" },
                          { "uri": "../../outside.png" } ],
              "textures": [ { "source": 0 }, { "source": 1 }, { "name": "Stripes", "source": 2 },
                            { "source": 3 }, { "extensions": { "EXT_texture_webp": { "source": 2 } } } ] }
            """);

        var parts = GltfContents.Read("ships/ship.gltf");

        Assert.NotNull(parts);
        Assert.Equal(
            [
                new GltfPart("Texture0", "Texture 0", AssetKind.Image),
                new GltfPart("Texture1", "hull color", AssetKind.Image, "ships/paint/hull color.png"),
                new GltfPart("Texture2", "Stripes", AssetKind.Image),
                new GltfPart("Texture4", "Decal", AssetKind.Image),
            ],
            parts);

        // Bytes inside the file load by their label, and a file beside it loads as itself.
        Assert.Equal("ships/ship.gltf#Texture0", parts[0].PathIn("ships/ship.gltf"));
        Assert.Equal("ships/paint/hull color.png", parts[1].PathIn("ships/ship.gltf"));
    }
}

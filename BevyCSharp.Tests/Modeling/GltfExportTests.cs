using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers an edit mesh written as glTF 2.0 and read from it, a box out and back with its counts,
/// the written file held to the fields glTF's schema requires, the materials' colors and textures
/// both ways, the modifiers written as built, known glTF files read into faces, and what a file
/// that is no glTF is answered with.
/// </summary>
/// <remarks>
/// The writer is the engine's own, and so is the reader, so the box read back is the writer's
/// file through the reader's eyes, and the test models are files neither wrote. The schema's
/// required fields are read from the JSON here rather than through a validator, which the library
/// does not carry.
/// </remarks>
[Collection("engine")]
public sealed class GltfExportTests : IDisposable
{
    private const float Close = 1e-4f;

    private readonly TestFolder _folder = new("bcs-gltf-");

    public void Dispose() => _folder.Dispose();

    private static EditMesh Cube() => new EditMesh().Box(new Vec3(2f));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABoxWrittenAndReadBackHasTheSameCounts(bool embedded)
    {
        var path = Cube().Export(_folder.File("box.gltf"), embedded);
        Assert.Equal(!embedded, File.Exists(_folder.File("box.bin")));

        var back = new EditMesh().Import(path);
        Assert.Equal((8, 12, 6), (back.VertexCount, back.EdgeCount, back.FaceCount));
        Assert.True(back.IsClosed);
        Assert.Equal(8f, back.Volume, Close);
        Assert.Equal((Vec3.Zero, new Vec3(2f)), back.Bounds);
        Assert.Equal(6, back.SelectedFaceCount);

        // Each face keeps its texture coordinates, the box projection's meters.
        for (var face = 0; face < back.FaceCount; face++)
        {
            Assert.True(back.HasOwnUvs(face));
            Assert.Equal(Cube().UvsOf(0).Select(uv => MathF.Abs(uv.X) + MathF.Abs(uv.Y)).Sum(), back.UvsOf(face).Select(uv => MathF.Abs(uv.X) + MathF.Abs(uv.Y)).Sum(), 4f);
        }
    }

    [Fact]
    public void TheFileHoldsEveryFieldTheSchemaRequires()
    {
        var mesh = Cube().SelectFacing(Vec3.UnitY).Material("ceiling").SlotTexture("ceiling", "textures/checker.png");
        var path = mesh.Export(_folder.File("held.gltf"));

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Assert.Equal("2.0", root.GetProperty("asset").GetProperty("version").GetString());

        var bin = File.ReadAllBytes(_folder.File("held.bin"));
        var buffer = root.GetProperty("buffers")[0];
        Assert.Equal(bin.Length, buffer.GetProperty("byteLength").GetInt32());
        Assert.Equal("held.bin", buffer.GetProperty("uri").GetString());

        var views = root.GetProperty("bufferViews").EnumerateArray().ToArray();
        foreach (var view in views)
        {
            Assert.Equal(0, view.GetProperty("buffer").GetInt32());
            var (offset, length) = (view.GetProperty("byteOffset").GetInt32(), view.GetProperty("byteLength").GetInt32());
            Assert.Equal(0, offset % 4);
            Assert.InRange(offset + length, 1, bin.Length);
        }

        var accessors = root.GetProperty("accessors").EnumerateArray().ToArray();
        foreach (var accessor in accessors)
        {
            var count = accessor.GetProperty("count").GetInt32();
            var type = accessor.GetProperty("type").GetString();
            var width = type switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, _ => throw new InvalidDataException(type) };
            Assert.Contains(accessor.GetProperty("componentType").GetInt32(), new[] { 5125, 5126 });
            Assert.True(count >= 1);
            Assert.Equal(count * width * 4, views[accessor.GetProperty("bufferView").GetInt32()].GetProperty("byteLength").GetInt32());
        }

        var primitives = root.GetProperty("meshes")[0].GetProperty("primitives").EnumerateArray().ToArray();
        Assert.Equal(2, primitives.Length);
        var materials = root.GetProperty("materials").EnumerateArray().ToArray();
        Assert.Equal(new[] { "default", "ceiling" }, materials.Select(material => material.GetProperty("name").GetString()));

        foreach (var primitive in primitives)
        {
            // A position accessor gives its bounds, and every index names a vertex it has.
            var positions = accessors[primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32()];
            Assert.Equal(3, positions.GetProperty("min").GetArrayLength());
            Assert.Equal(3, positions.GetProperty("max").GetArrayLength());

            var indices = accessors[primitive.GetProperty("indices").GetInt32()];
            var view = views[indices.GetProperty("bufferView").GetInt32()];
            var start = view.GetProperty("byteOffset").GetInt32();
            for (var i = 0; i < indices.GetProperty("count").GetInt32(); i++)
                Assert.InRange(BitConverter.ToUInt32(bin, start + 4 * i), 0u, (uint)positions.GetProperty("count").GetInt32() - 1);

            Assert.InRange(primitive.GetProperty("material").GetInt32(), 0, materials.Length - 1);
            Assert.Equal(4, primitive.GetProperty("mode").GetInt32());
        }

        // The texture named by its path from the file's folder, to the file the slot names.
        var uri = root.GetProperty("images")[0].GetProperty("uri").GetString()!;
        Assert.True(File.Exists(Path.Combine(_folder.Path, Uri.UnescapeDataString(uri))) || !File.Exists(Path.Combine(AssetIds.Root, "textures/checker.png")));
        Assert.Equal(0, materials[1].GetProperty("pbrMetallicRoughness").GetProperty("baseColorTexture").GetProperty("index").GetInt32());
    }

    [Fact]
    public void ASlotsLookIsWrittenAndReadBackAsItWas()
    {
        var mesh = Cube()
            .SelectFacing(Vec3.UnitY).Material("lamp")
            .SlotColor("default", 1f, 0f, 0f).SlotMetallic("default", 0.25f).SlotRoughness("default", 0.5f)
            .SlotEmissive("lamp", 4f, 2f, 0f).SlotTexture("lamp", "textures/checker.png");
        var back = new EditMesh().Import(mesh.Export(_folder.File("look.gltf")));

        Assert.Equal(new[] { "default", "lamp" }, back.Slots);
        var red = back.LookOf("default");
        Assert.Equal(new Vec4(1f, 0f, 0f, 1f), red.Color);
        Assert.Equal((0.25f, 0.5f), (red.Metallic, red.Roughness));

        // The light brighter than one goes by glTF's emissive strength and comes back as it was.
        var lamp = back.LookOf("lamp");
        Assert.Equal(new Vec3(4f, 2f, 0f), lamp.Emissive);
        Assert.Equal("textures/checker.png", lamp.Texture);
        Assert.Equal(1, Enumerable.Range(0, back.FaceCount).Count(face => back.SlotOf(face) == "lamp"));
    }

    [Fact]
    public void AModelIsWrittenAsItsModifiersBuildIt()
    {
        // The subdivision's faces are not flat, so each comes back as the two triangles it is drawn as.
        var mesh = Cube().AddModifier("subdivision 1");
        var back = new EditMesh().Import(mesh.Export(_folder.File("round.gltf"), embedded: true));

        Assert.Equal((26, 48), (back.VertexCount, back.FaceCount));
        Assert.True(back.IsClosed);

        // The volume of the triangles the build draws, which the file holds, its curved faces cut
        // as the drawing cuts them rather than as the mesh's own volume fans them.
        var drawn = Modeling.Build(mesh)[0];
        var volume = drawn.Indices!.Chunk(3).Sum(t => Vec3.Dot(drawn.Positions[t[0]], Vec3.Cross(drawn.Positions[t[1]], drawn.Positions[t[2]]))) / 6f;
        Assert.Equal(volume, back.Volume, Close);

        // A smoothed face comes back smoothed, its corners' normals leaning from its own.
        var smooth = new EditMesh().Cylinder(1f, 2f, 16).SelectFacing(Vec3.UnitX).Shade(true);
        var cylinder = new EditMesh().Import(smooth.Export(_folder.File("smooth.gltf")));
        Assert.Contains(Enumerable.Range(0, cylinder.FaceCount), face => cylinder.SmoothOf(face) > 0f);
        Assert.Contains(Enumerable.Range(0, cylinder.FaceCount), face => cylinder.SmoothOf(face) == 0f);
    }

    [Fact]
    public void KnownGltfFilesAreReadIntoFacesTheirTrianglesJoined()
    {
        // Two placements of one strip of two triangles, a meter and a half either side, each a
        // square of one face in the material called White.
        var pair = new EditMesh().Import(Path.Combine(EngineHarness.AssetDirectory, "models/pair.gltf"));
        Assert.Equal((8, 8, 2), (pair.VertexCount, pair.EdgeCount, pair.FaceCount));
        Assert.Equal(new[] { "default", "White" }, pair.Slots);
        Assert.Equal((new Vec3(-1.75f, -1f, 0f), new Vec3(1.75f, 1f, 0f)), pair.Bounds);

        // A strip of four triangles in one plane, one face of six corners.
        var bend = new EditMesh().Import(Path.Combine(EngineHarness.AssetDirectory, "models/bend.gltf"));
        Assert.Equal((6, 1), (bend.VertexCount, bend.FaceCount));
        Assert.Equal(6, bend.Corners(0).Length);

        var triangle = new EditMesh().Import(Path.Combine(EngineHarness.AssetDirectory, "models/triangle.gltf"));
        Assert.Equal((3, 1), (triangle.VertexCount, triangle.FaceCount));
    }

    [Fact]
    public void AnImportIsALineOfTheRecipeAndTheCommandsExportAndImport()
    {
        var path = Cube().Export(_folder.File("crate.gltf"));
        var mesh = new EditMesh().Import(path).SelectFacing(Vec3.UnitY).Extrude(1f);
        Assert.Equal(new[] { $"import {path}", "select +y", "extrude 1" }, mesh.Recipe);
        Assert.Equal(12f, EditMesh.FromRecipe(mesh.RecipeText).Volume, Close);

        Assert.StartsWith("crate10: 8 vertices", ConsoleCommands.Run("model.box crate10 2 2 2"));
        Assert.Equal($"wrote crate10 to {_folder.File("out.gltf")}", ConsoleCommands.Run($"model.export crate10 {_folder.File("out.gltf")} embedded"));
        Assert.False(File.Exists(_folder.File("out.bin")));
        Assert.Equal("crate11: 8 vertices, 12 edges, 6 faces, 6 faces and 8 vertices selected", ConsoleCommands.Run($"model.import crate11 {_folder.File("out.gltf")}"));
        Assert.Contains("takes a model's name, a path", ConsoleCommands.Run("model.export crate10"));
        Assert.Equal("forgot crate10", ConsoleCommands.Run("model.free crate10"));
        Assert.Equal("forgot crate11", ConsoleCommands.Run("model.free crate11"));
    }

    [Fact]
    public void AFileThatIsNoGltfIsRefusedWithItsPath()
    {
        Assert.Throws<FileNotFoundException>(() => new EditMesh().Import(_folder.File("none.gltf")));

        File.WriteAllText(_folder.File("text.gltf"), "a model");
        var notJson = Assert.Throws<FormatException>(() => new EditMesh().Import(_folder.File("text.gltf")));
        Assert.Contains(_folder.File("text.gltf"), notJson.Message);

        File.WriteAllText(_folder.File("old.gltf"), """{ "asset": { "version": "1.0" } }""");
        Assert.Contains("version", Assert.Throws<FormatException>(() => new EditMesh().Import(_folder.File("old.gltf"))).Message);

        // A buffer cut short is named for the accessor that runs past it.
        Cube().Export(_folder.File("short.gltf"));
        var bin = File.ReadAllBytes(_folder.File("short.bin"));
        File.WriteAllBytes(_folder.File("short.bin"), bin[..(bin.Length / 3)]);
        Assert.Contains("runs past its buffer", Assert.Throws<FormatException>(() => new EditMesh().Import(_folder.File("short.gltf"))).Message);
    }

    [SkippableFact]
    public void BevysOwnLoaderReadsTheFileAsTheWriterWroteIt()
    {
        Needs.Renderer();

        // Written under the asset root, so the asset server finds it, and read as its first primitive.
        var name = $"bcs-gltf-{Guid.NewGuid():N}"[..20] + ".gltf";
        var full = Cube().Export(Path.Combine(EngineHarness.AssetDirectory, name), embedded: true);
        try
        {
            MeshData? read = null;
            AssetHandle mesh = AssetHandle.None;
            using var app = new App(new Config { Offscreen = true, Width = 32, Height = 32, HeadlessFps = 60, HeadlessFrames = 240, AssetRoot = EngineHarness.AssetDirectory });
            app.AddSystem(Stage.Startup, new SystemDescriptor(_ => mesh = AssetServer.Load(AssetKind.Mesh, name + "#Mesh0/Primitive0"), "Test.Load"));
            app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
            {
                if (read is null && Render.TryReadMesh(mesh, out var triangles)) read = triangles;
                if (read is not null) App.RequestExit();
            }, "Test.Read"));

            Assert.Equal(0, app.Run());
            Assert.NotNull(read);

            // Four corners for each of the six faces, two triangles each.
            Assert.Equal((24, 36), (read.Positions.Length, read.Indices!.Length));
            Assert.Equal(8f, read.Indices.Chunk(3).Sum(t => Vec3.Dot(read.Positions[t[0]], Vec3.Cross(read.Positions[t[1]], read.Positions[t[2]]))) / 6f, Close);
        }
        finally
        {
            File.Delete(full);
        }
    }
}

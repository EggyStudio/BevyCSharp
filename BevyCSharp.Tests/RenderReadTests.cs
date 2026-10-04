using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reading meshes and materials back from the engine: what a mesh holds, how a primitive was
/// made, and a material's settings.
/// </summary>
/// <remarks>
/// A mesh and a material are made by the renderer's asset types, so these are skipped on a
/// headless bridge, as the other drawing tests do.
/// </remarks>
[Collection("engine")]
public sealed class RenderReadTests
{
    [SkippableFact]
    public void ACuboidIsTwentyFourVerticesAndTwelveTriangles()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 2f, 3f);

            // Four corners a face rather than eight a box, because each face has normals of its own.
            Assert.True(Render.TryGetMeshInfo(cube, out var info));
            Assert.Equal(24, info.Vertices);
            Assert.Equal(12, info.Triangles);
            Assert.Equal(MeshTopology.Triangles, info.Topology);
            Assert.True(info.Attributes.HasFlag(MeshAttributes.Normals | MeshAttributes.Uvs));
            Assert.Equal(new Vec3(1f, 2f, 3f), info.Size);

            Assert.Equal(new MeshRecipe(MeshShape.Cuboid, 1f, 2f, 3f), Render.RecipeOf(cube));
            Assert.False(Render.TryGetMeshInfo(AssetHandle.None, out _));
            ran = true;
        });

        harness.Run();
        Assert.True(ran);
    }

    [SkippableFact]
    public void EveryPrimitiveBuildsAndRemembersHowItWasMade()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            string[] shapes =
            [
                MeshShape.Cuboid, MeshShape.Sphere, MeshShape.Plane, MeshShape.Capsule, MeshShape.Cylinder,
                MeshShape.Cone, MeshShape.ConicalFrustum, MeshShape.Torus, MeshShape.Circle,
                MeshShape.Annulus, MeshShape.Rectangle, MeshShape.Triangle, MeshShape.Tetrahedron,
            ];

            foreach (var shape in shapes)
            {
                var mesh = Render.CreateMesh(shape, 0.5f, 1f, 1.5f);
                Assert.True(Render.TryGetMeshInfo(mesh, out var info), shape);
                Assert.True(info.Triangles > 0, $"{shape} has no triangles");
                Assert.Equal(shape, Render.RecipeOf(mesh)?.Shape);
            }

            ran = true;
        });

        harness.Run();
        Assert.True(ran);
    }

    [SkippableFact]
    public void AMaterialIsReadBackAsItWasMade()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var made = new MaterialSettings
            {
                BaseColor = (0.25f, 0.5f, 0.75f, 1f),
                Metallic = 0.8f,
                Roughness = 0.3f,
                Emissive = (2f, 1f, 0f, 1f),
                AlphaMode = AlphaMode.Mask,
                AlphaCutoff = 0.4f,
                DoubleSided = true,
                UvScale = (4f, 2f),
            };

            var material = Render.CreateMaterial(made);
            Assert.True(Render.TryReadMaterial(material, out var read));

            Assert.Equal(made.BaseColor, read!.BaseColor);
            Assert.Equal(made.Metallic, read.Metallic);
            Assert.Equal(made.Roughness, read.Roughness);
            Assert.Equal(made.Emissive, read.Emissive);
            Assert.Equal(AlphaMode.Mask, read.AlphaMode);
            Assert.Equal(0.4f, read.AlphaCutoff);
            Assert.True(read.DoubleSided);
            Assert.False(read.BaseColorTexture.IsValid);
            Assert.Equal(4f, read.UvScale.U, 4);
            Assert.Equal(2f, read.UvScale.V, 4);

            // A mesh is not a material.
            Assert.False(Render.TryReadMaterial(Render.CreateMesh(MeshShape.Sphere), out _));
            ran = true;
        });

        harness.Run();
        Assert.True(ran);
    }

    [SkippableFact]
    public void AMeshsNormalsComeBackWithItsPositions()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var cube = Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f);
            Assert.True(Render.TryReadNormals(cube, out var positions, out var normals));

            // Four corners for each of the six faces, each normal of unit length facing out of its face.
            Assert.Equal(24, positions.Length);
            Assert.Equal(24, normals.Length);
            Assert.All(normals, normal => Assert.Equal(1f, normal.Length, 4));

            // Lines have no normals to read.
            var lines = Render.CreateMesh(new MeshData { Positions = [Vec3.Zero, Vec3.UnitY], Topology = MeshTopology.Lines });
            Assert.False(Render.TryReadNormals(lines, out _, out _));
            ran = true;
        });

        harness.Run();
        Assert.True(ran);
    }

    [SkippableFact]
    public void AStripIsBrokenWhereAnIndexIsTheLargestAndAListIsNot()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        MeshInfo? strip = null;
        Exception? list = null;

        harness.OnContext(Stage.Startup, _ =>
        {
            // Two strips in one mesh, a line and then two more.
            var broken = Render.CreateMesh(new MeshData
            {
                Topology = MeshTopology.LineStrip,
                Positions = [Vec3.Zero, Vec3.UnitX, Vec3.UnitY, Vec3.UnitZ, Vec3.One],
                Indices = [0, 1, uint.MaxValue, 2, 3, 4],
            });
            if (Render.TryGetMeshInfo(broken, out var info)) strip = info;

            // The same index in a list names no vertex.
            list = Record.Exception(() => Render.CreateMesh(new MeshData
            {
                Topology = MeshTopology.Lines,
                Positions = [Vec3.Zero, Vec3.UnitX],
                Indices = [0, uint.MaxValue],
            }));
        });

        harness.Run();

        // Built as it was given, every index kept, the restart among them.
        Assert.NotNull(strip);
        Assert.Equal(MeshTopology.LineStrip, strip!.Value.Topology);
        Assert.Equal(6, strip.Value.Indices);
        Assert.Equal(32, strip.Value.IndexBits);
        Assert.IsType<ArgumentException>(list);
    }

    [SkippableFact]
    public void AnImageMadeInCodeIsGivenASamplerAndOneThatIsNotThereIsRefused()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        Exception? made = null, missing = null;

        harness.OnContext(Stage.Startup, _ =>
        {
            var checker = Render.CreateImage(new byte[2 * 2 * 4], 2, 2);
            made = Record.Exception(() => Render.SetSampler(checker, new TextureSettings { Wrap = TextureWrap.Repeat }));
            missing = Record.Exception(() => Render.SetSampler(new AssetHandle(987_654), TextureSettings.Tiling));
        });

        harness.Run();

        Assert.Null(made);
        Assert.IsType<BevyNativeException>(missing);
    }
}

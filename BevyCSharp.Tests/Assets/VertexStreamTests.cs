using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers vertex streams of the game's own (<see cref="MeshStream"/>), carried on a mesh beside
/// Bevy's attributes and read by a vertex shader of the program's own at their locations.
/// </summary>
[Collection("engine")]
public sealed class VertexStreamTests
{
    private const uint Green = 0xFF00FF00, Blue = 0xFFFF0000;

    /// <summary>
    /// Two quads carrying one word a vertex at location 8, a color's bytes, are drawn in those
    /// colors by a vertex stage that unpacks them, the mesh's info says it carries a stream, and the
    /// stream's name given again in another format is refused with both shapes said.
    /// </summary>
    [SkippableFact]
    public void AStreamReachesTheProgramsOwnVertexStage()
    {
        Needs.Shaders();

        var info = default(MeshInfo);
        ArgumentException? reshaped = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d();
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 4f), Vec3.Zero, Vec3.UnitY));

                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = "shaders/stream_color.slang",
                    Fragment = "shaders/stream_color.slang",
                });
                var material = Shaders.CreateMaterial(program);

                foreach (var (x, word) in new[] { (-0.8f, Green), (0.8f, Blue) })
                {
                    var quad = Render.CreateMesh(Quad(MeshStream.From<uint>("TestColor", MeshStreamFormat.UInt, 8, [word, word, word, word])));
                    ecs.SpawnMesh(quad, material, Transform.At(x, 0f, 0f));
                    Render.TryGetMeshInfo(quad, out info);
                }

                reshaped = Assert.Throws<ArgumentException>(() =>
                    Render.CreateMesh(Quad(MeshStream.From<float>("TestColor", MeshStreamFormat.Float, 8, [0f, 0f, 0f, 0f]))));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        Assert.True(picture.At(25, 48) is { G: > 200, R: < 40, B: < 40 }, $"the green quad was {picture.At(25, 48)}");
        Assert.True(picture.At(71, 48) is { B: > 200, R: < 40, G: < 40 }, $"the blue quad was {picture.At(71, 48)}");
        Assert.True(info.Attributes.HasFlag(MeshAttributes.Streams), $"the mesh's attributes were {info.Attributes}");
        Assert.NotNull(reshaped);
        Assert.Contains("UInt at location 8", reshaped.Message, StringComparison.Ordinal);
        Assert.Contains("Float at location 8", reshaped.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A stream is checked before it crosses: its bytes against its format and the vertices, its
    /// location against the streams' range, and its location and name against the mesh's other
    /// streams.
    /// </summary>
    [Fact]
    public void AStreamOfTheWrongLengthOrLocationIsRefused()
    {
        var shortOne = Assert.Throws<ArgumentException>(() => Render.CreateMesh(Quad(MeshStream.From<uint>("A", MeshStreamFormat.UInt, 8, [1u, 2u, 3u]))));
        Assert.Contains("12 bytes for 4 vertices", shortOne.Message, StringComparison.Ordinal);

        var low = Assert.Throws<ArgumentException>(() => Render.CreateMesh(Quad(MeshStream.From<uint>("A", MeshStreamFormat.UInt, 1, [1u, 2u, 3u, 4u]))));
        Assert.Contains("location 1", low.Message, StringComparison.Ordinal);

        var data = Quad(MeshStream.From<uint>("A", MeshStreamFormat.UInt, 9, [1u, 2u, 3u, 4u]));
        data.Streams = [data.Streams![0], MeshStream.From<uint>("B", MeshStreamFormat.UInt, 9, [1u, 2u, 3u, 4u])];
        Assert.Contains("location 9", Assert.Throws<ArgumentException>(() => Render.CreateMesh(data)).Message, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => MeshStream.From<byte>("A", MeshStreamFormat.Float4, 8, [1, 2, 3]));
    }

    /// <summary>A mesh's streams cross in the layout the bridge reads them in.</summary>
    [Fact]
    public void TheMeshDataIsWhereTheBridgeReadsIt()
    {
        Assert.Equal(56, Marshal.OffsetOf<NativeMeshData>(nameof(NativeMeshData.Streams)).ToInt32());
        Assert.Equal(64, Marshal.OffsetOf<NativeMeshData>(nameof(NativeMeshData.StreamCount)).ToInt32());
        Assert.Equal(72, Marshal.SizeOf<NativeMeshData>());
        Assert.Equal(12, Marshal.OffsetOf<NativeMeshStream>(nameof(NativeMeshStream.Format)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<NativeMeshStream>(nameof(NativeMeshStream.Location)).ToInt32());
        Assert.Equal(24, Marshal.OffsetOf<NativeMeshStream>(nameof(NativeMeshStream.Data)).ToInt32());
        Assert.Equal(32, Marshal.SizeOf<NativeMeshStream>());
    }

    // A quad one wide facing the camera, with the stream given.
    private static MeshData Quad(MeshStream stream) => new()
    {
        Positions = [new Vec3(-0.5f, -0.5f, 0f), new Vec3(0.5f, -0.5f, 0f), new Vec3(0.5f, 0.5f, 0f), new Vec3(-0.5f, 0.5f, 0f)],
        Indices = [0, 1, 2, 0, 2, 3],
        Streams = [stream],
    };
}

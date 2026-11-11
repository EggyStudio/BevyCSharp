using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A primitive, made without tangents, is given them for a normal map or anisotropy to read, and a
/// mesh with nothing to work them out from is not.
/// </summary>
/// <remarks>
/// The feature test's spheres under an anisotropic material were drawn as a blaze of white, the
/// direction the surface is brushed in read from tangents they did not have, as Bevy's own
/// <c>anisotropy</c> example makes its sphere with <c>with_generated_tangents</c>.
/// </remarks>
[Collection("engine")]
public sealed class MeshTangentTests
{
    [SkippableFact]
    public void APrimitiveIsGivenTangentsAndAMeshWithNoTextureCoordinatesIsNot()
    {
        Needs.Renderer();

        MeshInfo before = default, after = default;
        bool given = false, kept = false, unmapped = true;

        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, _ =>
        {
            var sphere = Render.CreateMesh(MeshShape.Sphere, 0.5f);
            Render.TryGetMeshInfo(sphere, out before);
            given = Render.GenerateTangents(sphere);
            Render.TryGetMeshInfo(sphere, out after);
            kept = Render.GenerateTangents(sphere);

            // A triangle with positions and normals and no texture coordinates.
            var triangle = Render.CreateMesh(new MeshData
            {
                Positions = [new Vec3(0f, 0f, 0f), new Vec3(1f, 0f, 0f), new Vec3(0f, 1f, 0f)],
                Normals = [Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ],
                Indices = [0, 1, 2],
            });
            unmapped = Render.GenerateTangents(triangle);
        });
        harness.Run();

        Assert.False(before.Attributes.HasFlag(MeshAttributes.Tangents), "a sphere is made with tangents already");
        Assert.True(given, "the sphere was not given tangents");
        Assert.True(after.Attributes.HasFlag(MeshAttributes.Tangents), "the sphere has no tangents after being given them");
        Assert.True(kept, "a mesh with tangents already was refused");
        Assert.False(unmapped, "a mesh with no texture coordinates was given tangents");
    }
}

using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// Each cell of the field keeps its nearest surface's gloss beside the way its face looks, a cell
    /// nearer a polished metal block's face holding the metal's roughness, metallic share and
    /// reflectance and the face's way, and one nearer a rough dielectric block's face across a gap of a
    /// meter the dielectric's, each the winner's own values and no blend of the two, read back from the
    /// image the probes' rays read.
    /// </summary>
    /// <remarks>
    /// A cell of the faces image holds the face that won it in 32 bits, the way it looks in bits 0 to
    /// 7 and its gloss in bits 8 to 31 (<c>faceBits</c> and <c>glossBits</c> in bcs_field.slang),
    /// painted by the triangle that painted the cell's color, so a probe's ray lights what it meets by
    /// its gloss as well as its color (<c>shadeProbeHit</c>).
    /// </remarks>
    [SkippableFact]
    public void TheFieldKeepsEachCellsNearestSurfacesGlossBesideItsFace()
    {
        Needs.Shaders();
        var origin = Vector3.Zero;
        var faces = ReadBack(
            ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                Glossy(ecs, new MaterialSettings { BaseColor = (0.9f, 0.6f, 0.3f, 1f), Roughness = 0.2f, Metallic = 1f, Reflectance = 0.5f }, new Vec3(-1f, 1f, 0f));
                Glossy(ecs, new MaterialSettings { BaseColor = (0.3f, 0.6f, 0.9f, 1f), Roughness = 0.9f, Metallic = 0f, Reflectance = 0.35f }, new Vec3(2f, 1f, 0f));
                Render.SetSceneField(1, 0.25f);
            },
            stills: 2,
            () =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                return SceneField.Faces;
            });

        (Vector3 Way, Vector3 Gloss) At(Vector3 p)
        {
            var cell = (p - origin) / 0.25f;
            var at = (((int)cell.Z * SceneFieldPlan.Resolution + (int)cell.Y) * SceneFieldPlan.Resolution + (int)cell.X) * 4;
            var bits = BitConverter.ToUInt32(faces, at);
            var gloss = new Vector3((bits >> 8) & 255, (bits >> 16) & 255, bits >> 24) / 255f;
            return (FaceWay(bits & 255), gloss);
        }

        var metal = At(new Vector3(0.3f, 1f, 0.1f));
        Assert.True(Vector3.Dot(metal.Way, Vector3.UnitX) > 0.9f, $"a cell beside the metal block's face looks along {metal.Way}");
        Assert.True(Vector3.Distance(metal.Gloss, new Vector3(0.2f, 1f, 0.5f)) < 0.01f, $"a cell beside the metal block's face holds the gloss {metal.Gloss}");
        var rough = At(new Vector3(0.7f, 1f, 0.1f));
        Assert.True(Vector3.Dot(rough.Way, -Vector3.UnitX) > 0.9f, $"a cell beside the rough block's face looks along {rough.Way}");
        Assert.True(Vector3.Distance(rough.Gloss, new Vector3(0.9f, 0f, 0.35f)) < 0.01f, $"a cell beside the rough block's face holds the gloss {rough.Gloss}");
    }

    /// <summary>A cube two meters a side of <paramref name="material"/> standing with its middle at <paramref name="at"/>.</summary>
    private static void Glossy(EcsWorld ecs, MaterialSettings material, Vec3 at)
    {
        var block = ecs.Spawn();
        Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));
        Render.SetMaterial(ecs, block, Render.CreateMaterial(material));
        ecs.Add(block, Transform.At(at.X, at.Y, at.Z));
    }

    /// <summary>The way a face looks from the eight bits a cell of the faces image keeps it in, as faceWay in bcs_field.slang unfolds them.</summary>
    private static Vector3 FaceWay(uint bits)
    {
        var o = new Vector2(((bits & 15) + 0.5f) / 16f * 2f - 1f, (((bits >> 4) & 15) + 0.5f) / 16f * 2f - 1f);
        var n = new Vector3(o.X, o.Y, 1f - MathF.Abs(o.X) - MathF.Abs(o.Y));
        if (n.Z < 0f)
            n = new Vector3((1f - MathF.Abs(n.Y)) * (n.X >= 0f ? 1f : -1f), (1f - MathF.Abs(n.X)) * (n.Y >= 0f ? 1f : -1f), n.Z);
        return Vector3.Normalize(n);
    }
}

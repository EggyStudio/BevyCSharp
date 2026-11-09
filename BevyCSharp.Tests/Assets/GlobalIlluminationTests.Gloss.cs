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

    /// <summary>
    /// A probe in a closed room of 8 of smooth walls of the level's white, at a roughness of 0.1,
    /// reads the light its second bounce adds within 12% of its reference's, the walls its rays meet
    /// reflecting by their gloss the light the probes around them hold along each ray's mirror way.
    /// </summary>
    /// <remarks>
    /// The probe stands at (1, 1.5, 1) in the first cascade, held at its first bounce and at its
    /// second (<c>WorldProbes.HoldBounces</c>), against 256 paths a way of one face and of two past
    /// the first. A ray's hit reflects the indirect light by its gloss along the ray's mirror way,
    /// rather than as though it reached the face evenly from all around. What is left is the sun's
    /// and the lights' light, which a hit reflects evenly by its gloss, since the probe's rays
    /// leave along the same ways each frame and would catch or miss a highlight as sharp as a
    /// roughness of 0.1 gives. The second bounce reads about 11% over its reference, which the
    /// bound of 12% holds.
    /// </remarks>
    [SkippableFact]
    public void ASmoothRoomsProbeReflectsItsSecondBounceAlongEachRaysMirrorWay()
    {
        Needs.Shaders();
        var supported = false;
        var ready = 0;
        var at = new Vector3(1f, 1.5f, 1f);
        var read = new List<(double Probe, double Drawn)>();
        var run = new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 12000,
            Scene = ecs => ClosedRoom(ecs, 8f, 0.1f),
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 300)
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3);
        foreach (var bounces in new[] { 0, 1 })
        {
            var begun = false;
            run = run.Do($"holding at {bounces}", _ =>
                {
                    if (supported) WorldProbes.HoldBounces(bounces);
                })
                .Until($"held at {bounces}", _ => !supported || WorldProbes.HoldReached)
                .Until($"read at {bounces}", world =>
                {
                    if (!supported) return true;
                    var ecs = world.Resource<EcsWorld>();
                    if (!begun)
                    {
                        begun = true;
                        Assert.Null(GiReference.BeginProbe(ecs, at, 0, 256, bounces));
                        return false;
                    }
                    if (GiReference.StepProbe(ecs) is not { } said) return false;
                    output.WriteLine($"held at {bounces} past the first: {said}");
                    var (probe, _, drawn, _) = GiReference.LastProbe!.Value;
                    read.Add((probe, drawn));
                    return true;
                });
        }
        run.Go();
        Needs.RayQueries(supported);

        var second = (read[1].Probe - read[0].Probe) / (read[1].Drawn - read[0].Drawn) - 1;
        output.WriteLine($"the second bounce reads {second:+0.0%;-0.0%} against its reference");
        Assert.True(Math.Abs(second) < 0.12, $"the second bounce reads {second:+0.0%;-0.0%} against its reference");
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

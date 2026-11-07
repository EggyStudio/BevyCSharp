using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// An aisle of walls 0.3 thick seen down its length from a unit inside its front wall, where a
    /// cascade snapped down to its grid would end on that wall behind the eye, reads each of its
    /// faces within four points of its reference's share as it reads from 0.3 nearer the wall,
    /// where none would.
    /// </summary>
    /// <remarks>
    /// Snapped down to its grid alone, a cascade's side behind an eye looking down an axis the
    /// negative way lay 1.6 to 9.6 of its cells behind it, and from z 7 the first two cascades
    /// ended on the front wall at z 8. The probes' hits on that wall lay past their sides, the wall
    /// read black to the probes before it, the first cascade's probes a unit from it reading their
    /// face toward it 0.01 where 0.33, and through the bounces the aisle's floor read 11% under its
    /// reference and its arch's wall 11% under, where from z 7.3, the second cascade reaching four
    /// units past the wall, they read 4% and 7% over. A cascade kept a step of eight cells or more
    /// from the eye on every side (<see cref="SceneFieldPlan.OriginAround"/>) has a layer of probes
    /// behind the eye and the wall inside it, and the two views read their faces within 2.1 points
    /// of each other. What each reads over its reference, 6 to 16% on the aisle's faces and 32% on
    /// its back wall, lying on a cascade's side ahead of the eye, is read apart.
    /// </remarks>
    [SkippableFact]
    public void AnAisleReadsAlikeWithACascadesSideOnTheWallBehindTheEye()
    {
        Needs.Shaders();
        var near = AisleFaces(7f);
        var nearer = AisleFaces(7.3f);
        var faults = new List<string>();
        foreach (var (face, (off, pixels)) in near.OrderBy(f => f.Key))
        {
            if (pixels < 400 || !nearer.TryGetValue(face, out var other) || other.Pixels < 400) continue;
            output.WriteLine($"the aisle's {face} reads {off:+0.000;-0.000} of its reference from z 7 and {other.Off:+0.000;-0.000} from z 7.3");
            if (Math.Abs(off - other.Off) >= 0.04) faults.Add($"the {face} reads {off:+0.000;-0.000} from z 7 where {other.Off:+0.000;-0.000} from z 7.3");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// Each face the camera sees of the bay and its stretch of aisle (<see cref="BayAndAisle"/>)
    /// from <paramref name="eye"/> along the aisle, what its light in the picture reads over its
    /// light in a reference of 256 paths, and its pixels.
    /// </summary>
    private Dictionary<string, (double Off, int Pixels)> AisleFaces(float eye)
    {
        var supported = false;
        var (begun, read, ready) = (false, false, 0);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 6000,
            Scene = ecs => BayAndAisle(ecs, eye),
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 300)
            .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("referenced", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    Assert.Null(GiReference.BeginReference(ecs, null, 256));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Until("read", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!read)
                {
                    read = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Go();
        Needs.RayQueries(supported);

        var traced = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var faces = new Dictionary<string, (double, int)>();
        foreach (var name in traced.Names)
        {
            var (shown, wanted, pixels) = (0.0, 0.0, 0);
            for (var i = 0; i < traced.Regions.Length; i++)
            {
                if (traced.Regions[i] != name.Key) continue;
                pixels++;
                for (var c = 0; c < 3; c++)
                {
                    shown += picture[i * 3 + c];
                    wanted += traced.Light[i * 3 + c];
                }
            }
            if (pixels > 0 && wanted > 0) faces[name.Value] = (shown / wanted - 1, pixels);
        }
        return faces;
    }

    /// <summary>
    /// One bay and its stretch of aisle with the feature test's hall's measures in the level's
    /// white, every wall, the floor and the ceiling 0.3 thick, the aisle eight units wide and
    /// sixteen long, the bay eight units square off its west side through an arch six units wide
    /// and 4.5 high between two piers, a column in it, lit by three shadowed lamps in red, green
    /// and blue as the hall's first west bay is, the camera at <paramref name="eye"/> along the
    /// aisle's middle looking down its length.
    /// </summary>
    private static void BayAndAisle(EcsWorld ecs, float eye)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.6f, eye));
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.6f, eye), new Vec3(0f, 1.5f, -8f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight((1f, 1f, 1f), 0f);
        var white = Render.CreateMaterial(0.787f, 0.787f, 0.787f, roughness: 0.9f);
        void Box(string name, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            var box = ecs.Spawn();
            Render.SetMesh(ecs, box, Render.CreateMesh(MeshShape.Cuboid, x1 - x0, y1 - y0, z1 - z0));
            Render.SetMaterial(ecs, box, white);
            ecs.Add(box, Transform.At((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f));
            ecs.SetName(box, name);
        }

        const float Half = 0.15f, High = 5.5f;
        Box("floor", -12f - 2f * Half, 4f + 2f * Half, -2f * Half, 0f, -8f - 2f * Half, 8f + 2f * Half);
        Box("ceiling", -12f - 2f * Half, 4f + 2f * Half, High, High + 2f * Half, -8f - 2f * Half, 8f + 2f * Half);
        Box("aisle east", 4f - Half, 4f + Half, 0f, High, -8f, 8f);
        Box("aisle front", -4f, 4f, 0f, High, 8f - Half, 8f + Half);
        Box("aisle back", -4f, 4f, 0f, High, -8f - Half, -8f + Half);
        Box("aisle west", -4f - Half, -4f + Half, 0f, High, 0f, 8f);
        Box("pier", -4f - Half, -4f + Half, 0f, High, -1f, 0f);
        Box("pier", -4f - Half, -4f + Half, 0f, High, -8f, -7f);
        Box("lintel", -4f - Half, -4f + Half, 4.5f, High, -7f, -1f);
        Box("bay west", -12f - Half, -12f + Half, 0f, High, -8f, 0f);
        Box("bay front", -12f, -4f, 0f, High, -Half, Half);
        Box("bay back", -12f, -4f, 0f, High, -8f - Half, -8f + Half);
        Box("column", -8.75f, -8.25f, 0f, 2.6f, -4.55f, -4.05f);
        foreach (var (at, color) in new[] { (new Vec3(-5.6f, 3.6f, -1.4f), (1f, 0.1f, 0.1f)), (new Vec3(-10.6f, 3.6f, -2.4f), (0.1f, 1f, 0.1f)), (new Vec3(-7.2f, 3.6f, -7f), (0.15f, 0.3f, 1f)) })
        {
            var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 250_000f, Color = color, Range = 10f, Radius = 0.05f, Shadows = true });
            ecs.Add(lamp, Transform.At(at.X, at.Y, at.Z));
        }
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
    }
}

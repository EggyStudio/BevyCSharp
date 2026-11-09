using System.Numerics;
using Bevy;
using Vegetation;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// The probes about a lone oak lit from behind the eye, global illumination at Medium, hold the
    /// light its leaves scatter along their ways as their faces take it (<c>gi.probe</c>), the one
    /// two meters before the crown's front and the one in its front half within 25% of their
    /// reference's, and the one among the cards at its core standing outside every mesh and holding
    /// 0.4 of its reference's or more.
    /// </summary>
    /// <remarks>
    /// The crown lies past the first cascade, which lies about the eye, so its leaves read the
    /// second cascade's probes four meters apart, whose rays' interval begins at four meters. A ray
    /// crossing the crown lights what it stops where it stops it on average, since the light of the
    /// place it met the crown at, taken for all the crossing stops, reads the sunlit front for the
    /// shaded inside behind it. A crossing begun before the interval gives its light to the probe's
    /// faces, since keeping only its share leaves a probe whose every crossing begins at it near
    /// dark. And a probe in a cell the cards painted stands outside, since the cards read below
    /// zero where they run together and a probe inside a mesh holds nothing. The core's probe reads
    /// under looking away from the sun, a place deep in a crown lit as a surface facing out of it
    /// by the field's normal and letting nothing through, where the reference lights a leaf by its
    /// normal bent toward the way out and lets its translucent share through, so its bound is 0.4
    /// of its reference's.
    /// </remarks>
    [SkippableFact]
    public void ProbesAboutAFrontLitCrownHoldTheLightItsLeavesScatter()
    {
        Needs.Shaders();
        var species = TreeSpecies.CaliforniaBlackOak;
        var cards = Trees.BuildLeaves(TreeSkeleton.Grow(species, 75), species, 75).Positions;
        var (low, high) = (new Vec3(float.MaxValue), new Vec3(float.MinValue));
        foreach (var p in cards)
            (low, high) = (new Vec3(MathF.Min(low.X, p.X), MathF.Min(low.Y, p.Y), MathF.Min(low.Z, p.Z)), new Vec3(MathF.Max(high.X, p.X), MathF.Max(high.Y, p.Y), MathF.Max(high.Z, p.Z)));
        var middle = (low + high) * 0.5f;
        var eye = new Vec3(0f, middle.Y, MathF.Sqrt(22f * 22f - middle.Y * middle.Y));
        var at = new Vec3(0f, middle.Y, 0f);
        var places = new (string Name, Vector3 At, double Least, double Most)[]
        {
            ("two meters before the front", new Vector3(middle.X, middle.Y, high.Z + 2f), 0.75, 1.25),
            ("in the front half", new Vector3(middle.X, middle.Y, middle.Z + (high.Z - middle.Z) * 0.5f), 0.75, 1.25),
            ("among the core's cards", new Vector3(middle.X + 3f, middle.Y, middle.Z), 0.4, 1.25),
        };
        var supported = false;
        var ready = 0;
        Entity? tree = null;
        var read = new List<(string Name, string Said, double Least, double Most, (double Probe, double Middle, double Drawn, int Within) Reading)>();
        var wind = Trees.Wind;
        Trees.Wind = Vec3.Zero;
        try
        {
            var run = new PictureRun
            {
                Width = 320,
                Height = 180,
                Frames = 12000,
                Scene = ecs =>
                {
                    var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.World });
                    Render.SetClearColor((0f, 0f, 0f, 1f));
                    ecs.Add(camera, Transform.LookingAt(eye, at, Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                    tree = Trees.Plant(ecs, "california-black-oak", 75, Vec3.Zero);
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                    ecs.Add(sun, Transform.LookingAt(at + new Vec3(0.3f, 0.355f, 0.9f) * 20f, at, Vec3.UnitY));
                    Render.SetSceneField(4, 0.25f);
                    Render.SetGlobalIllumination(GlobalIllumination.Medium);
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("dressed", world => tree is not { } t || world.Resource<EcsWorld>().ChildrenOf(t).Length > 0)
                .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                    && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
                .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 400)
                .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3);
            foreach (var (name, spot, least, most) in places)
            {
                var begun = false;
                run = run.Until($"read {name}", world =>
                {
                    if (!supported) return true;
                    var ecs = world.Resource<EcsWorld>();
                    if (!begun)
                    {
                        begun = true;
                        Assert.Null(GiReference.BeginProbe(ecs, spot, 1, 64));
                        return false;
                    }
                    if (GiReference.StepProbe(ecs) is not { } said) return false;
                    read.Add((name, said, least, most, GiReference.LastProbe!.Value));
                    return true;
                });
            }
            run.Go();
        }
        finally
        {
            Trees.Wind = wind;
        }
        Needs.RayQueries(supported);

        var faults = new List<string>();
        foreach (var (name, said, least, most, reading) in read)
        {
            var share = reading.Probe / reading.Drawn;
            output.WriteLine($"{name}: {said}, {share:0.000} of its reference's light");
            if (said.Contains("inside a mesh", StringComparison.Ordinal)) faults.Add($"the probe {name} stands inside a mesh");
            else if (share < least || share > most) faults.Add($"the probe {name} holds {share:0.000} of its reference's light");
        }
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A reference asked of a scene with nothing drawn is refused, saying so.
    /// </summary>
    /// <remarks>
    /// With nothing drawn no ray scene is made, so the reference is refused before a scene that was
    /// never made is built again, which fails on a component type never registered.
    /// </remarks>
    [SkippableFact]
    public void AReferenceOfASceneWithNothingDrawnIsRefused()
    {
        Needs.Shaders();
        var supported = false;
        string? said = null;
        new PictureRun
        {
            Width = 64,
            Height = 36,
            Frames = 600,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.World });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1f, 4f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Do("referenced", world =>
            {
                if (supported) said = GiReference.BeginReference(world.Resource<EcsWorld>(), null, 1);
            })
            .Go();
        Needs.RayQueries(supported);
        Assert.Equal("the camera draws no meshes for the rays to meet", said);
    }
}

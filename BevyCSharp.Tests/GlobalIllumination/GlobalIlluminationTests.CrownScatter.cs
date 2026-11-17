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
    /// shaded inside behind it. A leaf it stops is lit as the leaf material lights it, the share
    /// its diffuse transmission sends through lit from the side away from the ray and the rest from
    /// the side out of the crown, since a leaf sending its whole color from either side reads the
    /// probe in the front half half again over its reference. A crossing begun before the interval
    /// gives its light to the probe's faces, since keeping only its share leaves a probe whose
    /// every crossing begins at it near dark. And a probe in a cell the cards painted stands
    /// outside, since the cards read below zero where they run together and a probe inside a mesh
    /// holds nothing. The core's probe reads under looking away from the sun, a place deep in a
    /// crown the sun reaches through the gaps between its leaves, where a face's ray to the sun
    /// stops at the crown whole, so its bound is 0.4 of its reference's.
    /// </remarks>
    [SkippableFact]
    public void ProbesAboutAFrontLitCrownHoldTheLightItsLeavesScatter()
    {
        Needs.Shaders();
        Needs.Gpu("takes past forty minutes on a device that draws on the processor, where a graphics card takes seconds");
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
    /// A probe before a sunlit wall that sends half its diffuse light through to its far side, as a
    /// leaf does, holds about half the light it holds before the same wall sending none through,
    /// the wall keeping on its lit side the half it does not send on.
    /// </summary>
    /// <remarks>
    /// The wall stands in the dark with the sun behind the probe, so the probe holds the wall's light
    /// alone, and the wall's far side faces away from the sun, so the half sent through it brings the
    /// probe next to nothing. Its gloss sends back the same either way, a few percent of its light at
    /// a roughness of 1, so the probe holds a little over half.
    /// </remarks>
    [SkippableFact]
    public void AProbeBeforeASunlitWallSendingHalfItsLightThroughHoldsAboutHalfTheLight()
    {
        Needs.Shaders();
        var supported = false;
        double Held(float transmission)
        {
            var ready = 0;
            double held = 0;
            var begun = false;
            new PictureRun
            {
                Width = 64,
                Height = 36,
                Frames = 4000,
                Scene = ecs =>
                {
                    var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.World });
                    Render.SetClearColor((0f, 0f, 0f, 1f));
                    ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 8f), new Vec3(0f, 2f, 0f), Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                    var wall = ecs.Spawn();
                    Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, 4f, 4f, 0.5f));
                    Render.SetMaterial(ecs, wall, Render.CreateMaterial(new MaterialSettings
                    {
                        BaseColor = (0.8f, 0.8f, 0.8f, 1f),
                        Roughness = 1f,
                        DiffuseTransmission = transmission,
                    }));
                    ecs.Add(wall, Transform.At(0f, 2f, 0f));
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                    ecs.Add(sun, Transform.LookingAt(new Vec3(0f, 8f, 20f), new Vec3(0f, 2f, 0f), Vec3.UnitY));
                    Render.SetSceneField(2, 0.25f);
                    Render.SetGlobalIllumination(GlobalIllumination.Medium);
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("settled", _ => !supported || Settled(1))
                .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 400)
                .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
                .Until("read", world =>
                {
                    if (!supported) return true;
                    var ecs = world.Resource<EcsWorld>();
                    if (!begun)
                    {
                        begun = true;
                        Assert.Null(GiReference.BeginProbe(ecs, new Vector3(0f, 2f, 2.5f), 0, 1));
                        return false;
                    }
                    if (GiReference.StepProbe(ecs) is not { } said) return false;
                    output.WriteLine($"sending {transmission:0.0} through: {said}");
                    held = GiReference.LastProbe!.Value.Probe;
                    return true;
                })
                .Go();
            return held;
        }

        var (opaque, sending) = (Held(0f), Held(0.5f));
        Needs.RayQueries(supported);
        output.WriteLine($"the probe holds {sending / opaque:0.000} of the light before the wall sending half through");
        Assert.True(opaque > 1e-4, $"the probe before the opaque wall holds {opaque}, too little to hold the other to");
        Assert.InRange(sending / opaque, 0.45, 0.6);
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

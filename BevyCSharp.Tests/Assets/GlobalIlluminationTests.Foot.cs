using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A wall in shade standing on a floor thinner than the field's coarsest cell, with an orange
    /// lamp under the floor that the last cascade's probes beneath the wall see, reads along its
    /// foot a light that changes from one column of the picture to the next by under 0.15 of its
    /// mean, and whose red over its blue lies within 0.05 of its reference's, which the floor keeps
    /// from the lamp.
    /// </summary>
    /// <remarks>
    /// A probe weighs a surface by how much of it it sees, its reach toward a point taken three
    /// tenths of the probes' spacing off the surface against the point's distance with a slack of
    /// an eighth of the spacing, two units at High's last cascade. At a wall's foot that point lies
    /// at the floor's height, and the rays of a probe under the floor stop at the floor's underside,
    /// within the slack of the point. Read from the one texel of the probe's reach the way falls
    /// in, the probe took about half its weight where its texel's ray stopped near the point and a
    /// twentieth where the next one's stopped short of it, and the lamp's orange light lay along the
    /// foot in triangles as wide as the texels, as along the feature test's yard wall at High. Read
    /// between the four texels nearest the way, the probe's share moves along the foot as the way
    /// does. The wall stands on a plane of the last cascade's probes eight units over one beneath
    /// the floor, as the yard's east wall does, the lamp past the wall on its far side, so no face
    /// the camera sees faces it. The sun stands overhead, so the wall's face takes none of its light
    /// and the floor before the wall is lit evenly along it, and the ambient light is none and the
    /// camera has no environment map, so the foot's light comes from the sunlit floor alone.
    /// </remarks>
    [SkippableFact]
    public void AWallsFootOverAThinFloorReadsEvenWithALampUnderTheFloor()
    {
        var (step, warmth) = WallFootOverAThinFloor();
        Assert.True(step < 0.15, $"the foot's light changes by {step:0.000} of its mean from one column to the next");
        Assert.True(warmth < 0.05, $"the foot's red over its blue reads {warmth:+0.000;-0.000} of its reference's");
    }

    /// <summary>
    /// The foot of the wall of <see cref="AWallsFootOverAThinFloorReadsEvenWithALampUnderTheFloor"/>,
    /// the two lowest rows of the wall in each column across its middle, as the most its light
    /// changes from one column to the next as a share of its mean, the largest of the three
    /// channels', and as the share its red over its blue lies over its reference's.
    /// </summary>
    private (double Step, double Warmth) WallFootOverAThinFloor()
    {
        Needs.Shaders();
        var supported = false;
        var (begun, read, ready) = (false, false, 0);
        var (corner, apart) = (Vector3.Zero, 0f);
        new PictureRun
        {
            Width = 320,
            Height = 180,
            Frames = 6000,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.7f, 0f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 0f), new Vec3(56f, 0.5f, 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                var white = Render.CreateMaterial(0.8f, 0.8f, 0.8f);
                void Box(string name, Vec3 at, Vec3 size)
                {
                    var box = ecs.Spawn();
                    Render.SetMesh(ecs, box, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
                    Render.SetMaterial(ecs, box, white);
                    ecs.Add(box, Transform.At(at.X, at.Y, at.Z));
                    ecs.SetName(box, name);
                }
                Box("floor", new Vec3(40f, -0.25f, 0f), new Vec3(200f, 0.5f, 200f));
                Box("ground", new Vec3(40f, -20f, 0f), new Vec3(200f, 8f, 200f));
                Box("wall", new Vec3(56f, 8f, 0f), new Vec3(0.5f, 16f, 48f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0f, 1f, 0f), Vec3.Zero, Vec3.UnitZ));
                var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1_000_000f, Color = (1f, 0.45f, 0.1f), Range = 60f, Shadows = true });
                ecs.Add(lamp, Transform.At(64f, -8f, 0f));
                Render.SetSceneField(4, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.High);
                Render.SetGlobalIlluminationScreenProbes(false);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Do("placed", _ =>
            {
                if (!supported) return;
                var plan = SceneField.Plan!;
                corner = plan.BuiltOrigin(plan.Cascades - 1)!.Value;
                apart = WorldProbes.ProbeSpacing * plan.CellOf(plan.Cascades - 1);
            })
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 240)
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

        // The wall's middle and the probe beneath the floor lie on the last cascade's planes of
        // probes, which the cascade's corner, a whole number of its spacings, places there.
        float Off(float at, float low) => MathF.Abs((at - low) / apart - 0.5f - MathF.Round((at - low) / apart - 0.5f));
        Assert.True(Off(56f, corner.X) < 0.01f && Off(-8f, corner.Y) < 0.01f,
            $"the last cascade's corner {corner} with probes {apart} apart puts none on the wall's middle and beneath the floor");

        var traced = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var face = traced.Names.Single(n => n.Value == "wall -x").Key;
        var (width, height) = (320, 180);
        var feet = new List<(double[] Shown, double[] Wanted)>();
        for (var x = 0; x < width; x++)
        {
            var lowest = Enumerable.Range(0, height).LastOrDefault(y => traced.Regions[y * width + x] == face, -1);
            if (lowest < 1 || traced.Regions[(lowest - 1) * width + x] != face) continue;
            var (shown, wanted) = (new double[3], new double[3]);
            foreach (var y in new[] { lowest - 1, lowest })
            {
                for (var c = 0; c < 3; c++)
                {
                    shown[c] += picture[(y * width + x) * 3 + c] / 2;
                    wanted[c] += traced.Light[(y * width + x) * 3 + c] / 2;
                }
            }
            feet.Add((shown, wanted));
        }

        // The middle of the wall's columns, its ends lying where the probes past them weigh in.
        var middle = feet.Skip(feet.Count / 6).Take(feet.Count * 2 / 3).ToList();
        var (mean, reference) = (Enumerable.Range(0, 3).Select(c => middle.Average(f => f.Shown[c])).ToArray(),
            Enumerable.Range(0, 3).Select(c => middle.Average(f => f.Wanted[c])).ToArray());
        var step = Enumerable.Range(0, 3).Max(c => Enumerable.Range(1, middle.Count - 1).Max(i => Math.Abs(middle[i].Shown[c] - middle[i - 1].Shown[c])) / mean[c]);
        var warmth = mean[0] / mean[2] / (reference[0] / reference[2]) - 1;
        output.WriteLine($"the foot over {middle.Count} columns reads {mean[0]:0.0000} {mean[1]:0.0000} {mean[2]:0.0000} against {reference[0]:0.0000} {reference[1]:0.0000} {reference[2]:0.0000}, its red over its blue {warmth:+0.000;-0.000} of the reference's, changing by {step:0.000} of its mean from one column to the next at most");
        output.WriteLine("along the foot " + string.Join(" ", middle.Select(f => $"{f.Shown[0]:0.000}/{f.Shown[2]:0.000}")));
        return (step, warmth);
    }
}

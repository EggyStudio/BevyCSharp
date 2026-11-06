using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// An open floor of the level's white under a white sky of a light of one, lit by nothing else,
    /// reads its light within 5% of its reference's both where the eye looks down on it and far off
    /// at a grazing angle.
    /// </summary>
    /// <remarks>
    /// The light pass takes back the share of the ambient light Bevy's lighting gave a surface and
    /// adds the probes' light in its place, and Bevy works that share out from F_AB, from its table
    /// where the bridge is built with it, as every bridge that draws is. Taken back by the
    /// polynomial Bevy uses without the table, which gives a rough surface's diffuse share at 0.45
    /// at every angle where the table gives it from 0.32 face on to 0.92 edge on, the floor read
    /// 0.846 of its reference near the eye and 1.334 far off. Taken back by the table it reads
    /// 0.998 and 1.000. The floor reflects nothing (its reflectance 0), since the reference's first
    /// face takes the diffuse share alone of the light from all around, and the sky's reflection on
    /// a floor of the default reflectance read it 4 and 6% over.
    /// </remarks>
    [SkippableFact]
    public void AnOpenFloorUnderTheSkyReadsItsReferenceNearTheEyeAndAtAGrazingAngle()
    {
        Needs.Shaders();
        var supported = false;
        var (begun, read, ready) = (false, false, 0);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 12000,
            Scene = OpenFloor,
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
                    Assert.Null(GiReference.BeginReference(ecs, null, 256, gloss: GiReference.Gloss.LightsAlone));
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
        var (width, height, picture) = GiReference.LastPicture!.Value;
        var floor = traced.Names.Single(n => n.Value.StartsWith("floor")).Key;
        var faults = new List<string>();
        // The rows of the floor nearest the eye, under it, and the farthest, toward the horizon.
        foreach (var (band, from, to) in new[] { ("near the eye", height * 3 / 4, height), ("far off", 0, height / 4) })
        {
            var (shown, wanted, pixels) = (0.0, 0.0, 0);
            for (var y = from; y < to; y++)
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (traced.Regions[i] != floor) continue;
                    pixels++;
                    for (var c = 0; c < 3; c++)
                        (shown, wanted) = (shown + picture[i * 3 + c], wanted + traced.Light[i * 3 + c]);
                }
            Assert.True(pixels > 200, $"the floor {band} holds {pixels} pixels");
            output.WriteLine($"the floor {band} reads {shown / wanted:0.000} of its reference's light over {pixels} pixels");
            if (Math.Abs(shown / wanted - 1) > 0.05) faults.Add($"the floor {band} reads {shown / wanted:0.000} of its reference's light");
        }
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A floor of the level's white 400 units across reflecting nothing, under a white sky of a
    /// light of one and no other light, the camera 4 units over it looking down on a point 6 units
    /// ahead, in the field the feature test has.
    /// </summary>
    private static void OpenFloor(EcsWorld ecs)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(0f, 4f, 6f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 4f, 6f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight(camera, (1f, 1f, 1f), 1f / (Exposure * AmbientReturned));
        var floor = ecs.Spawn();
        Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 400f, 0.4f, 400f));
        Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.787f, 0.787f, 0.787f, 1f), Roughness = 0.9f, Reflectance = 0f }));
        ecs.Add(floor, Transform.At(0f, -0.2f, 0f));
        ecs.SetName(floor, "floor");
        Render.SetSceneField(4, 0.25f);
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
    }
}

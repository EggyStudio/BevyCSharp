using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A corridor of the level's white 5.6 units wide and 40 long, open above and at both ends to
    /// the sky and lit by a lamp near its far end, reads the light past the direct on its walls and
    /// its floor within 30% of its reference's.
    /// </summary>
    /// <remarks>
    /// Cascade 2's probes stand 8 apart at x -4 and 4, outside the corridor's walls, each hidden
    /// from the corridor's cascade 1 probes at x -2 and 2, so the merge's weights summed to nothing
    /// and no light came from past 16 units, neither the sky above nor the lamp's end of the
    /// corridor, as in the feature test's alley at night, and the floor and the walls read 0.377,
    /// 0.480 and 0.431 of their reference's light past the direct. With a probe that sees too
    /// little of the cascade above carrying its rays on to the field's edge (gi_trace.slang) they
    /// read 0.826, 0.831 and 0.795, the first bounce near its reference and the bounces after it
    /// short. The light pass took back Bevy's ambient term by the polynomial Bevy uses without its
    /// table of F_AB, so a surface seen at a grazing angle kept a share of it, and the carried
    /// floor and walls read 1.285, 1.045 and 1.010 until the pass read the table (the floor of
    /// AnOpenFloorUnderTheSkyReadsItsReferenceNearTheEyeAndAtAGrazingAngle). The bar was 25% while
    /// the reference's faces past the first went on by their diffuse share. The whole reference,
    /// every face scattering by its gloss, took the picture as it stood from 0.770 to 0.721 on the
    /// right wall, from 0.807 to 0.754 on the left and from 0.805 to 0.765 on the floor, so the bar
    /// moved with the reference to 30%. With the probes' rays carrying the gloss of what they meet
    /// the corridor reads 0.749 to 0.788, short by the gloss carried between bounces past the
    /// probes above, which stand outside its walls and hold little of it.
    /// </remarks>
    [SkippableFact]
    public void ACorridorNarrowerThanTheProbesAboveReadsItsLightPastTheDirectAsItsReference()
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, past) in PastTheDirect(NarrowCorridor, screen: true))
        {
            output.WriteLine($"{face} reads {past:0.000} of its reference's light past the direct");
            if (Math.Abs(past - 1) > 0.30) faults.Add($"{face} reads {past:0.000}");
        }
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A corridor of the level's white 5.6 units wide, 6 high and 40 long, its floor and walls 0.4
    /// thick and its top and both ends open, under a white sky of a tenth of a light of one, with a
    /// shadowed lamp 2 units up near its far end, the camera near its near end looking along it, in
    /// a field of four cascades of a quarter unit as the feature test has it.
    /// </summary>
    private static void NarrowCorridor(EcsWorld ecs)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.6f, 6f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.6f, 6f), new Vec3(0f, 1f, -20f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight(camera, (1f, 1f, 1f), 0.1f / (Exposure * AmbientReturned));
        var white = Render.CreateMaterial(0.787f, 0.787f, 0.787f, roughness: 0.9f);
        void Slab(string name, Vec3 at, Vec3 extent)
        {
            var slab = ecs.Spawn();
            Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, extent.X, extent.Y, extent.Z));
            Render.SetMaterial(ecs, slab, white);
            ecs.Add(slab, Transform.At(at.X, at.Y, at.Z));
            ecs.SetName(slab, name);
        }

        Slab("floor", new Vec3(0f, -0.2f, -10f), new Vec3(6.4f, 0.4f, 40f));
        Slab("left", new Vec3(-3f, 3f, -10f), new Vec3(0.4f, 6f, 40f));
        Slab("right", new Vec3(3f, 3f, -10f), new Vec3(0.4f, 6f, 40f));
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 400_000f, Color = (1f, 1f, 1f), Range = 30f, Radius = 0.05f, Shadows = true });
        ecs.Add(lamp, Transform.At(0f, 2f, -24f));
        Render.SetSceneField(4, 0.25f);
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
    }
}

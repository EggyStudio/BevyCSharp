using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

// A surface that lets a share of global illumination's light through, as a canopy's leaves, held to
// what 3DEngine's tests read of its own, the floor under a slab two and five units up.
public sealed partial class GlobalIlluminationTests(ITestOutputHelper output)
{
    /// <summary>
    /// A gray floor lit by the sky alone, read in linear light under a dark green slab a unit thick
    /// and twenty across, two and a half units over it or five and a half, with no slab, with a solid
    /// one, and with one letting half the light through a meter and one letting all of it through,
    /// at Low through the cascades' rays and at High through the screen's probes' too. A ray keeps
    /// half its light a meter of slab, and one at a slant crosses more of it, so the half slab lets
    /// through the share of the sky's light the solid one holds back that the slab's depth along each
    /// way the floor's sky comes from gives, 0.36 two units up and 0.41 five, as 3DEngine's test works
    /// it out, which its engine read at 0.36 and 0.40 two up and 0.37 and 0.32 five up, and a slab
    /// letting all of it through holds back next to none. This engine's floor takes from a fifth to a
    /// half of it, and holds back up to a tenth under a slab letting all of it through five up.
    /// </summary>
    [SkippableTheory]
    [InlineData(GlobalIllumination.Low, 2.5f)]
    [InlineData(GlobalIllumination.High, 2.5f)]
    [InlineData(GlobalIllumination.Low, 5.5f)]
    [InlineData(GlobalIllumination.High, 5.5f)]
    public void AFloorUnderASlabThatLetsLightThroughTakesTheSkyLightItsShareLetsThrough(GlobalIllumination quality, float height)
    {
        Needs.Shaders();

        float Floor(float? through)
        {
            var (r, g, b) = Looking(
                ecs =>
                {
                    Slab(ecs, (0.22f, 0.22f, 0.22f), new Vec3(0f, -0.5f, 0f), new Vec3(30f, 1f, 30f), 0f);
                    if (through is { } share) Slab(ecs, (0.045f, 0.1f, 0.03f), new Vec3(0f, height, 0f), new Vec3(20f, 1f, 20f), share);
                    Render.SetSceneField(2, 0.25f);
                    Render.SetGlobalIllumination(quality);
                },
                stills: through is null ? 1 : 2,
                eye: new Vec3(0f, 1.2f, 7f),
                at: new Vec3(0f, 0f, 3.5f),
                ambient: 400f,
                traced: true,
                // Traced for three seconds' frames rather than one's, since with the GPU drawing for
                // another app beside the suite, as 3DEngine's sessions do, the floors read after 60
                // came out short of where they settle, the half slab's at 0.0303 where it settles at
                // 0.0329 five units up, and at 180 they read as they do with the GPU to the suite.
                frames: 180);
            return Luminance(r, g, b);
        }

        var open = Floor(null);
        var solid = Floor(0f);
        var half = Floor(0.5f);
        var all = Floor(1f);
        var share = (half - solid) / (open - solid);
        output.WriteLine($"{quality}, a slab {height} units up, the floor {open:0.0000} open, {solid:0.0000} under a solid slab, {half:0.0000} under one letting half through a meter, {share:0.00} of what the solid one holds back, and {all:0.0000} under one letting all through");
        Assert.True(open - solid > 0.05f * open, $"a solid slab keeps the sky's light off the floor under it, {solid:0.0000} against {open:0.0000}");
        Assert.True((all - solid) / (open - solid) > 0.85f, $"a slab letting all of it through holds back little, {all:0.0000}");
        Assert.InRange(share, 0.2f, 0.5f);
    }

    /// <summary>A material's share of light let through comes back as it was given and as it was written.</summary>
    [SkippableFact]
    public void AMaterialsShareOfLightLetThroughIsReadBack()
    {
        Needs.Renderer();
        float given = -1f, written = -1f, none = -1f;
        new PictureRun().Do("read", _ =>
            {
                var leaves = Render.CreateMaterial(new MaterialSettings { LightThrough = 0.6f });
                Assert.True(Render.TryReadMaterial(leaves, out var read));
                given = read!.LightThrough;
                read.LightThrough = 0.25f;
                Assert.True(Render.WriteMaterial(leaves, read));
                Assert.True(Render.TryReadMaterial(leaves, out read));
                written = read!.LightThrough;
                Assert.True(Render.TryReadMaterial(Render.CreateMaterial(new MaterialSettings()), out read));
                none = read!.LightThrough;
            }).Go();

        Assert.Equal((0.6f, 0.25f, 0f), (given, written, none));
    }

    /// <summary>A box of a color, a share of light let through a meter of it, fully rough.</summary>
    private static void Slab(EcsWorld ecs, (float R, float G, float B) color, Vec3 at, Vec3 size, float through)
    {
        var slab = ecs.Spawn();
        Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
        Render.SetMaterial(ecs, slab, Render.CreateMaterial(new MaterialSettings { BaseColor = (color.R, color.G, color.B, 1f), Roughness = 1f, LightThrough = through }));
        ecs.Add(slab, Transform.At(at.X, at.Y, at.Z));
    }

    /// <summary>Luminance in linear light of a pixel's sRGB bytes.</summary>
    private static float Luminance(int r, int g, int b)
    {
        static float Linear(int value) => MathF.Pow(value / 255f, 2.2f);
        return 0.2126f * Linear(r) + 0.7152f * Linear(g) + 0.0722f * Linear(b);
    }
}

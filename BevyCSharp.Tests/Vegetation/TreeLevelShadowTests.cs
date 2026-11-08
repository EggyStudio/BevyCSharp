using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// One level of a tree casts the sun's shadow at any distance, so a tree in the band where its
/// full level is dithered into its reduced one shades its leaves as the reference's one level does.
/// </summary>
[Collection("engine")]
public sealed class TreeLevelShadowTests(ITestOutputHelper output)
{
    /// <summary>
    /// A lone oak in the band between its full and its reduced level, lit from the side by a low
    /// sun with nothing else, takes the sun on its leaves as the reference's direct light does.
    /// </summary>
    /// <remarks>
    /// Bevy draws a mesh into a light's shadow map over the whole of its visibility range, both of
    /// its margins included, and the shadow pass takes no dither, so in the band from 2.5 to 3 of a
    /// tree's heights both levels cast whole and the crown shades itself twice. The oak read leaf by
    /// leaf against the reference's light at its first faces, which traces the level of the camera's
    /// distance alone, read 16, 19, 15, 17 and 12% under at 26.5, 27.85, 28.05, 29.5 and 30.3
    /// meters, the band's middle at 27.94, and 3% under at 22 meters and 8% over at 33 with either
    /// level alone. A mesh of a visibility range casts from the middle of the margin it fades in
    /// over to the middle of the one it fades out over, the half of each margin the reference traces
    /// it on, and the oak reads 8, 4, 3, 1 and 7% over. The bar fails the least of the shortfalls
    /// and holds the largest excess with 7 points to spare.
    /// </remarks>
    [SkippableTheory]
    [InlineData(26.5f)]
    [InlineData(27.85f)]
    [InlineData(28.05f)]
    [InlineData(29.5f)]
    [InlineData(30.3f)]
    public void AnOakBetweenItsLevelsTakesTheSunAsTheReferenceDoes(float distance)
    {
        Needs.Shaders();
        var species = TreeSpecies.CaliforniaBlackOak;
        var (low, high) = (float.MaxValue, float.MinValue);
        foreach (var p in Trees.BuildLeaves(Trees.Grow(species, 75), species, 75).Positions) (low, high) = (MathF.Min(low, p.Y), MathF.Max(high, p.Y));
        var middle = (low + high) / 2f;

        // The eye at the distance from the tree's foot Bevy measures its levels by, level with the
        // crown's middle, and the sun 21 degrees up from the side.
        var eye = new Vec3(0f, middle, MathF.Sqrt(distance * distance - middle * middle));
        var at = new Vec3(0f, middle, 0f);
        var supported = false;
        Entity? tree = null;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        var wind = Trees.Wind;
        Trees.Wind = Vec3.Zero;
        try
        {
            new PictureRun
            {
                Width = 320,
                Height = 180,
                Frames = 6000,
                Scene = ecs =>
                {
                    var camera = PictureRun.Camera(ecs, eye);
                    ecs.Set(camera, Transform.LookingAt(eye, at, Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                    tree = Trees.Plant(ecs, "california-black-oak", 75, Vec3.Zero);
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                    ecs.Add(sun, Transform.LookingAt(at + new Vec3(0.866f, 0.355f, 0.352f), at, Vec3.UnitY));
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("ready", _ => !supported || Render.PipelinesReady())
                .Until("dressed", world => tree is not { } planted || world.Resource<EcsWorld>().ChildrenOf(planted).Length > 0)
                .Wait(120)
                .Until("drawn", _ => Render.PipelinesReady())
                .Until("referenced", world => !supported || Read(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 64, bounces: 0, leaves: true)))
                .Wait(10)
                .Until("read", world => !supported || Read(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
                .Go();
        }
        finally
        {
            Trees.Wind = wind;
        }
        Needs.RayQueries(supported);

        // The leaves' light where every path meets a leaf of the oak first, in both.
        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var (pixels, seen, met) = (0, 0.0, 0.0);
        for (var i = 0; i < reference.Regions.Length; i++)
        {
            if (!reference.Names.TryGetValue(reference.Regions[i], out var name) || !name.Contains(", leaves", StringComparison.Ordinal)) continue;
            if (reference.Leaves![i] < GiReference.WholeLeaf) continue;
            pixels++;
            seen += 0.2126 * picture[i * 3] + 0.7152 * picture[i * 3 + 1] + 0.0722 * picture[i * 3 + 2];
            met += 0.2126 * reference.Light[i * 3] + 0.7152 * reference.Light[i * 3 + 1] + 0.0722 * reference.Light[i * 3 + 2];
        }
        output.WriteLine($"at {distance} m {pixels} leaf pixels read {seen / pixels:0.0000} in the picture and {met / pixels:0.0000} in the reference, {(seen / met - 1) * 100:+0;-0}%");
        Assert.True(pixels > 500, $"the oak's leaves cover {pixels} pixels");
        Assert.InRange(seen / met, 0.92, 1.15);
    }

    /// <summary>
    /// The oak's shadow changes level in the frame the eye crosses the middle of the band, the
    /// frame before it the full level's alone and the frame after it the reduced level's alone, each
    /// as the picture reads once it has stood there, and never both levels' or neither's.
    /// </summary>
    /// <remarks>
    /// The eye stands 5 centimeters short of the band's middle, then crosses to 5 centimeters past
    /// it between one frame and the next, each frame captured and set against the picture taken once
    /// the eye has stood on its side of the middle. The two pairs read as one, a mean light of
    /// 0.1682 and 0.1679 where the head read 0.1659 and 0.1660 with both levels casting and the tree
    /// read 0.1807 casting nothing, so the shadow changes in the frame the eye crosses and is one
    /// level's on either side of it.
    /// </remarks>
    [SkippableFact]
    public void TheShadowChangesLevelInTheFrameTheEyeCrossesTheBandsMiddle()
    {
        Needs.Renderer();
        var species = TreeSpecies.CaliforniaBlackOak;
        var (low, high) = (float.MaxValue, float.MinValue);
        foreach (var p in Trees.BuildLeaves(Trees.Grow(species, 75), species, 75).Positions) (low, high) = (MathF.Min(low, p.Y), MathF.Max(high, p.Y));
        var at = new Vec3(0f, (low + high) / 2f, 0f);
        Vec3 Eye(float distance) => new(0f, at.Y, MathF.Sqrt(distance * distance - at.Y * at.Y));

        Entity? tree = null, camera = null;
        var middle = 0f;
        Capture? before = null, after = null;
        CapturedImage? frameBefore = null, frameAfter = null;
        var wind = Trees.Wind;
        Trees.Wind = Vec3.Zero;
        PictureRun run;
        try
        {
            run = new PictureRun
            {
                Width = 320,
                Height = 180,
                Frames = 3000,
                Scene = ecs =>
                {
                    camera = PictureRun.Camera(ecs, Eye(20f));
                    ecs.Set(camera.Value, Transform.LookingAt(Eye(20f), at, Vec3.UnitY));
                    Render.SetPostProcessing(camera.Value, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera.Value, (1f, 1f, 1f), 300f);
                    PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 80f, new Vec3(0f, -40f, 0f));
                    tree = Trees.Plant(ecs, "california-black-oak", 75, Vec3.Zero);
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                    ecs.Add(sun, Transform.LookingAt(at + new Vec3(0.866f, 0.355f, 0.352f), at, Vec3.UnitY));
                },
            }
                .Until("ready", _ => Render.PipelinesReady())
                .Until("dressed", world => tree is not { } planted || world.Resource<EcsWorld>().ChildrenOf(planted).Length > 0)
                .Do("the band's middle", world =>
                {
                    // The full level's margin, which the tree's body carries, from the tree's foot.
                    var ecs = world.Resource<EcsWorld>();
                    var end = ecs.Get<Bevy.Reflected.VisibilityRangeRef>(ecs.ChildrenOf(tree!.Value)[0])!.Value.EndMargin;
                    middle = (end.Start + end.End) / 2f;
                    ecs.Set(camera!.Value, Transform.LookingAt(Eye(middle - 0.05f), at, Vec3.UnitY));
                })
                .Wait(120)
                .Capture("standing before")
                .Wait(10)
                .Do("the frame before", _ => before = Render.BeginCapture())
                .Wait(1)
                .Do("the frame after", world =>
                {
                    world.Resource<EcsWorld>().Set(camera!.Value, Transform.LookingAt(Eye(middle + 0.05f), at, Vec3.UnitY));
                    after = Render.BeginCapture();
                })
                .Until("both frames", _ => (frameBefore ??= Read(before)) is not null & (frameAfter ??= Read(after)) is not null)
                .Wait(120)
                .Capture("standing after")
                .Do("casting nothing", world =>
                {
                    var ecs = world.Resource<EcsWorld>();
                    foreach (var part in ecs.ChildrenOf(ecs.ChildrenOf(tree!.Value)[0]).Append(ecs.ChildrenOf(tree.Value)[0]))
                        if (ecs.Get<Bevy.Reflected.VisibilityRangeRef>(part) is not null) Render.SetMeshFlags(ecs, part, MeshFlags.NoShadowCasting);
                })
                .Wait(10)
                .Capture("casting nothing");
            run.Go();
        }
        finally
        {
            Trees.Wind = wind;
        }

        // Each picture's mean light, its pixels' sRGB read as linear, and how far each pixel of one
        // lies from another's on average, of the mean.
        static double Mean(CapturedImage picture) => Enumerable.Range(0, picture.Pixels.Length / 4).Average(i => Linear(picture.Pixels, i));
        static double Apart(CapturedImage a, CapturedImage b) => Enumerable.Range(0, a.Pixels.Length / 4).Average(i => Math.Abs(Linear(a.Pixels, i) - Linear(b.Pixels, i))) / Mean(b);
        var (standingBefore, standingAfter, none) = (run.Picture("standing before"), run.Picture("standing after"), run.Picture("casting nothing"));
        var (shortOf, pastIt) = (Apart(frameBefore!, standingBefore), Apart(frameAfter!, standingAfter));
        output.WriteLine($"the band's middle at {middle:0.00} m, the mean light standing before it {Mean(standingBefore):0.0000}, "
            + $"the frame before {Mean(frameBefore!):0.0000}, the frame after {Mean(frameAfter!):0.0000}, standing after {Mean(standingAfter):0.0000}, "
            + $"casting nothing {Mean(none):0.0000}, the frames {shortOf:0.0000} and {pastIt:0.0000} from standing, of their light");
        Assert.True(shortOf < 0.01, $"the frame before the step lies {shortOf:0.0000} from the full level's shadow alone");
        Assert.True(pastIt < 0.01, $"the frame after the step lies {pastIt:0.0000} from the reduced level's shadow alone");
    }

    // A pixel's light, its sRGB channels' luminance read as linear.
    private static double Linear(byte[] pixels, int index)
    {
        static double Channel(byte value) => Math.Pow(value / 255.0, 2.2);
        return 0.2126 * Channel(pixels[index * 4]) + 0.7152 * Channel(pixels[index * 4 + 1]) + 0.0722 * Channel(pixels[index * 4 + 2]);
    }

    // A capture once it has come back, or none yet.
    private static CapturedImage? Read(Capture? ticket) =>
        ticket is { } asked && Render.TryReadCapture(asked, out var picture) ? picture : null;

    // Asks for a read once and answers whether it is done.
    private static bool Read(EcsWorld ecs, ref bool asked, ref string? said, Func<EcsWorld, string?> begin)
    {
        if (!asked)
        {
            asked = true;
            Assert.Null(begin(ecs));
            return false;
        }
        return (said = GiReference.Step(ecs)) is not null;
    }
}

using Bevy;
using Bevy.Reflected;
using Vegetation;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The measure global illumination is read by (<see cref="GiReference"/>), the camera's picture
/// read with global illumination off as with it on and from the camera the view is drawn by, a read
/// that never answers given up before the console gives it up, a reference holding its own paths
/// alone, and the reference lit by the sun as Bevy lights the picture, through the camera's air.
/// </summary>
[Collection("engine")]
public sealed class GiReferenceTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private const uint Width = 64, Height = 36;

    /// <summary>
    /// The picture is read where no global illumination is traced, before it was ever on and after
    /// it was turned off, as gi.compare reads the light with it off beside the light with it on.
    /// </summary>
    /// <remarks>
    /// Global illumination asks for Bevy's G-buffer, which draws every material deferred, and
    /// leaves them so when it is turned off, and Bevy draws a deferred material in its G-buffer
    /// alone, so a camera left drawing no prepass would show a black picture from then on.
    /// </remarks>
    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThePictureIsReadWithGlobalIlluminationOff(bool wasOn)
    {
        Needs.Shaders();
        string? said = null;
        var asked = false;
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 900,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 5f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 1f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 2f, 1f), Vec3.Zero, Vec3.UnitY));
                if (wasOn) Render.SetGlobalIllumination(GlobalIllumination.Low);
            },
        }
            .Until("traced", _ => !wasOn || WorldProbes.TracedFrames >= 30)
            .Do("turning it off", _ => Render.SetGlobalIllumination(GlobalIllumination.Off))
            .Wait(30)
            .Until("ready", _ => Render.PipelinesReady())
            .Until("read", world => Read(world.Resource<EcsWorld>(), ref asked, ref said, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();

        Assert.Equal($"read the picture's light, {Width} by {Height}", said);
        Assert.Contains(GiReference.LastPicture!.Value.Light, light => light > 0.01f);
    }

    /// <summary>
    /// A read whose copy never runs, its camera put out once it began, gives itself up a few frames
    /// before the console gives the command up, saying where it stood, and the next read is taken,
    /// where the read once waited as long as the console and, given up by it, held the measure busy
    /// for good.
    /// </summary>
    [SkippableFact]
    public void AReadThatNeverAnswersGivesItselfUpBeforeTheConsoleDoes()
    {
        Needs.Shaders();
        var camera = Entity.None;
        string? said = null, next = null;
        var (asked, steps, busy) = (false, 0, true);
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = (int)ConsoleHost.LaterFrames + 120,
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 5f));
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 1f);
            },
        }
            .Until("ready", _ => Render.PipelinesReady())
            .Until("given up", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                if (!asked)
                {
                    asked = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    ecs.Wrap<CameraRef>(camera).IsActive = false;
                    return false;
                }
                steps++;
                return (said = GiReference.Step(ecs)) is not null;
            })
            .Do("asking again", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                busy = GiReference.Busy;
                ecs.Wrap<CameraRef>(camera).IsActive = true;
                next = GiReference.BeginPicture(ecs, null);
            })
            .Go();

        Assert.StartsWith("the picture's size was never read", said);
        Assert.InRange(steps, 1, (int)ConsoleHost.LaterFrames - 1);
        Assert.False(busy);
        Assert.Null(next);
    }

    /// <summary>
    /// With no field built, the picture is read from the camera the view is drawn by, the active 3D
    /// camera of the highest order, as the field takes it, past a 3D camera that draws nothing.
    /// </summary>
    /// <remarks>
    /// With no field the view's camera is taken as the field takes it, since the world's first 3D
    /// camera, which in the feature test's yard is a camera drawing nothing, would leave a read
    /// with global illumination off asking for the picture's size until it gave up.
    /// </remarks>
    [SkippableFact]
    public void WithNoFieldThePictureIsReadFromTheCameraTheViewIsDrawnBy()
    {
        Needs.Shaders();
        string? said = null;
        var asked = false;
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 900,
            Scene = ecs =>
            {
                var idle = Render.SpawnCamera3d(new CameraSettings());
                ecs.Wrap<CameraRef>(idle).IsActive = false;
                var camera = Render.SpawnCamera3d(new CameraSettings { Order = 1, Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 5f), Vec3.Zero, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 1f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 2f, 1f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Until("ready", _ => Render.PipelinesReady())
            .Until("read", world => Read(world.Resource<EcsWorld>(), ref asked, ref said, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();

        Assert.Equal($"read the picture's light, {Width} by {Height}", said);
        Assert.Contains(GiReference.LastPicture!.Value.Light, light => light > 0.01f);
    }

    /// <summary>
    /// A reference traced after another holds its own paths alone, as many as were asked for, where
    /// it started from what the image's memory held, the paths of the one before it.
    /// </summary>
    [SkippableFact]
    public void AReferenceTracedAfterAnotherHoldsItsOwnPathsAlone()
    {
        Needs.Shaders();
        var supported = false;
        string? first = null, second = null;
        var (askedFirst, askedSecond) = (false, false);
        var paths = (First: 0, Second: 0);
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 900,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 5f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 1f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 2f, 1f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Until("first", world => !supported || Read(world.Resource<EcsWorld>(), ref askedFirst, ref first, ecs => GiReference.BeginReference(ecs, null, 32)))
            .Do("counting the first", _ => paths.First = supported ? GiReference.Last!.Samples : 0)
            .Until("second", world => !supported || Read(world.Resource<EcsWorld>(), ref askedSecond, ref second, ecs => GiReference.BeginReference(ecs, null, 32)))
            .Do("counting the second", _ => paths.Second = supported ? GiReference.Last!.Samples : 0)
            .Go();
        Needs.RayQueries(supported);

        Assert.Equal((32, 32), paths);
    }

    /// <summary>
    /// The reference's sun reaches a white floor as Bevy's lighting gives it under the camera's
    /// air, ten degrees up, where the air dims the sun and reddens it, the first face's light alone
    /// in both.
    /// </summary>
    /// <remarks>
    /// The reference lights a face by the sun through the air, the light the picture's faces take,
    /// since lit by the sun as it is above the air, its own color and strength, every sunlit face
    /// of the feature test's yard at a low sun would read well over the picture and whiter,
    /// whatever global illumination gave the faces in shade.
    /// </remarks>
    [SkippableFact]
    public void TheReferencesSunIsDimmedThroughTheAirAsThePictures()
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 900,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 2f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 40f, new Vec3(0f, -20f, 0f));
                var elevation = 10f * MathF.PI / 180f;
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(MathF.Cos(elevation), MathF.Sin(elevation), 0f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 16, bounces: 0)))
            .Until("read", world => !supported || Read(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var (shown, wanted) = (new double[3], new double[3]);
        for (var y = (int)Height / 4; y < 3 * (int)Height / 4; y++)
        for (var x = (int)Width / 4; x < 3 * (int)Width / 4; x++)
        for (var c = 0; c < 3; c++)
        {
            var i = (y * (int)Width + x) * 3 + c;
            shown[c] += picture[i];
            wanted[c] += reference.Light[i];
        }

        for (var c = 0; c < 3; c++)
            Assert.True(Math.Abs(wanted[c] / shown[c] - 1) < 0.08, $"the reference's floor reads {wanted[0] / shown[0]:0.000}, {wanted[1] / shown[1]:0.000} and {wanted[2] / shown[2]:0.000} of the picture's, red, green and blue");
        Assert.True(shown[2] < shown[0] * 0.9, $"the low sun reaches the floor {shown[0]:0}, {shown[1]:0}, {shown[2]:0}, which the air should redden");
    }

    /// <summary>
    /// A card cut out by a checkered texture, seen from above under a sun straight overhead, shows
    /// the lit floor through its cut-out half in the reference and its texture's green in the
    /// other, where an opaque card covers the floor beneath it whole.
    /// </summary>
    /// <remarks>
    /// The reference takes a card's cut-out and a leaf's material into its paths, since the
    /// material buffer's base color alone would trace a cut-out card, a leaf or a grass card,
    /// whole, and a leaf's material as black, and the meadow's grass and its crowns would read
    /// nothing and hide the floor behind them. The card fills half its footprint, as its checkered
    /// texture is cut out, and the floor through it reads about the open floor's light, the floor's
    /// highlight under the eye and the sun overhead.
    /// </remarks>
    [SkippableFact]
    public void ACutOutCardLetsTheReferencesPathsThroughWhereItsAlphaIsUnderTheCutoff()
    {
        Needs.Shaders();
        var footprint = Looked(cutOut: false, standing: false, out var supported);
        Needs.RayQueries(supported);
        var cut = Looked(cutOut: true, standing: false, out _);

        var (card, holes, open) = (0, 0.0, 0.0);
        var (inside, outside, green) = (0, 0, 0.0);
        for (var i = 0; i < footprint.Regions.Length; i++)
        {
            if (IsCard(footprint, footprint.Regions[i]))
            {
                inside++;
                if (IsCard(cut, cut.Regions[i]))
                {
                    card++;
                    green += cut.Light[i * 3 + 1] - cut.Light[i * 3];
                }
                else holes += cut.Light[i * 3 + 1];
            }
            else if (cut.Regions[i] != 0)
            {
                outside++;
                open += cut.Light[i * 3 + 1];
            }
        }
        var filled = (double)card / inside;
        var through = holes / Math.Max(1, inside - card) / (open / Math.Max(1, outside));
        output.WriteLine($"the cut-out card fills {filled:0.000} of its footprint of {inside} pixels, the floor through it {through:0.000} of the open floor's light, its own green {green / Math.Max(1, card):0.000} over its red");
        Assert.True(inside > 300, $"the opaque card covers {inside} pixels");
        Assert.True(Math.Abs(filled - 0.5) < 0.1, $"the cut-out card fills {filled:0.000} of its footprint");
        Assert.True(Math.Abs(through - 1) < 0.1, $"the floor through the card reads {through:0.000} of the open floor's light");
        Assert.True(green > 0, "the cut-out card reads no green of its texture's");
    }

    /// <summary>
    /// A cut-out card standing between a low sun and a floor lets half the sun through to the floor
    /// in its shadow in the reference, a checkered shadow, where an opaque card's shadow is dark.
    /// </summary>
    /// <remarks>
    /// The shadow rays meet the card as the paths do, so the card cut out where its leaves are not
    /// lets the sun through there, half its shadow's light, where an opaque card's shadow lets none
    /// through.
    /// </remarks>
    [SkippableFact]
    public void ACutOutCardLetsTheSunThroughToItsShadowWhereItsAlphaIsUnderTheCutoff()
    {
        Needs.Shaders();
        var solid = Looked(cutOut: false, standing: true, out var supported);
        Needs.RayQueries(supported);
        var cut = Looked(cutOut: true, standing: true, out _);

        // The open floor is read where the opaque card's shadow does not reach, and its shadow
        // where the floor reads under a tenth of that.
        var open = Enumerable.Range(0, solid.Regions.Length).Where(i => solid.Regions[i] != 0).Max(i => solid.Light[i * 3 + 1]);
        var (shadowed, opaqueLight, cutLight, lit, litLight) = (0, 0.0, 0.0, 0, 0.0);
        for (var i = 0; i < solid.Regions.Length; i++)
        {
            if (solid.Regions[i] == 0) continue;
            if (solid.Light[i * 3 + 1] < 0.1 * open)
                (shadowed, opaqueLight, cutLight) = (shadowed + 1, opaqueLight + solid.Light[i * 3 + 1], cutLight + cut.Light[i * 3 + 1]);
            else
                (lit, litLight) = (lit + 1, litLight + cut.Light[i * 3 + 1]);
        }
        var mean = litLight / Math.Max(1, lit);
        var share = cutLight / Math.Max(1, shadowed) / mean;
        output.WriteLine($"the cut-out card's shadow of {shadowed} pixels reads {share:0.000} of the open floor's light, the opaque card's {opaqueLight / Math.Max(1, shadowed) / mean:0.000}");
        Assert.True(shadowed > 200, $"the opaque card shades {shadowed} pixels");
        Assert.True(Math.Abs(share - 0.5) < 0.1, $"the cut-out card's shadow reads {share:0.000} of the open floor's light");
    }

    /// <summary>
    /// A mesh of a visibility range is in the reference where the eye's distance from it gives it,
    /// as the picture draws it, a near level up to the middle of the margin it fades out over and a
    /// far level from there on, so the reference meets the level the camera draws.
    /// </summary>
    /// <remarks>
    /// The reference holds the mesh of each visibility range the eye's distance gives, since every
    /// shown mesh traced at once would hold a tree at every level, its leaves, its reduced leaves
    /// and its far cards over each other, the far cards standing as planes over the crowns. Here
    /// the level not drawn is the larger of two cubes about the same middle, so where it is traced
    /// the eye meets it first, the far level from near and the near one from far.
    /// </remarks>
    [SkippableTheory]
    [InlineData(5f, "near")]
    [InlineData(8.5f, "near")]
    [InlineData(9.5f, "far")]
    [InlineData(20f, "far")]
    public void AReferenceMeetsTheLevelOfAMeshItsDistanceFromTheEyeGives(float distance, string drawn)
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null;
        var asked = false;
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 900,
            Scene = ecs =>
            {
                var eye = new Vec3(0f, 0f, distance);
                var camera = PictureRun.Camera(ecs, eye);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                foreach (var (name, start, end) in new[] { ("near", new FloatRange(0f, 0f), new FloatRange(8f, 10f)), ("far", new FloatRange(8f, 10f), new FloatRange(100f, 120f)) })
                {
                    var level = PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), name == drawn ? 1f : 2f, Vec3.Zero);
                    ecs.SetName(level, name);
                    var range = ecs.Insert<VisibilityRangeRef>(level);
                    (range.StartMargin, range.EndMargin, range.UseAabb) = (start, end, false);
                }
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 2f, 3f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref asked, ref traced, ecs => GiReference.BeginReference(ecs, null, 1, bounces: 0)))
            .Go();
        Needs.RayQueries(supported);

        var met = GiReference.Last!.Regions.Where(region => region != 0).Select(region => GiReference.Last.Names[region].Split(' ')[0]).Distinct().ToArray();
        output.WriteLine($"from {distance} the reference meets {string.Join(" and ", met)}: {traced}");
        Assert.Equal([drawn], met);
    }

    /// <summary>
    /// A patch of grass under a sun behind the eye, and under one before it lighting its blades
    /// from behind, reads in the reference the light the picture gives it, the first face's light
    /// alone in both, since the reference lights a grass card as the leaf material does, its normal
    /// map bending it, and its shadow rays pass the blades, which cast no shadow.
    /// </summary>
    /// <remarks>
    /// The reference lights each card as the leaf material does, the share of a blade's color it
    /// keeps by its mapped normal bent half way up to its way out of the crown, straight up for
    /// grass, and the rest let through from behind, and its shadow rays pass the blades, as the
    /// picture's blades cast no shadow, since a card lit by its own flat normal, the whole of its
    /// color diffuse, with shadow rays meeting the blades around it, reads the meadow's grass in a
    /// wall's shadow far darker than the picture's. The grass is planted without the tangents its
    /// normal map is read along, so Bevy leaves a blade seen from behind facing away from the eye,
    /// and its mapped normal, which a way straight up leaves to a rounding error and a normal map
    /// to its lean, cannot tell a card's side, so the leaf material takes it by the card's own
    /// normal turned to the eye. The picture smooths the normal map through its mips where the
    /// reference reads its own size, so a coarser picture reads the grass farther over its
    /// reference than this one does.
    /// </remarks>
    [SkippableTheory]
    [InlineData(3f)]
    [InlineData(-3f)]
    public void AGrassPatchReadsInTheReferenceTheLightThePictureGivesIt(float sunZ)
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        var wind = Trees.Wind;
        Trees.Wind = Vec3.Zero;
        try
        {
            new PictureRun
            {
                Width = 384,
                Height = 256,
                Frames = 1200,
                Scene = ecs =>
                {
                    var eye = new Vec3(0f, 0.9f, 2.4f);
                    var camera = PictureRun.Camera(ecs, eye);
                    ecs.Set(camera, Transform.LookingAt(eye, new Vec3(0f, 0.2f, 0f), Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                    var floor = PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 40f, new Vec3(0f, -20f, 0f));
                    ecs.SetName(floor, "floor");
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                    ecs.Add(sun, Transform.LookingAt(new Vec3(0.4f, 2f, sunZ), Vec3.Zero, Vec3.UnitY));
                    Grass.Plant(ecs, Vec3.Zero, new Vec2(3f, 3f), new GrassSettings { Density = 12f, Height = 0.6f });
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("ready", _ => !supported || Render.PipelinesReady())
                .Wait(ShaderMaterialTests.Settled)
                .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 32, bounces: 0)))
                .Until("read", world => !supported || Read(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
                .Go();
        }
        finally
        {
            Trees.Wind = wind;
        }
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var (pixels, seen, met) = (0, 0.0, 0.0);
        for (var i = 0; i < reference.Regions.Length; i++)
        {
            if (!reference.Names.TryGetValue(reference.Regions[i], out var name) || !name.StartsWith("Grass patch")) continue;
            pixels++;
            seen += 0.2126 * picture[i * 3] + 0.7152 * picture[i * 3 + 1] + 0.0722 * picture[i * 3 + 2];
            met += 0.2126 * reference.Light[i * 3] + 0.7152 * reference.Light[i * 3 + 1] + 0.0722 * reference.Light[i * 3 + 2];
        }
        output.WriteLine($"the grass's {pixels} pixels read {seen / Math.Max(1, pixels):0.0000} in the picture and {met / Math.Max(1, pixels):0.0000} in the reference, {(seen / met - 1) * 100:+0;-0}%");
        Assert.True(pixels > reference.Regions.Length / 4, $"the grass covers {pixels} pixels");
        Assert.InRange(seen / met, 0.85, 1.15);
    }

    /// <summary>
    /// The feature test's meadow's grass, seen from where its scripted run's eye stands over it at
    /// Medium, draws every pixel a number.
    /// </summary>
    /// <remarks>
    /// A blade seen edge on reads its normal map's smallest levels, which average its normals to
    /// nothing, and normalizing nothing gives no number, so with the grass given its tangents 18 of
    /// the meadow's values were none, at the bottom of the view, and made the comparison's sums
    /// none, as 30 are here. The leaf material takes the card's own normal where the mapped one has
    /// no length, and the normal bent toward the way out where that has none.
    /// </remarks>
    [SkippableFact]
    public void TheMeadowsGrassDrawsEveryPixelANumber()
    {
        Needs.Shaders();
        string? read = null;
        var asked = false;
        var wind = Trees.Wind;
        Trees.Wind = Vec3.Zero;
        try
        {
            new PictureRun
            {
                Width = 1280,
                Height = 720,
                Frames = 1200,
                Scene = ecs =>
                {
                    var eye = new Vec3(12f, 2.66f, 7f);
                    var camera = PictureRun.Camera(ecs, eye);
                    ecs.Set(camera, Transform.LookingAt(eye, new Vec3(0f, 1.66f, -3f), Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 40f, new Vec3(0f, -20f, 0f));
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                    ecs.Add(sun, Transform.LookingAt(new Vec3(2f, 1f, 2f), Vec3.Zero, Vec3.UnitY));
                    Grass.Plant(ecs, Vec3.Zero, new Vec2(30f, 30f), new GrassSettings { Density = 3f, Height = 0.6f, Seed = 70 });
                    Render.SetSceneField(2, 0.25f);
                    Render.SetGlobalIllumination(GlobalIllumination.Medium);
                },
            }
                .Until("ready", _ => Render.PipelinesReady())
                .Wait(ShaderMaterialTests.Settled)
                .Until("read", world => Read(world.Resource<EcsWorld>(), ref asked, ref read, ecs => GiReference.BeginPicture(ecs, null)))
                .Go();
        }
        finally
        {
            Trees.Wind = wind;
        }

        var light = GiReference.LastPicture!.Value.Light;
        var none = light.Count(value => !float.IsFinite(value));
        output.WriteLine($"{none} of the picture's {light.Length} values are no number");
        Assert.Equal(0, none);
    }

    /// <summary>
    /// A cut-out card's normal map turns its light in the reference as in the picture, a map that
    /// leans every normal 45 degrees lighting a card lying flat under a low sun as both draw it.
    /// </summary>
    /// <remarks>
    /// The reference lit a face by its flat normal, so a cut-out material's normal map, a grass
    /// set's and a leaf set's, which lean a blade's normal across its width, held no light of its
    /// own there. A uniform map is the same at every mip, so the two read alike however far the
    /// card lies, where a blade's map is smoothed by the picture's mips and read at its own size by
    /// the reference.
    /// </remarks>
    [SkippableFact]
    public void ACutOutCardsNormalMapTurnsItsLightInTheReferenceAsInThePicture()
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        new PictureRun
        {
            Width = 64,
            Height = 64,
            Frames = 1200,
            Scene = ecs =>
            {
                var eye = new Vec3(0f, 4f, 0.01f);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Set(camera, Transform.LookingAt(eye, Vec3.Zero, -Vec3.UnitZ));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var white = new byte[16 * 16 * 4];
                Array.Fill(white, (byte)255);
                var leaning = new byte[16 * 16 * 4];
                for (var i = 0; i < leaning.Length; i += 4)
                    (leaning[i], leaning[i + 1], leaning[i + 2], leaning[i + 3]) = (218, 128, 218, 255);
                var mesh = Render.CreateMesh(MeshShape.Plane, 2f, 2f);
                Render.GenerateTangents(mesh);
                var card = ecs.Spawn();
                Render.SetMesh(ecs, card, mesh);
                Render.SetMaterial(ecs, card, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.8f, 0.8f, 0.8f, 1f),
                    BaseColorTexture = Render.CreateImage(white, 16, 16, srgb: true, mipmaps: false),
                    NormalMap = Render.CreateImage(leaning, 16, 16, srgb: false, mipmaps: false),
                    Roughness = 1f,
                    AlphaMode = AlphaMode.Mask,
                    DoubleSided = true,
                }));
                ecs.Add(card, Transform.Identity);
                ecs.SetName(card, "card");
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 1f, 0f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Wait(ShaderMaterialTests.Settled)
            .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 16, bounces: 0)))
            .Until("read", world => !supported || Read(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var (pixels, seen, met) = (0, 0.0, 0.0);
        for (var i = 0; i < reference.Regions.Length; i++)
        {
            if (!IsCard(reference, reference.Regions[i])) continue;
            pixels++;
            seen += picture[i * 3 + 1];
            met += reference.Light[i * 3 + 1];
        }
        output.WriteLine($"the card's {pixels} pixels read {seen / Math.Max(1, pixels):0.0000} in the picture and {met / Math.Max(1, pixels):0.0000} in the reference");
        Assert.True(pixels > reference.Regions.Length / 4, $"the card covers {pixels} pixels");
        Assert.InRange(seen / met, 0.9, 1.1);
    }

    /// <summary>
    /// A smooth metal plane under a sky of one light reads in the reference that light times its
    /// Fresnel, as a mirror of its color shows the sky.
    /// </summary>
    /// <remarks>
    /// The reference's paths went on by the cosine about a face alone, the diffuse share past its
    /// metal, so a metal under the sky read black, and the light a gloss reflects, the sky at a
    /// grazing view off grass and walls, came into the picture and not the reference. The paths go
    /// on by the GGX lobe as well, one or the other drawn by their weights.
    /// </remarks>
    [SkippableFact]
    public void ASmoothMetalPlaneReflectsTheSkyInTheReference()
    {
        var (plane, sky) = PlaneUnderTheSky(color: 0.9f, metallic: 1f, roughness: 0.05f);
        output.WriteLine($"the metal plane reads {plane / sky:0.000} of the sky");
        Assert.InRange(plane / sky, 0.85, 1.05);
    }

    /// <summary>
    /// A rough gray dielectric plane seen at a grazing view under a sky of one light reads in the
    /// reference its diffuse share of the sky and Fresnel's share over it, 0.5 and about 0.15.
    /// </summary>
    /// <remarks>
    /// Bevy's table gives a dielectric of a reflectance of a half at a roughness of a half and a
    /// grazing view of a tenth a gloss of about 0.15 of the light around it, which the reference's
    /// paths take beside the diffuse share's 0.5.
    /// </remarks>
    [SkippableFact]
    public void ARoughDielectricAtAGrazingViewReadsFresnelsShareOfTheSkyOverItsDiffuse()
    {
        var (plane, sky) = PlaneUnderTheSky(color: 0.5f, metallic: 0f, roughness: 0.5f);
        output.WriteLine($"the gray plane reads {plane / sky:0.000} of the sky");
        Assert.InRange(plane / sky, 0.59, 0.72);
    }

    /// <summary>
    /// The reference's mean light over a plane four units a side lying at the origin, seen from six
    /// units off and a little over it at a grazing view, under a sky of one light and no sun, and
    /// over the sky beyond it, paths meeting the plane and going on once.
    /// </summary>
    private (double Plane, double Sky) PlaneUnderTheSky(float color, float metallic, float roughness)
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null;
        var asked = false;
        new PictureRun
        {
            Width = 128,
            Height = 128,
            Frames = 1200,
            Scene = ecs =>
            {
                var eye = new Vec3(0f, 0.6f, 6f);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Set(camera, Transform.LookingAt(eye, new Vec3(0f, 0.3f, 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 500f);
                var plane = ecs.Spawn();
                Render.SetMesh(ecs, plane, Render.CreateMesh(MeshShape.Plane, 4f, 4f));
                Render.SetMaterial(ecs, plane, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (color, color, color, 1f),
                    Metallic = metallic,
                    Roughness = roughness,
                }));
                ecs.Add(plane, Transform.Identity);
                ecs.SetName(plane, "plane");
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref asked, ref traced, ecs => GiReference.BeginReference(ecs, null, 256, bounces: 1)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var (planePixels, planeLight, skyPixels, skyLight) = (0, 0.0, 0, 0.0);
        for (var i = 0; i < reference.Regions.Length; i++)
        {
            var light = reference.Light[i * 3 + 1];
            if (reference.Regions[i] == 0)
                (skyPixels, skyLight) = (skyPixels + 1, skyLight + light);
            else if (reference.Names.TryGetValue(reference.Regions[i], out var name) && name.StartsWith("plane"))
                (planePixels, planeLight) = (planePixels + 1, planeLight + light);
        }
        Assert.True(planePixels > 200 && skyPixels > 200, $"the plane covers {planePixels} pixels and the sky {skyPixels}");
        return (planeLight / planePixels, skyLight / skyPixels);
    }

    /// <summary>
    /// The reference binds 32 masks where the device's shaders sample room for them and its other
    /// textures a stage, and 16 or 8 where they sample fewer, as wgpu's lowest default of 16 does,
    /// so a smaller device still makes the tracer.
    /// </summary>
    [Fact]
    public void TheReferenceBindsTheMasksTheDevicesTexturesAStageLeaveRoomFor()
    {
        Assert.Equal(32, GiReference.MasksFor(128));
        Assert.Equal(32, GiReference.MasksFor(40));
        Assert.Equal(16, GiReference.MasksFor(39));
        Assert.Equal(16, GiReference.MasksFor(24));
        Assert.Equal(8, GiReference.MasksFor(16));
        Assert.Equal(8, GiReference.MasksFor(0));
    }

    // Whether a pixel's region is the card's, facing either way.
    private static bool IsCard(GiReference.Traced traced, uint region) => traced.Names.TryGetValue(region, out var name) && name.StartsWith("card");

    /// <summary>
    /// The reference of a white floor and a card of a checkered texture, green where it is whole
    /// and cut out where its alpha is none, or whole everywhere, lying a twentieth of a unit over
    /// the floor, so the eye sees through each hole what lies under it, under a sun straight
    /// overhead and seen from above, or standing two units tall at x -2 under a sun 45 degrees up
    /// from -x and seen from above its shadow, the direct light alone.
    /// </summary>
    private static GiReference.Traced Looked(bool cutOut, bool standing, out bool supported)
    {
        var rays = false;
        string? traced = null;
        var asked = false;
        new PictureRun
        {
            Width = 96,
            Height = 96,
            Frames = 900,
            Scene = ecs =>
            {
                var (eye, at) = standing ? (new Vec3(-1f, 6f, 0f), new Vec3(-1f, 0f, 0f)) : (new Vec3(0f, 5f, 0f), Vec3.Zero);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Add(camera, Transform.LookingAt(eye, at, -Vec3.UnitZ));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var floor = PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 40f, new Vec3(0f, -20f, 0f));
                ecs.SetName(floor, "floor");

                var pixels = new byte[64 * 64 * 4];
                for (var y = 0; y < 64; y++)
                    for (var x = 0; x < 64; x++)
                    {
                        var whole = (x / 8 + y / 8) % 2 == 0;
                        var i = (y * 64 + x) * 4;
                        (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (0, 255, 0, whole ? (byte)255 : (byte)0);
                    }
                var texture = Render.CreateImage(pixels, 64, 64, srgb: true, mipmaps: false);
                var card = ecs.Spawn();
                Render.SetMesh(ecs, card, Render.CreateMesh(MeshShape.Plane, 2f, 2f));
                Render.SetMaterial(ecs, card, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColorTexture = texture,
                    AlphaMode = cutOut ? AlphaMode.Mask : AlphaMode.Opaque,
                    AlphaCutoff = 0.5f,
                    DoubleSided = true,
                }));
                ecs.Add(card, standing
                    ? new Transform(new Vec3(-2f, 1f, 0f), Quat.FromRotationZ(MathF.PI / 2f), Vec3.One)
                    : Transform.At(0f, 0.05f, 0f));
                ecs.SetName(card, "card");

                var toSun = standing ? new Vec3(-1f, 1f, 0f) : new Vec3(0f, 1f, 0.001f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(toSun, Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => rays = Shaders.SupportsRayQueries)
            .Until("ready", _ => !rays || Render.PipelinesReady())
            .Wait(3)
            .Until("traced", world => !rays || Read(world.Resource<EcsWorld>(), ref asked, ref traced, ecs => GiReference.BeginReference(ecs, null, 16, bounces: 0)))
            .Go();
        supported = rays;
        return rays ? GiReference.Last! : null!;
    }

    // Asks for a read or a reference once, then moves it on a frame at a time until it answers.
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

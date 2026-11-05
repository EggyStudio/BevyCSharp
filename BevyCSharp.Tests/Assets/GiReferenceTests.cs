using Bevy;
using Bevy.Reflected;
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
    /// leaves them so when it is turned off. The camera then drew no prepass, and Bevy draws a
    /// deferred material in its G-buffer alone, so the picture was black from then on.
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
    /// The world's first 3D camera was taken where the field lay around none, which in the feature
    /// test's yard was a camera drawing nothing, so a read with global illumination off asked for
    /// the picture's size until it gave up.
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
    /// The reference was lit by the sun as it is above the air, the light's own color and strength,
    /// so every sunlit face of the feature test's yard at eight in the morning read 30 to 45% over
    /// the picture and whiter, whatever global illumination gave the faces in shade.
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
    /// The reference read the material buffer's base color alone, so a cut-out card, a leaf or a
    /// grass card, was traced whole, and a material the buffer did not know, a leaf's, as black,
    /// and the meadow's grass and its crowns read nothing and hid the floor behind them. The card
    /// read the whole of its footprint and the floor through it none, and reads 0.500 of it, the
    /// floor through it 1.049 of the open floor's light, the floor's highlight under the eye and
    /// the sun overhead.
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
    /// lets the sun through there. The cut-out card's shadow read 0.001 of the open floor's light
    /// as the opaque card's does, and reads 0.501.
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
    /// The reference held every shown mesh, so a tree was traced at every level at once, its leaves,
    /// its reduced leaves and its far cards over each other, and in the feature test's meadow its far
    /// cards stood as planes over the crowns. Here the level not drawn is the larger of two cubes
    /// about the same middle, so where it was traced the eye met it first, the far level from near
    /// and the near one from far.
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

using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A patch of grass under a uniform sky and no sun, global illumination at Medium, reads its
    /// blades' light within 15% of its reference's.
    /// </summary>
    /// <remarks>
    /// The grass is drawn forward with the leaf material, whose gloss reflects the light the probes
    /// the blade sees hold along its mirror way in place of Bevy's reflection of its ambient term
    /// and its environment map. The probes' sky holds the ambient light as Bevy's lighting returns
    /// it from a surface facing the view, a share of 0.45 of it, and the reference's paths bring
    /// that back where they meet nothing, where Bevy's ambient term reflects the ambient light's
    /// whole color. Reflecting Bevy's, the blades read 40% over their reference, and reflecting the
    /// probes' light 1% over.
    /// </remarks>
    [SkippableFact]
    public void AGrassPatchUnderAUniformSkyReadsItsReference()
    {
        var (pixels, seen, met) = ReadUnderUniformSky(new Vec3(0f, 0.9f, 2.4f), new Vec3(0f, 0.2f, 0f),
            ecs =>
            {
                Grass.Plant(ecs, Vec3.Zero, new Vec2(3f, 3f), new GrassSettings { Density = 12f, Height = 0.6f });
                return null;
            },
            name => name.StartsWith("Grass patch", StringComparison.Ordinal));
        output.WriteLine($"the grass's {pixels} pixels read {seen:0.0000} in the picture and {met:0.0000} in the reference, {(seen / met - 1) * 100:+0;-0}%");
        Assert.True(pixels > 320 * 180 / 4, $"the grass covers {pixels} pixels");
        Assert.InRange(seen / met, 0.85, 1.15);
    }

    /// <summary>
    /// A lone aspen's leaves under a uniform sky and no sun, global illumination at Medium, seen
    /// from 16 m at 320 by 180, read within 10% of their reference's.
    /// </summary>
    /// <remarks>
    /// Seen from there a leaf is a few pixels across, and its card samples its atlas's levels where
    /// a leaf is two texels or so across. Those levels' color and translucency were the box's means
    /// over the leaf and the transparent texels round it, the photograph's light background and a
    /// translucency of none, so the aspen's leaves took a color half as bright again as their own
    /// and let through 0.094 of their light where they let through 0.235, and read 17% over their
    /// reference. Each level's color and surface weighted by how much of each texel the leaves
    /// cover, the leaves read 4% over. The gaps between them show the sky, which the reference's
    /// paths bring back where they pass between the leaves, so the picture's background is the
    /// reference's sky (<see cref="ReadUnderUniformSky"/>), where over black the leaves read 37%
    /// under their reference.
    /// </remarks>
    [SkippableFact]
    public void ALoneAspensLeavesUnderAUniformSkyReadTheirReference()
    {
        var (pixels, seen, met) = ReadUnderUniformSky(new Vec3(0f, 5f, 16f), new Vec3(0f, 5f, 0f),
            ecs => Trees.Plant(ecs, "quaking-aspen", 3, Vec3.Zero),
            name => name.Contains(", leaves", StringComparison.Ordinal));
        output.WriteLine($"the aspen's leaves' {pixels} pixels read {seen:0.0000} in the picture and {met:0.0000} in the reference, {(seen / met - 1) * 100:+0;-0}%");
        Assert.InRange(seen / met, 0.9, 1.1);
    }

    /// <summary>
    /// What is planted, seen from eye toward at under a uniform white sky of ambient light and no
    /// sun with global illumination at Medium, read at 320 by 180 against its reference traced at
    /// 128 paths a pixel, giving how many pixels hold the regions counted names and their mean light
    /// in the picture and in the reference, in luma.
    /// </summary>
    /// <remarks>
    /// The picture's background is the reference's sky, the light its paths bring back where they
    /// meet nothing, read where the reference's pixels meet nothing and set as the clear color
    /// before the picture is read, since a pixel of a crown is partly the sky past its leaves,
    /// which the reference's paths spread across the pixel bring back and the picture shows only
    /// where it draws no leaf.
    /// </remarks>
    private (int Pixels, double Seen, double Met) ReadUnderUniformSky(Vec3 eye, Vec3 at, Func<EcsWorld, Entity?> plant, Func<string, bool> counted)
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        Entity? planted = null;
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
                    var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.World });
                    Render.SetClearColor((0f, 0f, 0f, 1f));
                    ecs.Add(camera, Transform.LookingAt(eye, at, Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 2000f);
                    var floor = PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 40f, new Vec3(0f, -20f, 0f));
                    ecs.SetName(floor, "floor");
                    planted = plant(ecs);
                    Render.SetSceneField(4, 0.25f);
                    Render.SetGlobalIllumination(GlobalIllumination.Medium);
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("ready", _ => !supported || Render.PipelinesReady())
                .Until("dressed", world => planted is not { } tree || world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0)
                .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 240)
                .Until("drawn", _ => Render.PipelinesReady())
                .Wait(120)
                .Until("drawn again", _ => Render.PipelinesReady())
                .Until("referenced", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 128)))
                .Do("the sky behind", _ =>
                {
                    if (supported && Sky(GiReference.Last!) is { } sky) Render.SetClearColor((sky.R, sky.G, sky.B, 1f));
                })
                .Wait(10)
                .Until("read", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
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
            if (!reference.Names.TryGetValue(reference.Regions[i], out var name) || !counted(name)) continue;
            pixels++;
            seen += 0.2126 * picture[i * 3] + 0.7152 * picture[i * 3 + 1] + 0.0722 * picture[i * 3 + 2];
            met += 0.2126 * reference.Light[i * 3] + 0.7152 * reference.Light[i * 3 + 1] + 0.0722 * reference.Light[i * 3 + 2];
        }
        Assert.True(pixels > reference.Regions.Length / 50, $"what is read covers {pixels} pixels");
        return (pixels, seen / pixels, met / pixels);
    }

    // The reference's mean light where its pixels meet nothing, or none where every pixel meets
    // something.
    private static (float R, float G, float B)? Sky(GiReference.Traced reference)
    {
        var (count, r, g, b) = (0, 0.0, 0.0, 0.0);
        for (var i = 0; i < reference.Regions.Length; i++)
        {
            if (reference.Regions[i] != 0) continue;
            (count, r, g, b) = (count + 1, r + reference.Light[i * 3], g + reference.Light[i * 3 + 1], b + reference.Light[i * 3 + 2]);
        }
        return count == 0 ? null : ((float)(r / count), (float)(g / count), (float)(b / count));
    }

    // Asks for a reading once and answers whether it is done.
    private static bool ReadOnce(EcsWorld ecs, ref bool asked, ref string? said, Func<EcsWorld, string?> begin)
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

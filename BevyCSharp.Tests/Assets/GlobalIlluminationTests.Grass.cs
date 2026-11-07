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
                Width = 320,
                Height = 180,
                Frames = 6000,
                Scene = ecs =>
                {
                    var eye = new Vec3(0f, 0.9f, 2.4f);
                    var camera = PictureRun.Camera(ecs, eye);
                    ecs.Set(camera, Transform.LookingAt(eye, new Vec3(0f, 0.2f, 0f), Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 2000f);
                    var floor = PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 40f, new Vec3(0f, -20f, 0f));
                    ecs.SetName(floor, "floor");
                    Grass.Plant(ecs, Vec3.Zero, new Vec2(3f, 3f), new GrassSettings { Density = 12f, Height = 0.6f });
                    Render.SetSceneField(4, 0.25f);
                    Render.SetGlobalIllumination(GlobalIllumination.Medium);
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("ready", _ => !supported || Render.PipelinesReady())
                .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 240)
                .Until("drawn", _ => Render.PipelinesReady())
                .Wait(120)
                .Until("drawn again", _ => Render.PipelinesReady())
                .Until("referenced", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 128)))
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
            if (!reference.Names.TryGetValue(reference.Regions[i], out var name) || !name.StartsWith("Grass patch")) continue;
            pixels++;
            seen += 0.2126 * picture[i * 3] + 0.7152 * picture[i * 3 + 1] + 0.0722 * picture[i * 3 + 2];
            met += 0.2126 * reference.Light[i * 3] + 0.7152 * reference.Light[i * 3 + 1] + 0.0722 * reference.Light[i * 3 + 2];
        }
        output.WriteLine($"the grass's {pixels} pixels read {seen / Math.Max(1, pixels):0.0000} in the picture and {met / Math.Max(1, pixels):0.0000} in the reference, {(seen / met - 1) * 100:+0;-0}%");
        Assert.True(pixels > reference.Regions.Length / 4, $"the grass covers {pixels} pixels");
        Assert.InRange(seen / met, 0.85, 1.15);
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

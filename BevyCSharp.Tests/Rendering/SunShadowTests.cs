using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// The sun's shadow map as it shades what it lights, a wall the sun grazes read against the
/// path-traced reference's sun.
/// </summary>
[Collection("engine")]
public sealed class SunShadowTests(ITestOutputHelper output)
{
    /// <summary>
    /// A white wall forty units off that the sun lights at a grazing 70 degrees from its normal
    /// reads in the picture the light the reference's sun gives it, within 5%.
    /// </summary>
    /// <remarks>
    /// The light's normal bias is Bevy's for each kind of light, 1.8 texels for a directional light
    /// and a spot light, since at a smaller bias a face the sun grazes in a far cascade falls under
    /// its own shadow over a share of its filter, as the feature test's meadow's walls do where the
    /// sun grazes them.
    /// </remarks>
    [SkippableFact]
    public void AWallTheSunGrazesReadsTheReferencesSun()
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 1200,
            Scene = ecs =>
            {
                var eye = new Vec3(0f, 2f, 0f);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Set(camera, Transform.LookingAt(eye, new Vec3(0f, 4f, -40f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var wall = PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f), 1f, new Vec3(0f, 5f, -40.5f));
                ecs.Set(wall, new Transform { Translation = new Vec3(0f, 5f, -40.5f), Rotation = Quat.Identity, Scale = new Vec3(40f, 20f, 1f) });
                ecs.SetName(wall, "wall");
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0.9f, 0.25f, 0.35f), Vec3.Zero, Vec3.UnitY));
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
            if (!reference.Names.TryGetValue(reference.Regions[i], out var name) || !name.StartsWith("wall")) continue;
            pixels++;
            seen += picture[i * 3 + 1];
            met += reference.Light[i * 3 + 1];
        }
        output.WriteLine($"the wall's {pixels} pixels read {seen / Math.Max(1, pixels):0.0000} in the picture and {met / Math.Max(1, pixels):0.0000} in the reference, {(seen / met - 1) * 100:+0;-0}%");
        Assert.True(pixels > reference.Regions.Length / 4, $"the wall covers {pixels} pixels");
        Assert.InRange(seen / met, 0.95, 1.05);
    }

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

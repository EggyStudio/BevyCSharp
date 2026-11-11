using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// An app that never turns global illumination on makes none of its programs and none of the
    /// scene's distance field's, traces no frame and builds no field.
    /// </summary>
    /// <remarks>
    /// On a device that draws on the CPU each program takes minutes to compile, so a program made
    /// for nothing at the start slows every frame until it is done.
    /// </remarks>
    [SkippableFact]
    public void AnAppWithGlobalIlluminationOffMakesNoneOfItsPrograms()
    {
        Needs.Shaders();
        var made = new List<string>();
        var (traced, planned) = (-1L, true);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 600,
            Scene = OffScene,
        }
            .Wait(60)
            .Until("ready", _ => Render.PipelinesReady())
            .Do("read", _ =>
            {
                made.AddRange(Shaders.Programs.Select(program => program.Files));
                (traced, planned) = (WorldProbes.TracedFrames, SceneField.Plan is not null);
            })
            .Go();

        var illumination = made.Where(files => files.Contains("gi_", StringComparison.Ordinal) || files.Contains("field_", StringComparison.Ordinal)).ToList();
        Assert.True(illumination.Count == 0, $"the app made {string.Join("; ", illumination)}");
        Assert.Equal(0, traced);
        Assert.False(planned, "the app built the scene's distance field");
    }

    /// <summary>
    /// An app that turns global illumination on and then off traces no frame more once it is off,
    /// and lets the scene's distance field it built go.
    /// </summary>
    [SkippableFact]
    public void GlobalIlluminationTurnedOffTracesNoMoreAndLetsItsFieldGo()
    {
        Needs.Shaders();
        var supported = false;
        var (off, later, planned) = (-1L, -1L, true);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 3000,
            Scene = ecs =>
            {
                OffScene(ecs);
                Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 60 && SceneField.Plan is not null)
            .Do("off", _ =>
            {
                if (supported) Render.SetGlobalIllumination(GlobalIllumination.Off);
            })
            .Wait(10)
            .Do("read off", _ => off = WorldProbes.TracedFrames)
            .Wait(30)
            .Do("read later", _ => (later, planned) = (WorldProbes.TracedFrames, SceneField.Plan is not null))
            .Go();
        Needs.RayQueries(supported);

        Assert.Equal(off, later);
        Assert.False(planned, "the scene's distance field stood thirty frames after global illumination went off");
    }

    // A floor, a block and a sun, from a camera above the floor.
    private static void OffScene(EcsWorld ecs)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.7f, 6f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 6f), new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
        var white = Render.CreateMaterial(0.8f, 0.8f, 0.8f);
        PictureRun.Cube(ecs, white, 40f, new Vec3(0f, -20f, 0f));
        PictureRun.Cube(ecs, white, 1f, new Vec3(0f, 0.5f, 0f));
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
        ecs.Add(sun, Transform.LookingAt(new Vec3(0.4f, 0.8f, 0.3f), Vec3.Zero, Vec3.UnitY));
    }
}

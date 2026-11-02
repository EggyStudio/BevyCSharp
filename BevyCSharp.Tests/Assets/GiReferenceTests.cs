using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The measure global illumination is read by (<see cref="GiReference"/>), the camera's picture read
/// with global illumination off as with it on, a read that never answers given up before the console
/// gives it up, and the reference lit by the sun as Bevy lights the picture, through the camera's air.
/// </summary>
[Collection("engine")]
public sealed class GiReferenceTests
{
    private const uint Width = 64, Height = 36;

    /// <summary>
    /// The picture is read where no global illumination is traced, before it was ever on and after it
    /// was turned off, as gi.compare reads the light with it off beside the light with it on.
    /// </summary>
    /// <remarks>
    /// Global illumination asks for Bevy's G-buffer, which draws every material deferred, and leaves
    /// them so when it is turned off. The camera then drew no prepass, and Bevy draws a deferred
    /// material in its G-buffer alone, so the picture was black from then on.
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
    /// A read whose copy never runs, its camera drawing nothing, gives itself up a few frames before
    /// the console gives the command up, saying where it stood, and the next read is taken, where the
    /// read once waited as long as the console and, given up by it, held the measure busy for good.
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
            .Do("putting the camera out", world => world.Resource<EcsWorld>().Wrap<CameraRef>(camera).IsActive = false)
            .Until("given up", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                if (!asked)
                {
                    asked = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                steps++;
                return (said = GiReference.Step(ecs)) is not null;
            })
            .Do("asking again", world =>
            {
                busy = GiReference.Busy;
                next = GiReference.BeginPicture(world.Resource<EcsWorld>(), null);
            })
            .Go();

        Assert.StartsWith("the picture's size was never read", said);
        Assert.InRange(steps, 1, (int)ConsoleHost.LaterFrames - 1);
        Assert.False(busy);
        Assert.Null(next);
    }

    /// <summary>
    /// The reference's sun reaches a white floor as Bevy's lighting gives it under the camera's air,
    /// ten degrees up, where the air dims the sun and reddens it, the first face's light alone in both.
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

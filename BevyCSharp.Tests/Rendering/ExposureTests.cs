using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Auto exposure taken off gives the picture back the exposure it had before, rather than the one
/// it had adjusted to, and on a camera with a viewport meters the camera's own part of the window.
/// </summary>
/// <remarks>
/// Bevy forgets the effect's buffer by the camera's own entity where it keeps it by the render
/// world's, so its pass would go on adjusting a dim scene brighter after the component was gone,
/// the effect turned on and never off. The bridge gives the camera a new entity in the render world
/// as the effect goes.
/// </remarks>
[Collection("engine")]
public sealed class ExposureTests
{
    [SkippableFact]
    public void AutoExposureTakenOffGivesTheExposureBack()
    {
        Needs.Renderer();

        var camera = Entity.None;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });

                // A gray cube under a dim light, which auto exposure brightens by more than the
                // fifteen levels asked below.
                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.5f, 0.5f, 0.5f, 1f), Roughness = 1f }), 3f);
                var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 300f, Shadows = false });
                ecs.Add(light, Transform.LookingAt(new Vec3(1f, 2f, 3f), Vec3.Zero, Vec3.UnitY));
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("before")
            .Do("on", _ => Render.SetEffects(camera, new EffectSettings { AutoExposure = true, SpeedBrighten = 20f }))
            .Wait(60)
            .Capture("adjusted")
            .Do("off", _ => Render.SetEffects(camera, new EffectSettings()))
            .Wait(30)
            .Capture("after")
            .Go();

        var before = Brightness(run.Picture("before"));
        var adjusted = Brightness(run.Picture("adjusted"));
        var after = Brightness(run.Picture("after"));

        // Without the adjustment showing, the last check would pass whether or not it came off.
        Assert.True(adjusted > before + 15, $"auto exposure did not brighten the dim cube, from {before:0.0} to {adjusted:0.0}, and {after:0.0} after");
        Assert.True(Math.Abs(after - before) < 3 + (before * 0.1), $"the exposure stayed at {after:0.0} after auto exposure came off, from {before:0.0} before it and {adjusted:0.0} with it");
    }

    /// <summary>
    /// Auto exposure on the camera drawing the right half of a window meters that half, and exposes
    /// its dark cube as it does drawing a window of that size alone, beside a camera drawing a white
    /// cube in the left half.
    /// </summary>
    /// <remarks>
    /// Bevy 0.20 meters a block the viewport's size from the picture's top left corner, the left
    /// camera's half, which the bridge mends (<see cref="Render.ExposureMeteringMended"/>).
    /// </remarks>
    [SkippableFact]
    public void AutoExposureMetersItsCamerasViewport()
    {
        Needs.Renderer();
        bool? mended = null;

        CapturedImage Picture(bool beside)
        {
            var run = new PictureRun
            {
                Width = beside ? 192u : 96u,
                Height = 96,

                // On a set frame time, so the exposure settles as far between starts on any device.
                Configure = config => config.FrameSeconds = 1.0 / 60,
                Scene = ecs =>
                {
                    // A white cube and a dark one far apart under the same dim light, each filling
                    // the view of the camera looking at it.
                    PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.9f, 0.9f, 0.9f, 1f), Roughness = 1f }), 3f, new Vec3(-20f, 0f, 0f));
                    PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.05f, 0.05f, 0.05f, 1f), Roughness = 1f }), 3f, new Vec3(20f, 0f, 0f));
                    var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 300f, Shadows = false });
                    ecs.Add(light, Transform.LookingAt(new Vec3(0f, 0f, 3f), Vec3.Zero, Vec3.UnitY));

                    Entity Looking(float x, CameraSettings settings)
                    {
                        var camera = Render.SpawnCamera3d(settings);
                        ecs.Add(camera, Transform.LookingAt(new Vec3(x, 0f, 3.5f), new Vec3(x, 0f, 0f), Vec3.UnitY));
                        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                        return camera;
                    }

                    var black = new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) };
                    if (beside) Looking(-20f, PictureRun.LeftHalf(black, 192, 96));
                    var dark = Looking(20f, beside ? new CameraSettings { Viewport = (96, 0, 96, 96), Order = 1, Clear = ClearMode.Keep } : black);
                    Render.SetEffects(dark, new EffectSettings { AutoExposure = true, SpeedBrighten = 20f, SpeedDarken = 20f });
                },
            };

            run.Wait(ShaderMaterialTests.Settled)
                .Wait(90)
                .Do("reading the mend", _ => mended = Render.ExposureMeteringMended)
                .Capture("picture")
                .Go();
            return run.Picture("picture");
        }

        var (alone, beside) = (Picture(false), Picture(true));
        double Mean(CapturedImage picture, uint fromX)
        {
            long sum = 0;
            for (var y = 0u; y < 96; y++)
                for (var x = fromX; x < fromX + 96; x++)
                {
                    var pixel = picture.At(x, y);
                    sum += pixel.R + pixel.G + pixel.B;
                }
            return sum / (96.0 * 96.0 * 3.0);
        }

        var (own, there) = (Mean(alone, 0), Mean(beside, 96));
        Assert.True(Math.Abs(own - there) < 3 + (own * 0.05), $"the right half's camera reads {there:0.0} beside a camera drawing a white cube, and {own:0.0} alone");
        Assert.True(mended == true, $"the exposure's mend reads {mended?.ToString() ?? "not loaded"}");
    }

    /// <summary>
    /// The mean of the color channels over the middle third of the picture, which the cube fills,
    /// so the black around it does not dilute what changed.
    /// </summary>
    private static double Brightness(CapturedImage picture)
    {
        var (fromX, toX, fromY, toY) = (picture.Width / 3, picture.Width * 2 / 3, picture.Height / 3, picture.Height * 2 / 3);
        long sum = 0;
        for (var y = fromY; y < toY; y++)
        {
            for (var x = fromX; x < toX; x++)
            {
                var pixel = picture.At(x, y);
                sum += pixel.R + pixel.G + pixel.B;
            }
        }

        return sum / (3.0 * (toX - fromX) * (toY - fromY));
    }
}

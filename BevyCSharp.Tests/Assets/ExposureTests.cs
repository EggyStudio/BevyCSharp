using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Auto exposure taken off gives the picture back the exposure it had before, rather than the one
/// it had adjusted to.
/// </summary>
/// <remarks>
/// Bevy 0.19 forgets the effect's buffer by the camera's own entity where it keeps it by the render
/// world's, so its pass went on adjusting a dim scene brighter after the component was gone, and
/// the feature test's effects page could turn it on but never off. The bridge gives the camera a
/// new entity in the render world as the effect goes.
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

                // A gray cube under a dim light, which auto exposure brings up a long way.
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

using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The point lights the render world held and drew, which Bevy's many_lights stress test logs.
/// </summary>
[Collection("engine")]
public sealed class LightCountTests
{
    [SkippableFact]
    public void ThePointLightsInViewAreHeldAndDrawn()
    {
        Needs.Renderer();

        (int Visible, int Rendered) after = default;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                foreach (var x in new[] { -1f, 0f, 1f })
                    ecs.Add(Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1000f }), Transform.At(x, 0f, 0f));
            },
            EachFrame = _ => after = Render.PointLightsDrawn,
        };

        run.Wait(10).Go();

        // The three lights in front of the camera, each held and each in the view's clusters.
        Assert.Equal((3, 3), after);
    }
}

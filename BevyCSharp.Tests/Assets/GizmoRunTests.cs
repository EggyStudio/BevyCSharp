using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers long runs of gizmo lines, which <see cref="Gizmos.Lines"/> builds in an array kept
/// between calls rather than one made each call.
/// </summary>
[Collection("engine")]
public sealed class GizmoRunTests
{
    /// <summary>
    /// Two long runs drawn one after the other each reach the picture whole, the second built in
    /// the array the first was built in, and neither drawn in the other's place.
    /// </summary>
    /// <remarks>
    /// Green lines across the top half and red across the bottom, more of each than a run built on
    /// the stack holds, so both go through the kept array.
    /// </remarks>
    [SkippableFact]
    public void LongRunsOfLinesDrawnInTurnAreEachDrawnWhole()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = ecs => PictureRun.Camera(ecs),
            EachFrame = _ =>
            {
                Gizmos.Lines(Rows(100, 0.3f, 2f, (0f, 1f, 0f, 1f)));
                Gizmos.Lines(Rows(80, -2f, -0.3f, (1f, 0f, 0f, 1f)));
            },
        };

        run.Wait(10).Capture("runs").Go();
        var picture = run.Picture("runs");

        var (greenTop, greenBottom) = Halves(picture, (r, g, b) => g > 120 && r < 90 && b < 90);
        var (redTop, redBottom) = Halves(picture, (r, g, b) => r > 120 && g < 90 && b < 90);

        Assert.True(greenTop > 100, $"the green run drew {greenTop} pixels in the top half");
        Assert.True(redBottom > 100, $"the red run drew {redBottom} pixels in the bottom half");
        Assert.Equal(0, greenBottom);
        Assert.Equal(0, redTop);
    }

    /// <summary>
    /// A 2D camera laid over a 3D scene draws the world's gizmos a unit a pixel about the window's
    /// middle, a line through the world's middle as a mark across the picture's, and put on a layer
    /// of its own with <see cref="Render.SetLayers"/> draws none of them, as
    /// <see cref="Render2d.SpawnCamera2d"/> says, the feature test's sprite overlay having left such
    /// marks at the middle of its window.
    /// </summary>
    [SkippableFact]
    public void A2dCameraOverA3dSceneDrawsNoGizmoOnALayerOfItsOwn()
    {
        Needs.Renderer();
        var (shared, own) = (RedInTheMiddle(ownLayer: false), RedInTheMiddle(ownLayer: true));
        Assert.True(shared > 20, $"the overlay on the gizmos' layer drew {shared} red pixels in the middle");
        Assert.Equal(0, own);
    }

    // How many red pixels lie within a few rows of the picture's middle, where a 2D camera over a
    // 3D one that looks away from the world's middle draws a red line through it, on the gizmos'
    // layer or on one of its own.
    private static int RedInTheMiddle(bool ownLayer)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 0f, 6f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), new Vec3(0f, 0f, 12f), Vec3.UnitY));
                var overlay = Render2d.SpawnCamera2d(order: 1);
                if (ownLayer) Render.SetLayers(ecs, overlay, 1u << 1);
            },
            EachFrame = _ => Gizmos.Line(new Vec3(-20f, 0f, 0f), new Vec3(20f, 0f, 0f), (1f, 0f, 0f, 1f)),
        };

        run.Wait(10).Capture("overlay").Go();
        var picture = run.Picture("overlay");
        var red = 0;
        for (var y = picture.Height / 2 - 4; y < picture.Height / 2 + 4; y++)
            for (var x = 0u; x < picture.Width; x++)
            {
                var (r, g, b, _) = picture.At(x, y);
                if (r > 120 && g < 90 && b < 90) red++;
            }
        return red;
    }

    /// <summary>
    /// A line crosses as the bridge reads it, each field where the bridge asserts it is, since a
    /// mirror the right size with one field out of place draws every line from its neighbor's
    /// numbers.
    /// </summary>
    [Theory]
    [InlineData(nameof(NativeGizmoSegment.EndX), 12)]
    [InlineData(nameof(NativeGizmoSegment.ColorR), 24)]
    [InlineData(nameof(NativeGizmoSegment.EndColorR), 40)]
    [InlineData(nameof(NativeGizmoSegment.Fades), 56)]
    public void ALineIsLaidOutAsTheBridgeReadsIt(string field, int offset)
    {
        Assert.Equal(offset, (int)Marshal.OffsetOf<NativeGizmoSegment>(field));
        Assert.Equal(60, Marshal.SizeOf<NativeGizmoSegment>());
    }

    /// <summary>Horizontal lines four units long, spread evenly between two heights.</summary>
    private static GizmoSegment[] Rows(int count, float from, float to, (float R, float G, float B, float A) color)
    {
        var rows = new GizmoSegment[count];
        for (var i = 0; i < count; i++)
        {
            var y = from + (to - from) * i / (count - 1);
            rows[i] = new GizmoSegment(new Vec3(-2f, y, 0f), new Vec3(2f, y, 0f), color);
        }

        return rows;
    }

    /// <summary>How many pixels pass a test in the picture's top half and in its bottom half.</summary>
    private static (int Top, int Bottom) Halves(CapturedImage picture, Func<byte, byte, byte, bool> test)
    {
        var (top, bottom) = (0, 0);
        for (var y = 0; y < picture.Height; y++)
        {
            for (var x = 0; x < picture.Width; x++)
            {
                var at = (int)((y * picture.Width + x) * 4);
                if (!test(picture.Pixels[at], picture.Pixels[at + 1], picture.Pixels[at + 2])) continue;
                if (y < picture.Height / 2) top++;
                else bottom++;
            }
        }

        return (top, bottom);
    }
}

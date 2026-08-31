using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers debug drawing.
/// </summary>
/// <remarks>
/// Gizmos are drawn by a plugin that comes with the window, so every one of these runs windowless
/// and asserts the refusal. What a line looks like is confirmed by running the sample, which
/// draws one along each orbit.
/// </remarks>
[Collection("engine")]
public sealed class GizmoTests
{
    [Fact]
    public void EveryShapeIsRefusedWithoutAWindow()
    {
        // The refusal is the contract here: a windowless run must say so rather than collect
        // shapes that nothing will ever draw, which would look like a silent failure.
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Update, _ =>
        {
            var line = Assert.Throws<BevyNativeException>(
                () => Gizmos.Line(Vec3.Zero, Vec3.UnitY, (1f, 1f, 0f, 1f)));
            var sphere = Assert.Throws<BevyNativeException>(
                () => Gizmos.Sphere(Vec3.Zero, 1f, (1f, 0f, 0f, 1f)));
            var axes = Assert.Throws<BevyNativeException>(
                () => Gizmos.Axes(Transform.Identity, 2f));

            foreach (var status in new[] { line.Status, sphere.Status, axes.Status })
                Assert.Equal(NativeStatus.Unsupported, status);

            Assert.Contains("HasRenderer", line.Message);
        });

        harness.Run();
    }

    [Fact]
    public void DrawingIsSafeToAttemptEveryFrame()
    {
        // A behavior that draws unconditionally should fail the same way on every frame rather
        // than corrupting anything or growing a queue that is never drained.
        using var harness = new EngineHarness(frames: 6);
        var attempts = 0;
        var failures = 0;

        harness.OnContext(Stage.Update, _ =>
        {
            attempts++;
            try
            {
                Gizmos.Line(Vec3.Zero, new Vec3(0f, attempts, 0f), (0f, 1f, 0f, 1f));
            }
            catch (BevyNativeException)
            {
                failures++;
            }
        });

        harness.Run();

        Assert.True(attempts > 1);
        Assert.Equal(attempts, failures);
    }
}

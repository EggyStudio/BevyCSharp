using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the debug overlay, F3 let go alone turning it over and a chord on F3 not, the key moved,
/// and its lines saying the frame, the camera, the world, the system and the renderer of a scene
/// whose numbers are known.
/// </summary>
/// <remarks>
/// The key is set to F3 before each test, since an example that holds F3 turns it off in the
/// process the suite shares with it, and the overlay and its key are put back after.
/// </remarks>
[Collection("engine")]
public sealed class DebugOverlayTests : IDisposable
{
    public DebugOverlayTests() => Debug.OverlayKey = Key.F3;

    public void Dispose()
    {
        Debug.OverlayShown = false;
        Debug.OverlayKey = Key.F3;
    }

    /// <summary>The scene's counts at the offsets the bridge writes them, which its own test holds to the same numbers.</summary>
    [Fact]
    public void TheSceneCountsLieWhereTheBridgeWritesThem()
    {
        Assert.Equal(8, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Triangles)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Directional)).ToInt32());
        Assert.Equal(28, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Shadowed)).ToInt32());
        Assert.Equal(32, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Camera)).ToInt32());
        Assert.Equal(40, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Fov)).ToInt32());
        Assert.Equal(44, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.X)).ToInt32());
        Assert.Equal(56, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.ForwardX)).ToInt32());
        Assert.Equal(72, Marshal.SizeOf<NativeSceneCounts>());
    }

    [SkippableFact]
    public void F3LetGoAloneTurnsTheOverlayOverAndAChordOnItDoesNot()
    {
        Needs.Renderer();
        var seen = new List<bool>();

        var run = new PictureRun { Width = 64, Height = 48, Scene = ecs => PictureRun.Camera(ecs) };
        run.Wait(3)
            .Do("holding F3", _ => SyntheticInput.Press(Key.F3))
            .Wait(2)
            .Do("letting it go", _ => SyntheticInput.Lift(Key.F3))
            .Wait(2)
            .Do("reading", _ => seen.Add(Debug.OverlayShown))
            .Do("holding F3 again", _ => SyntheticInput.Press(Key.F3))
            .Wait(2)
            .Do("pressing F4 with it", _ => SyntheticInput.Tap(Key.F4))
            .Wait(2)
            .Do("letting F3 go", _ => SyntheticInput.Lift(Key.F3))
            .Wait(2)
            .Do("reading again", _ => seen.Add(Debug.OverlayShown))
            .Do("moving the key and pressing F3", _ =>
            {
                Debug.OverlayKey = Key.F6;
                SyntheticInput.Tap(Key.F3);
            })
            .Wait(3)
            .Do("reading after", _ => seen.Add(Debug.OverlayShown))
            .Do("pressing the moved key", _ => SyntheticInput.Tap(Key.F6))
            .Wait(3)
            .Do("reading last", _ => seen.Add(Debug.OverlayShown))
            .Go();

        // Shown by F3 alone, kept by the chord, kept by F3 once moved, and hidden by the moved key.
        Assert.Equal(new[] { true, true, true, false }, seen);
    }

    [SkippableFact]
    public void TheOverlaySaysTheFrameTheCameraTheWorldTheSystemAndTheRenderer()
    {
        Needs.Renderer();
        List<string> left = [], right = [];

        // A cube of twelve triangles seen from up and behind along -Z, a directional light with
        // shadow maps and a point light without.
        var run = new PictureRun
        {
            Width = 96,
            Height = 64,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f));
                Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
                Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Shadows = false });
            },
        };
        run.Wait(10).Do("reading the lines", world => (left, right) = Debug.Lines(world)).Go();

        Assert.StartsWith("BevyCSharp ", left[0]);
        Assert.Contains(" fps, ", left[1]);
        Assert.Contains("XYZ 0.000 / 2.000 / 6.000", left);
        Assert.Contains(left, line => line.StartsWith("Facing north (towards negative Z), heading 0.0, pitch -18.4", StringComparison.Ordinal));
        Assert.Contains("Field of view 45 degrees", left);
        Assert.Contains(left, line => line.StartsWith("Entities ", StringComparison.Ordinal));
        Assert.Contains(left, line => line.StartsWith("Meshes ", StringComparison.Ordinal));

        Assert.Contains(right, line => line.StartsWith(".NET ", StringComparison.Ordinal));
        Assert.Contains(right, line => line.StartsWith("CPU ", StringComparison.Ordinal));
        Assert.Contains(right, line => line.StartsWith("GPU ", StringComparison.Ordinal));
        Assert.Contains("Display offscreen, 96 x 64", right);
        Assert.Contains("Meshes in view 1, triangles 12", right);
        Assert.Contains("Lights 1 directional, 1 point, 0 spot, 1 with shadow maps", right);
    }
}

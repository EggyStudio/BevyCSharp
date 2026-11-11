using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the screenshot key, a press of F2 through the input writing a picture of the frame into
/// the screenshots folder, the program still seeing the press, two pictures in one second given two
/// names, and the key moved to another or turned off.
/// </summary>
/// <remarks>
/// A headless run here draws nothing, so the picture is taken in an offscreen one, which draws the
/// frame into an image as a window would be drawn. The folder is the test's own, and the key and
/// the folder are put back as they were after each test.
/// </remarks>
[Collection("engine")]
public sealed class ScreenshotKeyTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-screenshots-");
    private readonly string _folderBefore = Capture.ScreenshotFolder;

    public ScreenshotKeyTests() => Capture.ScreenshotFolder = _folder.Path;

    public void Dispose()
    {
        Capture.ScreenshotKey = Key.F2;
        Capture.ScreenshotFolder = _folderBefore;
        _folder.Dispose();
    }

    [Fact]
    public void ASecondAndAThirdPictureInOneSecondAreCountedFromTwo()
    {
        var at = new DateTime(2026, 10, 21, 3, 45, 12);
        var taken = new HashSet<string>();
        string Next()
        {
            var path = Capture.ScreenshotName("shots", at, taken.Contains);
            taken.Add(path);
            return path;
        }

        Assert.Equal(Path.Combine("shots", "2026-10-21_03.45.12.png"), Next());
        Assert.Equal(Path.Combine("shots", "2026-10-21_03.45.12_2.png"), Next());
        Assert.Equal(Path.Combine("shots", "2026-10-21_03.45.12_3.png"), Next());
        Assert.Equal(Path.Combine("shots", "2026-10-21_03.45.13.png"), Capture.ScreenshotName("shots", at.AddSeconds(1), taken.Contains));
    }

    [SkippableFact]
    public void F2WritesTwoPicturesOfTheFrameAndTheProgramStillSeesThePress()
    {
        Needs.Renderer();
        var seen = 0;

        Press(Key.F2, presses: 2, (_, world) => { if (world.Resource<Input>().KeyPressed(Key.F2)) seen++; });

        var pictures = Directory.GetFiles(_folder.Path, "*.png").Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(2, pictures.Length);
        Assert.Equal(2, seen);
        Assert.All(pictures, picture =>
        {
            var frame = AnimationReader.Png(File.ReadAllBytes(picture)).Frames[0];
            Assert.Equal((48, 32), (frame.Width, frame.Height));
        });
    }

    [SkippableFact]
    public void TheKeyMovedTakesThePictureInItsPlaceAndNoneTakesNone()
    {
        Needs.Renderer();

        Capture.ScreenshotKey = Key.F12;
        Press(Key.F2, presses: 1);
        Assert.Empty(Directory.GetFiles(_folder.Path));
        Press(Key.F12, presses: 1);
        Assert.Single(Directory.GetFiles(_folder.Path, "*.png"));

        Capture.ScreenshotKey = null;
        Press(Key.F12, presses: 1);
        Assert.Single(Directory.GetFiles(_folder.Path, "*.png"));
    }

    /// <summary>
    /// Runs an offscreen app of 48 by 32 that presses a key through the input as many times as
    /// asked, a few frames apart, and waits for the pictures to come back.
    /// </summary>
    private static void Press(Key key, int presses, Action<int, World>? eachFrame = null)
    {
        var frame = 0;
        var run = new PictureRun { Width = 48, Height = 32, EachFrame = world => eachFrame?.Invoke(frame++, world) };
        for (var press = 0; press < presses; press++) run.Wait(5).Do($"press {press + 1}", _ => SyntheticInput.Tap(key));
        run.Wait(20).Go();
    }
}

using System.Numerics;
using Bevy;
using ImGuiNET;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the hidden scope F1 hides, an interface node made inside it and an ImGui window drawn
/// inside it hidden and shown again, a window drawn outside it and the debug overlay left as they
/// were, and a node the program hid itself left hidden, all read from the frame's pixels.
/// </summary>
/// <remarks>
/// The key is set to F1 before each test, since the editor's start turns it off in the process the
/// suite shares with it, and the key, the state and the overlay are put back after.
/// </remarks>
[Collection("engine")]
public sealed class HiddenScopeTests : IDisposable
{
    public HiddenScopeTests() => Ui.HiddenKey = Key.F1;

    public void Dispose()
    {
        Ui.Hidden = false;
        Ui.HiddenKey = Key.F1;
        Debug.OverlayShown = false;
    }

    [Fact]
    public void ClosingAScopeNoneOpenedIsRefused() => Assert.Throws<InvalidOperationException>(Ui.EndHidden);

    [SkippableFact]
    public void F1HidesTheScopesNodeAndWindowAndLeavesTheWindowOutsideItAndTheOverlay()
    {
        Needs.Renderer();

        // Along the bottom, below where the overlay's lines reach, from the left a node made inside
        // the scope, a node made inside it that the program hid itself, an ImGui window drawn inside
        // it and one drawn outside it.
        var run = new PictureRun
        {
            Width = 480,
            Height = 360,
            Configure = config => config.Gui = true,
            Scene = ecs =>
            {
                ImGuiRuntime.Start();
                PictureRun.Camera(ecs);
                Debug.OverlayShown = true;

                Ui.BeginHidden();
                Ui.SpawnNode(Square(20f, (1f, 0f, 0f, 1f)));
                var hiddenAlready = Ui.SpawnNode(Square(100f, (1f, 0f, 1f, 1f)));
                ecs.Set(hiddenAlready, new Visibility(VisibilityMode.Hidden));
                Ui.EndHidden();
            },
            EachFrame = world =>
            {
                var ctx = new BehaviorContext(world);
                ImGuiRuntime.Begin(ctx);
                if (Ui.BeginHidden()) Window("hud##hidden-scope", 200f, new Vector4(0f, 1f, 0f, 1f));
                Ui.EndHidden();
                Window("screen##hidden-scope", 340f, new Vector4(0f, 0f, 1f, 1f));
                ImGuiRuntime.End();
            },
        };

        run.Wait(20)
            .Capture("shown")
            .Do("pressing F1", _ => SyntheticInput.Tap(Key.F1))
            .Until("hiding", _ => Ui.Hidden)
            .Wait(5)
            .Capture("hidden")
            .Do("pressing F1 again", _ => SyntheticInput.Tap(Key.F1))
            .Until("showing", _ => !Ui.Hidden)
            .Wait(5)
            .Capture("again")
            .Go();

        var (shown, hidden, again) = (run.Picture("shown"), run.Picture("hidden"), run.Picture("again"));

        Assert.True(Is(shown, 50, 310, red: true), "the scope's node was not drawn before F1");
        Assert.True(Is(shown, 230, 310, green: true), "the scope's window was not drawn before F1");
        Assert.True(Is(hidden, 50, 310), "the scope's node was drawn after F1");
        Assert.True(Is(hidden, 230, 310), "the scope's window was drawn after F1");
        Assert.True(Is(again, 50, 310, red: true), "the scope's node did not come back on F1 again");
        Assert.True(Is(again, 230, 310, green: true), "the scope's window did not come back on F1 again");

        // What F1 leaves, the window outside the scope, the node the program hid and the overlay.
        foreach (var picture in new[] { shown, hidden, again })
        {
            Assert.True(Is(picture, 370, 310, blue: true), "the window outside the scope changed");
            Assert.True(Is(picture, 130, 310), "the node the program hid itself was drawn");
            Assert.True(PictureRun.Count(picture, (r, g, b) => r > 150 && g > 150 && b > 150) > 100, "the overlay's text is missing");
        }
    }

    /// <summary>A square of 60 logical pixels a bottom corner's 20 up, at a distance from the left.</summary>
    private static UiSettings Square(float left, (float R, float G, float B, float A) color) => new()
    {
        Absolute = true,
        Left = Length.Px(left),
        Bottom = Length.Px(20f),
        Width = Length.Px(60f),
        Height = Length.Px(60f),
        Color = color,
    };

    /// <summary>An ImGui window of one color and 60 logical pixels square, at a distance from the left along the bottom.</summary>
    private static void Window(string name, float left, Vector4 color)
    {
        ImGui.SetNextWindowPos(new Vector2(left, 280f), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(60f, 60f), ImGuiCond.Always);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, color);
        ImGui.Begin(name, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings);
        ImGui.End();
        ImGui.PopStyleColor();
    }

    /// <summary>Whether a pixel is the color named, or the clear black where none is.</summary>
    private static bool Is(CapturedImage picture, uint x, uint y, bool red = false, bool green = false, bool blue = false)
    {
        var (r, g, b, _) = picture.At(x, y);
        return (r > 150) == red && (g > 150) == green && (b > 150) == blue;
    }
}

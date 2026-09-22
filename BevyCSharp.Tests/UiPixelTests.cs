using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the layout by looking at what it laid out.
/// </summary>
/// <remarks>
/// <para>
/// Every other interface test asks whether a node was accepted, because whether a screen looks
/// right needs an eye. A run that draws into an image is the eye: the screen is drawn, the pixels
/// come back, and a field that decides a size can be checked against the size it decided.
/// </para>
/// <para>
/// It needs the camera to be named, because Bevy picks a default interface camera only from the
/// cameras drawing to a window and there is none here.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class UiPixelTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const ulong Settled = 120;

    [Fact]
    public void AnAspectRatioDecidesTheSideTheLayoutWasNotTold()
    {
        if (!App.HasRenderer) return;

        var picture = Draw(ratio: 1f);

        Assert.NotNull(picture);

        var (width, height) = Filled(picture);

        // A quarter of a 128-pixel screen across, and square by its ratio, so its height follows
        // its width rather than its parent's.
        Assert.InRange(width, 26, 38);
        Assert.InRange(height, width - 4, width + 4);
    }

    [Fact]
    public void ANodeWithNoRatioTakesTheHeightItWasGiven()
    {
        if (!App.HasRenderer) return;

        var picture = Draw(ratio: 0f);

        Assert.NotNull(picture);

        var (width, height) = Filled(picture);

        // Told to be sixteen pixels tall, and not told anything about a ratio, so it stays that
        // whatever its width came out as.
        Assert.InRange(height, 13, 19);
        Assert.True(width > height + 6, $"the node came out {width} by {height}");
    }

    /// <summary>Text set closer together takes fewer pixels from top to bottom.</summary>
    /// <remarks>
    /// Two lines of the same words in the same font, so the only thing that can move the second
    /// line is the spacing asked for.
    /// </remarks>
    [Fact]
    public void LineHeightMovesTheLinesApart()
    {
        if (!App.HasRenderer) return;

        var packed = DrawText(lineHeight: 1f);
        var spread = DrawText(lineHeight: 2.5f);

        Assert.NotNull(packed);
        Assert.NotNull(spread);

        var (_, packedHeight) = Filled(packed);
        var (_, spreadHeight) = Filled(spread);

        Assert.True(
            spreadHeight > packedHeight + 4,
            $"the lines took {packedHeight} pixels packed and {spreadHeight} spread");
    }

    /// <summary>A shadow puts something behind the glyphs that was not there before.</summary>
    [Fact]
    public void AShadowIsDrawnBehindTheText()
    {
        if (!App.HasRenderer) return;

        var plain = DrawText(lineHeight: 1f);
        var shadowed = DrawText(lineHeight: 1f, shadow: true);

        Assert.NotNull(plain);
        Assert.NotNull(shadowed);

        Assert.True(
            Green(shadowed) > Green(plain),
            "the shadow covered no more of the screen than the text alone");
    }

    /// <summary>How many pixels the text and anything behind it reached.</summary>
    private static int Green(CapturedImage picture)
    {
        var lit = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (picture.Pixels[i + 1] > 40) lit++;
        }

        return lit;
    }

    /// <summary>Draws two lines of text and hands back the picture.</summary>
    private static CapturedImage? DrawText(float lineHeight, bool shadow = false) => Capture(
        (camera, _) => Ui.SpawnText(
            "one\ntwo",
            new UiSettings
            {
                Absolute = true,
                Left = Length.Px(4f),
                Top = Length.Px(4f),
                Color = (0f, 1f, 0f, 1f),
                Camera = camera,
            },
            new UiTextSettings
            {
                FontSize = 16f,
                LineHeight = lineHeight,
                ShadowOffset = (2f, 2f),
                ShadowColor = shadow ? (0f, 0.6f, 0f, 1f) : (0f, 0f, 0f, 0f),
            }));

    /// <summary>How wide and how tall the drawn node is, in pixels.</summary>
    private static (int Width, int Height) Filled(CapturedImage picture)
    {
        int left = int.MaxValue, right = -1, top = int.MaxValue, bottom = -1;

        for (var y = 0u; y < picture.Height; y++)
        {
            for (var x = 0u; x < picture.Width; x++)
            {
                // The node is the only green thing in the picture.
                var pixel = picture.At(x, y);
                if (pixel.G < 120 || pixel.R > 90) continue;

                left = Math.Min(left, (int)x);
                right = Math.Max(right, (int)x);
                top = Math.Min(top, (int)y);
                bottom = Math.Max(bottom, (int)y);
            }
        }

        Assert.True(right >= 0, "the screen drew nothing");
        return (right - left + 1, bottom - top + 1);
    }

    /// <summary>Draws one node in the corner of an otherwise empty screen.</summary>
    private static CapturedImage? Draw(float ratio) => Capture((camera, _) => Ui.SpawnNode(
        new UiSettings
        {
            Absolute = true,
            Left = Length.Px(8f),
            Top = Length.Px(8f),
            Width = Length.Percent(25f),
            Height = ratio > 0f ? Length.Auto : Length.Px(16f),
            AspectRatio = ratio,
            Color = (0f, 1f, 0f, 1f),
            Camera = camera,
        }));

    /// <summary>Runs an offscreen app, lets <paramref name="screen"/> build one, and captures it.</summary>
    private static CapturedImage? Capture(Action<Entity, World> screen)
    {
        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(128, 128, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                world.Resource<EcsWorld>().Add(
                    camera, Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));

                screen(camera, world);
            },
            "Test.Screen"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == Settled)
                {
                    world.InsertResource(new Ticket(Render.BeginCapture()));
                    return;
                }

                if (picture is not null) return;
                if (!world.TryGetResource<Ticket>(out var ticket)) return;

                if (Render.TryReadCapture(ticket.Capture, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());
        return picture;
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}

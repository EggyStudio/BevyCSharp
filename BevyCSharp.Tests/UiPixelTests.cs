using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the layout by looking at what it laid out.
/// </summary>
/// <remarks>
/// <para>
/// Every other interface test asks whether a node was accepted, because whether a screen looks
/// right needs an eye. A run that draws into an image is the eye. The screen is drawn, the pixels
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

    /// <summary>Text tracked out takes more room across than the same words set normally.</summary>
    /// <remarks>
    /// The same words in the same font at the same size, so the only thing that can widen them is
    /// the room asked for between the letters.
    /// </remarks>
    [Fact]
    public void LetterSpacingMovesTheLettersApart()
    {
        if (!App.HasRenderer) return;

        var normal = DrawText(lineHeight: 1f);
        var tracked = DrawText(lineHeight: 1f, letterSpacing: 0.5f);

        Assert.NotNull(normal);
        Assert.NotNull(tracked);

        var (normalWidth, _) = Filled(normal);
        var (trackedWidth, _) = Filled(tracked);

        Assert.True(
            trackedWidth > normalWidth + 4,
            $"the words took {normalWidth} pixels normally and {trackedWidth} tracked out");
    }

    /// <summary>A sliced edge that tiles repeats its pattern where a stretched one smears it.</summary>
    /// <remarks>
    /// The same picture drawn at the same size both ways. A stripe per source column becomes four
    /// wide bands when the slice is stretched and a row of thin ones when it is tiled, so counting
    /// the changes across the top edge tells the two apart without knowing where any of them fell.
    /// </remarks>
    [Fact]
    public void ASlicedEdgeCanTileRatherThanStretch()
    {
        if (!App.HasRenderer) return;

        var stretched = DrawStripes(SliceTiling.None);
        var tiled = DrawStripes(SliceTiling.Sides);

        Assert.NotNull(stretched);
        Assert.NotNull(tiled);

        // Two rows down, which is inside the top edge rather than in the middle below it. The
        // middle is a slice of its own and stretches either way here.
        var smeared = Changes(stretched, y: 2);
        var repeated = Changes(tiled, y: 2);

        Assert.True(smeared > 0, "the stretched edge drew no stripes at all");
        Assert.True(
            repeated > smeared * 2,
            $"the edge changed {smeared} times stretched and {repeated} times tiled");
    }

    /// <summary>How many times the row changes between lit and unlit across the picture.</summary>
    private static int Changes(CapturedImage picture, uint y)
    {
        var changes = 0;
        var lit = false;

        for (var x = 0u; x < picture.Width; x++)
        {
            var now = picture.At(x, y).R > 120;
            if (now != lit) changes++;
            lit = now;
        }

        return changes;
    }

    /// <summary>Draws a striped nine-slice, sliced the way <paramref name="tiling"/> asks.</summary>
    private static CapturedImage? DrawStripes(SliceTiling tiling) => Capture((camera, _) =>
    {
        // Twelve pixels square with a four-pixel border, so the slice between the corners is the
        // four middle columns, and those carry a stripe each.
        const int side = 12;
        var pixels = new byte[side * side * 4];

        for (var y = 0; y < side; y++)
        {
            for (var x = 0; x < side; x++)
            {
                var at = ((y * side) + x) * 4;

                // Red on the odd columns of the middle band, opaque black everywhere else, so the
                // pattern is only in the slice the test reads.
                if (x is >= 4 and < 8 && x % 2 == 1) pixels[at] = 255;

                pixels[at + 3] = 255;
            }
        }

        var made = Render.CreateImage(pixels, side, side);

        var node = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Zero,
            Top = Length.Zero,
            Width = Length.Px(120f),
            Height = Length.Px(40f),
            Camera = camera,
        });

        Ui.SetImage(node, new UiImageSettings
        {
            Image = made,
            Mode = UiImageMode.Sliced,
            SliceBorder = (4f, 4f, 4f, 4f),
            SliceTiling = tiling,
        });
    });

    /// <summary>An image built from bytes here is drawn like any other.</summary>
    /// <remarks>
    /// The whole round trip. The pixels never touch a file, so red arriving on screen says the
    /// bytes crossed the bridge, became an asset, took a key the table knows, and reached the
    /// renderer through the same path a loaded picture does.
    /// </remarks>
    [Fact]
    public void AnImageMadeFromBytesIsDrawn()
    {
        if (!App.HasRenderer) return;

        var picture = Capture((camera, _) =>
        {
            // Four red pixels, which is the smallest picture that still has a shape.
            var pixels = new byte[2 * 2 * 4];

            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
                pixels[i + 3] = 255;
            }

            var made = Render.CreateImage(pixels, 2, 2);

            var node = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Left = Length.Px(8f),
                Top = Length.Px(8f),
                Width = Length.Px(32f),
                Height = Length.Px(32f),
                Camera = camera,
            });

            Ui.SetImage(node, new UiImageSettings { Image = made });
        });

        Assert.NotNull(picture);
        Assert.True(Red(picture) > 400, $"the picture reached {Red(picture)} pixels");
    }

    /// <summary>A grid puts its children in the cells the tracks describe.</summary>
    /// <remarks>
    /// Two columns of a quarter of the screen each and one row, so the second child starts where
    /// the first column ends. Laid out by the flexbox the node would otherwise use, the two would
    /// be packed against each other at their own widths instead.
    /// </remarks>
    [Fact]
    public void AGridPutsItsChildrenInItsCells()
    {
        if (!App.HasRenderer) return;

        var picture = Capture((camera, world) =>
        {
            var panel = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Left = Length.Zero,
                Top = Length.Zero,
                Width = Length.Px(128f),
                Height = Length.Px(32f),
                Camera = camera,
            });

            // Two columns of a fixed width, so where the second child lands is arithmetic rather
            // than whatever it measured itself to be.
            UiGrid.Set(panel, new GridSettings
            {
                Columns = [Track.Px(64f).Repeated(2)],
                Rows = [Track.Px(32f)],
            });

            // Small, so each fills a corner of its own cell rather than the whole of it, and the
            // gap between the two says the cells are where the tracks put them.
            var first = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Px(8f),
                Height = Length.Px(8f),
                Color = (0f, 1f, 0f, 1f),
                Camera = camera,
            });

            var second = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Px(8f),
                Height = Length.Px(8f),
                Color = (0f, 1f, 0f, 1f),
                Camera = camera,
            });

            var ecs = world.Resource<EcsWorld>();
            ecs.SetParent(first, panel);
            ecs.SetParent(second, panel);
        });

        Assert.NotNull(picture);

        var (left, right) = Edges(picture, red: false);

        // The first child at the left edge and the second at the start of the second column, so
        // what was drawn reaches from zero to a little past sixty-four.
        Assert.InRange(left, 0, 2);
        Assert.InRange(right, 66, 76);
    }

    /// <summary>A rounded corner takes the corner pixel off a node that still fills its middle.</summary>
    /// <remarks>
    /// The same node twice, so the corner going dark while the middle stays lit is the radius and
    /// nothing else. A radius that never reached the layout would leave the two pictures equal.
    /// </remarks>
    [Fact]
    public void RoundedCornersCutTheCornerOffANode()
    {
        if (!App.HasRenderer) return;

        var square = DrawBox(radius: 0f);
        var rounded = DrawBox(radius: 16f);

        Assert.NotNull(square);
        Assert.NotNull(rounded);

        // Two pixels in from the node's own top left, which is inside a square corner and outside
        // a sixteen-pixel curve.
        Assert.True(square.At(10, 10).G > 120, "the square node did not reach its own corner");
        Assert.True(rounded.At(10, 10).G < 90, "the corner was not rounded off");

        // And the middle of the node is filled either way, so nothing simply failed to draw.
        Assert.True(rounded.At(28, 28).G > 120, "the rounded node drew nothing at all");
    }

    /// <summary>Draws one square node, rounded by <paramref name="radius"/> pixels.</summary>
    private static CapturedImage? DrawBox(float radius) => Capture((camera, _) => Ui.SpawnNode(
        new UiSettings
        {
            Absolute = true,
            Left = Length.Px(8f),
            Top = Length.Px(8f),
            Width = Length.Px(40f),
            Height = Length.Px(40f),
            Corners = Length.Px(radius),
            Color = (0f, 1f, 0f, 1f),
            Camera = camera,
        }));

    /// <summary>A span adds its own words to the line, in its own color.</summary>
    /// <remarks>
    /// Two colors in one line of text is the thing a single run cannot do, so red appearing at all
    /// is the whole assertion, and the line growing wider says the words were laid out rather than
    /// drawn on top of the ones already there.
    /// </remarks>
    [Fact]
    public void ASpanSetsPartOfALineInItsOwnColor()
    {
        if (!App.HasRenderer) return;

        var plain = DrawSpan(span: false);
        var mixed = DrawSpan(span: true);

        Assert.NotNull(plain);
        Assert.NotNull(mixed);

        Assert.Equal(0, Red(plain));
        Assert.True(Red(mixed) > 0, "the span drew nothing of its own");

        // The span follows the parent's own words, so every red pixel is to the right of the last
        // green one. Drawn over the top instead, or laid out as a line of its own, and they would
        // overlap.
        var (_, green) = Edges(mixed, red: false);
        var (spanStart, _) = Edges(mixed, red: true);

        Assert.True(
            spanStart > green,
            $"the line ended at {green} and the span began at {spanStart}");
    }

    /// <summary>The leftmost and rightmost pixel of one color in the picture.</summary>
    private static (int Left, int Right) Edges(CapturedImage picture, bool red)
    {
        int left = int.MaxValue, right = -1;

        for (var y = 0u; y < picture.Height; y++)
        {
            for (var x = 0u; x < picture.Width; x++)
            {
                var pixel = picture.At(x, y);
                var lit = red ? pixel.R > 120 && pixel.G < 90 : pixel.G > 120 && pixel.R < 90;
                if (!lit) continue;

                left = Math.Min(left, (int)x);
                right = Math.Max(right, (int)x);
            }
        }

        Assert.True(right >= 0, red ? "the span drew nothing" : "the line drew nothing");
        return (left, right);
    }

    /// <summary>How many pixels the span reached, which is the only red in the picture.</summary>
    private static int Red(CapturedImage picture)
    {
        var lit = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (picture.Pixels[i] > 120 && picture.Pixels[i + 1] < 90) lit++;
        }

        return lit;
    }

    /// <summary>Draws one line of text, with or without a second run after it.</summary>
    private static CapturedImage? DrawSpan(bool span) => Capture(
        (camera, _) =>
        {
            var style = new UiTextSettings { FontSize = 16f };

            var line = Ui.SpawnText(
                "one",
                new UiSettings
                {
                    Absolute = true,
                    Left = Length.Px(4f),
                    Top = Length.Px(4f),
                    Color = (0f, 1f, 0f, 1f),
                    Camera = camera,
                },
                style);

            if (span) Ui.SpawnTextSpan(line, " two", style, (1f, 0f, 0f, 1f));
        });

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
    private static CapturedImage? DrawText(
        float lineHeight,
        bool shadow = false,
        float letterSpacing = 0f) => Capture(
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
                LetterSpacing = letterSpacing,
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

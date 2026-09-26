using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the shapes gizmos can draw, by drawing them and looking.
/// </summary>
/// <remarks>
/// A gizmo call answers nothing, which makes it pleasant to write and impossible to check any other
/// way. The queue can be asserted on, and <see cref="GizmoTests"/> does, but a shape that reaches
/// the queue and is then drawn as a different shape, or as nothing, looks the same from there. An
/// offscreen run draws them against a known background and the pixels say which happened.
/// </remarks>
[Collection("engine")]
public sealed class GizmoShapeTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const ulong Settled = 120;

    /// <summary>Each shape, drawn alone, covers some of the picture and leaves the rest alone.</summary>
    [Theory]
    [InlineData("rect")]
    [InlineData("circle")]
    [InlineData("arc")]
    [InlineData("arrow")]
    [InlineData("grid")]
    [InlineData("box")]
    [InlineData("capsule")]
    [InlineData("cone")]
    [InlineData("cylinder")]
    [InlineData("torus")]
    [InlineData("frustum")]
    public void AShapeIsDrawn(string shape)
    {
        if (!App.HasRenderer) return;

        var picture = Draw(shape);

        Assert.NotNull(picture);

        var drawn = 0;
        var background = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            // Green, and nothing else in the picture is, so a green pixel is a pixel the shape
            // reached. The camera clears to black.
            if (picture.Pixels[i + 1] > 100) drawn++;
            else background++;
        }

        Assert.True(drawn > 0, $"{shape} drew nothing");
        Assert.True(background > 0, $"{shape} covered the whole picture, so it is not a shape");
    }

    /// <summary>Turning gizmos off stops the drawing without the caller stopping asking.</summary>
    [Fact]
    public void ConfiguringThemOffDrawsNothing()
    {
        if (!App.HasRenderer) return;

        var picture = Draw("circle", enabled: false);

        Assert.NotNull(picture);
        Assert.All(
            Enumerable.Range(0, (int)(picture.Width * picture.Height)),
            i => Assert.True(picture.Pixels[(i * 4) + 1] < 100, "something was drawn"));
    }

    /// <summary>Each flat shape, drawn alone for a 2D camera, covers some of the picture.</summary>
    /// <remarks>
    /// The same question as <see cref="AShapeIsDrawn"/> and a different camera, because a flat
    /// shape reaches Bevy through its own call and a kind that fell through to the default arm
    /// would draw a line instead of the shape asked for.
    /// </remarks>
    [Theory]
    [InlineData("rect")]
    [InlineData("circle")]
    [InlineData("line")]
    [InlineData("arrow")]
    [InlineData("arc")]
    [InlineData("grid")]
    public void AFlatShapeIsDrawn(string shape)
    {
        if (!App.HasRenderer) return;

        var picture = DrawFlat(shape);

        Assert.NotNull(picture);

        var drawn = Lit(picture);

        Assert.True(drawn > 0, $"the flat {shape} drew nothing");
        Assert.True(
            drawn < picture.Width * picture.Height,
            $"the flat {shape} covered the whole picture, so it is not a shape");
    }

    /// <summary>Turning one group off leaves the other drawing.</summary>
    /// <remarks>
    /// The grid is drawn behind the scene and the circle in front of it, so switching the group the
    /// scene can hide takes one away and leaves the other. Configuring both at once, as the call
    /// did before there was a group to name, would have taken both.
    /// </remarks>
    [Fact]
    public void OneGroupCanBeTurnedOffWithoutTheOther()
    {
        if (!App.HasRenderer) return;

        var both = Draw("pair");
        var front = Draw("pair", behind: false);

        Assert.NotNull(both);
        Assert.NotNull(front);

        var all = Lit(both);
        var some = Lit(front);

        Assert.True(some > 0, "turning the far group off took the near one with it");
        Assert.True(
            some < all,
            $"both groups covered {all} pixels and one covered {some}");
    }

    /// <summary>A run of lines drawn in one call reaches the screen like any other shape.</summary>
    /// <remarks>
    /// Three segments making a triangle, so what is checked is that every one of them arrived
    /// rather than only the first, which is the way a batched call goes wrong.
    /// </remarks>
    [Fact]
    public void ARunOfLinesIsDrawnInOneCall()
    {
        if (!App.HasRenderer) return;

        var one = Draw("line");
        var run = Draw("run");

        Assert.NotNull(one);
        Assert.NotNull(run);

        var single = Lit(one);
        var many = Lit(run);

        Assert.True(single > 0, "the single line drew nothing");
        Assert.True(
            many > single * 2,
            $"one line covered {single} pixels and a run of three covered {many}");
    }

    /// <summary>A dashed line covers less of the picture than the same shape drawn solid.</summary>
    /// <remarks>
    /// The same shape at the same width, so the gaps are the only thing that can take pixels away,
    /// and a style that never reached the renderer would leave the two counts equal.
    /// </remarks>
    [Fact]
    public void ADashedLineLeavesGapsInItself()
    {
        if (!App.HasRenderer) return;

        var solid = Draw("rect");
        var dashed = Draw("rect", style: GizmoLine.Dashed);

        Assert.NotNull(solid);
        Assert.NotNull(dashed);

        var whole = Lit(solid);
        var broken = Lit(dashed);

        Assert.True(broken > 0, "the dashed line drew nothing at all");
        Assert.True(
            broken < whole,
            $"the line covered {whole} pixels solid and {broken} dashed");
    }

    /// <summary>How many pixels the shape reached.</summary>
    private static int Lit(CapturedImage picture)
    {
        var drawn = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (picture.Pixels[i + 1] > 100) drawn++;
        }

        return drawn;
    }

    /// <summary>Runs one shape in an offscreen app and hands back the picture.</summary>
    private static CapturedImage? Draw(
        string shape,
        bool enabled = true,
        GizmoLine style = GizmoLine.Solid,
        bool behind = true)
    {
        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(96, 96, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 50f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                world.Resource<EcsWorld>().Add(
                    camera, Transform.LookingAt(new Vec3(0f, 0f, 8f), Vec3.Zero, Vec3.UnitY));

                Gizmos.Configure(width: 4f, enabled: enabled);
                Gizmos.SetLineStyle(style);

                // Said second, so it narrows what the line above set for everything.
                if (!behind) Gizmos.Configure(width: 4f, enabled: false, which: GizmoGroup.Behind);
            },
            "Test.Camera"));

        // Every frame, because a gizmo lasts one, and the capture is read frames after it is asked
        // for rather than on the frame that asked.
        app.AddSystem(Stage.Update, new SystemDescriptor(
            _ =>
            {
                var green = (0f, 1f, 0f, 1f);

                switch (shape)
                {
                    case "rect":
                        Gizmos.Rect(Vec3.Zero, Quat.Identity, 3f, 2f, green);
                        break;

                    case "line":
                        Gizmos.Line(new Vec3(-2f, -2f, 0f), new Vec3(2f, -2f, 0f), green);
                        break;

                    case "pair":
                        // One in each group, so which of them survives says which was configured.
                        Gizmos.Grid(Vec3.Zero, Quat.Identity, 4, 4, 0.8f, green, inFront: false);
                        Gizmos.Circle(Vec3.Zero, Quat.Identity, 1.5f, green, inFront: true);
                        break;

                    case "run":
                        Gizmos.Lines(
                        [
                            new GizmoSegment(new Vec3(-2f, -2f, 0f), new Vec3(2f, -2f, 0f), green),
                            new GizmoSegment(new Vec3(2f, -2f, 0f), new Vec3(0f, 2f, 0f), green),
                            new GizmoSegment(new Vec3(0f, 2f, 0f), new Vec3(-2f, -2f, 0f), green),
                        ]);
                        break;

                    case "circle":
                        Gizmos.Circle(Vec3.Zero, Quat.Identity, 1.5f, green);
                        break;

                    case "arc":
                        Gizmos.Arc(Vec3.Zero, Quat.Identity, 2f, 2f, green);
                        break;

                    case "arrow":
                        Gizmos.Arrow(new Vec3(-2f, -1f, 0f), new Vec3(2f, 1f, 0f), green);
                        break;

                    case "grid":
                        Gizmos.Grid(Vec3.Zero, Quat.Identity, 4, 4, 0.8f, green, inFront: true);
                        break;

                    case "capsule":
                        Gizmos.Capsule(Vec3.Zero, Quat.Identity, 1f, 2f, green);
                        break;

                    case "cone":
                        Gizmos.Cone(Vec3.Zero, Quat.Identity, 1.5f, 2f, green);
                        break;

                    case "cylinder":
                        Gizmos.Cylinder(Vec3.Zero, Quat.Identity, 1.2f, 1.2f, green);
                        break;

                    case "torus":
                        Gizmos.Torus(Vec3.Zero, Quat.Identity, 2f, 0.5f, green);
                        break;

                    case "frustum":
                        Gizmos.Frustum(Vec3.Zero, Quat.Identity, 1.6f, 0.6f, 2.4f, green);
                        break;

                    default:
                        Gizmos.Box(Vec3.Zero, Quat.Identity, new Vec3(2f, 2f, 2f), green);
                        break;
                }
            },
            "Test.Draw"));

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

    /// <summary>Runs one flat shape against a 2D camera and hands back the picture.</summary>
    private static CapturedImage? DrawFlat(string shape)
    {
        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(96, 96, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            _ =>
            {
                // Bevy's own clear color, which is a dark gray well under the green the shapes
                // are drawn in, so nothing has to be said about it here.
                Render2d.SpawnCamera2d();
                Gizmos.Configure(width: 4f);
            },
            "Test.Camera"));

        // A 2D camera counts in pixels from the middle of the screen, so a shape tens of units
        // across fills a picture this size the way a shape a few units across does in the scene.
        app.AddSystem(Stage.Update, new SystemDescriptor(
            _ =>
            {
                var green = (0f, 1f, 0f, 1f);

                switch (shape)
                {
                    case "rect":
                        Gizmos.Rect2d((0f, 0f), 40f, 30f, green);
                        break;

                    case "circle":
                        Gizmos.Circle2d((0f, 0f), 24f, green);
                        break;

                    case "line":
                        Gizmos.Line2d((-30f, -20f), (30f, 20f), green);
                        break;

                    case "arrow":
                        Gizmos.Arrow2d((-30f, -10f), (30f, 10f), green);
                        break;

                    case "arc":
                        Gizmos.Arc2d((0f, 0f), 30f, 2f, green);
                        break;

                    default:
                        Gizmos.Grid2d((0f, 0f), 4, 4, 16f, green, inFront: true);
                        break;
                }
            },
            "Test.Draw"));

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

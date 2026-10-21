using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the modeling window's use of the pointer in the 3D view, the face a click picks by a ray
/// from the camera tested against the edit mesh's faces wherever the mesh is drawn, how far a drag
/// along a face's normal moves it, in whole grid steps, and the click and the drags made through
/// the input in a running app.
/// </summary>
[Collection("engine")]
public sealed class ModelingWindowTests
{
    private static int FaceFacing(EditMesh mesh, Vec3 way) => Enumerable.Range(0, mesh.FaceCount).Single(face => (mesh.Normal(face) - way).Length < 1e-4f);

    [Fact]
    public void ARayFromAboveTheMovedBoxPicksItsTopAndOneBesideItPicksNothing()
    {
        var mesh = new EditMesh().Box(new Vec3(2f, 3f, 4f));
        var placed = Transform.At(10f, 0f, 0f);

        Assert.Equal(FaceFacing(mesh, Vec3.UnitY), Modeling.Pick(mesh, placed, new Vec3(11f, 10f, 2f), -Vec3.UnitY));
        Assert.Equal(FaceFacing(mesh, -Vec3.UnitX), Modeling.Pick(mesh, placed, new Vec3(0f, 1f, 2f), Vec3.UnitX));
        Assert.Null(Modeling.Pick(mesh, placed, new Vec3(1f, 10f, 2f), -Vec3.UnitY));
    }

    [Fact]
    public void AMeshTurnedAndScaledIsPickedWhereItIsDrawn()
    {
        var mesh = new EditMesh().Box(new Vec3(1f));

        // Turned a quarter about Y, its +X face faces -Z in the world, and twice as large, its
        // far corner two meters out.
        var placed = new Transform(Vec3.Zero, Quat.FromAxisAngle(Vec3.UnitY, MathF.PI / 2f), new Vec3(2f));
        Assert.Equal(FaceFacing(mesh, Vec3.UnitX), Modeling.Pick(mesh, placed, new Vec3(1f, 1f, -10f), Vec3.UnitZ));
        Assert.Null(Modeling.Pick(mesh, placed, new Vec3(2.5f, 1f, -10f), Vec3.UnitZ));
    }

    [Fact]
    public void ADragAlongAFacesNormalGoesInWholeGridSteps()
    {
        var middle = new Vec3(1f, 3f, 2f);
        var pointer = new Vec3(5f, 4.3f, 2f);

        Assert.Equal(1f, Modeling.DragDistance(middle, Vec3.UnitY, pointer, -Vec3.UnitX, 1f), 1e-5f);
        Assert.Equal(1.25f, Modeling.DragDistance(middle, Vec3.UnitY, pointer, -Vec3.UnitX, 0.25f), 1e-5f);
        Assert.Equal(-2f, Modeling.DragDistance(middle, Vec3.UnitY, new Vec3(5f, 1.1f, 2f), -Vec3.UnitX, 1f), 1e-5f);

        // A pointer's ray along the normal itself says no distance, rather than any.
        Assert.Equal(0f, Modeling.DragDistance(middle, Vec3.UnitY, new Vec3(1f, 10f, 2f), -Vec3.UnitY, 1f));
    }

    [SkippableFact]
    public void AClickPicksTheTopAndADragUpExtrudesItAMeterAndWithAltMovesIt()
    {
        Needs.Renderer();

        // The top's middle at the world's middle, which the camera looks at from above and in
        // front, so the middle of the picture is on the top. A drag from there to 20 pixels from
        // the top comes nearest the top's normal 1.28 meters up, and the second, from the top
        // extruded to 5 pixels from the edge, 0.81 meters above it, each a meter on the grid.
        var mesh = new EditMesh().Box(new Vec3(2f, 1f, 2f), new Vec3(-1f, 0f, -1f));
        var placed = Transform.At(0f, -1f, 0f);
        var top = FaceFacing(mesh, Vec3.UnitY);
        var camera = Entity.None;
        var (hovering, away) = (false, true);

        var run = new PictureRun
        {
            Scene = ecs => camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 4f)),
            EachFrame = world => Modeling.DrawPanel(new BehaviorContext(world), mesh, camera, placed),
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("over the top", _ => SyntheticInput.MoveTo(48f, 48f))
            .Wait(2)
            .Do("click", _ =>
            {
                hovering = Modeling.WantsPointer;
                SyntheticInput.Press(48f, 48f);
            })
            .Wait(2)
            .Do("let go", _ => SyntheticInput.Release(48f, 48f))
            .Wait(2)
            .Do("press to drag", _ => SyntheticInput.Press(48f, 48f))
            .Wait(2)
            .Do("drag", _ => SyntheticInput.MoveTo(48f, 34f))
            .Wait(1)
            .Do("drag on", _ => SyntheticInput.MoveTo(48f, 20f))
            .Wait(2)
            .Do("let go of the drag", _ => SyntheticInput.Release(48f, 20f))
            .Wait(2)
            .Do("press with alt", _ =>
            {
                SyntheticInput.Press(Key.AltLeft);
                SyntheticInput.Press(48f, 30f);
            })
            .Wait(2)
            .Do("drag with alt", _ => SyntheticInput.MoveTo(48f, 18f))
            .Wait(1)
            .Do("drag on with alt", _ => SyntheticInput.MoveTo(48f, 5f))
            .Wait(2)
            .Do("let go of alt", _ =>
            {
                SyntheticInput.Release(48f, 5f);
                SyntheticInput.Lift(Key.AltLeft);
            })
            .Wait(2)
            .Do("away", _ => SyntheticInput.MoveTo(2f, 2f))
            .Wait(3)
            .Do("read", _ => away = Modeling.WantsPointer)
            .Go();

        Assert.True(hovering, "the window did not hold the pointer over the top");
        Assert.False(away, "the window held the pointer away from the box");
        Assert.Equal(new[] { $"select face {top}", "extrude 1", "move 0 1 0" }, mesh.Recipe.Skip(1));
        Assert.Equal(3f, mesh.Bounds.High.Y, 1e-5f);

        // A headless app after it outlines nothing of the window, a run without a window having no
        // lines to draw with, which the logged-error check of every test would report.
        using var headless = new EngineHarness(frames: 3);
        headless.Run();
    }
}

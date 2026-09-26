using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the step every other render test stops short of, that what the bridge builds is
/// extracted into the render world and drawn with.
/// </summary>
/// <remarks>
/// <para>
/// The rest of the suite asserts that a setting was accepted, which can go wrong quietly. A mesh
/// handle that names nothing, a material the render world never hears about, a camera pointed at a
/// target it cannot draw into. None of those show up as a failed call, and all of them show up as a
/// picture with nothing in it. Until a run could draw without a display and hand its pixels back,
/// that was not something a test could look at.
/// </para>
/// <para>
/// An unlit material, because a lit one is a question about the lighting rather than about whether
/// the mesh arrived. Two pixels are enough, one where the shape is and one where the camera's own
/// clear color should still be showing.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class DrawnTests
{
    /// <summary>
    /// How long to run before looking.
    /// </summary>
    /// <remarks>
    /// A material's render pipeline is compiled the first time something asks to be drawn with it,
    /// and until it is ready the renderer skips the mesh and clears the frame anyway. A capture
    /// taken in the first frames of a run is therefore a picture of the clear color with nothing in
    /// it, which reads exactly like a mesh that never arrived. This is far past that.
    /// </remarks>
    private const ulong Settled = 120;

    [Fact]
    public void AMeshAndItsMaterialReachTheRenderWorld()
    {
        if (!App.HasRenderer) return;

        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(64, 64, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 50f,
                    Clear = ClearMode.Custom,

                    // Blue, and nothing in the scene is blue, so a blue pixel is one the shape
                    // did not cover.
                    ClearColor = (0f, 0f, 1f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));

                var cube = ecs.Spawn();

                // Small enough at this distance to leave the corners of the picture alone, which
                // makes the clear color worth asserting on.
                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));
                Render.SetMaterial(ecs, cube, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (1f, 0f, 0f, 1f),
                    Unlit = true,
                }));

                ecs.Add(cube, Transform.Identity);
            },
            "Test.Scene"));

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
        Assert.NotNull(picture);

        var middle = picture.At(32, 32);
        var corner = picture.At(1, 1);

        Assert.True(
            middle.R > 180 && middle.G < 90 && middle.B < 90,
            $"the cube should be red in the middle of the picture, and it is {middle}");

        Assert.True(
            corner.B > 180 && corner.R < 90,
            $"the camera's clear color should show in the corner, and it is {corner}");
    }

    /// <summary>Where the test keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}

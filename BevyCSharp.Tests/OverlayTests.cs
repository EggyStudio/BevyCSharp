using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a 2D camera drawing over a 3D one, by drawing both and looking.
/// </summary>
/// <remarks>
/// An overlay is not simply a second camera with a higher order, and the two ways of getting it
/// wrong both look like something else. A camera that writes its own view over the target replaces
/// the scene, which reads as the scene having failed to draw; a camera told not to clear
/// accumulates its own output instead, which reads as smearing. Only a picture of both together
/// tells any of that apart, so this is a pixel test or it is nothing.
/// </remarks>
[Collection("engine")]
public sealed class OverlayTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const ulong Settled = 120;

    /// <summary>A sprite drawn over a scene leaves the scene visible around it.</summary>
    [Fact]
    public void AnOverlaidSpriteKeepsTheSceneUnderIt()
    {
        if (!App.HasRenderer) return;

        var picture = Draw();

        Assert.NotNull(picture);

        // The middle of the picture is the sprite, which is green.
        var middle = picture.At(48, 48);
        Assert.True(
            middle.G > 120 && middle.R < 90,
            $"the sprite was not drawn; the middle is {middle}");

        // And a corner is the scene the camera under it cleared to, which an overlay that
        // overwrote the target rather than blending into it would have replaced.
        var corner = picture.At(4, 4);
        Assert.True(
            corner.R > 120 && corner.G < 90,
            $"the scene under the overlay was lost; the corner is {corner}");
    }

    /// <summary>Runs an offscreen app with both cameras and hands back the picture.</summary>
    private static CapturedImage? Draw()
    {
        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(96, 96, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                // Under everything, clearing to a color nothing else in the picture is, so what
                // survives says whether the scene was kept.
                var scene = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (1f, 0f, 0f, 1f),
                });

                world.Resource<EcsWorld>().Add(
                    scene, Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));

                // Over it, which is the thing being checked.
                Render2d.SpawnCamera2d(order: 1);

                var green = new byte[2 * 2 * 4];

                for (var i = 0; i < green.Length; i += 4)
                {
                    green[i + 1] = 255;
                    green[i + 3] = 255;
                }

                var badge = world.Resource<EcsWorld>().Spawn();
                world.Resource<EcsWorld>().Add(badge, Transform.Identity);

                Render2d.SetSprite(
                    world.Resource<EcsWorld>(),
                    badge,
                    Render.CreateImage(green, 2, 2),
                    new SpriteSettings { Size = (32f, 32f) });
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
        return picture;
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}

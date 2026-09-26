using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the lens effects, by drawing the same scene with and without each of them.
/// </summary>
/// <remarks>
/// <para>
/// An effect is a change to a picture, so the only honest check is two pictures. A setting that
/// reached nothing draws the same frame, and every one of these would look like that from the
/// managed side, where the call succeeded and the component was inserted.
/// </para>
/// <para>
/// What each test asserts is the shape of the change rather than its exact pixels, because the
/// numbers depend on the driver. A vignette darkens the corners and leaves the middle; a
/// distortion moves an edge; a fringe puts color where there was none.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class LensTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const ulong Settled = 120;

    /// <summary>A vignette darkens the corners and leaves the middle where it was.</summary>
    [Fact]
    public void AVignetteDarkensTheCornersOnly()
    {
        if (!App.HasRenderer) return;

        var plain = Draw(_ => { });
        var shaded = Draw(effects => effects.Vignette = 0.9f);

        Assert.NotNull(plain);
        Assert.NotNull(shaded);

        var cornerBefore = Brightness(plain.At(4, 4));
        var cornerAfter = Brightness(shaded.At(4, 4));

        var middleBefore = Brightness(plain.At(48, 48));
        var middleAfter = Brightness(shaded.At(48, 48));

        Assert.True(
            cornerAfter < cornerBefore - 10,
            $"the corner read {cornerBefore} plain and {cornerAfter} vignetted");

        Assert.True(
            Math.Abs(middleAfter - middleBefore) < 40,
            $"the middle read {middleBefore} plain and {middleAfter} vignetted");
    }

    /// <summary>A chromatic fringe puts color at an edge that had none.</summary>
    /// <remarks>
    /// The scene is gray on black, so every pixel of it is neutral until something splits the
    /// channels apart. This counts pixels whose channels disagree, which is a fringe and which
    /// nothing else in this picture could produce.
    /// </remarks>
    [Fact]
    public void AChromaticFringeSplitsAnEdgeIntoColors()
    {
        if (!App.HasRenderer) return;

        var plain = Draw(_ => { });
        var fringed = Draw(effects => effects.Aberration = 0.4f);

        Assert.NotNull(plain);
        Assert.NotNull(fringed);

        var before = Colored(plain);
        var after = Colored(fringed);

        Assert.True(
            after > before + 20,
            $"{before} pixels were colored plain and {after} with the fringe");
    }

    /// <summary>A lens distortion moves the edges of what is drawn.</summary>
    [Fact]
    public void ADistortionMovesTheEdgesOfTheShape()
    {
        if (!App.HasRenderer) return;

        var plain = Draw(_ => { });

        var warped = Draw(effects =>
        {
            effects.Distortion = -0.4f;
            effects.DistortionScale = 1f;
        });

        Assert.NotNull(plain);
        Assert.NotNull(warped);

        var before = Lit(plain);
        var after = Lit(warped);

        Assert.True(before > 0, "the shape was not drawn at all");
        Assert.True(
            Math.Abs(after - before) > before / 20,
            $"the shape covered {before} pixels plain and {after} warped");
    }

    /// <summary>How bright a pixel is, which is all these tests need of a color.</summary>
    private static int Brightness((byte R, byte G, byte B, byte A) pixel) =>
        pixel.R + pixel.G + pixel.B;

    /// <summary>How many pixels have something drawn in them.</summary>
    private static int Lit(CapturedImage picture)
    {
        var drawn = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (Brightness((picture.Pixels[i], picture.Pixels[i + 1], picture.Pixels[i + 2], 255))
                > 90)
            {
                drawn++;
            }
        }

        return drawn;
    }

    /// <summary>How many pixels have channels that disagree, which marks a fringe.</summary>
    private static int Colored(CapturedImage picture)
    {
        var tinted = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            var low = Math.Min(picture.Pixels[i], Math.Min(picture.Pixels[i + 1], picture.Pixels[i + 2]));
            var high = Math.Max(picture.Pixels[i], Math.Max(picture.Pixels[i + 1], picture.Pixels[i + 2]));

            if (high - low > 18) tinted++;
        }

        return tinted;
    }

    /// <summary>Draws a gray scene through whatever the effects were set to.</summary>
    /// <param name="lens">What to turn on, or nothing for the picture to compare against.</param>
    private static CapturedImage? Draw(Action<EffectSettings> lens)
    {
        CapturedImage? picture = null;

        using var app = new App(Config.OffscreenFor(96, 96, frames: (uint)Settled + 40));

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 50f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 4f), Vec3.Zero, Vec3.UnitY));

                // High dynamic range, because several of these run in the same pass bloom does
                // and Bevy asks for one either way.
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true });

                var effects = new EffectSettings();
                lens(effects);
                Render.SetEffects(camera, effects);

                var near = ecs.Spawn();

                Render.SetMesh(ecs, near, Render.CreateMesh(MeshShape.Cuboid, 2.4f, 2.4f, 2.4f));

                // Unlit and gray, so every pixel of the shape is neutral and a color in the
                // picture came from the lens rather than from the light.
                Render.SetMaterial(ecs, near, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.75f, 0.75f, 0.75f, 1f),
                    Unlit = true,
                }));

                ecs.Add(near, Transform.Identity);
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

using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the cubemap behind the scene.
/// </summary>
/// <remarks>
/// A skybox is the first thing the bridge does with a cubemap, and a cubemap is an ordinary image
/// told that it is six square faces stacked on top of each other. The telling can only happen once
/// the file has decoded, which is a frame or more after the handle is handed over, so what is worth
/// pinning down is that the waiting works, so the picture ends up with the sky in it rather than
/// with the camera's clear color.
/// </remarks>
[Collection("engine")]
public sealed class SkyboxTests
{
    /// <summary>Frames to let the image load and the pipelines compile.</summary>
    private const ulong Settled = 140;

    [SkippableFact]
    public void ASkyboxDrawsWhereTheSceneDoesNot()
    {
        Needs.Renderer();

        CapturedImage? picture = null;

        using var app = new App(new Config
        {
            Offscreen = true,
            Width = 64,
            Height = 64,
            HeadlessFps = 60,
            HeadlessFrames = (uint)Settled + 40,
            AssetRoot = EngineHarness.AssetDirectory,
        });

        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 60f,

                    // Black, and nothing about the sky is, so a black pixel is one the skybox did
                    // not reach.
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                world.Resource<EcsWorld>().Add(
                    camera, Transform.LookingAt(new Vec3(0f, 0f, 3f), Vec3.Zero, Vec3.UnitY));

                Render.SetSkybox(
                    camera,
                    AssetServer.Load(AssetKind.Image, "textures/cubemap.png"),
                    brightness: 1000f);
            },
            "Test.Sky"));

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

        var lit = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (picture.Pixels[i] + picture.Pixels[i + 1] + picture.Pixels[i + 2] > 60) lit++;
        }

        // Nothing is in the scene, so every pixel is either the sky or the clear color, and the
        // faces of this cubemap are all bright.
        Assert.True(
            lit > picture.Width * picture.Height / 2,
            $"only {lit} of {picture.Width * picture.Height} pixels have anything in them");
    }

    [SkippableFact]
    public void ASkyboxOnSomethingThatIsNotACameraIsRefused()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var entity = world.Resource<EcsWorld>().Spawn();

            var refused = Assert.Throws<BevyNativeException>(
                () => Render.SetSkybox(entity, AssetHandle.None));

            Assert.Equal(NativeStatus.NotPresent, refused.Status);
        });

        harness.Run();
    }

    /// <summary>
    /// The same six faces, laid out as a column, a horizontal cross, a vertical cross and a row, or
    /// given as six images, draw the same sky, so each puts every face where the column has it and
    /// turned the way the column has it.
    /// </summary>
    /// <remarks>
    /// Each face is its own blue with a gradient across it, red along and green down, so a face in
    /// the wrong place or turned the wrong way changes the picture. Three views, along -Z, along +Z
    /// and along +X, see three of the faces between them, the one a vertical cross turns among them.
    /// </remarks>
    [SkippableFact]
    public void ACrossARowAndSixImagesDrawTheSameSkyAsAColumn()
    {
        Needs.Renderer();

        const int Face = 8;

        // A face's pixel, at a place across and down it.
        static (byte R, byte G, byte B) Pixel(int face, int x, int y) => ((byte)(x * 32), (byte)(y * 32), (byte)(20 + (face * 40)));

        static byte[] Layout(int across, int down, (int X, int Y, bool Turned)[] places)
        {
            var width = across * Face;
            var pixels = new byte[width * down * Face * 4];

            for (var face = 0; face < 6; face++)
            {
                var (atX, atY, turned) = places[face];
                for (var y = 0; y < Face; y++)
                {
                    for (var x = 0; x < Face; x++)
                    {
                        // A face drawn half a turn round has its first pixel in its far corner.
                        var (drawnX, drawnY) = turned ? (Face - 1 - x, Face - 1 - y) : (x, y);
                        var at = (((atY * Face) + drawnY) * width + (atX * Face) + drawnX) * 4;
                        var (r, g, b) = Pixel(face, x, y);
                        pixels[at] = r;
                        pixels[at + 1] = g;
                        pixels[at + 2] = b;
                        pixels[at + 3] = 255;
                    }
                }
            }

            return pixels;
        }

        // In the column's order, +X, -X, +Y, -Y, +Z, -Z.
        var column = (1, 6, Layout(1, 6, [(0, 0, false), (0, 1, false), (0, 2, false), (0, 3, false), (0, 4, false), (0, 5, false)]));
        var horizontal = (4, 3, Layout(4, 3, [(2, 1, false), (0, 1, false), (1, 0, false), (1, 2, false), (1, 1, false), (3, 1, false)]));
        var vertical = (3, 4, Layout(3, 4, [(2, 1, false), (0, 1, false), (1, 0, false), (1, 2, false), (1, 1, false), (1, 3, true)]));
        var row = (6, 1, Layout(6, 1, [(0, 0, false), (1, 0, false), (2, 0, false), (3, 0, false), (4, 0, false), (5, 0, false)]));

        // And the six as images of their own, as a cubemap shipped as six files is.
        AssetHandle Separate()
        {
            var faces = new AssetHandle[6];
            for (var face = 0; face < 6; face++)
            {
                var pixels = new byte[Face * Face * 4];
                for (var y = 0; y < Face; y++)
                {
                    for (var x = 0; x < Face; x++)
                    {
                        var (r, g, b) = Pixel(face, x, y);
                        var at = ((y * Face) + x) * 4;
                        (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = (r, g, b, 255);
                    }
                }

                faces[face] = Render.CreateImage(pixels, Face, Face);
            }

            return Render.CubemapFromFaces(faces[0], faces[1], faces[2], faces[3], faces[4], faces[5]);
        }

        foreach (var looking in new[] { -Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitX })
        {
            var expected = Sky(Image(column), looking);

            foreach (var (name, make) in new[] { ("horizontal cross", Image(horizontal)), ("vertical cross", Image(vertical)), ("row", Image(row)), ("six images", Separate) })
            {
                var drawn = Sky(make, looking);
                var different = 0;

                for (var i = 0; i < expected.Pixels.Length; i++)
                {
                    if (Math.Abs(expected.Pixels[i] - drawn.Pixels[i]) > 6) different++;
                }

                Assert.True(different < expected.Pixels.Length / 100, $"the {name} looking along {looking} differs from the column in {different} channels");
            }
        }

        static Func<AssetHandle> Image((int Across, int Down, byte[] Pixels) layout) =>
            () => Render.CreateImage(layout.Pixels, (uint)(layout.Across * Face), (uint)(layout.Down * Face));

        static CapturedImage Sky(Func<AssetHandle> make, Vec3 looking)
        {
            CapturedImage? picture = null;
            Capture? ticket = null;

            using var app = new App(Config.OffscreenFor(48, 48, frames: (uint)Settled + 40));
            app.AddPlugin(new EnginePlugin());

            app.AddSystem(Stage.Startup, new SystemDescriptor(
                world =>
                {
                    var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                    world.Resource<EcsWorld>().Add(camera, Transform.LookingAt(Vec3.Zero, looking, Vec3.UnitY));

                    Render.SetSkybox(camera, make(), brightness: 1000f);
                },
                "Test.Sky"));

            app.AddSystem(Stage.Update, new SystemDescriptor(
                world =>
                {
                    if (world.Resource<Time>().FrameCount == Settled) ticket = Render.BeginCapture();
                    if (picture is null && ticket is { } asked && Render.TryReadCapture(asked, out var arrived)) picture = arrived;
                },
                "Test.Read"));

            Assert.Equal(0, app.Run());
            Assert.NotNull(picture);
            return picture;
        }
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}

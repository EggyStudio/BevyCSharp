using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers depth of field, the one lens effect <see cref="LensTests"/> leaves out, by how much it
/// softens a surface it is not focused on.
/// </summary>
/// <remarks>
/// <para>
/// Built as the effect's own traps say it has to be. A short lens focused far away keeps almost
/// everything sharp, so the lens here is focused a hundred meters off with its aperture wide open
/// and a tall sensor, which puts the near surface far out of focus. The blur is capped in pixels,
/// so the cap is set well above what the test needs. And the pass keeps a silhouette from
/// smearing into what is behind it, so the edge measured is not a silhouette at all. The surface
/// fills the frame, and its own checkers are the edges, every one of them at the same depth.
/// </para>
/// <para>
/// Sharpness is the square of how much neighboring pixels differ, summed over the middle of the
/// picture. Squared, because a blur spreads an edge's change over several pixels without changing
/// how much it changes in all, so a plain sum of differences comes out the same however soft the
/// edge is, while the squares of many small steps add up to far less than the square of one.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class DepthOfFieldTests
{
    private const ulong Settled = 150;

    [SkippableFact]
    public void ASurfaceOutOfFocusIsSofterThanTheSameSurfaceInFocus()
    {
        Needs.Renderer();

        var sharp = Draw(_ => { });
        var soft = Draw(effects =>
        {
            effects.DepthOfField = DepthOfFieldMode.Gaussian;
            effects.FocalDistance = 100f;
            effects.Aperture = 0.5f;
            effects.SensorHeight = 0.1f;
            effects.MaxBlurDiameter = 48f;
        });

        Assert.NotNull(sharp);
        Assert.NotNull(soft);

        var before = Edges(sharp);
        var after = Edges(soft);

        Assert.True(before > 0, "the checkers drew no edges, so there is nothing to blur");
        Assert.True(after < before * 0.6, $"in focus the edges added up to {before} and out of focus to {after}");
    }

    /// <summary>How much each pixel differs from the one to its right, squared and added up over the middle.</summary>
    private static long Edges(CapturedImage picture)
    {
        long total = 0;
        var margin = (int)picture.Width / 4;

        for (var y = margin; y < picture.Height - margin; y++)
        {
            for (var x = margin; x < picture.Width - margin - 1; x++)
            {
                var here = picture.At((uint)x, (uint)y);
                var next = picture.At((uint)x + 1, (uint)y);
                var difference = Math.Abs(here.R - next.R) + Math.Abs(here.G - next.G) + Math.Abs(here.B - next.B);
                total += (long)difference * difference;
            }
        }

        return total;
    }

    /// <summary>Draws a checkered surface filling the frame through whatever the effects were set to.</summary>
    private static CapturedImage? Draw(Action<EffectSettings> lens)
    {
        CapturedImage? picture = null;
        Capture? ticket = null;

        using var app = new App(Config.OffscreenFor(128, 128, frames: (uint)Settled + 60));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 50f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 4f), Vec3.Zero, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true });

                var effects = new EffectSettings();
                lens(effects);
                Render.SetEffects(camera, effects);

                // Eight checkers across, black and white, nearest-sampled so each edge starts as
                // one pixel wide.
                const int Side = 64;
                var pixels = new byte[Side * Side * 4];
                for (var y = 0; y < Side; y++)
                {
                    for (var x = 0; x < Side; x++)
                    {
                        var white = ((x / 8) + (y / 8)) % 2 == 0;
                        var at = ((y * Side) + x) * 4;
                        pixels[at] = pixels[at + 1] = pixels[at + 2] = white ? (byte)255 : (byte)0;
                        pixels[at + 3] = 255;
                    }
                }

                var checkers = Render.CreateImage(pixels, Side, Side);

                var surface = ecs.Spawn();
                Render.SetMesh(ecs, surface, Render.CreateMesh(MeshShape.Rectangle, 8f, 8f, 1f));
                Render.SetMaterial(ecs, surface, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColorTexture = checkers,
                    Unlit = true,
                }));
                ecs.Add(surface, Transform.Identity);
            },
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == Settled) ticket = Render.BeginCapture();
                if (picture is null && ticket is { } asked && Render.TryReadCapture(asked, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());
        return picture;
    }
}

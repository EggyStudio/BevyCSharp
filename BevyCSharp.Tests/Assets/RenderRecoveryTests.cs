using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what an error the GPU reports does to an app, by each policy, with the error caused on
/// purpose as Bevy's render_recovery example causes it.
/// </summary>
/// <remarks>
/// Only a validation error is caused. A lost device ends the run in Bevy's own code more often than
/// not (<see cref="ARendererMadeAgainDrawsOn"/>), and the other two reach the machine's own graphics,
/// which a test run has no business doing.
/// </remarks>
[Collection("engine")]
public sealed class RenderRecoveryTests
{
    /// <summary>The frame the error is caused in, once the renderer has drawn a few.</summary>
    private const ulong CausedAt = 10;

    /// <summary>Unless told otherwise, an error closes the app with an error, long before its last frame.</summary>
    [SkippableFact]
    [ExpectsError("bevy", "Validation")]
    public void AnErrorEndsTheRunUnlessToldOtherwise()
    {
        Needs.Renderer();

        var run = Run(RenderErrorPolicy.Quit, RenderErrorCause.Validation, frames: 400);

        Assert.NotEqual(0, run.Exit);
        Assert.True(run.LastFrame < 300, $"the app ran to frame {run.LastFrame}");
        Assert.True(run.Errors >= 1, "no error was counted");
    }

    /// <summary>
    /// Ignored, an error is counted and its text kept, and the app runs to its last frame.
    /// </summary>
    [SkippableFact]
    [ExpectsError("bevy", "Validation Error")]
    public void AnIgnoredErrorLeavesTheRunGoing()
    {
        Needs.Renderer();

        var run = Run(RenderErrorPolicy.Ignore, RenderErrorCause.Validation, frames: 40);

        Assert.Equal(0, run.Exit);
        Assert.Equal(39ul, run.LastFrame);
        Assert.True(run.Errors >= 1, "no error was counted");
        Assert.False(string.IsNullOrEmpty(run.Text), "the error's text was not kept");
    }

    /// <summary>
    /// With drawing stopped the app's systems run on to its last frame, and the error Bevy hands
    /// back every frame since is counted once.
    /// </summary>
    [SkippableFact]
    [ExpectsError("bevy", "Validation Error")]
    public void StoppedDrawingLeavesTheSystemsRunningAndCountsTheErrorOnce()
    {
        Needs.Renderer();

        var run = Run(RenderErrorPolicy.StopRendering, RenderErrorCause.Validation, frames: 40);

        Assert.Equal(0, run.Exit);
        Assert.Equal(39ul, run.LastFrame);
        Assert.Equal(1, run.Errors);
    }

    /// <summary>
    /// Under <see cref="RenderErrorPolicy.Recover"/> the renderer is made again, a device and all,
    /// and the app draws on with the new one to its last frame.
    /// </summary>
    /// <remarks>
    /// After a validation error, since a device lost while Bevy's GPU clustering waits on a readback
    /// ends the run in Bevy's own code, which a run of this test met four times in six.
    /// </remarks>
    [SkippableFact]
    [ExpectsError("bevy", "Validation Error")]
    public void ARendererMadeAgainDrawsOn()
    {
        Needs.Renderer();

        var run = Run(RenderErrorPolicy.Recover, RenderErrorCause.Validation, frames: 120, captureAt: 90);

        Assert.Equal(0, run.Exit);
        Assert.Equal(119ul, run.LastFrame);
        Assert.True(run.Errors >= 1, "no error was counted");

        // The cube, pale blue and unlit, is drawn in the middle of a picture taken after the device
        // was made again, so drawing went on with the new one rather than the frames merely counting on.
        Assert.NotNull(run.Picture);
        var (r, _, b, _) = run.Picture.At(32, 32);
        Assert.True(b > r + 20, $"the middle of the picture reads red {r} and blue {b}");
    }

    /// <summary>A policy or a cause that is neither is refused before the bridge is asked.</summary>
    [Fact]
    public void ValuesThatAreNoneOfThemAreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.ErrorPolicy = (RenderErrorPolicy)7);
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.CauseError((RenderErrorCause)9));
    }

    /// <summary>What a run came to.</summary>
    /// <param name="Exit">The code the run ended with.</param>
    /// <param name="LastFrame">
    /// The last frame its systems ran in, counted from zero, so a run of forty frames ends in
    /// frame 39.
    /// </param>
    /// <param name="Errors">How many errors the renderer reported during it.</param>
    /// <param name="Text">The last error's text.</param>
    /// <param name="Picture">The picture captured where one was asked for.</param>
    private sealed record Outcome(int Exit, ulong LastFrame, int Errors, string Text, CapturedImage? Picture);

    /// <summary>
    /// Runs a small scene offscreen under <paramref name="policy"/>, causing the error at
    /// <see cref="CausedAt"/>, and puts the policy back after.
    /// </summary>
    private static Outcome Run(RenderErrorPolicy policy, RenderErrorCause cause, uint frames, ulong captureAt = 0)
    {
        var config = Config.OffscreenFor(64, 64, frames);
        config.AssetRoot = EngineHarness.AssetDirectory;

        ulong lastFrame = 0;
        var before = Render.ErrorCount;
        Capture? capture = null;
        CapturedImage? picture = null;

        try
        {
            Render.ErrorPolicy = policy;

            int exit;
            using (var app = new App(config))
            {
                app.AddPlugin(new EnginePlugin());

                app.AddSystem(Stage.Startup, new SystemDescriptor(
                    world =>
                    {
                        var ecs = world.Resource<EcsWorld>();
                        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 2f, 5f), Vec3.Zero, Vec3.UnitY));
                        // Unlit, since Bevy 0.20 makes the renderer again without the lighting it
                        // had, a lit mesh drawing black after, where an unlit one draws as before.
                        var material = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.5f, 0.6f, 1f, 1f), Unlit = true });
                        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, Transform.Identity);
                        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f));
                    },
                    "Test.Scene"));

                app.AddSystem(Stage.Update, new SystemDescriptor(
                    world =>
                    {
                        lastFrame = world.Resource<Time>().FrameCount;
                        if (lastFrame == CausedAt) Render.CauseError(cause);

                        if (captureAt > 0 && lastFrame == captureAt) capture = Render.BeginCapture();
                        if (capture is { } asked && picture is null && Render.TryReadCapture(asked, out var arrived)) picture = arrived;
                    },
                    "Test.Cause"));

                exit = app.Run();
            }

            return new Outcome(exit, lastFrame, Render.ErrorCount - before, Shaders.LastRenderError, picture);
        }
        finally
        {
            Render.ErrorPolicy = RenderErrorPolicy.Quit;
        }
    }
}

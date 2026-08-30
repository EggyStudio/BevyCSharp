using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Runs a headless engine for a fixed number of frames and collects what happened.
/// </summary>
/// <remarks>
/// <para>
/// Everything the ECS surface does needs a live world loaned by a running system, so there is
/// no way to assert on it from outside the loop. The harness inverts that: you hand it the
/// assertions as systems, it runs a real Bevy app for a few frames, and afterwards you inspect
/// what those systems recorded.
/// </para>
/// <para>
/// An exception thrown inside a system cannot unwind into Rust, so the engine catches it. The
/// harness re-raises it here instead, which is what makes a failing assertion inside a system
/// show up as a failing test rather than a line on stderr.
/// </para>
/// </remarks>
public sealed class EngineHarness : IDisposable
{
    private readonly App _app;
    private readonly List<Exception> _failures = [];
    private bool _ran;

    /// <summary>The app under test.</summary>
    public App App => _app;

    /// <summary>The managed resource world.</summary>
    public World World => _app.World;

    /// <summary>Builds a headless engine that will run <paramref name="frames"/> ticks.</summary>
    /// <param name="frames">Number of frames to run, or 0 to run until a system asks to exit.</param>
    /// <param name="discoverBehaviors">
    /// Whether to run the generated behavior registration. Off by default so a test that adds
    /// its own systems is not perturbed by every <c>[Behavior]</c> struct in the test assembly.
    /// </param>
    /// <param name="fps">
    /// Frames per second, or 0 to run them back to back. A test waiting on work the engine does
    /// off the main thread should pace itself, because an unpaced loop competes with that work
    /// for the core it needs.
    /// </param>
    public EngineHarness(uint frames = 4, bool discoverBehaviors = false, uint fps = 0)
    {
        _app = new App(new Config
        {
            Headless = true,
            HeadlessFrames = frames,
            HeadlessFps = fps,
        });

        _app.AddPlugin(new EnginePlugin());
        if (discoverBehaviors) _app.AddPlugin(new BehaviorsPlugin());
    }

    /// <summary>Adds a system, capturing anything it throws for the test to re-raise.</summary>
    public EngineHarness On(Stage stage, Action<World> body, string? name = null)
    {
        _app.AddSystem(stage, new SystemDescriptor(world =>
        {
            try
            {
                body(world);
            }
            catch (Exception ex)
            {
                lock (_failures) _failures.Add(ex);
            }
        }, name ?? $"Test.{stage}"));

        return this;
    }

    /// <summary>Adds a system that receives a <see cref="BehaviorContext"/>.</summary>
    public EngineHarness OnContext(Stage stage, Action<BehaviorContext> body, string? name = null) =>
        On(stage, world => body(new BehaviorContext(world)), name);

    /// <summary>Runs the engine and rethrows the first failure a system recorded.</summary>
    public void Run()
    {
        Assert.False(_ran, "The harness can only be run once.");
        _ran = true;

        var exitCode = _app.Run();

        lock (_failures)
        {
            if (_failures.Count == 1) throw _failures[0];
            if (_failures.Count > 1) throw new AggregateException(_failures);
        }

        Assert.Equal(0, exitCode);
    }

    /// <inheritdoc/>
    public void Dispose() => _app.Dispose();
}

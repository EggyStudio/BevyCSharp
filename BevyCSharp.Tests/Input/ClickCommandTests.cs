using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <c>input.click</c>, the click a session or a script asks for by a point.
/// </summary>
[Collection("engine")]
public sealed class ClickCommandTests
{
    /// <summary>
    /// Three clicks, each at a point of a node the pointer has not been at, are each heard as a
    /// click there, as the button example counts three of three.
    /// </summary>
    /// <remarks>
    /// Picking finds what is under the pointer as it moves, so a press at a point the pointer has
    /// not been found nothing under it as often as not, and the command moves it there and waits
    /// before it presses. Run as the CLI runs a command, the world lent to it and its answer asked
    /// for each frame after until it comes.
    /// </remarks>
    [SkippableFact]
    public void AClickWhereThePointerHasNotBeenIsHeardEveryTime()
    {
        Needs.Renderer();

        var clicks = new List<Pointer<Click>>();
        var answers = new List<string>();
        (float X, float Y)[] points = [(20f, 20f), (60f, 30f), (35f, 70f)];
        Func<string?>? waiting = null;
        var frame = 0;
        var next = 0;

        using var app = new App(Config.OffscreenFor(200, 200, frames: 90));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            var node = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(0f), Width = Length.Px(100f), Height = Length.Px(100f) });
            ecs.Observe<Pointer<Click>>(node, on => clicks.Add(on.Event));
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            frame++;
            if (frame < 10) return;

            using (ConsoleHost.Lend(world))
            {
                if (waiting is { } poll)
                {
                    if (poll() is { } answer)
                    {
                        answers.Add(answer);
                        waiting = null;
                    }

                    return;
                }

                if (next == points.Length) return;

                var (x, y) = points[next++];
                ConsoleHost.Reset();
                var said = ConsoleCommands.Run(FormattableString.Invariant($"input.click {x} {y}"));
                waiting = ConsoleHost.Pending;
                if (waiting is null && said is not null) answers.Add(said);
            }
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        Assert.Equal(3, answers.Count);
        Assert.Equal(3, clicks.Count);
        for (var i = 0; i < points.Length; i++)
        {
            Assert.InRange(clicks[i].Position.X, points[i].X - 1f, points[i].X + 1f);
            Assert.InRange(clicks[i].Position.Y, points[i].Y - 1f, points[i].Y + 1f);
        }
    }
}

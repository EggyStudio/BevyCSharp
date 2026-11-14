using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A change hot reload makes to an assembly has its behaviors registered again the frame after,
/// each system named again kept where it stands, one added run, one gone stopped, and a startup
/// system passed over.
/// </summary>
/// <remarks>
/// The registration's body changes between its runs, as the generated one's does when a behavior
/// gains a method under <c>dotnet watch</c>, and the test calls the handler as the runtime calls it
/// after applying a change. The test's process takes no change, so the plugin is told to follow
/// them, and it is handed the test's registration in place of those the process announced.
/// </remarks>
[Collection("engine")]
public sealed class BehaviorReloadTests
{
    /// <summary>
    /// After a change a system named again runs once a frame on, one the registration adds runs
    /// from the frame after, one it adds no more stops, one whose method moved to another stage
    /// runs there alone, and neither startup system runs again.
    /// </summary>
    [Fact]
    public void AChangedAssemblyIsRegisteredAgainKeepingTheSystemsItNamesAgain()
    {
        var (changed, at) = (false, 0ul);
        var (kept, gone, added, movedFrom, movedTo) = (new List<ulong>(), new List<ulong>(), new List<ulong>(), new List<ulong>(), new List<ulong>());
        var (started, startedAfter) = (0, 0);
        void Register(App app)
        {
            app.AddSystem(Stage.Startup, new SystemDescriptor(_ => started++, "Reloaded.Start"));
            app.AddSystem(Stage.Update, new SystemDescriptor(world => kept.Add(Frame(world)), "Reloaded.Kept"));
            if (!changed)
            {
                app.AddSystem(Stage.Update, new SystemDescriptor(world => gone.Add(Frame(world)), "Reloaded.Gone"));
                app.AddSystem(Stage.Update, new SystemDescriptor(world => movedFrom.Add(Frame(world)), "Reloaded.Moved"));
                return;
            }

            app.AddSystem(Stage.Update, new SystemDescriptor(world => added.Add(Frame(world)), "Reloaded.Added"));
            app.AddSystem(Stage.PostUpdate, new SystemDescriptor(world => movedTo.Add(Frame(world)), "Reloaded.Moved"));
            app.AddSystem(Stage.Startup, new SystemDescriptor(_ => startedAfter++, "Reloaded.StartAdded"));
        }

        using var harness = new EngineHarness(frames: 20);
        harness.App.AddPlugin(new BehaviorsPlugin { Reloads = true, ScanLoadedAssemblies = false, Announced = () => [Register] });
        harness.On(Stage.Update, world =>
        {
            if (changed || Frame(world) < 10) return;
            (changed, at) = (true, Frame(world));
            BehaviorReload.UpdateApplication([typeof(BehaviorReloadTests)]);
        }, "Reloaded.Change");
        harness.Run();

        var last = kept.Max();
        Assert.Equal(Frames(kept.Min(), last), kept);
        Assert.Equal(Frames(kept.Min(), at), gone);
        Assert.Equal(Frames(at + 1, last), added);
        Assert.Equal(Frames(kept.Min(), at), movedFrom);
        Assert.Equal(Frames(at + 1, last), movedTo);
        Assert.Equal((1, 0), (started, startedAfter));
        Assert.DoesNotContain(harness.App.SystemsIn(Stage.Update), system => system.Name is "Reloaded.Gone" or "Reloaded.Moved");
    }

    /// <summary>
    /// A system whose method a change took out, called in the frame the change comes in before the
    /// app registers its behaviors again, is taken out at its throw with no error logged, one the
    /// app started with and one added while it ran alike.
    /// </summary>
    /// <remarks>
    /// Each throws as hot reload's body for a method taken out does, from the method itself, in a
    /// stage after the one the change comes in, as a change applied on a thread of its own can.
    /// </remarks>
    [Fact]
    public void ASystemWhoseMethodAChangeTookOutIsTakenOutAtItsThrow()
    {
        var (changes, at) = (0, new List<ulong>());
        var (started, added) = (new List<ulong>(), new List<ulong>());
        void Register(App app)
        {
            if (changes == 0)
            {
                app.AddSystem(Stage.PostUpdate, new SystemDescriptor(world =>
                {
                    if (changes >= 1) throw new MissingMethodException("taken out");
                    started.Add(Frame(world));
                }, "Reloaded.Started"));
            }

            if (changes == 1)
            {
                app.AddSystem(Stage.PostUpdate, new SystemDescriptor(world =>
                {
                    if (changes >= 2) throw new MissingMethodException("taken out");
                    added.Add(Frame(world));
                }, "Reloaded.Added"));
            }
        }

        using var harness = new EngineHarness(frames: 20);
        harness.App.AddPlugin(new BehaviorsPlugin { Reloads = true, ScanLoadedAssemblies = false, Announced = () => [Register] });
        harness.On(Stage.Update, world =>
        {
            if (Frame(world) is not (10 or 15)) return;
            changes++;
            at.Add(Frame(world));
            BehaviorReload.UpdateApplication([typeof(BehaviorReloadTests)]);
        }, "Reloaded.Change");

        var logged = new List<string>();
        void Heard(App? by, string kind, string text, Exception? exception)
        {
            if (by == harness.App) lock (logged) logged.Add(text);
        }

        EngineLog.ErrorLogged += Heard;
        try
        {
            harness.Run();
        }
        finally
        {
            EngineLog.ErrorLogged -= Heard;
        }

        Assert.Empty(logged);
        Assert.Equal([10ul, 15ul], at);
        Assert.Equal(Frames(started.Min(), 9), started);
        Assert.Equal(Frames(11, 14), added);
        Assert.DoesNotContain(harness.App.SystemsIn(Stage.PostUpdate), system => system.Name is "Reloaded.Started" or "Reloaded.Added");
    }

    /// <summary>
    /// An app follows changes where its process can take one, with a system for it and the
    /// dispatchers, and one whose process cannot, as the suite's cannot, has neither.
    /// </summary>
    [Fact]
    public void AnAppFollowsChangesWhereItsProcessCanTakeThem()
    {
        using var app = new App(Config.HeadlessFor(1));
        app.AddPlugin(new EnginePlugin());
        app.AddPlugin(EngineHarness.Behaviors());

        var first = app.SystemsIn(Stage.First).Select(system => system.Name).ToList();
        Assert.Equal(BehaviorReload.Possible, first.Contains("Behaviors.Reload"));
        Assert.Equal(BehaviorReload.Possible, first.Contains("DynamicSystems.First"));
    }

    private static ulong Frame(World world) => new BehaviorContext(world).Time.FrameCount;

    private static List<ulong> Frames(ulong first, ulong last) => [.. Enumerable.Range((int)first, (int)(last - first + 1)).Select(frame => (ulong)frame)];
}

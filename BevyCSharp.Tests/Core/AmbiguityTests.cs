using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The pairs of systems whose declared access conflicts and whose order nothing states, as Bevy's
/// nondeterministic_system_order reports them.
/// </summary>
[Collection("engine")]
public sealed class AmbiguityTests
{
    private sealed class First;

    private sealed class Second;

    private static void Nothing(World world) { }

    /// <summary>
    /// A writer and a reader of one resource with no order are a pair, and an order stated through a
    /// third system, a pair of readers, a pair said to be ambiguous and a system declaring nothing
    /// are not.
    /// </summary>
    [Fact]
    public void APairIsAConflictNoOrderCoversAndNoneExcuses()
    {
        using var harness = new EngineHarness(frames: 1);
        var app = harness.App;
        app.AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Writes").Write<First>());
        app.AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Reads").Read<First>());
        app.AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.AlsoReads").Read<First>().AmbiguousWith("Tests.Writes"));
        app.AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Between").After("Tests.Writes"));
        app.AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Later").Write<Second>().Read<First>().After("Tests.Between"));
        app.AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Unsaid"));

        var found = app.AmbiguitiesIn(Stage.Update).Select(pair => (pair.First, pair.Second, pair.Conflicts.Single())).ToList();

        Assert.Equal([("Tests.Writes", "Tests.Reads", typeof(First))], found);
        Assert.Empty(app.AmbiguitiesIn(Stage.Update, among: name => name != "Tests.Reads"));
    }

    /// <summary>A stage asked to refuse its ambiguities keeps the app from starting, naming the pair.</summary>
    [Fact]
    public void AStageAskedToRefuseKeepsTheAppFromStarting()
    {
        using var harness = new EngineHarness(frames: 1);
        harness.App
            .DetectAmbiguities(Stage.Update, AmbiguityReport.Error)
            .AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Writes").Write<First>())
            .AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Reads").Read<First>());

        var refused = Assert.Throws<InvalidOperationException>(harness.Run);
        Assert.Contains(" -- Tests.Writes and Tests.Reads", refused.Message);
    }

    /// <summary>A stage asked only to warn runs, as one with no pair does.</summary>
    [Fact]
    public void AStageAskedToWarnRuns()
    {
        using var harness = new EngineHarness(frames: 2);
        var ran = 0;
        harness.App
            .DetectAmbiguities(Stage.Update)
            .AddSystem(Stage.Update, new SystemDescriptor(_ => ran++, "Tests.Writes").Write<First>())
            .AddSystem(Stage.Update, new SystemDescriptor(Nothing, "Tests.Reads").Read<First>());

        harness.Run();

        Assert.Equal(2, ran);
    }
}

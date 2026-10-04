using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers ordering systems within a stage, by name, through Bevy's schedule and among systems added
/// while the app runs.
/// </summary>
/// <remarks>
/// Each test adds its systems in the opposite of the order it asks for, so an order that matched
/// only because Bevy happened to keep the order of adding would still fail.
/// </remarks>
[Collection("engine")]
public sealed class SystemOrderTests
{
    [Fact]
    public void SystemsRunAfterTheOnesTheyName()
    {
        var ran = new List<string>();
        using var app = new App(Config.HeadlessFor(3));
        app.AddPlugin(new EnginePlugin());

        // E after D after C after B after A, added from E to A.
        string[] names = ["A", "B", "C", "D", "E"];
        for (var i = names.Length - 1; i >= 0; i--)
        {
            var name = names[i];
            var descriptor = new SystemDescriptor(_ => { lock (ran) ran.Add(name); }, name);
            if (i > 0) descriptor.After(names[i - 1]);
            app.AddSystem(Stage.Update, descriptor);
        }

        Assert.Equal(0, app.Run());
        Assert.Equal(["A", "B", "C", "D", "E", "A", "B", "C", "D", "E", "A", "B", "C", "D", "E"], ran);
    }

    [Fact]
    public void BeforeAndAChainOrderAsAfterDoes()
    {
        var ran = new List<string>();
        using var app = new App(Config.HeadlessFor(1));
        app.AddPlugin(new EnginePlugin());

        SystemDescriptor Log(string name) => new(_ => { lock (ran) ran.Add(name); }, name);

        // Last names a system added after it, which is found as the app starts.
        app.AddSystem(Stage.Update, Log("Last").After("Three"));
        app.Chain(Stage.Update, Log("One"), Log("Two"), Log("Three"));
        app.AddSystem(Stage.Update, Log("First").Before("One"));

        Assert.Equal(0, app.Run());
        Assert.Equal(["First", "One", "Two", "Three", "Last"], ran);
    }

    [Fact]
    public void ANameNothingCarriesStopsTheAppStartingAndSaysWhich()
    {
        using var app = new App(Config.HeadlessFor(1));
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.Update, new SystemDescriptor(_ => { }, "Late").After("Missing"));

        var error = Assert.Throws<InvalidOperationException>(() => app.Run());
        Assert.Contains("'Missing'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANameInAnotherStageIsRefusedAndTheStagesNamed()
    {
        using var app = new App(Config.HeadlessFor(1));
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.PreUpdate, new SystemDescriptor(_ => { }, "Early"));
        app.AddSystem(Stage.Update, new SystemDescriptor(_ => { }, "Late").After("Early"));

        var error = Assert.Throws<InvalidOperationException>(() => app.Run());
        Assert.Contains("PreUpdate", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemsAddedWhileRunningAreOrderedAmongThemselves()
    {
        var ran = new List<string>();
        using var app = new App(Config.HeadlessFor(4));
        app.AddPlugin(new EnginePlugin());
        app.EnableDynamicSystems();

        var added = false;
        app.AddSystem(Stage.First, new SystemDescriptor(_ =>
        {
            if (added) return;
            added = true;
            app.AddSystem(Stage.Update, new SystemDescriptor(_ => { lock (ran) ran.Add("Second"); }, "Second").After("First"));
            app.AddSystem(Stage.Update, new SystemDescriptor(_ => { lock (ran) ran.Add("First"); }, "First"));
        }, "Test.AddWhileRunning"));

        Assert.Equal(0, app.Run());
        Assert.NotEmpty(ran);
        for (var i = 0; i < ran.Count; i += 2) Assert.Equal(["First", "Second"], ran.Skip(i).Take(2));
    }
}

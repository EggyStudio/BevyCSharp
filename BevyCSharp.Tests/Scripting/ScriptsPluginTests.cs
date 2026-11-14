using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The scripts' plugin compiles the folder's scripts as the app is built, compiles a script saved
/// while an app watching its assets runs so that its new system runs, leaves a change alone where
/// the app does not watch until <c>scripts.reload</c> asks, keeps the running scripts where a build
/// fails, whether in compiling, in reading a script or in registering, says each build in the log
/// and through <c>scripts.status</c>, stops what the last script ran when it is deleted, and starts
/// unwatched where its folder cannot be watched.
/// </summary>
/// <remarks>
/// The script's system names an entity after the version written, so which generation runs is read
/// from the world. Bevy's log reaches the console's ring where the run's log is started, which the
/// suite's apps leave off, so each case starts it in a folder of its own.
/// </remarks>
[Collection("engine")]
public sealed class ScriptsPluginTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-scripts-plugin-");
    private readonly TestFolder _logs = new("bcs-scripts-plugin-logs-");
    private readonly string? _asked = Environment.GetEnvironmentVariable(Config.WatchAssetsVariable);

    public ScriptsPluginTests()
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, null);
        Directory.CreateDirectory(_folder.Path);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, _asked);
        _folder.Dispose();
        _logs.Dispose();
    }

    /// <summary>
    /// A script whose system names an entity after <paramref name="version"/>, once, beside a second
    /// behavior, so a build's count is of its behaviors and not of the one registration an assembly
    /// is given.
    /// </summary>
    private void Write(string version) => File.WriteAllText(_folder.File("Marker.cs"), $$"""
        using Bevy;

        namespace Watched;

        [Behavior]
        public partial struct Marker
        {
            [OnUpdate]
            public static void Mark(BehaviorContext ctx)
            {
                foreach (var entity in ctx.Ecs.All())
                    if (ctx.Ecs.NameOf(entity) == "{{version}}") return;

                ctx.Ecs.SetName(ctx.Ecs.Spawn(), "{{version}}");
            }
        }

        [Behavior]
        public partial struct Idle
        {
            [OnUpdate]
            public static void Wait(BehaviorContext ctx)
            {
            }
        }
        """);

    /// <summary>The names in the world.</summary>
    private static string[] Names(EcsWorld ecs) => [.. ecs.All().Select(ecs.NameOf).OfType<string>()];

    /// <summary>The info and warning lines the app said of its scripts.</summary>
    private static string[] Said() =>
        [.. ConsoleLog.All().Where(line => line.Text.Contains("scripts", StringComparison.Ordinal)).Select(line => $"{line.Level} {line.Text}")];

    /// <summary>
    /// A script saved again while an app watching its assets runs is compiled, its new system runs
    /// in place of the old one's, both builds are said and the second is posted.
    /// </summary>
    [Fact]
    public void AScriptSavedWhileTheAppWatchesIsCompiledAndItsNewSystemRuns()
    {
        Write("first");
        var (seen, written, after) = (Array.Empty<string>(), false, 0);
        var reloads = new List<AssetReloaded>();
        string[] said;
        CrashLog.Start(_logs.Path, GraphicsBackend.Vulkan);
        try
        {
            // Thirty seconds for the watcher's one event, the settling and the build, a loaded
            // runner's late event among them, and the run ends as the new system is seen.
            using (var harness = new EngineHarness(frames: 3600, fps: 120, configure: config => config.WatchAssets = true))
            {
                harness.App.AddPlugin(new ScriptsPlugin(_folder.Path));
                harness.OnContext(Stage.Update, ctx =>
                {
                    seen = Names(ctx.Ecs);
                    reloads.AddRange(ctx.Read<AssetReloaded>().ToArray());
                    if (!written && seen.Contains("first"))
                    {
                        Write("second");
                        written = true;
                    }

                    // A few frames on, since what Bevy is told in a frame reaches the console's ring
                    // in a frame after it.
                    if (seen.Contains("second") && ++after == 5) ctx.Exit();
                });
                harness.Run();
            }

            said = Said();
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Contains("first", seen);
        Assert.Contains("second", seen);
        Assert.Contains($"Info Compiled the scripts in {_folder.Path}, one file and 2 behaviors.", said);
        Assert.Contains($"Info Compiled the scripts in {_folder.Path} again after a change, one file and 2 behaviors.", said);

        // The folder outside the asset root, so named in full.
        Assert.Equal([new AssetReloaded(_folder.Path, "Scripts")], reloads);
    }

    /// <summary>
    /// Where the app does not watch, a script saved again is left alone until <c>scripts.reload</c>
    /// compiles it, a script that does not compile leaves the running one and is said as a failure,
    /// and <c>scripts.status</c> says each.
    /// </summary>
    [Fact]
    public void WhereTheAppDoesNotWatchScriptsReloadCompilesAChangeAndAFailedBuildKeepsTheRunningOne()
    {
        Write("first");
        var answers = new List<string>();
        var failures = new List<string?>();
        var seen = new List<string[]>();
        var frame = 0;
        string[] said;
        CrashLog.Start(_logs.Path, GraphicsBackend.Vulkan);
        try
        {
            using (var harness = new EngineHarness(frames: 100, fps: 120))
            {
                harness.App.AddPlugin(new ScriptsPlugin(_folder.Path));
                harness.On(Stage.Update, world =>
                {
                    using var lent = ConsoleHost.Lend(world);
                    switch (frame++)
                    {
                        case 2:
                            answers.Add(ConsoleCommands.Run("scripts.status") ?? "");
                            Write("second");
                            break;

                        // Past the time a watcher lets a file sit still, at the rate the app runs.
                        case 92:
                            seen.Add(Names(world.Resource<EcsWorld>()));
                            answers.Add(ConsoleCommands.Run("scripts.reload") ?? "");
                            failures.Add(ConsoleHost.Failure?.Code);
                            break;

                        case 96:
                            seen.Add(Names(world.Resource<EcsWorld>()));
                            File.WriteAllText(_folder.File("Marker.cs"), "this is no C#");
                            answers.Add(ConsoleCommands.Run("scripts.reload") ?? "");
                            failures.Add(ConsoleHost.Failure?.Code);
                            answers.Add(ConsoleCommands.Run("scripts.status") ?? "");
                            break;
                    }
                });
                harness.Run();
            }

            said = Said();
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Equal($"{_folder.Path}, 2 behaviors from one file running, built one time, not watched, so scripts.reload builds a change, the last build compiled", answers[0]);
        Assert.DoesNotContain("second", seen[0]);
        Assert.Equal("compiled one file into 2 behaviors, 0 components carried onto the new types", answers[1]);
        Assert.Null(failures[0]);
        Assert.Contains("second", seen[1]);
        Assert.StartsWith("not compiled, the ones running stay: ", answers[2]);
        Assert.Equal("NOT_COMPILED", failures[1]);
        Assert.Contains("the last build failed: ", answers[3]);
        Assert.Contains($"Info Compiled the scripts in {_folder.Path} again on request, one file and 2 behaviors.", said);
        Assert.Contains(said, line => line.StartsWith($"Warning The scripts in {_folder.Path} did not compile on request, so the ones running stay: ", StringComparison.Ordinal));
    }

    /// <summary>
    /// A build whose registration throws after its first system is in, as a behavior's startup
    /// throwing does, leaves the running generation alone, its counting system running once a frame
    /// with none of the failed generation's beside it.
    /// </summary>
    [Fact]
    public void ABuildWhoseRegistrationThrowsLeavesNothingOfItRunning()
    {
        File.WriteAllText(_folder.File("Counter.cs"), """
            using Bevy;

            namespace Counted;

            [Behavior]
            public partial struct Counter
            {
                [OnUpdate]
                public static void Tick(BehaviorContext ctx) => ctx.Ecs.SetName(ctx.Ecs.Spawn(), "tick");
            }
            """);

        var (failure, before, after, frame) = ((string?)null, 0, 0, 0);
        using (var harness = new EngineHarness(frames: 40, fps: 120))
        {
            harness.App.AddPlugin(new ScriptsPlugin(_folder.Path));
            harness.On(Stage.Update, world =>
            {
                var ticks = Names(world.Resource<EcsWorld>()).Count(name => name == "tick");
                using var lent = ConsoleHost.Lend(world);
                switch (frame++)
                {
                    // Behaviors register by name, so the counter's system is in before the
                    // starter's startup throws.
                    case 2:
                        File.WriteAllText(_folder.File("Starter.cs"), """
                            using Bevy;

                            namespace Counted;

                            [Behavior]
                            public partial struct Starter
                            {
                                [OnStartup]
                                public static void Start(BehaviorContext ctx) => throw new InvalidOperationException("a startup that throws");
                            }
                            """);
                        ConsoleCommands.Run("scripts.reload");
                        failure = ConsoleHost.Failure?.Code;
                        break;

                    case 10:
                        before = ticks;
                        break;

                    case 20:
                        after = ticks;
                        break;
                }
            });
            harness.Run();
        }

        Assert.Equal("NOT_COMPILED", failure);
        Assert.Equal(10, after - before);
    }

    /// <summary>
    /// An editor's lock file beside the scripts is passed over and a script gone by the time it is
    /// read is too, and a script that cannot be read fails the build as a warning naming it, the
    /// running scripts kept, where both threw out of the build.
    /// </summary>
    /// <remarks>
    /// A lock file is a link to nothing where links can be made, and a file of no C# where they
    /// cannot. Where files have no permissions to take away, on Windows, the unreadable half is left
    /// out.
    /// </remarks>
    [Fact]
    public void ALockFileIsPassedOverAndAnUnreadableScriptFailsAsAWarning()
    {
        Write("first");
        var lockFile = _folder.File(".#Marker.cs");
        try
        {
            File.CreateSymbolicLink(lockFile, "someone@machine.4242:1700000000");
            File.CreateSymbolicLink(_folder.File("Gone.cs"), _folder.File("Removed.cs"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            File.WriteAllText(lockFile, "this is no C#");
        }

        var unreadable = !OperatingSystem.IsWindows();
        var answers = new List<string>();
        var failures = new List<string?>();
        var frame = 0;
        string[] said;
        CrashLog.Start(_logs.Path, GraphicsBackend.Vulkan);
        try
        {
            using (var harness = new EngineHarness(frames: 12, fps: 120))
            {
                harness.App.AddPlugin(new ScriptsPlugin(_folder.Path));
                harness.On(Stage.Update, world =>
                {
                    using var lent = ConsoleHost.Lend(world);
                    switch (frame++)
                    {
                        case 2:
                            answers.Add(ConsoleCommands.Run("scripts.reload") ?? "");
                            failures.Add(ConsoleHost.Failure?.Code);
                            break;

                        case 4 when !OperatingSystem.IsWindows():
                            var shut = _folder.File("Shut.cs");
                            File.WriteAllText(shut, "namespace Watched; public static class Shut { }");
                            File.SetUnixFileMode(shut, UnixFileMode.None);
                            answers.Add(ConsoleCommands.Run("scripts.reload") ?? "");
                            failures.Add(ConsoleHost.Failure?.Code);
                            File.SetUnixFileMode(shut, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                            break;
                    }
                });
                harness.Run();
            }

            said = Said();
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Equal("compiled one file into 2 behaviors, 0 components carried onto the new types", answers[0]);
        Assert.Null(failures[0]);
        if (!unreadable) return;

        Assert.StartsWith($"not compiled, the ones running stay: {_folder.File("Shut.cs")} could not be read: ", answers[1]);
        Assert.Equal("NOT_COMPILED", failures[1]);
        Assert.Contains(said, line => line.StartsWith($"Warning The scripts in {_folder.Path} did not compile on request, so the ones running stay: {_folder.File("Shut.cs")} could not be read", StringComparison.Ordinal));
    }

    /// <summary>
    /// Deleting the last script stops the behaviors it ran, as deleting one of two stops that one's,
    /// and the commands say none run.
    /// </summary>
    [Fact]
    public void DeletingTheLastScriptStopsItsBehaviors()
    {
        Write("first");
        var (answer, status, seen, frame) = ("", "", Array.Empty<string>(), 0);
        using (var harness = new EngineHarness(frames: 12, fps: 120))
        {
            harness.App.AddPlugin(new ScriptsPlugin(_folder.Path));
            harness.On(Stage.Update, world =>
            {
                var ecs = world.Resource<EcsWorld>();
                using var lent = ConsoleHost.Lend(world);
                switch (frame++)
                {
                    case 2:
                        foreach (var entity in ecs.All().Where(entity => ecs.NameOf(entity) == "first").ToArray()) ecs.Despawn(entity);
                        File.Delete(_folder.File("Marker.cs"));
                        answer = ConsoleCommands.Run("scripts.reload") ?? "";
                        status = ConsoleCommands.Run("scripts.status") ?? "";
                        break;

                    case 10:
                        seen = Names(ecs);
                        break;
                }
            });
            harness.Run();
        }

        Assert.Equal($"no scripts are left in {_folder.Path}, so none run", answer);
        Assert.Equal($"{_folder.Path}, none running, the folder emptied, built one time, not watched, so scripts.reload builds a change, the last build compiled", status);
        Assert.DoesNotContain("first", seen);
    }

    /// <summary>
    /// A scripts folder the system will not watch, as one that cannot be made, leaves the app
    /// starting with its scripts built on request, and says why.
    /// </summary>
    [Fact]
    public void AFolderThatCannotBeWatchedLeavesTheAppStartingUnwatched()
    {
        // A folder under a file, which no system makes.
        File.WriteAllText(_folder.File("plain"), "");
        var directory = Path.Combine(_folder.File("plain"), "scripts");
        var status = "";
        string[] said;
        CrashLog.Start(_logs.Path, GraphicsBackend.Vulkan);
        try
        {
            using (var harness = new EngineHarness(frames: 8, configure: config => config.WatchAssets = true))
            {
                harness.App.AddPlugin(new ScriptsPlugin(directory));
                harness.On(Stage.Update, world =>
                {
                    using var lent = ConsoleHost.Lend(world);
                    status = ConsoleCommands.Run("scripts.status") ?? "";
                });
                harness.Run();
            }

            said = [.. ConsoleLog.All().Select(line => $"{line.Level} {line.Text}")];
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Contains("not watched, so scripts.reload builds a change", status);
        Assert.Contains(said, line => line.StartsWith($"Warning {directory} cannot be watched, so a changed script is compiled by scripts.reload alone: ", StringComparison.Ordinal));
    }

    /// <summary>An app that added no plugin answers the commands that it compiles no scripts, as a failure.</summary>
    [Fact]
    public void AnAppWithoutThePluginSaysItCompilesNoScripts()
    {
        // The commands are registered as this assembly is first touched, as an app adding the
        // plugin touches it.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(ScriptsPlugin).Module.ModuleHandle);

        var (answer, failure) = ("", (string?)null);
        using var harness = new EngineHarness(frames: 2);
        harness.On(Stage.Update, world =>
        {
            using var lent = ConsoleHost.Lend(world);
            answer = ConsoleCommands.Run("scripts.status") ?? "";
            failure = ConsoleHost.Failure?.Code;
        });
        harness.Run();

        Assert.Equal("this app compiles no scripts, since it added no ScriptsPlugin", answer);
        Assert.Equal("NO_SCRIPTS", failure);
    }
}

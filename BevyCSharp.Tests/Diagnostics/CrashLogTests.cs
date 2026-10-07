using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The run's log and the crash file, written in a folder of the test's own, as a game writes them
/// beside its executable.
/// </summary>
/// <remarks>
/// <para>
/// Each test starts the log itself, since the suite's apps leave it off (<c>SuiteLogs</c>), and
/// stops it as it is disposed, putting the console's streams back. An exception nothing caught ends
/// the process it is thrown in, so the crash written for one is tried through
/// <see cref="CrashLog.Write"/>, which the handler calls, and the bridge's hook is tried with a
/// panic made on purpose on a thread of its own, which ends that thread alone.
/// </para>
/// <para>
/// In the engine collection, since the log and the console's ring are the process's, and one test
/// runs an app.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class CrashLogTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-logs-");

    public CrashLogTests() => CrashLog.Start(_folder.Path, GraphicsBackend.Vulkan);

    public void Dispose()
    {
        CrashLog.Stop();
        _folder.Dispose();
    }

    private string Latest => Read(_folder.File("latest.log"));

    /// <summary>A file of the run's, read whole while the log may still be open for writing.</summary>
    /// <remarks>
    /// The log is open for the whole run, and Windows lets a reader open a file another handle
    /// writes only where the reader shares writing as well, which <c>File.ReadAllText</c> does
    /// not, so the three tests reading the log failed there alone. This reads as a tester's tail
    /// does, and every file here is read through it so none is read the other way.
    /// </remarks>
    private static string Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void ACrashIsWrittenWithWhatHappenedTheLogAndTheMachine()
    {
        Console.WriteLine("a line before the crash");

        var path = CrashLog.Write("Something went wrong", "System.Exception: the reason");

        Assert.NotNull(path);
        Assert.StartsWith(_folder.File("crash-"), path);
        var text = Read(path);
        Assert.Contains("== Something went wrong", text);
        Assert.Contains("System.Exception: the reason", text);
        Assert.Contains("a line before the crash", text);
        Assert.Contains($"ABI {Native.ExpectedAbiVersion}", text);
        Assert.Contains("Vulkan asked for", text);
        Assert.Contains(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, text);

        // A second crash in the same run goes in the same file, after the first.
        Assert.Equal(path, CrashLog.Write("Then something else", "the second reason"));
        Assert.True(Read(path).IndexOf("the second reason", StringComparison.Ordinal) > text.Length - 1);
    }

    [Fact]
    public void APanicOnAThreadOfBevysIsWrittenAsItHappens()
    {
        Native.Check(Native.bcs_panic_on_purpose(0), "panicking on purpose");

        Assert.NotNull(CrashLog.Crash);
        var text = Read(CrashLog.Crash);
        Assert.Contains("A panic inside Bevy", text);
        Assert.Contains("thread 'panic on purpose' panicked at", text);
        Assert.Contains("stack:", text);
    }

    [Fact]
    public void APanicCaughtAtTheBoundarySaysWhatItSaidInTheException()
    {
        var thrown = Assert.Throws<BevyNativeException>(
            () => Native.Check(Native.bcs_panic_on_purpose(1), "panicking on purpose"));

        Assert.Contains("a panic asked for, to try the crash log", thrown.Message);

        // Caught and thrown as an exception it is not a crash until nothing catches that.
        Assert.Null(CrashLog.Crash);
    }

    [Fact]
    public void TheNextRunSaysWhereTheLastCrashIsAndKeepsTheLastRunsLog()
    {
        Console.WriteLine("said in the first run");
        var crash = CrashLog.Write("The first run crashed", "the reason");

        CrashLog.Stop();
        CrashLog.Start(_folder.Path, GraphicsBackend.Vulkan);

        Assert.Equal(crash, CrashLog.LastCrash);
        Assert.Contains("said in the first run", Read(_folder.File("latest.1.log")));
        Assert.Contains("The last run crashed", Latest);
        Assert.DoesNotContain("said in the first run", Latest);
        Assert.False(File.Exists(_folder.File("crashed.txt")), "the marker is read once");
    }

    [Fact]
    public void ALineSaidOverAndOverIsWrittenOnceWithACount()
    {
        for (var i = 0; i < 5; i++) Console.WriteLine("again and again");
        Console.WriteLine("something else");

        var lines = Read(_folder.File("latest.log")).ReplaceLineEndings("\n").Split('\n');
        Assert.Single(lines, line => line.EndsWith("again and again", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Trim() == "said 4 more times");
    }

    [Fact]
    public void WhatBevyAndCSharpLogAreInTheRunsLog()
    {
        using var harness = new EngineHarness(frames: 3);
        harness.OnContext(Stage.Startup, _ => Log.Warn("a warning from C#"));
        harness.Run();

        Assert.Contains(ConsoleLog.All(), line => line is { Level: LogLevel.Warning, Text: "a warning from C#" });
        Assert.Contains("WARN  a warning from C#", Latest);
    }
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers how a running app is found, which is a file and a heartbeat.
/// </summary>
/// <remarks>
/// Each test points the directory at one of its own. The real one holds whatever the person at
/// this machine is running, and a test that wrote into it would both disturb that and be disturbed
/// by it.
/// </remarks>
public sealed class CliSessionTests : IDisposable
{
    private readonly string _was = CliSessionFile.Directory;
    private readonly TestFolder _folder = new("bcs-sessions-");

    public CliSessionTests() => CliSessionFile.Directory = _folder.Path;

    public void Dispose()
    {
        CliSessionFile.Directory = _was;

        _folder.Dispose();
    }

    [Fact]
    public void WhatIsWrittenIsWhatIsRead()
    {
        var written = Session(Environment.ProcessId);

        CliSessionFile.Write(written);

        var read = Assert.Single(CliSessionFile.All());

        Assert.Equal(written.Pid, read.Pid);
        Assert.Equal(written.Port, read.Port);
        Assert.Equal(written.Token, read.Token);
        Assert.Equal(written.Project, read.Project);
        Assert.Equal(written.Name, read.Name);
        Assert.Equal(written.Abi, read.Abi);
        Assert.True(read.Renderer);
        Assert.Equal("ready", read.State);
        Assert.Equal(12u, read.Frame);
    }

    /// <summary>A heartbeat that has stopped is the signal, not the file's absence.</summary>
    [Fact]
    public void AnOldHeartbeatReadsAsStale()
    {
        var fresh = Session(Environment.ProcessId);
        var old = fresh with { Heartbeat = DateTimeOffset.UtcNow - CliSession.Patience * 2 };

        Assert.False(fresh.Stale);
        Assert.True(old.Stale);
    }

    /// <summary>
    /// A file whose process is gone is reported as gone rather than as ready.
    /// </summary>
    /// <remarks>
    /// A port usually refuses a connection because the app died without the chance to tidy up, and
    /// what it left behind still says "ready". Reading the process settles it.
    /// </remarks>
    [Fact]
    public void AFileWithoutAProcessIsGone()
    {
        var orphan = Session(Gone) with { State = "ready" };

        CliSessionFile.Write(orphan);

        var read = Assert.Single(CliSessionFile.All());

        Assert.False(read.Running);
        Assert.Equal("gone", read.Report);
    }

    [Fact]
    public void PruningSweepsUpWhatIsGoneAndLeavesWhatIsNot()
    {
        CliSessionFile.Write(Session(Gone));
        CliSessionFile.Write(Session(Environment.ProcessId));

        Assert.Equal(1, CliSessionFile.Prune());

        var left = Assert.Single(CliSessionFile.All());

        Assert.Equal(Environment.ProcessId, left.Pid);
    }

    /// <summary>
    /// A file another reader has open is written over once the reader lets it go, rather than the
    /// refusal reaching the app.
    /// </summary>
    /// <remarks>
    /// Held as a reading of the whole file holds it, sharing reads alone, which on Windows refuses
    /// the move with access denied, as the app's first ready file met in the suite on a Windows
    /// runner. The reader lets go at the first pause between tries, so the second try goes through
    /// whenever the first was refused, and on Linux and macOS the first goes through.
    /// </remarks>
    [Fact]
    public void AWriteOutlastsAReaderHoldingTheFile()
    {
        CliSessionFile.Write(Session(Environment.ProcessId));

        var held = new FileStream(
            CliSessionFile.PathFor(Environment.ProcessId), FileMode.Open, FileAccess.Read, FileShare.Read);
        var was = CliSessionFile.Pause;
        CliSessionFile.Pause = held.Dispose;

        try
        {
            CliSessionFile.Write(Session(Environment.ProcessId) with { Frame = 13 });
        }
        finally
        {
            CliSessionFile.Pause = was;
            held.Dispose();
        }

        Assert.Equal(13u, Assert.Single(CliSessionFile.All()).Frame);
    }

    [Fact]
    public void AFileThatIsNotOneOfTheseIsIgnoredRatherThanThrown()
    {
        Directory.CreateDirectory(_folder.Path);
        File.WriteAllText(Path.Combine(_folder.Path, "9999.json"), "{ not json at all");

        Assert.Empty(CliSessionFile.All());
    }

    [Fact]
    public void RemovingTakesTheFileBackOut()
    {
        CliSessionFile.Write(Session(Environment.ProcessId));
        CliSessionFile.Remove(Environment.ProcessId);

        Assert.Empty(CliSessionFile.All());
    }

    /// <summary>
    /// A process id that cannot be in use.
    /// </summary>
    /// <remarks>
    /// Past what any platform hands out, so nothing answers to it. Low numbers do not work for
    /// this. 0 is the kernel on Linux and the system idle process on Windows, and asking about
    /// either reports something that is, in the only sense that matters here, running.
    /// </remarks>
    private const int Gone = int.MaxValue - 1;

    /// <summary>One session, with everything filled in.</summary>
    private static CliSession Session(int pid) => new(
        Pid: pid,
        Port: 44444,
        Token: "0123456789abcdef",
        Project: "/somewhere/MyGame",
        Name: "MyGame",
        Title: "My Game",
        Started: DateTimeOffset.UtcNow,
        Abi: App.AbiVersion,
        Renderer: true,
        Editor: false,
        State: "ready",
        Frame: 12,
        Heartbeat: DateTimeOffset.UtcNow);
}

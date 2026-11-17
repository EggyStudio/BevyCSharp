using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers how the norm's rules read what git answers, which N 7.2 reads the commits' messages
/// through.
/// </summary>
/// <remarks>
/// git writes a message as the bytes it was made with, UTF-8 for every commit here. A reader that
/// names no encoding decodes them by the console's code page on Windows and as UTF-8 elsewhere, so
/// the three marks of a subject arrive whole on Linux and macOS whatever the reader names, and how
/// git is started is asked what it names besides.
/// </remarks>
public sealed class NormGitTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-norm-git-");

    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// A subject of the three marks, read through the norm's git, is the marks' own bytes, and git is
    /// started with both its streams read as UTF-8.
    /// </summary>
    [SkippableFact]
    public void ASubjectsMarksReadThroughGitAreTheirOwnBytes()
    {
        Skip.If(NormTests.Git("--version") is null, "needs git");

        var work = Directory.CreateDirectory(_folder.File("work")).FullName;
        var message = _folder.File("message.txt");
        File.WriteAllText(message, "‎ ‎ ‎\n\nA commit in the form.\n", new UTF8Encoding(false));
        Assert.NotNull(NormTests.Git("-C", work, "init", "-q"));
        Assert.NotNull(NormTests.Git("-C", work, "-c", "user.name=Tests", "-c", "user.email=tests@example.com",
            "-c", "commit.gpgsign=false", "commit", "-q", "--allow-empty", "-F", message));

        var subject = NormTests.Git("-C", work, "log", "-1", "--format=%s");
        Assert.NotNull(subject);
        byte[] marks = [0xe2, 0x80, 0x8e, 0x20, 0xe2, 0x80, 0x8e, 0x20, 0xe2, 0x80, 0x8e];
        Assert.Equal(marks, Encoding.UTF8.GetBytes(subject.TrimEnd('\n')));

        var start = NormTests.GitStart();
        Assert.Equal(Encoding.UTF8.WebName, start.StandardOutputEncoding?.WebName);
        Assert.Equal(Encoding.UTF8.WebName, start.StandardErrorEncoding?.WebName);
    }
}

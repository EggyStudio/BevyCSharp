using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/release-notes.py</c> writes the commits since the package the feed lists last was
/// built, read from the feed's index and that version's nuspec, and the minor's commits where the
/// feed answers nothing, lists no version of the minor or names a commit the checkout does not have,
/// leaving out each commit whose every file lies under <c>.github/</c>, which the package does not
/// carry.
/// </summary>
/// <remarks>
/// The script is copied into a history of its own, a minor begun and four commits after it, with the
/// package built at the second of those, and given a folder standing in for the feed, so the notes
/// it writes are known from the history alone and no request reaches nuget.org.
/// </remarks>
public sealed class ReleaseNotesTests : IDisposable
{
    private static readonly string Root = FindRoot();
    private readonly TestFolder _folder = new("bcs-release-notes-");
    private int _feeds;

    public void Dispose() => _folder.Dispose();

    [SkippableFact]
    public void TheNotesStartAfterTheCommitThePackageTheFeedListsLastWasBuiltFrom()
    {
        var python = Needs.Python();
        var (built, _) = History();
        var (notes, said, summary) = Notes(python, Feed(["0.4.9", "0.5.1", "0.5.2"], "0.5.2", built));

        Assert.Equal(["Fourth change.", "A workflow and the README change.", "Third change."], notes);
        Assert.Contains($"start after {built[..8]}, which 0.5.2", said);
        Assert.Contains(built[..8], summary);
    }

    [SkippableFact]
    public void TheNotesAreTheMinorsWhereTheFeedNamesNoCommitOfThisHistory()
    {
        var python = Needs.Python();
        var (built, minor) = History();
        string[] whole = ["Fourth change.", "A workflow and the README change.", "Third change.", "Second change.", "First change."];

        // No version of the minor, only the one before.
        var (notes, said, _) = Notes(python, Feed(["0.4.8", "0.4.9"], "0.4.9", built));
        Assert.Equal(whole, notes);
        Assert.Contains($"from {minor[..8]}, where build/version.txt last changed, since nuget.org lists no version of 0.5", said);

        // A commit this history does not have, as a package built elsewhere names.
        (notes, said, _) = Notes(python, Feed(["0.5.2"], "0.5.2", new string('7', 40)));
        Assert.Equal(whole, notes);
        Assert.Contains("which this checkout does not have", said);

        // No answer at all, from a feed with nothing where the package's folder should be.
        (notes, said, _) = Notes(python, Path.Combine(_folder.File("no-feed"), "bevycsharp"));
        Assert.Equal(whole, notes);
        Assert.Contains("nuget.org answered nothing for the package's versions", said);
    }

    /// <summary>
    /// A history holding the script, the minor begun and four commits after it, each message in
    /// COMMITS.md's form, and the commits the package was built at and the minor began at. After the
    /// third, a review's record touches <c>.github/</c> alone, which the notes leave out, and a commit
    /// touches a workflow and the README, which they keep.
    /// </summary>
    private (string Built, string Minor) History()
    {
        var work = Directory.CreateDirectory(_folder.File("work")).FullName;
        Directory.CreateDirectory(Path.Combine(work, "build"));
        File.Copy(Path.Combine(Root, "build", "release-notes.py"), Path.Combine(work, "build", "release-notes.py"));
        Git(work, "init", "-q");

        File.WriteAllText(Path.Combine(work, "build", "version.txt"), "0.5\n");
        var minor = Commit(work, "The minor begins.");
        string[] changes = ["First change.", "Second change.", "Third change.", "Fourth change."];
        var built = string.Empty;
        foreach (var change in changes)
        {
            File.AppendAllText(Path.Combine(work, "changes.txt"), change + "\n");
            var commit = Commit(work, change);
            if (change == "Second change.") built = commit;
            if (change != "Third change.") continue;

            Directory.CreateDirectory(Path.Combine(work, ".github", "workflows"));
            File.WriteAllText(Path.Combine(work, ".github", "REVIEW.md"), "The review.\n");
            Commit(work, "The review records the third change.");
            File.WriteAllText(Path.Combine(work, ".github", "workflows", "pack.yml"), "name: pack\n");
            File.WriteAllText(Path.Combine(work, "README.md"), "The README.\n");
            Commit(work, "A workflow and the README change.");
        }

        return (built, minor);
    }

    /// <summary>
    /// A feed's folder for the package, a new one each call, holding its index and the nuspec of one
    /// version naming a commit, laid out as nuget.org's flat container is.
    /// </summary>
    private string Feed(string[] versions, string named, string commit)
    {
        var feed = Path.Combine(_folder.File($"feed-{++_feeds}"), "bevycsharp");
        Directory.CreateDirectory(Path.Combine(feed, named));
        File.WriteAllText(Path.Combine(feed, "index.json"), "{\"versions\":[" + string.Join(",", versions.Select(version => $"\"{version}\"")) + "]}");
        File.WriteAllText(Path.Combine(feed, named, "bevycsharp.nuspec"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\"><metadata>"
            + $"<id>BevyCSharp</id><version>{named}</version><repository type=\"git\" url=\"https://github.com/EggyStudio/BevyCSharp\" commit=\"{commit}\" />"
            + "</metadata></package>");
        return feed;
    }

    /// <summary>The notes the script writes in the history, what it said and what it put in the summary.</summary>
    private (string[] Notes, string Said, string Summary) Notes(string python, string feed)
    {
        var work = _folder.File("work");
        var notes = _folder.File("notes.txt");
        var summary = _folder.File("summary.md");
        File.WriteAllText(summary, string.Empty);

        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = work,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(Path.Combine(work, "build", "release-notes.py"));
        start.ArgumentList.Add(notes);
        start.Environment["RELEASE_NOTES_FEED"] = new Uri(feed).AbsoluteUri;
        start.Environment["GITHUB_STEP_SUMMARY"] = summary;

        using var script = Process.Start(start)!;
        var said = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(60_000), "the script ended");
        Assert.True(script.ExitCode == 0, $"the script ended with {script.ExitCode}: {errors.Result}");
        return (File.ReadAllLines(notes), said.Result, File.ReadAllText(summary));
    }

    /// <summary>A commit of everything in the history, its message in COMMITS.md's form, and its hash.</summary>
    private string Commit(string work, string sentence)
    {
        var message = _folder.File("message.txt");
        File.WriteAllText(message, "‎ ‎ ‎\n\n" + sentence + "\n", new UTF8Encoding(false));
        Git(work, "add", "-A");
        Git(work, "-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-q", "-F", message);
        return Git(work, "rev-parse", "HEAD").Trim();
    }

    private static string Git(string work, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = work, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var git = Process.Start(start)!;
        var output = git.StandardOutput.ReadToEndAsync();
        var errors = git.StandardError.ReadToEndAsync();
        Assert.True(git.WaitForExit(60_000), $"git {string.Join(' ', arguments)} ended");
        Assert.True(git.ExitCode == 0, $"git {string.Join(' ', arguments)} ended with {git.ExitCode}: {errors.Result}");
        return output.Result;
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The shell scripts a workflow runs on Windows or macOS, and those they call, use only what GNU's
/// tools and the BSD ones macOS has both read, and what the bash 3.2 macOS ships reads, as N 6.6 of
/// NORM.md has it.
/// </summary>
/// <remarks>
/// <para>
/// A script written on Linux passes every Linux run and fails the first on macOS on a form only
/// GNU's tools read, as 3DEngine's pack did on <c>sed -i</c>, whose BSD reading took the first file's
/// name as its script. Windows runs the scripts in Git's bash, whose tools are GNU's, so macOS is
/// the system that tells, and the test reads for it on every system.
/// </para>
/// <para>
/// A job runs elsewhere than Linux when its <c>runs-on</c> names anything but an Ubuntu label, which
/// takes in a job whose system comes from a matrix of inputs, since the inputs decide that and not
/// the job. A job that calls another workflow names no script of its own.
/// </para>
/// </remarks>
public sealed class ScriptTests
{
    private static readonly string Root = FindRoot();

    // Each form, how it is found and what is written in its place.
    private static readonly (string Form, Regex Pattern, string Instead)[] GnuOnly =
    [
        ("sed -i", new(@"\bsed\s+(?:-\w+\s+)*-\w*i\b"), "sed writing a file beside it, moved over it"),
        ("grep -P", new(@"\bgrep\s+(?:-\w+\s+)*-\w*P"), "grep -E, or Python"),
        ("readarray", new(@"\b(?:readarray|mapfile)\b"), "a while read loop"),
        ("date -d", new(@"\bdate\s+(?:\S+\s+)*?(?:-d|--date)\b"), "Python"),
        ("stat -c", new(@"\bstat\s+(?:-\w+\s+)*(?:-c|--format)\b"), "wc -c, or Python"),
        ("sha256sum", new(@"\b(?:sha256sum|sha1sum|md5sum)\b"), "Python's hashlib"),
        ("${x,,}", new(@"\$\{[A-Za-z_]\w*(?:,,?|\^\^?)\}"), "tr"),
    ];

    [Fact]
    public void TheScriptsRunOnWindowsAndMacOSUseOnlyWhatBothSystemsToolsRead()
    {
        var scripts = ScriptsRunElsewhere();
        Assert.Contains("build/build-native.sh", scripts);

        var found = new List<string>();
        foreach (var script in scripts)
        {
            var lines = File.ReadAllLines(Path.Combine(Root, script));
            for (var i = 0; i < lines.Length; i++)
                foreach (var (form, pattern, instead) in GnuOnly)
                    if (pattern.IsMatch(Code(lines[i])))
                        found.Add($"{script}:{i + 1} has {form}, where {instead} reads on both");
        }

        Assert.True(found.Count == 0, "N 6.6: scripts run on macOS use what only GNU's tools or bash 4 read:\n  " + string.Join("\n  ", found));
    }

    [Theory]
    [InlineData("sed -i \"s/PACKED_VERSION/$version/\" template.json", "sed -i")]
    [InlineData("sed -Ei 's/a/b/' file", "sed -i")]
    [InlineData("floor=$(objdump -T \"$BUILT\" | grep -oP 'GLIBC_\\K[0-9]+')", "grep -P")]
    [InlineData("readarray -t lines < file", "readarray")]
    [InlineData("then=$(date -d yesterday +%s)", "date -d")]
    [InlineData("size=$(stat -c %s file)", "stat -c")]
    [InlineData("sha256sum package.nupkg", "sha256sum")]
    [InlineData("echo \"${profile,,}\"", "${x,,}")]
    [InlineData("sed -e 's/a/b/' file > file.new && mv file.new file", null)]
    [InlineData("floor=$(objdump -T \"$BUILT\" | grep -oE 'GLIBC_[0-9]+' | sed 's/^GLIBC_//')", null)]
    [InlineData("now=$(date +%s)", null)]
    [InlineData("size=$(wc -c < file)", null)]
    [InlineData("echo \"${profile}\" \"${#features[@]}\"", null)]
    [InlineData("# sed -i is left out, which macOS reads otherwise", null)]
    public void EachFormIsFoundWhereALineHasItAndNowhereElse(string line, string? form)
    {
        string[] expected = form is null ? [] : [form];
        Assert.Equal(expected, GnuOnly.Where(entry => entry.Pattern.IsMatch(Code(line))).Select(entry => entry.Form));
    }

    // The shell scripts that the jobs which run on Windows or macOS name, and those each of them
    // names in turn, by path from the root.
    private static SortedSet<string> ScriptsRunElsewhere()
    {
        var pending = new Stack<string>();
        foreach (var workflow in Directory.EnumerateFiles(Path.Combine(Root, ".github", "workflows"), "*.yml"))
        {
            var jobs = Regex.Split(File.ReadAllText(workflow), @"^  [\w-]+:\n", RegexOptions.Multiline);
            foreach (var job in jobs.Where(job => Regex.IsMatch(job, @"^\s+runs-on:\s*(?!ubuntu-)\S", RegexOptions.Multiline)))
                foreach (var script in Called(job))
                    pending.Push(script);
        }

        var scripts = new SortedSet<string>(StringComparer.Ordinal);
        while (pending.TryPop(out var script))
            if (scripts.Add(script))
                foreach (var line in File.ReadLines(Path.Combine(Root, script)))
                    foreach (var called in Called(Code(line)))
                        pending.Push(called);
        return scripts;
    }

    // The scripts a text names that are in the checkout, which leaves out the local settings a
    // script reads where a machine has them.
    private static IEnumerable<string> Called(string text) =>
        Regex.Matches(text, @"(?<![\w/.])build/[\w.-]+\.sh\b")
            .Select(match => match.Value)
            .Where(script => File.Exists(Path.Combine(Root, script)));

    // A line without its comment, so a script that names a form to say it is left out is not taken
    // as using it. A # inside ${...} counts a length and begins no comment.
    private static string Code(string line) => Regex.Replace(line, @"(^|\s)#.*", "");

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

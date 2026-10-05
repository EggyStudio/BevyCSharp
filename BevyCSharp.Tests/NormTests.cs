using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Checks the rules of <c>.github/NORM.md</c> that a machine can check, a test a rule, named for it
/// as <c>N_1_3</c> is for N 1.3.
/// </summary>
/// <remarks>
/// <para>
/// A rule that code written before it does not keep has its list, <c>build/norm/&lt;number&gt;.txt</c>,
/// a place a line. A test fails for a place that breaks the rule and is not on the list, and for a
/// line whose place keeps the rule now or is gone, so a list only gets shorter and a place comes off
/// it in the batch that mends it.
/// </para>
/// <para>
/// The sources are the files git knows, tracked or new and not ignored, so what the build writes
/// is never counted, and a checkout without git reads the folders with <c>bin</c> and <c>obj</c>
/// left out.
/// </para>
/// </remarks>
public sealed class NormTests
{
    private static readonly string Root = FindRoot();

    /// <summary>The last commit before the one that added these tests, which N 7.2 reads from.</summary>
    private const string MessagesFrom = "3e694f067b5a74185620b9a09bfe122df6b0072f";

    /// <summary>The library's namespaces, as Annex B gives them.</summary>
    private static readonly string[] Namespaces = ["Bevy", "Bevy.Reflected", "Bevy.Interop", "Bevy.Physics"];

    [Fact]
    public void N_1_1()
    {
        var found = typeof(App).Assembly.GetExportedTypes()
            .Select(type => type.Namespace ?? "(no namespace)")
            .Where(name => !Namespaces.Contains(name));
        Hold("1.1", found, "a namespace of the library that Annex B does not list");
    }

    [Fact]
    public void N_1_2()
    {
        var found = new List<string>();
        foreach (var file in Sources("BevyCSharp/", ".cs"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(Full(file)));
            foreach (var type in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (type is not (BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)) continue;
                if (type.Parent is TypeDeclarationSyntax) continue;
                if (!type.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;

                var declared = type switch
                {
                    BaseTypeDeclarationSyntax declaration => declaration.Identifier.Text,
                    DelegateDeclarationSyntax delegated => delegated.Identifier.Text,
                    _ => "",
                };
                if (name != declared && !name.StartsWith(declared + ".", StringComparison.Ordinal))
                    found.Add($"{file} {declared}");
            }
        }

        Hold("1.2", found, "a public type in a file not named for it");
    }

    [Fact]
    public void N_1_3()
    {
        var found = Sources("BevyCSharp/", ".cs")
            .Concat(Sources("BevyCSharp.Tests/", ".cs"))
            .Concat(Sources("BevyCSharp.Examples/", ".cs"))
            .Concat(Sources("native/bevy_csharp/src/", ".rs"))
            .Where(file => !file.EndsWith(".g.cs", StringComparison.Ordinal))
            .Where(file => File.ReadLines(Full(file)).Count() > 800);
        Hold("1.3", found, "a file of more than 800 lines");
    }

    [Fact]
    public void N_1_4()
    {
        var areas = LibraryFolders().Select(folder => folder["BevyCSharp/".Length..]).ToHashSet(StringComparer.Ordinal);
        // What the tests share, a harness or a helper with no test of its own, is at the project's
        // root, so only a file holding a test is placed by the area it tests. A test of what is no
        // area of the library, in a folder named for what it tests, and this class at the root are
        // found here too, and listed with that reason.
        var found = Sources("BevyCSharp.Tests/", ".cs")
            .Where(file => !file.StartsWith("BevyCSharp.Tests/assets/", StringComparison.Ordinal))
            .Where(file => Regex.IsMatch(File.ReadAllText(Full(file)), @"^\s*\[(Skippable)?(Fact|Theory)\b", RegexOptions.Multiline))
            .Where(file => file.Split('/') is var parts && (parts.Length < 3 || !areas.Contains(parts[1])));
        Hold("1.4", found, "a test outside the folder of the area it tests");
    }

    [Fact]
    public void N_1_5()
    {
        var rows = File.ReadLines(Full("AGENTS.md"))
            .Select(line => Regex.Match(line, @"^\| `([^`]+)` \|"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value.TrimEnd('/'))
            .ToList();

        // Each top folder of the repository, the hidden ones apart, and each top folder of the
        // library in place of the library's own. A project is in a top folder, so the folder's row
        // is the project's.
        var folders = Files()
            .Where(file => file.Contains('/') && !file.StartsWith('.') && !file.StartsWith("BevyCSharp/", StringComparison.Ordinal))
            .Select(file => file[..file.IndexOf('/')])
            .Concat(LibraryFolders())
            .Distinct(StringComparer.Ordinal);

        // A folder is named by a row of its own or by a row for a folder within it.
        var found = folders.Where(folder => !rows.Any(row => row == folder || row.StartsWith(folder + "/", StringComparison.Ordinal)));
        Hold("1.5", found, "a top folder of the repository or of the library with no row in AGENTS.md's table of areas");
    }

    [Fact]
    public void N_3_3()
    {
        string[] clocks = ["Thread.Sleep", "Task.Delay", "Stopwatch", "DateTime.Now", "DateTime.UtcNow", "Environment.TickCount", "SpinWait"];
        Hold("3.3", Uses("BevyCSharp.Tests/", clocks), "a test that waits on or reads the machine's clock");
    }

    [Fact]
    public void N_3_4()
    {
        string[] temporary = ["Path.GetTempPath", "GetTempFileName", "CreateTempSubdirectory", "Directory.Delete"];
        Hold("3.4", Uses("BevyCSharp.Tests/", temporary), "a temporary folder made or removed without the one helper");
    }

    [Fact]
    public void N_4_1()
    {
        string[] prose = [".md", ".cs", ".rs", ".slang", ".py", ".sh", ".yml", ".toml"];
        var found = Sources("", prose)
            .Where(file => !file.StartsWith("BevyCSharp.Examples/bevy-assets/", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(Full(file)).Any(c => c is '\u2014' or '\u2013'));
        Hold("4.1", found, "a file with a dash STYLE.md forbids");
    }

    [Fact]
    public void N_4_2()
    {
        var readme = File.ReadAllText(Full("README.md"));
        var found = Sources("docs/", ".md")
            .Where(page => !readme.Contains("/blob/main/" + page, StringComparison.Ordinal));
        if (readme.Split('\n').Length - 1 > 320) found = found.Append("README.md");
        Hold("4.2", found, "a page of docs/ the README does not link, or a README over 320 lines");
    }

    [Fact]
    public void N_4_5()
    {
        var found = new List<string>();
        foreach (var capture in Sources(".github/assets/examples/", ".webp"))
        {
            var (width, height) = WebpSize(File.ReadAllBytes(Full(capture)));
            if ((width, height) != (1280, 720)) found.Add($"{Path.GetFileNameWithoutExtension(capture)} {width}x{height}");
        }

        // Each picture in the README's gallery opens the program that drew it.
        var readme = File.ReadAllText(Full("README.md"));
        foreach (Match picture in Regex.Matches(readme, @"<td>(?:<a href=""([^""]*)"">)?<img src=""[^""]*/examples/([^""/]+)\.webp"""))
        {
            var (link, name) = (picture.Groups[1].Value, picture.Groups[2].Value);
            if (!link.EndsWith("/" + name + ".cs", StringComparison.Ordinal)) found.Add($"README.md {name}");
        }

        Hold("4.5", found, "a capture not at Bevy's window of 1280 by 720, or a README picture that does not open its example");
    }

    [SkippableFact]
    public void N_6_4()
    {
        // Whose work the package carries, every crate of the bridge's lock named with its version,
        // held on the file the package is packed from, which needs no package to read.
        var crates = LockedCrates();
        NamesEvery(File.ReadAllText(Full("THIRD-PARTY-NOTICES.md")), crates, "THIRD-PARTY-NOTICES.md");

        var package = Environment.GetEnvironmentVariable("BCS_PACKAGE");
        if (string.IsNullOrEmpty(package))
        {
            var packed = Path.Combine(Root, "build", "package");
            package = Directory.Exists(packed)
                ? Directory.EnumerateFiles(packed, "BevyCSharp.*.nupkg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                : null;
        }

        Skip.If(package is null, "N 6.4 has no package to open; BCS_PACKAGE names one, and the pack job sets it");

        using var zip = ZipFile.OpenRead(package!);
        var held = zip.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.Ordinal);
        var wanted = new List<string>
        {
            "lib/net10.0/BevyCSharp.dll",
            "analyzers/dotnet/cs/BevyCSharp.Generator.dll",
            "build/BevyCSharp.props",
            "build/BevyCSharp.targets",
            "README.md",
            "THIRD-PARTY-NOTICES.md",
        };

        // The natives staged for the pack, a system a folder, each in its runtime's folder.
        var staged = Path.Combine(Root, "build", "artifacts");
        if (Directory.Exists(staged))
        {
            foreach (var native in Directory.EnumerateFiles(staged, "*", SearchOption.AllDirectories))
            {
                var rid = Path.GetFileName(Path.GetDirectoryName(native)!);
                wanted.Add($"runtimes/{rid}/native/{Path.GetFileName(native)}");
            }
        }

        var missing = wanted.Where(path => !held.Contains(path)).ToList();
        Assert.True(missing.Count == 0, $"N 6.4: {Path.GetFileName(package)} does not hold {string.Join(", ", missing)}");

        using var notices = new StreamReader(zip.GetEntry("THIRD-PARTY-NOTICES.md")!.Open());
        NamesEvery(notices.ReadToEnd(), crates, $"the THIRD-PARTY-NOTICES.md of {Path.GetFileName(package)}");
    }

    /// <summary>Every crate of native/Cargo.lock, the bridge's own apart, as its name and version.</summary>
    private static List<string> LockedCrates() =>
        Regex.Matches(File.ReadAllText(Full("native/Cargo.lock")), @"^\[\[package\]\]\nname = ""([^""]+)""\nversion = ""([^""]+)""", RegexOptions.Multiline)
            .Where(match => match.Groups[1].Value != "bevy_csharp")
            .Select(match => $"{match.Groups[1].Value} {match.Groups[2].Value}")
            .ToList();

    /// <summary>Fails for each crate the notices have no row for, by its name and version.</summary>
    private static void NamesEvery(string notices, List<string> crates, string what)
    {
        var unnamed = crates
            .Where(crate => crate.Split(' ') is var parts && !notices.Contains($"| {parts[0]} | {parts[1]} |", StringComparison.Ordinal))
            .ToList();
        Assert.True(unnamed.Count == 0, $"N 6.4: {what} names no {string.Join(", ", unnamed)}, which build/third-party-notices.py writes in");
    }

    [SkippableFact]
    public void N_7_2()
    {
        var log = Git("log", "--format=%H%x1f%s%x1f%b%x1e", $"{MessagesFrom}..HEAD");
        Skip.If(log is null, "N 7.2 reads the commits with git, which this checkout does not have");

        var found = new List<string>();
        foreach (var entry in log!.Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Trim('\n').Split('\u001f');
            if (parts.Length < 3) continue;
            var (hash, subject, body) = (parts[0][..7], parts[1], parts[2].Trim());
            var sentence = body.Length > 0 && !body.Contains('\n') && body.EndsWith('.') && !Regex.IsMatch(body[..^1], @"[.!?] [A-Z]");
            if (subject != "‎ ‎ ‎" || !sentence) found.Add(hash);
        }

        Assert.True(found.Count == 0, $"N 7.2: these commits do not have the form COMMITS.md gives, three marks and one sentence: {string.Join(", ", found)}");
    }

    [Fact]
    public void B_3()
    {
        var found = new List<string>();
        foreach (var file in Sources("native/bevy_csharp/src/", ".rs"))
        {
            var text = File.ReadAllText(Full(file));
            // An entry point is exported by its name, and a test's own callback is not one.
            foreach (Match entry in Regex.Matches(text, @"#\[unsafe\(no_mangle\)\]\s*pub (?:unsafe )?extern ""C"" fn (\w+)"))
            {
                var open = text.IndexOf('{', entry.Index);
                if (open < 0) continue;
                var depth = 0;
                var close = open;
                for (; close < text.Length; close++)
                {
                    if (text[close] == '{') depth++;
                    else if (text[close] == '}' && --depth == 0) break;
                }

                if (!Regex.IsMatch(text[open..Math.Min(close + 1, text.Length)], @"\bguard(_with)?\("))
                    found.Add($"{file} {entry.Groups[1].Value}");
            }
        }

        Hold("B3", found, "an entry point of the bridge that does not run under the guard");
    }

    /// <summary>
    /// Every test here is named for a rule NORM.md has, every rule its table calls listed for this
    /// engine, or checked by these tests, has its test, and every list has its test.
    /// </summary>
    [Fact]
    public void NormAndItsTestsAgree()
    {
        var norm = File.ReadAllText(Full(".github/NORM.md"));
        var rules = Regex.Matches(norm, @"\*\*(N \d+\.\d+|B \d+) ").Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var tests = typeof(NormTests).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(FactAttribute), inherit: true).Length > 0)
            .Select(method => method.Name)
            .Where(name => name != nameof(NormAndItsTestsAgree))
            .ToHashSet(StringComparer.Ordinal);

        static string RuleOf(string test) => test.Replace("N_", "N ").Replace("B_", "B ").Replace('_', '.');

        var problems = new List<string>();
        problems.AddRange(tests.Where(test => !rules.Contains(RuleOf(test))).Select(test => $"{test} is named for no rule in NORM.md"));

        foreach (Match row in Regex.Matches(norm, @"^\| (N \d+\.\d+|B \d+) \|[^|\n]*\|([^|\n]*)\|", RegexOptions.Multiline))
        {
            var (rule, cell) = (row.Groups[1].Value, row.Groups[2].Value.Trim());
            var ours = cell.StartsWith("listed", StringComparison.Ordinal)
                || (cell.StartsWith("checked", StringComparison.Ordinal) && cell.Contains("NormTests", StringComparison.Ordinal));
            if (ours && !tests.Contains(rule.Replace(' ', '_').Replace('.', '_'))) problems.Add($"{rule} is {cell} in NORM.md and NormTests has no test for it");
        }

        var lists = Path.Combine(Root, "build", "norm");
        if (Directory.Exists(lists))
        {
            foreach (var list in Directory.EnumerateFiles(lists, "*.txt"))
            {
                var number = Path.GetFileNameWithoutExtension(list);
                var test = number.StartsWith('B') ? "B_" + number[1..] : "N_" + number.Replace('.', '_');
                if (!tests.Contains(test)) problems.Add($"build/norm/{Path.GetFileName(list)} is the list of no test");
            }
        }

        Assert.True(problems.Count == 0, "NORM.md and NormTests disagree: " + string.Join("; ", problems));
    }

    // -- What the tests share

    /// <summary>
    /// Fails for a place that breaks the rule and is not on its list, and for a listed place that
    /// keeps the rule now or is gone, each named, the message beginning with the rule's number.
    /// </summary>
    private static void Hold(string number, IEnumerable<string> breaking, string what)
    {
        var found = breaking.ToHashSet(StringComparer.Ordinal);
        var listed = ReadList(number);
        var unlisted = found.Where(place => !listed.Contains(place)).Order(StringComparer.Ordinal).ToList();
        var mended = listed.Where(place => !found.Contains(place)).Order(StringComparer.Ordinal).ToList();

        var rule = number.StartsWith('B') ? "B " + number[1..] : "N " + number;
        var message = $"{rule}: ";
        if (unlisted.Count > 0) message += $"breaking the rule, {what}, and not on build/norm/{number}.txt:\n  {string.Join("\n  ", unlisted)}\n";
        if (mended.Count > 0) message += $"on build/norm/{number}.txt and keeping the rule now, or gone, so off the list they come:\n  {string.Join("\n  ", mended)}\n";
        Assert.True(unlisted.Count == 0 && mended.Count == 0, message);
    }

    /// <summary>A rule's list, a place a line, the place before a tab where a reason follows.</summary>
    private static HashSet<string> ReadList(string number)
    {
        var path = Path.Combine(Root, "build", "norm", number + ".txt");
        if (!File.Exists(path)) return [];
        return File.ReadLines(path)
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('\t')[0])
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The places in a folder's sources that use any of the words, as "file word".</summary>
    private static IEnumerable<string> Uses(string folder, string[] words) =>
        Sources(folder, ".cs")
            .Where(file => !file.EndsWith("/NormTests.cs", StringComparison.Ordinal))
            .SelectMany(file =>
            {
                var text = File.ReadAllText(Full(file));
                return words.Where(word => text.Contains(word, StringComparison.Ordinal)).Select(word => $"{file} {word}");
            });

    /// <summary>The library's top folders, as paths from the checkout.</summary>
    private static IEnumerable<string> LibraryFolders() =>
        Files()
            .Where(file => file.StartsWith("BevyCSharp/", StringComparison.Ordinal))
            .Select(file => file.Split('/'))
            .Where(parts => parts.Length > 2)
            .Select(parts => $"BevyCSharp/{parts[1]}")
            .Distinct(StringComparer.Ordinal);

    /// <summary>The files under a folder with any of the endings, as paths from the checkout.</summary>
    private static IEnumerable<string> Sources(string folder, params string[] endings) =>
        Files().Where(file => file.StartsWith(folder, StringComparison.Ordinal)
            && endings.Any(ending => file.EndsWith(ending, StringComparison.Ordinal)));

    private static string[]? _files;

    /// <summary>Every file git knows, tracked or new and not ignored, or the folders' own walk without git.</summary>
    private static string[] Files() => _files ??=
        Git("ls-files", "--cached", "--others", "--exclude-standard") is { } listed
            ? listed.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(file => File.Exists(Full(file))).ToArray()
            : Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
                .Where(file => !Regex.IsMatch(file, @"(^|/)(bin|obj|\.git)/"))
                .ToArray();

    private static string Full(string relative) => Path.Combine(Root, relative);

    /// <summary>What git answers, or nothing where git is not there or this is no checkout.</summary>
    private static string? Git(params string[] arguments)
    {
        try
        {
            var start = new ProcessStartInfo("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var git = Process.Start(start);
            if (git is null) return null;
            var output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            return git.ExitCode == 0 ? output : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>A WebP picture's size, from its first chunk, lossy, lossless or extended.</summary>
    private static (int Width, int Height) WebpSize(byte[] bytes)
    {
        var chunk = System.Text.Encoding.ASCII.GetString(bytes, 12, 4);
        return chunk switch
        {
            "VP8 " => ((bytes[26] | (bytes[27] << 8)) & 0x3fff, (bytes[28] | (bytes[29] << 8)) & 0x3fff),
            "VP8L" => (((bytes[21] | (bytes[22] << 8)) & 0x3fff) + 1, (((bytes[22] >> 6) | (bytes[23] << 2) | ((bytes[24] & 0xf) << 10)) & 0x3fff) + 1),
            "VP8X" => (1 + (bytes[24] | (bytes[25] << 8) | (bytes[26] << 16)), 1 + (bytes[27] | (bytes[28] << 8) | (bytes[29] << 16))),
            _ => (0, 0),
        };
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

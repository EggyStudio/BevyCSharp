using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
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
            var tree = CSharpSyntaxTree.ParseText(Text(file));
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
            .Where(file => Regex.IsMatch(Text(file), @"^\s*\[(Skippable)?(Fact|Theory)\b", RegexOptions.Multiline))
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
    public void N_2_8()
    {
        // The list Annex B names, the rows of BUILDING.md's tables under its section of packages.
        var building = Text(".github/BUILDING.md");
        var start = building.IndexOf("\n## Packages\n", StringComparison.Ordinal);
        Assert.True(start >= 0, "N 2.8: BUILDING.md has no section of packages, which Annex B names as the list");
        var end = building.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        var named = Regex.Matches(building[start..(end < 0 ? building.Length : end)], @"^\| `([^`]+)` \|", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        // What the package carries, the library and its generator, and the bridge's crates, those of
        // a platform's own table among them.
        var referenced = new[] { "BevyCSharp/BevyCSharp.csproj", "BevyCSharp.Generator/BevyCSharp.Generator.csproj" }
            .SelectMany(project => Regex.Matches(Text(project), @"<PackageReference Include=""([^""]+)""").Select(match => match.Groups[1].Value))
            .Concat(Crates("native/bevy_csharp/Cargo.toml"))
            .ToHashSet(StringComparer.Ordinal);

        var found = referenced.Where(name => !named.Contains(name)).Select(name => $"{name} is referenced and not listed")
            .Concat(named.Where(name => !referenced.Contains(name)).Select(name => $"{name} is listed and referenced nowhere"));
        Hold("2.8", found, "a package the project files and BUILDING.md's list of packages disagree on");
    }

    /// <summary>The crates a manifest's tables of dependencies name, a platform's own among them.</summary>
    private static IEnumerable<string> Crates(string manifest)
    {
        var inDependencies = false;
        foreach (var line in File.ReadLines(Full(manifest)))
        {
            if (line.StartsWith('[')) inDependencies = line.TrimEnd().EndsWith("dependencies]", StringComparison.Ordinal);
            else if (inDependencies && Regex.Match(line, @"^([A-Za-z0-9_-]+)\s*=") is { Success: true } crate) yield return crate.Groups[1].Value;
        }
    }

    [Fact]
    public void N_2_10()
    {
        var handed = HandedToNativeCode();
        Assert.True(new[] { "Bevy.App+RegisteredSystem.Trampoline", "Bevy.AssetFiles.ReadCarried", "Bevy.ImGuiInput.OnImeData" }.All(handed.Select(Name).Contains),
            "N 2.10 finds the methods marked to be called from native code, a system's trampoline, the reader of carried files and ImGui's input method among them, and found "
            + string.Join(", ", handed.Select(Name)));

        var found = handed
            .Select(method => (method, call: UnguardedCall(method)))
            .Where(entry => entry.call is not null)
            .Select(entry => $"{Name(entry.method)} calls {entry.call} outside a catch of every exception");
        Hold("2.10", found, "a method native code calls");
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
        // The one helper, TestFolder, makes and removes them, and is the place that may.
        string[] temporary = ["Path.GetTempPath", "GetTempFileName", "CreateTempSubdirectory", "Directory.Delete"];
        var found = Uses("BevyCSharp.Tests/", temporary).Where(place => !place.StartsWith("BevyCSharp.Tests/TestFolder.cs ", StringComparison.Ordinal));
        Hold("3.4", found, "a temporary folder made or removed without the one helper");
    }

    [Fact]
    public void N_4_1()
    {
        string[] prose = [".md", ".cs", ".rs", ".slang", ".py", ".sh", ".yml", ".toml"];
        // Bevy's assets and the vendored weather carry their authors' own words, as the rule's
        // leaving out of the followed engine's has it.
        var found = Sources("", prose)
            .Where(file => !file.StartsWith("BevyCSharp.Examples/bevy-assets/", StringComparison.Ordinal))
            .Where(file => !file.StartsWith("native/bevy_weather/", StringComparison.Ordinal))
            .Where(file => Text(file).Any(c => c is '\u2014' or '\u2013'));
        Hold("4.1", found, "a file with a dash STYLE.md forbids");
    }

    [Fact]
    public void N_4_2()
    {
        var readme = Text("README.md");
        var found = Sources("docs/", ".md")
            .Where(page => !readme.Contains("/blob/main/" + page, StringComparison.Ordinal));
        if (ProseLines(readme) > 320) found = found.Append("README.md");
        Hold("4.2", found, "a page of docs/ the README does not link, or a README over 320 lines of prose");
    }

    // The lines a reader reads, which leaves out each row of a table, an HTML one's as the gallery
    // writes them and a Markdown one's, since a row is taken in at a glance.
    private static int ProseLines(string text) =>
        text.Split('\n')[..^1].Count(line => !line.TrimStart().StartsWith("<tr>", StringComparison.Ordinal) && !line.TrimStart().StartsWith('|'));

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
        var readme = Text("README.md");
        foreach (Match picture in Regex.Matches(readme, @"<td>(?:<a href=""([^""]*)"">)?<img src=""[^""]*/examples/([^""/]+)\.webp"""))
        {
            var (link, name) = (picture.Groups[1].Value, picture.Groups[2].Value);
            if (!link.EndsWith("/" + name + ".cs", StringComparison.Ordinal)) found.Add($"README.md {name}");
        }

        Hold("4.5", found, "a capture not at Bevy's window of 1280 by 720, or a README picture that does not open its example");
    }

    [Fact]
    public void N_4_7()
    {
        // The pages a game's author reads, none of whom has an owner or a session to follow. Case
        // is ignored, since a sentence can begin with any of the words.
        string[] deciders = ["the owner", "the reviewing session", "REVIEW.md"];
        var found = new[] { "README.md", "CHEATSHEET.md" }.Concat(Sources("docs/", ".md"))
            .SelectMany(page =>
            {
                var text = Text(page);
                return deciders.Where(word => text.Contains(word, StringComparison.OrdinalIgnoreCase)).Select(word => $"{page} {word}");
            });
        Hold("4.7", found, "a page a game's author reads naming who decided rather than why");
    }

    [SkippableFact]
    public void N_6_4()
    {
        var package = Package();
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
    }

    [Fact]
    public void N_6_5()
    {
        // Whose work the package carries, every crate of the bridge's lock named with its version,
        // held on the file the package is packed from, which needs no package to read, and then on
        // the copy in the package where there is one.
        var crates = LockedCrates();
        NamesEvery(Text("THIRD-PARTY-NOTICES.md"), crates, "THIRD-PARTY-NOTICES.md");

        if (Package() is not { } package) return;
        using var zip = ZipFile.OpenRead(package);
        var notices = zip.GetEntry("THIRD-PARTY-NOTICES.md");
        Assert.True(notices is not null, $"N 6.5: {Path.GetFileName(package)} carries no THIRD-PARTY-NOTICES.md at its root");
        using var reader = new StreamReader(notices!.Open());
        NamesEvery(reader.ReadToEnd(), crates, $"the THIRD-PARTY-NOTICES.md of {Path.GetFileName(package)}");
    }

    /// <summary>
    /// The package to open, the one <c>BCS_PACKAGE</c> names or else the newest packed into
    /// build/package, or none.
    /// </summary>
    /// <remarks>
    /// A path the variable gives relative is taken from the repository's root, as the norm's other
    /// files are. A step names it from the checkout and the test runs from its own folder,
    /// bin/Release/net10.0, so read from there it named nothing, and the pack job of 421d4e1 failed
    /// opening a folder that is not there. A variable naming no file fails here and says so, with
    /// the path it was read as, rather than skipping as an unset one does, since a job that sets it
    /// means a package to be opened.
    /// </remarks>
    private static string? Package()
    {
        var named = Environment.GetEnvironmentVariable("BCS_PACKAGE");
        if (!string.IsNullOrEmpty(named))
        {
            var package = Path.IsPathRooted(named) ? named : Path.GetFullPath(Path.Combine(Root, named));
            Assert.True(File.Exists(package), $"BCS_PACKAGE names {named}, read as {package}, and no file is there");
            return package;
        }

        var packed = Path.Combine(Root, "build", "package");
        return Directory.Exists(packed)
            ? Directory.EnumerateFiles(packed, "BevyCSharp.*.nupkg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
    }

    /// <summary>Every crate of native/Cargo.lock, the bridge's own apart, as its name and version.</summary>
    private static List<string> LockedCrates() =>
        Regex.Matches(Text("native/Cargo.lock"), @"^\[\[package\]\]\nname = ""([^""]+)""\nversion = ""([^""]+)""", RegexOptions.Multiline)
            .Where(match => match.Groups[1].Value != "bevy_csharp")
            .Select(match => $"{match.Groups[1].Value} {match.Groups[2].Value}")
            .ToList();

    /// <summary>Fails for each crate the notices have no row for, by its name and version.</summary>
    private static void NamesEvery(string notices, List<string> crates, string what)
    {
        var unnamed = crates
            .Where(crate => crate.Split(' ') is var parts && !notices.Contains($"| {parts[0]} | {parts[1]} |", StringComparison.Ordinal))
            .ToList();
        Assert.True(unnamed.Count == 0, $"N 6.5: {what} names no {string.Join(", ", unnamed)}, which build/third-party-notices.py writes in");
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
            if (subject == "‎ ‎ ‎" && sentence) continue;

            // The owner's setting of the version, a commit of build/version.txt alone, whatever it
            // says. Any other commit of the owner's is on the list with that reason.
            if (Git("show", "--name-only", "--format=", parts[0])?.Trim() == "build/version.txt") continue;
            found.Add(hash);
        }

        Hold("7.2", found, "a commit without the form COMMITS.md gives, three marks and one sentence");
    }

    [Fact]
    public void B_3()
    {
        var found = new List<string>();
        foreach (var file in Sources("native/bevy_csharp/src/", ".rs"))
        {
            var text = Text(file);
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
        var norm = Text(".github/NORM.md");
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

    // -- Native code

    /// <summary>
    /// The methods of the library and the editor that native code calls, those marked to be called
    /// from it and those made into a delegate of a type marked to be handed to it.
    /// </summary>
    /// <remarks>
    /// A function pointer the bridge is given can only point at a method marked to be called from
    /// native code, so the mark finds every callback of the bridge, ImGui's among them. A delegate
    /// handed over by the marshaller is found by its type, which none is today and which 3DEngine's
    /// test finds the same way.
    /// </remarks>
    private static List<MethodBase> HandedToNativeCode()
    {
        var handed = new HashSet<MethodBase>();
        foreach (var assembly in new[] { typeof(App).Assembly, Assembly.Load("BevyCSharp.Editor") })
            foreach (var method in Methods(assembly))
            {
                if (method.IsDefined(typeof(UnmanagedCallersOnlyAttribute))) handed.Add(method);
                foreach (var target in DelegatesHandedOver(method))
                    if (target.Module.Assembly == assembly) handed.Add(target);
            }

        return [.. handed.OrderBy(Name, StringComparer.Ordinal)];
    }

    /// <summary>The methods a method makes into a delegate of a type marked to be handed to native code.</summary>
    private static IEnumerable<MethodBase> DelegatesHandedOver(MethodBase method)
    {
        MethodBase? pointed = null;
        foreach (var (_, code, operand) in Instructions(method))
        {
            if (code == OpCodes.Ldftn || code == OpCodes.Ldvirtftn)
                pointed = Resolve(method, operand);
            else if (code == OpCodes.Newobj && pointed is not null
                     && Resolve(method, operand)?.DeclaringType?.IsDefined(typeof(UnmanagedFunctionPointerAttribute)) == true)
                yield return pointed;
            else if (code != OpCodes.Dup && code != OpCodes.Ldarg_0 && code != OpCodes.Ldnull)
                pointed = null;
        }
    }

    /// <summary>
    /// The first call, allocation or throw of a method that no catch of every exception covers, or
    /// none where there is none.
    /// </summary>
    /// <remarks>
    /// A catch's own body counts as covered, since what it does to answer native code, and that it
    /// cannot throw in turn, is read by review.
    /// </remarks>
    private static string? UnguardedCall(MethodBase method)
    {
        var body = method.GetMethodBody();
        if (body is null) return null;
        var guarded = body.ExceptionHandlingClauses
            .Where(clause => clause.Flags == ExceptionHandlingClauseOptions.Clause && (clause.CatchType == typeof(Exception) || clause.CatchType == typeof(object)))
            .SelectMany(clause => new[] { (clause.TryOffset, clause.TryOffset + clause.TryLength), (clause.HandlerOffset, clause.HandlerOffset + clause.HandlerLength) })
            .ToList();
        foreach (var (offset, code, operand) in Instructions(method))
        {
            if (!Throwing.Contains(code) || guarded.Any(range => offset >= range.Item1 && offset < range.Item2)) continue;
            return code == OpCodes.Throw ? "throw" : Resolve(method, operand) is { } called ? Name(called) : code.Name;
        }

        return null;
    }

    private static readonly HashSet<OpCode> Throwing =
        [OpCodes.Call, OpCodes.Callvirt, OpCodes.Calli, OpCodes.Newobj, OpCodes.Newarr, OpCodes.Throw, OpCodes.Castclass, OpCodes.Unbox, OpCodes.Unbox_Any];

    private static IEnumerable<MethodBase> Methods(Assembly assembly)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException error)
        {
            types = [.. error.Types.OfType<Type>()];
        }

        return types.SelectMany(type => type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)));
    }

    private static MethodBase? Resolve(MethodBase within, int token)
    {
        try
        {
            return within.Module.ResolveMethod(token,
                within.DeclaringType is { IsGenericType: true } type ? type.GetGenericArguments() : null,
                within.IsGenericMethod ? within.GetGenericArguments() : null);
        }
        catch (Exception error) when (error is ArgumentException or BadImageFormatException or TypeLoadException or FileNotFoundException)
        {
            return null;
        }
    }

    private static string Name(MethodBase method) => $"{method.DeclaringType?.FullName}.{method.Name}";

    /// <summary>A method's instructions, each with where it starts and its operand where that is a token.</summary>
    private static IEnumerable<(int Offset, OpCode Code, int Operand)> Instructions(MethodBase method)
    {
        byte[]? il;
        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray();
        }
        catch (Exception error) when (error is InvalidOperationException or BadImageFormatException)
        {
            il = null;
        }

        if (il is null) yield break;
        for (var at = 0; at < il.Length;)
        {
            var offset = at;
            var code = il[at] == 0xFE ? TwoByteCodes[il[at + 1]] : OneByteCodes[il[at]];
            at += code.Size;
            var operand = 0;
            switch (code.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar:
                    at += 1;
                    break;
                case OperandType.InlineVar:
                    at += 2;
                    break;
                case OperandType.InlineI8 or OperandType.InlineR:
                    at += 8;
                    break;
                case OperandType.InlineSwitch:
                    at += 4 + 4 * BitConverter.ToInt32(il, at);
                    break;
                default:
                    operand = BitConverter.ToInt32(il, at);
                    at += 4;
                    break;
            }

            yield return (offset, code, operand);
        }
    }

    private static readonly OpCode[] OneByteCodes = Codes(twoBytes: false);
    private static readonly OpCode[] TwoByteCodes = Codes(twoBytes: true);

    /// <summary>Every opcode of one byte or of two, by its last byte.</summary>
    private static OpCode[] Codes(bool twoBytes)
    {
        var codes = new OpCode[256];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.GetValue(null) is OpCode code && (code.Size == 2) == twoBytes)
                codes[(ushort)code.Value & 0xFF] = code;
        return codes;
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
                var text = Text(file);
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

    // A file of the checkout with its lines ended by \n alone, whatever the checkout ends them
    // with, so a rule's pattern finds on Windows what it finds on Linux, and a pattern that finds
    // nothing on a file with other ends does not pass a rule it never read.
    private static string Text(string relative) => File.ReadAllText(Full(relative)).ReplaceLineEndings("\n");

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

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/package-size.py</c> says each package of a version with its room under the size
/// nuget.org takes, and fails naming each one over it.
/// </summary>
/// <remarks>
/// The script is copied into a folder of its own with files standing in for packages, which it
/// measures and never opens, and given a limit of a few kilobytes, so no package of hundreds of
/// megabytes is written to find where it fails.
/// </remarks>
public sealed class PackageSizeTests : IDisposable
{
    private static readonly string Root = FindRoot();
    private readonly TestFolder _folder = new("bcs-package-size-");

    public void Dispose() => _folder.Dispose();

    [SkippableFact]
    public void EachPackageUnderTheSizeIsSaidWithItsRoom()
    {
        var python = Needs.Python();
        Package("BevyCSharp.1.0.0-t.nupkg", 1000);
        Package("BevyCSharp.Templates.1.0.0-t.nupkg", 500);
        Package("BevyCSharp.0.9.0.nupkg", 100_000);

        // The script leaves 4,096 bytes of the request for its framing, so 8,192 lets 4,096 through.
        var (exit, said, errors) = Measure(python, 8192);

        Assert.True(exit == 0, $"the script ended with {exit}: {errors}");
        Assert.Contains("BevyCSharp.1.0.0-t.nupkg 1,000 bytes, 3,096 bytes of room", said);
        Assert.Contains("BevyCSharp.Templates.1.0.0-t.nupkg 500 bytes, 3,596 bytes of room", said);
        Assert.DoesNotContain("0.9.0", said);
    }

    [SkippableFact]
    public void APackageOverTheSizeFailsTheRunNamingIt()
    {
        var python = Needs.Python();
        Package("BevyCSharp.1.0.0-t.nupkg", 5000);
        Package("BevyCSharp.Templates.1.0.0-t.nupkg", 500);

        var (exit, said, errors) = Measure(python, 8192);

        Assert.True(exit != 0, $"the script ended with {exit} where a package was over the size: {said}");
        Assert.Contains("BevyCSharp.1.0.0-t.nupkg 5,000 bytes, -904 bytes of room", said);
        Assert.Contains("nothing is pushed: BevyCSharp.1.0.0-t.nupkg", errors);
        Assert.DoesNotContain("Templates", errors);
    }

    /// <summary>A file of the size given in the copy's build/package, standing in for a package.</summary>
    private void Package(string name, int bytes)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_folder.File("work"), "build", "package")).FullName;
        File.WriteAllBytes(Path.Combine(folder, name), new byte[bytes]);
    }

    /// <summary>The script's measure of the copy's packages of 1.0.0-t, its exit code, what it said and its errors.</summary>
    private (int Exit, string Said, string Errors) Measure(string python, int limit)
    {
        var work = _folder.File("work");
        Directory.CreateDirectory(Path.Combine(work, "build"));
        File.Copy(Path.Combine(Root, "build", "package-size.py"), Path.Combine(work, "build", "package-size.py"), overwrite: true);

        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = work,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(Path.Combine(work, "build", "package-size.py"));
        start.ArgumentList.Add("1.0.0-t");
        start.Environment["PACKAGE_SIZE_LIMIT"] = limit.ToString();
        start.Environment.Remove("GITHUB_STEP_SUMMARY");

        using var script = Process.Start(start)!;
        var said = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(60_000), "the script ended");
        return (script.ExitCode, said.Result, errors.Result);
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A build of the library warns with <c>BCS102</c> when the bridge it copies beside each project's
/// binaries is older than a file under <c>native</c> it is built from, a shader among them, naming
/// the first such file and counting the others, and says nothing where the bridge is current, where
/// there is none, or where only the crate's tests changed.
/// </summary>
/// <remarks>
/// The check ran at packing alone and read the Rust files and the manifest alone, so a pull that
/// changed a shader ran the bridge's old one under the new C# with no word, at <c>dotnet run</c> and
/// at packing both. Each case runs the library's own project to its <c>BeforeBuild</c>, the step a
/// build takes first, with the native folder and the bridge's folder pointed at a folder of the
/// test's own whose files carry set times, so nothing of the checkout is read or compiled.
/// </remarks>
public sealed class NativeFreshnessTests : IDisposable
{
    private static readonly string Root = FindRoot();
    private static readonly DateTime Built = new(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
    private readonly TestFolder _folder = new("bcs-native-freshness-");

    public NativeFreshnessTests()
    {
        foreach (var file in new[] { "Cargo.toml", "Cargo.lock", "bevy_csharp/Cargo.toml", "bevy_csharp/build.rs", "bevy_csharp/src/lib.rs", "bevy_csharp/src/render/bcs.slang" })
            Write(file, Built.AddDays(-1));
        Directory.CreateDirectory(_folder.File("release"));
        File.WriteAllText(Bridge, "bridge");
        File.SetLastWriteTimeUtc(Bridge, Built);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void ABridgeOlderThanAShaderItIsBuiltFromIsSaidNamingTheShader()
    {
        Write("bevy_csharp/src/render/bcs.slang", Built.AddDays(1));

        var said = BeforeBuild();
        Assert.Contains("warning BCS102", said);
        Assert.Contains("bcs.slang in ", said);
        Assert.DoesNotContain("other file", said);
    }

    [Fact]
    public void ABridgeOlderThanSeveralFilesNamesTheFirstAndCountsTheOthers()
    {
        Write("bevy_csharp/src/render/bcs.slang", Built.AddDays(1));
        Write("bevy_csharp/src/lib.rs", Built.AddDays(1));
        Write("Cargo.lock", Built.AddDays(1));

        var said = BeforeBuild();
        Assert.Contains("warning BCS102", said);
        Assert.Contains(" and 2 other files in ", said);
    }

    [Fact]
    public void TheCratesTestsACurrentBridgeAndNoBridgeAreSaidNothing()
    {
        // A test module and its fixture newer than the bridge leave the library as it was.
        Write("bevy_csharp/src/render/tests.rs", Built.AddDays(1));
        Write("bevy_csharp/src/render/fixtures/material.wgsl", Built.AddDays(1));
        Assert.DoesNotContain("BCS102", BeforeBuild());

        // A bridge built after every file.
        Write("bevy_csharp/src/render/bcs.slang", Built.AddDays(1));
        File.SetLastWriteTimeUtc(Bridge, Built.AddDays(2));
        Assert.DoesNotContain("BCS102", BeforeBuild());

        // No bridge, as where a job downloads one into its slot and no build ran here.
        File.Delete(Bridge);
        Assert.DoesNotContain("BCS102", BeforeBuild());
    }

    /// <summary>The bridge this system's build copies, by the name it carries here.</summary>
    private string Bridge => _folder.File(Path.Combine("release",
        OperatingSystem.IsWindows() ? "bevy_csharp.dll" : OperatingSystem.IsMacOS() ? "libbevy_csharp.dylib" : "libbevy_csharp.so"));

    /// <summary>A file under the test's native folder, written with <paramref name="time"/> as its time.</summary>
    private void Write(string file, DateTime time)
    {
        var path = _folder.File(Path.Combine("native", file));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, file);
        File.SetLastWriteTimeUtc(path, time);
    }

    /// <summary>What the library's project says when run to its <c>BeforeBuild</c> against the test's folders.</summary>
    private string BeforeBuild()
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet")
        {
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in new[]
                 {
                     "msbuild", Path.Combine(Root, "BevyCSharp", "BevyCSharp.csproj"), "-t:BeforeBuild", "-nologo", "-nodeReuse:false", "-v:m",
                     $"-p:BevyCSharpNativeDir={_folder.File("native")}{Path.DirectorySeparatorChar}",
                     $"-p:BevyCSharpNativeOutDir={_folder.File("release")}{Path.DirectorySeparatorChar}",
                     "-p:BevyCSharpSkipNativeFreshnessCheck=false",
                 })
            start.ArgumentList.Add(argument);

        using var build = Process.Start(start)!;
        var said = build.StandardOutput.ReadToEndAsync();
        var errors = build.StandardError.ReadToEndAsync();
        Assert.True(build.WaitForExit(120_000), "the build ended");
        Assert.True(build.ExitCode == 0, $"the build ended with {build.ExitCode}: {said.Result}{errors.Result}");
        return said.Result;
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

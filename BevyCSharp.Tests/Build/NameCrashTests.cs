using System.Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/name-crash.py</c> reads a library's build id as the bridge writes it, the same in a copy
/// with its symbol table taken out as in the full library, and names a frame from the full library
/// and from no stripped one.
/// </summary>
/// <remarks>
/// A pack strips the bridge it carries and keeps the full library as its symbols, so a crash on a
/// player's machine writes the stripped library's build id and offsets, and the namer finds the full
/// library by that id. The copies are the bridge the tests run on, stripped here with GNU strip, so
/// the test runs where the bridge is an ELF library.
/// </remarks>
public sealed class NameCrashTests : IDisposable
{
    private const string Library = "libbevy_csharp.so";
    private readonly TestFolder _folder = new("bcs-name-crash-");

    public void Dispose() => _folder.Dispose();

    [SkippableFact]
    public void AStrippedCopyKeepsItsBuildIdAndNamesNothingWhereTheFullLibraryNamesTheFrame()
    {
        Skip.IfNot(OperatingSystem.IsLinux(), "strips an ELF library with GNU strip");
        var python = Needs.Python();
        var full = Directory.CreateDirectory(_folder.File("full")).FullName;
        var stripped = Directory.CreateDirectory(_folder.File("stripped")).FullName;
        File.Copy(Path.Combine(AppContext.BaseDirectory, Library), Path.Combine(full, Library));
        Skip.IfNot(Run("strip", "--strip-all", "-o", Path.Combine(stripped, Library), Path.Combine(full, Library)) is not null, "needs strip");

        var build = NameCrash.BuildId(python, Path.Combine(full, Library));
        Assert.Matches("^[0-9a-f]{16,}$", build);
        Assert.Equal(build, NameCrash.BuildId(python, Path.Combine(stripped, Library)));

        // A frame a few bytes into the function the crash log's test panics in, at the offset the
        // full library's symbol table gives it, as the second frame of a stack, whose offset is a
        // return address and is looked up a byte before.
        var listed = Run("nm", "--defined-only", Path.Combine(full, Library));
        Skip.If(listed is null, "needs nm");
        var line = listed!.Split('\n').First(l => l.EndsWith(" T bcs_panic_on_purpose", StringComparison.Ordinal));
        var offset = Convert.ToUInt64(line.Split(' ')[0], 16) + 8;
        var crash = _folder.File("crash.txt");
        File.WriteAllText(crash, $"stack:\nframes:\nbridge elf {build}\n  0: 0x7f0000001000\n  1: bridge+0x{offset:x}\n");

        Assert.Matches(@"1: bridge\+0x[0-9a-f]+ +bcs_panic_on_purpose", NameCrash.Run(python, crash, full));
        var unnamed = NameCrash.Run(python, crash, stripped);
        Assert.Contains("1: bridge+0x", unnamed);
        Assert.DoesNotContain("bcs_panic_on_purpose", unnamed);
    }

    /// <summary>What a program wrote, or null where it is not on this machine.</summary>
    private static string? Run(string program, params string[] arguments)
    {
        var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start)!;
            var said = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? said : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}

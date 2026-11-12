using System.Diagnostics;
using System.Text;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Runs <c>build/name-crash.py</c>, which names a crash file's bridge frames from the full library
/// of their build, for the tests of the crash log and of the script.
/// </summary>
internal static class NameCrash
{
    /// <summary>The crash file as the script writes it again with its frames named from the libraries under <paramref name="symbols"/>.</summary>
    public static string Run(string python, string crash, string symbols) => Script(python, crash, symbols);

    /// <summary>The library's build id as the script reads it.</summary>
    public static string BuildId(string python, string library) => Script(python, "--build-id", library).Trim();

    private static string Script(string python, params string[] arguments)
    {
        var start = new ProcessStartInfo(python)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(Path.Combine(CheatsheetTests.RepositoryRoot(), "build", "name-crash.py"));
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var script = Process.Start(start)!;
        var said = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(60_000), "the script ended");
        Assert.True(script.ExitCode == 0, $"the script ended with {script.ExitCode}: {errors.Result}");
        return said.Result;
    }
}

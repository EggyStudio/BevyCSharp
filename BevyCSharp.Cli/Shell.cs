using System.Diagnostics;
using System.Text;

namespace BevyCSharp.Cli;

/// <summary>What a child process did.</summary>
/// <param name="Code">Its exit code.</param>
/// <param name="Output">Everything it wrote, both streams, in the order it wrote them.</param>
internal readonly record struct Ran(int Code, string Output)
{
    /// <summary>Whether it worked.</summary>
    public bool Ok => Code == 0;

    /// <summary>The last few lines, which is where a build puts the reason it failed.</summary>
    public string Tail(int lines = 20)
    {
        var all = Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n", all[^Math.Min(lines, all.Length)..]).TrimEnd();
    }
}

/// <summary>Runs the things that are not a running app: the build, the tests, a cold run.</summary>
internal static class Shell
{
    /// <summary>
    /// Runs a program to completion, collecting what it wrote.
    /// </summary>
    /// <param name="file">The program.</param>
    /// <param name="arguments">Its arguments, already separated.</param>
    /// <param name="working">Where to run it.</param>
    /// <param name="echo">Whether to pass its output through as it arrives.</param>
    public static Ran Run(
        string file, IEnumerable<string> arguments, string? working = null, bool echo = false)
    {
        var start = new ProcessStartInfo(file)
        {
            WorkingDirectory = working ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        var collected = new StringBuilder(4096);

        try
        {
            using var child = Process.Start(start)
                              ?? throw new InvalidOperationException($"could not start {file}");

            // Both streams are read on their own threads. A build that fills one pipe while this
            // reads the other is a build that hangs for as long as the pipe's buffer takes to fill,
            // which for a solution build is early.
            var error = new Thread(() => Drain(child.StandardError, collected, echo, toError: true));
            error.Start();

            Drain(child.StandardOutput, collected, echo, toError: false);
            error.Join();
            child.WaitForExit();

            return new Ran(child.ExitCode, collected.ToString());
        }
        catch (Exception failure) when (failure is System.ComponentModel.Win32Exception
                                            or InvalidOperationException or IOException)
        {
            return new Ran(127, $"{file} could not be run: {failure.Message}");
        }
    }

    /// <summary>True when a program is on the path at all.</summary>
    public static bool Exists(string file) =>
        Run(OperatingSystem.IsWindows() ? "where" : "which", [file]).Ok;

    /// <summary>Reads one stream into the collection, and optionally straight through.</summary>
    private static void Drain(StreamReader reader, StringBuilder collected, bool echo, bool toError)
    {
        while (reader.ReadLine() is { } line)
        {
            lock (collected) collected.Append(line).Append('\n');

            if (!echo) continue;

            if (toError) Console.Error.WriteLine(line);
            else Console.WriteLine(line);
        }
    }
}

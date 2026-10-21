using System.Diagnostics;
using System.Text;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <c>build/animate.py</c>, which makes captures on disk into one animation, run over three PNG
/// captures by the engine's own writers through <c>./bcs animate</c> and, where Pillow is
/// installed, by Pillow, each file read back by the engine's reader.
/// </summary>
/// <remarks>
/// <para>
/// The three are flat colors, so a GIF holds each exactly, and the third is twice the size of the
/// others, which the script fits to the first's, as captures of a window resized between them
/// would need. The engine's writers are asked for by <c>BCS_ANIMATE_WRITER=engine</c>, so a machine
/// with Pillow tries both and one without it the engine's alone.
/// </para>
/// <para>
/// The engine's path runs the client the suite's own build made, named by <c>BCS_CLIENT</c>, and
/// never builds one. The checkout's <c>./bcs</c> builds a client where there is none, which on the
/// workflow's Linux runner, whose suite is built in Release where the launcher looks for Debug, it
/// did inside this test, and an MSBuild worker node that build started held the script's output
/// open while it idled for reuse, so the test read on until the suite was lost (Verdict 41). The
/// script is held to a minute and ended with all it started past that, the test failing with what
/// it printed.
/// </para>
/// </remarks>
public sealed class AnimateScriptTests : IDisposable
{
    private static readonly (byte R, byte G, byte B)[] Colors = [(200, 40, 30), (20, 60, 220), (240, 200, 30)];

    private readonly TestFolder _folder = new("bcs-animate-");

    public void Dispose() => _folder.Dispose();

    [SkippableTheory]
    [InlineData("engine", "three.gif")]
    [InlineData("engine", "three.png")]
    [InlineData("pillow", "three.gif")]
    [InlineData("pillow", "three.png")]
    public void ThreeCapturesMakeAnAnimationOfThreeFramesWithTheDelayGiven(string writer, string name)
    {
        var python = Needs.Python();
        if (writer == "pillow") Skip.IfNot(HasPillow(python), "Pillow is not installed for the script to write with");

        var (exit, log) = Animate(python, writer, name, "--delay", "250");
        Assert.True(exit == 0, log);

        var bytes = File.ReadAllBytes(_folder.File(name));
        var read = name.EndsWith(".gif", StringComparison.Ordinal) ? AnimationReader.Gif(bytes) : AnimationReader.Png(bytes);

        Assert.Equal(3, read.Frames.Count);
        Assert.Equal(new[] { 250, 250, 250 }, read.Delays);
        Assert.Equal(0, read.Loops);
        for (var i = 0; i < 3; i++)
        {
            var frame = read.Frames[i];
            Assert.Equal((48, 32), (frame.Width, frame.Height));

            var middle = (16 * 48 + 24) * 4;
            Assert.Equal(Colors[i], (frame.Pixels[middle], frame.Pixels[middle + 1], frame.Pixels[middle + 2]));
        }
    }

    [SkippableFact]
    public void PillowWritesAnAnimatedWebPOfTheThree()
    {
        var python = Needs.Python();
        Skip.IfNot(HasPillow(python), "Pillow is not installed for the script to write with");

        var (exit, log) = Animate(python, "pillow", "three.webp", "--delay", "250");
        Assert.True(exit == 0, log);

        // A RIFF of WebP holding an animation's chunk and a frame's chunk for each picture.
        var bytes = File.ReadAllBytes(_folder.File("three.webp"));
        Assert.Equal("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WEBP", Encoding.ASCII.GetString(bytes, 8, 4));
        Assert.Contains("ANIM", Encoding.ASCII.GetString(bytes));
        Assert.Equal(3, Encoding.ASCII.GetString(bytes).Split("ANMF").Length - 1);
    }

    [SkippableFact]
    public void TheEngineSaysAWebPNeedsPillowAndTheScriptRefusesAKindItDoesNotMake()
    {
        var python = Needs.Python();

        var (exit, log) = Animate(python, "engine", "three.webp", "--delay", "250");
        Assert.NotEqual(0, exit);
        Assert.Contains("needs Pillow", log);

        (exit, log) = Animate(python, "engine", "three.bmp", "--delay", "250");
        Assert.Equal(2, exit);
        Assert.Contains("none of the kinds", log);

        (exit, _) = Animate(python, "engine", "three.gif", "--delay", "0");
        Assert.Equal(2, exit);
    }

    /// <summary>Writes the three captures and runs the script over them into the folder, by the writer named.</summary>
    private (int Exit, string Log) Animate(string python, string writer, string name, params string[] options)
    {
        var pictures = new List<string>();
        for (var i = 0; i < Colors.Length; i++)
        {
            var (width, height) = i == 2 ? (96, 64) : (48, 32);
            var pixels = new byte[width * height * 4];
            for (var at = 0; at < pixels.Length; at += 4)
            {
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = (Colors[i].R, Colors[i].G, Colors[i].B, (byte)255);
            }

            var path = _folder.File($"capture{i}.png");
            File.WriteAllBytes(path, PngWriter.Still((uint)width, (uint)height, pixels));
            pictures.Add(path);
        }

        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = CheatsheetTests.RepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(Path.Combine("build", "animate.py"));
        start.ArgumentList.Add(_folder.File(name));
        foreach (var picture in pictures) start.ArgumentList.Add(picture);
        foreach (var option in options) start.ArgumentList.Add(option);
        if (writer == "engine")
        {
            start.Environment["BCS_ANIMATE_WRITER"] = "engine";
            start.Environment["BCS_CLIENT"] = Client();
        }

        using var script = new Process { StartInfo = start };
        var said = new StringBuilder();
        var ended = new TaskCompletionSource();
        var open = 2;
        DataReceivedEventHandler take = (_, line) =>
        {
            if (line.Data is null)
            {
                if (Interlocked.Decrement(ref open) == 0) ended.TrySetResult();
                return;
            }

            lock (said) said.AppendLine(line.Data);
        };
        script.OutputDataReceived += take;
        script.ErrorDataReceived += take;
        script.Start();
        script.BeginOutputReadLine();
        script.BeginErrorReadLine();

        // A minute for three small pictures, past which the script and all it started are ended and
        // the test says what it printed.
        if (!script.WaitForExit(60_000))
        {
            script.Kill(entireProcessTree: true);
            lock (said) Assert.Fail($"build/animate.py had not ended a minute after it started, having printed:\n{said}");
        }

        // Its streams end once nothing it started holds them, which is waited for five seconds at
        // most, since a process it left behind holding them is no fault of what it wrote.
        ended.Task.Wait(TimeSpan.FromSeconds(5));
        lock (said) return (script.ExitCode, said.ToString());
    }

    /// <summary>
    /// The client the suite's own build made, in the suite's configuration, which the script runs in
    /// place of building one, or the test skipped where there is none.
    /// </summary>
    private static string Client()
    {
        // The tests run from BevyCSharp.Tests/bin/<configuration>/<framework>, and the client is
        // built beside them in the same two.
        var framework = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var client = Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp.Cli", "bin", framework.Parent!.Name, framework.Name, OperatingSystem.IsWindows() ? "bcs.exe" : "bcs");
        Skip.IfNot(File.Exists(client), $"no client is built at {client}, which the suite's build of the solution makes");
        return client;
    }

    private static bool HasPillow(string python)
    {
        using var check = Process.Start(new ProcessStartInfo(python, "-c \"import PIL\"") { RedirectStandardOutput = true, RedirectStandardError = true })!;
        if (check.WaitForExit(60_000)) return check.ExitCode == 0;

        check.Kill(entireProcessTree: true);
        return false;
    }
}

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
/// The three are flat colors, so a GIF holds each exactly, and the third is twice the size of the
/// others, which the script fits to the first's, as captures of a window resized between them
/// would need. The engine's writers are asked for by <c>BCS_ANIMATE_WRITER=engine</c>, so a machine
/// with Pillow tries both and one without it the engine's alone.
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
        if (writer == "engine") start.Environment["BCS_ANIMATE_WRITER"] = "engine";

        using var script = Process.Start(start)!;
        var said = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(300_000), "the script ended within its limit");
        return (script.ExitCode, said.Result + errors.Result);
    }

    private static bool HasPillow(string python)
    {
        using var check = Process.Start(new ProcessStartInfo(python, "-c \"import PIL\"") { RedirectStandardOutput = true, RedirectStandardError = true })!;
        check.WaitForExit(60_000);
        return check.HasExited && check.ExitCode == 0;
    }
}

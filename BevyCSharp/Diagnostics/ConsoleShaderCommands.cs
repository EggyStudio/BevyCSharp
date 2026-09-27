using System.Globalization;
using System.Text;

namespace Bevy;

/// <summary>
/// What the console and the command line can ask about the shaders a running app draws with.
/// </summary>
/// <remarks>
/// A shader being edited is compiled in the background and reported in the log, which is the
/// right place for a line saying it worked and the wrong place to go looking for why it did not.
/// These answer the question directly: which programs there are, which of them failed and what
/// the compiler said, what each declares and at which offset, and a way to compile one again
/// without touching its file.
/// </remarks>
internal static class ConsoleShaderCommands
{
    /// <summary>Says how long each render pass takes.</summary>
    [Command("render.timings", "How long each render pass takes, on the CPU and the GPU, in milliseconds")]
    internal static string Timings()
    {
        if (!RendererPresent()) return "no renderer, so there are no render passes";

        var timings = Render.Timings();
        if (timings.Count == 0) return "no timings; run with Config.GpuTimings to measure them";

        static string Milliseconds(double? value) =>
            value is { } number ? number.ToString("0.000", CultureInfo.InvariantCulture) : "-";

        return string.Join(
            "\n",
            timings
                .OrderByDescending(timing => timing.GpuMilliseconds ?? timing.CpuMilliseconds ?? 0)
                .Select(timing =>
                    $"{Milliseconds(timing.GpuMilliseconds),8} gpu {Milliseconds(timing.CpuMilliseconds),8} cpu  {timing.Name}"));
    }

    /// <summary>Reads a shader buffer back from the GPU and shows its numbers.</summary>
    /// <remarks>
    /// The answer comes a frame or two later, since the buffer has to come back off the GPU, so
    /// this says it is reading and hands the numbers over once they arrive (see
    /// <see cref="ConsoleHost.Later"/>). A buffer is named by its asset key, the number an
    /// <see cref="AssetHandle"/> prints.
    /// </remarks>
    [Command("shader.buffer", "Reads a buffer back: shader.buffer <key> [float|int|uint] [count]")]
    internal static string Buffer(string words)
    {
        if (!RendererPresent()) return "no renderer, so there are no shader buffers";

        var parts = words.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var key) || key <= 0)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", "shader.buffer takes a buffer's asset key, a number above zero.");
            return "which buffer? shader.buffer <key> [float|int|uint] [count]";
        }

        var kind = parts.Length > 1 ? parts[1].ToLowerInvariant() : "float";

        if (kind is not ("float" or "int" or "uint"))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{kind}' is not a kind of number shader.buffer reads.");
            return "read as float, int or uint";
        }

        var shown = parts.Length > 2 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var asked)
            ? Math.Max(1, asked)
            : 64;

        BufferRead read;

        try
        {
            read = Shaders.BeginBufferRead(new AssetHandle(key));
        }
        catch (Bevy.Interop.BevyNativeException error)
        {
            ConsoleHost.Fail("NOT_FOUND", error.Message);
            return $"no buffer has the key {key}";
        }

        ConsoleHost.Later(() =>
        {
            if (!Shaders.TryReadBuffer(read, out var bytes) || bytes is null) return null;
            return Numbers(bytes, kind, shown);
        });

        return $"reading buffer {key}";
    }

    /// <summary>A buffer's bytes as numbers of one kind, four to a line, and how many there are.</summary>
    private static string Numbers(byte[] bytes, string kind, int shown)
    {
        var count = bytes.Length / 4;
        var lines = new StringBuilder();
        lines.Append(CultureInfo.InvariantCulture, $"{count} {kind}s in {bytes.Length} bytes");

        for (var index = 0; index < Math.Min(count, shown); index++)
        {
            if (index % 4 == 0) lines.Append(CultureInfo.InvariantCulture, $"\n{index,6}:");

            var at = index * 4;
            var text = kind switch
            {
                "int" => BitConverter.ToInt32(bytes, at).ToString(CultureInfo.InvariantCulture),
                "uint" => BitConverter.ToUInt32(bytes, at).ToString(CultureInfo.InvariantCulture),
                _ => BitConverter.ToSingle(bytes, at).ToString("G7", CultureInfo.InvariantCulture),
            };

            lines.Append(' ').Append(text);
        }

        if (count > shown) lines.Append(CultureInfo.InvariantCulture, $"\nand {count - shown} more");

        return lines.ToString();
    }

    /// <summary>Lists the programs the app has made.</summary>
    [Command("shader.list", "Lists the shader programs: number, state and files")]
    internal static string List()
    {
        if (!RendererPresent()) return "no renderer, so there are no shader programs";

        var programs = Shaders.Programs.ToList();
        if (programs.Count == 0) return "no shader programs have been made";

        return string.Join(
            "\n",
            programs.Select(program =>
                $"#{program.Id} {Describe(program.State)}: {program.Files}"));
    }

    /// <summary>Says what the compilers said about one program, or about every one that failed.</summary>
    [Command("shader.errors", "What the compiler said: shader.errors <number>, or all that failed")]
    internal static string Errors(string which)
    {
        if (!RendererPresent()) return "no renderer, so there are no shader programs";

        if (which.Trim().Length > 0)
        {
            if (Find(which) is not { } program) return $"there is no shader program {which}";

            var said = program.Diagnostics;
            return said.Length == 0 ? $"#{program.Id} compiled without a word" : said;
        }

        var failed = Shaders.Programs
            .Where(program => program.State == ShaderProgramState.Failed)
            .ToList();

        if (failed.Count == 0) return "no shader program has failed";

        var text = new StringBuilder();

        foreach (var program in failed)
        {
            if (text.Length > 0) text.Append("\n\n");
            text.Append(CultureInfo.InvariantCulture, $"#{program.Id}: {program.Files}\n");
            text.Append(program.Diagnostics);
        }

        return text.ToString();
    }

    /// <summary>Says what a program's shaders declare, and where each name is.</summary>
    [Command("shader.layout", "What a program declares, by binding and offset: shader.layout <number>")]
    internal static string Layout(string which)
    {
        if (!RendererPresent()) return "no renderer, so there are no shader programs";
        if (Find(which) is not { } program) return $"there is no shader program {which}";

        var layout = program.Layout;

        return layout.Length > 0
            ? layout
            : $"#{program.Id} has not compiled yet, so what it declares is not known";
    }

    /// <summary>Compiles or reloads a program, or every program, now.</summary>
    [Command("shader.reload", "Compiles a program again now: shader.reload <number>, or all")]
    internal static string Reload(string which)
    {
        if (!RendererPresent()) return "no renderer, so there are no shader programs";

        if (which.Trim().Length > 0 && which.Trim() != "all")
        {
            if (Find(which) is not { } program) return $"there is no shader program {which}";

            program.Reload();
            return $"reloading #{program.Id}: {program.Files}";
        }

        var count = 0;

        foreach (var program in Shaders.Programs)
        {
            program.Reload();
            count++;
        }

        return $"reloading {count} shader programs";
    }

    /// <summary>Says whether Slang can be compiled here, and what the renderer last complained of.</summary>
    [Command("shader.status", "Whether slangc was found, and the last error the renderer reported")]
    internal static string Status()
    {
        if (!RendererPresent()) return "no renderer";

        var slang = Shaders.SlangAvailable
            ? "slangc was found, so Slang shaders compile and reload"
            : "slangc was not found, so Slang shaders load from the cache and edits are not compiled";

        var last = Shaders.LastRenderError;

        return last.Length == 0
            ? $"{slang}. The renderer has reported no errors."
            : $"{slang}. The renderer last reported: {last}";
    }

    private static ShaderProgram? Find(string which)
    {
        var text = which.Trim().TrimStart('#');

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;

        if (id < 0 || id >= Shaders.ProgramCount) return null;
        return new ShaderProgram(id);
    }

    private static string Describe(ShaderProgramState state) => state switch
    {
        ShaderProgramState.Ready => "ready",
        ShaderProgramState.Failed => "failed",
        _ => "compiling",
    };

    private static bool RendererPresent()
    {
        if (App.HasRenderer && ConsoleHost.World?.Resource<Config>() is not { Headless: true })
            return true;

        ConsoleHost.Fail(
            "NO_RENDERER",
            "This app draws nothing, so it has no shaders. Start one with a window, or one that "
            + "draws offscreen.");

        return false;
    }
}

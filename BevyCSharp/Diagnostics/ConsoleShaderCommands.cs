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

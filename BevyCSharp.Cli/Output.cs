using System.Text;
using System.Text.Json;

namespace BevyCSharp.Cli;

/// <summary>
/// Writes the answer, and decides what the process exits with.
/// </summary>
/// <remarks>
/// <para>
/// Everything goes to the standard output stream, success and failure alike, so a caller reads one
/// place and branches on one field. The error stream carries only what a person would want while
/// watching, which is why nothing should ever be parsed off it.
/// </para>
/// <para>
/// Under <c>--json</c> the envelope the app produced is what gets printed, pretty-printed and
/// otherwise untouched. The human rendering is a reading of that same document, never a second
/// source of truth.
/// </para>
/// </remarks>
internal static class Output
{
    /// <summary>Prints one envelope and answers with the exit code it implies.</summary>
    /// <param name="options">How to write it.</param>
    /// <param name="envelope">The document, as the app or this tool produced it.</param>
    /// <param name="human">Writes the payload for a person; the raw result when omitted.</param>
    public static int Print(Options options, string envelope, Action<JsonElement>? human = null)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(envelope);
        }
        catch (JsonException error)
        {
            Console.Error.WriteLine($"bcs: the answer was not JSON: {error.Message}");
            return Exit.General;
        }

        using (document)
        {
            var root = document.RootElement;
            var success = root.TryGetProperty("success", out var flag)
                          && flag.ValueKind == JsonValueKind.True;

            if (options.Json)
            {
                Console.WriteLine(Pretty(root));
                return success ? Exit.Ok : Exit.For(Code(root));
            }

            if (!success)
            {
                foreach (var error in Errors(root))
                {
                    Console.WriteLine($"error [{error.Code}] {error.Message}");
                }

                // A failure can still carry a payload worth seeing: what a command answered before
                // it said it had failed, or the candidates an ambiguous session listed.
                Payload(root, human, options);
                return Exit.For(Code(root));
            }

            Payload(root, human, options);
            return Exit.Ok;
        }
    }

    /// <summary>Prints a sentence, as an envelope or as itself.</summary>
    public static int Say(Options options, string command, string sentence)
    {
        if (!options.Json)
        {
            if (!options.Quiet) Console.WriteLine(sentence);
            return Exit.Ok;
        }

        Console.WriteLine(Pretty(Bevy.CliJson.Ok(
            command, writer => writer.WriteString("message", sentence))));

        return Exit.Ok;
    }

    /// <summary>Prints a refusal that this tool decided on its own.</summary>
    public static int Refuse(Options options, string command, string code, string message) =>
        Print(options, Bevy.CliJson.Fail(command, code, message));

    /// <summary>The first error's code, or a general one.</summary>
    public static string Code(JsonElement root) =>
        Errors(root) is [{ Code: { Length: > 0 } code }, ..] ? code : "ERROR";

    /// <summary>The errors an envelope carries.</summary>
    private static List<(string Code, string Message)> Errors(JsonElement root)
    {
        var found = new List<(string, string)>();

        if (!root.TryGetProperty("errors", out var errors)
            || errors.ValueKind != JsonValueKind.Array)
        {
            return found;
        }

        foreach (var error in errors.EnumerateArray())
        {
            found.Add((
                error.TryGetProperty("code", out var code) ? code.GetString() ?? "ERROR" : "ERROR",
                error.TryGetProperty("message", out var message)
                    ? message.GetString() ?? string.Empty
                    : string.Empty));
        }

        return found;
    }

    /// <summary>Writes the payload for a person.</summary>
    private static void Payload(JsonElement root, Action<JsonElement>? human, Options options)
    {
        if (options.Quiet) return;
        if (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (human is not null)
        {
            human(data);
            return;
        }

        // The default reading: what the command answered, which is the whole of it for most.
        if (data.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.String)
        {
            var text = result.GetString();
            if (!string.IsNullOrEmpty(text)) Console.WriteLine(text);

            return;
        }

        Console.WriteLine(Pretty(data));
    }

    /// <summary>The same document, laid out to be read.</summary>
    public static string Pretty(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Pretty(document.RootElement);
    }

    /// <summary>The same element, laid out to be read.</summary>
    public static string Pretty(JsonElement element)
    {
        var buffer = new MemoryStream(512);

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            element.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}

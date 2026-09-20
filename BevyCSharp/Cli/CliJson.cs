using System.Text;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// What went wrong, as something a script can branch on.
/// </summary>
/// <param name="Code">
/// A stable token, in capitals. The sentence is for a person and may be reworded at any time; the
/// code is the part a script is allowed to depend on.
/// </param>
/// <param name="Message">One sentence about what happened, and where possible what to do about it.</param>
public readonly record struct CliError(string Code, string Message);

/// <summary>
/// The one shape every answer takes.
/// </summary>
/// <remarks>
/// <para>
/// Success and failure are the same document, differing in a field, and both are written to the
/// standard output stream. A caller therefore reads one place and branches on one field, rather
/// than guessing from an empty output or picking sentences off the error stream. That is the whole
/// of the contract:
/// </para>
/// <code>
/// { "success": true, "command": "command", "data": { }, "errors": [ ], "warnings": [ ] }
/// </code>
/// <para>
/// Written by hand with <see cref="Utf8JsonWriter"/> rather than serialised from an object, because
/// this library is built to be ahead-of-time compiled and a reflecting serialiser would take that
/// away from everything that references it.
/// </para>
/// </remarks>
public static class CliJson
{
    /// <summary>Writes an envelope, with whatever <paramref name="data"/> writes as its payload.</summary>
    /// <param name="command">Which command answered.</param>
    /// <param name="success">Whether it worked.</param>
    /// <param name="data">Writes the payload's properties, or null for no payload.</param>
    /// <param name="errors">What went wrong, for a failure.</param>
    /// <param name="warnings">What went nearly wrong, for either.</param>
    /// <param name="id">The caller's correlation id, echoed back when there was one.</param>
    public static string Envelope(
        string command,
        bool success,
        Action<Utf8JsonWriter>? data = null,
        IReadOnlyList<CliError>? errors = null,
        IReadOnlyList<string>? warnings = null,
        string? id = null)
    {
        var buffer = new MemoryStream(512);

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            if (id is not null) writer.WriteString("id", id);

            writer.WriteBoolean("success", success);
            writer.WriteString("command", command);

            if (data is null)
            {
                writer.WriteNull("data");
            }
            else
            {
                writer.WritePropertyName("data");
                writer.WriteStartObject();
                data(writer);
                writer.WriteEndObject();
            }

            writer.WritePropertyName("errors");
            writer.WriteStartArray();

            foreach (var error in errors ?? [])
            {
                writer.WriteStartObject();
                writer.WriteString("code", error.Code);
                writer.WriteString("message", error.Message);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WritePropertyName("warnings");
            writer.WriteStartArray();

            foreach (var warning in warnings ?? []) writer.WriteStringValue(warning);

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>An envelope that worked.</summary>
    public static string Ok(string command, Action<Utf8JsonWriter>? data = null, string? id = null) =>
        Envelope(command, success: true, data, id: id);

    /// <summary>An envelope that did not.</summary>
    public static string Fail(
        string command, string code, string message, string? id = null) =>
        Envelope(command, success: false, data: null, errors: [new CliError(code, message)], id: id);
}

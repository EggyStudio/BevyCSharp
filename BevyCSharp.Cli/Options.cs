namespace BevyCSharp.Cli;

/// <summary>What every verb understands, whichever verb it is.</summary>
internal sealed record Options
{
    /// <summary>How to write the answer: <c>human</c> or <c>json</c>.</summary>
    public string Format { get; init; } = "human";

    /// <summary>Only sessions started from this directory.</summary>
    public string? Project { get; init; }

    /// <summary>Only the session of this entry assembly, such as <c>BevyCSharp.Editor</c>.</summary>
    public string? Name { get; init; }

    /// <summary>Only the session with this process id.</summary>
    public int? Pid { get; init; }

    /// <summary>How long to wait for an answer, in seconds.</summary>
    public double Timeout { get; init; } = 30;

    /// <summary>Say only what was asked for.</summary>
    public bool Quiet { get; init; }

    /// <summary>True when the answer should be the envelope itself.</summary>
    public bool Json => Format is "json";

    /// <summary>
    /// Reads the flags out of a command line, leaving everything else in order.
    /// </summary>
    /// <remarks>
    /// Flags are recognised anywhere until a bare <c>--</c>, and everything after that is left
    /// alone. That matters for <c>bcs command</c>, whose own arguments can look like flags:
    /// <c>bcs command input.type -- --headless</c> types the word rather than reading it.
    /// </remarks>
    public static (Options Flags, string[] Words) Parse(string[] arguments)
    {
        // The environment first, so a flag on the line always wins over it.
        var options = new Options
        {
            Format = Text("BCS_FORMAT") ?? "human",
            Project = Text("BCS_PROJECT"),
            Name = Text("BCS_NAME"),
            Timeout = Number("BCS_TIMEOUT") ?? 30,
        };

        var rest = new List<string>();
        var passing = false;

        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];

            if (passing)
            {
                rest.Add(argument);
                continue;
            }

            switch (argument)
            {
                case "--":
                    passing = true;
                    continue;

                case "--json":
                    options = options with { Format = "json" };
                    continue;

                case "--human":
                    options = options with { Format = "human" };
                    continue;

                case "--quiet" or "-q":
                    options = options with { Quiet = true };
                    continue;

                case "--format" when index + 1 < arguments.Length:
                    options = options with { Format = arguments[++index].ToLowerInvariant() };
                    continue;

                case "--project" or "-p" when index + 1 < arguments.Length:
                    options = options with
                    {
                        Project = Path.GetFullPath(arguments[++index]),
                    };
                    continue;

                // No short form: -n belongs to `logs`, where it means how many lines, and a flag
                // that means two things is a flag that silently does the wrong one.
                case "--name" when index + 1 < arguments.Length:
                    options = options with { Name = arguments[++index] };
                    continue;

                case "--session" or "-s" when index + 1 < arguments.Length
                                              && int.TryParse(arguments[index + 1], out var pid):
                    index++;
                    options = options with { Pid = pid };
                    continue;

                case "--timeout" or "-t" when index + 1 < arguments.Length
                                              && double.TryParse(arguments[index + 1], out var wait):
                    index++;
                    options = options with { Timeout = wait };
                    continue;

                default:
                    rest.Add(argument);
                    continue;
            }
        }

        return (options, [.. rest]);
    }

    /// <summary>An environment variable, or nothing when it is unset or empty.</summary>
    private static string? Text(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    /// <summary>An environment variable as a number, or nothing.</summary>
    private static double? Number(string name) =>
        double.TryParse(Text(name), out var value) ? value : null;
}

namespace BevyCSharp.Cli;

/// <summary>
/// What the process exits with.
/// </summary>
/// <remarks>
/// The split that matters is <see cref="Failed"/> against <see cref="TestsFailed"/>: a run that
/// produced a verdict and the verdict was bad, against a run that never produced one. Retrying the
/// second is worth doing and retrying the first never is, and a caller can only tell them apart if
/// they are different numbers.
/// </remarks>
internal static class Exit
{
    /// <summary>It worked.</summary>
    public const int Ok = 0;

    /// <summary>Something went wrong that has no better code.</summary>
    public const int General = 1;

    /// <summary>The command line itself was wrong.</summary>
    public const int BadArguments = 2;

    /// <summary>Nothing was wrong with the request; the world was not ready for it.</summary>
    public const int Precondition = 4;

    /// <summary>It ran, and it failed.</summary>
    public const int Failed = 6;

    /// <summary>Tests ran and some did not pass.</summary>
    public const int TestsFailed = 8;

    /// <summary>The code for an error, by the token it carries.</summary>
    public static int For(string code) => code switch
    {
        "NO_SESSION" or "SESSION_UNREACHABLE" or "SESSION_CLOSING" => Precondition,
        "NO_RENDERER" or "NO_WINDOW" or "NO_BRIDGE" or "NO_CHECKOUT" => Precondition,
        "AMBIGUOUS_SESSION" or "BAD_ARGUMENT" or "BAD_REQUEST" => BadArguments,
        "TESTS_FAILED" => TestsFailed,
        _ => Failed,
    };
}

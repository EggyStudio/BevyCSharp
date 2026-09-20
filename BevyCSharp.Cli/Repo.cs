namespace BevyCSharp.Cli;

/// <summary>Where the checkout is, and what is in it.</summary>
/// <remarks>
/// Found by walking up from the working directory for the solution file, the same way git finds
/// its own root, so the command line works from anywhere inside a checkout and says so plainly
/// when it is run from outside one.
/// </remarks>
internal static class Repo
{
    /// <summary>The checkout's root, or null when this is not being run inside one.</summary>
    public static string? Root { get; } = Find(Environment.CurrentDirectory);

    /// <summary>Where launched apps write what they say.</summary>
    public static string Logs => Path.Combine(Root ?? Environment.CurrentDirectory, "build", "sessions");

    /// <summary>The log one app writes to.</summary>
    public static string LogFor(string name) => Path.Combine(Logs, $"{name}.log");

    /// <summary>The project directory for a short name such as <c>editor</c>.</summary>
    public static string? ProjectFor(string name) =>
        Root is null ? null : Path.Combine(Root, name);

    /// <summary>
    /// The built executable for a project, when there is one to run directly.
    /// </summary>
    /// <remarks>
    /// Preferred over <c>dotnet run</c>, which builds first and then launches the app as a child of
    /// itself: two processes where one will do, and a second of delay on every start.
    /// </remarks>
    public static string? BinaryFor(string project)
    {
        if (ProjectFor(project) is not { } directory) return null;

        foreach (var configuration in (string[])["Debug", "Release"])
        {
            var candidate = Path.Combine(
                directory, "bin", configuration, "net10.0",
                OperatingSystem.IsWindows() ? $"{project}.exe" : project);

            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>Walks up for the solution file.</summary>
    private static string? Find(string from)
    {
        var directory = new DirectoryInfo(from);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BevyCSharp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

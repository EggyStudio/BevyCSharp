namespace Bevy;

/// <summary>
/// What a console command may ask of whoever is running it.
/// </summary>
/// <remarks>
/// <para>
/// A <c>[Command]</c> method takes words and returns a sentence, which is the whole of its signature
/// and the reason adding one is writing one. That leaves three things it sometimes needs and cannot
/// be handed: the world, a way to say it failed rather than merely answered, and a way to say its
/// answer is not ready yet. All three live here, set around the call by whatever ran it.
/// </para>
/// <para>
/// Everything ECS-touching is ambient on the world Bevy lends the running system, so a command is
/// only ever run from inside one. <see cref="Lend"/> says so, and <see cref="Ecs"/> refuses rather
/// than returning something dead when it was called from anywhere else.
/// </para>
/// </remarks>
public static class ConsoleHost
{
    /// <summary>The world the running command may touch, or null outside a command.</summary>
    public static World? World { get; private set; }

    /// <summary>What the last command said went wrong, or null when it merely answered.</summary>
    internal static CliError? Failure { get; private set; }

    /// <summary>The frame the last command's answer should be held until, or null for now.</summary>
    internal static ulong? Held { get; private set; }

    /// <summary>The entities, for a command that reads or changes the world.</summary>
    /// <exception cref="InvalidOperationException">Called from outside a running system.</exception>
    public static EcsWorld Ecs =>
        World?.Resource<EcsWorld>()
        ?? throw new InvalidOperationException(
            "This command touches the world, so it can only run inside a system. The console runs "
            + "commands from a system; something else ran this one from outside the frame.");

    /// <summary>The clock, for a command that says when it is.</summary>
    /// <exception cref="InvalidOperationException">Called from outside a running system.</exception>
    public static Time Time =>
        World?.Resource<Time>()
        ?? throw new InvalidOperationException(
            "This command reads the clock, so it can only run inside a system.");

    /// <summary>
    /// Says the command failed, with a code a script can branch on.
    /// </summary>
    /// <remarks>
    /// The returned sentence is still the answer; this only decides whether the answer is reported
    /// as one. Without it, "no renderer, so nothing was captured" is indistinguishable to a caller
    /// from a capture that worked, because both are a string.
    /// </remarks>
    /// <param name="code">A stable token in capitals, such as <c>NO_RENDERER</c>.</param>
    /// <param name="message">One sentence about what happened.</param>
    public static void Fail(string code, string message) =>
        Failure = new CliError(code, message);

    /// <summary>
    /// Holds this command's answer until the frame counter reaches <paramref name="frame"/>.
    /// </summary>
    /// <remarks>
    /// For a command whose point is the waiting. The command runs once, now, and says when its
    /// answer should be handed back; it is not run again. Whoever is driving the console decides
    /// what to do with that, and a console with no frames to wait for ignores it.
    /// </remarks>
    public static void Hold(ulong frame) => Held = frame;

    /// <summary>
    /// Lends the world to commands run inside the returned scope.
    /// </summary>
    /// <example>
    /// <code>
    /// using (ConsoleHost.Lend(world)) answer = ConsoleCommands.Run(line);
    /// </code>
    /// </example>
    public static Scope Lend(World world) => new(world);

    /// <summary>Forgets what the last command said about itself.</summary>
    internal static void Reset()
    {
        Failure = null;
        Held = null;
    }

    /// <summary>The loan, which ends when it is disposed.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly World? _previous;

        /// <summary>Starts a loan of <paramref name="world"/>.</summary>
        internal Scope(World world)
        {
            ArgumentNullException.ThrowIfNull(world);

            _previous = World;
            World = world;
            Reset();
        }

        /// <summary>Ends it, putting back whatever was lent before.</summary>
        public void Dispose() => World = _previous;
    }
}

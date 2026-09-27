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

    /// <summary>What the last command said to ask each frame for its real answer, or null.</summary>
    /// <remarks>
    /// For whatever runs commands. One that has frames to wait through asks this once a frame, with
    /// the world lent, until it answers, as <see cref="Later"/> describes.
    /// </remarks>
    public static Func<string?>? Pending { get; private set; }

    /// <summary>
    /// How many frames a runner asks <see cref="Later"/>'s question before answering that it gave up.
    /// </summary>
    public const ulong LaterFrames = 600;

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
    /// Says this command's answer is not ready yet, and how to ask for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a command whose answer comes back from the GPU a few frames later, such as a buffer read
    /// back. The command returns what it has now, which a console shows at once, and
    /// <paramref name="poll"/> is asked once a frame, with the world lent, until it answers with
    /// something other than null. That is then handed back as the command's answer: in place of the
    /// first one to a caller of <c>bcs command</c>, and as a line of its own in a console.
    /// </para>
    /// <para>
    /// A runner gives up after <see cref="LaterFrames"/> frames and says so, and one with no
    /// frames to ask it in ignores it, so the answer returned now should stand on its own.
    /// </para>
    /// </remarks>
    public static void Later(Func<string?> poll) => Pending = poll ?? throw new ArgumentNullException(nameof(poll));

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
        Pending = null;
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

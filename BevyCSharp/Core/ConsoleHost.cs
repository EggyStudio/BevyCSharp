using Diagnostics;

namespace Bevy;

/// <summary>
/// What a console command may ask of whoever is running it.
/// </summary>
/// <remarks>
/// <para>
/// A <c>[Command]</c> method takes words and returns a sentence, which is the whole of its
/// signature and the reason adding one is writing one. That leaves three things it sometimes needs
/// and cannot be handed, the world, a way to say it failed rather than merely answered, and a way
/// to say its answer is not ready yet. All three live here, set around the call by whatever ran it.
/// </para>
/// <para>
/// Everything ECS-touching is ambient on the world Bevy lends the running system, so a command is
/// only ever run from inside one. <see cref="Lend"/> says so, and <see cref="Ecs"/> refuses rather
/// than returning something dead when it was called from anywhere else.
/// </para>
/// <para>
/// It also reads the words the commands of every area read alike, a line's first word and an
/// entity named by its name or its <c>#index</c>.
/// </para>
/// <para>
/// The commands are the Diagnostics module's <see cref="Registry.Process"/>, which the generated
/// registrations of every assembly add to, and <see cref="Run(string)"/> runs a line through it,
/// with this host's own word about the command, its failure, its hold and its later answer,
/// forgotten before the line runs.
/// </para>
/// </remarks>
public static class ConsoleHost
{
    /// <summary>The world the running command may touch, or null outside a command.</summary>
    public static World? World { get; private set; }

    /// <summary>What the last command said went wrong, or null when it merely answered.</summary>
    internal static CliError? Failure { get; private set; }

    /// <summary>
    /// The frame the last command's answer should be held until, counted in the frames the clock
    /// moved in (<see cref="Time.ClockFrames"/>), or null for now.
    /// </summary>
    internal static ulong? Held { get; private set; }

    /// <summary>
    /// What the last command said to ask each frame for its real answer, or null.
    /// </summary>
    /// <remarks>
    /// For whatever runs commands. One that has frames to wait through asks this once a frame, with
    /// the world lent, until it answers, as <see cref="Later"/> describes.
    /// </remarks>
    public static Func<string?>? Pending { get; private set; }

    /// <summary>
    /// The frame from which a runner counts <see cref="LaterFrames"/> before giving
    /// <see cref="Pending"/> up, or null to count from the frame the command ran in, each counted
    /// in the frames the clock moved in.
    /// </summary>
    internal static ulong? PendingFrom { get; private set; }

    /// <summary>
    /// Whether <see cref="Pending"/> waits on something other than the game, as the renderer's
    /// pipelines, so a clock held between commands stays still while it waits and the runner
    /// counts <see cref="LaterFrames"/> in the frames the app draws.
    /// </summary>
    internal static bool PendingStill { get; private set; }

    /// <summary>
    /// The frame a runner gives <see cref="Pending"/> up at, counted from <paramref name="now"/> or
    /// a later frame the command named.
    /// </summary>
    internal static ulong GiveUpAt(ulong now) => Math.Max(now, PendingFrom ?? now) + LaterFrames;

    /// <summary>
    /// How many frames a runner asks <see cref="Later"/>'s question before answering that it gave
    /// up.
    /// </summary>
    public const ulong LaterFrames = 600;

    /// <summary>The entities, for a command that reads or changes the world.</summary> <exception
    /// cref="InvalidOperationException">Called from outside a running system.</exception>
    public static EcsWorld Ecs =>
        World?.Resource<EcsWorld>()
        ?? throw new InvalidOperationException(
            "This command touches the world, so it can only run inside a system. The console runs "
            + "commands from a system; something else ran this one from outside the frame.");

    /// <summary>The clock, for a command that says when it is.</summary> <exception
    /// cref="InvalidOperationException">Called from outside a running system.</exception>
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
    /// Holds this command's answer until the clock's count of frames reaches <paramref
    /// name="frame"/>.
    /// </summary>
    /// <remarks>
    /// For a command whose point is the waiting. The command runs once, now, and says when its
    /// answer should be handed back; it is not run again. Whoever is using the console decides
    /// what to do with that, and a console with no frames to wait for ignores it. The count is
    /// <see cref="Time.ClockFrames"/>, which leaves out the frames a hold kept the clock still in.
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
    /// something other than null. That is then handed back as the command's answer, in place of the
    /// first one to a caller of <c>bcs command</c>, and as a line of its own in a console.
    /// </para>
    /// <para>
    /// A runner gives up after <see cref="LaterFrames"/> frames and says so, and one with no
    /// frames to ask it in ignores it, so the answer returned now should stand on its own.
    /// </para>
    /// </remarks>
    public static void Later(Func<string?> poll) => Pending = poll ?? throw new ArgumentNullException(nameof(poll));

    /// <summary>
    /// <see cref="Later"/> for a command whose answer waits on a frame it names, which a runner
    /// gives as long as <see cref="LaterFrames"/> past that frame rather than past now.
    /// </summary>
    /// <remarks>
    /// For a capture taken at a frame further off than a runner would otherwise wait, such as a
    /// scene that settles over fifteen seconds of its clock.
    /// </remarks>
    internal static void LaterFrom(Func<string?> poll, ulong from)
    {
        Later(poll);
        PendingFrom = from;
    }

    /// <summary>
    /// <see cref="Later"/> for a command that waits on something other than the game, which leaves
    /// a clock held between commands still while it waits, so the command after it finds the game
    /// where the one before left it however long the wait took on the device.
    /// </summary>
    /// <remarks>
    /// The frames still run, the clock held at its moment, and a runner gives the poll up after
    /// <see cref="LaterFrames"/> of them, counted in the frames the app draws since the clock's own
    /// count stands still.
    /// </remarks>
    internal static void LaterStill(Func<string?> poll)
    {
        Later(poll);
        PendingStill = true;
    }

    /// <summary>
    /// Lends the world to commands run inside the returned scope.
    /// </summary>
    /// <example>
    /// <code>
    /// using (ConsoleHost.Lend(world)) answer = ConsoleHost.Run(line);
    /// </code>
    /// </example>
    public static Scope Lend(World world) => new(world);

    /// <summary>
    /// The commands and the settings every assembly of the engine adds, the module's
    /// <see cref="Registry.Process"/> with each of those assemblies loaded, so their registrations
    /// have run.
    /// </summary>
    public static Registry Commands
    {
        get
        {
            EngineAssemblies.Load();
            return Registry.Process;
        }
    }

    /// <summary>Runs a line as it was typed, through <see cref="Commands"/>.</summary>
    /// <remarks>
    /// What the last command said about itself is forgotten first, so the failure, the hold and the
    /// later answer read after it are this line's. Everything a command can do wrong is answered
    /// rather than thrown, the module's own codes among them, because the console is a place where
    /// people type things that are not quite right.
    /// </remarks>
    public static Answer Run(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        Reset();
        return Commands.Run(line);
    }

    /// <summary>
    /// Runs a line the player entered through the process's console (<see
    /// cref="ConsoleLog.Model"/>), which writes it and its answer, with the world lent where one is
    /// given.
    /// </summary>
    /// <remarks>
    /// The line and its answer are the console's, never counted on a line before them as a
    /// repeated line the program writes is, so each line typed shows each time it was typed.
    /// </remarks>
    /// <param name="line">What was typed.</param>
    /// <param name="world">
    /// The world to lend the command, which a command that reads or changes an entity needs, or
    /// nothing outside a frame.
    /// </param>
    public static Answer Submit(string line, World? world = null)
    {
        ArgumentNullException.ThrowIfNull(line);
        EngineAssemblies.Load();
        if (world is null)
        {
            Reset();
            return ConsoleLog.Model.Submit(line);
        }

        using (Lend(world)) return ConsoleLog.Model.Submit(line);
    }

    /// <summary>
    /// The answer of a command's text, or of nothing, as the generated registrations answer, failed
    /// with the code the command gave <see cref="Fail"/> where it called it.
    /// </summary>
    /// <remarks>
    /// The text is the command's own where it gave one, as a console shows it, and the sentence it
    /// gave <see cref="Fail"/> where it returned none. Whoever runs the line reads that sentence
    /// from <see cref="Failure"/> as well, as the command line reports it beside the text.
    /// </remarks>
    /// <param name="text">
    /// What the command returned, or null for a command that returns nothing.
    /// </param>
    public static Answer Answered(string? text) => Failure is { } failure
        ? Answer.Fail(failure.Code, string.IsNullOrEmpty(text) ? failure.Message : text)
        : new Answer(text ?? string.Empty);

    /// <summary>
    /// A line with one pair of enclosing quotes taken off, if it has them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For the command that takes everything after its name. Somebody typing <c>do "Spawn/Cube"</c>
    /// means the path without the quotes, and something sending a line over a socket has to quote a
    /// value that contains spaces for it to survive as one. Both need the same thing back.
    /// </para>
    /// <para>
    /// Only a line that is entirely one quoted run is unwrapped, so a fragment that merely contains
    /// quotes, such as <c>eval world.SetName(e, "x")</c>, is left exactly as it was written.
    /// </para>
    /// </remarks>
    public static string Unwrap(string line)
    {
        if (line.Length < 2 || line[0] != '"' || line[^1] != '"') return line;

        var inside = line[1..^1];
        var escaped = false;

        foreach (var character in inside)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (character == '\\') escaped = true;
            else if (character == '"') return line;
        }

        return inside.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    /// <summary>Forgets what the last command said about itself.</summary>
    internal static void Reset()
    {
        Failure = null;
        Held = null;
        Pending = null;
        PendingFrom = null;
        PendingStill = false;
    }

    /// <summary>
    /// A line split into its first word and the rest, the first being the words in quotes when the
    /// line starts with one, so an entity whose name has a space can be named.
    /// </summary>
    internal static string[] FirstAndRest(string line)
    {
        var trimmed = line.Trim();

        string[] split;
        if (trimmed.StartsWith('"') && trimmed.IndexOf('"', 1) is > 0 and var close)
        {
            var rest = trimmed[(close + 1)..].Trim();
            split = rest.Length > 0 ? [trimmed[1..close], rest] : [trimmed[1..close]];
        }
        else
        {
            split = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        }

        // The rest without the quotes a caller put round it as a whole, as bcs does for a word
        // with a space in it.
        if (split.Length == 2 && split[1].Length > 1 && split[1][0] == '"' && split[1][^1] == '"')
            split[1] = split[1][1..^1];

        return split;
    }

    /// <summary>The entity a word names, by name or by <c>#index</c>.</summary>
    internal static Entity? Find(EcsWorld world, string which)
    {
        if (which.Length == 0) return null;

        if (which[0] == '#' && uint.TryParse(which[1..], out var index))
        {
            foreach (var entity in world.All())
            {
                if (entity.Index == index) return entity;
            }

            return null;
        }

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) == which) return entity;
        }

        return null;
    }

    /// <summary>What to say, and to report, when nothing answers to a name.</summary>
    internal static string Missing(string which)
    {
        ConsoleHost.Fail(
            "NO_SUCH_ENTITY", $"No entity is called '{which}'. Run entity.list to see what is.");

        return $"no entity called {which}";
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

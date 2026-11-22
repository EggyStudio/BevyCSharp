using System.Text;
using Diagnostics;

namespace Bevy;

/// <summary>
/// What the program has been saying, kept so something can show it.
/// </summary>
/// <remarks>
/// <para>
/// Everything written to the standard output and error streams is teed into the process's console
/// here, the Diagnostics module's <see cref="ConsoleModel"/> over <see cref="Registry.Process"/>,
/// so a console can put it on screen without anything that writes a line having to know that a
/// console exists. Writing through <see cref="Write(ConsoleLevel, string)"/> says the level
/// outright; a line that arrives through the streams is an error if it came from the error stream
/// and information otherwise.
/// </para>
/// <para>
/// The model keeps the last <see cref="Depth"/> lines, because a program left running all day
/// writes a great many lines and the interesting ones are always the last few. A line repeating
/// the last one the program wrote is counted on it, because the line that repeats every frame is
/// the one that would otherwise push everything else off the end, and each line is stamped with
/// the frame it was last written on (<see cref="Frame"/>).
/// </para>
/// </remarks>
public static class ConsoleLog
{
    /// <summary>How many lines are kept.</summary>
    public const int Depth = ConsoleModel.DefaultDepth;

    private static readonly object Gate = new();

    /// <summary>
    /// The process's console, the lines written and the lines entered with their answers, which a
    /// console draws and the run's log file hears of (<see cref="ConsoleModel.LineWritten"/>).
    /// </summary>
    public static ConsoleModel Model { get; } = Made();

    // Each line the console runs is followed by the host, which logs a thrown stack and keeps a
    // later answer, so a console drawn by anybody behaves as the engine's does.
    private static ConsoleModel Made()
    {
        var model = new ConsoleModel(Registry.Process, Depth, () => (long)Frame);
        model.LineRun += ConsoleHost.Ran;
        return model;
    }

    /// <summary>How many lines have ever been written, so a reader can notice new ones.</summary>
    public static long Written => Model.Written;

    /// <summary>What the frame counter says, for whoever writes a line next.</summary>
    /// <remarks>
    /// Set by whatever runs the program's loop. A log that had to ask the engine what frame it is
    /// would be a log that cannot be written to from a thread, or before there is an engine.
    /// </remarks>
    public static ulong Frame { get; set; }

    /// <summary>Starts teeing the output and error streams into the console.</summary>
    /// <remarks>
    /// Idempotent, and harmless if it is never called, since the console stays empty and whatever
    /// shows it says so.
    /// </remarks>
    public static void Start()
    {
        // Kept here rather than found by asking what the console writes to, which is the tee in a
        // synchronized wrapper the console puts round whatever it is given, so a second start
        // that asked would tee the tee and write every line twice.
        lock (Gate)
        {
            if (_output is not null) return;

            _output = new Tee(Console.Out, ConsoleLevel.Info);
            _error = new Tee(Console.Error, ConsoleLevel.Error);
        }

        Console.SetOut(_output);
        Console.SetError(_error);
    }

    private static Tee? _output;
    private static Tee? _error;

    /// <summary>Whether the streams are teed into the console.</summary>
    internal static bool Teeing
    {
        get { lock (Gate) return _output is not null; }
    }

    /// <summary>
    /// Puts the streams back as they were before <see cref="Start"/>, for a test that teed them.
    /// </summary>
    internal static void Stop()
    {
        Tee? output, error;
        lock (Gate)
        {
            (output, error) = (_output, _error);
            (_output, _error) = (null, null);
        }

        if (output is not null) Console.SetOut(output.Inner);
        if (error is not null) Console.SetError(error.Inner);
    }

    /// <summary>Adds a line, a line for each of its lines, and nothing for no text.</summary>
    public static void Write(ConsoleLevel level, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        Model.Write(level, text);
    }

    /// <summary>Adds a line at the ordinary level.</summary>
    public static void Write(string text) => Write(ConsoleLevel.Info, text);

    /// <summary>The lines kept, oldest first.</summary>
    public static IReadOnlyList<ConsoleLine> All() => Model.Lines();

    /// <summary>Forgets everything.</summary>
    public static void Clear() => Model.Clear();

    /// <summary>
    /// A writer that passes everything through and keeps a copy of each line.
    /// </summary>
    /// <remarks>
    /// Line by line rather than write by write, because a program that writes a word at a time
    /// means one line and a console showing each word on a row of its own is unreadable.
    /// </remarks>
    private sealed class Tee(TextWriter inner, ConsoleLevel level) : TextWriter
    {
        private readonly StringBuilder _pending = new();

        /// <summary>The stream it writes through to.</summary>
        public TextWriter Inner => inner;

        /// <inheritdoc/>
        public override Encoding Encoding => inner.Encoding;

        /// <inheritdoc/>
        public override void Write(char value)
        {
            inner.Write(value);

            if (value == '\r') return;

            if (value != '\n')
            {
                _pending.Append(value);
                return;
            }

            ConsoleLog.Write(level, _pending.ToString());
            _pending.Clear();
        }

        /// <inheritdoc/>
        public override void Write(string? value)
        {
            if (value is null) return;

            foreach (var character in value) Write(character);
        }

        /// <inheritdoc/>
        public override void WriteLine(string? value)
        {
            Write(value);
            Write('\n');
        }

        /// <inheritdoc/>
        public override void Flush() => inner.Flush();
    }
}

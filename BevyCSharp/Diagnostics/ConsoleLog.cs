using System.Text;

namespace Bevy;

/// <summary>
/// What the program has been saying, kept so something can show it.
/// </summary>
/// <remarks>
/// <para>
/// Everything written to the standard output and error streams is teed into a ring here, so a
/// console can put it on screen without anything that writes a line having to know that a console
/// exists. Writing through <see cref="Write(LogLevel, string)"/> says the level outright; a line that arrives through
/// the streams is an error if it came from the error stream and information otherwise.
/// </para>
/// <para>
/// A ring rather than a list, because a program left running all day writes a great many lines and
/// the interesting ones are always the last few. Repeats collapse, because the line that repeats
/// every frame is the one that would otherwise push everything else off the end.
/// </para>
/// </remarks>
public static class ConsoleLog
{
    /// <summary>How many lines are kept.</summary>
    public const int Depth = 2000;

    private static readonly object Gate = new();
    private static readonly List<LogLine> Lines = [];

    /// <summary>How many lines have ever been written, so a reader can notice new ones.</summary>
    public static int Written { get; private set; }

    /// <summary>What the frame counter says, for whoever writes a line next.</summary>
    /// <remarks>
    /// Set by whatever is driving the program. A log that had to ask the engine what frame it is
    /// would be a log that cannot be written to from a thread, or before there is an engine.
    /// </remarks>
    public static ulong Frame { get; set; }

    /// <summary>Starts teeing the output and error streams into the ring.</summary>
    /// <remarks>
    /// Idempotent, and harmless if it is never called, since the ring stays empty and whatever
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

            _output = new Tee(Console.Out, LogLevel.Info);
            _error = new Tee(Console.Error, LogLevel.Error);
        }

        Console.SetOut(_output);
        Console.SetError(_error);
    }

    private static Tee? _output;
    private static Tee? _error;

    /// <summary>Whether the streams are teed into the ring.</summary>
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

    /// <summary>
    /// Told of each line as it is added, with whether it repeats the one before, as the run's log
    /// file is (<see cref="CrashLog"/>).
    /// </summary>
    /// <remarks>Told outside the ring's lock, on whichever thread wrote the line.</remarks>
    internal static event Action<LogLine, bool>? Added;

    /// <summary>Adds a line.</summary>
    public static void Write(LogLevel level, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        LogLine added;
        bool repeat;
        lock (Gate)
        {
            // A repeat of the last line is counted rather than added. What produces one is a
            // warning inside something that runs every frame, and sixty copies of it a second is
            // a log that holds nothing else.
            repeat = Lines.Count > 0 && Lines[^1] is { } last && last.Level == level && last.Text == text;
            if (repeat)
            {
                added = Lines[^1] = Lines[^1] with { Count = Lines[^1].Count + 1, Frame = Frame };
            }
            else
            {
                added = new LogLine(Written, Frame, level, text);
                Lines.Add(added);
                if (Lines.Count > Depth) Lines.RemoveRange(0, Lines.Count - Depth);
            }

            Written++;
        }

        Added?.Invoke(added, repeat);
    }

    /// <summary>Adds a line at the ordinary level.</summary>
    public static void Write(string text) => Write(LogLevel.Info, text);

    /// <summary>The lines kept, oldest first.</summary>
    public static LogLine[] All()
    {
        lock (Gate) return [.. Lines];
    }

    /// <summary>Forgets everything.</summary>
    public static void Clear()
    {
        lock (Gate) Lines.Clear();
    }

    /// <summary>
    /// A writer that passes everything through and keeps a copy of each line.
    /// </summary>
    /// <remarks>
    /// Line by line rather than write by write, because a program that writes a word at a time
    /// means one line and a console showing each word on a row of its own is unreadable.
    /// </remarks>
    private sealed class Tee(TextWriter inner, LogLevel level) : TextWriter
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

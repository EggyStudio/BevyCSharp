using System.Text;

namespace Bevy;

/// <summary>How loudly a line was said.</summary>
/// <remarks>
/// Four kinds and no more. What a person does with a console is scan it for the lines that matter,
/// and every extra kind is another decision at the moment of writing and another thing to filter.
/// </remarks>
public enum LogLevel
{
    /// <summary>Something happened.</summary>
    Info,

    /// <summary>Something happened that probably should not have.</summary>
    Warning,

    /// <summary>Something did not happen that should have.</summary>
    Error,

    /// <summary>What somebody typed, and what answered them.</summary>
    Echo,
}

/// <summary>
/// One line of the log.
/// </summary>
/// <param name="Index">Which line it is, counted from the first ever written.</param>
/// <param name="Frame">The frame it was written on.</param>
/// <param name="Level">How loudly it was said.</param>
/// <param name="Text">What it says.</param>
/// <param name="Count">
/// How many times in a row it has been said. A line repeated every frame is one line with a number
/// beside it rather than a screenful of the same sentence.
/// </param>
public readonly record struct LogLine(
    int Index, ulong Frame, LogLevel Level, string Text, int Count = 1);

/// <summary>
/// What the program has been saying, kept so something can show it.
/// </summary>
/// <remarks>
/// <para>
/// Everything written to the standard output and error streams is teed into a ring here, so a
/// console can put it on screen without anything that writes a line having to know that a console
/// exists. Writing through <see cref="Write"/> says the level outright; a line that arrives through
/// the streams is an error if it came from the error stream and information otherwise.
/// </para>
/// <para>
/// A ring rather than a list: a program left running all day writes a great many lines and the
/// interesting ones are always the last few. Repeats collapse, because the line that repeats every
/// frame is the one that would otherwise push everything else off the end.
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
    /// Idempotent, and harmless if it is never called: the ring stays empty and whatever shows it
    /// says so.
    /// </remarks>
    public static void Start()
    {
        if (Console.Out is not Tee) Console.SetOut(new Tee(Console.Out, LogLevel.Info));
        if (Console.Error is not Tee) Console.SetError(new Tee(Console.Error, LogLevel.Error));
    }

    /// <summary>Adds a line.</summary>
    public static void Write(LogLevel level, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        lock (Gate)
        {
            // A repeat of the last line is counted rather than added. What produces one is a
            // warning inside something that runs every frame, and sixty copies of it a second is
            // a log that holds nothing else.
            if (Lines.Count > 0 && Lines[^1] is { } last
                && last.Level == level
                && last.Text == text)
            {
                Lines[^1] = last with { Count = last.Count + 1, Frame = Frame };
                Written++;
                return;
            }

            Lines.Add(new LogLine(Written, Frame, level, text));
            Written++;

            if (Lines.Count > Depth) Lines.RemoveRange(0, Lines.Count - Depth);
        }
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

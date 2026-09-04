using System.Text;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the editor has been saying, kept so a panel can show it.
/// </summary>
/// <remarks>
/// <para>
/// A script that does not compile reports why, and until now the why went to a terminal the person
/// running the editor may not be looking at. Everything written to the console is teed into a ring
/// here as well, so a panel can put it on screen without anything that writes a line having to
/// know that a panel exists.
/// </para>
/// <para>
/// A ring rather than a list: an editor left running all day writes a great many lines, and the
/// interesting ones are always the last few.
/// </para>
/// </remarks>
public static class EditorLog
{
    /// <summary>How many lines are kept.</summary>
    private const int Depth = 400;

    private static readonly object Gate = new();
    private static readonly List<string> Lines = [];

    /// <summary>How many lines have ever been written, so a panel can notice new ones.</summary>
    public static int Written { get; private set; }

    /// <summary>Starts teeing the console into the ring.</summary>
    /// <remarks>
    /// Idempotent, and harmless if it is never called: the ring is simply empty and the panel
    /// showing it says so.
    /// </remarks>
    public static void Start()
    {
        if (Console.Out is TeeWriter) return;

        Console.SetOut(new TeeWriter(Console.Out));
    }

    /// <summary>Adds a line.</summary>
    public static void Add(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        lock (Gate)
        {
            Lines.Add(line);
            Written++;

            if (Lines.Count > Depth) Lines.RemoveRange(0, Lines.Count - Depth);
        }
    }

    /// <summary>The lines kept, oldest first.</summary>
    public static string[] All()
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
    /// Lines rather than writes: <c>Console.WriteLine</c> reaches a writer as several calls, so
    /// the pieces are gathered until a newline arrives and only then does the ring get a line.
    /// </remarks>
    private sealed class TeeWriter(TextWriter inner) : TextWriter
    {
        private readonly StringBuilder _pending = new();

        /// <inheritdoc/>
        public override Encoding Encoding => inner.Encoding;

        /// <inheritdoc/>
        public override void Write(char value)
        {
            inner.Write(value);

            if (value == '\n')
            {
                Add(_pending.ToString().TrimEnd('\r'));
                _pending.Clear();
                return;
            }

            _pending.Append(value);
        }

        /// <inheritdoc/>
        public override void Write(string? value)
        {
            if (value is null) return;

            foreach (var character in value) Write(character);
        }

        /// <inheritdoc/>
        public override void Flush() => inner.Flush();
    }
}

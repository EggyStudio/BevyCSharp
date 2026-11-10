namespace Bevy;

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

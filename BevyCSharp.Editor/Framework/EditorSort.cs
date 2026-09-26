namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Puts names in the order a person reading them expects.
/// </summary>
/// <remarks>
/// Ordinary string order compares digits one character at a time, which puts Cube 10 before Cube 2
/// and scatters a numbered run across the list. Since the editor numbers what it spawns, and a
/// person numbering files by hand does the same thing, a run of digits is compared as the number it
/// spells instead.
/// </remarks>
public static class EditorSort
{
    /// <summary>Compares two names, reading each run of digits in them as one number.</summary>
    /// <param name="a">One name.</param>
    /// <param name="b">The other.</param>
    public static int Naturally(string? a, string? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        int here = 0, there = 0;

        while (here < a.Length && there < b.Length)
        {
            if (char.IsAsciiDigit(a[here]) && char.IsAsciiDigit(b[there]))
            {
                var mine = Number(a, ref here);
                var yours = Number(b, ref there);

                if (mine != yours) return mine.CompareTo(yours);
                continue;
            }

            var one = char.ToUpperInvariant(a[here]);
            var two = char.ToUpperInvariant(b[there]);

            if (one != two) return one.CompareTo(two);

            here++;
            there++;
        }

        // The shorter of two names that agree so far is the earlier one.
        var left = a.Length - here;
        var right = b.Length - there;

        if (left != right) return left.CompareTo(right);

        // Everything read the same, so fall back to something that never calls two different names
        // equal, or a sorted list would drop one of them in an order that changes between frames.
        return string.CompareOrdinal(a, b);
    }

    /// <summary>Comparing by this, for the calls that take a comparer rather than a
    /// method.</summary>
    public static IComparer<string> Comparer { get; } = Comparer<string>.Create(Naturally!);

    /// <summary>
    /// The number a run of digits spells, leaving the reader after it.
    /// </summary>
    /// <remarks>
    /// Read as a value with a cap rather than parsed, because a name is allowed to hold a hundred
    /// digits and nothing here should throw over one that does. Past the cap the numbers compare by
    /// how long they are, which is the same answer for anything a person would write.
    /// </remarks>
    /// <param name="text">What is being read.</param>
    /// <param name="at">Where the digits start, left after the last of them.</param>
    private static long Number(string text, ref int at)
    {
        // Leading zeroes say nothing about how big a number is, so 007 and 7 sort together.
        while (at < text.Length && text[at] == '0') at++;

        long value = 0;
        var digits = 0;

        while (at < text.Length && char.IsAsciiDigit(text[at]))
        {
            if (digits < 18) value = (value * 10) + (text[at] - '0');

            digits++;
            at++;
        }

        return digits > 18 ? long.MaxValue : value;
    }
}

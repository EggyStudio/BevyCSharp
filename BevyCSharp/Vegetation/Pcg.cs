namespace Bevy;

/// <summary>
/// The random numbers a tree grows from, O'Neill's PCG of 2014 with 64 bits of state and 32 out,
/// written here so the same seed gives the same numbers on every runtime.
/// </summary>
/// <remarks>
/// <see cref="Random"/> is no promise across versions of .NET, which have changed its numbers for a
/// seed before, and a tree kept in a level by its seed has to grow the same branches wherever the
/// level is opened. This generator is a few lines of integer arithmetic whose every step is fixed
/// by its paper, so its numbers are the same everywhere, and what can still differ between
/// machines is the floating point the growth does with them, which is why a level may keep the grown
/// skeleton beside the seed.
/// </remarks>
internal struct Pcg
{
    private const ulong Multiplier = 6364136223846793005ul;
    private const ulong Increment = 1442695040888963407ul;

    private ulong _state;

    /// <summary>A generator started from a seed, as the paper's reference starts one.</summary>
    public Pcg(ulong seed)
    {
        _state = 0;
        Next();
        _state += seed;
        Next();
    }

    /// <summary>The next 32 bits.</summary>
    public uint Next()
    {
        var old = _state;
        _state = old * Multiplier + Increment;
        var shifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return (shifted >> rotation) | (shifted << (-rotation & 31));
    }

    /// <summary>A number from zero up to one, one never reached, from the top 24 bits so every value is a float's exactly.</summary>
    public float Unit() => (Next() >> 8) * (1f / 16_777_216f);

    /// <summary>A number from minus one up to one.</summary>
    public float Signed() => Unit() * 2f - 1f;

    /// <summary>A number from zero up to <paramref name="count"/>, never reaching it.</summary>
    public int Below(int count) => (int)((ulong)Next() * (uint)count >> 32);
}

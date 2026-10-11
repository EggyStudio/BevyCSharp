namespace Bevy;

/// <summary>
/// Reads the messages of type <typeparamref name="T"/> it has not read yet and changes them in
/// place, as Bevy's <c>MessageMutator</c> does, so the systems reading after it read them changed.
/// Only valid inside a system, on the main thread.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <remarks>
/// It keeps its own place, as a <see cref="MessageReader{T}"/> does. A message sent while the
/// messages read are being changed is read on the next call, and not through the ones in hand,
/// which may no longer be where it was put, so a system sends what it read only once it is done
/// changing them, as Bevy's example does.
/// </remarks>
/// <example>
/// <code>
/// var armor = new MessageMutator&lt;DealDamage&gt;();
/// app.Update(ctx =>
/// {
///     foreach (ref var dealt in armor.Read(ctx)) dealt.Amount -= 1;
/// }, "ApplyArmor");
/// </code>
/// </example>
public sealed class MessageMutator<T> where T : notnull
{
    private long _cursor;

    /// <summary>The messages this mutator has not read yet, oldest first, in place, and the mutator moved past them.</summary>
    public Unread Read(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ctx.Messages.MutatePast<T>(ref _cursor, out var older, out var newer);
        return new Unread(older, newer);
    }

    /// <summary>The messages one call read, walked with <c>foreach (ref var message in ...)</c>.</summary>
    public readonly ref struct Unread
    {
        private readonly Span<T> _older;
        private readonly Span<T> _newer;

        internal Unread(Span<T> older, Span<T> newer)
        {
            _older = older;
            _newer = newer;
        }

        /// <summary>How many there are.</summary>
        public int Length => _older.Length + _newer.Length;

        /// <summary>Walks them oldest first.</summary>
        public Enumerator GetEnumerator() => new(_older, _newer);
    }

    /// <summary>Walks the messages one call read, each by reference.</summary>
    public ref struct Enumerator
    {
        private readonly Span<T> _older;
        private readonly Span<T> _newer;
        private int _index;

        internal Enumerator(Span<T> older, Span<T> newer)
        {
            _older = older;
            _newer = newer;
            _index = -1;
        }

        /// <summary>The message the walk is at.</summary>
        public readonly ref T Current => ref _index < _older.Length ? ref _older[_index] : ref _newer[_index - _older.Length];

        /// <summary>Moves to the next message, answering whether there is one.</summary>
        public bool MoveNext() => ++_index < _older.Length + _newer.Length;
    }
}

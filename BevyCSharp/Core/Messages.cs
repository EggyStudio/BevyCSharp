using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// A broadcast channel between systems, one queue per message type.
/// </summary>
/// <remarks>
/// <para>
/// Components say what an entity <em>is</em> and resources what the world <em>has</em>. Neither
/// says what just <em>happened</em>, which is why a collision or a button press otherwise ends up
/// as a component invented to carry it, on an entity invented to hold that. A message is sent by
/// one system and read by any number of others, none of which need know about each other.
/// </para>
/// <para>
/// <b>A reader sees the previous frame's messages.</b> The queue is swapped once at the top of
/// each frame, so every reader sees the same complete set, exactly once, whatever stage it runs
/// in and whatever order the systems happen to run. That costs a frame of latency: a message sent
/// during a frame is not readable until the next one, including by the sender.
/// </para>
/// <para>
/// This differs from Bevy's own messages, which give each reader a cursor and let it catch up
/// within the frame. A cursor needs a stable identity per reader, and a C# system has none the
/// engine can see, so the swap is what makes "exactly once" true here instead.
/// </para>
/// <para>
/// <b>Threading.</b> <see cref="Send{T}"/> is safe from a parallel behavior method, like
/// <see cref="EcsCommands"/>. Reading is not: the readable set belongs to the frame and is
/// replaced between frames on the main thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public readonly record struct Collided(Entity A, Entity B);
///
/// ctx.Send(new Collided(a, b));                 // in one system
///
/// foreach (var hit in ctx.Read&lt;Collided&gt;())     // in any number of others, next frame
///     Console.WriteLine($"{hit.A} hit {hit.B}");
/// </code>
/// </example>
public sealed class MessageBus
{
    private readonly ConcurrentDictionary<Type, object> _channels = new();

    /// <summary>Sends a message, for every reader to see next frame.</summary>
    public void Send<T>(T message) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        ChannelFor<T>().Send(message);
    }

    /// <summary>Sends several messages at once.</summary>
    public void SendAll<T>(ReadOnlySpan<T> messages) where T : notnull
    {
        var channel = ChannelFor<T>();
        foreach (var message in messages) channel.Send(message);
    }

    /// <summary>The messages of type <typeparamref name="T"/> sent during the previous frame.</summary>
    /// <remarks>
    /// The span is valid until the next frame swaps the queue, so read it inside the system that
    /// asked for it rather than holding on to it.
    /// </remarks>
    public ReadOnlySpan<T> Read<T>() where T : notnull => ChannelFor<T>().Readable;

    /// <summary>How many messages of type <typeparamref name="T"/> are readable this frame.</summary>
    public int Count<T>() where T : notnull => ChannelFor<T>().Readable.Length;

    /// <summary>True when nothing of type <typeparamref name="T"/> arrived.</summary>
    public bool IsEmpty<T>() where T : notnull => Count<T>() == 0;

    /// <summary>Messages sent this frame, which are not readable yet.</summary>
    public int PendingCount
    {
        get
        {
            var total = 0;
            foreach (var channel in _channels.Values) total += ((IChannel)channel).PendingCount;
            return total;
        }
    }

    /// <summary>
    /// Makes this frame's messages readable and starts a new frame's queue.
    /// </summary>
    /// <remarks>
    /// Run once at the top of each frame by the engine. Anything sent before the first swap is
    /// therefore readable from the first frame that follows it, which includes messages sent
    /// during <see cref="Stage.Startup"/>.
    /// </remarks>
    public void Swap()
    {
        foreach (var channel in _channels.Values) ((IChannel)channel).Swap();
    }

    /// <summary>Discards everything, sent and readable alike.</summary>
    public void Clear()
    {
        foreach (var channel in _channels.Values) ((IChannel)channel).Clear();
    }

    private Channel<T> ChannelFor<T>() where T : notnull =>
        (Channel<T>)_channels.GetOrAdd(typeof(T), static _ => new Channel<T>());

    /// <summary>The per-type operations the bus performs without knowing the type.</summary>
    private interface IChannel
    {
        int PendingCount { get; }

        void Swap();

        void Clear();
    }

    /// <summary>One message type's pair of queues: the one being filled, and the one being read.</summary>
    private sealed class Channel<T> : IChannel where T : notnull
    {
        private readonly object _gate = new();
        private List<T> _incoming = [];
        private List<T> _readable = [];

        public ReadOnlySpan<T> Readable => CollectionsMarshal.AsSpan(_readable);

        public int PendingCount
        {
            get
            {
                lock (_gate) return _incoming.Count;
            }
        }

        public void Send(T message)
        {
            lock (_gate) _incoming.Add(message);
        }

        public void Swap()
        {
            lock (_gate)
            {
                // The list just read is reused for the incoming side rather than allocated
                // again, so a steady stream of messages settles into two lists and stays there.
                (_readable, _incoming) = (_incoming, _readable);
                _incoming.Clear();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _incoming.Clear();
                _readable.Clear();
            }
        }
    }
}

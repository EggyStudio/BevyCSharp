using System.Collections.Concurrent;

namespace Bevy;

/// <summary>A read on its way from disk, from <see cref="Streaming.Read"/>.</summary>
/// <param name="Ticket">What the read is known by.</param>
public readonly record struct StreamRead(long Ticket);

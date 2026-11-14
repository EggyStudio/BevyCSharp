namespace Bevy;

/// <summary>
/// The files the app read again lately, kept for <c>live.status</c> from the
/// <see cref="AssetReloaded"/> messages each frame carries.
/// </summary>
/// <remarks>
/// A resource of the world, so each app keeps its own. The latest are kept with the frame each was
/// read in, older ones giving way as a log's tail does, and the count of all of them beside.
/// </remarks>
internal sealed class LiveReloads
{
    /// <summary>How many of the latest are kept.</summary>
    private const int Kept = 32;

    private readonly Queue<(ulong Frame, AssetReloaded Reload)> _latest = new();

    /// <summary>How many files the app read again since it started.</summary>
    internal int Count { get; private set; }

    /// <summary>The latest, oldest first, with the frame each was read in.</summary>
    internal IReadOnlyCollection<(ulong Frame, AssetReloaded Reload)> Latest => _latest;

    /// <summary>Keeps the reloads the last frame posted. Called by the app each frame.</summary>
    /// <remarks>The bus hands a reader the last frame's messages, so each is kept under that frame.</remarks>
    internal void Read(World world)
    {
        var frame = world.Resource<Time>().FrameCount;
        foreach (var reload in world.Resource<MessageBus>().Read<AssetReloaded>())
        {
            _latest.Enqueue((frame == 0 ? 0 : frame - 1, reload));
            Count++;
            while (_latest.Count > Kept) _latest.Dequeue();
        }
    }
}

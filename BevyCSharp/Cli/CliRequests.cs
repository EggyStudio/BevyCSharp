using System.Collections.Concurrent;

namespace Bevy;

/// <summary>
/// One thing a caller asked for, and somewhere to put the answer.
/// </summary>
/// <remarks>
/// Made on the listening thread and answered on the main one, because everything a command can
/// reach belongs to the frame. The waiting thread holds the task; the frame completes it.
/// </remarks>
internal sealed class CliRequest(string operation, string? line, string? id)
{
    private readonly TaskCompletionSource<string> _answer =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>What was asked: <c>run</c>, <c>list</c>, <c>status</c> or <c>ping</c>.</summary>
    public string Operation { get; } = operation;

    /// <summary>The command line to run, for <c>run</c>.</summary>
    public string? Line { get; } = line;

    /// <summary>The caller's correlation id, echoed back.</summary>
    public string? Id { get; } = id;

    /// <summary>The answer, once a frame has produced one.</summary>
    public Task<string> Answer => _answer.Task;

    /// <summary>The command whose answer is being held, if one is.</summary>
    public string? Holding { get; set; }

    /// <summary>What it answered, to be handed back once its frame arrives.</summary>
    public string? Held { get; set; }

    /// <summary>The frame that releases it.</summary>
    public ulong Release { get; set; }

    /// <summary>Hands the answer back to whoever is waiting.</summary>
    public void Complete(string envelope) => _answer.TrySetResult(envelope);
}

/// <summary>
/// What has been asked for and not yet answered.
/// </summary>
/// <remarks>
/// <para>
/// The listening thread only ever adds; the frame only ever takes. Nothing a request touches is
/// read off the main thread, which is what makes serving an app safe while it runs: the socket
/// never sees the world, and the world never waits on the socket.
/// </para>
/// <para>
/// A command that asked to be held keeps its place here until its frame arrives, so a caller
/// waiting on "five frames from now" costs a list entry rather than a blocked frame.
/// </para>
/// </remarks>
internal sealed class CliQueue
{
    private readonly ConcurrentQueue<CliRequest> _arrived = new();
    private readonly List<CliRequest> _held = [];

    /// <summary>Adds one, from whichever thread took the call.</summary>
    public void Add(CliRequest request) => _arrived.Enqueue(request);

    /// <summary>
    /// Answers everything that arrived, and releases anything whose frame has come.
    /// </summary>
    /// <remarks>
    /// Run from a system, so <paramref name="world"/> is the one Bevy lent this frame.
    /// </remarks>
    public void Pump(World world)
    {
        while (_arrived.TryDequeue(out var request))
        {
            var envelope = CliDispatch.Answer(request, world);

            if (envelope is null) _held.Add(request);
            else request.Complete(envelope);
        }

        if (_held.Count == 0) return;

        var frame = world.Resource<Time>().FrameCount;

        for (var index = _held.Count - 1; index >= 0; index--)
        {
            var request = _held[index];
            if (frame < request.Release) continue;

            request.Complete(CliDispatch.Release(request, frame));
            _held.RemoveAt(index);
        }
    }

    /// <summary>Answers everything outstanding, on the way down.</summary>
    /// <remarks>
    /// A caller waiting on a session that is closing gets a sentence rather than a timeout, which
    /// is the difference between "it shut down" and "something is wrong with it".
    /// </remarks>
    public void Abandon()
    {
        while (_arrived.TryDequeue(out var request))
        {
            request.Complete(CliJson.Fail(
                request.Operation, "SESSION_CLOSING", "The app is shutting down.", request.Id));
        }

        foreach (var request in _held)
        {
            request.Complete(CliJson.Fail(
                request.Operation,
                "SESSION_CLOSING",
                "The app shut down before the frame this was waiting for.",
                request.Id));
        }

        _held.Clear();
    }
}

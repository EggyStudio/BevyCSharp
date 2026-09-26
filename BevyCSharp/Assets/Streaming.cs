using System.Collections.Concurrent;

namespace Bevy;

/// <summary>A read on its way from disk, from <see cref="Streaming.Read"/>.</summary>
/// <param name="Ticket">What the read is known by.</param>
public readonly record struct StreamRead(long Ticket);

/// <summary>
/// Reads parts of files on worker threads and hands them over a few at a time, within a budget of
/// bytes a frame.
/// </summary>
/// <remarks>
/// <para>
/// Streaming textures or geometry is reading many small pieces of large files while a game runs:
/// the tiles or clusters a frame found missing, requested by the GPU and uploaded as they arrive.
/// Reading them on the frame's thread stalls the frame on the disk, and handing them all over the
/// frame they finish turns a burst of finished reads into a burst of uploads, which is a hitch of
/// its own. So reads run on the thread pool, and <see cref="TryTake"/> hands finished ones over
/// until <see cref="BytesPerFrame"/> have been handed over this frame; the rest wait for the next.
/// </para>
/// <para>
/// A read higher in <c>priority</c> is handed over first among the finished ones, since what is
/// on screen matters more than what is about to be. Paths are relative to <see cref="Root"/>,
/// which is the asset directory unless set otherwise. Every call is safe from any thread.
/// </para>
/// </remarks>
public static class Streaming
{
    private sealed record Finished(byte[]? Bytes, Exception? Error, int Priority);

    private static readonly ConcurrentDictionary<long, Finished> Done = new();
    private static readonly ConcurrentDictionary<long, CancellationTokenSource> Running = new();
    private static long _next;
    private static long _takenThisFrame;

    /// <summary>
    /// How many bytes <see cref="TryTake"/> hands over in one frame before the rest wait, which
    /// bounds what a frame spends on uploading what arrived. The first read taken in a frame is
    /// always handed over, however large, so a read larger than the budget still arrives.
    /// </summary>
    public static long BytesPerFrame { get; set; } = 16L * 1024 * 1024;

    /// <summary>
    /// The directory paths are read relative to. Empty uses the asset directory the app was made
    /// with, or the working directory before there is an app.
    /// </summary>
    public static string Root { get; set; } = string.Empty;

    /// <summary>How many reads are still on their way.</summary>
    public static int Pending => Running.Count;

    /// <summary>
    /// Starts reading <paramref name="length"/> bytes at <paramref name="offset"/> of a file on a
    /// worker thread, and answers the read at once.
    /// </summary>
    /// <param name="path">The file, relative to <see cref="Root"/> or absolute.</param>
    /// <param name="offset">Where in the file to start.</param>
    /// <param name="length">How many bytes, or a negative number for the rest of the file.</param>
    /// <param name="priority">Higher is handed over first among finished reads.</param>
    public static StreamRead Read(string path, long offset = 0, int length = -1, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        var ticket = Interlocked.Increment(ref _next);
        var full = Path.IsPathRooted(path) ? path : Path.Combine(ResolvedRoot(), path);
        var cancel = new CancellationTokenSource();

        Running[ticket] = cancel;

        _ = Task.Run(async () =>
        {
            try
            {
                using var file = File.OpenHandle(full, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
                var available = RandomAccess.GetLength(file) - offset;
                var wanted = length < 0 ? available : Math.Min(length, available);
                var bytes = new byte[Math.Max(0, wanted)];
                var read = 0;

                // A read can come back short, so it is asked for again until it has everything.
                while (read < bytes.Length)
                {
                    var got = await RandomAccess.ReadAsync(file, bytes.AsMemory(read), offset + read, cancel.Token);
                    if (got == 0) break;
                    read += got;
                }

                Done[ticket] = new Finished(read == bytes.Length ? bytes : bytes[..read], null, priority);
            }
            catch (OperationCanceledException)
            {
                // Cancelled, so nobody is waiting for it.
            }
            catch (Exception error)
            {
                Done[ticket] = new Finished(null, error, priority);
            }
            finally
            {
                Running.TryRemove(ticket, out _);
                cancel.Dispose();
            }
        });

        return new StreamRead(ticket);
    }

    /// <summary>
    /// Takes what a read brought back, if it has finished, fits this frame's budget, and no
    /// finished read of higher priority is waiting.
    /// </summary>
    /// <returns>
    /// True with the bytes once it is handed over, and false while it is still on its way or waits
    /// for a later frame.
    /// </returns>
    /// <exception cref="IOException">The file could not be read; the read is forgotten.</exception>
    public static bool TryTake(StreamRead read, out byte[]? bytes)
    {
        bytes = null;

        if (!Done.TryGetValue(read.Ticket, out var finished)) return false;

        if (finished.Error is { } error)
        {
            Done.TryRemove(read.Ticket, out _);
            throw new IOException($"Streaming read {read.Ticket} failed: {error.Message}", error);
        }

        var size = finished.Bytes!.LongLength;
        var taken = Interlocked.Read(ref _takenThisFrame);

        if (taken > 0 && taken + size > BytesPerFrame) return false;

        // Something more urgent that has also finished goes first, so this waits.
        if (Done.Any(entry => entry.Key != read.Ticket && entry.Value.Error is null && entry.Value.Priority > finished.Priority))
        {
            return false;
        }

        if (!Done.TryRemove(read.Ticket, out _)) return false;

        Interlocked.Add(ref _takenThisFrame, size);
        bytes = finished.Bytes;
        return true;
    }

    /// <summary>Stops a read, or forgets one that finished and was never taken.</summary>
    public static void Cancel(StreamRead read)
    {
        if (Running.TryGetValue(read.Ticket, out var cancel))
        {
            try
            {
                cancel.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Finished between the look and the cancel.
            }
        }

        Done.TryRemove(read.Ticket, out _);
    }

    /// <summary>Starts a new frame's budget. Called by the engine before any system runs.</summary>
    internal static void BeginFrame() => Interlocked.Exchange(ref _takenThisFrame, 0);

    /// <summary>The asset directory the running app reads from, which paths are relative
    /// to.</summary>
    internal static string AssetRoot { get; set; } = string.Empty;

    private static string ResolvedRoot() =>
        Root.Length > 0 ? Root : AssetRoot.Length > 0 ? AssetRoot : Directory.GetCurrentDirectory();
}

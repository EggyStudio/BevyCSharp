using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Bevy;

/// <summary>
/// One file holding a game's assets, read in place of the asset folder, for a game whose assets are
/// too large to compile into its assembly.
/// </summary>
/// <remarks>
/// <para>
/// A shipped game can carry its assets as a folder, inside its assembly, or in a pack. A folder is
/// thousands of files a player can lose or change, and an assembly holds everything in memory once
/// it loads, which suits a game of modest size and no other. A pack is one file read a part at a
/// time, so a game of any size ships as the executable, the bridge and the pack.
/// </para>
/// <para>
/// The format is an index followed by the files' bytes, in that order, so opening a pack reads the
/// index alone. It starts with the eight bytes <c>BCSPACK</c> and a zero, then the format's version
/// and the number of files as little-endian 32-bit integers, then for each file its path under the
/// asset root (a 16-bit length and UTF-8, with forward slashes), and where its bytes start and how
/// many there are, as little-endian 64-bit integers counted from the start of the pack. Nothing is
/// compressed, so a part of a file is read without the rest, as <see cref="Streaming"/> reads one,
/// and a texture already compressed for the GPU is not compressed twice.
/// </para>
/// <para>
/// Every read is positional on one handle (<see cref="RandomAccess"/>), so the threads Bevy loads
/// on read their files at once without a lock or a handle each.
/// </para>
/// </remarks>
public sealed class AssetPack : IDisposable
{
    /// <summary>The name a game's pack has beside its executable, where an app looks for one.</summary>
    public const string DefaultName = "assets.pack";

    /// <summary>The first bytes of every pack.</summary>
    private static ReadOnlySpan<byte> Magic => "BCSPACK\0"u8;

    /// <summary>The format's version, raised when the layout changes.</summary>
    private const int Version = 1;

    private readonly SafeFileHandle _handle;
    private readonly Dictionary<string, (long Offset, long Length)> _files;

    private AssetPack(string path, SafeFileHandle handle, Dictionary<string, (long Offset, long Length)> files)
    {
        Path = path;
        _handle = handle;
        _files = files;
    }

    /// <summary>Where the pack is.</summary>
    public string Path { get; }

    /// <summary>The path of every file the pack holds, under the asset root, with forward slashes.</summary>
    public IReadOnlyCollection<string> Files => _files.Keys;

    /// <summary>Opens a pack and reads its index.</summary>
    /// <param name="path">The pack.</param>
    /// <exception cref="InvalidDataException">The file is not a pack, or not one this build reads.</exception>
    public static AssetPack Open(string path)
    {
        // The index through a stream of its own, since a stream made over the handle would close
        // it when disposed, and the handle is kept for the reads that come after.
        var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            Span<byte> magic = stackalloc byte[8];
            if (reader.Read(magic) != magic.Length || !magic.SequenceEqual(Magic))
                throw new InvalidDataException($"{path} is not an asset pack.");

            var version = reader.ReadInt32();
            if (version != Version)
                throw new InvalidDataException($"{path} is a pack of version {version}, and this build reads version {Version}.");

            var count = reader.ReadInt32();
            var size = RandomAccess.GetLength(handle);
            var files = new Dictionary<string, (long, long)>(count, StringComparer.Ordinal);

            for (var index = 0; index < count; index++)
            {
                var name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                var offset = reader.ReadInt64();
                var length = reader.ReadInt64();

                // A pack cut short, by a copy that stopped, would otherwise read past its end as
                // the file's last bytes rather than failing where the cause is.
                if (offset < 0 || length < 0 || offset + length > size)
                    throw new InvalidDataException($"{path} is cut short, since {name} runs past its end.");

                files[name] = (offset, length);
            }

            return new AssetPack(path, handle, files);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>Whether the pack holds a file.</summary>
    /// <param name="path">Under the asset root, with forward slashes.</param>
    public bool Contains(string path) => _files.ContainsKey(path);

    /// <summary>A file in the pack, opened for reading and seeking, or nothing when the pack lacks it.</summary>
    /// <param name="path">Under the asset root, with forward slashes.</param>
    /// <remarks>
    /// The stream shares the pack's handle and reads at its own position, so any number may be open
    /// at once on any threads. Disposing it leaves the pack open.
    /// </remarks>
    public Stream? OpenFile(string path) =>
        _files.TryGetValue(path, out var file) ? new PackedFile(_handle, file.Offset, file.Length) : null;

    /// <summary>
    /// Writes a pack of every file under a folder that <paramref name="include"/> takes.
    /// </summary>
    /// <param name="folder">The asset folder, whose paths the files are named by.</param>
    /// <param name="pack">Where to write the pack, replaced if it is there.</param>
    /// <param name="include">Which files go in, by full path, or nothing for all of them.</param>
    /// <returns>How many files the pack holds.</returns>
    /// <remarks>
    /// Written beside its place under another name and moved over it once whole, so a pack an app
    /// has open is never seen half written. The files go in by path, so the same folder always
    /// makes the same pack.
    /// </remarks>
    public static int Write(string folder, string pack, Func<string, bool>? include = null)
    {
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => include?.Invoke(file) ?? true)
            .Select(file => (Full: file, Name: System.IO.Path.GetRelativePath(folder, file).Replace('\\', '/')))
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .ToList();

        var names = files.Select(file => Encoding.UTF8.GetBytes(file.Name)).ToList();
        foreach (var (name, bytes) in files.Select(file => file.Name).Zip(names))
        {
            if (bytes.Length > ushort.MaxValue) throw new PathTooLongException($"{name} is too long a path for a pack.");
        }

        var lengths = files.Select(file => new FileInfo(file.Full).Length).ToList();
        long offset = Magic.Length + sizeof(int) * 2 + names.Sum(name => sizeof(ushort) + name.Length + sizeof(long) * 2);

        var partial = pack + ".partial";
        using (var stream = File.Create(partial))
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(files.Count);

            for (var index = 0; index < files.Count; index++)
            {
                writer.Write((ushort)names[index].Length);
                writer.Write(names[index]);
                writer.Write(offset);
                writer.Write(lengths[index]);
                offset += lengths[index];
            }

            writer.Flush();

            foreach (var file in files)
            {
                using var source = File.OpenRead(file.Full);
                source.CopyTo(stream);
            }
        }

        File.Move(partial, pack, overwrite: true);
        return files.Count;
    }

    /// <summary>Closes the pack. A stream still open on it fails its next read.</summary>
    public void Dispose() => _handle.Dispose();

    /// <summary>One file of a pack, read at positions of its own over the pack's handle.</summary>
    private sealed class PackedFile(SafeFileHandle handle, long start, long length) : Stream
    {
        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => _position = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var left = length - _position;
            if (left <= 0) return 0;

            var read = RandomAccess.Read(handle, buffer[..(int)Math.Min(buffer.Length, left)], start + _position);
            _position += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var left = length - _position;
            if (left <= 0) return 0;

            var read = await RandomAccess.ReadAsync(handle, buffer[..(int)Math.Min(buffer.Length, left)], start + _position, cancellationToken);
            _position += read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) => Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            _ => length + offset,
        };

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

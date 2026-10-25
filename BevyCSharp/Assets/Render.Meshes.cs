using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Builds a mesh from vertices and returns a handle to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a shape no primitive describes, such as a terrain from a heightmap, a ribbon, a line of
    /// points, or a mesh of ten thousand quads whose vertex shader places each one from a buffer a
    /// compute shader writes. Built in any profile, since a mesh is data until something draws it.
    /// </para>
    /// <para>
    /// A triangle mesh given no normals has them worked out, smooth where it is indexed and flat
    /// where it is not, because every lit material and every shader reading a normal would
    /// otherwise read zeros, unless <see cref="MeshData.ComputeNormals"/> says not to. The attributes
    /// land where Bevy's shaders look for them, with positions
    /// at location zero, normals at one, UVs at two and colors at five, and each stream of the
    /// game's own at the location it names, for a vertex shader of the program's own to read.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// There are no positions, an array is the wrong length for the vertices, an index names no
    /// vertex, or a stream is outside the streams' locations, shares one with another, or has a
    /// name the running app gave a stream of another format or location.
    /// </exception>
    public static AssetHandle CreateMesh(MeshData mesh)
    {
        var key = Send(mesh, AssetHandle.None);
        Keep(key, mesh);
        return new AssetHandle(key);
    }

    /// <summary>Writes vertices over a mesh, so everything drawn with it changes. Only valid inside a system.</summary>
    /// <remarks>
    /// Bevy's way of changing a mesh in place, as a mesh deformed while the game runs is, rather
    /// than making a new one and pointing every entity at it. <see cref="TryReadMesh"/> reads what a
    /// mesh holds, so a loaded model can be read, changed and written back. What
    /// <see cref="CreateMesh(MeshData)"/> checks, this checks, and the mesh's recipe, if it was
    /// built from a primitive, is forgotten, since it no longer describes it.
    /// </remarks>
    /// <exception cref="ArgumentException">As for <see cref="CreateMesh(MeshData)"/>.</exception>
    /// <exception cref="BevyNativeException">The handle names no mesh, or this build has no renderer.</exception>
    public static void WriteMesh(AssetHandle mesh, MeshData data)
    {
        Send(data, mesh);
        Keep(mesh.Key, data);
    }

    /// <summary>
    /// Checks a mesh's vertices and hands them over, as a new mesh, or written over the one
    /// <paramref name="into"/> names.
    /// </summary>
    private static int Send(MeshData mesh, AssetHandle into)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var count = mesh.Positions.Length;

        if (count == 0)
            throw new ArgumentException("A mesh needs at least one position.", nameof(mesh));

        if (mesh.Normals is { } normals && normals.Length != count)
            throw new ArgumentException($"{normals.Length} normals for {count} vertices.", nameof(mesh));

        if (mesh.Uvs is { } uvs && uvs.Length != count * 2)
            throw new ArgumentException($"{uvs.Length} UV floats for {count} vertices, which is two each.", nameof(mesh));

        if (mesh.SecondUvs is { } second && second.Length != count * 2)
            throw new ArgumentException($"{second.Length} second UV floats for {count} vertices, which is two each.", nameof(mesh));

        if (mesh.Colors is { } colors && colors.Length != count * 4)
            throw new ArgumentException($"{colors.Length} color floats for {count} vertices, which is four each.", nameof(mesh));

        // A strip is broken where an index is the largest a uint holds, which starts it again from
        // the next, as the GPU reads it, and anywhere else that index names no vertex.
        var strip = mesh.Topology is MeshTopology.LineStrip or MeshTopology.TriangleStrip;
        if (mesh.Indices is { } indices && indices.Any(index => index >= count && !(strip && index == uint.MaxValue)))
            throw new ArgumentException($"An index names a vertex past the {count} there are.", nameof(mesh));

        var streams = mesh.Streams ?? [];
        CheckStreams(streams, count);

        // Each stream's name and bytes are held where they are for the call, which reads them and
        // copies what it keeps before it returns.
        var pinned = new List<GCHandle>(streams.Length * 2);
        try
        {
            var described = new NativeMeshStream[streams.Length];
            for (var i = 0; i < streams.Length; i++)
            {
                var name = Encoding.UTF8.GetBytes(streams[i].Name);
                pinned.Add(GCHandle.Alloc(name, GCHandleType.Pinned));
                pinned.Add(GCHandle.Alloc(streams[i].Data, GCHandleType.Pinned));
                described[i] = new NativeMeshStream
                {
                    Name = (byte*)pinned[^2].AddrOfPinnedObject(),
                    NameLength = name.Length,
                    Format = (int)streams[i].Format,
                    Location = streams[i].Location,
                    Data = (byte*)pinned[^1].AddrOfPinnedObject(),
                };
            }

            fixed (Vec3* positions = mesh.Positions)
            fixed (Vec3* normalsAt = mesh.Normals)
            fixed (float* uvsAt = mesh.Uvs)
            fixed (float* secondUvsAt = mesh.SecondUvs)
            fixed (float* colorsAt = mesh.Colors)
            fixed (uint* indicesAt = mesh.Indices)
            fixed (NativeMeshStream* streamsAt = described)
            {
                var native = new NativeMeshData
                {
                    Positions = (float*)positions,
                    VertexCount = count,
                    Normals = (float*)normalsAt,
                    Uvs = uvsAt,
                    Colors = colorsAt,
                    Indices = indicesAt,
                    IndexCount = mesh.Indices?.Length ?? 0,
                    Topology = (int)mesh.Topology,
                    Streams = streams.Length > 0 ? streamsAt : null,
                    StreamCount = streams.Length,
                    Flags = mesh.ComputeNormals ? 0u : 1u,
                    SecondUvs = secondUvsAt,
                };

                var status = into == AssetHandle.None
                    ? Native.bcs_mesh_create_from(&native)
                    : Native.bcs_mesh_write(into.Key, &native);

                if (status == NativeStatus.InvalidState) RefuseReshapedStream(streams);

                if (into == AssetHandle.None)
                    return Native.Check(status, $"building a mesh of {count} vertices");

                Native.Check(status, $"writing {count} vertices over the mesh {into}");
                return into.Key;
            }
        }
        finally
        {
            foreach (var handle in pinned) handle.Free();
        }
    }

    /// <summary>
    /// Checks a mesh's streams before they cross, each named, at a location of the streams' range no
    /// other takes, and the length its format and the vertices make.
    /// </summary>
    private static void CheckStreams(MeshStream[] streams, int count)
    {
        var locations = new HashSet<uint>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var stream in streams)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(MeshData.Streams));

            if (string.IsNullOrEmpty(stream.Name))
                throw new ArgumentException("A vertex stream needs a name.", nameof(MeshData.Streams));

            if (!Enum.IsDefined(stream.Format))
                throw new ArgumentException($"The vertex stream {stream.Name} has no format called {stream.Format}.", nameof(MeshData.Streams));

            if (stream.Location is < MeshStream.FirstLocation or > MeshStream.LastLocation)
            {
                throw new ArgumentException(
                    $"The vertex stream {stream.Name} is at location {stream.Location}, and a stream's is from "
                    + $"{MeshStream.FirstLocation} to {MeshStream.LastLocation}, since Bevy places its own attributes "
                    + "at 0 to 7 and wgpu allows 16.",
                    nameof(MeshData.Streams));
            }

            if (!locations.Add(stream.Location))
                throw new ArgumentException($"Two vertex streams are at location {stream.Location}.", nameof(MeshData.Streams));

            if (!names.Add(stream.Name))
                throw new ArgumentException($"Two vertex streams are called {stream.Name}.", nameof(MeshData.Streams));

            var wanted = (long)count * MeshStream.SizeOf(stream.Format);
            if (stream.Data is null || stream.Data.Length != wanted)
            {
                throw new ArgumentException(
                    $"The vertex stream {stream.Name} is {stream.Data?.Length ?? 0} bytes for {count} vertices, which "
                    + $"as {stream.Format} is {wanted}.",
                    nameof(MeshData.Streams));
            }
        }
    }

    /// <summary>
    /// Throws for the stream whose name the running app gave another format or location, where one
    /// was, so the bridge's refusal says which and what it was given before.
    /// </summary>
    private static void RefuseReshapedStream(MeshStream[] streams)
    {
        foreach (var stream in streams)
        {
            var name = Encoding.UTF8.GetBytes(stream.Name);
            int format;
            uint location;

            fixed (byte* at = name)
            {
                if (Native.bcs_mesh_stream_of(at, name.Length, &format, &location) != NativeStatus.Ok) continue;
            }

            if ((MeshStreamFormat)format == stream.Format && location == stream.Location) continue;

            throw new ArgumentException(
                $"The vertex stream {stream.Name} was given as {(MeshStreamFormat)format} at location {location}, and "
                + $"is given now as {stream.Format} at location {stream.Location}. A name is one stream for the "
                + "running app, which every mesh carrying it carries alike, so a stream of another shape takes "
                + "another name.",
                nameof(MeshData.Streams));
        }
    }

    /// <summary>
    /// Keeps a copy of the vertices a mesh was given, so the caller changing its arrays afterward
    /// does not change what a scene writes for a mesh already drawn.
    /// </summary>
    private static void Keep(int key, MeshData mesh)
    {
        var kept = new MeshData
        {
            Positions = [.. mesh.Positions],
            Normals = mesh.Normals is { } n ? [.. n] : null,
            Uvs = mesh.Uvs is { } u ? [.. u] : null,
            SecondUvs = mesh.SecondUvs is { } u2 ? [.. u2] : null,
            Colors = mesh.Colors is { } c ? [.. c] : null,
            Indices = mesh.Indices is { } i ? [.. i] : null,
            Topology = mesh.Topology,
            Streams = mesh.Streams is { } s ? [.. s.Select(stream => stream with { Data = [.. stream.Data] })] : null,
            ComputeNormals = mesh.ComputeNormals,
        };
        lock (Built) Built[key] = kept;
        lock (Recipes) Recipes.Remove(key);
    }
}

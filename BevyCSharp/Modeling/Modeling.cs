using System.Runtime.CompilerServices;
using Bevy.Physics;

namespace Bevy;

/// <summary>
/// Builds an <see cref="EditMesh"/> into what the engine draws and collides with, a mesh for each
/// material slot and a collider of its triangles.
/// </summary>
/// <remarks>
/// <para>
/// Each face is cut into triangles by ear clipping, so a concave face is cut inside its outline,
/// and keeps corners of its own, so its normal and its texture coordinates are its own where
/// faces meet at an angle. A flat face's corners take its normal, and a smoothed one's take the
/// mean of the normals of the smoothed faces round each corner within its angle, each weighted by
/// its area, so a cylinder's sides are round and its ends flat.
/// </para>
/// <para>
/// A face's texture coordinates are its corners' places in meters, seen along the axis its normal
/// is nearest, so a texture of a meter's grid shows a meter on every face and carries on across
/// faces meeting along a line, and a face moved shows the grid of the place it was moved to, as a
/// level's prototype grid should. <see cref="Meshes"/> works tangents out from them where the
/// bridge draws (<see cref="Render.GenerateTangents"/>), so a material with a normal map lights
/// the faces right.
/// </para>
/// </remarks>
public static partial class Modeling
{
    private static readonly ConditionalWeakTable<EditMesh, Drawn> Handles = new();

    // The edit mesh behind each mesh handle, held weakly, so a mesh a program let go of is not kept
    // for the handles it was drawn with.
    private static readonly Dictionary<AssetHandle, WeakReference<EditMesh>> Sources = [];

    /// <summary>The handles a mesh was drawn with and the version they were last written at.</summary>
    private sealed class Drawn
    {
        public AssetHandle[] Meshes = [];
        public int Version = -1;
    }

    /// <summary>Builds the mesh into one mesh's data for each material slot, in the order of <see cref="EditMesh.Slots"/>.</summary>
    /// <param name="mesh">The mesh.</param>
    /// <returns>The data, one for each slot, with no positions for a slot no face is in.</returns>
    public static MeshData[] Build(EditMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var slots = mesh.Slots.Count;
        var positions = new List<Vec3>[slots];
        var normals = new List<Vec3>[slots];
        var uvs = new List<float>[slots];
        var indices = new List<uint>[slots];
        for (var slot = 0; slot < slots; slot++)
        {
            positions[slot] = [];
            normals[slot] = [];
            uvs[slot] = [];
            indices[slot] = [];
        }

        var faceNormals = new Vec3[mesh.FaceCount];
        var around = new List<int>[mesh.VertexCount];
        for (var face = 0; face < mesh.FaceCount; face++)
        {
            faceNormals[face] = mesh.Area(face);
            foreach (var corner in mesh.Corners(face)) (around[corner] ??= []).Add(face);
        }

        for (var face = 0; face < mesh.FaceCount; face++)
        {
            var corners = mesh.Corners(face);
            var slot = mesh.SlotIndexOf(face);
            var normal = faceNormals[face].Normalized;
            var smooth = mesh.SmoothOf(face);
            var first = (uint)positions[slot].Count;
            var (u, v) = Projection(normal);

            var points = new Vec3[corners.Length];
            for (var i = 0; i < corners.Length; i++) points[i] = mesh.Position(corners[i]);

            for (var i = 0; i < corners.Length; i++)
            {
                var point = points[i];
                positions[slot].Add(point);
                normals[slot].Add(smooth > 0f ? Smoothed(corners[i], normal, smooth, faceNormals, around, mesh) : normal);
                uvs[slot].Add(Vec3.Dot(point, u));
                uvs[slot].Add(Vec3.Dot(point, v));
            }

            foreach (var index in EarClipping.Cut(points, faceNormals[face])) indices[slot].Add(first + (uint)index);
        }

        var built = new MeshData[slots];
        for (var slot = 0; slot < slots; slot++)
        {
            built[slot] = new MeshData
            {
                Positions = [.. positions[slot]],
                Normals = [.. normals[slot]],
                Uvs = [.. uvs[slot]],
                Indices = [.. indices[slot]],
            };
        }

        return built;
    }

    /// <summary>
    /// The meshes the mesh is drawn with, one for each material slot, made the first time and
    /// written again in place once it has changed, so an entity drawn with one follows the edits.
    /// Only valid inside a system.
    /// </summary>
    /// <param name="mesh">The mesh.</param>
    /// <returns>
    /// A handle for each slot, in the order of <see cref="EditMesh.Slots"/>, <see cref="AssetHandle.None"/>
    /// for a slot no face has been in yet.
    /// </returns>
    /// <remarks>
    /// The same handles each time for the same mesh, written with <see cref="Render.WriteMesh"/>
    /// when its <see cref="EditMesh.Version"/> moved since they were last written and left alone when
    /// it did not, so a system asks for them every frame for nothing. Each is given tangents after it
    /// is made or written, which a write takes away, so a normal map on a face reads a frame turned
    /// the way its texture runs. A slot whose faces are all gone is written as one triangle with no
    /// area, which draws nothing and keeps its handle for faces put in it later.
    /// </remarks>
    public static AssetHandle[] Meshes(EditMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var drawn = Handles.GetOrCreateValue(mesh);
        if (drawn.Version == mesh.Version && drawn.Meshes.Length == mesh.Slots.Count) return [.. drawn.Meshes];

        var built = Build(mesh);
        var handles = new AssetHandle[built.Length];
        for (var slot = 0; slot < built.Length; slot++)
        {
            var known = slot < drawn.Meshes.Length ? drawn.Meshes[slot] : AssetHandle.None;
            var data = built[slot];

            if (data.Positions.Length == 0)
            {
                if (known != AssetHandle.None) Render.WriteMesh(known, Nothing());
                handles[slot] = known;
                continue;
            }

            if (known == AssetHandle.None) known = Render.CreateMesh(data);
            else Render.WriteMesh(known, data);

            Render.GenerateTangents(known);
            handles[slot] = known;
            lock (Sources) Sources[known] = new WeakReference<EditMesh>(mesh);
        }

        drawn.Meshes = handles;
        drawn.Version = mesh.Version;
        return [.. handles];
    }

    /// <summary>The edit mesh a mesh of <see cref="Meshes"/> was built from, or nothing for a mesh it did not build.</summary>
    /// <param name="mesh">A mesh an entity is drawn with.</param>
    /// <remarks>What the editor's Model tab opens on, the edit mesh behind the entity selected.</remarks>
    public static EditMesh? Source(AssetHandle mesh)
    {
        lock (Sources) return Sources.TryGetValue(mesh, out var source) && source.TryGetTarget(out var found) ? found : null;
    }

    /// <summary>The mesh's triangles as a shape a static body collides as, its faces as one surface whatever their slots.</summary>
    /// <param name="mesh">The mesh, with a face or more.</param>
    /// <returns>A mesh shape, which <see cref="PhysicsWorld.Add"/> gives a static body, such as a level's floors and walls.</returns>
    /// <remarks>
    /// Its own vertices rather than the drawing's, which copies a vertex for each face it is a corner
    /// of, so the surface is one piece. Bepu collides a moving body with a mesh from outside alone,
    /// so it is a static body's shape, and a body made of it again after an edit takes the change,
    /// the old one removed with <see cref="PhysicsWorld.Remove"/> first. A
    /// <see cref="Physics.Collider"/> of <see cref="ColliderShape.Mesh"/> on an entity drawn with the meshes
    /// of <see cref="Meshes"/> collides as the same triangles.
    /// </remarks>
    public static PhysicsShape Collider(EditMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.FaceCount == 0) throw new ArgumentException("A mesh with no faces has nothing to collide with.", nameof(mesh));

        var positions = new Vec3[mesh.VertexCount];
        for (var vertex = 0; vertex < positions.Length; vertex++) positions[vertex] = mesh.Position(vertex);

        var indices = new List<uint>();
        for (var face = 0; face < mesh.FaceCount; face++)
        {
            var corners = mesh.Corners(face);
            var points = new Vec3[corners.Length];
            for (var i = 0; i < corners.Length; i++) points[i] = positions[corners[i]];
            foreach (var index in EarClipping.Cut(points, mesh.Area(face))) indices.Add((uint)corners[index]);
        }

        return PhysicsShape.Mesh(positions, [.. indices]);
    }

    /// <summary>
    /// The two directions a face's texture coordinates are measured along, by the axis its normal is
    /// nearest, chosen so the texture reads the right way round seen from in front of the face, its
    /// top up on a wall.
    /// </summary>
    internal static (Vec3 U, Vec3 V) Projection(Vec3 normal)
    {
        float x = MathF.Abs(normal.X), y = MathF.Abs(normal.Y), z = MathF.Abs(normal.Z);

        // A texture's second coordinate grows downward, so a wall's runs down the world's Y.
        if (y >= x && y >= z) return normal.Y >= 0f ? (Vec3.UnitX, Vec3.UnitZ) : (Vec3.UnitX, -Vec3.UnitZ);
        if (x >= z) return normal.X >= 0f ? (-Vec3.UnitZ, -Vec3.UnitY) : (Vec3.UnitZ, -Vec3.UnitY);
        return normal.Z >= 0f ? (Vec3.UnitX, -Vec3.UnitY) : (-Vec3.UnitX, -Vec3.UnitY);
    }

    /// <summary>
    /// The normal of a smoothed face's corner, the mean of the normals of the smoothed faces round
    /// the corner within the face's angle of it, each by its area.
    /// </summary>
    private static Vec3 Smoothed(int corner, Vec3 normal, float degrees, Vec3[] faceNormals, List<int>[] around, EditMesh mesh)
    {
        var least = MathF.Cos(degrees * MathF.PI / 180f) - 1e-5f;
        var sum = Vec3.Zero;

        foreach (var other in around[corner])
        {
            if (mesh.SmoothOf(other) <= 0f) continue;
            var area = faceNormals[other];
            if (Vec3.Dot(area.Normalized, normal) >= least) sum += area;
        }

        return sum == Vec3.Zero ? normal : sum.Normalized;
    }

    /// <summary>One triangle with no area, which a slot whose faces are all gone is written as.</summary>
    private static MeshData Nothing() => new()
    {
        Positions = [Vec3.Zero, Vec3.Zero, Vec3.Zero],
        Normals = [Vec3.UnitY, Vec3.UnitY, Vec3.UnitY],
        Uvs = [0f, 0f, 0f, 0f, 0f, 0f],
        Indices = [0, 1, 2],
    };
}

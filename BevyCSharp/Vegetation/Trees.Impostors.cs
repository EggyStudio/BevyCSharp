namespace Bevy;

// A planted tree's furthest level, an octahedral impostor, the whole tree pictured once a variant
// from a grid of ways over the upper half of the sphere, each picture a frame of an atlas, and
// drawn as one quad turned to the eye that blends the three frames pictured nearest the way it is
// seen from, as native/bevy_csharp/src/render/impostors.rs draws it.
public static partial class Trees
{
    // The frames a side of the atlas, the ways a tree is pictured from laid on a square folded over
    // the upper half of the sphere, its edge the horizon, so a tree seen from the ground is blended
    // from frames pictured level and two of its frames' ways are some fifteen degrees apart.
    internal const int ImpostorAcross = 8;

    // The atlas, texels a side, a frame a sixty-four texel square, as many as a tree some tens of
    // pixels tall needs where the impostor is drawn. Its pictures are taken twice as fine and halved,
    // so a leaf smaller than a texel is counted by the share of the texel it covers.
    private const int ImpostorAtlasSize = 512;
    private const int ImpostorFiner = 2;

    // The share of a frame left round the tree's sphere, so its edge is not filtered into the next
    // frame's as the atlas's smaller levels are read.
    private const float ImpostorMargin = 0.03f;

    // How far above a variant's cards its frames stand to be pictured, clear of the cards' camera.
    private const float ImpostorStageRise = 16f;

    /// <summary>A variant's impostor as its growth works it out, before its frames are pictured.</summary>
    /// <param name="Middle">The middle of the tree's sphere, in the tree's space.</param>
    /// <param name="Extent">Half a frame's side in the tree's meters, the sphere's radius and a margin.</param>
    /// <param name="Quad">The quad the impostor stands, its corners in its texture coordinates.</param>
    internal sealed record ImpostorPlan(Vec3 Middle, float Extent, MeshData Quad);

    /// <summary>
    /// Works out a variant's impostor from its full level's stems and leaves, the sphere round them
    /// and the quad standing in it.
    /// </summary>
    /// <remarks>
    /// The sphere's middle is the middle of the box round the tree and its radius the furthest of
    /// the tree's vertices from it, so every way's picture of the tree falls inside a frame. The
    /// quad's four vertices stand at four corners of the box round the sphere, a vertex at each
    /// end of each axis, so Bevy's bounds round the mesh hold the sphere, and carry the quad's
    /// corners in their texture coordinates, which the vertex stage stands square to the view.
    /// </remarks>
    private static ImpostorPlan PlanImpostor(MeshData stems, MeshData leaves)
    {
        var places = stems.Positions.Concat(leaves.Positions).ToArray();
        var low = places.Aggregate((a, b) => new Vec3(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z)));
        var high = places.Aggregate((a, b) => new Vec3(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z)));
        var middle = (low + high) * 0.5f;
        var radius = MathF.Sqrt(places.Max(place => (place - middle).LengthSquared));
        var extent = MathF.Max(radius, 1e-3f) / (1f - 2f * ImpostorMargin);

        var quad = new MeshData
        {
            Positions =
            [
                middle + new Vec3(-extent, -extent, -extent),
                middle + new Vec3(extent, -extent, extent),
                middle + new Vec3(extent, extent, -extent),
                middle + new Vec3(-extent, extent, extent),
            ],
            Normals = [Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ],
            Uvs = [0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f],
            Indices = [0, 1, 2, 0, 2, 3],
        };
        return new ImpostorPlan(middle, extent, quad);
    }

    /// <summary>A grown tree's impostor as a planted tree of its species draws it, or nothing where its leaves are fronds.</summary>
    internal static ImpostorPlan? PlanImpostorLevel(TreeSkeleton skeleton, TreeSpecies species, uint seed) =>
        species.Leaves > 0 ? PlanImpostor(Modeling.Build(Build(skeleton, TreeDetail.Full))[0], MakeCards(skeleton, species, seed).Data()) : null;

    /// <summary>The way a frame pictures the tree from, by its column and row on the folded square.</summary>
    internal static Vec3 ImpostorWay(int column, int row)
    {
        var (u, v) = (column * 2f / (ImpostorAcross - 1) - 1f, row * 2f / (ImpostorAcross - 1) - 1f);
        var (x, z) = ((u + v) * 0.5f, (u - v) * 0.5f);
        return new Vec3(x, MathF.Max(1f - MathF.Abs(x) - MathF.Abs(z), 0f), z).Normalized;
    }

    /// <summary>A frame's right, level where its way is not straight up, and its up, square to both.</summary>
    internal static (Vec3 Right, Vec3 Up) ImpostorBasis(Vec3 way)
    {
        var right = Vec3.Cross(Vec3.UnitY, way);
        right = right.LengthSquared > 1e-8f ? right.Normalized : Vec3.UnitX;
        return (right, Vec3.Cross(way, right));
    }

    /// <summary>
    /// The three frames pictured nearest a way the tree is seen from, the corners of the triangle of
    /// the frames' grid the way falls in, and how much of each the picture takes, a way from below
    /// seen as from level.
    /// </summary>
    internal static ((int Column, int Row) Frame, float Weight)[] NearestFrames(Vec3 toward)
    {
        var way = new Vec3(toward.X, MathF.Max(toward.Y, 0f), toward.Z);
        var sum = MathF.Abs(way.X) + MathF.Abs(way.Y) + MathF.Abs(way.Z);
        if (sum < 1e-6f) (way, sum) = (Vec3.UnitX, 1f);
        var (x, z) = (way.X / sum, way.Z / sum);
        var last = ImpostorAcross - 1f;
        var (gx, gy) = (((x + z) * 0.5f + 0.5f) * last, ((x - z) * 0.5f + 0.5f) * last);
        var (cx, cy) = ((int)Math.Clamp(MathF.Floor(gx), 0f, last - 1f), (int)Math.Clamp(MathF.Floor(gy), 0f, last - 1f));
        var (fx, fy) = (gx - cx, gy - cy);
        return fx + fy <= 1f
            ? [((cx, cy), 1f - fx - fy), ((cx + 1, cy), fx), ((cx, cy + 1), fy)]
            : [((cx + 1, cy + 1), fx + fy - 1f), ((cx + 1, cy), 1f - fy), ((cx, cy + 1), 1f - fx)];
    }

    /// <summary>
    /// Stands a variant's full stems and leaves in a grid of frames, each turned so the camera sees
    /// the tree from its frame's way and scaled so the tree's sphere fills it, and spawns the camera
    /// that pictures them, twice as fine as the atlas.
    /// </summary>
    private static void StandImpostor(EcsWorld ecs, CardPictures pictures, ImpostorPlan plan, Vec3 at, AssetHandle stems, AssetHandle leaves, AssetHandle bark, AssetHandle leafMaterial)
    {
        var middle = at + new Vec3(0f, ImpostorStageRise, 0f);
        var scale = 0.5f / plan.Extent;
        var half = ImpostorAcross * 0.5f;
        for (var row = 0; row < ImpostorAcross; row++)
        for (var column = 0; column < ImpostorAcross; column++)
        {
            var way = ImpostorWay(column, row);
            var (right, up) = ImpostorBasis(way);
            var cell = middle + new Vec3(column + 0.5f - half, half - row - 0.5f, 0f);
            var turn = Quat.FromBasis(right, up, way).Conjugate;
            var place = cell - turn * plan.Middle * scale;
            foreach (var (mesh, material) in new[] { (stems, bark), (leaves, leafMaterial) })
            {
                if (mesh == AssetHandle.None) continue;
                var part = ecs.Spawn();
                ecs.Add(part, new Transform(place, turn, new Vec3(scale)));
                Render.SetMesh(ecs, part, mesh);
                Render.SetMaterial(ecs, part, material);
                Render.SetLayers(ecs, part, 1u << CardLayer);
                Render.SetMeshFlags(ecs, part, MeshFlags.NoShadowCasting);
                pictures.Stood.Add(part);
            }
        }

        const uint size = ImpostorAtlasSize * ImpostorFiner;
        pictures.ImpostorTarget = Render.CreateTarget(size, size);
        pictures.ImpostorCamera = Render.SpawnCamera3d(new CameraSettings
        {
            Projection = CameraProjection.Orthographic,
            Height = ImpostorAcross,
            Clear = ClearMode.Custom,
            ClearColor = (0f, 0f, 0f, 0f),
            Order = -49,
            Layers = 1u << CardLayer,
        });
        ecs.Add(pictures.ImpostorCamera, Transform.LookingAt(middle + new Vec3(0f, 0f, 10f), middle, Vec3.UnitY));
        Render.SetCameraTarget(pictures.ImpostorCamera, pictures.ImpostorTarget);
        Render.SetPostProcessing(pictures.ImpostorCamera, new PostSettings { Hdr = false, Msaa = 1, Tonemapper = Tonemapper.None, Dither = false });
    }

    /// <summary>
    /// The impostor's three atlases from the picture of its frames, the color's levels kept as the
    /// cards' are, and the two normals' drawn here from the same stems and leaves, each with the
    /// finer picture's first level dropped.
    /// </summary>
    private static (byte[] Colors, byte[] Normals, byte[] Faces, uint Levels) ImpostorLevels(byte[] pixels, ImpostorPlan plan, MeshData stems, MeshData leaves)
    {
        const int size = ImpostorAtlasSize * ImpostorFiner;
        var cell = size / (float)ImpostorAcross;
        var frames = new List<(float Left, float Top, float Right, float Bottom)>();
        for (var row = 0; row < ImpostorAcross; row++)
        for (var column = 0; column < ImpostorAcross; column++)
            frames.Add((column * cell, row * cell, (column + 1) * cell, (row + 1) * cell));

        var (colors, levels) = AtlasLevels(pixels, size, frames, dropped: ImpostorFiner / 2);
        var (normals, faces, covered) = ImpostorNormals(plan, stems, leaves, size);
        byte[] Leveled(byte[] picture) => Dropped(Render.MipChain(Bled(picture, size, covered, channels: 4), size, size, srgb: false, out _), size, ImpostorFiner / 2);
        return (colors, Leveled(normals), Leveled(faces), levels);
    }

    /// <summary>
    /// The normals a variant's frames picture, its stems' and leaves' triangles drawn here in each
    /// frame as the camera draws them, the nearest one's kept at each texel, in the tree's own space
    /// and folded into a texel as a normal map holds it, with the texels they cover. The first is
    /// the normal the leaf material lights a leaf's diffuse light by, its own on the side of its
    /// card the way out of the crown is on, bent half way to the way out, with its fourth channel
    /// full, and the second the leaf's own turned to face the frame's way, as a double-sided card's
    /// is turned to the eye. A stem's is its own in both, facing the frame's way, the first's fourth
    /// channel clear.
    /// </summary>
    private static (byte[] Normals, byte[] Faces, bool[] Covered) ImpostorNormals(ImpostorPlan plan, MeshData stems, MeshData leaves, int size)
    {
        var normals = new byte[size * size * 4];
        var faces = new byte[size * size * 4];
        var covered = new bool[size * size];
        var nearest = new float[size * size];
        Array.Fill(nearest, float.NegativeInfinity);

        // The way out of the crown at each of the leaves' vertices, unfolded once for every frame.
        var outward = leaves.SecondUvs is { } folded
            ? Enumerable.Range(0, leaves.Positions.Length).Select(i => Unfolded(folded[i * 2], folded[i * 2 + 1])).ToArray()
            : null;

        var cell = size / (float)ImpostorAcross;
        var scale = 0.5f / plan.Extent;
        var points = new (float X, float Y, float Z)[Math.Max(stems.Positions.Length, leaves.Positions.Length)];
        for (var row = 0; row < ImpostorAcross; row++)
        for (var column = 0; column < ImpostorAcross; column++)
        {
            var way = ImpostorWay(column, row);
            var (right, up) = ImpostorBasis(way);
            var (cellLeft, cellTop) = ((int)(column * cell), (int)(row * cell));
            var (cellRight, cellBottom) = ((int)((column + 1) * cell), (int)((row + 1) * cell));

            foreach (var (mesh, isLeaf) in new[] { (stems, false), (leaves, true) })
            {
                if (mesh.Positions.Length == 0 || mesh.Normals is null) continue;
                for (var i = 0; i < mesh.Positions.Length; i++)
                {
                    var away = mesh.Positions[i] - plan.Middle;
                    points[i] = ((column + 0.5f + Vec3.Dot(away, right) * scale) * cell, (row + 0.5f - Vec3.Dot(away, up) * scale) * cell, Vec3.Dot(away, way));
                }

                var corners = mesh.Indices ?? [.. Enumerable.Range(0, mesh.Positions.Length).Select(i => (uint)i)];
                for (var t = 0; t + 2 < corners.Length; t += 3)
                {
                    var (a, b, c) = ((int)corners[t], (int)corners[t + 1], (int)corners[t + 2]);
                    var (pa, pb, pc) = (points[a], points[b], points[c]);
                    var area = (pb.X - pa.X) * (pc.Y - pa.Y) - (pc.X - pa.X) * (pb.Y - pa.Y);
                    if (MathF.Abs(area) < 1e-8f) continue;
                    var own = mesh.Normals[a] + mesh.Normals[b] + mesh.Normals[c];
                    if (own.LengthSquared < 1e-12f) continue;
                    own = own.Normalized;

                    if (!isLeaf && Vec3.Dot(own, way) < 0f) continue;
                    var face = Vec3.Dot(own, way) >= 0f ? own : -own;
                    var normal = face;
                    if (isLeaf && outward is not null)
                    {
                        var wayOut = (outward[a] + outward[b] + outward[c]).Normalized;
                        normal = ((Vec3.Dot(own, wayOut) >= 0f ? own : -own) + wayOut).Normalized;
                    }

                    var x0 = Math.Max(cellLeft, (int)MathF.Floor(MathF.Min(pa.X, MathF.Min(pb.X, pc.X))));
                    var x1 = Math.Min(cellRight - 1, (int)MathF.Ceiling(MathF.Max(pa.X, MathF.Max(pb.X, pc.X))));
                    var y0 = Math.Max(cellTop, (int)MathF.Floor(MathF.Min(pa.Y, MathF.Min(pb.Y, pc.Y))));
                    var y1 = Math.Min(cellBottom - 1, (int)MathF.Ceiling(MathF.Max(pa.Y, MathF.Max(pb.Y, pc.Y))));
                    for (var y = y0; y <= y1; y++)
                    for (var x = x0; x <= x1; x++)
                    {
                        var (px, py) = (x + 0.5f, y + 0.5f);
                        var wa = ((pb.X - px) * (pc.Y - py) - (pc.X - px) * (pb.Y - py)) / area;
                        var wb = ((pc.X - px) * (pa.Y - py) - (pa.X - px) * (pc.Y - py)) / area;
                        var wc = 1f - wa - wb;
                        if (wa < 0f || wb < 0f || wc < 0f) continue;
                        var depth = wa * pa.Z + wb * pb.Z + wc * pc.Z;
                        var at = y * size + x;
                        if (depth <= nearest[at]) continue;
                        nearest[at] = depth;
                        covered[at] = true;
                        normals[at * 4] = (byte)MathF.Round((normal.X * 0.5f + 0.5f) * 255f);
                        normals[at * 4 + 1] = (byte)MathF.Round((normal.Y * 0.5f + 0.5f) * 255f);
                        normals[at * 4 + 2] = (byte)MathF.Round((normal.Z * 0.5f + 0.5f) * 255f);
                        normals[at * 4 + 3] = isLeaf ? (byte)255 : (byte)0;
                        faces[at * 4] = (byte)MathF.Round((face.X * 0.5f + 0.5f) * 255f);
                        faces[at * 4 + 1] = (byte)MathF.Round((face.Y * 0.5f + 0.5f) * 255f);
                        faces[at * 4 + 2] = (byte)MathF.Round((face.Z * 0.5f + 0.5f) * 255f);
                        faces[at * 4 + 3] = 255;
                    }
                }
            }
        }

        return (normals, faces, covered);
    }

    /// <summary>A way folded onto a square by <see cref="Folded"/>, unfolded, as the leaf material unfolds it.</summary>
    private static Vec3 Unfolded(float u, float v)
    {
        var (x, z) = (u * 2f - 1f, v * 2f - 1f);
        var y = 1f - MathF.Abs(x) - MathF.Abs(z);
        var t = MathF.Max(-y, 0f);
        x += x >= 0f ? -t : t;
        z += z >= 0f ? -t : t;
        return new Vec3(x, y, z).Normalized;
    }
}

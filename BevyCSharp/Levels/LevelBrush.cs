namespace Bevy;

/// <summary>A brush of a level file, a convex solid, what lies behind every one of its faces' planes.</summary>
/// <remarks>
/// A brush is kept as its planes, as Quake's tools, Hammer and TrenchBroom keep one, and its corners
/// are found where three planes meet within the rest (<see cref="Polygons"/>), so a brush stays
/// convex whatever moves its planes and two of them never share a corner by accident.
/// </remarks>
public sealed class LevelBrush
{
    // How far, in the file's units, a corner may stand outside a plane and still be within it, and
    // how near two corners may be and be one. Planes through integer points meet at corners exact in
    // double precision, and a millionth of a unit is far below a texel and far above the rounding
    // where planes through points TrenchBroom writes to seventeen digits meet.
    private const double Within = 1e-6;
    private const double Same = 1e-5;

    /// <summary>The faces.</summary>
    public List<LevelFace> Faces { get; } = [];

    /// <summary>
    /// Each face's polygon, its corners wound clockwise seen from outside as the file's points are,
    /// in the file's units, or nothing for a face whose plane only touches the solid.
    /// </summary>
    /// <returns>A polygon a face, in the order of <see cref="Faces"/>.</returns>
    /// <exception cref="InvalidOperationException">A face's three points lie on one line, or the planes close nothing.</exception>
    /// <remarks>
    /// The corners are the points where three planes meet that stand behind every other plane, each
    /// found once and shared by the faces whose planes it lies on, so the faces of a brush meet edge
    /// to edge with no gap a ray could pass. Each face's corners are ordered round their middle.
    /// A brush with several faces in one plane keeps the polygon for the first and none for the rest.
    /// </remarks>
    public LevelPoint[]?[] Polygons()
    {
        var count = Faces.Count;
        var normals = new LevelPoint[count];
        var distances = new double[count];
        for (var face = 0; face < count; face++)
        {
            normals[face] = Faces[face].Normal;
            if (normals[face] == default) throw new InvalidOperationException($"The brush's face {face} has three points on one line, which make no plane.");
            distances[face] = Faces[face].Distance;
        }

        // Every three planes that meet at one point within the rest give a corner, kept once.
        var corners = new List<LevelPoint>();
        var on = new List<List<int>>();
        for (var a = 0; a < count; a++)
        for (var b = a + 1; b < count; b++)
        for (var c = b + 1; c < count; c++)
        {
            if (Meet(normals[a], distances[a], normals[b], distances[b], normals[c], distances[c]) is not { } point) continue;
            var inside = true;
            for (var other = 0; other < count && inside; other++) inside = LevelPoint.Dot(normals[other], point) - distances[other] <= Within * Math.Max(1, Math.Abs(distances[other]));
            if (!inside) continue;
            if (corners.FindIndex(known => (known - point).Length < Same) >= 0) continue;
            corners.Add(point);
            on.Add([]);
        }

        if (corners.Count < 4) throw new InvalidOperationException($"The brush's {count} planes close no solid.");
        for (var corner = 0; corner < corners.Count; corner++)
        for (var face = 0; face < count; face++)
        {
            if (Math.Abs(LevelPoint.Dot(normals[face], corners[corner]) - distances[face]) <= Same) on[corner].Add(face);
        }

        // Each face's corners round their middle, clockwise seen from outside along the normal.
        var polygons = new LevelPoint[]?[count];
        var edges = new Dictionary<(LevelPoint, LevelPoint), int>();
        for (var face = 0; face < count; face++)
        {
            var mine = Enumerable.Range(0, corners.Count).Where(corner => on[corner].Contains(face)).Select(corner => corners[corner]).ToList();
            if (mine.Count < 3 || Enumerable.Range(0, face).Any(earlier => polygons[earlier] is not null && LevelPoint.Dot(normals[earlier], normals[face]) > 1 - 1e-9 && Math.Abs(distances[earlier] - distances[face]) < Same)) continue;

            var middle = mine.Aggregate(default(LevelPoint), (sum, point) => sum + point) * (1.0 / mine.Count);
            var normal = normals[face];
            var across = Math.Abs(normal.Z) < 0.9 ? new LevelPoint(0, 0, 1) : new LevelPoint(1, 0, 0);
            var right = LevelPoint.Cross(across, normal);
            right *= 1 / right.Length;
            var up = LevelPoint.Cross(normal, right);
            var polygon = polygons[face] = [.. mine.OrderByDescending(point => Math.Atan2(LevelPoint.Dot(point - middle, up), LevelPoint.Dot(point - middle, right)))];
            for (var i = 0; i < polygon.Length; i++) edges[(polygon[i], polygon[(i + 1) % polygon.Length])] = face;
        }

        // A solid's every edge runs one way round one face and back round the face beside it, and
        // planes that leave the solid open on a side give faces whose edges run round one alone.
        if (edges.Keys.Any(edge => !edges.ContainsKey((edge.Item2, edge.Item1)))) throw new InvalidOperationException($"The brush's {count} planes close no solid.");
        return polygons;
    }

    /// <summary>Where three planes meet, or nothing where two of them run side by side.</summary>
    private static LevelPoint? Meet(LevelPoint n1, double d1, LevelPoint n2, double d2, LevelPoint n3, double d3)
    {
        var cross23 = LevelPoint.Cross(n2, n3);
        var denominator = LevelPoint.Dot(n1, cross23);
        if (Math.Abs(denominator) < 1e-9) return null;
        return (cross23 * d1 + LevelPoint.Cross(n3, n1) * d2 + LevelPoint.Cross(n1, n2) * d3) * (1 / denominator);
    }
}

namespace Bevy;

/// <summary>Cuts a face of any count into triangles, the way <see cref="EditMesh"/> draws and triangulates one.</summary>
/// <remarks>
/// <para>
/// Ear clipping, which cuts a concave face right where a fan from one corner would lay triangles
/// outside it, as a fan of a staircase's side or an arch's front would. The face is laid flat
/// across the axis its normal is nearest and turned counter-clockwise there, and an ear is a
/// corner whose two neighbors make a triangle that turns the same way and holds no other corner of
/// what is left, inside or on its sides.
/// </para>
/// <para>
/// The ear cut is the first in the order of the corners left, from the first, so the same face is
/// cut into the same triangles wherever it is cut, and a recipe that triangulates and then picks a
/// face by its index picks the same one in either engine. A face left with no ear, which
/// only one whose corners double back on themselves is, is finished as a fan of what is left, so
/// every face of n corners gives n - 2 triangles.
/// </para>
/// </remarks>
internal static class EarClipping
{
    /// <summary>The triangles of a face, three corners each, as positions in its loop, wound as it is.</summary>
    /// <param name="corners">The face's corners, in order round it.</param>
    /// <param name="normal">The way it faces, of any length.</param>
    public static int[] Cut(ReadOnlySpan<Vec3> corners, Vec3 normal)
    {
        var count = corners.Length;
        var triangles = new int[(count - 2) * 3];
        if (count == 3)
        {
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            return triangles;
        }

        var flat = Flatten(corners, normal);
        var left = Enumerable.Range(0, count).ToList();
        var written = 0;

        while (left.Count > 3)
        {
            var cut = -1;
            for (var i = 0; i < left.Count && cut < 0; i++)
            {
                if (IsEar(flat, left, i)) cut = i;
            }

            // Nothing turns the face's way with nothing inside it, so what is left doubles back
            // on itself, and a fan of it gives the count of triangles if not their shape.
            if (cut < 0) break;

            var previous = left[(cut + left.Count - 1) % left.Count];
            var next = left[(cut + 1) % left.Count];
            triangles[written++] = previous;
            triangles[written++] = left[cut];
            triangles[written++] = next;
            left.RemoveAt(cut);
        }

        for (var i = 1; i < left.Count - 1; i++)
        {
            triangles[written++] = left[0];
            triangles[written++] = left[i];
            triangles[written++] = left[i + 1];
        }

        return triangles;
    }

    /// <summary>
    /// The face laid flat across the axis its normal is nearest, in the two other coordinates in
    /// the order that keeps it counter-clockwise, so an ear is a corner that turns left.
    /// </summary>
    private static Vec2[] Flatten(ReadOnlySpan<Vec3> corners, Vec3 normal)
    {
        float x = MathF.Abs(normal.X), y = MathF.Abs(normal.Y), z = MathF.Abs(normal.Z);
        var flat = new Vec2[corners.Length];

        for (var i = 0; i < corners.Length; i++)
        {
            var point = corners[i];

            // Seen from the side the normal points to, (y, z) turns counter-clockwise about +X,
            // (z, x) about +Y and (x, y) about +Z, and the pair swaps where the normal points the
            // other way.
            flat[i] = x >= y && x >= z ? (normal.X >= 0f ? new Vec2(point.Y, point.Z) : new Vec2(point.Z, point.Y))
                : y >= z ? (normal.Y >= 0f ? new Vec2(point.Z, point.X) : new Vec2(point.X, point.Z))
                : normal.Z >= 0f ? new Vec2(point.X, point.Y) : new Vec2(point.Y, point.X);
        }

        return flat;
    }

    private static bool IsEar(Vec2[] flat, List<int> left, int at)
    {
        var a = flat[left[(at + left.Count - 1) % left.Count]];
        var b = flat[left[at]];
        var c = flat[left[(at + 1) % left.Count]];

        // A corner that turns right, or not at all, is no ear, since the triangle it would cut
        // lies outside the face or has no area.
        if (Turn(a, b, c) <= 0f) return false;

        for (var i = 0; i < left.Count; i++)
        {
            var point = flat[left[i]];
            if (i == at || point.Equals(a) || point.Equals(b) || point.Equals(c)) continue;
            if (Turn(a, b, point) >= 0f && Turn(b, c, point) >= 0f && Turn(c, a, point) >= 0f) return false;
        }

        return true;
    }

    /// <summary>Twice the area of a triangle, positive where it turns counter-clockwise.</summary>
    private static float Turn(Vec2 a, Vec2 b, Vec2 c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}

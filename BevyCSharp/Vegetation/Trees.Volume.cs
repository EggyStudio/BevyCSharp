namespace Bevy;

// The crown as a volume, the leaf area its cards hold in each voxel of a grid over it, from which
// each leaf takes how much of the sky reaches it through the leaves round it.
public static partial class Trees
{
    // The grid's voxels across the crown's longest side, fine enough to tell a crown's middle from
    // its rim and coarse enough that each voxel holds some tens of leaves.
    private const int VolumeVoxels = 24;

    // The share of a leaf's card its leaf covers, the rest of the box its picture's transparent
    // corners.
    private const float LeafFill = 0.6f;

    // The mean share of a leaf's area that faces a ray, for leaves turned every way, which Beer and
    // Lambert's law takes the leaf area along a ray by.
    private const float FacingShare = 0.5f;

    // The ways the sky is read from, straight up and a ring of eight a third of the way down from
    // it, each weighted by how high it stands, as a cosine weights the sky a surface facing up sees.
    private static readonly (Vec3 Way, float Weight)[] SkyWays = MakeSkyWays();

    private static (Vec3 Way, float Weight)[] MakeSkyWays()
    {
        var ways = new List<(Vec3, float)> { (Vec3.UnitY, 1f) };
        var height = MathF.Sin(MathF.PI / 6f);
        var across = MathF.Cos(MathF.PI / 6f);
        for (var k = 0; k < 8; k++)
        {
            var (sin, cos) = MathF.SinCos(k * MathF.Tau / 8f);
            ways.Add((new Vec3(across * cos, height, across * sin), height));
        }

        var total = ways.Sum(way => way.Item2);
        return [.. ways.Select(way => (way.Item1, way.Item2 / total))];
    }

    /// <summary>
    /// How much of the sky each leaf sees through the crown, from 0 to 1, for leaves at the given
    /// places of the given areas.
    /// </summary>
    /// <remarks>
    /// The leaves' area is gathered into a grid of voxels over the crown, a leaf area density, and
    /// the sky a leaf sees is the light let through along each of <see cref="SkyWays"/> by Beer and
    /// Lambert's law, the leaf area density times the share facing the ray summed along it, from a
    /// voxel past the leaf's own so a leaf does not shade itself.
    /// </remarks>
    internal static float[] CrownSky(IReadOnlyList<Vec3> places, IReadOnlyList<float> areas)
    {
        var count = places.Count;
        if (count == 0) return [];

        var low = places.Aggregate((a, b) => new Vec3(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z)));
        var high = places.Aggregate((a, b) => new Vec3(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z)));
        var size = MathF.Max(MathF.Max(high.X - low.X, high.Y - low.Y), MathF.Max(high.Z - low.Z, 1e-3f)) / VolumeVoxels;
        low -= new Vec3(size);
        var (width, height, depth) = ((int)((high.X - low.X) / size) + 2, (int)((high.Y - low.Y) / size) + 2, (int)((high.Z - low.Z) / size) + 2);

        // The leaf area in each voxel over its volume, square meters a cubic meter.
        var density = new float[width * height * depth];
        int Index(int x, int y, int z) => x + width * (y + height * z);
        (int X, int Y, int Z) Voxel(Vec3 place) => ((int)MathF.Floor((place.X - low.X) / size), (int)MathF.Floor((place.Y - low.Y) / size), (int)MathF.Floor((place.Z - low.Z) / size));
        bool Inside(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < width && y < height && z < depth;
        for (var i = 0; i < count; i++)
        {
            var (x, y, z) = Voxel(places[i]);
            density[Index(x, y, z)] += areas[i] * LeafFill / (size * size * size);
        }

        var sky = new float[count];
        for (var i = 0; i < count; i++)
        {
            var place = places[i];
            var seen = 0f;
            foreach (var (way, weight) in SkyWays)
            {
                // Half a voxel a step, from a voxel off the leaf, until the ray leaves the grid.
                var depthAlong = 0f;
                for (var along = size; ; along += size * 0.5f)
                {
                    var (x, y, z) = Voxel(place + way * along);
                    if (!Inside(x, y, z)) break;
                    depthAlong += density[Index(x, y, z)] * size * 0.5f;
                }

                seen += weight * MathF.Exp(-FacingShare * depthAlong);
            }

            sky[i] = seen;
        }

        return sky;
    }
}

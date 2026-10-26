namespace Bevy;

// A doorway, a wall with a rounded opening, made of the brushes a level editor blocks one out
// with, each convex, so a level built in code has the rounded doorways of Unreal's prototyping.
public sealed partial class Level
{
    /// <summary>
    /// A wall along X with a rounded opening through its middle, a brush each side of the opening
    /// and the wall above it cut into straight segments round the opening's top, as a level
    /// editor's arch tool cuts one.
    /// </summary>
    /// <param name="low">The wall's corner with the least of each axis, in the file's units.</param>
    /// <param name="high">Its corner with the most of each, the wall running along X and as thick as its Y.</param>
    /// <param name="width">How wide the opening is, its top a half circle as wide.</param>
    /// <param name="height">How high the opening's top stands above the wall's foot.</param>
    /// <param name="material">What the wall's sides are drawn with.</param>
    /// <param name="arch">What the segments above the opening are drawn with, the wall's material where none is given.</param>
    /// <param name="segments">How many straight pieces round the opening's top, three or more.</param>
    /// <param name="scale">The faces' scale, <see cref="LevelBrush.DefaultScale"/> unless given.</param>
    /// <returns>The brushes, the two sides first and then the segments from the right round to the left.</returns>
    /// <remarks>
    /// <para>
    /// The opening's sides rise straight from the wall's foot to where its top begins, half its
    /// width under its height, and its top runs round a half circle. Each segment reaches from the
    /// half circle out to the wall's edge along two rays from the circle's middle, and the rays
    /// through the wall's top corners are among them, so each segment's outer side lies along one
    /// edge and the segment is convex.
    /// </para>
    /// <para>
    /// A wall another way is this one turned (<see cref="LevelBrush.Turned"/>).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">The wall has no size along an axis, or the opening does not fit it.</exception>
    public static IReadOnlyList<LevelBrush> Doorway(
        LevelPoint low, LevelPoint high, double width, double height, string material, string? arch = null, int segments = 8, double scale = LevelBrush.DefaultScale)
    {
        if (!(high.X > low.X && high.Y > low.Y && high.Z > low.Z)) throw new ArgumentException($"A wall from {low} to {high} has no size along an axis; the second corner has the most of each.", nameof(high));
        ArgumentOutOfRangeException.ThrowIfLessThan(segments, 3);
        var radius = width / 2;
        var middle = (low.X + high.X) / 2;
        var spring = low.Z + height - radius;
        if (!(width > 0) || middle - radius <= low.X || middle + radius >= high.X || spring < low.Z || low.Z + height >= high.Z)
            throw new ArgumentException($"An opening {width} wide and {height} high does not fit a wall from {low} to {high} with wall beside and above it.", nameof(width));

        var brushes = new List<LevelBrush>
        {
            LevelBrush.Box(low, new LevelPoint(middle - radius, high.Y, high.Z), material, scale),
            LevelBrush.Box(new LevelPoint(middle + radius, low.Y, low.Z), high, material, scale),
        };

        // The rays the segments are cut along, evenly round the half circle and through the top
        // corners, where a segment's outer side turns from the wall's side to its top.
        var corner = Math.Atan2(high.Z - spring, radius);
        var angles = Enumerable.Range(0, segments + 1).Select(piece => piece * Math.PI / segments)
            .Concat([corner, Math.PI - corner]).Order().ToList();
        angles = [.. angles.Where((angle, at) => at == 0 || angle - angles[at - 1] > 1e-9)];

        (double X, double Z) Inside(double angle) => (middle + radius * Math.Cos(angle), spring + radius * Math.Sin(angle));
        (double X, double Z) Outside(double angle)
        {
            if (angle <= corner + 1e-12) return (middle + radius, spring + radius * Math.Tan(angle));
            if (angle >= Math.PI - corner - 1e-12) return (middle - radius, spring + radius * Math.Tan(Math.PI - angle));
            return (middle + (high.Z - spring) / Math.Tan(angle), high.Z);
        }

        for (var i = 0; i + 1 < angles.Count; i++)
        {
            var (a, b) = (angles[i], angles[i + 1]);
            brushes.Add(LevelBrush.Prism([Inside(a), Outside(a), Outside(b), Inside(b)], low.Y, high.Y, arch ?? material, scale));
        }

        return brushes;
    }
}

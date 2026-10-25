namespace Bevy;

/// <summary>
/// The shape of a bolt of lightning from the clouds' base to the ground, a jagged channel with
/// branches, as segments for <see cref="SkyThunder"/> to draw.
/// </summary>
/// <remarks>
/// The channel is the straight line from its top to where it lands, its middle pushed aside at
/// random, and each half's middle again, seven times over, each push a quarter of the length it
/// splits, so it zigzags at every scale as a real channel does, the step leader having found its
/// way down a stretch at a time. A few branches leave it from its upper part, turned from its way
/// by twenty to sixty degrees and leaning down, each a channel of its own five times split, a fifth
/// to two fifths of the bolt's length, dimmer and narrower than the channel. A branch ends in the
/// air, as the leaders that did not reach the ground do.
/// </remarks>
internal static class LightningBolt
{
    /// <summary>One straight stretch of a bolt, from <c>From</c> to <c>To</c>, how wide and how bright against the main channel.</summary>
    internal readonly record struct Segment(Vec3 From, Vec3 To, float Width, float Brightness);

    /// <summary>The segments of a bolt from <paramref name="top"/> to <paramref name="ground"/>.</summary>
    internal static List<Segment> Grow(Vec3 top, Vec3 ground, uint seed)
    {
        var random = new Random((int)(seed % int.MaxValue));
        var segments = new List<Segment>();
        var channel = Split(top, ground, 7, random);
        for (var i = 0; i + 1 < channel.Count; i++)
            segments.Add(new Segment(channel[i], channel[i + 1], 1f, 1f));

        var length = (ground - top).Length;
        var down = (ground - top).Normalized;
        var branches = 3 + random.Next(4);
        for (var b = 0; b < branches; b++)
        {
            // From the channel's upper two thirds, turned aside and leaning down.
            var from = channel[random.Next(channel.Count * 2 / 3)];
            var side = new Vec3((random.NextSingle() * 2f) - 1f, 0f, (random.NextSingle() * 2f) - 1f);
            if (side.Length < 1e-3f) side = Vec3.UnitX;
            var angle = (20f + (40f * random.NextSingle())) * MathF.PI / 180f;
            var way = ((down * MathF.Cos(angle)) + (side.Normalized * MathF.Sin(angle))).Normalized;
            var reach = length * (0.2f + (0.2f * random.NextSingle()));
            var branch = Split(from, from + (way * reach), 5, random);
            var bright = 0.35f + (0.25f * random.NextSingle());
            for (var i = 0; i + 1 < branch.Count; i++)
                segments.Add(new Segment(branch[i], branch[i + 1], 0.5f, bright * (1f - (0.6f * i / branch.Count))));
        }
        return segments;
    }

    // The points of a line from one end to the other, its middles pushed aside so many times over.
    private static List<Vec3> Split(Vec3 from, Vec3 to, int times, Random random)
    {
        var points = new List<Vec3> { from, to };
        for (var time = 0; time < times; time++)
        {
            var next = new List<Vec3>(points.Count * 2) { points[0] };
            for (var i = 0; i + 1 < points.Count; i++)
            {
                var (a, b) = (points[i], points[i + 1]);
                var along = b - a;
                var span = along.Length;
                var across = Vec3.Cross(along, new Vec3((random.NextSingle() * 2f) - 1f, (random.NextSingle() * 2f) - 1f, (random.NextSingle() * 2f) - 1f));
                var push = across.Length > 1e-5f ? across.Normalized * (((random.NextSingle() * 2f) - 1f) * span * 0.25f) : Vec3.Zero;
                next.Add(((a + b) * 0.5f) + push);
                next.Add(b);
            }
            points = next;
        }
        return points;
    }
}

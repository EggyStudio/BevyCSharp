namespace Bevy;

// The bark and the leaves the generator makes where a species names no set of the collection, small
// pictures worked out in code from noise and from a leaf's outline, the same each time, white or gray
// so a species' color tints them.
internal static class TreeLooks
{
    // The bark's picture, round the stem across and along it down, tiling both ways.
    public const int BarkWidth = 128;
    public const int BarkHeight = 512;

    // The leaves' atlas, two by two leaves, each cell square.
    public const int LeafSize = 256;

    /// <summary>The generated atlas's four leaves, a cell each, their stalks at the bottom.</summary>
    public static readonly IReadOnlyList<(float U, float V, float Width, float Height)> LeafBoxes =
        [(0f, 0f, 0.5f, 0.5f), (0.5f, 0f, 0.5f, 0.5f), (0f, 0.5f, 0.5f, 0.5f), (0.5f, 0.5f, 0.5f, 0.5f)];

    /// <summary>
    /// The bark's color and its normal map as RGBA, a gray of ridges running along the stem, from
    /// noise four times finer round it than along it, in sRGB, and the normal its ridges make.
    /// </summary>
    public static (byte[] Color, byte[] Normal) Bark()
    {
        var height = new float[BarkWidth * BarkHeight];
        for (var y = 0; y < BarkHeight; y++)
        for (var x = 0; x < BarkWidth; x++)
        {
            // Three octaves of value noise, each finer and fainter, its lattice wrapping so the
            // picture tiles, the ridges four lattice cells round for one along.
            var sum = 0f;
            var weight = 0.55f;
            for (var octave = 0; octave < 3; octave++)
            {
                var cells = 8 << octave;
                sum += weight * Noise(x * (4f * cells) / BarkWidth, y * (float)cells / BarkHeight, 4 * cells, cells, octave);
                weight *= 0.5f;
            }

            height[x + BarkWidth * y] = sum;
        }

        var color = new byte[BarkWidth * BarkHeight * 4];
        var normal = new byte[BarkWidth * BarkHeight * 4];
        for (var y = 0; y < BarkHeight; y++)
        for (var x = 0; x < BarkWidth; x++)
        {
            var at = x + BarkWidth * y;
            var gray = (byte)Math.Clamp(110f + 145f * height[at], 0f, 255f);
            color[4 * at] = color[4 * at + 1] = color[4 * at + 2] = gray;
            color[4 * at + 3] = 255;

            // The slope round and along, the ridges standing out by a few texels' worth.
            var dx = height[(x + 1) % BarkWidth + BarkWidth * y] - height[(x + BarkWidth - 1) % BarkWidth + BarkWidth * y];
            var dy = height[x + BarkWidth * ((y + 1) % BarkHeight)] - height[x + BarkWidth * ((y + BarkHeight - 1) % BarkHeight)];
            var n = new Vec3(-dx * 3f, dy * 3f, 1f).Normalized;
            normal[4 * at] = (byte)Math.Clamp(127.5f + 127.5f * n.X, 0f, 255f);
            normal[4 * at + 1] = (byte)Math.Clamp(127.5f + 127.5f * n.Y, 0f, 255f);
            normal[4 * at + 2] = (byte)Math.Clamp(127.5f + 127.5f * n.Z, 0f, 255f);
            normal[4 * at + 3] = 255;
        }

        return (color, normal);
    }

    /// <summary>
    /// The leaves' atlas as RGBA in sRGB, four leaves of a shape and a width against their length, a
    /// little different each, white with a darker midrib and stalk, cut out by their alpha.
    /// </summary>
    public static byte[] Leaves(TreeLeafShape shape, float width)
    {
        var pixels = new byte[LeafSize * LeafSize * 4];
        var cell = LeafSize / 2;
        for (var index = 0; index < 4; index++)
        {
            var (left, top) = ((index % 2) * cell, (index / 2) * cell);
            var wide = Math.Clamp(width * (0.9f + 0.07f * index), 0.1f, 1f);
            for (var y = 0; y < cell; y++)
            for (var x = 0; x < cell; x++)
            {
                // The leaf's own space, its stalk at the bottom middle and its tip at the top.
                var along = 1f - (y + 0.5f) / cell;
                var across = (x + 0.5f) / cell - 0.5f;
                var (inside, rib) = Inside(shape, along, across, wide);
                if (!inside) continue;

                var at = 4 * (left + x + LeafSize * (top + y));
                var shade = (byte)(rib ? 190 : 235 - (int)(20f * along));
                pixels[at] = pixels[at + 1] = pixels[at + 2] = shade;
                pixels[at + 3] = 255;
            }
        }

        return pixels;
    }

    /// <summary>Whether a point of a leaf's space is in the leaf, and on its midrib or its stalk.</summary>
    private static (bool Inside, bool Rib) Inside(TreeLeafShape shape, float along, float across, float width)
    {
        // The stalk, the bottom tenth, a thin line up the middle.
        const float Stalk = 0.1f;
        if (along < Stalk) return (MathF.Abs(across) < 0.012f, true);
        var y = (along - Stalk) / (1f - Stalk);
        var rib = MathF.Abs(across) < 0.008f + 0.006f * (1f - y);

        var half = shape switch
        {
            TreeLeafShape.Triangle => 0.5f * width * (1f - y),
            TreeLeafShape.ThreeLobeOak => Oval(y, width) * (0.72f + 0.28f * MathF.Abs(MathF.Cos(y * 3f * MathF.PI))),
            TreeLeafShape.ThreeLobeMaple or TreeLeafShape.FiveLobeMaple => 0f,
            TreeLeafShape.ThreeLeaflets => 0f,
            _ => Oval(y, width),
        };

        if (shape is TreeLeafShape.ThreeLobeMaple or TreeLeafShape.FiveLobeMaple)
        {
            // Lobes spreading from a point a third of the way up, pointed at their tips.
            var lobes = shape == TreeLeafShape.ThreeLobeMaple ? 3 : 5;
            var dx = across / (0.5f * width);
            var dy = (y - 0.33f) / 0.67f;
            var angle = MathF.Atan2(dx, dy);
            if (MathF.Abs(angle) > MathF.PI * 0.75f) return (false, false);
            var reach = 0.45f + 0.55f * MathF.Pow(MathF.Abs(MathF.Cos(angle * lobes * 0.5f)), 3f);
            return (MathF.Sqrt(dx * dx + dy * dy) < reach, rib && dy > 0f);
        }

        if (shape == TreeLeafShape.ThreeLeaflets)
        {
            // A long leaflet up the middle and two shorter ones leaning out, each an oval.
            foreach (var (lean, length) in (ReadOnlySpan<(float, float)>)[(0f, 1f), (0.7f, 0.6f), (-0.7f, 0.6f)])
            {
                var (sin, cos) = MathF.SinCos(lean);
                var u = (y * cos + across * sin) / length;
                var v = -y * sin + across * cos;
                if (u is > 0f and < 1f && MathF.Abs(v) < Oval(u, width) * length) return (true, MathF.Abs(v) < 0.008f);
            }

            return (false, false);
        }

        return (MathF.Abs(across) < half, rib);
    }

    /// <summary>An oval leaf's half width at a share of its length, widest a little below its middle.</summary>
    private static float Oval(float y, float width) => 0.5f * width * MathF.Pow(MathF.Sin(MathF.PI * MathF.Pow(y, 0.8f)), 0.9f);

    /// <summary>Value noise from zero to one on a lattice wrapping at a width and a height of cells, smoothed between its corners.</summary>
    private static float Noise(float x, float y, int width, int height, int layer)
    {
        var (x0, y0) = ((int)MathF.Floor(x), (int)MathF.Floor(y));
        var (fx, fy) = (x - x0, y - y0);
        var (sx, sy) = (fx * fx * (3f - 2f * fx), fy * fy * (3f - 2f * fy));
        float Corner(int cx, int cy) => Hash(((cx % width) + width) % width, ((cy % height) + height) % height, layer);
        var top = Corner(x0, y0) + (Corner(x0 + 1, y0) - Corner(x0, y0)) * sx;
        var bottom = Corner(x0, y0 + 1) + (Corner(x0 + 1, y0 + 1) - Corner(x0, y0 + 1)) * sx;
        return top + (bottom - top) * sy;
    }

    /// <summary>A number from zero to one for a lattice corner, the same for the same corner every time.</summary>
    private static float Hash(int x, int y, int layer)
    {
        var bits = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(layer * 83492791);
        bits ^= bits >> 13;
        bits *= 0x5bd1e995;
        bits ^= bits >> 15;
        return (bits & 0xFFFFFF) / 16_777_216f;
    }
}

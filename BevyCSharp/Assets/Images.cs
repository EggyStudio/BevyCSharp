namespace Bevy;

/// <summary>Images the engine makes in code, such as the grid a level is blocked out in.</summary>
public static class Images
{
    /// <summary>How many pixels a meter the grid has where none is given.</summary>
    public const int DefaultPixelsPerMeter = 1024;

    /// <summary>The face of the grid where none is given, a light gray, so a tint shows as itself.</summary>
    public static Color DefaultFace => Color.FromSrgb(0.9f, 0.9f, 0.9f);

    /// <summary>The grid's lines a meter and a half meter apart and its mark where none is given, a dark gray.</summary>
    public static Color DefaultLine => Color.FromSrgb(0.35f, 0.35f, 0.35f);

    /// <summary>The grid's lines a quarter of a meter apart where none is given, a gray between the two.</summary>
    public static Color DefaultFine => Color.FromSrgb(0.68f, 0.68f, 0.68f);

    /// <summary>Makes the grid of a meter's cell, in its default colors and size.</summary>
    /// <returns>A handle to the image, repeating, with its mipmaps.</returns>
    public static AssetHandle Grid() => Grid(DefaultPixelsPerMeter, DefaultFace, DefaultLine, DefaultFine);

    /// <summary>Makes the grid of a meter's cell, in its default colors, at a size.</summary>
    /// <param name="pixelsPerMeter">How many pixels a side the meter's cell has.</param>
    /// <returns>A handle to the image, repeating, with its mipmaps.</returns>
    public static AssetHandle Grid(int pixelsPerMeter) => Grid(pixelsPerMeter, DefaultFace, DefaultLine, DefaultFine);

    /// <summary>
    /// Makes the image of one meter's cell of a grid, its edges a line, a thinner line across its
    /// middle and a fine line at each quarter, with a mark of its size in its corner, to tile a
    /// level's faces whose texture coordinates are in meters.
    /// </summary>
    /// <param name="pixelsPerMeter">How many pixels a side the meter's cell has, sixteen or more.</param>
    /// <param name="face">The cell's color.</param>
    /// <param name="line">The color of the lines a meter and a half meter apart and of the mark.</param>
    /// <param name="fine">The color of the lines a quarter of a meter apart.</param>
    /// <returns>A handle to the image, repeating, with its mipmaps.</returns>
    /// <remarks>
    /// <para>
    /// The grid of the prototype textures of the engines of the 2000s, which tells a reader of a
    /// level blocked out in it how big each thing is, laid over every face of an
    /// <see cref="EditMesh"/> by its texture coordinates in meters (<see cref="Modeling.Build"/>), so
    /// a cell is a meter on every face and the lines run on across faces. A meter's line is half on
    /// each edge of the cell, so cells tiled side by side make it whole, and the half's and the
    /// quarters' lines are centered on theirs. The steps are a level editor's, as Source's measuring
    /// textures step 16, 64 and 128 units, so at TrenchBroom's 32 units a meter a brush snapped to
    /// its 8, 16 or 32 unit grid ends on a line. The corner of each cell reads <c>1m</c>, as those
    /// textures print their sizes, where the cell has 128 pixels or more.
    /// </para>
    /// <para>
    /// Made with its mipmaps, so a floor running into the distance fades into the face's tone
    /// rather than shimmering, and given a sampler that repeats and filters across sixteen samples
    /// along a surface seen at a slant, so its lines stay sharp along a corridor. A light face
    /// tinted by a material's color (<see cref="Modeling.Prototype(string)"/>) shows the tint, and
    /// the lines a darker tone of it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pixelsPerMeter"/> is less than sixteen or more than 4096.</exception>
    /// <exception cref="Interop.BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle Grid(int pixelsPerMeter, Color face, Color line, Color fine)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelsPerMeter, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pixelsPerMeter, 4096);

        var size = (uint)pixelsPerMeter;
        var image = Render.CreateImage(GridPixels(pixelsPerMeter, face, line, fine), size, size, srgb: true, mipmaps: true);
        Render.SetSampler(image, new TextureSettings
        {
            Wrap = TextureWrap.Repeat,
            MagFilter = TextureFilter.Linear,
            MinFilter = TextureFilter.Linear,
            MipmapFilter = TextureFilter.Linear,
            Anisotropy = 16,
        });
        return image;
    }

    /// <summary>The pixels of a grid's cell, four bytes a pixel, rows from the top.</summary>
    /// <remarks>
    /// A meter's line is a 128th of the cell wide, two pixels at least, half of it on each edge, the
    /// half's line a 256th, and a quarter's a 512th, a pixel at least, each starting half its width
    /// before its place, rounded to the nearest pixel, a half rounded up. A pixel on a meter's or the
    /// half's line, or on the mark, is the line's color, else one on a quarter's line the fine color,
    /// else the face's. The mark is <c>1m</c> in letters of five by seven squares, each square a
    /// 128th of the cell, standing two squares in from the cell's top left inside its line.
    /// </remarks>
    internal static byte[] GridPixels(int pixelsPerMeter, Color face, Color line, Color fine)
    {
        var size = pixelsPerMeter;
        var edge = Math.Max(2, size / 128) / 2;

        // Along each axis, 2 where a pixel is on a line of the line's color and 1 on a fine one.
        var kind = new byte[size];
        for (var at = 0; at < size; at++)
        {
            if (at < edge || at >= size - edge) kind[at] = 2;
        }

        void Line(double place, int width, byte which)
        {
            var start = (int)Math.Round(place - width / 2.0, MidpointRounding.AwayFromZero);
            for (var at = start; at < start + width; at++) kind[at] = Math.Max(kind[at], which);
        }

        Line(size / 2.0, Math.Max(1, size / 256), 2);
        Line(size / 4.0, Math.Max(1, size / 512), 1);
        Line(size * 3 / 4.0, Math.Max(1, size / 512), 1);

        ReadOnlySpan<byte> faceBytes = Bytes(face), lineBytes = Bytes(line), fineBytes = Bytes(fine);
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var which = Math.Max(kind[x], kind[y]);
                (which == 2 ? lineBytes : which == 1 ? fineBytes : faceBytes).CopyTo(pixels.AsSpan((y * size + x) * 4));
            }
        }

        // The mark of the cell's size, where a square of its letters is a pixel or more.
        var square = size / 128;
        if (square >= 1)
        {
            var (left, top) = (edge + 2 * square, edge + 2 * square);
            foreach (var (glyph, column) in new[] { (One, 0), (Em, 6) })
            {
                for (var row = 0; row < 7; row++)
                for (var bit = 0; bit < 5; bit++)
                {
                    if ((glyph[row] >> (4 - bit) & 1) == 0) continue;
                    for (var dy = 0; dy < square; dy++)
                    for (var dx = 0; dx < square; dx++)
                        lineBytes.CopyTo(pixels.AsSpan(((top + row * square + dy) * size + left + (column + bit) * square + dx) * 4));
                }
            }
        }

        return pixels;

        static byte[] Bytes(Color color)
        {
            var srgb = color.ToSrgb();
            return [To8(srgb.X), To8(srgb.Y), To8(srgb.Z), To8(color.A)];
        }

        static byte To8(float value) => (byte)Math.Clamp(MathF.Round(value * 255f), 0f, 255f);
    }

    // The mark's letters, five squares a row, the highest square the row's highest bit.
    private static readonly byte[] One = [0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110];
    private static readonly byte[] Em = [0b00000, 0b00000, 0b11010, 0b10101, 0b10101, 0b10101, 0b10101];
}

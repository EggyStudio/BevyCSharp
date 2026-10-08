using System.Buffers.Binary;
using System.IO.Compression;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// SHARED.md's ramp drawn through each of Bevy's eight tonemappers and held to the picture kept for
/// it under <c>BevyCSharp.Tests/references/tonemapping</c>, which 3DEngine's tonemappers are held
/// to as well.
/// </summary>
/// <remarks>
/// <para>
/// The ramp is a material of its own (<c>shaders/tonemap_ramp.slang</c>) on a plane filling the
/// view, so the value a pixel holds is the value written and nothing else, no light, exposure or
/// grading on it, on a camera with a high dynamic range target, no dither, no multisampling and no
/// bloom, so nothing stands between the material and the tonemapper either. The picture is drawn
/// into an eight-bit sRGB image and read back as it is.
/// </para>
/// <para>
/// <c>build/tonemap-references.sh</c> writes the pictures again through this test with
/// <c>BCS_WRITE_TONEMAP_REFERENCES</c> set, once Bevy or the bridge changes what a tonemapper draws
/// and the change is meant. Otherwise each is held to its reference within two levels of 255 for a
/// tonemapper worked out by a formula and four for the three Bevy draws through lookup tables,
/// which a GPU filters with a precision of its own, as 3DEngine holds its pictures to them.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class TonemapRampTests
{
    private const uint Width = 1024;
    private const uint Height = 8;

    /// <summary>Where the references are kept.</summary>
    internal static string References() =>
        Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp.Tests", "references", "tonemapping");

    /// <summary>The name Bevy gives a tonemapper, which names its reference.</summary>
    internal static string BevyName(Tonemapper tonemapper) =>
        tonemapper == Tonemapper.SomewhatBoring ? "SomewhatBoringDisplayTransform" : tonemapper.ToString();

    [SkippableTheory]
    [InlineData(Tonemapper.None)]
    [InlineData(Tonemapper.Reinhard)]
    [InlineData(Tonemapper.ReinhardLuminance)]
    [InlineData(Tonemapper.AcesFitted)]
    [InlineData(Tonemapper.AgX)]
    [InlineData(Tonemapper.SomewhatBoring)]
    [InlineData(Tonemapper.TonyMcMapface)]
    [InlineData(Tonemapper.BlenderFilmic)]
    public void TheRampThroughATonemapperIsItsReference(Tonemapper tonemapper)
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Width = Width,
            Height = Height,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Tonemapper = tonemapper, Dither = false, Msaa = 1 });

                // Turned to face the camera, and wide enough to fill a view 128 times wider than it
                // is high, whose sides a perspective camera six units back sees some 320 units out.
                var plane = ecs.Spawn();
                Render.SetMesh(ecs, plane, Render.CreateMesh(MeshShape.Plane, 2000f, 2000f));
                Render.SetMaterial(ecs, plane, Shaders.CreateMaterial(Shaders.CreateProgram("shaders/tonemap_ramp.slang")));
                ecs.Add(plane, new Transform(Vec3.Zero, Quat.FromRotationX(MathF.PI / 2f), Vec3.One));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("ramp").Go();
        var drawn = run.Picture("ramp");

        var file = Path.Combine(References(), BevyName(tonemapper) + ".png");
        if (Environment.GetEnvironmentVariable("BCS_WRITE_TONEMAP_REFERENCES") == "1")
        {
            Directory.CreateDirectory(References());
            File.WriteAllBytes(file, drawn.ToPng());
        }

        Assert.True(File.Exists(file), $"{file} is missing, which build/tonemap-references.sh writes.");
        var kept = Read(File.ReadAllBytes(file));
        Assert.Equal((Width, Height), (kept.Width, kept.Height));

        var allowed = tonemapper is Tonemapper.AgX or Tonemapper.TonyMcMapface or Tonemapper.BlenderFilmic ? 4 : 2;
        var worst = (Difference: 0, X: 0u, Y: 0u);
        for (var y = 0u; y < Height; y++)
        {
            for (var x = 0u; x < Width; x++)
            {
                var (r, g, b, _) = drawn.At(x, y);
                var (kr, kg, kb, _) = kept.At(x, y);
                var difference = Math.Max(Math.Abs(r - kr), Math.Max(Math.Abs(g - kg), Math.Abs(b - kb)));
                if (difference > worst.Difference) worst = (difference, x, y);
            }
        }

        Assert.True(
            worst.Difference <= allowed,
            $"{BevyName(tonemapper)} drew the ramp {worst.Difference} levels from its reference at ({worst.X}, {worst.Y}), "
                + $"{drawn.At(worst.X, worst.Y)} where {kept.At(worst.X, worst.Y)} is kept, past the {allowed} allowed.");
    }

    /// <summary>The ramp's own values, so a reference shows what the test says it does.</summary>
    /// <remarks>
    /// Read off the references rather than drawn, where every tonemapper agrees: the darkest column
    /// is black or nearly in every row, a column brightens or holds as it goes right, and the first
    /// row, the gray, has its three channels within a level of each other.
    /// </remarks>
    [Fact]
    public void TheReferencesHoldARampFromBlackThatNeverDarkens()
    {
        foreach (var tonemapper in Enum.GetValues<Tonemapper>())
        {
            var file = Path.Combine(References(), BevyName(tonemapper) + ".png");
            Assert.True(File.Exists(file), $"{file} is missing, which build/tonemap-references.sh writes.");
            var kept = Read(File.ReadAllBytes(file));

            for (var y = 0u; y < Height; y++)
            {
                var (r, g, b, _) = kept.At(0, y);
                Assert.True(Math.Max(r, Math.Max(g, b)) <= 8, $"{BevyName(tonemapper)}'s row {y} starts at {kept.At(0, y)}, not black.");

                for (var x = 1u; x < Width; x++)
                {
                    var before = kept.At(x - 1, y);
                    var now = kept.At(x, y);
                    var darkened = Luma(now) + 1 < Luma(before);
                    Assert.False(darkened, $"{BevyName(tonemapper)}'s row {y} darkens at column {x}, from {before} to {now}.");
                }
            }

            for (var x = 0u; x < Width; x++)
            {
                var (r, g, b, _) = kept.At(x, 0);
                Assert.True(Math.Max(Math.Abs(r - g), Math.Max(Math.Abs(g - b), Math.Abs(r - b))) <= 1, $"{BevyName(tonemapper)}'s gray is {kept.At(x, 0)} at column {x}.");
            }
        }
    }

    /// <summary>How bright a pixel is, by the weights of Rec. 709.</summary>
    private static double Luma((byte R, byte G, byte B, byte A) pixel) =>
        (0.2126 * pixel.R) + (0.7152 * pixel.G) + (0.0722 * pixel.B);

    /// <summary>
    /// Reads a PNG as <see cref="CapturedImage.ToPng"/> writes one, eight bits a channel in red,
    /// green, blue and alpha with every row stored unfiltered, which is every reference here.
    /// </summary>
    private static CapturedImage Read(byte[] png)
    {
        var width = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16));
        var height = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20));
        Assert.Equal(8, png[24]);
        Assert.Equal(6, png[25]);

        using var data = new MemoryStream();
        for (var at = 8; at + 8 <= png.Length;)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at));
            if (System.Text.Encoding.ASCII.GetString(png, at + 4, 4) == "IDAT") data.Write(png, at + 8, length);
            at += 12 + length;
        }

        data.Position = 0;
        using var rows = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionMode.Decompress)) zlib.CopyTo(rows);

        var stored = rows.ToArray();
        var stride = (int)width * 4;
        var pixels = new byte[stride * (int)height];
        for (var y = 0; y < height; y++)
        {
            Assert.Equal(0, stored[y * (stride + 1)]);
            Array.Copy(stored, (y * (stride + 1)) + 1, pixels, y * stride, stride);
        }

        return new CapturedImage(width, height, pixels);
    }
}

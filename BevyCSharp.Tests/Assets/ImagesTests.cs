using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the grid image a level is blocked out in, its lines at the pixels they are said to be at,
/// the chain of smaller levels an image is made with, each half the one above and averaged as
/// light, and the grid drawn with its levels on a floor.
/// </summary>
[Collection("engine")]
public sealed class ImagesTests
{
    private static readonly Color Face = Color.White;
    private static readonly Color Line = Color.Black;
    private static readonly Color Fine = Color.FromSrgb(0.5f, 0.5f, 0.5f);

    private static byte Red(byte[] pixels, int size, int x, int y) => pixels[(y * size + x) * 4];

    [Fact]
    public void AGridOfAHundredPixelsAMeterHasItsLinesAtTheMeterAndEveryTenth()
    {
        var pixels = Images.GridPixels(100, Face, Line, Fine);
        Assert.Equal(100 * 100 * 4, pixels.Length);

        // A meter's line two pixels wide, one on each edge, so cells tiled make it whole.
        Assert.Equal(0, Red(pixels, 100, 0, 50));
        Assert.Equal(0, Red(pixels, 100, 99, 50));
        Assert.Equal(0, Red(pixels, 100, 50, 0));

        // A tenth's line a pixel wide on each tenth, the meter's line over it where they cross, its
        // gray of half written as the sRGB byte 128.
        for (var tenth = 1; tenth < 10; tenth++)
        {
            Assert.Equal(128, Red(pixels, 100, tenth * 10, 55));
            Assert.Equal(128, Red(pixels, 100, 55, tenth * 10));
            Assert.Equal(0, Red(pixels, 100, tenth * 10, 0));
        }

        Assert.Equal(255, Red(pixels, 100, 55, 55));
        Assert.Equal(255, Red(pixels, 100, 11, 1));
    }

    [Fact]
    public void AGridOfTheDefaultSizeHasItsLinesWidenedWithIt()
    {
        var pixels = Images.GridPixels(Images.DefaultPixelsPerMeter, Face, Line, Fine);
        const int Size = Images.DefaultPixelsPerMeter;

        // A meter's line eight pixels wide, four on each edge.
        Assert.All([0, 3, 1020, 1023], x => Assert.Equal(0, Red(pixels, Size, x, 500)));
        Assert.Equal(255, Red(pixels, Size, 4, 500));

        // A tenth's line two pixels wide, starting a pixel before its tenth at 102.4.
        Assert.Equal((255, 128, 128, 255), (Red(pixels, Size, 100, 500), Red(pixels, Size, 101, 500), Red(pixels, Size, 102, 500), Red(pixels, Size, 103, 500)));
    }

    [Fact]
    public void AChainsLevelsAreEachHalfTheOneAboveAndTheMeanOfItsLight()
    {
        // Black and white in turn, whose mean as light is half, sRGB 188, where the mean of the
        // encoded numbers would be the darker 128.
        var checker = new byte[4 * 4 * 4];
        for (var at = 0; at < 16; at++)
        {
            var value = (byte)((at % 4 + at / 4) % 2 == 0 ? 0 : 255);
            (checker[at * 4], checker[at * 4 + 1], checker[at * 4 + 2], checker[at * 4 + 3]) = (value, value, value, 255);
        }

        var chain = Render.MipChain(checker, 4, 4, srgb: true, out var levels);
        Assert.Equal(3u, levels);
        Assert.Equal((16 + 4 + 1) * 4, chain.Length);
        Assert.Equal(checker, chain[..64]);
        Assert.All(Enumerable.Range(0, 5), pixel => Assert.Equal(new byte[] { 188, 188, 188, 255 }, chain[(64 + pixel * 4)..(68 + pixel * 4)]));

        // A row four wide halves to two and then one, the one pixel high it stays.
        Render.MipChain(new byte[4 * 4], 4, 1, srgb: false, out levels);
        Assert.Equal(3u, levels);
    }

    [SkippableFact]
    public void AFloorInThePrototypeGridIsDrawnWithItsTintFromItsLevels()
    {
        Needs.Renderer();
        CapturedImage? picture = null;
        var (same, other) = (false, false);

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d();
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 0f), new Vec3(0f, 0f, -20f), Vec3.UnitY));
                Render.SpawnLight(LightKind.Directional, 10_000f);

                var floor = new EditMesh().Plane(100f, 100f, Vec3.Zero, centered: true);
                var entity = ecs.Spawn();
                ecs.Add(entity, Transform.Identity);
                Render.SetMesh(ecs, entity, Modeling.Meshes(floor)[0]);

                var material = Modeling.Prototype("floor");
                Render.SetMaterial(ecs, entity, material);
                same = Modeling.Prototype("floor") == material;
                other = Modeling.Prototype("wall") != material;
            },
        };
        run.Wait(30).Capture("floor").Go();
        picture = run.Picture("floor");

        Assert.True(same, "the same name gives the same material");
        Assert.True(other, "another name gives another");

        // The floor below the horizon is drawn gray, the floor's tint, red, green and blue near one
        // another, dim under a light that grazes it and not black, as a texture whose levels did
        // not arrive would leave it, and the run logged no error from the GPU.
        var (r, g, b, _) = picture!.At(32, 40);
        Assert.InRange((int)r, 10, 255);
        Assert.InRange(Math.Abs(r - b), 0, 30);
        Assert.InRange(Math.Abs(g - b), 0, 30);
    }
}

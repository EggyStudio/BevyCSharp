using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// An image made from pixels and given a shape, a cube, a volume or an array, has that shape in
/// the system that asked, so what it is handed to there never sees it flat.
/// </summary>
/// <remarks>
/// The shape was applied at the next frame's reshaping, which a file still loading has to wait for
/// and an image made from pixels does not. In the meantime an irradiance volume refused the image
/// as flat, and a skybox given a cube warned that it was not one, both in the feature test.
/// </remarks>
[Collection("engine")]
public sealed class ImageShapeTests
{
    [SkippableFact]
    public void AnImageMadeFromPixelsTakesItsShapeAtOnce()
    {
        Needs.Renderer();

        (uint, uint, uint) cube = default, volume = default, layers = default;

        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, _ =>
        {
            cube = Shaped(4, 24, image => Render.MakeCubemap(image));
            volume = Shaped(4, 8, image => Render.MakeVolume(image, 2));
            layers = Shaped(4, 8, image => Render.MakeTextureArray(image, 2));
        });
        harness.Run();

        Assert.Equal((4u, 4u, 6u), cube);
        Assert.Equal((4u, 4u, 2u), volume);
        Assert.Equal((4u, 4u, 2u), layers);
    }

    /// <summary>
    /// The size of an image of gray pixels, <paramref name="width"/> by <paramref name="height"/>,
    /// read straight after <paramref name="shape"/> is asked of it.
    /// </summary>
    private static (uint, uint, uint) Shaped(uint width, uint height, Action<AssetHandle> shape)
    {
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, (byte)128);

        var image = Render.CreateImage(pixels, width, height);
        shape(image);

        return Render.TryImageSize(image, out var across, out var down, out var deep) ? (across, down, deep) : default;
    }
}

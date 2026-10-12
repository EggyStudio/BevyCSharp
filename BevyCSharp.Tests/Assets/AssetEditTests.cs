using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers meshes and images changed in place, as Bevy's assets are changed.</summary>
[Collection("engine")]
public sealed class AssetEditTests
{
    /// <summary>
    /// A mesh written over keeps its handle and holds the new vertices, and an image's texels are
    /// read, written over and read back as written, with a wrong length refused.
    /// </summary>
    [SkippableFact]
    public void AMeshAndAnImageAreChangedInPlace()
    {
        Needs.Renderer();

        MeshData? mesh = null;
        ImagePixels? read = null, inverted = null;
        BevyNativeException? refused = null;

        using var app = new App(new Config { Offscreen = true, Width = 32, Height = 32, HeadlessFps = 60, HeadlessFrames = 4 });
        app.AddSystem(Stage.Startup, new SystemDescriptor(_ =>
        {
            var triangle = Render.CreateMesh(new MeshData { Positions = [new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 1f, 0f)] });
            Render.WriteMesh(triangle, new MeshData { Positions = [new(0f, 0f, 0f), new(2f, 0f, 0f), new(0f, 2f, 0f)] });
            Render.TryReadMesh(triangle, out mesh);

            // Two texels, one black and one opaque red.
            var image = Render.CreateImage([0, 0, 0, 255, 255, 0, 0, 255], 2, 1, srgb: false);
            Render.TryReadImage(image, out read);
            Render.WriteImagePixels(image, read!.Data.Select(b => (byte)(255 - b)).ToArray());
            Render.TryReadImage(image, out inverted);
            refused = Assert.Throws<BevyNativeException>(() => Render.WriteImagePixels(image, new byte[3]));
        }, "Test.Edit"));

        Assert.Equal(0, app.Run());

        Assert.Equal(new Vec3(2f, 0f, 0f), mesh!.Positions[1]);
        Assert.Equal((2u, 1u, 4u), (read!.Width, read.Height, read.BytesPerTexel));
        Assert.Equal(new byte[] { 0, 0, 0, 255, 255, 0, 0, 255 }, read.Data);
        Assert.Equal(new byte[] { 255, 255, 255, 0, 0, 255, 255, 0 }, inverted!.Data);
        Assert.Equal(NativeStatus.NullArgument, refused!.Status);
    }
}

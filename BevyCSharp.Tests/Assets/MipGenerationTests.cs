using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers mipmaps made on the GPU from an image's top level, as Bevy's <c>MipGenerationJobs</c>
/// makes them (<see cref="Render.SetMipGeneration"/>).
/// </summary>
[Collection("engine")]
public sealed class MipGenerationTests
{
    /// <summary>
    /// A level below the top of an image whose mipmaps are made is the top's red, where the same
    /// level of an image whose mipmaps are not made stays clear, and an sRGB image is refused,
    /// since the downsampler writes no sRGB format.
    /// </summary>
    [SkippableFact]
    public void ALevelBelowTheTopIsMadeFromItWhileGenerationIsOn()
    {
        Needs.Shaders();

        InvalidOperationException? refused = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/one_level.slang" });

                foreach (var (x, made) in new[] { (-24f, true), (24f, false) })
                {
                    var picture = Render.CreateImage(Red(64), 64, 64, srgb: false);
                    Render.SetMipGeneration(picture, true);
                    if (!made) Render.SetMipGeneration(picture, false);

                    var square = ecs.Spawn();
                    ecs.Add(square, Transform.At(x, 0f, 0f) with { Scale = new Vec3(32f) });
                    Render2d.SetMesh(ecs, square, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
                    Render2d.SetMaterial(ecs, square, Shaders.CreateMaterial2d(program, AlphaMode2d.Blend)
                        .Set("level", 3u)
                        .SetTexture("picture", picture));
                }

                refused = Assert.Throws<InvalidOperationException>(() => Render.SetMipGeneration(Render.CreateImage(Red(4), 4, 4), true));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        Assert.True(picture.At(24, 48) is { R: > 240, G: < 10 }, $"the made level was {picture.At(24, 48)}");
        Assert.True(picture.At(72, 48) is { R: < 90 }, $"the level never made was {picture.At(72, 48)}");
        Assert.NotNull(refused);
    }

    // Every pixel opaque red, a size square.
    private static byte[] Red(int size)
    {
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4) (pixels[i], pixels[i + 3]) = (255, 255);
        return pixels;
    }
}

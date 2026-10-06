using Bevy;
using Bevy.Interop;
using Xunit;
using static Bevy.Tests.ComputeShaderTests;

namespace Bevy.Tests;

/// <summary>
/// Covers compute shaders that write images, of any format, a region or a block at a time and a
/// level of a pyramid at a time, each read back.
/// </summary>
/// <remarks>
/// Beside <see cref="ComputeShaderTests"/>, which holds the buffers and shares its way of making an
/// instance, as one file of both would be too long to read whole.
/// </remarks>
[Collection("engine")]
public sealed class ComputeImageTests
{
    /// <summary>
    /// An image a compute shader wrote is read back texel by texel in order, its rows without the
    /// padding the GPU's copy gives them, which a row three texels long has most of.
    /// </summary>
    [SkippableFact]
    public void AnImageAComputeShaderWroteIsReadBackInOrder()
    {
        Needs.Shaders();

        var write = default(ShaderInstance);
        var image = AssetHandle.None;
        var read = default(BufferRead);
        uint[]? texels = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                image = Shaders.CreateImage(3, 5, ShaderImageFormat.R32UInt);
                write = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Compute = ShaderStage.Slang("""
                        import bcs_compute;

                        [format("r32ui")] RWTexture2D<uint> texels;

                        [shader("compute")]
                        [numthreads(1, 1, 1)]
                        void main(uint3 id : SV_DispatchThreadID)
                        {
                            texels[id.xy] = id.y * 10 + id.x;
                        }
                        """),
                })).SetTexture("texels", image);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("writing", _ => Shaders.Dispatch(write, 3, 5))
            .Wait(2)
            .Do("asking for it back", _ => read = Shaders.BeginImageRead(image))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out texels))
            .Go();

        Assert.NotNull(texels);
        Assert.Equal(
            [0u, 1u, 2u, 10u, 11u, 12u, 20u, 21u, 22u, 30u, 31u, 32u, 40u, 41u, 42u],
            texels);
    }

    /// <summary>
    /// A material samples what a compute shader wrote into an image, and a float image beside it
    /// holds numbers no eight-bit image could.
    /// </summary>
    [SkippableFact]
    public void AComputeShaderWritesImagesOfAnyFormat()
    {
        Needs.Shaders();

        var paint = default(ShaderInstance);
        var copy = default(ShaderInstance);
        var into = AssetHandle.None;
        var read = default(BufferRead);
        System.Numerics.Vector4[]? copied = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);

                var image = Shaders.CreateImage(16, 16);
                var precise = Shaders.CreateImage(16, 16, ShaderImageFormat.Rgba32Float);
                into = Shaders.CreateBuffer(16);

                paint = Compute("shaders/paint_image.slang")
                    .Set("color", ShaderMaterialTests.Green)
                    .SetTexture("image", image)
                    .SetTexture("precise", precise);

                copy = Compute("shaders/copy_image.slang")
                    .SetTexture("source", precise)
                    .SetBuffer("into", into);

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(Shaders.CreateProgram("shaders/sampled.slang")).SetTexture("picture", image));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("painting", _ =>
            {
                Shaders.Dispatch(paint, 2, 2);
                Shaders.Dispatch(copy, 1);
            })
            .Wait(10)
            .Capture("picture")
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.True(PictureRun.Green(run.Picture("picture")) > 100, "the cube did not show the painted image");

        Assert.NotNull(copied);
        Assert.Equal(new System.Numerics.Vector4(1000.5f, -2.25f, 3f, 1f), copied[0]);
    }

    /// <summary>
    /// An image's mip levels are bound one at a time, so one dispatch reads a level while the next
    /// writes the level below, and the whole image is read afterwards.
    /// </summary>
    [SkippableFact]
    public void AnImagePyramidIsBuiltALevelAtATime()
    {
        Needs.Shaders();

        var fill = default(ShaderInstance);
        var halve = default(ShaderInstance);
        var readLevels = default(ShaderInstance);
        var into = AssetHandle.None;
        var read = default(BufferRead);
        float[]? levels = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                var pyramid = Shaders.CreateImage(16, 16, ShaderImageFormat.R32Float, mips: 3);
                into = Shaders.CreateBuffer(16);

                fill = Compute("shaders/fill_image_level.slang").Set("value", 0.75f).SetTexture("level", pyramid, mip: 0);
                halve = Compute("shaders/halve_level.slang")
                    .SetTexture("above", pyramid, mip: 0)
                    .SetTexture("below", pyramid, mip: 1);
                readLevels = Compute("shaders/read_level.slang").SetTexture("pyramid", pyramid).SetBuffer("into", into);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("building", _ =>
            {
                Shaders.Dispatch(fill, 2, 2);
                Shaders.Dispatch(halve, 1, 1);
                Shaders.Dispatch(readLevels, 1);
            })
            .Wait(2)
            .Do("asking for the levels", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out levels))
            .Go();

        Assert.NotNull(levels);
        Assert.Equal(0.75f, levels[0]);
        Assert.Equal(1.5f, levels[1]);
    }

    /// <summary>
    /// Writing a region of an image changes those texels and no others, at the level asked for,
    /// and a region outside the image is refused.
    /// </summary>
    [SkippableFact]
    public void ARegionOfAnImageIsWritten()
    {
        Needs.Shaders();

        AssetHandle into = default;
        ShaderInstance copy = default;
        BufferRead read = default;
        float[]? copied = null;
        Exception? outside = null;

        new PictureRun
        {
            Scene = _ =>
            {
                var image = Shaders.CreateImage(8, 8, ShaderImageFormat.R32Float, mips: 2);

                // Two by two at three, four, and one texel of the second level.
                Shaders.WriteImage<float>(image, [1f, 2f, 3f, 4f], x: 3, y: 4, width: 2, height: 2);
                Shaders.WriteImage<float>(image, [9f], x: 0, y: 0, width: 1, height: 1, mip: 1);

                outside = Record.Exception(() => Shaders.WriteImage<float>(image, [1f, 2f], x: 7, y: 0, width: 2, height: 1));

                into = Shaders.CreateBuffer(65 * 4);
                copy = Compute("shaders/read_image.slang").SetTexture("image", image).SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("copying the image", _ => Shaders.Dispatch(copy, 1))
            .Wait(2)
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);
        Assert.IsType<BevyNativeException>(outside);

        Assert.Equal(1f, copied[4 * 8 + 3]);
        Assert.Equal(2f, copied[4 * 8 + 4]);
        Assert.Equal(3f, copied[5 * 8 + 3]);
        Assert.Equal(4f, copied[5 * 8 + 4]);
        Assert.Equal(9f, copied[64]);

        // Everything else is as it was made.
        Assert.Equal(60, copied.Take(64).Count(value => value == 0f));
    }

    /// <summary>
    /// A block-compressed image made empty takes one four by four block written into it, which the
    /// GPU decodes where the block is and nowhere else, and a region off the block grid is refused.
    /// </summary>
    [SkippableFact]
    public void ABlockIsWrittenIntoACompressedImage()
    {
        Needs.Shaders();

        AssetHandle into = default;
        ShaderInstance copy = default;
        BufferRead read = default;
        float[]? copied = null;
        Exception? offGrid = null;

        new PictureRun
        {
            Scene = _ =>
            {
                var image = Shaders.CreateImage(8, 8, ShaderImageFormat.Bc4);

                // A BC4 block whose two end values are both 200 and whose sixteen indices all pick
                // the first, so every texel of it decodes to 200 out of 255.
                byte[] block = [200, 200, 0, 0, 0, 0, 0, 0];
                Shaders.WriteImage<byte>(image, block, x: 4, y: 4, width: 4, height: 4);

                offGrid = Record.Exception(() => Shaders.WriteImage<byte>(image, block, x: 2, y: 0, width: 4, height: 4));

                into = Shaders.CreateBuffer(64 * 4);
                copy = Compute("shaders/read_block.slang").SetTexture("image", image).SetBuffer("into", into);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(3)
            .Do("copying the image", _ => Shaders.Dispatch(copy, 1))
            .Wait(2)
            .Do("asking for the copy", _ => read = Shaders.BeginBufferRead(into))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out copied))
            .Go();

        Assert.NotNull(copied);
        Assert.IsType<BevyNativeException>(offGrid);

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var expected = x >= 4 && y >= 4 ? 200f / 255f : 0f;
                Assert.Equal(expected, copied[y * 8 + x], 2);
            }
        }
    }
}

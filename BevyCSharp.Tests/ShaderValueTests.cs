using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a shader may declare and how its values are set by name: far more of everything
/// than a fixed layout would hold, every shape of texture, structs and matrices, and the errors a
/// wrong name or shape gets.
/// </summary>
/// <remarks>
/// Each shader reads the <i>last</i> of whatever it declares many of, so a picture of the right
/// color says every element before it was laid out and bound as well, rather than a prefix the
/// size of some limit.
/// </remarks>
[Collection("engine")]
public sealed class ShaderValueTests
{
    private const uint Settled = ShaderMaterialTests.Settled;

    private static bool CanRun => ShaderMaterialTests.CanRun;

    private static bool Ready() => ShaderMaterialTests.ProgramsReady();

    /// <summary>The middle of a cube drawn with <paramref name="material"/>, once it has compiled.</summary>
    private static (byte R, byte G, byte B) Middle(Func<EcsWorld, AssetHandle> material)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, material(ecs));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var pixel = run.Picture("picture").At(48, 48);
        return (pixel.R, pixel.G, pixel.B);
    }

    private static void AssertGreen((byte R, byte G, byte B) pixel, string what) =>
        Assert.True(pixel.G > 120 && pixel.R < 90 && pixel.B < 90, $"{what} came out {pixel} rather than green");

    [Fact]
    public void AThousandNumbersReachTheShader()
    {
        if (!CanRun) return;

        var middle = Middle(_ =>
        {
            // All zero but the last, so a shader that read any other element draws black.
            var weights = new float[1000];
            weights[^1] = 1f;

            return Shaders.CreateMaterial(Shaders.CreateProgram("shaders/many.slang"))
                .Set("weights", weights)
                .Set("color", ShaderMaterialTests.Green);
        });

        AssertGreen(middle, "the thousandth weight");
    }

    /// <summary>Sixty-four textures in one material, sampled by index.</summary>
    [Fact]
    public void SixtyFourTexturesAreBound()
    {
        if (!CanRun) return;

        var middle = Middle(_ =>
        {
            var red = Render.CreateImage([255, 0, 0, 255], 1, 1);
            var green = Render.CreateImage([0, 255, 0, 255], 1, 1);

            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/layers.slang"))
                .Set("pick", 63u)
                .SetSampler("linear", SamplerSettings.Nearest);

            for (var i = 0; i < 63; i++) material.SetTexture("layers", red, i);
            return material.SetTexture("layers", green, 63);
        });

        AssertGreen(middle, "the sixty-fourth texture");
    }

    /// <summary>Sixteen cubemaps in one material, of which the last is sampled.</summary>
    [Fact]
    public void SixteenCubemapsAreBound()
    {
        if (!CanRun) return;

        var middle = Middle(_ =>
        {
            // Six faces stacked, one pixel each, with the top face green and every other red, so
            // looking straight up is the only way to see green.
            byte[] faces =
            [
                255, 0, 0, 255, 255, 0, 0, 255, 0, 255, 0, 255,
                255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255,
            ];

            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/skies.slang"));

            var sky = Render.CreateImage(faces, 1, 6);
            Render.MakeCubemap(sky);
            return material.SetTexture("skies", sky, 15);
        });

        AssertGreen(middle, "the sixteenth cubemap");
    }

    /// <summary>A 3D texture and an array texture, each bound as the shape the shader declares.</summary>
    [Fact]
    public void VolumesAndArraysAreBoundByShape()
    {
        if (!CanRun) return;

        var middle = Middle(_ =>
        {
            // A volume two slices deep, black in front and green behind, and an array of two
            // layers, black then black, so all the green is from the back of the volume.
            var fog = Render.CreateImage([0, 0, 0, 255, 0, 255, 0, 255], 1, 2);
            Render.MakeVolume(fog, 2);

            var stack = Render.CreateImage([0, 0, 0, 255, 0, 0, 0, 255], 1, 2);
            Render.MakeTextureArray(stack, 2);

            return Shaders.CreateMaterial(Shaders.CreateProgram("shaders/volume.slang"))
                .SetTexture("fog", fog)
                .SetTexture("stack", stack)
                .SetSampler("nearest", SamplerSettings.Nearest with { AddressW = SamplerAddress.Clamp });
        });

        AssertGreen(middle, "the back of the volume");
    }

    /// <summary>
    /// A field of a struct in an array in a constant buffer is found by its path, and a whole
    /// struct is set as its bytes.
    /// </summary>
    [Fact]
    public void AFieldIsFoundByItsPath()
    {
        if (!CanRun) return;

        var middle = Middle(_ => Shaders.CreateMaterial(Shaders.CreateProgram("shaders/lights.slang"))
            .Set("lighting.count", 4)
            .Set("lighting.lights[2].color", new Vector3(1f, 0f, 0f))
            .SetStruct("lighting.lights[3]", new Light(new Vector3(0f, 0.5f, 0f), 2f))
            .Set("lighting.ambient", Vector4.Zero));

        AssertGreen(middle, "the fourth light");
    }

    /// <summary>A C# matrix is read by the shader the way <c>mul</c> reads it.</summary>
    [Fact]
    public void AMatrixArrivesTheRightWayRound()
    {
        if (!CanRun) return;

        // A rotation taking x to y, written row by row as C# writes a matrix. `mul(m, v)` dots each
        // row with v, so the first column is where x goes, and a matrix that arrived transposed
        // would send x to black.
        var twist = new Matrix4x4(
            0f, 0f, 0f, 0f,
            1f, 0f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f);

        var middle = Middle(_ => Shaders.CreateMaterial(Shaders.CreateProgram("shaders/twist.slang"))
            .Set("twist", twist));

        AssertGreen(middle, "x turned by the matrix");
    }

    /// <summary>
    /// A C# struct in a raw buffer is read back field by field by the same struct declared in
    /// Slang.
    /// </summary>
    [Fact]
    public void ACSharpStructIsReadByTheSameStructInSlang()
    {
        if (!CanRun) return;

        var middle = Middle(_ =>
        {
            // Only the third is green at full size, so a stride or an offset that is wrong
            // anywhere reads one of the others, or a mixture.
            var particles = Shaders.CreateBuffer<Particle>(
            [
                new Particle(1f, 1f, 0f, 0f),
                new Particle(0.2f, 0f, 0f, 1f),
                new Particle(1f, 0f, 1f, 0f),
                new Particle(1f, 1f, 1f, 1f),
            ]);

            return Shaders.CreateMaterial(Shaders.CreateProgram("shaders/particles.slang"))
                .SetBuffer("particles", particles);
        });

        AssertGreen(middle, "the third particle");
    }

    /// <summary>
    /// A buffer takes as many elements as it is given, and the shader reads the last of them.
    /// </summary>
    [Fact]
    public void ABufferHoldsAsMuchAsItIsGiven()
    {
        if (!CanRun) return;

        var middle = Middle(_ =>
        {
            // A thousand colors, all red but the last, which is sixteen kilobytes.
            var colors = new Vector4[1000];
            Array.Fill(colors, ShaderMaterialTests.Red);
            colors[^1] = ShaderMaterialTests.Green;

            return Shaders.CreateMaterial(Shaders.CreateProgram("shaders/data.slang"))
                .SetBuffer("colors", Shaders.CreateBuffer<Vector4>(colors));
        });

        AssertGreen(middle, "the last color");
    }

    /// <summary>An image made from floats keeps numbers no eight-bit image could.</summary>
    [Fact]
    public void AnImageOfFloatsKeepsItsNumbers()
    {
        if (!CanRun) return;

        var middle = Middle(_ => Shaders.CreateMaterial(Shaders.CreateProgram("shaders/load_float.slang"))
            .SetTexture("heights", Shaders.CreateImage<float>(2, 1, ShaderImageFormat.R32Float, [0.25f, 1234.5f]))
            .Set("expected", 1234.5f));

        AssertGreen(middle, "the float image");
    }

    /// <summary>Texels of the wrong size for the image are refused rather than read as noise.</summary>
    [Fact]
    public void TexelsOfTheWrongSizeAreRefused()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.CreateImage<float>(2, 2, ShaderImageFormat.R32Float, [1f, 2f, 3f]));
    }

    /// <summary>Two materials of different layouts draw side by side in one frame.</summary>
    [Fact]
    public void MaterialsOfDifferentLayoutsDrawTogether()
    {
        if (!CanRun) return;

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            Scene = ecs =>
            {
                ShaderMaterialTests.Row(ecs);

                var weights = new float[1000];
                weights[^1] = 1f;

                PictureRun.Cube(ecs, ShaderMaterialTests.Flat(ShaderMaterialTests.Green), 1f, ShaderMaterialTests.InRow(0));

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(Shaders.CreateProgram("shaders/many.slang"))
                        .Set("weights", weights)
                        .Set("color", ShaderMaterialTests.Red),
                    1f,
                    ShaderMaterialTests.InRow(1));

                PictureRun.Cube(
                    ecs,
                    Shaders.CreateMaterial(Shaders.CreateProgram("shaders/data.slang"))
                        .SetBuffer("colors", Shaders.CreateBuffer<Vector4>([ShaderMaterialTests.Blue])),
                    1f,
                    ShaderMaterialTests.InRow(2));
            },
        };

        run.Until("compiled", _ => Ready()).Wait(Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var (green, red, blue) = (picture.At(32, 32), picture.At(96, 32), picture.At(160, 32));

        Assert.True(green.G > 120 && green.R < 90, $"the flat cube came out {green}");
        Assert.True(red.R > 120 && red.G < 90, $"the weighted cube came out {red}");
        Assert.True(blue.B > 120 && blue.G < 90, $"the buffered cube came out {blue}");
    }

    /// <summary>
    /// An edit that adds a parameter gives the material a layout with it, and every value set
    /// before the edit is kept by name.
    /// </summary>
    [Fact]
    public void AnEditThatAddsAParameterKeepsTheValuesByName()
    {
        if (!CanRun) return;

        using var assets = PictureRun.Temporary();
        assets.Write("grow.slang", """
            import bcs;

            uniform float4 color;

            [shader("fragment")]
            float4 fragment(bcs::VertexOutput mesh) : SV_Target
            {
                return color;
            }
            """);

        var program = ShaderProgram.None;
        var material = default(ShaderMaterial);
        var seen = 0;
        IReadOnlyList<ShaderParameter> grown = [];

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("grow.slang");
                material = Shaders.CreateMaterial(program).Set("color", ShaderMaterialTests.Green);
                PictureRun.Cube(ecs, material);
            },
        };

        run.Until("compiled", _ => program.State == ShaderProgramState.Ready)
            .Wait(Settled)
            .Capture("before")
            .Do("adding a parameter before the one there is", _ =>
            {
                seen = program.Generation;

                // Declared first, so the color moves to a different offset, which a value kept as
                // bytes rather than by name would get wrong.
                assets.Write("grow.slang", """
                    import bcs;

                    uniform float4 tint;
                    uniform float4 color;

                    [shader("fragment")]
                    float4 fragment(bcs::VertexOutput mesh) : SV_Target
                    {
                        return color + tint;
                    }
                    """);
            })
            .Until("recompiled", _ => program.Generation > seen)
            .Wait(30)
            .Capture("kept")
            .Do("setting the new one", _ =>
            {
                grown = material.Parameters;
                material.Set("tint", new Vector4(0f, 0f, 1f, 0f));
            })
            .Wait(10)
            .Capture("tinted")
            .Go();

        Assert.True(PictureRun.Green(run.Picture("before")) > 100, "the cube did not start green");
        Assert.True(PictureRun.Green(run.Picture("kept")) > 100, "the color was lost in the reload");
        Assert.Equal(["tint", "color"], grown.Select(parameter => parameter.Name));

        var tinted = run.Picture("tinted").At(48, 48);
        Assert.True(tinted.G > 120 && tinted.B > 120, $"the new parameter did nothing, leaving {tinted}");
    }

    /// <summary>
    /// A name the shader does not declare, or a value of the wrong shape, is refused with the
    /// names it does declare, once the program has compiled, and kept before, when nothing can be
    /// checked.
    /// </summary>
    [Fact]
    public void AWrongNameOrShapeIsRefusedWithWhatThereIs()
    {
        if (!CanRun) return;

        Exception? unknown = null;
        Exception? scalar = null;
        Exception? texture = null;
        Exception? early = null;

        var material = default(ShaderMaterial);
        var image = AssetHandle.None;

        new PictureRun
        {
            Scene = _ =>
            {
                material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/flat.slang"));
                image = Render.CreateImage([0, 0, 0, 255], 1, 1);

                // Not compiled yet, so there is nothing to check this against.
                early = Record.Exception(() => material.Set("tint", 1f));
            },
        }
            .Until("compiled", _ => Ready())
            .Do("setting what is not there", _ =>
            {
                unknown = Record.Exception(() => material.Set("tint", ShaderMaterialTests.Green));
                scalar = Record.Exception(() => material.Set("color", 1f));
                texture = Record.Exception(() => material.SetTexture("color", image));
            })
            .Go();

        Assert.Null(early);

        // The name asked for, and the one the shader does declare.
        var wrongName = Assert.IsType<ArgumentException>(unknown);
        Assert.Contains("tint", wrongName.Message);
        Assert.Contains("color", wrongName.Message);

        Assert.IsType<ArgumentException>(scalar);
        Assert.IsType<ArgumentException>(texture);
    }

    /// <summary>What the Slang shader calls a light, padded to sixteen bytes as a uniform lays it.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Light(Vector3 Color, float Power);

    /// <summary>The C# side of the struct <c>particles.slang</c> declares.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Particle(float Size, float R, float G, float B);
}

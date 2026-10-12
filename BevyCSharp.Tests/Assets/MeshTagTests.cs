using System.Numerics;
using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a shader telling meshes apart by their tag, and an image loaded as layers.</summary>
[Collection("engine")]
public sealed class MeshTagTests
{
    /// <summary>A picture of four layers stacked, two pixels wide and two tall each, red, green, blue and white.</summary>
    private static byte[] Layers()
    {
        (byte R, byte G, byte B)[] colors = [(255, 0, 0), (0, 255, 0), (0, 0, 255), (255, 255, 255)];
        var pixels = new byte[2 * 8 * 4];
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var (r, g, b) = colors[y / 2];
                var at = (y * 2 + x) * 4;
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = (r, g, b, 255);
            }
        }

        return new CapturedImage(2, 8, pixels).ToPng();
    }

    /// <summary>
    /// Four cubes sharing one material, each with its own MeshTag, show the layer of a stacked
    /// picture loaded as an array texture that their tag names, which takes the tag reaching the
    /// fragment shader through the instance index and the picture being cut into layers.
    /// </summary>
    [SkippableFact]
    public void EachMeshShowsTheLayerItsTagNames()
    {
        Needs.Shaders();
        using var assets = PictureRun.Temporary();
        File.WriteAllBytes(Path.Combine(assets.Root, "layers.png"), Layers());

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            AssetRoot = assets.Root,
            Scene = ecs =>
            {
                ShaderMaterialTests.Row(ecs);
                var layers = AssetServer.LoadImage("layers.png", new TextureSettings { Layers = 4, Srgb = false });
                var program = Shaders.CreateProgram(ShaderStage.Slang("""
                    import bcs;

                    Texture2DArray layers;
                    SamplerState nearest;

                    [shader("fragment")]
                    float4 fragment(bcs::VertexOutput mesh) : SV_Target
                    {
                        return layers.Sample(nearest, float3(0.5, 0.5, float(bcs::tag(mesh.instance_index))));
                    }
                    """));
                var material = Shaders.CreateMaterial(program).SetTexture("layers", layers).SetSampler("nearest", SamplerSettings.Nearest);

                for (var i = 0; i < 4; i++)
                {
                    var cube = PictureRun.Cube(ecs, material, 1f, ShaderMaterialTests.InRow(i + 1));
                    ecs.Insert<MeshTagRef>(cube).Value = (uint)i;
                }
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        // Each cube's middle is at the middle of its sixth of the picture.
        var picture = run.Picture("picture");
        var seen = Enumerable.Range(1, 4).Select(i => picture.At((uint)(32 + 64 * i), 32)).ToArray();
        Assert.True(seen[0] is { R: > 200, G: < 50, B: < 50 }, $"the cube tagged 0 was {seen[0]}");
        Assert.True(seen[1] is { R: < 50, G: > 200, B: < 50 }, $"the cube tagged 1 was {seen[1]}");
        Assert.True(seen[2] is { R: < 50, G: < 50, B: > 200 }, $"the cube tagged 2 was {seen[2]}");
        Assert.True(seen[3] is { R: > 200, G: > 200, B: > 200 }, $"the cube tagged 3 was {seen[3]}");
    }
}

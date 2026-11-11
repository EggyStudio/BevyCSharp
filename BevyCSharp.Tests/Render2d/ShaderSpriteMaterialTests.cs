using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a 2D material a Slang program draws a sprite with, as Bevy's <c>SpriteMaterial</c>
/// draws one, the fragment shader reading the sprite through <c>bcs_sprite</c>.
/// </summary>
[Collection("engine")]
public sealed class ShaderSpriteMaterialTests
{
    /// <summary>
    /// A sprite drawn with a material is its image times its color times what the material sets,
    /// and changes when the material's value changes, set through the sprite, and when the
    /// sprite's color does.
    /// </summary>
    [SkippableFact]
    public void ASpritesMaterialReadsTheSpriteAndFollowsBothAsTheyChange()
    {
        Needs.Shaders();

        var bird = Entity.None;
        var image = AssetHandle.None;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                image = White(alpha: 255);
                bird = Sprite(ecs, image, (1f, 1f, 0f, 1f), Material(alpha: null, tint: new Vector4(0f, 1f, 1f, 1f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("green")
            .Do("tinting it magenta", _ => Shaders.MaterialOn(bird).Set("tint", new Vector4(1f, 0f, 1f, 1f)))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("red")
            .Do("making the sprite magenta", world => Render2d.SetSprite(world.Resource<EcsWorld>(), bird, image, new SpriteSettings
            {
                Color = (1f, 0f, 1f, 1f),
                Size = (40f, 40f),
            }))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("magenta")
            .Go();

        var green = run.Picture("green");
        Assert.True(green.At(48, 48) is { G: > 240, R: < 10, B: < 10 }, $"yellow by cyan was {green.At(48, 48)}");
        Assert.True(green.At(4, 4) is { G: < 90 }, $"outside the sprite was {green.At(4, 4)}");
        Assert.True(run.Picture("red").At(48, 48) is { R: > 240, G: < 10, B: < 10 }, $"yellow by magenta was {run.Picture("red").At(48, 48)}");
        Assert.True(run.Picture("magenta").At(48, 48) is { R: > 240, G: < 10, B: > 240 }, $"magenta by magenta was {run.Picture("magenta").At(48, 48)}");
    }

    /// <summary>
    /// The sprite keeps its size and its anchor under a material, here forty pixels a side hung
    /// from its bottom left corner, so it reaches up and right from the middle.
    /// </summary>
    [SkippableFact]
    public void ASpriteUnderAMaterialKeepsItsSizeAndAnchor()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var bird = ecs.Spawn();
                ecs.Add(bird, Transform.Identity);
                Render2d.SetSprite(ecs, bird, White(alpha: 255), new SpriteSettings { Size = (40f, 40f), Anchor = (-0.5f, -0.5f) });
                Render2d.SetMaterial(ecs, bird, Material(alpha: null, tint: new Vector4(0f, 1f, 0f, 1f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        Assert.True(picture.At(68, 28) is { G: > 240 }, $"above and right of the middle was {picture.At(68, 28)}");
        Assert.True(picture.At(28, 68) is { G: < 90 }, $"below and left of the middle was {picture.At(28, 68)}");
        Assert.True(picture.At(92, 28) is { G: < 90 }, $"past the sprite's width was {picture.At(92, 28)}");
    }

    /// <summary>
    /// A sprite half transparent blends as a sprite does under a material that names no alpha
    /// mode, is drawn whole under an opaque one, and is not drawn under one masking it below a
    /// cutoff its alpha does not reach.
    /// </summary>
    [SkippableTheory]
    [InlineData(null)]
    [InlineData(AlphaMode2d.Opaque)]
    [InlineData(AlphaMode2d.Mask)]
    public void ASpritesAlphaFollowsTheMaterialOrElseTheSprite(AlphaMode2d? alpha)
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                Sprite(ecs, White(alpha: 128), (1f, 1f, 1f, 1f), Material(alpha, tint: new Vector4(0f, 1f, 0f, 1f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        switch (alpha)
        {
            case null:
                Assert.True(middle is { G: > 100 and < 230, R: > 10 }, $"half green over the gray was {middle}");
                break;
            case AlphaMode2d.Opaque:
                Assert.True(middle is { G: > 240, R: < 10 }, $"an opaque sprite was {middle}");
                break;
            default:
                Assert.True(middle is { G: < 90 }, $"a sprite masked away was {middle}");
                break;
        }
    }

    /// <summary>
    /// A program that reads the sprite draws nothing on a 2D mesh, which has no sprite to bind.
    /// </summary>
    [SkippableFact]
    public void AProgramReadingTheSpriteDrawsNoMesh()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var square = ecs.Spawn();
                ecs.Add(square, Transform.Identity with { Scale = new Vec3(40f) });
                Render2d.SetMesh(ecs, square, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
                Render2d.SetMaterial(ecs, square, Material(alpha: null, tint: new Vector4(0f, 1f, 0f, 1f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle is { G: < 90 }, $"the square was drawn, {middle}");
    }

    /// <summary>
    /// A program with a 2D vertex shader of its own draws no sprite, whose quad is placed by the
    /// sprite's own vertex shader.
    /// </summary>
    [SkippableFact]
    public void AProgramWithItsOwnVertexShaderDrawsNoSprite()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Fragment2d = "shaders/flat_material_2d.slang",
                    Vertex2d = new ShaderStage("shaders/flat_material_2d.slang"),
                });
                Sprite(ecs, White(alpha: 255), (1f, 1f, 1f, 1f), Shaders.CreateMaterial2d(program).Set("color", new Vector4(0f, 1f, 0f, 1f)));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var middle = run.Picture("picture").At(48, 48);
        Assert.True(middle is { G: < 90 }, $"the sprite was drawn, {middle}");
    }

    private static ShaderMaterial Material(AlphaMode2d? alpha, Vector4 tint)
    {
        var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/sprite_tint.slang" });
        return Shaders.CreateMaterial2d(program, alpha, cutoff: 0.75f).Set("tint", tint);
    }

    // Two pixels square of white at the alpha given.
    private static AssetHandle White(byte alpha)
    {
        var pixels = new byte[2 * 2 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = pixels[i + 1] = pixels[i + 2] = 255;
            pixels[i + 3] = alpha;
        }

        return Render.CreateImage(pixels, 2, 2);
    }

    // A sprite forty pixels across in the middle of the picture, drawn with the material.
    private static Entity Sprite(EcsWorld ecs, AssetHandle image, (float, float, float, float) color, ShaderMaterial material)
    {
        var sprite = ecs.Spawn();
        ecs.Add(sprite, Transform.Identity);
        Render2d.SetSprite(ecs, sprite, image, new SpriteSettings { Color = color, Size = (40f, 40f) });
        Render2d.SetMaterial(ecs, sprite, material);
        return sprite;
    }
}

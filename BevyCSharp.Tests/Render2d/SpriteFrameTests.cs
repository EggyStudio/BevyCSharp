using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers sprites moved through the frames of their sheets together, in one call.
/// </summary>
[Collection("engine")]
public sealed class SpriteFrameTests
{
    /// <summary>
    /// Sprites each move to the frame beside them, keeping their sheet, and an entity with no
    /// sprite is passed over and left out of the count, as an animation outliving one of its
    /// sprites would have it.
    /// </summary>
    [SkippableFact]
    public void SpritesMoveToTheirFramesTogether()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var moved = -1;
        TextureAtlas? first = null, second = null;
        var layoutKept = AssetHandle.None;
        var layout = AssetHandle.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var ecs = ctx.Ecs;
            layout = Render2d.CreateAtlas(24, 24, 7, 1);
            var image = Render.CreateImage([255, 255, 255, 255], 1, 1);

            var drawn = ecs.Spawn();
            Render2d.SetSprite(ecs, drawn, image, new SpriteSettings { Atlas = layout, Frame = 0 });

            var other = ecs.Spawn();
            Render2d.SetSprite(ecs, other, image, new SpriteSettings { Atlas = layout, Frame = 0 });

            var bare = ecs.Spawn();

            moved = Render2d.SetSpriteFrames([drawn, other, bare], [3u, 5u, 6u]);
            first = ecs.Wrap<SpriteRef>(drawn).TextureAtlas;
            second = ecs.Wrap<SpriteRef>(other).TextureAtlas;
            layoutKept = ecs.GetReflectedAsset(other, SpriteRef.TypePath, ".texture_atlas.0.layout") ?? AssetHandle.None;
        });

        harness.Run();

        Assert.Equal(2, moved);
        Assert.Equal(new TextureAtlas(3), first);
        Assert.Equal(new TextureAtlas(5), second);
        Assert.Equal(layout, layoutKept);
    }

    /// <summary>Runs of different lengths are refused before anything crosses.</summary>
    [Fact]
    public void AFrameIsAskedForEachSprite()
    {
        Assert.Throws<ArgumentException>(() => Render2d.SetSpriteFrames([new Entity(1)], []));
    }
}

using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Bevy's outlines of interface nodes, which show where a layout put each box.</summary>
[Collection("engine")]
public sealed class UiDebugOutlineTests
{
    /// <summary>
    /// Bevy's outlines of interface nodes, turned on for every node, draw the edge of a node that
    /// draws nothing of its own, and turned off leave it unseen, as Bevy's image_node_resizing has
    /// them.
    /// </summary>
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void BevysOutlinesDrawTheEdgeOfANodeThatDrawsNothing(bool outlined)
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(20f), Top = Length.Px(20f), Width = Length.Px(50f), Height = Length.Px(50f) });
                if (ecs.Resource<GlobalUiDebugOptionsRef>() is { } options) options.Enabled = outlined;
            },
        };
        run.Wait(10).Capture("picture").Go();

        // Down the node's left edge, and in its middle, where only the clear color is either way.
        var picture = run.Picture("picture");
        var middle = picture.At(45, 45);
        var edge = Enumerable.Range(18, 5).Select(x => picture.At((uint)x, 45)).Any(pixel => pixel != middle);
        Assert.Equal(outlined, edge);
    }
}

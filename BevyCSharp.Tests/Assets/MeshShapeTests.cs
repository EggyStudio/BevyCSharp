using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a sector and a segment whose image is mapped at an angle, <see cref="MeshShape.UvAngle"/>.</summary>
/// <remarks>
/// The angle turns the texture coordinates alone, which nothing reads back here, so Bevy's
/// <c>mesh2d_arcs</c> shows them. What is pinned down is the rest, that the shape is the sector or
/// the segment its first two numbers make, that the recipe names it so it is made again the same
/// way, and that a shape with no arc is refused.
/// </remarks>
[Collection("engine")]
public sealed class MeshShapeTests
{
    /// <summary>A sector or a segment mapped at an angle has the triangles of the one mapped straight, and its recipe keeps the angle.</summary>
    [SkippableFact]
    public void AnArcMappedAtAnAngleIsTheSameShapeWithItsRecipe()
    {
        Needs.Renderer();

        var ran = false;
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, _ =>
        {
            foreach (var arc in new[] { MeshShape.CircularSector, MeshShape.CircularSegment })
            {
                var straight = Render.CreateMesh(arc, 40f, MathF.PI / 4f);
                var turned = Render.CreateMesh(MeshShape.UvAngle(arc), 40f, MathF.PI / 4f, -MathF.PI / 4f);

                Assert.True(Render.TryReadMesh(straight, out var first), arc);
                Assert.True(Render.TryReadMesh(turned, out var second), arc);
                Assert.Equal(first!.Positions, second!.Positions);
                Assert.Equal(new MeshRecipe(MeshShape.UvAngle(arc), 40f, MathF.PI / 4f, -MathF.PI / 4f), Render.RecipeOf(turned));
            }

            var refused = Assert.Throws<BevyNativeException>(() => Render.CreateMesh(MeshShape.UvAngle(MeshShape.Circle), 1f, 1f, 1f));
            Assert.Equal(NativeStatus.NoComponent, refused.Status);
            ran = true;
        });

        harness.Run();
        Assert.True(ran);
    }
}

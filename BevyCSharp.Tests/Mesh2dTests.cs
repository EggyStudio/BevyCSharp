using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers 2D meshes drawn with Bevy's <c>ColorMaterial</c>, which a 2D camera draws in the plane it
/// looks at.
/// </summary>
[Collection("engine")]
public sealed class Mesh2dTests
{
    /// <summary>
    /// A flat shape with a color material is drawn in its color where it stands, and a material
    /// written over is drawn in its new color by every mesh sharing it.
    /// </summary>
    [SkippableFact]
    public void AFlatShapeIsDrawnInItsMaterialsColorAndChangesWithIt()
    {
        Needs.Renderer();

        var material = AssetHandle.None;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                material = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (1f, 0f, 0f, 1f) });

                // A hexagon on the left and a disc on the right, sharing the one material.
                foreach (var (shape, x) in new[] { (MeshShape.RegularPolygon, -24f), (MeshShape.Circle, 24f) })
                {
                    var entity = ecs.Spawn();
                    ecs.Add(entity, Transform.At(x, 0f, 0f));
                    Render2d.SetMesh(ecs, entity, Render.CreateMesh(shape, 16f, 6f));
                    Render2d.SetMaterial(ecs, entity, material);
                }
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("red")
            .Do("turning the material blue", _ => Render2d.WriteMaterial(material, new ColorMaterialSettings { Color = (0f, 0f, 1f, 1f) }))
            .Wait(10)
            .Capture("blue")
            .Go();

        var red = run.Picture("red");
        var blue = run.Picture("blue");
        var (left, right, middle) = ((uint)(red.Width / 2 - 24), (uint)(red.Width / 2 + 24), red.Height / 2);

        foreach (var x in new[] { left, right })
        {
            var (r, g, b, _) = red.At(x, middle);
            Assert.True(r > 200 && g < 60 && b < 60, $"the shape at {x} was not drawn red, but ({r}, {g}, {b})");
            (r, g, b, _) = blue.At(x, middle);
            Assert.True(b > 200 && r < 60 && g < 60, $"the shape at {x} was not drawn blue once the material was, but ({r}, {g}, {b})");
        }

        // Between the two shapes is only the clear color.
        var (gr, gg, gb, _) = red.At(red.Width / 2, middle);
        Assert.False(gr > 200 && gg < 60 && gb < 60, "the space between the shapes was drawn red");
    }

    /// <summary>
    /// A ring of a flat shape draws a band along the inside of the shape's outline and leaves its
    /// middle empty, for an outline Bevy insets and for one it is given the inside of.
    /// </summary>
    [SkippableFact]
    public void ARingDrawsItsBandAndLeavesItsMiddleEmpty()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Width = 200,
            Height = 100,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var red = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (1f, 0f, 0f, 1f) });

                // A square band on the left, inset evenly, and an elliptical one on the right,
                // whose inside is an ellipse smaller on each axis.
                foreach (var (mesh, x) in new[]
                {
                    (Render.CreateMesh(MeshShape.Ring(MeshShape.Rectangle), 60f, 60f, 10f), -40f),
                    (Render.CreateMesh(MeshShape.Ring(MeshShape.Ellipse), 30f, 30f, 10f), 40f),
                })
                {
                    var entity = ecs.Spawn();
                    ecs.Add(entity, Transform.At(x, 0f, 0f));
                    Render2d.SetMesh(ecs, entity, mesh);
                    Render2d.SetMaterial(ecs, entity, red);
                }
            },
        };

        run.Wait(ShaderMaterialTests.Settled).Capture("rings").Go();

        var picture = run.Picture("rings");
        var (center, middle) = (picture.Width / 2, picture.Height / 2);
        foreach (var x in new[] { center - 40, center + 40 })
        {
            var (r, g, b, _) = picture.At((uint)(x - 25), middle);
            Assert.True(r > 200 && g < 60 && b < 60, $"the band of the ring at {x} was not drawn, but ({r}, {g}, {b})");
            (r, g, b, _) = picture.At((uint)x, middle);
            Assert.False(r > 200 && g < 60 && b < 60, $"the middle of the ring at {x} was drawn");
        }
    }
}

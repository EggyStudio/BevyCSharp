using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The captures of the modeling operations, drawn on the device and read from the picture, a box
/// loop cut round its sides at half its height, its lower half in a slot of its own, its top inset
/// and pulled up into a panel and every edge beveled round, and a box beveled and subdivided by its
/// modifiers into a rounded one.
/// </summary>
/// <remarks>
/// <see cref="BevelTests"/>, <see cref="FillAndMirrorTests"/> and <see cref="ModifierTests"/> hold
/// each operation's counts and volumes on the CPU. These draw them, so the operations meet each
/// other's faces, the slots they carry and the renderer's prototype material in one mesh.
/// </remarks>
[Collection("engine")]
public sealed class ModelingPictureTests
{
    [SkippableFact]
    public void ABeveledInsetAndLoopCutBoxIsDrawnInItsTwoSlotsSplitAtTheLoop()
    {
        Needs.Renderer();

        // The loop cut first, on the box's upright edge, while the box's own corners keep their
        // numbers, then the faces under the loop in the band's slot.
        var box = new EditMesh().Box(new Vec3(2f), centered: true)
            .SelectEdges([(0, 4)]).LoopCut(0.5f)
            .SelectInside(new Vec3(-2f, -2f, -2f), new Vec3(2f, 0.01f, 2f)).Material("band")
            .SlotColor("band", 0.85f, 0.15f, 0.1f)
            .SelectFacing(Vec3.UnitY).Inset(0.4f).Extrude(0.3f)
            .SelectAll().Bevel(0.12f, 3);

        Assert.True(box.IsClosed, "the box is closed after the four operations");

        var picture = Drawn(box, new Vec3(3.2f, 2.6f, 4.2f), new Vec3(0f, 0.1f, 0f));

        // The band's red under the loop and the prototype's gray over it, each a share of the box,
        // the red's rows on the whole below the gray's.
        var (red, redRow) = Rows(picture, (r, g, b) => r > g + 60 && r > b + 60);
        var (gray, grayRow) = Rows(picture, (r, g, b) => r > 40 && Math.Abs(r - g) < 20 && Math.Abs(g - b) < 20);
        Assert.True(red > 400, $"{red} pixels of the band's red");
        Assert.True(gray > 400, $"{gray} pixels of the prototype's gray");
        Assert.True(redRow > grayRow + 10, $"the red's rows average {redRow:0} and the gray's {grayRow:0}");
    }

    /// <summary>
    /// A box beveled in two segments and subdivided twice by its modifiers, seen straight on, is
    /// the plain box's square with its corners rounded off and its sides kept, where the base under
    /// the modifiers stays the box.
    /// </summary>
    [SkippableFact]
    public void ABoxBeveledAndSubdividedByItsModifiersIsDrawnRound()
    {
        Needs.Renderer();

        var plain = new EditMesh().Box(new Vec3(2f), centered: true);
        var round = new EditMesh().Box(new Vec3(2f), centered: true).AddModifier("bevel 0.3 2").AddModifier("subdivision 2");
        Assert.Equal((8, 6), (round.VertexCount, round.FaceCount));
        Assert.True(round.Built().IsClosed, "the built shape is closed");

        var eye = new Vec3(0f, 0f, 6f);
        var square = Drawn(plain, eye, Vec3.Zero);
        var rounded = Drawn(round, eye, Vec3.Zero);

        // The plain box's square, the lit pixels' bounds.
        static bool Lit(CapturedImage picture, uint x, uint y)
        {
            var (r, g, b, _) = picture.At(x, y);
            return r + g + b > 30;
        }

        uint left = square.Width, right = 0, top = square.Height, bottom = 0;
        for (var y = 0u; y < square.Height; y++)
            for (var x = 0u; x < square.Width; x++)
                if (Lit(square, x, y)) (left, right, top, bottom) = (Math.Min(left, x), Math.Max(right, x), Math.Min(top, y), Math.Max(bottom, y));
        Assert.True(right > left + 40 && bottom > top + 40, $"the plain box spans {left} to {right} across and {top} to {bottom} down");

        // Each corner of the square two pixels in is dark in the rounded box, and the middle of each
        // side a tenth of the square in is lit in both.
        var (inX, inY) = ((right - left) / 10, (bottom - top) / 10);
        var (middleX, middleY) = ((left + right) / 2, (top + bottom) / 2);
        foreach (var (x, y) in new[] { (left + 2, top + 2), (right - 2, top + 2), (left + 2, bottom - 2), (right - 2, bottom - 2) })
        {
            Assert.True(Lit(square, x, y), $"the plain box's corner at {x}, {y} is lit");
            Assert.False(Lit(rounded, x, y), $"the rounded box's corner at {x}, {y} is cut away");
        }
        foreach (var (x, y) in new[] { (middleX, top + inY), (middleX, bottom - inY), (left + inX, middleY), (right - inX, middleY) })
            Assert.True(Lit(rounded, x, y), $"the rounded box's side at {x}, {y} is drawn");
    }

    /// <summary>A model drawn in its slots under a sun from above and in front, seen from an eye, and its picture.</summary>
    private static CapturedImage Drawn(EditMesh model, Vec3 eye, Vec3 target)
    {
        var run = new PictureRun
        {
            Width = 160,
            Height = 120,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(eye, target, Vec3.UnitY));

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(2f, 4f, 3f), Vec3.Zero, Vec3.UnitY));

                var meshes = Modeling.Meshes(model);
                var materials = Modeling.Materials(model);
                for (var slot = 0; slot < meshes.Length; slot++)
                {
                    if (meshes[slot] == AssetHandle.None) continue;
                    var part = ecs.Spawn();
                    ecs.Add(part, Transform.Identity);
                    Render.SetMesh(ecs, part, meshes[slot]);
                    Render.SetMaterial(ecs, part, materials[slot]);
                }
            },
        };

        run.Wait(20).Capture("model").Go();
        return run.Picture("model");
    }

    /// <summary>How many pixels pass a test and the mean of their rows, down from the top.</summary>
    private static (int Count, double Row) Rows(CapturedImage picture, Func<int, int, int, bool> test)
    {
        var (count, rows) = (0, 0.0);
        for (var y = 0u; y < picture.Height; y++)
        {
            for (var x = 0u; x < picture.Width; x++)
            {
                var (r, g, b, _) = picture.At(x, y);
                if (!test(r, g, b)) continue;
                count++;
                rows += y;
            }
        }

        return (count, count > 0 ? rows / count : 0);
    }
}

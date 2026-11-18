using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A white floor with a red one 2 cm over it, within one of the field's cells of 0.25 m, the
    /// white one hidden and then shown again: the cell under the white floor's top, which its
    /// nearest surface paints, reads white, then the red floor's red while the white one is hidden,
    /// then white again once it is shown, read back from the image the probes' rays read.
    /// </summary>
    /// <remarks>
    /// A floor a scene stands on lies within a cell of the scene's own floor, and the field paints
    /// a cell by its nearest surface whichever floor that is, so a game hides the floor a scene
    /// covers and the field leaves it out as the picture does, and takes it back when the scene
    /// goes.
    /// </remarks>
    [SkippableFact]
    public void AnEntityHiddenStandsOutOfTheFieldAndInItAgainWhenShown()
    {
        Needs.Shaders();
        var white = Entity.None;
        var origin = Vector3.Zero;
        var read = default(BufferRead);
        byte[]? paint = null;
        var seen = new List<(int Drawn, (byte R, byte G, byte B, byte A) Cell)>();
        var run = new PictureRun
        {
            Width = 32,
            Height = 32,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                white = Plate(ecs, (0.9f, 0.9f, 0.9f), new Vec3(0f, -0.1f, 0f), new Vec3(20f, 0.2f, 20f));
                Plate(ecs, (0.8f, 0.05f, 0.05f), new Vec3(0f, 0.025f, 0f), new Vec3(4f, 0.01f, 4f));
                Render.SetSceneField(1, 0.25f);
            },
        };

        // The field built with every mesh it gathered still, the cell read back and kept with how
        // many meshes the field holds.
        void Read(int drawn) => run
            .Until($"settled with {drawn}", _ => SceneField.Plan is { } plan && SceneField.DrawnThisFrame.Count == drawn
                && plan.StillCount == drawn && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Wait(3)
            .Do($"asking for the field's colors with {drawn}", _ =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                read = Shaders.BeginImageRead(SceneField.Albedo);
            })
            .Until($"read back with {drawn}", _ => Shaders.TryReadBuffer(read, out paint))
            .Do($"keeping the cell with {drawn}", _ => seen.Add((SceneField.DrawnThisFrame.Count, Cell(new Vector3(0.1f, -0.1f, 0.1f)))));

        (byte R, byte G, byte B, byte A) Cell(Vector3 p)
        {
            var cell = (p - origin) / 0.25f;
            var (x, y, z) = ((int)MathF.Floor(cell.X), (int)MathF.Floor(cell.Y), (int)MathF.Floor(cell.Z));
            var at = ((z * SceneFieldPlan.Resolution + y) * SceneFieldPlan.Resolution + x) * 4;
            return (paint![at], paint[at + 1], paint[at + 2], paint[at + 3]);
        }

        Read(2);
        run.Do("hiding the white floor", world => world.Resource<EcsWorld>().GetRef<Visibility>(white) = Visibility.Hidden);
        Read(1);
        run.Do("showing it again", world => world.Resource<EcsWorld>().GetRef<Visibility>(white) = Visibility.Inherited);
        Read(2);
        run.Go();

        Assert.Equal(3, seen.Count);
        var (shown, hidden, again) = (seen[0].Cell, seen[1].Cell, seen[2].Cell);
        Assert.True(shown is { R: > 150, G: > 150, B: > 150, A: 255 }, $"with both floors in the field the cell under the white one's top reads {shown}");
        Assert.True(hidden is { R: > 100, G: < 30, B: < 30, A: 255 }, $"with the white floor hidden the cell reads {hidden}");
        Assert.True(again == shown, $"with the white floor shown again the cell reads {again} where it read {shown}");
    }

    // A block of a color, standing in the field as any still mesh that casts a shadow does.
    private static Entity Plate(EcsWorld ecs, (float R, float G, float B) color, Vec3 at, Vec3 size)
    {
        var plate = ecs.Spawn();
        Render.SetMesh(ecs, plate, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
        Render.SetMaterial(ecs, plate, Render.CreateMaterial(color.R, color.G, color.B, roughness: 1f));
        ecs.Add(plate, Transform.At(at.X, at.Y, at.Z));
        ecs.Add(plate, Visibility.Inherited);
        return plate;
    }
}

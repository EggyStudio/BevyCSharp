using System.Numerics;
using Bevy;
using BevyCSharp.FeatureTest.Behaviors;
using Levels;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The feature test's level built into a world, with a red floor two centimeters over the scenes
/// yard's site as a scene's ground stands, and the game's rule for the yard's floor under the site
/// (<see cref="SceneFloor"/>, one file of the game compiled in for this test) hiding it while the
/// scene stands and showing it again when the scene goes.
/// </summary>
[Collection("engine")]
public sealed class FeatureTestSceneFloorTests
{
    /// <summary>
    /// The cell under the yard floor's top at the site, which its nearest surface paints, reads the
    /// yard's floor while it stands, the red floor's red while the rule hides it and the yard's
    /// floor again once the rule shows it, read back from the image the probes' rays read.
    /// </summary>
    /// <remarks>
    /// The field paints a cell by its nearest surface whichever floor that is, and under Sponza the
    /// yard's white floor painted the cells of Sponza's stone floor near white, which lit the walls
    /// under its arcades at twice the reference's light.
    /// </remarks>
    [SkippableFact]
    public void TheYardsFloorUnderASceneStandsOutOfTheFieldWhileTheSceneStandsAndInItAgainWhenItGoes()
    {
        Needs.Shaders();
        var site = new Vector3(70f, 0f, -70f);
        var origin = Vector3.Zero;
        var read = default(BufferRead);
        byte[]? paint = null;
        var (hidden, shown) = (-1, -1);
        var seen = new List<(int Drawn, (byte R, byte G, byte B, byte A) Cell)>();
        var run = new PictureRun
        {
            Width = 32,
            Height = 32,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                ecs.GetRef<Transform>(camera) = Transform.LookingAt(new Vec3(site.X, 3f, site.Z + 8f), new Vec3(site.X, 0f, site.Z), Vec3.UnitY);
                // The level without the meadow's trees, whose cards' pictures are captured over
                // frames the run does not wait for, so a capture would end unread with the run.
                var level = Level.Parse(File.ReadAllText(FeatureTestLevelTests.LevelPath), FeatureTestLevelTests.LevelPath);
                level.Entities.RemoveAll(entity => entity.ClassName == "tree");
                LevelSpawner.Spawn(ecs, level, "feature-test");
                var ground = ecs.Spawn();
                Render.SetMesh(ecs, ground, Render.CreateMesh(MeshShape.Cuboid, 4f, 0.01f, 4f));
                Render.SetMaterial(ecs, ground, Render.CreateMaterial(0.8f, 0.05f, 0.05f, roughness: 1f));
                ecs.Add(ground, Transform.At(site.X, 0.025f, site.Z));
                Render.SetSceneField(1, 0.25f);
            },
        };

        // The field built with every mesh it gathered still, after the count it holds has moved
        // from the one read before where a floor was hidden or shown, the cell read back and kept.
        void Read(string name, Func<int, bool> drawn) => run
            .Until($"settled {name}", _ => SceneField.Plan is { } plan && drawn(SceneField.DrawnThisFrame.Count)
                && plan.StillCount == SceneField.DrawnThisFrame.Count && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Wait(3)
            .Do($"asking for the field's colors {name}", _ =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                read = Shaders.BeginImageRead(SceneField.Albedo);
            })
            .Until($"read back {name}", _ => Shaders.TryReadBuffer(read, out paint))
            .Do($"keeping the cell {name}", _ => seen.Add((SceneField.DrawnThisFrame.Count, Cell(site + new Vector3(0.1f, -0.1f, 0.1f)))));

        (byte R, byte G, byte B, byte A) Cell(Vector3 p)
        {
            var cell = (p - origin) / 0.25f;
            var (x, y, z) = ((int)MathF.Floor(cell.X), (int)MathF.Floor(cell.Y), (int)MathF.Floor(cell.Z));
            var at = ((z * SceneFieldPlan.Resolution + y) * SceneFieldPlan.Resolution + x) * 4;
            return (paint![at], paint[at + 1], paint[at + 2], paint[at + 3]);
        }

        Read("with the yard's floor", count => count > 1);
        run.Do("the scene standing", world => hidden = SceneFloor.Show(world.Resource<EcsWorld>(), shown: false));
        Read("with the scene standing", count => hidden != 1 || count < seen[0].Drawn);
        run.Do("the scene gone", world => shown = SceneFloor.Show(world.Resource<EcsWorld>(), shown: true));
        Read("with the scene gone", count => shown != 1 || count == seen[0].Drawn);
        run.Go();

        Assert.True(hidden == 1 && shown == 1, $"the rule hid {hidden} floors and showed {shown}");
        var (yard, standing, gone) = (seen[0].Cell, seen[1].Cell, seen[2].Cell);
        Assert.True(yard is { R: > 150, G: > 150, B: > 150, A: 255 }, $"with the yard's floor in the field the cell under its top reads {yard}");
        Assert.True(standing is { R: > 100, G: < 30, B: < 30, A: 255 }, $"with the scene standing the cell reads {standing}");
        Assert.True(gone == yard, $"with the scene gone the cell reads {gone} where it read {yard}");
    }
}

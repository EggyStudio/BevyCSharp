using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A red pane Bevy blends at 0.35 of its alpha, as a faint decal over a wall is blended, stands
    /// in the field as nothing, the cells beside it left unpainted, where a pane blended at 0.8
    /// paints the cells beside it red as an opaque one does, read back from the image the probes'
    /// rays read.
    /// </summary>
    /// <remarks>
    /// The wall a decal lies on shows through it as it is drawn, and held solid the decal stands in
    /// the field as a wall of its own color over the wall beneath, which the probes then read
    /// darkened by the dirt where the picture shows the stone through it.
    /// </remarks>
    [SkippableFact]
    public void ADrawBlendedUnderHalfItsAlphaStandsInTheFieldAsNothing()
    {
        Needs.Shaders();
        var origin = Vector3.Zero;
        var drawn = 0;
        var read = default(BufferRead);
        byte[]? paint = null;
        new PictureRun
        {
            Width = 32,
            Height = 32,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                Floor(ecs, (0.9f, 0.9f, 0.9f));
                Pane(ecs, 0.35f, new Vec3(-2f, 1.5f, 0f));
                Pane(ecs, 0.8f, new Vec3(2f, 1.5f, 0f));
                Render.SetSceneField(1, 0.25f);
            },
        }
            .Until("settled", _ => SceneField.Plan is { } plan && SceneField.DrawnThisFrame.Count >= 2
                && plan.StillCount == SceneField.DrawnThisFrame.Count && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Wait(3)
            .Do("asking for the field's colors", _ =>
            {
                drawn = SceneField.DrawnThisFrame.Count;
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                read = Shaders.BeginImageRead(SceneField.Albedo);
            })
            .Until("read back", _ => Shaders.TryReadBuffer(read, out paint))
            .Go();
        Assert.NotNull(paint);

        (byte R, byte G, byte B, byte A) At(Vector3 p)
        {
            var cell = (p - origin) / 0.25f;
            var (x, y, z) = ((int)cell.X, (int)cell.Y, (int)cell.Z);
            var at = ((z * SceneFieldPlan.Resolution + y) * SceneFieldPlan.Resolution + x) * 4;
            return (paint![at], paint[at + 1], paint[at + 2], paint[at + 3]);
        }

        var faint = At(new Vector3(-2.1f, 1.6f, 0.1f));
        Assert.True(drawn == 2 && faint.A == 0, $"the field holds {drawn} meshes and a cell beside the pane blended at 0.35 reads {faint}");
        var strong = At(new Vector3(2.1f, 1.6f, 0.1f));
        Assert.True(strong.A == 255 && strong.R > 100 && strong.G < 30 && strong.B < 30, $"a cell beside the pane blended at 0.8 reads {strong}");
    }

    // A red pane standing upright, blended at the alpha given.
    private static void Pane(EcsWorld ecs, float alpha, Vec3 at)
    {
        var pane = ecs.Spawn();
        Render.SetMesh(ecs, pane, Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 0.1f));
        Render.SetMaterial(ecs, pane, Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (0.8f, 0.05f, 0.05f, alpha),
            Roughness = 1f,
            AlphaMode = AlphaMode.Blend,
        }));
        ecs.Add(pane, Transform.At(at.X, at.Y, at.Z));
    }
}

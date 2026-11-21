using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class SceneFieldTests
{
    /// <summary>
    /// A ball whose corners are painted apart, built into the field by one app and then by another,
    /// paints every cell of the field the same in both.
    /// </summary>
    /// <remarks>
    /// Two triangles of one mesh tie on a cell whose middle lies as near both along the same way,
    /// in the steps of a cell the code keeps, since the code holds the distance, the way and the
    /// mesh's instance and nothing of the triangle. Both then paint the cell (splatPaint), each the
    /// color of its own nearest point, which differs where its corners do, and the field's splat
    /// keeps the larger color in its bytes (field_splat.slang). Stored as they came, the cell kept
    /// whichever triangle's store landed last, and 226 of the ball's 17,416 cells took another
    /// color in the second build, so the probes of a still scene read other light each start.
    /// </remarks>
    [SkippableFact]
    public void TwoBuildsOfOneSceneGiveTheSamePaint()
    {
        Needs.Shaders();
        var first = Painted();
        var second = Painted();
        Assert.Equal(first.Length, second.Length);
        var (apart, painted) = (0, 0);
        for (var i = 0; i < first.Length; i += 4)
        {
            if (first[i + 3] > 0) painted++;
            if (first[i] != second[i] || first[i + 1] != second[i + 1] || first[i + 2] != second[i + 2] || first[i + 3] != second[i + 3]) apart++;
        }
        Assert.True(painted > 1000, $"the ball paints {painted} of the field's cells");
        Assert.True(apart == 0, $"{apart} of the {painted} cells the ball paints take another color in the second build");
    }

    // The field's paint of the ball, four bytes a cell, as one app builds it.
    private static byte[] Painted()
    {
        byte[]? bytes = null;
        var read = default(BufferRead);
        new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 1f, 5f));
                var ball = ecs.Spawn();
                Render.SetMesh(ecs, ball, Render.CreateMesh(PaintedBall(24, 16, 1.5f)));
                Render.SetMaterial(ecs, ball, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 0.8f }));
                ecs.Add(ball, Transform.At(0f, 1.5f, 0f));
                ecs.Add(ball, Visibility.Inherited);
                Render.SetSceneField(2, 0.1f);
            },
        }
            .Until("settled", _ => SceneField.Plan is { } plan && plan.StillCount == 1 && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Wait(4)
            .Do("asking for the paint", _ => read = Shaders.BeginImageRead(SceneField.Albedo))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out bytes) && bytes is not null)
            .Go();
        return bytes!;
    }

    // A ball of `around` by `down` quads, each corner a color of its own from its place in the
    // grid.
    private static MeshData PaintedBall(int around, int down, float radius)
    {
        var positions = new List<Vec3>();
        var colors = new List<float>();
        for (var j = 0; j <= down; j++)
            for (var i = 0; i <= around; i++)
            {
                var (theta, phi) = (MathF.PI * j / down, 2f * MathF.PI * i / around);
                positions.Add(new Vec3(radius * MathF.Sin(theta) * MathF.Cos(phi), radius * MathF.Cos(theta), radius * MathF.Sin(theta) * MathF.Sin(phi)));
                var k = (uint)(j * 7919 + i * 104729);
                colors.AddRange([(k % 7) / 6f, (k / 7 % 5) / 4f, (k / 35 % 3) / 2f, 1f]);
            }
        var indices = new List<uint>();
        for (var j = 0; j < down; j++)
            for (var i = 0; i < around; i++)
            {
                var (a, b) = ((uint)(j * (around + 1) + i), (uint)((j + 1) * (around + 1) + i));
                indices.AddRange([a, b, a + 1, a + 1, b, b + 1]);
            }
        return new MeshData { Positions = [.. positions], Colors = [.. colors], Indices = [.. indices] };
    }
}

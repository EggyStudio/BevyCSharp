using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Moving entities in the editor's hierarchy without moving them in the world.
/// </summary>
/// <remarks>
/// Checked against the world transform Bevy works out itself a frame later, rather than against
/// the same arithmetic the editor did, so a mistake in that arithmetic cannot agree with itself.
/// In the engine collection, since it runs a real app.
/// </remarks>
[Collection("engine")]
public sealed class EditorHierarchyTests
{
    [Fact]
    public void AnEntityMovedUnderAnotherStaysWhereItStandsAndComesBack()
    {
        using var harness = new EngineHarness(frames: 8);

        var parent = Entity.None;
        var child = Entity.None;
        var frame = 0;

        var moved = false;
        var parentOf = Entity.None;
        GlobalTransform before = default;
        GlobalTransform after = default;
        var undoneParent = Entity.None;
        Transform undoneLocal = default;

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;

            switch (frame)
            {
                case 1:
                    EditorHistory.Clear();

                    // Turned, scaled and lifted, so a transform carried over unchanged would put
                    // the child somewhere else on every axis.
                    parent = ctx.Ecs.Spawn();
                    ctx.Ecs.Add(parent, new Transform
                    {
                        Translation = new Vec3(0f, 2f, 0f),
                        Rotation = Quat.FromRotationY(MathF.PI * 0.5f),
                        Scale = new Vec3(2f, 2f, 2f),
                    });

                    child = ctx.Ecs.Spawn();
                    ctx.Ecs.Add(child, Transform.At(3f, 0f, 1f));
                    break;

                case 3:
                    ctx.Ecs.TryGet(child, out before);
                    moved = EditorHierarchy.Reparent(ctx.Ecs, child, parent);
                    parentOf = ctx.Ecs.ParentOf(child);
                    break;

                case 5:
                    ctx.Ecs.TryGet(child, out after);
                    EditorHistory.Undo(ctx.Ecs);
                    undoneParent = ctx.Ecs.ParentOf(child);
                    ctx.Ecs.TryGet(child, out undoneLocal);
                    break;
            }
        });

        harness.Run();

        Assert.True(moved);
        Assert.Equal(parent, parentOf);

        // Bevy's own answer before the move, so a world transform that was never worked out, and
        // read back as zeros on both sides, cannot pass for one that stayed put.
        Near(new Vec3(3f, 0f, 1f), before.Translation);
        Near(before.Translation, after.Translation);
        Near(before.XAxis, after.XAxis);
        Near(before.ZAxis, after.ZAxis);

        Assert.True(undoneParent.IsNone);
        Near(new Vec3(3f, 0f, 1f), undoneLocal.Translation);
    }

    [Fact]
    public void AnEntityCannotGoUnderItsOwnDescendant()
    {
        using var harness = new EngineHarness(frames: 2);

        var refused = false;
        var allowed = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            var top = ctx.Ecs.Spawn();
            var middle = ctx.Ecs.Spawn();
            var bottom = ctx.Ecs.Spawn();

            ctx.Ecs.SetParent(middle, top);
            ctx.Ecs.SetParent(bottom, middle);

            refused = !EditorHierarchy.Reparent(ctx.Ecs, top, bottom, record: false)
                && !EditorHierarchy.Reparent(ctx.Ecs, top, top, record: false);

            // And the other way, which is only a move.
            allowed = EditorHierarchy.Reparent(ctx.Ecs, bottom, top, record: false);
        });

        harness.Run();

        Assert.True(refused);
        Assert.True(allowed);
    }

    private static void Near(Vec3 expected, Vec3 actual) =>
        Assert.True(
            (expected - actual).Length < 1e-3f,
            $"expected {expected}, got {actual}");
}

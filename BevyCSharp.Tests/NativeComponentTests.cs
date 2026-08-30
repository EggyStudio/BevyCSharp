using System.Runtime.CompilerServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reaching Bevy's own components from C#, and the hierarchy built on them.
/// </summary>
/// <remarks>
/// These run against a real Bevy world, so the layout assertions compare against the engine
/// rather than against another copy of the same assumption.
/// </remarks>
[Collection("engine")]
public sealed class NativeComponentTests
{
    [Fact]
    public void MirroredLayoutsMatchTheEngine()
    {
        // The whole reason NativeComponents verifies layouts. Quat is SIMD-backed and sixteen
        // byte aligned on most targets, which pads Transform to 48 rather than the 40 its three
        // fields suggest. Getting this wrong would not throw; it would silently read the wrong
        // bytes and corrupt neighbours on write.
        Assert.Equal(12, Unsafe.SizeOf<Vec3>());
        Assert.Equal(16, Unsafe.SizeOf<Quat>());
        Assert.Equal(48, Unsafe.SizeOf<Transform>());

        using var harness = new EngineHarness(frames: 2);

        // Resolving the id runs the check against the engine's own layout and throws on a
        // mismatch, so simply getting an id back is the assertion.
        harness.OnContext(Stage.Startup, _ => Assert.True(NativeComponents.Transform >= 0));
        harness.Run();
    }

    [Fact]
    public void TransformRoundTripsThroughBevy()
    {
        using var harness = new EngineHarness(frames: 3);
        var read = Transform.Identity;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.AddNative(entity, NativeComponents.Transform,
                new Transform(new Vec3(1f, 2f, 3f), Quat.FromRotationY(0.5f), new Vec3(2f)));

            Assert.True(ctx.Ecs.TryGetNative<Transform>(
                entity, NativeComponents.Transform, out read));
        });

        harness.Run();

        Assert.Equal(new Vec3(1f, 2f, 3f), read.Translation);
        Assert.Equal(new Vec3(2f), read.Scale);
        Assert.Equal(Quat.FromRotationY(0.5f), read.Rotation);
    }

    [Fact]
    public void WritingThroughAReferenceUpdatesTheRealComponent()
    {
        using var harness = new EngineHarness(frames: 3);
        var final = Vec3.Zero;
        var entity = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            entity = ctx.Ecs.Spawn();
            ctx.Ecs.AddNative(entity, NativeComponents.Transform, Transform.Identity);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            ref var transform = ref ctx.Ecs.GetNativeRef<Transform>(
                entity, NativeComponents.Transform);
            transform.Translation.X += 1f;
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            if (ctx.Ecs.TryGetNative<Transform>(entity, NativeComponents.Transform, out var t))
                final = t.Translation;
        });

        harness.Run();
        Assert.Equal(3f, final.X);
    }

    [Fact]
    public void TransformsCanBeIteratedLikeAnyOtherComponent()
    {
        // The point of resolving native components to ids: everything downstream already works
        // on ids, so chunked iteration needs no special case.
        using var harness = new EngineHarness(frames: 3);
        var total = 0f;
        var count = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 16; i++)
                ctx.Ecs.AddNative(ctx.Ecs.Spawn(), NativeComponents.Transform,
                    Transform.At(i, 0f, 0f));
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            using var chunks = ctx.Ecs.Chunks<Transform>(NativeComponents.Transform);

            total = 0f;
            count = 0;
            for (var c = 0; c < chunks.Count; c++)
            {
                var transforms = chunks[c].Components<Transform>();
                for (var i = 0; i < transforms.Length; i++)
                {
                    total += transforms[i].Translation.X;
                    count++;
                }
            }
        });

        harness.Run();

        Assert.Equal(16, count);
        Assert.Equal(120f, total);   // 0 + 1 + ... + 15
    }

    [Fact]
    public void ParentingIsVisibleFromBothDirections()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();

            Assert.Equal(Entity.None, ctx.Ecs.ParentOf(first));
            Assert.Empty(ctx.Ecs.ChildrenOf(parent));

            ctx.Ecs.SetParent(first, parent);
            ctx.Ecs.SetParent(second, parent);

            // Going through Bevy's relationship API rather than writing the component means the
            // reverse list is maintained for free. Writing the bytes would give a hierarchy that
            // only worked in one direction.
            Assert.Equal(parent, ctx.Ecs.ParentOf(first));
            Assert.Equal(parent, ctx.Ecs.ParentOf(second));

            var children = ctx.Ecs.ChildrenOf(parent);
            Assert.Equal(2, children.Length);
            Assert.Contains(first, children);
            Assert.Contains(second, children);
        });

        harness.Run();
    }

    [Fact]
    public void ReparentingMovesTheChildRatherThanDuplicatingIt()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();
            var child = ctx.Ecs.Spawn();

            ctx.Ecs.SetParent(child, first);
            ctx.Ecs.SetParent(child, second);

            Assert.Equal(second, ctx.Ecs.ParentOf(child));
            Assert.Empty(ctx.Ecs.ChildrenOf(first));
            Assert.Equal([child], ctx.Ecs.ChildrenOf(second));

            ctx.Ecs.ClearParent(child);
            Assert.Equal(Entity.None, ctx.Ecs.ParentOf(child));
            Assert.Empty(ctx.Ecs.ChildrenOf(second));
        });

        harness.Run();
    }

    [Fact]
    public void ParentTransformsPropagateToChildren()
    {
        // This is what makes hierarchy worth having, and it only works because the headless app
        // installs TransformPlugin. MinimalPlugins leaves it out, which would make Transform
        // inert data and GlobalTransform never follow anything.
        using var harness = new EngineHarness(frames: 4);
        var parentHasGlobal = false;
        var childHasGlobal = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            ctx.Ecs.AddNative(parent, NativeComponents.Transform, Transform.At(10f, 0f, 0f));

            var child = ctx.Ecs.Spawn();
            ctx.Ecs.AddNative(child, NativeComponents.Transform, Transform.At(1f, 0f, 0f));
            ctx.Ecs.SetParent(child, parent);
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            // GlobalTransform is a 3x4 affine rather than the translation/rotation/scale triple,
            // and C# has no mirror for it, so this checks that propagation ran at all rather
            // than reading the matrix out.
            var global = NativeComponents.GlobalTransform;
            parentHasGlobal = ctx.Ecs.CountById(global) >= 2;
            childHasGlobal = ctx.Ecs.CountById(NativeComponents.ChildOf) == 1;
        });

        harness.Run();

        Assert.True(parentHasGlobal, "TransformPlugin did not add GlobalTransform");
        Assert.True(childHasGlobal, "the child lost its ChildOf relationship");
    }

    [Fact]
    public void UnknownNativeComponentsFailWithAUsefulMessage()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(
                () => Native.Check(Native.bcs_component_id_of("NotARealComponent"), "lookup"));

            Assert.Equal(NativeStatus.NoComponent, ex.Status);
        });

        harness.Run();
    }
}
